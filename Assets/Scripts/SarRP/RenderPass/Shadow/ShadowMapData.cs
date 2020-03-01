using UnityEngine;

namespace SarRP.Renderer
{
    public struct ShadowMapData
    {
        public int shadowMapIdentifier;
        public Matrix4x4 world2Light;
        public float bias;
    }

}
