using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="TransparentPass", menuName ="SarRP/RenderPass/Transparent")]
    public class TransparentPass : RenderPassAsset
    {
        public bool UseDepthPeeling = false;
        [Range(1, 16)]
        public int DepthPeelingPass = 4;
        public override RenderPass CreateRenderPass()
        {
            return new TransparentPassRenderer(this);
        }
    }

    public class TransparentPassRenderer : RenderPassRenderer<TransparentPass>
    {
        public TransparentPassRenderer(TransparentPass asset) : base(asset) { }
        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("RenderTransparent");
            using (new ProfilingSample(cmd, "RenderTransparent"))
            {
                // Start profilling
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);

                if (!asset.UseDepthPeeling)
                    RenderDefaultTransparent(context, ref renderingData);
                else
                    RenderDepthPeeling(context, ref renderingData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void RenderDefaultTransparent(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.camera;
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            SortingSettings sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("TransparentBack"), sortingSettings)
            {
                enableDynamicBatching = true,
                perObjectData = PerObjectData.ReflectionProbes,
            };
            drawingSettings.SetShaderPassName(1, new ShaderTagId("TransparentFront"));
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
        }
        void RenderDepthPeeling(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.camera;
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            SortingSettings sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("DepthPeelingFirstPass"), sortingSettings)
            {
                enableDynamicBatching = true,
                perObjectData = PerObjectData.ReflectionProbes,
            };
            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            var cmd = CommandBufferPool.Get("Depth Peeling");
            using (new ProfilingSample(cmd, "Depth Peeling"))
            {
                // Start profilling
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                List<int> colorRTs = new List<int>(asset.DepthPeelingPass);
                List<int> depthRTs = new List<int>(asset.DepthPeelingPass);

                // Perform depth peeling
                for (var i = 0; i < asset.DepthPeelingPass; i++)
                {
                    depthRTs.Add(Shader.PropertyToID($"_DepthPeelingDepth{i}"));
                    colorRTs.Add(Shader.PropertyToID($"_DepthPeelingColor{i}"));
                    cmd.GetTemporaryRT(colorRTs[i], camera.pixelWidth, camera.pixelHeight, 0);
                    cmd.GetTemporaryRT(depthRTs[i], camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.RFloat);

                    if (i == 0)
                    {
                        drawingSettings.SetShaderPassName(0, new ShaderTagId("DepthPeelingFirstPass"));

                        cmd.SetRenderTarget(new RenderTargetIdentifier[] { colorRTs[i], depthRTs[i] }, depthRTs[i]);
                        cmd.ClearRenderTarget(true, true, Color.black);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
                    }
                    else
                    {
                        cmd.SetGlobalTexture("_MaxDepthTex", depthRTs[i - 1]);
                        drawingSettings.SetShaderPassName(0, new ShaderTagId("DepthPeelingPass"));

                        cmd.SetRenderTarget(new RenderTargetIdentifier[] { colorRTs[i], depthRTs[i] }, depthRTs[i]);
                        cmd.ClearRenderTarget(true, true, Color.black);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
                    }
                }
                
                cmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
                var mat = new Material(Shader.Find("SarRP/Transparent"));
                for (var i = asset.DepthPeelingPass - 1; i >= 0; i--)
                {
                    cmd.SetGlobalTexture("_DepthTex", depthRTs[i]);
                    cmd.Blit(colorRTs[i], renderingData.ColorTarget, mat, 4);

                    cmd.ReleaseTemporaryRT(depthRTs[i]);
                    cmd.ReleaseTemporaryRT(colorRTs[i]);
                }
                cmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }

}
