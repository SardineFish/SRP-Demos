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
        public Dictionary<Light, ShadowMapData> LightMaps = new Dictionary<Light, ShadowMapData>();
        Material shadowMapMat;
        public ShadowPassRenderer(ShadowPass asset) : base(asset)
        {
            shadowMapMat = new Material(Shader.Find("SarRP/Shadow/ShadowMap"));
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!shadowMapMat)
                shadowMapMat = new Material(Shader.Find("SarRP/Shadow/ShadowMap"));
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
                    ShadowMapData data = new ShadowMapData();
                    var hasData = false;
                    switch (shadowSettings.Algorithms)
                    {
                        case ShadowAlgorithms.Simple:
                            data = SimpleShadowMap(context, renderingData, shadowSettings, i);
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

        ShadowMapData SimpleShadowMap(ScriptableRenderContext context, RenderingData renderingData, ShadowSettings settings, int lightIndex)
        {
            var cmd = CommandBufferPool.Get();
            cmd.Clear();
            var depthBuf = IdentifierPool.Get();
            var colorBuf = IdentifierPool.Get();
            cmd.GetTemporaryRT(depthBuf, settings.Resolution, settings.Resolution, 32, FilterMode.Point, RenderTextureFormat.Depth);

            cmd.GetTemporaryRT(colorBuf, settings.Resolution, settings.Resolution);
            RenderTargetBinding binding = new RenderTargetBinding();
            binding.depthRenderTarget = depthBuf;
            cmd.SetRenderTarget(depthBuf);
            cmd.ClearRenderTarget(true, true, Color.black);

            ShadowMapData shadowMapData = new ShadowMapData()
            {
                shadowMapIdentifier = depthBuf,
                bias = settings.Bias,
            };


            //var view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * settings.light.transform.worldToLocalMatrix;
            var (view, projection) = Utils.GetShadowViewProjection(settings, renderingData, lightIndex);
            //renderingData.cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, 0, 1, new Vector3(1, 0, 0), settings.Resolution, 0.1f, out var view, out var projection, out var shadowSplitData);
            //renderingData.cullResults.GetShadowCasterBounds(lightIndex, out var bounds);
            //Debug.DrawLine(bounds.min, bounds.max);
            cmd.SetViewProjectionMatrices(view, projection);
            shadowMapData.world2Light = projection * view;
            cmd.SetGlobalDepthBias(1, 1);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();


            /*FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            SortingSettings sortingSettings = new SortingSettings(renderingData.camera);
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("ForwardLit"), sortingSettings)
            {
                enableDynamicBatching = true,
                overrideMaterial = shadowMapMat,
                overrideMaterialPassIndex = 0,
            };
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);*/
            foreach(var renderer in  GameObject.FindObjectsOfType<MeshRenderer>())
            {
                cmd.DrawRenderer(renderer, shadowMapMat);
            }

            cmd.SetViewProjectionMatrices(renderingData.camera.worldToCameraMatrix, renderingData.camera.projectionMatrix);

            cmd.ReleaseTemporaryRT(colorBuf);
            IdentifierPool.Release(colorBuf);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);

            return shadowMapData;
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
