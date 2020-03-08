using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SarRP.Renderer
{
    public partial class ShadowPassRenderer
    {
        public static class Utils
        {
            public static (Matrix4x4 view, Matrix4x4 projection) GetShadowViewProjection(ShadowSettings settings, RenderingData renderingData, int lightIndex)
            {
                var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
                var cameraToWorld = camera.transform.localToWorldMatrix;
                var p0 = cameraToWorld.MultiplyPoint(new Vector3(0, 0, 0));
                var h = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
                var w = h * camera.aspect;
                var p1 = cameraToWorld.MultiplyPoint(new Vector3(-w, -h, 1) * settings.MaxShadowDistance);
                var p2 = cameraToWorld.MultiplyPoint(new Vector3(w, -h, 1) * settings.MaxShadowDistance);
                var p3 = cameraToWorld.MultiplyPoint(new Vector3(w, h, 1) * settings.MaxShadowDistance);
                var p4 = cameraToWorld.MultiplyPoint(new Vector3(-w, h, 1) * settings.MaxShadowDistance);

                var view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * settings.transform.worldToLocalMatrix;
                renderingData.cullResults.GetShadowCasterBounds(lightIndex, out var bounds);

                var casterBoundVerts = new Vector3[8];
                for (int x = -1, i = 0; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            casterBoundVerts[i++] = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));


                var debug = camera.name == "Main Camera" && true;

                if (settings.light.type == LightType.Point)
                {
                    var rotation = Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.forward, settings.transform.worldToLocalMatrix.MultiplyPoint(bounds.center).normalized));
                    var rotatedView = rotation.inverse * settings.light.transform.worldToLocalMatrix;
                    var frustumCamera = BestfitFrustum(false, rotatedView, p0, p1, p2, p3, p4);
                    var frustumCaster = BestfitFrustum(false, rotatedView, casterBoundVerts);

                    if(debug)
                    {
                        DrawBound(bounds, Color.cyan);
                        DrawFrustum(frustumCaster, false, rotatedView.inverse);
                    }

                    return (Matrix4x4.Scale(new Vector3 (1, 1, -1)) * rotatedView, Matrix4x4.Frustum(frustumCaster));

                }
                else
                {
                    var frustumCamera = BestfitFrustum(true, settings.light.transform.worldToLocalMatrix, p0, p1, p2, p3, p4);
                    var frustumCaster = BestfitFrustum(true, settings.light.transform.worldToLocalMatrix, casterBoundVerts);

                    var left = Mathf.Max(frustumCaster.left, frustumCamera.left);
                    var right = Mathf.Min(frustumCaster.right, frustumCamera.right);
                    var bottom = Mathf.Max(frustumCaster.bottom, frustumCamera.bottom);
                    var top = Mathf.Min(frustumCaster.top, frustumCamera.top);
                    var zNear = frustumCaster.zNear;
                    var zFar = Mathf.Min(frustumCaster.zFar, frustumCamera.zFar);

                    if (camera.name == "Main Camera")
                    {
                        var bound = new Bounds();
                        bound.min = new Vector3(left, bottom, zNear);
                        bound.max = new Vector3(right, top, zFar);
                        DrawFrustum(new FrustumPlanes()
                        {
                            left = Mathf.Max(frustumCaster.left, frustumCamera.left),
                            right = Mathf.Min(frustumCaster.right, frustumCamera.right),
                            bottom = Mathf.Max(frustumCaster.bottom, frustumCamera.bottom),
                            top = Mathf.Min(frustumCaster.top, frustumCamera.top),
                            zNear = frustumCaster.zNear,
                            zFar = Mathf.Min(frustumCaster.zFar, frustumCamera.zFar),
                        }, true, settings.transform.localToWorldMatrix);
                        //DrawBound(bounds, Color.cyan);
                        Debug.DrawRay(settings.transform.localToWorldMatrix.MultiplyPoint(new Vector3(bound.center.x, bound.center.y, zNear)), settings.transform.forward * 5, Color.green);
                    }

                    return (view, Matrix4x4.Ortho(left, right, bottom, top, zNear, zFar));

                }
            }

            public static FrustumPlanes BestfitFrustum(bool orthographic, Matrix4x4 transform, params Vector3[] verts)
            {
                FrustumPlanes frustum = new FrustumPlanes()
                {
                    left = float.MaxValue,
                    right = float.MinValue,
                    bottom = float.MaxValue,
                    top = float.MinValue,
                    zNear = float.MaxValue,
                    zFar = float.MinValue,
                };
                if (orthographic)
                {
                    for (var i = 0; i < verts.Length; i++)
                    {
                        var transformedP = transform.MultiplyPoint(verts[i]);
                        frustum.left = Mathf.Min(frustum.left, transformedP.x);
                        frustum.bottom = Mathf.Min(frustum.bottom, transformedP.y);
                        frustum.zNear = Mathf.Min(frustum.zNear, transformedP.z);
                        frustum.right = Mathf.Max(frustum.right, transformedP.x);
                        frustum.top = Mathf.Max(frustum.top, transformedP.y);
                        frustum.zFar = Mathf.Max(frustum.zFar, transformedP.z);
                    }
                }
                else
                {
                    for (var i = 0; i < verts.Length; i++)
                    {
                        var transformedP = transform.MultiplyPoint(verts[i]);
                        frustum.left = Mathf.Min(frustum.left, transformedP.x / transformedP.z);
                        frustum.right = Mathf.Max(frustum.right, transformedP.x / transformedP.z);
                        frustum.bottom = Mathf.Min(frustum.bottom, transformedP.y / transformedP.z);
                        frustum.top = Mathf.Max(frustum.top, transformedP.y / transformedP.z);
                        frustum.zNear = Mathf.Min(frustum.zNear, transformedP.z);
                        frustum.zFar = Mathf.Max(frustum.zFar, transformedP.z);
                    }
                    frustum.left *= frustum.zNear;
                    frustum.right *= frustum.zNear;
                    frustum.bottom *= frustum.zNear;
                    frustum.top *= frustum.zNear;
                }
                return frustum;
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

                DrawFrustum(frustum, false, lightView.inverse);
                DrawBound(ndcBounds, Color.magenta);
                Debug.DrawLine(Vector3.forward * 0.5f, lightNDC.ToVector3());

                return (lightView, Matrix4x4.Frustum(frustum), inverseZ);
            }

            public static Matrix4x4 TSMTransform(Camera camera, Matrix4x4 lightViewProjection, ShadowSettings shadowSettings)
            {
                var frustumVerts = GetCameraFrustumVerticies(camera, camera.nearClipPlane, shadowSettings.MaxShadowDistance)
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
                var tangent = (bottomVert - bottomPoint).normalized * Mathf.Sign(Cross2(bottomVert - topPoint, centralLineVector));
                var height = (bottomPoint - topPoint).magnitude;
                var focusLen = Vector2.Dot(focusPoint - topPoint, centralLineVector);
                
                var λ = height;
                var ξ = -0.6f;
                var δ = focusLen;
                var η = (λ * δ + λ * δ * ξ) / (λ - 2 * δ - λ * ξ);

                var origin= -η * centralLineVector + topPoint;
                var cosMaxHalfAngle = convex.Min(p => Vector2.Dot((p - origin).normalized, centralLineVector));
                var tan = Mathf.Sqrt(1 - cosMaxHalfAngle * cosMaxHalfAngle) / cosMaxHalfAngle;
                var minSinHalfAngle = convex.Min(p => Cross2((p - origin).normalized, centralLineVector));
                var maxSinHalfAngle = convex.Max(p => Cross2((p - origin).normalized, centralLineVector));
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
                    new Vector4(u.x,  u.y, 0, 0),
                    new Vector4(u.y, -u.x, 0, 0),
                    new Vector4(0,    0,   1, 0),
                    new Vector4(0,    0,   0, 1)
                ) * transform;

                // #3
                u = transform * origin.ToVector4(0, 1);
                transform = Matrix4x4.Translate(-u) * transform;

                // #4
                u = (transform.MultiplyPoint((t2 + t3) / 2));
                transform = new Matrix4x4(
                    new Vector4(1,  -u.x/u.y, 0, 0),
                    new Vector4(0,   1,       0, 0),
                    new Vector4(0,   0,       1, 0),
                    new Vector4(0,   0,       0, 1)
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
                DrawPolygonOnLightPlane(trapezoidal, lightViewProjection, Color.blue);
                DrawPolygonOnLightPlane(new Vector2[] { nearCenter, farCenter }, lightViewProjection, Color.red);
                DrawPolygonOnLightPlane(convex, lightViewProjection, Color.red);
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
                        if (Cross2(currentDir, dir) < 0)
                            hasLeftSide = true;
                        else if(Vector2.Dot(currentDir, dir) > maxDot)
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
                    if(!boundaryFound && !hasLeftSide)
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

            public static void DrawPolygonOnLightPlane(Vector2[] polygon, Matrix4x4 lightViewProjection, Color color, float z = 0)
            {
                var inverseVP = lightViewProjection.inverse;
                var p = lightViewProjection * new Vector4(0, 0, z, 1);
                var w = p.w;
                p /= w;
                for (var i = 0; i < polygon.Length; i++)
                {
                    var p1 = inverseVP * polygon[i].ToVector4(p.z, 1) * w;
                    var p2 = inverseVP * polygon[(i + 1) % polygon.Length].ToVector4(p.z, 1) * w;
                    Debug.DrawLine(p1, p2, color);

                }
            }

            public static float Cross2(Vector2 u, Vector2 v)
                => u.x * v.y - u.y * v.x;

            public static Vector3[] GetCameraFrustumVerticies(Camera camera, float near, float far)
            {
                var frustumVerts = new Vector3[8];
                int idx = 0;
                var h = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
                var w = h * camera.aspect;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y+=2)
                    {
                        var p = new Vector3(w * x, h * y, 1);
                        var pNear = camera.transform.localToWorldMatrix.MultiplyPoint(p * near);
                        var pFar = camera.transform.localToWorldMatrix.MultiplyPoint(p * far);
                        frustumVerts[idx++] = pNear;
                        frustumVerts[idx++] = pFar;

                        //Debug.DrawLine(pNear, pFar, Color.magenta);
                    }
                return frustumVerts;
            }

            public static void DrawFrustum(FrustumPlanes frustum, bool orthographic, Matrix4x4 transform)
            {
                var verts = new Vector3[]
                {
                    new Vector3(frustum.left, frustum.bottom, frustum.zNear),
                    new Vector3(frustum.right, frustum.bottom, frustum.zNear),
                    new Vector3(frustum.right, frustum.top, frustum.zNear),
                    new Vector3(frustum.left, frustum.top, frustum.zNear),
                    new Vector3(frustum.left, frustum.bottom, frustum.zNear),
                };
                if(orthographic)
                {
                    var extend = Vector3.forward * (frustum.zFar - frustum.zNear);
                    for (var i = 0; i < 4; i++)
                    {
                        Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i + 1]), Color.yellow);
                        Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i] + extend), Color.green);
                        Debug.DrawLine(transform.MultiplyPoint(verts[i] + extend), transform.MultiplyPoint(verts[i + 1] + extend), Color.green);
                    }
                }
                else
                {
                    for (var i = 0; i < 4; i++)
                    {
                        Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i + 1]), Color.yellow);
                        Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i] * frustum.zFar / frustum.zNear), Color.green);
                        Debug.DrawLine(transform.MultiplyPoint(verts[i] * frustum.zFar / frustum.zNear), transform.MultiplyPoint(verts[i + 1] * frustum.zFar / frustum.zNear), Color.green);
                    }
                }
            }

            public static void DrawBound(Bounds bounds, Color color)
            {
                var verts = new Vector2[]
                {
                    new Vector2(-1, -1),
                    new Vector2(1, -1),
                    new Vector2(1, 1),
                    new Vector2(-1, 1),
                    new Vector2(-1, -1),
                };
                for (var i = 0; i < 4; i++)
                {
                    Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, 1)), color);
                    Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, -1)), color);

                    Debug.DrawLine(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1)), bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1)), color);
                }
            }
        }

    }
}