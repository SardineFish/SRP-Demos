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
        private Dictionary<Camera, RenderPass> perCameraPass = new Dictionary<Camera, RenderPass>();
        public abstract RenderPass CreateRenderPass();
        public RenderPass GetRenderPass(Camera camera)
        {
            if (!perCameraPass.ContainsKey(camera))
                perCameraPass[camera] = CreateRenderPass();
            return perCameraPass[camera];
        }
    }
}
