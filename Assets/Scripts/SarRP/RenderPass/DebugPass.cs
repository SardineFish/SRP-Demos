using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="DebugPass", menuName ="SarRP/RenderPass/Debug")]
    public class DebugPass : RenderPassAsset
    {
        public Material material;
        public string name = "Debug Pass";

        public override RenderPass CreateRenderPass()
        {
            return new DebugPassRenderer(this);
        }
    }
    public class DebugPassRenderer : RenderPassRenderer<DebugPass>
    {
        Mesh fullscreenMesh;
        public DebugPassRenderer(DebugPass asset) : base(asset)
        {
            fullscreenMesh = Utility.GenerateFullScreenQuad();
        }

        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(asset.name);
            cmd.DrawMesh(fullscreenMesh, Utility.ProjectionToWorldMatrix(renderingData.camera), asset.material);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

}
