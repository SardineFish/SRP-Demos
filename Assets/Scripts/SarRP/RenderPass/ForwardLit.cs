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
            SetupLights(context, ref renderingData);
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
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SetupLights(context, ref renderingData);

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                SortingSettings sortingSettings = new SortingSettings(camera);
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("ForwardLit"), sortingSettings)
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
            var mainLightIdx = GetMainLightIndex(ref renderingData);
            if (mainLightIdx >= 0)
            {
                var mainLight = renderingData.lights[GetMainLightIndex(ref renderingData)];
                if (renderingData.shadowMapData.ContainsKey(mainLight.light))
                {
                    var shadowData = renderingData.shadowMapData[mainLight.light];
                    cmd.SetGlobalMatrix("_WorldToLight", shadowData.world2Light);
                    cmd.SetGlobalTexture("_ShadowMap", shadowData.shadowMapIdentifier);
                    cmd.SetGlobalFloat("_ShadowBias", shadowData.bias);
                    cmd.SetGlobalInt("_ShadowType", (int)shadowData.ShadowType);
                    cmd.SetGlobalVector("_ShadowParameters", shadowData.ShadowParameters);
                    cmd.SetGlobalMatrix("_ShadowPostTransform", shadowData.postTransform);
                }
                else
                {
                    cmd.SetGlobalMatrix("_WorldToLight", Matrix4x4.identity);
                    cmd.SetGlobalTexture("_ShadowMap", renderingData.DefaultShadowMap);
                }

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
