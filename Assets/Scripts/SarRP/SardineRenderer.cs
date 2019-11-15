using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    public class SardineRenderer
    {
        RenderTargetIdentifier cameraColorTarget;
        RenderTargetIdentifier cameraDepthTarget;
        public void Reset()
        {
            cameraColorTarget = BuiltinRenderTextureType.CameraTarget;
            cameraDepthTarget = BuiltinRenderTextureType.CameraTarget;
        }

        public void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, SardineRenderPipelineAsset settings)
        {
            cullingParameters.shadowDistance = settings.MaxShadowDistance;
        }

        public void RenderOpaque(ScriptableRenderContext context, RenderingData renderingData)
        {
            var camera = renderingData.camera;
            var cmd = CommandBufferPool.Get("RenderOpaque");
            using (new ProfilingSample(cmd, "RenderOpaque"))
            {
                // Start profilling
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SetupLights(context, ref renderingData);

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                SortingSettings sortingSettings = new SortingSettings(camera);
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("ForwardOpaque"), sortingSettings)
                {
                    mainLightIndex = GetMainLightIndex(ref renderingData),
                    enableDynamicBatching = true
                };
                RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
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
