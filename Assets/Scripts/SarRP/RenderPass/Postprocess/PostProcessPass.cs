using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SarRP.Postprocess;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="Postprocess", menuName = "SarRP/RenderPass/Postprocess")]
    public class PostprocessPass : RenderPassAsset
    {
        [HideInInspector]
        [SerializeField]
        private List<PostprocessAsset> m_PostProcessSettings = new List<PostprocessAsset>();

        public List<PostprocessAsset> PostProcessSettings => m_PostProcessSettings;
        public override RenderPass CreateRenderPass()
        {
            return new PostprocessRenderer(this);
        }

        public class PostprocessRenderer : RenderPassRenderer<PostprocessPass>
        {
            Material helperMat;
            Mesh fullScreenMesh;
            public PostprocessRenderer(PostprocessPass asset) : base(asset)
            {
                helperMat = new Material(Shader.Find("SarRP/Postprocess/Helper"));
            }
            public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!fullScreenMesh)
                    fullScreenMesh = Utility.GenerateFullScreenQuad();
            }
            public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                int screenImage = Shader.PropertyToID("_ScreenImage");
                int src = screenImage;
                int dst = Shader.PropertyToID("_PostprocessRT_0");
                CommandBuffer cmd = CommandBufferPool.Get("Postprocess Pass");

                cmd.BeginSample("Postprocess Pass");

                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetGlobalMatrix("_ViewProjectionInverseMatrix", Utility.ProjectionToWorldMatrix(renderingData.camera));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();


                for (var i = 0; i < asset.PostProcessSettings.Count; i++)
                {
                    if(i == 0)
                    {
                        int rawImage = Shader.PropertyToID("_RawScreenImage");
                        cmd.GetTemporaryRT(rawImage, renderingData.camera.pixelWidth, renderingData.camera.pixelHeight, 0);
                        cmd.GetTemporaryRT(screenImage, renderingData.camera.pixelWidth, renderingData.camera.pixelHeight, 0);
                        cmd.Blit(BuiltinRenderTextureType.CameraTarget, rawImage);
                        cmd.Blit(rawImage, screenImage, helperMat, 0);
                        cmd.ReleaseTemporaryRT(rawImage);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                        src = screenImage;
                    }

                    var postprocess = asset.PostProcessSettings[i];

                    PostprocessContext postprocessContext = new PostprocessContext(context, renderingData)
                    {
                        source = new RenderTargetIdentifier(src),
                    };
                    PostprocessRenderSettings settings = new PostprocessRenderSettings()
                    {
                        width = renderingData.camera.pixelWidth,
                        height = renderingData.camera.pixelHeight,
                        filterMode = FilterMode.Bilinear,
                        format = RenderTextureFormat.ARGB32
                    };

                    var renderer = postprocess.GetRenderer();
                    renderer.Setup(postprocessContext, ref settings);


                    cmd.GetTemporaryRT(dst, settings.width, settings.height, settings.depth, settings.filterMode, settings.format);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    postprocessContext.destination = new RenderTargetIdentifier(dst);

                    renderer.Render(postprocessContext);

                    // Do not release screen image
                    if (i > 0)
                    {
                        cmd.ReleaseTemporaryRT(src);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                    }

                    src = dst;
                    dst = Shader.PropertyToID($"_PostprocessRT_{i + 1}");
                }

                cmd.Blit(src, BuiltinRenderTextureType.CameraTarget);
                cmd.ReleaseTemporaryRT(screenImage);
                cmd.ReleaseTemporaryRT(src);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.EndSample("Postprocess Pass");
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.SetupCameraProperties(renderingData.camera);

                CommandBufferPool.Release(cmd);
            }
        }
    }

    


}
