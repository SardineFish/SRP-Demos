using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Postprocess
{
    public static class Helper
    {
        static Mesh fullScreenMesh;
        public static Mesh FullScreenMesh
        {
            get
            {
                if (!fullScreenMesh)
                    fullScreenMesh = Utility.GenerateFullScreenQuad();
                return fullScreenMesh;
            }
        }

        public static void BlitFullScreen(this CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dst, Material mat, int pass)
        {
            cmd.SetGlobalTexture("_MainTex", src);
            cmd.SetRenderTarget(dst);
            cmd.DrawMesh(FullScreenMesh, Matrix4x4.identity, mat, pass);
        }

    }
}
