using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SarRP.Renderer;

namespace SarRP
{
    public class SardineRenderPipeline : RenderPipeline
    {
        SardineRenderPipelineAsset settings { get; set; }
        List<RenderPass> RenderPassQueue = new List<RenderPass>();
        public SardineRenderPipeline(SardineRenderPipelineAsset asset)
        {
            settings = asset;

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
            var p = Matrix4x4.Perspective(30, 16.0f / 9, .3f, 1000);
            var v = new Vector4(.5f, .5f, 10, 1);
            if (camera.name == "Main Camera")
                v = v;

            camera.TryGetCullingParameters(out var cullingParameters);
            
            var cmd = CommandBufferPool.Get(camera.name);

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

            InitRenderQueue();

            context.DrawSkybox(camera);

            foreach(var pass in RenderPassQueue)
            {
                pass.Setup(context, ref renderingData);
                pass.Render(context, ref renderingData);
            }

            if(camera.cameraType == CameraType.SceneView)
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            context.Submit();
        }

        void InitRenderQueue()
        {
            RenderPassQueue.Clear();
            foreach (var renderPassAsset in settings.RenderPasses)
            {
                if (renderPassAsset)
                    RenderPassQueue.Add(renderPassAsset.CreateRenderPass());
            }
        }
    }

}