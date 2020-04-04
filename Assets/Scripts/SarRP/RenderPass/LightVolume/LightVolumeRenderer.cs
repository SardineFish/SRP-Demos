using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SarRP.Component
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class LightVolumeRenderer : MonoBehaviour
    {
        public Vector2 RayMarchingRange = new Vector2(0, 100);
        public int RayMarchingSteps = 8;
        [Range(0, 1)]
        public float IncomingLoss = 0;
        public float LightDistance = 100;
        public bool ExtinctionOverride = false;
        public float VisibilityDistance = 100;
        public float IntensityMultiplier = 1;
        public new Light light { get; private set; }
        public Mesh VolumeMesh { get; private set; }
        float previousAngle;
        float previousRange;
        private void Awake()
        {
            light = GetComponent<Light>();
            VolumeMesh = new Mesh();
            Reset();
            previousAngle = light.spotAngle;
            previousRange = light.range;
        }
        private void Reset()
        {
            VolumeMesh.vertices = new Vector3[]
            {
                new Vector3(-1, -1, -1),
                new Vector3(-1,  1, -1),
                new Vector3( 1,  1, -1),
                new Vector3( 1, -1, -1),
                new Vector3(-1, -1,  1),
                new Vector3(-1,  1,  1),
                new Vector3( 1,  1,  1),
                new Vector3( 1, -1,  1),
            };
            VolumeMesh.triangles = new int[]
            {
                0,1,2, 0,2,3,
                0,4,5, 0,5,1,
                1,5,6, 1,6,2,
                2,6,7, 2,7,3,
                0,3,7, 0,7,4,
                4,6,5, 4,7,6,
            };
            VolumeMesh.RecalculateNormals();
            UpdateMesh();
        }
        private void Update()
        {
            if(light.spotAngle != previousAngle || light.range != previousRange)
            {
                previousAngle = light.spotAngle;
                previousRange = light.range;
                UpdateMesh();
            }
        }

        void UpdateMesh()
        {
            if(light.type == LightType.Spot)
            {
                var tanFOV = Mathf.Tan(light.spotAngle / 2 * Mathf.Deg2Rad);
                var verts = new Vector3[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(-tanFOV, -tanFOV, 1) * light.range,
                    new Vector3(-tanFOV,  tanFOV, 1) * light.range,
                    new Vector3( tanFOV,  tanFOV, 1) * light.range,
                    new Vector3( tanFOV, -tanFOV, 1) * light.range,
                };
                VolumeMesh.Clear();
                VolumeMesh.vertices = verts;
                VolumeMesh.triangles = new int[]
                {
                    0, 1, 2,
                    0, 2, 3,
                    0, 3, 4,
                    0, 4, 1,
                    1, 4, 3,
                    1, 3, 2,
                };
                VolumeMesh.RecalculateNormals();
            }
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireMesh(VolumeMesh, 0, transform.position, transform.rotation, transform.lossyScale);

        }

        List<Vector4> planes = new List<Vector4>(6);
        public List<Vector4> GetVolumeBoundFaces(Camera camera)
        {
            planes.Clear();
            Matrix4x4 viewProjection = Matrix4x4.identity;
            if(light.type == LightType.Spot)
            {
                viewProjection = Matrix4x4.Perspective(light.spotAngle, 1, 0.03f, light.range) * Matrix4x4.Scale(new Vector3(1, 1, -1)) * light.transform.worldToLocalMatrix;
                var m0 = viewProjection.GetRow(0);
                var m1 = viewProjection.GetRow(1);
                var m2 = viewProjection.GetRow(2);
                var m3 = viewProjection.GetRow(3);
                planes.Add( -(m3 + m0));
                planes.Add( -(m3 - m0));
                planes.Add( -(m3 + m1));
                planes.Add( -(m3 - m1));
                // planes.Add( -(m3 + m2)); // ignore near
                planes.Add( -(m3 - m2));
            }
            else if(light.type == LightType.Directional)
            {
                viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
                var m2 = viewProjection.GetRow(2);
                var m3 = viewProjection.GetRow(3);
                planes.Add(-(m3 + m2)); // near plane only
            }
            return planes;

        }
    }

}