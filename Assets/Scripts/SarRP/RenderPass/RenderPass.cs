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
        [NonSerialized]
        private bool _reload = true;
        protected virtual void Init()
        {

        }
        internal virtual void InternalSetup()
        {
            if (_reload)
            {
                Init();
                _reload = false;
            }
        }
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
