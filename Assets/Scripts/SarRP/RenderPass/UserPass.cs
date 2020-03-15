using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    public abstract class UserPass : MonoBehaviour
    {
        public bool Global = false;
        public virtual void Setup(ScriptableRenderContext context, ref RenderingData renderingData) { }
        public virtual void Render(ScriptableRenderContext context, ref RenderingData renderingData) { }
        public virtual void Cleanup(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }
}
