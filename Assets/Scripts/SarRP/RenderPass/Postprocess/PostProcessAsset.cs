using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SarRP.Postprocess
{
    public abstract class PostprocessAsset : ScriptableObject
    {
        private PostprocessRenderer renderer;
        public abstract PostprocessRenderer CreateRenderer();
        public PostprocessRenderer GetRenderer()
        {
            if (renderer is null)
                renderer = CreateRenderer();
            return renderer;
        }

    }
    public class PostprocessRenderer<T> : PostprocessRenderer where T : PostprocessAsset
    {
        protected T asset { get; private set; }
        public PostprocessRenderer(T asset)
        {
            this.asset = asset;
        }
    }

    public class PostprocessRenderer
    {
        public virtual void Setup(PostprocessContext context, ref PostprocessRenderSettings settings)
        {

        }
        public virtual void Render(PostprocessContext context)
        {

        }
    }
}
