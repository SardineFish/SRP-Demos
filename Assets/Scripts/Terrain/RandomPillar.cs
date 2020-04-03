using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class RandomPillar : MonoBehaviour
{
    public Vector2Int Size = new Vector2Int(10, 10);
    public Vector2 BlockSize = new Vector2(1, 1);
    public Vector2 OffsetJitter = new Vector2(0, 0);
    public Vector2 HightRange = new Vector2(0, 10);
    [SerializeField]
    [HideInInspector]
    Mesh mesh;

    [EditorButton]
    private void Reset()
    {
        if (!mesh)
            mesh = new Mesh();
        mesh.Clear();
        List<Vector3> verts = new List<Vector3>(Size.x * Size.y * 8);
        List<int> triangles = new List<int>(10 * 3 * Size.x * Size.y);
        MeshBuilder mb = new MeshBuilder(10 * Size.x * Size.y);
        for (var i = 0; i < Size.y; i++)
        {

            for (var j = 0; j < Size.x; j++)
            {
                float x = j - Size.x / 2f + (Size.x % 2 == 0 ? .5f : 0);
                float y = i - Size.y / 2f + (Size.y % 2 == 0 ? .5f : 0);
                var pos = new Vector3(x, 0, y);
                var offset = verts.Count;
                var height = Mathf.Pow(Random.value, 2) * (HightRange.y - HightRange.x) + HightRange.x;
                var jitterOffset = Vector2.Scale(OffsetJitter, Random.insideUnitCircle).ToVector3XZ(0);

                verts.Clear();
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3(-.5f, 0, -.5f), (BlockSize.ToVector3XZ(1))));
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3( .5f, 0, -.5f), (BlockSize.ToVector3XZ(1))));
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3( .5f, 0,  .5f), (BlockSize.ToVector3XZ(1))));
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3(-.5f, 0,  .5f), (BlockSize.ToVector3XZ(1))));
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3(-.5f, height, -.5f), (BlockSize.ToVector3XZ(1))));
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3( .5f, height, -.5f), (BlockSize.ToVector3XZ(1))));
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3( .5f, height,  .5f), (BlockSize.ToVector3XZ(1))));
                verts.Add(pos + jitterOffset + Vector3.Scale(new Vector3(-.5f, height,  .5f), (BlockSize.ToVector3XZ(1))));

                mb.AddTriangle(verts[0], verts[4], verts[5]);
                mb.AddTriangle(verts[0], verts[5], verts[1]);
                mb.AddTriangle(verts[1], verts[5], verts[6]);
                mb.AddTriangle(verts[1], verts[6], verts[2]);
                mb.AddTriangle(verts[2], verts[6], verts[7]);
                mb.AddTriangle(verts[2], verts[7], verts[3]);
                mb.AddTriangle(verts[3], verts[7], verts[4]);
                mb.AddTriangle(verts[3], verts[4], verts[0]);
                mb.AddTriangle(verts[4], verts[7], verts[6]);
                mb.AddTriangle(verts[4], verts[6], verts[5]);
            }
        }

        Destroy(mesh);
        mesh = mb.ToMesh();
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;

    }
}
