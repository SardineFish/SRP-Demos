using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace SarRP.Renderer
{
    public abstract class RenderPassAsset : ScriptableObject
    {
        public abstract RenderPass CreateRenderPass();
    }
}
