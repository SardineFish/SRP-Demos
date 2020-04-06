using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    public enum SamplingPatterns
    {
        Halton2_3,
        Uniform,
    }
    [CreateAssetMenu(fileName ="TAA", menuName ="SarRP/RenderPass/TAA")]
    public class TAAPass : RenderPassAsset
    {
        public SamplingPatterns SamplingPatterns;
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

        public static Vector2[] Halton8 = Sampler.HaltonSequence2(2, 3).Skip(1).Take(8).ToArray();
        public static Vector2[] Halton16 = Sampler.HaltonSequence2(2, 3).Skip(1).Take(16).ToArray();

        List<Vector2> patterns = new List<Vector2>(16);

        HistoricalRTSystem HistoricalRT = new HistoricalRTSystem();
        Material material;


        public TAARenderer(TAAPass asset) : base(asset)
        {
        }
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!material)
                material = new Material(Shader.Find("SarRP/TAA"));

            if (patterns.Capacity < asset.Samples)
                patterns.Capacity = asset.Samples;

            if (asset.SamplingPatterns == SamplingPatterns.Uniform)
            {
                asset.Samples = Mathf.ClosestPowerOfTwo(asset.Samples);
                var size = Mathf.Sqrt(asset.Samples);
                patterns.Clear();
                for (int y = 0; y < Mathf.Sqrt(asset.Samples); y++)
                {
                    for (int x = 0; x < Mathf.Sqrt(asset.Samples); x++)
                    {
                        patterns.Add(new Vector2(x / size + .5f * size, y / size + .5f * size));
                    }
                }
            }
            else if (asset.SamplingPatterns == SamplingPatterns.Halton2_3)
            {
                patterns = Sampler.HaltonSequence2(2, 3).Skip(1).Take(asset.Samples).ToList();
            }


            renderingData.NextProjectionJitter = patterns[renderingData.FrameID % asset.Samples];

            HistoricalRT.Swap();
        }
        int previousColor;
        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("TAA Resolve");
            var (previousColor, nextColor) = GetHistoricalColorBuffer(renderingData);

            cmd.SetGlobalTexture("_PreviousFrameBuffer", previousColor);
            cmd.SetGlobalTexture("_CurrentFrameBuffer", renderingData.ColorTarget);
            cmd.SetGlobalFloat("_Alpha", asset.BlendAlpha);
            cmd.SetGlobalTexture("_VelocityBuffer", renderingData.VelocityBuffer);
            cmd.Blit(renderingData.ColorTarget, nextColor, material, 0);
            cmd.Blit(nextColor, renderingData.ColorTarget);

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
