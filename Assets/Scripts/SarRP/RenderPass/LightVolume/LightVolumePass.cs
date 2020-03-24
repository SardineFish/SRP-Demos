using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="LightVolume", menuName ="SarRP/RenderPass/LightVolume")]
    public class LightVolumePass : RenderPassAsset
    {
        public int VolumeDepthResolutionScale = 2;
        public Material Material;
        public override RenderPass CreateRenderPass()
        {
            return new LightVolumeRenderer(this);
        }
    }
    public class LightVolumeRenderer : RenderPassRenderer<LightVolumePass>
    {
        struct LightVolumeData
        {
            public Component.LightVolume Volume;
            public int LightIndex;
            public int VolumeIndex;
        }
        const int PassVolumeDepth = 0;
        const int PassVolumeScattering = 1;
        List<LightVolumeData> visibleVolumes = new List<LightVolumeData>();
        int VolumeDepthTex = -1;

        Material volumeMat => asset.Material;

        public LightVolumeRenderer(LightVolumePass asset) : base(asset)
        {
            VolumeDepthTex = Shader.PropertyToID("_VolumeDepthTexture");
        }
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            visibleVolumes.Clear();
            for (var i = 0; i < renderingData.cullResults.visibleLights.Length; i++)
            {
                var light = renderingData.cullResults.visibleLights[i];
                if (light.light.GetComponent<Component.LightVolume>())
                {
                    visibleVolumes.Add(new LightVolumeData()
                    {
                        LightIndex = i,
                        VolumeIndex = visibleVolumes.Count,
                        Volume = light.light.GetComponent<Component.LightVolume>(),
                    });
                }
            }
            var cmd = CommandBufferPool.Get();
            var rt = new RenderTextureDescriptor()
            {
                width = renderingData.camera.pixelWidth / asset.VolumeDepthResolutionScale,
                height = renderingData.camera.pixelHeight / asset.VolumeDepthResolutionScale,
                colorFormat = RenderTextureFormat.RGHalf,
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = visibleVolumes.Count,
                useMipMap = false,
                enableRandomWrite = true,
                msaaSamples = 1
            };
            cmd.GetTemporaryRT(VolumeDepthTex, rt, FilterMode.Point);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RenderVolumeDepth(context, renderingData);
            RenderLightVolume(context, renderingData);
        }
        void RenderVolumeDepth(ScriptableRenderContext context, RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Light Volume Depth");
            var debugRT = IdentifierPool.Get();
            cmd.GetTemporaryRT(debugRT, renderingData.camera.pixelWidth, renderingData.camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat);
            cmd.SetRenderTarget(debugRT);
            cmd.SetGlobalTexture("_RWVolumeDepthTexture", VolumeDepthTex);
            
            foreach (var volumeData in visibleVolumes)
            {
                cmd.SetGlobalInt("_VolumeIndex", volumeData.VolumeIndex);
                cmd.DrawMesh(volumeData.Volume.VolumeMesh, volumeData.Volume.transform.localToWorldMatrix, volumeMat, 0, PassVolumeDepth);
            }

            cmd.ReleaseTemporaryRT(debugRT);
            IdentifierPool.Release(debugRT);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        void RenderLightVolume(ScriptableRenderContext context, RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Light Volume");
            cmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
            cmd.SetGlobalTexture("_CameraDepth", renderingData.DepthTarget);

            foreach(var volumeData in visibleVolumes)
            {
                var light = renderingData.cullResults.visibleLights[volumeData.LightIndex];
                Vector4 lightPos;
                if (light.lightType == LightType.Directional)
                    lightPos = (-light.light.transform.forward).ToVector4(0);
                else
                    lightPos = light.light.transform.position.ToVector4(1);
                cmd.SetGlobalVector("_LightPosition", lightPos);
                cmd.SetGlobalVector("_LightDirection", -light.light.transform.forward);
                cmd.SetGlobalFloat("_LightAngle", Mathf.Cos(Mathf.Deg2Rad * light.spotAngle / 2));
                cmd.SetGlobalVector("_LightColor", light.finalColor);
                cmd.SetGlobalVector("_WorldCameraPos", renderingData.camera.transform.position);

                if(renderingData.shadowMapData.ContainsKey(volumeData.Volume.light))
                {
                    var shadowData = renderingData.shadowMapData[volumeData.Volume.light];
                    cmd.SetGlobalTexture("_ShadowMap", shadowData.shadowMapIdentifier);
                    cmd.SetGlobalMatrix("_LightProjectionMatrix", shadowData.world2Light);
                    cmd.SetGlobalInt("_UseShadow", 1);
                }
                else
                {
                    cmd.SetGlobalInt("_UseShadow", 0);
                }

                var boundaryPlanes = volumeData.Volume.GetVolumeBoundFaces(renderingData.camera);
                cmd.SetGlobalVectorArray("_BoundaryPlanes", boundaryPlanes);
                cmd.SetGlobalInt("_BoundaryPlaneCount", boundaryPlanes.Count);

                cmd.DrawMesh(volumeData.Volume.VolumeMesh, volumeData.Volume.transform.localToWorldMatrix, volumeMat, 0, PassVolumeScattering);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            
        }
        public override void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(VolumeDepthTex);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}
