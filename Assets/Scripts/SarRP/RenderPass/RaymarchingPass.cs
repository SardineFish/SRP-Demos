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
        public float near;
        public float far;
        public float step;
        public Material material;
        public override RenderPass CreateRenderPass()
        {
            return new RaymarchingRenderer(this);
        }
    }

    public class RaymarchingRenderer : RenderPassRenderer<RaymarchingPass>
    {
        Mesh screenMesh;
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

            cmd.SetGlobalVector("_WorldCameraPos", renderingData.camera.transform.position);
            cmd.SetGlobalVector("_CameraClipPlane", new Vector3(renderingData.camera.nearClipPlane, renderingData.camera.farClipPlane, renderingData.camera.farClipPlane - renderingData.camera.nearClipPlane));
            cmd.SetGlobalMatrix("_ViewProjectionInverseMatrix", Utility.ProjectionToWorldMatrix(renderingData.camera));
            //cmd.Blit(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CameraTarget, asset.material);

            var cubes = Resources.FindObjectsOfTypeAll<Test.VolumetricTestCube>();
            foreach(var cube in cubes)
            {
                cmd.SetGlobalVector("_CubeSize", cube.transform.localScale);
                cmd.SetGlobalVector("_CubePos", cube.transform.position);
                cmd.DrawRenderer(cube.GetComponent<MeshRenderer>(), asset.material, 0, 1);
            }

            //cmd.DrawMesh(screenMesh, Utility.ProjectionToWorldMatrix(renderingData.camera), asset.material);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }

}
