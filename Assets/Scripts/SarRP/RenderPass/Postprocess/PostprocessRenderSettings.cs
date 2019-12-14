using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Postprocess
{
    public struct PostprocessRenderSettings
    {
        public int width;
        public int height;
        public int depth;
        public FilterMode filterMode;
        public RenderTextureFormat format;
    }
}
