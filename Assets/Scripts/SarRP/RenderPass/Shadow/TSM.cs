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
        ShadowMapData TSMShadowMap(ScriptableRenderContext context, RenderingData renderingData, ShadowSettings settings, int lightIndex)
        {
            var camera = renderingData.camera;
            if (settings.Debug)
                camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var (view, projection) = GetShadowViewProjection(settings, renderingData, lightIndex);

            var cmd = CommandBufferPool.Get();
            cmd.Clear();
            var depthBuf = IdentifierPool.Get();

            cmd.GetTemporaryRT(depthBuf, settings.Resolution, settings.Resolution, 32, FilterMode.Point, RenderTextureFormat.Depth);

            RenderTargetBinding binding = new RenderTargetBinding();
            binding.depthRenderTarget = depthBuf;
            cmd.SetRenderTarget(depthBuf);
            cmd.ClearRenderTarget(true, true, Color.black);

            ShadowMapData shadowMapData = new ShadowMapData()
            {
                shadowMapIdentifier = depthBuf,
                bias = settings.Bias,
                ShadowType = ShadowAlgorithms.TSM,
                world2Light = GL.GetGPUProjectionMatrix(projection, true) * view,
            };

            var trapezoidalTransfrom = TSMTransform(camera, shadowMapData.world2Light, settings);
            shadowMapData.postTransform = trapezoidalTransfrom;


            cmd.SetViewProjectionMatrices(view, projection);
            cmd.SetGlobalDepthBias(settings.DepthBias, settings.NormalBias);
            cmd.SetGlobalMatrix("_ShadowPostTransform", trapezoidalTransfrom);
            cmd.SetGlobalFloat("_SlopeDepthBias", -settings.NormalBias);
            cmd.SetGlobalFloat("_DepthBias", -Mathf.Pow(2, settings.DepthBias));

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();


            DrawShadowCasters(context, renderingData, shadowMapData, PassTSM);

            cmd.SetViewProjectionMatrices(renderingData.camera.worldToCameraMatrix, renderingData.camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);

            return shadowMapData;
        }

        public static Matrix4x4 TSMTransform(Camera camera, Matrix4x4 lightViewProjection, ShadowSettings shadowSettings)
        {
            var frustumVerts = Utils.GetCameraFrustumVerticies(camera, camera.nearClipPlane, shadowSettings.MaxShadowDistance)
                .Select(p => ToPostPerspective(p, lightViewProjection));
            var convex = GetConvexHull(frustumVerts.Select(p => p.ToVector2()).ToArray());
            var nearCenter = ToPostPerspective(camera.transform.position + camera.transform.forward * camera.nearClipPlane, lightViewProjection).ToVector2();
            var farCenter = ToPostPerspective(camera.transform.position + camera.transform.forward * shadowSettings.MaxShadowDistance, lightViewProjection).ToVector2();
            var focusPoint = ToPostPerspective(camera.transform.position + camera.transform.forward * shadowSettings.FocusDistance, lightViewProjection).ToVector2();
            var centralLine = farCenter - nearCenter;
            var centralLineVector = centralLine.normalized;
            var topPoint = nearCenter + centralLineVector * convex.Min(p => Vector2.Dot(p - nearCenter, centralLineVector));
            var bottomVert = convex.MaxOf(p => Vector2.Dot(p - nearCenter, centralLineVector), p => p);
            var bottomPoint = topPoint + centralLineVector * Vector2.Dot(bottomVert - topPoint, centralLineVector);
            var tangent = (bottomVert - bottomPoint).normalized * Mathf.Sign(Utils.Cross2(bottomVert - topPoint, centralLineVector));
            var height = (bottomPoint - topPoint).magnitude;
            var focusLen = Vector2.Dot(focusPoint - topPoint, centralLineVector);

            var λ = height;
            var ξ = -0.6f;
            var δ = focusLen;
            var η = (λ * δ + λ * δ * ξ) / (λ - 2 * δ - λ * ξ);

            var origin = -η * centralLineVector + topPoint;
            var cosMaxHalfAngle = convex.Min(p => Vector2.Dot((p - origin).normalized, centralLineVector));
            var tan = Mathf.Sqrt(1 - cosMaxHalfAngle * cosMaxHalfAngle) / cosMaxHalfAngle;
            var minSinHalfAngle = convex.Min(p => Utils.Cross2((p - origin).normalized, centralLineVector));
            var maxSinHalfAngle = convex.Max(p => Utils.Cross2((p - origin).normalized, centralLineVector));
            var minTan = minSinHalfAngle / Mathf.Sqrt(1 - minSinHalfAngle * minSinHalfAngle);
            var maxTan = maxSinHalfAngle / Mathf.Sqrt(1 - maxSinHalfAngle * maxSinHalfAngle);

            var t0 = bottomPoint + (bottomPoint - origin).magnitude * minTan * tangent;
            var t1 = bottomPoint + (bottomPoint - origin).magnitude * maxTan * tangent;
            var t2 = topPoint + (topPoint - origin).magnitude * minTan * tangent;
            var t3 = topPoint + (topPoint - origin).magnitude * maxTan * tangent;

            // Transform trapezoid into unit cube
            // Following https://www.comp.nus.edu.sg/~tants/tsm/TSM_recipe.html
            var transform = Matrix4x4.identity;

            // #1
            Vector4 u = (t2 + t3) / 2;
            transform = Matrix4x4.Translate(-u) * transform;

            // #2
            u = (t2 - t3) / (t2 - t3).magnitude;
            transform = new Matrix4x4(
                new Vector4(u.x, u.y, 0, 0),
                new Vector4(u.y, -u.x, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            ) * transform;

            // #3
            u = transform * origin.ToVector4(0, 1);
            transform = Matrix4x4.Translate(-u) * transform;

            // #4
            u = (transform.MultiplyPoint((t2 + t3) / 2));
            transform = new Matrix4x4(
                new Vector4(1, -u.x / u.y, 0, 0),
                new Vector4(0, 1, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            ).transpose * transform;

            // #5
            u = transform.MultiplyPoint(t2);
            transform = Matrix4x4.Scale(new Vector3(1 / u.x, 1 / u.y, 1)) * transform;

            // #6
            transform = new Matrix4x4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 1, 0, 1),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 1, 0, 0)
            ) * transform;

            // #7
            u = transform * t0.ToVector4(0, 1);
            var v = transform * t2.ToVector4(0, 1);
            transform = Matrix4x4.Translate(new Vector3(0, -(u.y / u.w + v.y / v.w) / 2, 0)) * transform;

            // #8
            u = transform * t0.ToVector4(0, 1);
            transform = Matrix4x4.Scale(new Vector3(1, -u.w / u.y, 1)) * transform;


            var trapezoidal = new Vector2[] { t0, t1, t3, t2 };
            var t = trapezoidal.Select(p => transform.MultiplyPoint(p).ToVector2()).ToArray();
            //DrawPolygonOnLightPlane(t, lightViewProjection, Color.blue);
            if(shadowSettings.Debug)
            {
                Utils.DrawPolygonOnLightPlane(trapezoidal, lightViewProjection, Color.blue);
                Utils.DrawPolygonOnLightPlane(new Vector2[] { nearCenter, farCenter }, lightViewProjection, Color.red);
                Utils.DrawPolygonOnLightPlane(convex, lightViewProjection, Color.red);
            }
            return transform;
        }

        public static Vector2[] GetConvexHull(Vector2[] verts)
        {
            List<int> convex = new List<int>(verts.Length);
            convex.Add(0);
            convex.Add(1);

            // First pass, find boundary and change to starting with boundray
            // Second psss, find a convex hull
            var boundaryFound = false;
        NextPass:
            for (var i = 1; i < convex.Count; i++)
            {
                var current = verts[convex[i]];
                var currentDir = (verts[convex[i]] - verts[convex[i - 1]]).normalized;
                var maxDot = -2f;
                var next = -1;
                var hasLeftSide = false;
                for (var j = 0; j < verts.Length; j++)
                {
                    if (j == convex[i] || j == convex[i - 1])
                        continue;
                    var dir = (verts[j] - current).normalized;
                    if (Utils.Cross2(currentDir, dir) < 0)
                        hasLeftSide = true;
                    else if (Vector2.Dot(currentDir, dir) > maxDot)
                    {
                        next = j;
                        maxDot = Vector2.Dot(currentDir, dir);
                    }
                }

                // Reverse entry due to wrong direction.
                // Might happen in the first pass.
                if (next < 0)
                {
                    convex[0] = 1;
                    convex[1] = 0;
                    goto NextPass;
                }
                if (!boundaryFound && !hasLeftSide)
                {
                    var p0 = convex[i];
                    var p1 = next;
                    convex.Clear();
                    convex.Add(p0);
                    convex.Add(p1);
                    boundaryFound = true;
                    goto NextPass;
                }

                if (next == convex[0])
                    break;
                else if (convex.IndexOf(next) > 0)
                    throw new Exception("Should not happen.");
                else
                    convex.Add(next);
            }

            return convex.Select(idx => verts[idx]).ToArray();
        }

        public static Vector3 ToPostPerspective(Vector3 worldPos, Matrix4x4 viewProjection)
        {
            var pClip = viewProjection * worldPos.ToVector4(1);
            return (pClip / pClip.w).ToVector3();
        }
    }
}