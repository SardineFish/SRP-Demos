using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleRP
{
    public class SimpleRenderPipeline : RenderPipeline
    {
        SimpleRenderPipelineAsset settings { get; set; }
        public SimpleRenderPipeline(SimpleRenderPipelineAsset asset)
        {
            settings = asset;

            Shader.globalRenderPipeline = "SimpleRenderPipeline";
        }
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {

                RenderCamera(context, camera);

            }
        }
        protected virtual void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            camera.TryGetCullingParameters(out var cullingParameters);


            var cullResults = context.Cull(ref cullingParameters);
            var renderingData = new RenderingData()
            {
                camera = camera,
                cullResults = cullResults
            };

            context.SetupCameraProperties(camera, false);

            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);

            context.DrawSkybox(camera);

            SimpleForwardRenderer.RenderOpaque(context, renderingData);

            if(camera.cameraType == CameraType.SceneView)
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }

            context.Submit();
        }
    }

}