using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    public class RenderTarget : ResourcesPool<RenderTarget>, IDisposable
    {
        #region Static Members

        static RenderTarget()
        {
            initator = NewRenderTarget;
        }
        private static RenderTarget NewRenderTarget()
            => new RenderTarget();

        public static RenderTarget GetTemporary(
            CommandBuffer cmd,
            int width,
            int height,
            int depthBuffer = 0,
            FilterMode filterMode = FilterMode.Bilinear,
            RenderTextureFormat textureFormat = RenderTextureFormat.Default)
        {
            var id = IdentifierPool.Get();
            cmd.GetTemporaryRT(id, width, height, depthBuffer, filterMode, textureFormat);
            return FromExist(id, width, height, depthBuffer, filterMode, textureFormat);
        }

        public static RenderTarget GetTemporary(
            CommandBuffer cmd,
            string name,
            int width,
            int height,
            int depthBuffer = 0,
            FilterMode filterMode = FilterMode.Bilinear,
            RenderTextureFormat textureFormat = RenderTextureFormat.Default)
        {
            var id = Shader.PropertyToID(name);
            cmd.GetTemporaryRT(id, width, height, depthBuffer, filterMode, textureFormat);
            return FromExist(id, width, height, depthBuffer, filterMode, textureFormat);
        }

        public static RenderTarget FromExist(
            int id,
            int width,
            int height,
            int depthBuffer = 0,
            FilterMode filterMode = FilterMode.Bilinear,
            RenderTextureFormat textureFormat = RenderTextureFormat.Default)
        {
            var rt = Get();
            rt.id = id;
            rt.Size = new Vector2Int(width, height);
            rt.DepthBuffer = depthBuffer;
            rt.FilterMode = filterMode;
            rt.Format = textureFormat;
            return rt;
        }


        public static RenderTarget GetCameraTarget(Camera camera)
        {
            var rt = Get();
            rt.id = -1;
            rt.Size = new Vector2Int(camera.pixelWidth, camera.pixelHeight);
            rt.DepthBuffer = 32;
            rt.FilterMode = FilterMode.Point;
            rt.Format = RenderTextureFormat.Default;
            return rt;
        }

        public static RenderTarget Empty()
        {
            var rt = Get();
            rt.Size = new Vector2Int(-1, -1);
            rt.DepthBuffer = 0;
            rt.id = 0;
            return rt;
        }

        #endregion

        public int id;
        public Vector2Int Size { get; private set; }
        public int DepthBuffer { get; private set; }
        public FilterMode FilterMode { get; private set; }
        public RenderTextureFormat Format { get; private set; }
        public RenderTargetIdentifier Identifier => id == -1 ? BuiltinRenderTextureType.CameraTarget : new RenderTargetIdentifier(id);
        public bool IsValid => id != 0;

        private RenderTarget()
        {
        }

        public void Release(CommandBuffer cmd)
        {
            if (id >= 0)
            {
                cmd.ReleaseTemporaryRT(id);
                IdentifierPool.Release(id);
                Dispose();
            }
        }

        public RenderTarget Transfer()
        {
            var rt = FromExist(id, Size.x, Size.y, DepthBuffer, FilterMode, Format);
            this.id = 0;
            return rt;
        }

        public void Dispose()
        {
            id = 0;
            RenderTarget.Release(this);
        }

        public static implicit operator RenderTargetIdentifier(RenderTarget rt) => rt.Identifier;
    }
}
