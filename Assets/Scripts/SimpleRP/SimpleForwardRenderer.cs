using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleRP
{
    public static class SimpleForwardRenderer
    {

        public static void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, SimpleRenderPipelineAsset settings)
        {
            cullingParameters.shadowDistance = settings.MaxShadowDistance;
        }

        public static void RenderOpaque(ScriptableRenderContext context, RenderingData renderingData)
        {
            var camera = renderingData.camera;

            SetupLights(context, ref renderingData);

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            SortingSettings sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("SimpleForward"), sortingSettings)
            {
                mainLightIndex = GetMainLightIndex(ref renderingData),
                enableDynamicBatching = true
            };
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
        }

        public static void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
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
            cmd.SetGlobalColor("_AmbientColor", RenderSettings.ambientLight);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        static int GetMainLightIndex(ref RenderingData renderingData)
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
