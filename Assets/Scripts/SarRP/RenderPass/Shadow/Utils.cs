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

                if (settings.light.type == LightType.Point)
                {
                    var maxTanFOV = Mathf.Max(
                        Mathf.Abs(p1.x / p1.z),
                        Mathf.Abs(p1.y / p1.z),
                        Mathf.Abs(p2.x / p2.z),
                        Mathf.Abs(p2.y / p2.z),
                        Mathf.Abs(p3.x / p3.z),
                        Mathf.Abs(p3.y / p3.z),
                        Mathf.Abs(p4.x / p4.z),
                        Mathf.Abs(p4.y / p4.z)
                    );
                    var maxZ = Mathf.Max(p1.z, p2.z, p3.z, p4.z);
                    var fov = Mathf.Atan(maxTanFOV) * Mathf.Rad2Deg;
                    return (view, Matrix4x4.Perspective(fov, 1, 0.1f, maxZ));
                }
                else
                {
                    renderingData.cullResults.GetShadowCasterBounds(lightIndex, out var bounds);

                    var minZ = float.MaxValue;
                    var lightDir = settings.transform.forward;
                    var lightPos = settings.transform.position;

                    for (int x = -1; x <= 1; x += 2)
                        for (int y = -1; y <= 1; y += 2)
                            for (int z = -1; z <= 1; z += 2)
                            {
                                minZ = Mathf.Min(minZ, Vector3.Dot(lightDir, bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)) - lightPos));
                                //Debug.DrawLine(bounds.center, bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                            }

                    // Debug.Log(minZ);
                    
                    var left = Mathf.Min(p0.x, p1.x, p2.x, p3.x, p4.x);
                    var right = Mathf.Max(p0.x, p1.x, p2.x, p3.x, p4.x);
                    var bottom = Mathf.Min(p0.y, p1.y, p2.y, p3.y, p4.y);
                    var top =Mathf.Max(p0.y, p1.y, p2.y, p3.y, p4.y);
                    var zNear = minZ;
                    var zFar = Mathf.Max(p1.z, p2.z, p3.z, p4.z);
                    if(camera.name == "Main Camera")
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