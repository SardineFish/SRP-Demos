using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    public struct RenderingData
    {
        public Camera camera;
        public CullingResults cullResults;
        public Unity.Collections.NativeArray<VisibleLight> lights;
        public RenderTargetIdentifier ColorTarget;
        public RenderTargetIdentifier DepthTarget;
        public RenderTextureFormat ColorBufferFormat;
        public Dictionary<Light, Renderer.ShadowMapData> shadowMapData;
        public RenderTargetIdentifier DefaultShadowMap;
        public int FrameID;
        public bool DiscardFrameBuffer;
        public Vector2 ProjectionJitter;
        public Vector2 NextProjectionJitter;
        public Matrix4x4 JitteredProjectionMatrix;
        public Matrix4x4 ProjectionMatrix;
        public Matrix4x4 ViewMatrix;
    }
}
