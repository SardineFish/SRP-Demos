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
        public int VolumeResolutionScale = 2;
        public bool GlobalFog = true;
        //[Range(0, 1)]
        //public float GlobalExtinction = .5f;
        public float VisibilityDistance = 20;
        [ColorUsage(false, true)]
        public Color FogLight;
        public Material Material;
        public Texture2D[] JitterPatterns;
        public override RenderPass CreateRenderPass()
        {
            return new LightVolumeRenderer(this);
        }
    }
    public class LightVolumeRenderer : RenderPassRenderer<LightVolumePass>
    {
        struct LightVolumeData
        {
            public Component.LightVolumeRenderer Volume;
            public int LightIndex;
            public int VolumeIndex;
        }
        const int PassVolumeDepth = 0;
        const int PassVolumeScattering = 1;
        const int PassFullScreenVolumeScattering = 2;
        const int PassVolumeResolve = 3;
        const int PassGlobalFog = 4;
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
                if (light.light.GetComponent<Component.LightVolumeRenderer>())
                {
                    visibleVolumes.Add(new LightVolumeData()
                    {
                        LightIndex = i,
                        VolumeIndex = visibleVolumes.Count,
                        Volume = light.light.GetComponent<Component.LightVolumeRenderer>(),
                    });
                }
            }
        }

        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //RenderVolumeDepth(context, renderingData);

            var cmd = CommandBufferPool.Get("Volumetric Light");
            using (new ProfilingSample(cmd, "Volumetric Light"))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                RenderLightVolume(context, renderingData);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
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

            var renderSize = new Vector2Int(renderingData.camera.pixelWidth, renderingData.camera.pixelHeight) / asset.VolumeResolutionScale;
            var rt = IdentifierPool.Get();
            cmd.GetTemporaryRT(rt, renderSize.x, renderSize.y, 0, FilterMode.Point, RenderTextureFormat.Default);
            cmd.SetRenderTarget(rt, rt);
            cmd.ClearRenderTarget(false, true, Color.black);

            cmd.SetGlobalTexture("_CameraDepthTex", renderingData.DepthTarget);
            cmd.SetCameraParams(renderingData.camera, true);

            float globalExtinction = Mathf.Log(1 / (0.1f)) / asset.VisibilityDistance;

            foreach (var volumeData in visibleVolumes)
            {
                if (!volumeData.Volume.enabled)
                    continue;
                var light = renderingData.cullResults.visibleLights[volumeData.LightIndex];
                Vector4 lightPos;
                if (light.lightType == LightType.Directional)
                    lightPos = (-light.light.transform.forward).ToVector4(0);
                else
                    lightPos = light.light.transform.position.ToVector4(1);
                cmd.SetGlobalVector("_LightPosition", lightPos);
                cmd.SetGlobalVector("_LightDirection", -light.light.transform.forward);
                cmd.SetGlobalFloat("_LightAngle", Mathf.Cos(Mathf.Deg2Rad * light.spotAngle / 2));
                cmd.SetGlobalVector("_LightColor", light.finalColor * volumeData.Volume.IntensityMultiplier);
                cmd.SetGlobalVector("_WorldCameraPos", renderingData.camera.transform.position);
                cmd.SetGlobalVector("_FrameSize", new Vector4(renderSize.x, renderSize.y, 1f / renderSize.x, 1f / renderSize.y));
                cmd.SetGlobalInt("_Steps", volumeData.Volume.RayMarchingSteps);
                cmd.SetGlobalVector("_RangeLimit", volumeData.Volume.RayMarchingRange);
                cmd.SetGlobalFloat("_IncomingLoss", volumeData.Volume.IncomingLoss);
                cmd.SetGlobalFloat("_LightDistance", volumeData.Volume.LightDistance);
                var extinction = globalExtinction;
                if (volumeData.Volume.ExtinctionOverride)
                    extinction = Mathf.Log(1 / (0.1f)) / volumeData.Volume.VisibilityDistance;
                cmd.SetGlobalVector("_TransmittanceExtinction", new Vector3(extinction, extinction, extinction));
                if (asset.JitterPatterns.Length > 0)
                    cmd.SetGlobalTexture("_SampleNoise", asset.JitterPatterns[renderingData.FrameID % asset.JitterPatterns.Length]);

                if (renderingData.shadowMapData.ContainsKey(volumeData.Volume.light))
                {
                    var shadowData = renderingData.shadowMapData[volumeData.Volume.light];
                    cmd.SetGlobalTexture("_ShadowMap", shadowData.shadowMapIdentifier);
                    cmd.SetGlobalMatrix("_WorldToLight", shadowData.world2Light);

                    cmd.SetGlobalMatrix("_WorldToLight", shadowData.world2Light);
                    cmd.SetGlobalTexture("_ShadowMap", shadowData.shadowMapIdentifier);
                    cmd.SetGlobalFloat("_ShadowBias", shadowData.bias);
                    cmd.SetGlobalInt("_ShadowType", (int)shadowData.ShadowType);
                    cmd.SetGlobalVector("_ShadowParameters", shadowData.ShadowParameters);
                    cmd.SetGlobalMatrix("_ShadowPostTransform", shadowData.postTransform);
                    //cmd.SetGlobalMatrix("_")
                    cmd.SetGlobalInt("_UseShadow", 1);
                }
                else
                {
                    cmd.SetGlobalInt("_UseShadow", 0);
                }

                var boundaryPlanes = volumeData.Volume.GetVolumeBoundFaces(renderingData.camera);
                cmd.SetGlobalVectorArray("_BoundaryPlanes", boundaryPlanes);
                cmd.SetGlobalInt("_BoundaryPlaneCount", boundaryPlanes.Count);

                switch (light.lightType)
                {
                    case LightType.Point:
                    case LightType.Spot:
                        cmd.DrawMesh(volumeData.Volume.VolumeMesh, volumeData.Volume.transform.localToWorldMatrix, volumeMat, 0, PassVolumeScattering);
                        break;
                    case LightType.Directional:
                        cmd.BlitFullScreen(BuiltinRenderTextureType.None, rt, volumeMat, PassFullScreenVolumeScattering);
                        break;
                }
                
                //cmd.DrawMesh(volumeData.Volume.VolumeMesh, volumeData.Volume.transform.localToWorldMatrix, volumeMat, 0, PassVolumeScattering);
            }

            cmd.SetGlobalTexture("_CameraDepthTex", renderingData.DepthTarget);
            cmd.SetGlobalFloat("_GlobalFogExtinction", globalExtinction);
            cmd.SetGlobalColor("_AmbientLight", asset.FogLight);
            //cmd.Blit(BuiltinRenderTextureType.None, renderingData.ColorTarget, volumeMat, PassGlobalFog);
            cmd.BlitFullScreen(BuiltinRenderTextureType.None, renderingData.ColorTarget, volumeMat, PassGlobalFog);

            cmd.Blit(rt, renderingData.ColorTarget, volumeMat, PassVolumeResolve);


            //cmd.ReleaseTemporaryRT(rt);
            //IdentifierPool.Release(rt);
            cmd.ReleaseTemporaryRT(rt);
            IdentifierPool.Release(rt);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            
        }
        public override void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            //cmd.ReleaseTemporaryRT(VolumeDepthTex);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}
