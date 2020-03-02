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
                var cameraToLight = settings.transform.worldToLocalMatrix * camera.transform.localToWorldMatrix;
                var p0 = cameraToLight.MultiplyPoint(new Vector3(0, 0, 0));
                var h = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
                var w = h * camera.aspect;
                var p1 = cameraToLight.MultiplyPoint(new Vector3(-w, -h, 1) * settings.MaxShadowDistance);
                var p2 = cameraToLight.MultiplyPoint(new Vector3(w, -h, 1) * settings.MaxShadowDistance);
                var p3 = cameraToLight.MultiplyPoint(new Vector3(w, h, 1) * settings.MaxShadowDistance);
                var p4 = cameraToLight.MultiplyPoint(new Vector3(-w, h, 1) * settings.MaxShadowDistance);

                var view = Matrix4x4.Scale(new Vector3(1, 1, -1)) * settings.transform.worldToLocalMatrix;


                renderingData.cullResults.GetShadowCasterBounds(lightIndex, out var bounds);

                var lightDir = settings.transform.forward;
                var lightPos = settings.transform.position;


                FrustumPlanes cameraFrustum = new FrustumPlanes()
                {
                    left = Mathf.Min(p0.x, p1.x, p2.x, p3.x, p4.x),
                    right = Mathf.Max(p0.x, p1.x, p2.x, p3.x, p4.x),
                    bottom = Mathf.Min(p0.y, p1.y, p2.y, p3.y, p4.y),
                    top = Mathf.Max(p0.y, p1.y, p2.y, p3.y, p4.y),
                    zNear = float.MaxValue,
                    zFar = Mathf.Max(p1.z, p2.z, p3.z, p4.z),
                };
                FrustumPlanes shadowCasterFrustum = new FrustumPlanes()
                {
                    left = float.MaxValue,
                    right = float.MinValue,
                    bottom = float.MaxValue,
                    top = float.MinValue,
                    zNear = float.MaxValue,
                    zFar = float.MinValue,
                };

                var casterBoundVerts = new Vector3[8];
                for (int x = -1, i = 0; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            casterBoundVerts[i++] = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));

                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var pLight = settings.light.transform.worldToLocalMatrix.MultiplyPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                            shadowCasterFrustum.left = Mathf.Min(shadowCasterFrustum.left, pLight.x);
                            shadowCasterFrustum.bottom = Mathf.Min(shadowCasterFrustum.bottom, pLight.y);
                            shadowCasterFrustum.zNear = Mathf.Min(shadowCasterFrustum.zNear, pLight.z);
                            shadowCasterFrustum.right = Mathf.Max(shadowCasterFrustum.right, pLight.x);
                            shadowCasterFrustum.top = Mathf.Max(shadowCasterFrustum.top, pLight.y);
                            shadowCasterFrustum.zFar = Mathf.Max(shadowCasterFrustum.zFar, pLight.z);
                        }

                var left = Mathf.Max(shadowCasterFrustum.left, cameraFrustum.left);
                var right = Mathf.Min(shadowCasterFrustum.right, cameraFrustum.right);
                var bottom = Mathf.Max(shadowCasterFrustum.bottom, cameraFrustum.bottom);
                var top = Mathf.Min(shadowCasterFrustum.top, cameraFrustum.top);
                var zNear = Mathf.Min(shadowCasterFrustum.zNear, cameraFrustum.zNear);
                var zFar = Mathf.Min(shadowCasterFrustum.zFar, cameraFrustum.zFar);

                var debug = camera.name == "Main Camera" && true;

                if (settings.light.type == LightType.Point)
                {
                    var rotation = Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.forward, settings.transform.worldToLocalMatrix.MultiplyPoint(bounds.center).normalized));
                    var rotatedView = rotation.inverse * settings.light.transform.worldToLocalMatrix;
                    var frustumCamera = BestfitFrustum(false, rotatedView, p0, p1, p2, p3, p4);
                    var frustumCaster = BestfitFrustum(false, rotatedView, casterBoundVerts);

                    /*var maxZ = Mathf.Max(p1.z, p2.z, p3.z, p4.z);
                    var fov = Mathf.Atan(Mathf.Min(maxCasterTanFOV.y, maxCameraTanFOV.y)) * Mathf.Rad2Deg;
                    var aspect = Mathf.Min(maxCasterTanFOV.x, maxCameraTanFOV.x) / Mathf.Min(maxCasterTanFOV.y, maxCameraTanFOV.y);
                    */
                    if(debug)
                    {
                        DrawBound(bounds, Color.cyan);
                        DrawFrustum(frustumCaster, false, rotatedView.inverse);
                    }

                    return (Matrix4x4.Scale(new Vector3 (1, 1, -1)) * rotatedView, Matrix4x4.Frustum(frustumCaster));

                    //return (view, Matrix4x4.Frustum(left, right, bottom, top, zNear, zFar));
                }
                else
                {

                    if (camera.name == "Main Camera")
                    {
                        var bound = new Bounds();
                        bound.min = new Vector3(left, bottom, zNear);
                        bound.max = new Vector3(right, top, zFar);
                        DrawLightOrthoFrustum(bound, settings.transform.localToWorldMatrix);
                        DrawBound(bounds, Color.cyan);
                        Debug.DrawRay(settings.transform.localToWorldMatrix.MultiplyPoint(new Vector3(bound.center.x, bound.center.y, zNear)), settings.transform.forward * 5, Color.green);
                    }

                    return (view, Matrix4x4.Ortho(left, right, bottom, top, zNear, zFar));
                    //return Matrix4x4.Frustum(frustum);

                }


                
                //Debug.DrawLine(camera.transform.position, camera.transform.localToWorldMatrix.MultiplyPoint(p1), Color.red);
                //Debug.DrawLine(camera.transform.position, camera.transform.localToWorldMatrix.MultiplyPoint(p3), Color.red);

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

            public static void DrawLightOrthoFrustum(Bounds bounds, Matrix4x4 transform)
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
                    Debug.DrawLine(transform.MultiplyPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1))), transform.MultiplyPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, 1))), Color.green);
                    Debug.DrawLine(transform.MultiplyPoint( bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1))), transform.MultiplyPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i + 1].x, verts[i + 1].y, -1))), Color.yellow);

                    Debug.DrawLine(transform.MultiplyPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, 1))), transform.MultiplyPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(verts[i].x, verts[i].y, -1))), Color.green);
                }
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
                for (var i = 0; i < 4; i++)
                {
                    Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i + 1]), Color.yellow);
                    Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i] * frustum.zFar / frustum.zNear), Color.green);
                    Debug.DrawLine(transform.MultiplyPoint(verts[i] * frustum.zFar / frustum.zNear), transform.MultiplyPoint(verts[i + 1] * frustum.zFar / frustum.zNear), Color.green);
                }
            }

            public static void DrawPerspectiveFrustum(float fov, float aspect, float near, float far, Matrix4x4 transform)
            {
                var tanFOV = Mathf.Tan(fov * Mathf.Deg2Rad);
                var verts = new Vector3[]
                {
                    new Vector3(-near*  tanFOV * aspect, -near* tanFOV, near),
                    new Vector3(near*  tanFOV * aspect, -near* tanFOV, near),
                    new Vector3(near*  tanFOV * aspect, near* tanFOV, near),
                    new Vector3(-near*  tanFOV * aspect, near* tanFOV, near),
                    new Vector3(-near*  tanFOV * aspect, -near* tanFOV, near),
                };
                for(var i=0;i<4;i++)
                {
                    Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i + 1]), Color.yellow);
                    Debug.DrawLine(transform.MultiplyPoint(verts[i]), transform.MultiplyPoint(verts[i] * far / near), Color.green);
                    Debug.DrawLine(transform.MultiplyPoint(verts[i] * far / near), transform.MultiplyPoint(verts[i + 1] * far / near), Color.green);
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