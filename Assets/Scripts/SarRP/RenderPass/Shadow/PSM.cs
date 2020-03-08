using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    public partial class ShadowPassRenderer
    {
        ShadowMapData PSMShadowMap(ScriptableRenderContext context, RenderingData renderingData, ShadowSettings settings, int lightIndex)
        {
            var (view, projection, inverseZ) = PSMProjection(lightIndex, renderingData);
            //Debug.Log(inverseZ);
            Vector4 p = new Vector4(-0.46017f, 0.16764f, 0.01015f, 1.00f);
            var p1 = projection * view * p;
            var p2 = GL.GetGPUProjectionMatrix(projection, false) * Matrix4x4.Scale(new Vector3(1, 1, -1)) * view * p;

            ShadowMapData shadowMapData = new ShadowMapData()
            {
                shadowMapIdentifier = IdentifierPool.Get(),
                world2Light = GL.GetGPUProjectionMatrix(projection, true) * Matrix4x4.Scale(new Vector3(1, 1, -1)) * view,
                bias = settings.Bias,
                ShadowType = ShadowAlgorithms.PSM,
                ShadowParameters = new Vector4(inverseZ ? 1 : 0, 0, 0),
            };

            var cmd = CommandBufferPool.Get();
            cmd.GetTemporaryRT(shadowMapData.shadowMapIdentifier, settings.Resolution, settings.Resolution, 32, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.SetRenderTarget(shadowMapData.shadowMapIdentifier);
            cmd.SetGlobalVector("_ShadowParameters", shadowMapData.ShadowParameters);
            cmd.SetGlobalDepthBias(1, 1);
            //cmd.SetViewProjectionMatrices(renderingData.camera.worldToCameraMatrix, renderingData.camera.projectionMatrix);
            cmd.ClearRenderTarget(true, true, Color.black);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            DrawShadowCasters(context, renderingData, shadowMapData, PassPSM);

            CommandBufferPool.Release(cmd);

            return shadowMapData;
        }



        public static (Matrix4x4 view, Matrix4x4 projection, bool inverseZ) PSMProjection(int lightIndex, RenderingData renderingData)
        {
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var cameraView = camera.worldToCameraMatrix;
            var cameraProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);

            var light = renderingData.cullResults.visibleLights[lightIndex].light;
            var p = light.transform.forward.ToVector4(1);
            var l = light.transform.forward.ToVector4(0);
            var pView = cameraView * p;
            var lView = cameraView * l;
            var lClip = cameraProjection * lView.ToVector3().normalized.ToVector4(0);
            var pClip = cameraProjection * lView.ToVector3().normalized.ToVector4(1);
            var lightNDC = lClip / pClip.w;

            var lightView = Matrix4x4.LookAt(lightNDC.ToVector3(), Vector3.forward * .5f, Vector3.up).inverse;

            var ndcBounds = new Bounds(Vector3.forward * 0.5f, new Vector3(2, 2, 1));
            var ndcBoundVerts = new Vector3[8];
            for (int x = -1, i = 0; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        ndcBoundVerts[i++] = ndcBounds.center + Vector3.Scale(ndcBounds.extents, new Vector3(x, y, z));

            var frustum = BestfitFrustum(false, lightView, ndcBoundVerts);

            var inverseZ = Vector3.Dot(lView, -lightNDC.ToVector3()) < 0;

            Utils.DrawFrustum(frustum, false, lightView.inverse);
            Utils.DrawBound(ndcBounds, Color.magenta);
            Debug.DrawLine(Vector3.forward * 0.5f, lightNDC.ToVector3());

            return (lightView, Matrix4x4.Frustum(frustum), inverseZ);
        }

    }
}