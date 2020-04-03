using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="Shadow", menuName ="SarRP/RenderPass/ShadowMap")]
    public class ShadowPass : RenderPassAsset
    {
        public override RenderPass CreateRenderPass()
        {
            return new ShadowPassRenderer(this);
        }
    }
    public partial class ShadowPassRenderer : RenderPassRenderer<ShadowPass>
    {
        const int PassSimple = 0;
        const int PassPSM = 1;
        const int PassTSM = 2;
        public Dictionary<Light, ShadowMapData> LightMaps = new Dictionary<Light, ShadowMapData>();
        Material shadowMapMat;
        int defaultShadowMap;
        public ShadowPassRenderer(ShadowPass asset) : base(asset)
        {
            shadowMapMat = new Material(Shader.Find("SarRP/Shadow/ShadowMap"));
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!shadowMapMat)
                shadowMapMat = new Material(Shader.Find("SarRP/Shadow/ShadowMap"));
            if (defaultShadowMap<=0)
            {
                var cmd = CommandBufferPool.Get();
                defaultShadowMap = Shader.PropertyToID("_DefaultShadowMapTex");
                cmd.GetTemporaryRT(defaultShadowMap, 16, 16, 32, FilterMode.Point, RenderTextureFormat.Depth);
                cmd.SetRenderTarget(defaultShadowMap, defaultShadowMap);
                cmd.ClearRenderTarget(true, true, Color.black);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
            renderingData.DefaultShadowMap = defaultShadowMap;
            LightMaps.Clear();
            base.Setup(context, ref renderingData);
        }

        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            for (var i = 0; i < renderingData.cullResults.visibleLights.Length; i++)
            {
                var light = renderingData.cullResults.visibleLights[i];
                if (light.light.GetComponent<ShadowSettings>() is ShadowSettings shadowSettings)
                {
                    if (!shadowSettings.Shadow)
                        continue;
                    ShadowMapData data = new ShadowMapData();
                    var hasData = false;
                    switch (shadowSettings.Algorithms)
                    {
                        case ShadowAlgorithms.Standard:
                            data = StandardShadowMap(context, renderingData, shadowSettings, i);
                            hasData = true;
                            break;
                        case ShadowAlgorithms.PSM:
                            data = PSMShadowMap(context, renderingData, shadowSettings, i);
                            hasData = true;
                            break;
                        case ShadowAlgorithms.TSM:
                            data = TSMShadowMap(context, renderingData, shadowSettings, i);
                            hasData = true;
                            break;
                    }
                    if (hasData)
                    {
                        renderingData.shadowMapData[light.light] = data;
                        LightMaps[light.light] = data;
                    }
                }
            }
        }

        
        
        

        
        void DrawShadowCasters(ScriptableRenderContext context, RenderingData renderingData, ShadowMapData shadowMapData, int pass)
        {
            var cmd = CommandBufferPool.Get();
            cmd.SetGlobalMatrix("_LightViewProjection", shadowMapData.world2Light);
            foreach (var renderer in GameObject.FindObjectsOfType<UnityEngine.Renderer>())
            {
                cmd.DrawRenderer(renderer, shadowMapMat, 0, pass);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            foreach(var light in renderingData.lights)
            {
                if (LightMaps.ContainsKey(light.light))
                {
                    IdentifierPool.Release(LightMaps[light.light].shadowMapIdentifier);
                    cmd.ReleaseTemporaryRT(LightMaps[light.light].shadowMapIdentifier);
                }
            }
            LightMaps.Clear();
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

    }

}
