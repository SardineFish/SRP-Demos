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
            VelocityBuffer = 0,
        }
        Material material;
        int velocityBuffer;
        Matrix4x4 previousGPUVPMatrix;
        public VelocityPassRenderer(VelocityPass asset) : base(asset)
        {
        }

        protected override void Init()
        {
            material = new Material(Shader.Find("SarRP/VelocityBuffer"));
            velocityBuffer = Shader.PropertyToID("_VelocityBuffer");
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            cmd.GetTemporaryRT(velocityBuffer, renderingData.ResolutionX, renderingData.ResolutionY, 0, FilterMode.Point, RenderTextureFormat.RGFloat);
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

            cmd.SetGlobalMatrix("_PreviousGPUViewProjection", previousGPUVPMatrix);
            cmd.SetGlobalTexture("_CameraDepthTex", renderingData.DepthTarget);

            cmd.SetCameraParams(renderingData.camera, false);
            cmd.BlitFullScreen(BuiltinRenderTextureType.None, velocityBuffer, material, (int)ShaderPass.VelocityBuffer);

            previousGPUVPMatrix = SaveGPUViewProjection(renderingData);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        Matrix4x4 SaveGPUViewProjection(RenderingData renderingData)
            => GL.GetGPUProjectionMatrix(renderingData.JitteredProjectionMatrix, false) * renderingData.ViewMatrix;

    }

}
