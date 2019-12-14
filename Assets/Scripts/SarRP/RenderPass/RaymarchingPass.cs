using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="RaymarchingPass", menuName ="SarRP/RenderPass/Raymarching")]
    public class RaymarchingPass : RenderPassAsset
    {
        public bool DrawFullScreen = false;
        public float near;
        public float far;
        public float step;
        public Material material;
        public ComputeShader CurlNoiseMotionComputeShader;
        public RenderTexture CurlNoiseTexture;
        public override RenderPass CreateRenderPass()
        {
            return new RaymarchingRenderer(this);
        }
    }

    public class RaymarchingRenderer : RenderPassRenderer<RaymarchingPass>
    {
        Mesh screenMesh;
        Noise.CurlNoiseMotionRenderer curlNoiseMotionRenderer;
        public RaymarchingRenderer(RaymarchingPass asset) : base(asset)
        {
            screenMesh = Utility.GenerateFullScreenQuad();
        }
        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.camera.cameraType == CameraType.Preview)
                return;
            if (!asset.material)
                return;
            var cmd = CommandBufferPool.Get("Ray-marching");
            cmd.Clear();
            cmd.BeginSample("Volumetric Cloud Rendering");

            if(!(curlNoiseMotionRenderer is null))
            {
                var buffer = curlNoiseMotionRenderer.Update(cmd);
                cmd.SetGlobalBuffer("_MotionPosBuffer", buffer);
                var size = curlNoiseMotionRenderer.Size;
                cmd.SetGlobalVector("_MotionPosBufferSize", new Vector3(size.x, size.y, size.z));
            }

            cmd.SetGlobalVector("_WorldCameraPos", renderingData.camera.transform.position);
            cmd.SetGlobalVector("_CameraClipPlane", new Vector3(renderingData.camera.nearClipPlane, renderingData.camera.farClipPlane, renderingData.camera.farClipPlane - renderingData.camera.nearClipPlane));
            cmd.SetGlobalMatrix("_ViewProjectionInverseMatrix", Utility.ProjectionToWorldMatrix(renderingData.camera));
            //cmd.Blit(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CameraTarget, asset.material);

            var curlNoiseMotion = Resources.FindObjectsOfTypeAll<Noise.CurlNoiseMotion2D>().FirstOrDefault();
            if(curlNoiseMotion)
            {
                cmd.SetGlobalTexture("_CurlNoiseMotionTex", curlNoiseMotion.CurrentMotionTexture);
            }

            if(asset.DrawFullScreen)
                cmd.DrawMesh(screenMesh, Utility.ProjectionToWorldMatrix(renderingData.camera), asset.material, 0, 0);
            else
            {
                var cubes = Resources.FindObjectsOfTypeAll<Test.VolumetricTestCube>();
                foreach (var cube in cubes)
                {
                    cmd.SetGlobalVector("_CubeSize", cube.transform.localScale);
                    cmd.SetGlobalVector("_CubePos", cube.transform.position);
                    cmd.DrawRenderer(cube.GetComponent<MeshRenderer>(), asset.material, 0, 1);
                }
            }


            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(curlNoiseMotionRenderer is null && asset.CurlNoiseMotionComputeShader && asset.CurlNoiseTexture)
            {
                curlNoiseMotionRenderer = new Noise.CurlNoiseMotionRenderer(asset.CurlNoiseTexture, asset.CurlNoiseMotionComputeShader, new Vector3Int(asset.CurlNoiseTexture.width, asset.CurlNoiseTexture.height, asset.CurlNoiseTexture.volumeDepth));
            }
            SetupLights(context, ref renderingData);
            var cmd = CommandBufferPool.Get();

            for (var i = 0; i < renderingData.cullResults.visibleLights.Length; i++)
            {
                var light = renderingData.cullResults.visibleLights[i];
                
            }
        }

        void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            renderingData.lights = renderingData.cullResults.visibleLights;
            var mainLightIdx = GetMainLightIndex(ref renderingData);
            if (mainLightIdx >= 0)
            {
                var mainLight = renderingData.lights[GetMainLightIndex(ref renderingData)];

                cmd.SetGlobalColor("_MainLightColor", mainLight.finalColor);
                cmd.SetGlobalVector("_MainLightDirection", mainLight.light.transform.forward);
            }
            else
            {
                cmd.SetGlobalColor("_MainLightColor", Color.black);
                cmd.SetGlobalVector("_MainLightPosition", Vector4.zero);
            }
            cmd.SetGlobalColor("_AmbientSkyColor", RenderSettings.ambientSkyColor);
            cmd.EndSample("Volumetric Cloud Rendering");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        int GetMainLightIndex(ref RenderingData renderingData)
        {
            var lights = renderingData.cullResults.visibleLights;
            var sun = RenderSettings.sun;
            if (sun == null)
                return -1;
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i].light == sun)
                    return i;
            }
            return -1;
        }
    }

}
