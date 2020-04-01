using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="TAA", menuName ="SarRP/RenderPass/TAA")]
    public class TAAPass : RenderPassAsset
    {
        public int Samples = 4;
        [Range(0, 1)]
        public float BlendAlpha = 0.1f;
        public override RenderPass CreateRenderPass()
        {
            return new TAARenderer(this);
        }
    }
    public class TAARenderer : RenderPassRenderer<TAAPass>
    {
        enum HistoricalBuffer : int
        {
            Color=1,
            Depth,
            Velocity,
        }

        public static Vector2[] Pattern4 = new Vector2[]
        {
            new Vector2(.25f, .25f),
            new Vector2(.75f, .25f),
            new Vector2(.75f, .75f),
            new Vector2(.25f, .75f),
        };
        HistoricalRTSystem HistoricalRT = new HistoricalRTSystem();
        Material material;


        public TAARenderer(TAAPass asset) : base(asset)
        {
        }
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!material)
                material = new Material(Shader.Find("SarRP/TAA"));
            if (asset.Samples == 4)
            {
                renderingData.NextProjectionJitter = Pattern4[renderingData.FrameID % 4];
            }
            HistoricalRT.Swap();
        }
        int previousColor;
        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("TAA Resolve");
            var (previousColor, nextColor) = GetHistoricalColorBuffer(renderingData);
            //var rt = RenderTarget.GetTemporary(cmd, renderingData.camera.pixelWidth, renderingData.camera.pixelHeight, 0, FilterMode.Point, renderingData.ColorBufferFormat);
            //var rt = IdentifierPool.Get();
            /*RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor()
            {
                width = renderingData.camera.pixelWidth,
                height = renderingData.camera.pixelHeight,
                depthBufferBits = 0,
                colorFormat = renderingData.ColorBufferFormat,
                memoryless = RenderTextureMemoryless.None,
                msaaSamples = 1,
                dimension = TextureDimension.Tex2D,
            };*/
            //cmd.GetTemporaryRT(rt, renderingData.camera.pixelWidth, renderingData.camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.Default);
            /*if(renderingData.FrameID == 0 || previousFrame.ColorBuffer == null)
            {
                cmd.Blit(renderingData.ColorTarget, rt);
            }
            else*/
            {
                cmd.SetGlobalTexture("_PreviousFrameBuffer", previousColor);
                cmd.SetGlobalTexture("_CurrentFrameBuffer", renderingData.ColorTarget);
                cmd.SetGlobalFloat("_Alpha", asset.BlendAlpha);
                cmd.Blit(renderingData.ColorTarget, nextColor, material, 0);
                cmd.Blit(nextColor, renderingData.ColorTarget);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        (RenderTexture previous, RenderTexture next) GetHistoricalColorBuffer(RenderingData renderingData)
        {
            RenderTexture allocator()
            {
                var rt = new RenderTexture(renderingData.camera.pixelWidth, renderingData.camera.pixelHeight, 0);
                rt.dimension = TextureDimension.Tex2D;
                rt.antiAliasing = 1;
                rt.format = renderingData.ColorBufferFormat;
                rt.filterMode = FilterMode.Bilinear;
                rt.memorylessMode = RenderTextureMemoryless.None;
                rt.Create();
                return rt;
            }
            return (HistoricalRT.GetPrevious((int)HistoricalBuffer.Color, allocator), HistoricalRT.GetNext((int)HistoricalBuffer.Color, allocator));
        }
        public override void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }
    }
}
