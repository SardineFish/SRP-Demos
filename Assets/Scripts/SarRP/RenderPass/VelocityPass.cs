using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="VelocityPass", menuName ="SarRP/RenderPass/Velocity")]
    public class VelocityPass : RenderPassAsset
    {
        public override RenderPass CreateRenderPass()
        {
            return new VelocityPassRenderer(this);
        }
    }

    public class VelocityPassRenderer : RenderPassRenderer<VelocityPass>
    {
        enum ShaderPass : int
        {
            // OpaqueVelocity = 0,
            StaticVelocity = 0,
        }
        static readonly ShaderTagId VelocityPassName = new ShaderTagId("MotionVectors");
        const string ShaderName = "SarRP/VelocityBuffer";
        int velocityBuffer;
        Matrix4x4 previousGPUVPMatrix;
        Vector2 previousJitterOffset;
        public VelocityPassRenderer(VelocityPass asset) : base(asset)
        {
        }

        protected override void Init()
        {
            velocityBuffer = Shader.PropertyToID("_VelocityBuffer");
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            renderingData.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            cmd.GetTemporaryRT(velocityBuffer, renderingData.ResolutionX, renderingData.ResolutionY, 32, FilterMode.Point, RenderTextureFormat.RGFloat);
            if (renderingData.FrameID == 0)
                previousGPUVPMatrix = SaveGPUViewProjection(renderingData);
            renderingData.VelocityBuffer = velocityBuffer;

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(velocityBuffer);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.camera;
            var cmd = CommandBufferPool.Get("Velocity Pass");

            using (new ProfilingSample(cmd,"Velocity Pass"))
            {
                cmd.SetGlobalMatrix("_PreviousGPUViewProjection", previousGPUVPMatrix);
                cmd.SetGlobalTexture("_CameraDepthTex", renderingData.DepthTarget);
                cmd.SetGlobalVector("_PreviousJitterOffset", previousJitterOffset);
                var jitterOffset = renderingData.ProjectionJitter - new Vector2(.5f, .5f);
                cmd.SetGlobalVector("_CurrentJutterOffset", jitterOffset);

                cmd.SetCameraParams(renderingData.camera, false);
                cmd.SetViewProjectionMatrices(renderingData.ViewMatrix, renderingData.JitteredProjectionMatrix);
                cmd.SetRenderTarget(velocityBuffer, velocityBuffer);
                var jitterVelocity = Vector2.Scale(jitterOffset - previousJitterOffset, new Vector2(1f / renderingData.ResolutionX, 1f / renderingData.ResolutionY));

                cmd.ClearRenderTarget(true, true, Color.black);
                //cmd.BlitFullScreen(BuiltinRenderTextureType.None, velocityBuffer, ShaderPool.Get("SarRP/VelocityBuffer"), (int)ShaderPass.VelocityBuffer);
                cmd.BlitFullScreen(BuiltinRenderTextureType.None, velocityBuffer, ShaderPool.Get(ShaderName), (int)ShaderPass.StaticVelocity);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();


                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                SortingSettings sortingSettings = new SortingSettings(renderingData.camera)
                {
                    criteria = SortingCriteria.CommonOpaque
                };
                var mat = ShaderPool.Get("SarRP/ForwardDefault");
                mat.SetShaderPassEnabled("MotionVectors", false);
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("MotionVectors"), sortingSettings)
                {
                    // overrideMaterial = ShaderPool.Get(ShaderName),
                    // overrideMaterialPassIndex = (int)ShaderPass.OpaqueVelocity,
                    enableDynamicBatching = true,
                    enableInstancing = true,
                    perObjectData = PerObjectData.MotionVectors,
                };
                drawingSettings.SetShaderPassName(0, new ShaderTagId("MotionVectors"));
                RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);
            }

            previousGPUVPMatrix = SaveGPUViewProjection(renderingData);
            previousJitterOffset = renderingData.ProjectionJitter - new Vector2(.5f, .5f);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        Matrix4x4 SaveGPUViewProjection(RenderingData renderingData)
            => GL.GetGPUProjectionMatrix(renderingData.JitteredProjectionMatrix, false) * renderingData.ViewMatrix;

    }

}
