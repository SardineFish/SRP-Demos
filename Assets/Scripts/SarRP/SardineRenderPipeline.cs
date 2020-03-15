using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SarRP.Renderer;
using System.Linq;

namespace SarRP
{
    public class SardineRenderPipeline : RenderPipeline
    {
        SardineRenderPipelineAsset settings { get; set; }
        List<RenderPass> RenderPassQueue = new List<RenderPass>();
        List<UserPass> globalUserPasses = new List<UserPass>();
        int ColorTarget;
        int DepthTarget;
        bool rtCreated = false;
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

            camera.TryGetCullingParameters(out var cullingParameters);
            
            var cmd = CommandBufferPool.Get(camera.name);

            cmd.Clear();

            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);

            var cullResults = context.Cull(ref cullingParameters);

            var renderingData = new RenderingData()
            {
                camera = camera,
                cullResults = cullResults,
                ColorTarget = BuiltinRenderTextureType.CameraTarget,
                DepthTarget = BuiltinRenderTextureType.CameraTarget,
                shadowMapData = new Dictionary<Light, ShadowMapData>()
            };

            this.Setup(context, ref renderingData);
            context.SetupCameraProperties(camera, false);

            InitRenderQueue();
            SetupLight(ref renderingData);

            /*RenderTargetBinding binding = new RenderTargetBinding();
            binding.colorRenderTargets = new RenderTargetIdentifier[] { ColorTarget };
            binding.colorLoadActions = new RenderBufferLoadAction[] { RenderBufferLoadAction.Clear };
            binding.depthRenderTarget = DepthTarget;
            binding.depthLoadAction = RenderBufferLoadAction.Clear;
            binding.colorStoreActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store };
            binding.depthStoreAction = RenderBufferStoreAction.Store;*/
            cmd.SetRenderTarget(ColorTarget, DepthTarget);
            cmd.ClearRenderTarget(true, true, Color.black, 1);
            cmd.SetRenderTarget(DepthTarget, DepthTarget);
            cmd.ClearRenderTarget(true, true, Color.black, 1);
            cmd.SetRenderTarget(ColorTarget, DepthTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.DrawSkybox(camera);

            foreach(var pass in RenderPassQueue)
            {
                pass.Setup(context, ref renderingData);
                pass.Render(context, ref renderingData);
            }

            // Draw global user passes
            foreach(var pass in globalUserPasses)
            {
                pass.Setup(context, ref renderingData);
                pass.Render(context, ref renderingData);
            }

            // Draw user passes
            var userPasses = camera.GetComponents<UserPass>();
            foreach(var pass in userPasses)
            {
                if (pass.Global)
                    continue;
                pass.Setup(context, ref renderingData);
                pass.Render(context, ref renderingData);
            }

            cmd.Blit(ColorTarget, BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if(camera.cameraType == CameraType.SceneView)
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }

            foreach (var pass in RenderPassQueue)
                pass.Cleanup(context, ref renderingData);
            foreach (var pass in globalUserPasses)
                pass.Cleanup(context, ref renderingData);
            foreach (var pass in userPasses)
                pass.Cleanup(context, ref renderingData);


            this.Cleanup(context, ref renderingData);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            context.Submit();
        }

        void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(!rtCreated)
            {
                var camera = renderingData.camera;
                this.ColorTarget = Shader.PropertyToID("_ColorTarget");
                this.DepthTarget = Shader.PropertyToID("_DepthTarget");
                var cmd = CommandBufferPool.Get();
                cmd.GetTemporaryRT(ColorTarget, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat);
                cmd.GetTemporaryRT(DepthTarget, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
            renderingData.ColorTarget = ColorTarget;
            renderingData.DepthTarget = DepthTarget;

        }

        void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.ReleaseTemporaryRT(ColorTarget);
            cmd.ReleaseTemporaryRT(DepthTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        void SetupLight(ref RenderingData renderingData)
        {
            renderingData.lights = renderingData.cullResults.visibleLights;
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