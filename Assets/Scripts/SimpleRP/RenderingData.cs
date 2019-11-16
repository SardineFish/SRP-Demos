using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleRP
{
    public struct RenderingData
    {
        public Camera camera;
        public CullingResults cullResults;
        public Unity.Collections.NativeArray<VisibleLight> lights;
    }
}
