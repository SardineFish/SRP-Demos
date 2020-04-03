using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="ForwardLit",menuName = "SarRP/RenderPass/ForwardLit")]
    public class ForwardLit : RenderPassAsset
    {
        public override RenderPass CreateRenderPass()
        {
            return new ForwardLitPass(this);
        }
    }

    public class ForwardLitPass : RenderPassRenderer<ForwardLit>
    {
        public ForwardLitPass(ForwardLit asset) : base(asset) { }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SetupGlobalLight(context, ref renderingData);
        }

        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.camera;
            var cmd = CommandBufferPool.Get("RenderOpaque");
            using (new ProfilingSample(cmd, "RenderOpaque"))
            {
                // Start profilling
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
                cmd.SetViewProjectionMatrices(renderingData.ViewMatrix, renderingData.JitteredProjectionMatrix);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SetupGlobalLight(context, ref renderingData);

                var mainLightIndex = GetMainLightIndex(ref renderingData);

                // Render Main Light
                if (mainLightIndex >= 0)
                {
                    RenderLight(context, ref renderingData, mainLightIndex, new ShaderTagId("ForwardBase"));
                }

                for (var i = 0; i < renderingData.cullResults.visibleLights.Length; i++)
                {
                    if (i == mainLightIndex)
                        continue;
                    RenderLight(context, ref renderingData, i, new ShaderTagId("ForwardAdd"));
                }

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void RenderLight(ScriptableRenderContext context, ref RenderingData renderingData, int lightIndex, ShaderTagId shaderTagId)
        {
            SetupLight(context, renderingData, lightIndex);

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            SortingSettings sortingSettings = new SortingSettings(renderingData.camera);
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings)
            {
                mainLightIndex = GetMainLightIndex(ref renderingData),
                enableDynamicBatching = true,
            };
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
        }

        void SetupLight(ScriptableRenderContext context, RenderingData renderingData, int lightIndex)
        {
            var cmd = CommandBufferPool.Get();
            var light = renderingData.cullResults.visibleLights[lightIndex];
            if(light.lightType == LightType.Directional)
            {
                cmd.SetGlobalVector("_LightPosition", -light.light.transform.forward.ToVector4(0));
                cmd.SetGlobalColor("_LightColor", light.finalColor);
                cmd.SetGlobalVector("_LightDirection", -light.light.transform.forward);
                cmd.SetGlobalFloat("_LightCosHalfAngle", -2);
            }
            else
            {
                cmd.SetGlobalVector("_LightPosition", light.light.transform.position.ToVector4(1));
                cmd.SetGlobalColor("_LightColor", light.finalColor);
                cmd.SetGlobalVector("_LightDirection", -light.light.transform.forward.normalized);
                if (light.lightType == LightType.Spot)
                    cmd.SetGlobalFloat("_LightCosHalfAngle", Mathf.Cos(Mathf.Deg2Rad * light.spotAngle / 2));
                else
                    cmd.SetGlobalFloat("_LightCosHalfAngle", -2);
            }

            if (renderingData.shadowMapData.ContainsKey(light.light))
            {
                var shadowData = renderingData.shadowMapData[light.light];
                cmd.SetGlobalInt("_UseShadow", 1);
                cmd.SetGlobalMatrix("_WorldToLight", shadowData.world2Light);
                cmd.SetGlobalTexture("_ShadowMap", shadowData.shadowMapIdentifier);
                cmd.SetGlobalFloat("_ShadowBias", shadowData.bias);
                cmd.SetGlobalInt("_ShadowType", (int)shadowData.ShadowType);
                cmd.SetGlobalVector("_ShadowParameters", shadowData.ShadowParameters);
                cmd.SetGlobalMatrix("_ShadowPostTransform", shadowData.postTransform);
            }
            else
            {
                cmd.SetGlobalInt("_UseShadow", 0);
                cmd.SetGlobalMatrix("_WorldToLight", Matrix4x4.identity);
                cmd.SetGlobalTexture("_ShadowMap", renderingData.DefaultShadowMap);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void SetupGlobalLight(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            var mainLightIdx = GetMainLightIndex(ref renderingData);
            if (mainLightIdx >= 0)
            {
                var mainLight = renderingData.lights[GetMainLightIndex(ref renderingData)];
                

                if (mainLight.light.type == LightType.Directional)
                    cmd.SetGlobalVector("_MainLightPosition", -mainLight.light.transform.forward.ToVector4(0));
                else
                    cmd.SetGlobalVector("_MainLightPosition", mainLight.light.transform.position.ToVector4(1));
                cmd.SetGlobalColor("_MainLightColor", mainLight.finalColor);
            }
            else
            {
                cmd.SetGlobalColor("_MainLightColor", Color.black);
                cmd.SetGlobalVector("_MainLightPosition", Vector4.zero);
            }
            cmd.SetGlobalColor("_AmbientLight", RenderSettings.ambientLight);
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
