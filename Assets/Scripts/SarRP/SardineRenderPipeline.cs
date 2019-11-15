using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    public class SardineRenderPipeline : RenderPipeline
    {
        SardineRenderPipelineAsset settings { get; set; }
        SardineRenderer Renderer { get; set; }
        public SardineRenderPipeline(SardineRenderPipelineAsset asset)
        {
            settings = asset;
            Renderer = new SardineRenderer();

            Shader.globalRenderPipeline = "SardineRenderPipeline";
        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            BeginFrameRendering(context, cameras);
            foreach (var camera in cameras)
            {
                BeginCameraRendering(context, camera);

                RenderCamera(context, camera);

                EndCameraRendering(context, camera);
            }
            EndFrameRendering(context, cameras);
        }
        protected virtual void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            camera.TryGetCullingParameters(out var cullingParameters);

            var renderer = this.Renderer;
            var cmd = CommandBufferPool.Get(camera.name);

            renderer.Reset();
            renderer.SetupCullingParameters(ref cullingParameters, settings);
            cmd.Clear();

            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);

            var cullResults = context.Cull(ref cullingParameters);

            var renderingData = new RenderingData()
            {
                camera = camera,
                cullResults = cullResults
            };
            context.SetupCameraProperties(camera, false);

            context.DrawSkybox(camera);
            renderer.RenderOpaque(context, renderingData);
            if(camera.cameraType == CameraType.SceneView)
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            context.Submit();
        }
    }

}