using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Postprocess
{
    public struct PostprocessContext
    {
        public RenderTargetIdentifier source;
        public RenderTargetIdentifier destination;
        public RenderingData renderingData;
        private ScriptableRenderContext context;
        public void ExecuteCommand(CommandBuffer command)
        {
            context.ExecuteCommandBuffer(command);
        }
        public PostprocessContext(ScriptableRenderContext ctx, RenderingData data)
        {
            source = BuiltinRenderTextureType.None;
            destination = BuiltinRenderTextureType.None;
            renderingData = data;
            context = ctx;
        }
    }

}
