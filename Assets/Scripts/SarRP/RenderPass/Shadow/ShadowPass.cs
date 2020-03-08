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

        ShadowMapData StandardShadowMap(ScriptableRenderContext context, RenderingData renderingData, ShadowSettings settings, int lightIndex)
        {
            var cmd = CommandBufferPool.Get();
            cmd.Clear();
            var depthBuf = IdentifierPool.Get();

            cmd.GetTemporaryRT(depthBuf, settings.Resolution, settings.Resolution, 32, FilterMode.Point, RenderTextureFormat.Depth);

            RenderTargetBinding binding = new RenderTargetBinding();
            binding.depthRenderTarget = depthBuf;
            cmd.SetRenderTarget(depthBuf);
            cmd.ClearRenderTarget(true, true, Color.black);

            ShadowMapData shadowMapData = new ShadowMapData()
            {
                shadowMapIdentifier = depthBuf,
                bias = settings.Bias,
                ShadowType = ShadowAlgorithms.Standard,
            };


            //var view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * settings.light.transform.worldToLocalMatrix;
            var (view, projection) = Utils.GetShadowViewProjection(settings, renderingData, lightIndex);
            //renderingData.cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, 0, 1, new Vector3(1, 0, 0), settings.Resolution, 0.1f, out var view, out var projection, out var shadowSplitData);
            //renderingData.cullResults.GetShadowCasterBounds(lightIndex, out var bounds);
            //Debug.DrawLine(bounds.min, bounds.max);
            cmd.SetViewProjectionMatrices(view, projection);
            shadowMapData.world2Light = projection * view;
            cmd.SetGlobalDepthBias(settings.DepthBias, settings.NormalBias);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();


            DrawShadowCasters(context, renderingData, shadowMapData, PassSimple);

            cmd.SetViewProjectionMatrices(renderingData.camera.worldToCameraMatrix, renderingData.camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);

            return shadowMapData;
        }
        
        ShadowMapData PSMShadowMap(ScriptableRenderContext context, RenderingData renderingData, ShadowSettings settings, int lightIndex)
        {
            var (view, projection, inverseZ) = Utils.PSMProjection(lightIndex, renderingData);
            //Debug.Log(inverseZ);
            Vector4 p = new Vector4(-0.46017f, 0.16764f, 0.01015f, 1.00f);
            var p1 = projection * view * p;
            var p2 = GL.GetGPUProjectionMatrix(projection, false) * Matrix4x4.Scale(new Vector3(1, 1, -1)) * view * p;

            ShadowMapData shadowMapData = new ShadowMapData()
            {
                shadowMapIdentifier = IdentifierPool.Get(),
                world2Light = GL.GetGPUProjectionMatrix(projection, true) * Matrix4x4.Scale(new Vector3(1, 1, -1)) * view,
                bias = settings.Bias,
                ShadowType = ShadowAlgorithms.PSM,
                ShadowParameters = new Vector4(inverseZ ? 1 : 0, 0, 0),
            };

            var cmd = CommandBufferPool.Get();
            cmd.GetTemporaryRT(shadowMapData.shadowMapIdentifier, settings.Resolution, settings.Resolution, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(shadowMapData.shadowMapIdentifier);
            cmd.SetGlobalVector("_ShadowParameters", shadowMapData.ShadowParameters);
            cmd.SetGlobalDepthBias(1, 1);
            //cmd.SetViewProjectionMatrices(renderingData.camera.worldToCameraMatrix, renderingData.camera.projectionMatrix);
            cmd.ClearRenderTarget(true, true, Color.black);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            DrawShadowCasters(context, renderingData, shadowMapData, PassPSM);

            CommandBufferPool.Release(cmd);

            return shadowMapData;
        }

        ShadowMapData TSMShadowMap(ScriptableRenderContext context, RenderingData renderingData, ShadowSettings settings, int lightIndex)
        {
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var (view, projection) = Utils.GetShadowViewProjection(settings, renderingData, lightIndex);

            var cmd = CommandBufferPool.Get();
            cmd.Clear();
            var depthBuf = IdentifierPool.Get();

            cmd.GetTemporaryRT(depthBuf, settings.Resolution, settings.Resolution, 32, FilterMode.Point, RenderTextureFormat.Depth);

            RenderTargetBinding binding = new RenderTargetBinding();
            binding.depthRenderTarget = depthBuf;
            cmd.SetRenderTarget(depthBuf);
            cmd.ClearRenderTarget(true, true, Color.black);

            ShadowMapData shadowMapData = new ShadowMapData()
            {
                shadowMapIdentifier = depthBuf,
                bias = settings.Bias,
                ShadowType = ShadowAlgorithms.TSM,
                world2Light = GL.GetGPUProjectionMatrix(projection, true) * view,
            };

            var trapezoidalTransfrom = Utils.TSMTransform(camera, shadowMapData.world2Light, settings);
            shadowMapData.postTransform = trapezoidalTransfrom;

            
            cmd.SetViewProjectionMatrices(view, projection);
            cmd.SetGlobalDepthBias(settings.DepthBias, settings.NormalBias);
            cmd.SetGlobalMatrix("_ShadowPostTransform", trapezoidalTransfrom);
            cmd.SetGlobalFloat("_SlopeDepthBias", -settings.NormalBias);
            cmd.SetGlobalFloat("_DepthBias", -Mathf.Pow(2, settings.DepthBias));

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();


            DrawShadowCasters(context, renderingData, shadowMapData, PassTSM);

            cmd.SetViewProjectionMatrices(renderingData.camera.worldToCameraMatrix, renderingData.camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);

            return shadowMapData;
        }

        void DrawShadowCasters(ScriptableRenderContext context, RenderingData renderingData, ShadowMapData shadowMapData, int pass)
        {
            var cmd = CommandBufferPool.Get();
            cmd.SetGlobalMatrix("_LightViewProjection", shadowMapData.world2Light);
            foreach (var renderer in GameObject.FindObjectsOfType<MeshRenderer>())
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
