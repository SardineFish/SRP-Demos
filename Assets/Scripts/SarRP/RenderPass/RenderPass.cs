using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    public abstract class RenderPassRenderer<TRenderPassAsset> : RenderPass
        where TRenderPassAsset : RenderPassAsset
    {
        protected TRenderPassAsset asset { get; private set; }
        public RenderPassRenderer(TRenderPassAsset asset)
        {
            this.asset = asset;
        }
    }

    public abstract class RenderPass
    {
        public virtual void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }

        public virtual void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }

        public virtual void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }
    }
}
