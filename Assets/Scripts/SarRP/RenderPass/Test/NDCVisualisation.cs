using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="NDCVisualize", menuName ="SarRP/RenderPass/Test/NDCVisualize")]
    public class NDCVisualisation : RenderPassAsset
    {
        public override RenderPass CreateRenderPass()
        {
            return new NDCVisualiseRenderer(this);
        }
    }

    public class NDCVisualiseRenderer : RenderPassRenderer<NDCVisualisation>
    {
        Material mat;
        public NDCVisualiseRenderer(NDCVisualisation asset) : base(asset)
        {
        }
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!mat)
                mat = new Material(Shader.Find("SarRP/Test/NDCVisualisation"));
        }

        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var visualizeCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var camera = renderingData.camera;
            var cmd = CommandBufferPool.Get("RenderOpaque");
            using (new ProfilingSample(cmd, "RenderOpaque"))
            {
                // Start profilling
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetRenderTarget(renderingData.ColorTarget, renderingData.DepthTarget);
                var proj = GL.GetGPUProjectionMatrix(visualizeCamera.projectionMatrix, true);
                cmd.SetGlobalMatrix("_CameraViewProjection", GL.GetGPUProjectionMatrix(visualizeCamera.projectionMatrix, false) * visualizeCamera.worldToCameraMatrix);
                cmd.SetGlobalVector("_MainLightPosition", camera.transform.position.ToVector4(1));
                cmd.SetGlobalColor("_MainLightColor", Color.white);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();


                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                SortingSettings sortingSettings = new SortingSettings(camera);
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("ForwardLit"), sortingSettings)
                {
                    enableDynamicBatching = true,
                    overrideMaterial = mat,
                    overrideMaterialPassIndex = 0,
                };
                RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref stateBlock);

                

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }


}