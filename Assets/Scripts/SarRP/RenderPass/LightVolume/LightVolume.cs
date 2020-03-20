using UnityEngine;
using System.Collections;
using System;

namespace SarRP.Component
{
    [RequireComponent(typeof(Light))]
    public class LightVolume : MonoBehaviour
    {
        new Light light;
        public Mesh VolumeMesh { get; private set; }
        private void Awake()
        {
            Reset();
        }
        private void Reset()
        {
            light = GetComponent<Light>();
            VolumeMesh = new Mesh();
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
            GetComponent<MeshFilter>().mesh = VolumeMesh;
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
                    0,1,2,
                    0,2,3,
                    0,3,4,
                    0,4,1,
                    1,4,3,
                    1,3,2,
                };
                VolumeMesh.RecalculateNormals();
            }
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireMesh(VolumeMesh, 0, transform.position, transform.rotation, transform.lossyScale);

        }
    }

}