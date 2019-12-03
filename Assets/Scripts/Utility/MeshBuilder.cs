using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MeshBuilder
{
    List<Vector3> verts;
    List<int> trianglesIndex;
    List<Color> vertsColor;
    List<Vector2> uvs;
    public List<Triangle> triangles;
    public MeshBuilder(int size)
    {
        verts = new List<Vector3>(size * 3);
        trianglesIndex = new List<int>(size * 3);
        vertsColor = new List<Color>(size * 3);
        uvs = new List<Vector2>(size * 3);
        triangles = new List<Triangle>(size);
    }
    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        AddTriangle(new Triangle(a, b, c), new TriangleData<Color>(Color.white), new TriangleData<Vector2>(Vector2.zero));
    }
    public void AddTriangle(Triangle triangle, TriangleData<Color> colors, TriangleData<Vector2> uvs)
    {
        // Reverse to avoid normal issue.
        triangle = -triangle;
        int idx = verts.Count;
        verts.Add(triangle.a);
        verts.Add(triangle.b);
        verts.Add(triangle.c);
        trianglesIndex.Add(idx + 0);
        trianglesIndex.Add(idx + 1);
        trianglesIndex.Add(idx + 2);
        vertsColor.Add(colors.a);
        vertsColor.Add(colors.b);
        vertsColor.Add(colors.c);
        this.uvs.Add(uvs.a);
        this.uvs.Add(uvs.b);
        this.uvs.Add(uvs.c);

        triangles.Add(-triangle);
    }
    public void AddTriangle(Triangle triangle)
        => AddTriangle(triangle, new TriangleData<Color>(Color.white), new TriangleData<Vector2>(Vector2.zero));
    public void AddTriangle(Triangle triangle, Color color)
        => AddTriangle(triangle, new TriangleData<Color>(color), new TriangleData<Vector2>(Vector2.zero));

    public void RemoveOverlap()
    {
        HashSet<int> overlapIdx = new HashSet<int>();
        for (int i = 0; i < triangles.Count; i++)
        {
            for (int j = i + 1; j < triangles.Count; j++)
            {
                if(triangles[i] == triangles[j])
                {
                    overlapIdx.Add(i);
                    overlapIdx.Add(j);
                }
            }
        }

        var restTriangles = this.triangles
            .Where((t, idx) => !overlapIdx.Contains(idx))
            .ToArray();

        this.triangles.Clear();
        this.verts.Clear();
        this.trianglesIndex.Clear();

        restTriangles
            .ForEach(triangle => this.AddTriangle(triangle));
    }

    public Mesh ToMesh()
    {
        Mesh mesh = new Mesh();
        mesh.SetVertices(this.verts);
        mesh.SetTriangles(this.trianglesIndex, 0);
        mesh.SetColors(vertsColor);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        return mesh;
    }
}

public struct Triangle
{
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        this.a = a;
        this.b = b;
        this.c = c;
    }

    public Vector3 normal => Vector3.Cross(b - a, c - b).normalized;

    public TriangleEdge u => new TriangleEdge(this.a, this.b);
    public TriangleEdge v => new TriangleEdge(this.b, this.c);
    public TriangleEdge w => new TriangleEdge(this.c, this.a);

    public override bool Equals(object obj)
    {
        return obj is Triangle triangle &&
               a.Equals(triangle.a) &&
               b.Equals(triangle.b) &&
               c.Equals(triangle.c);
    }

    public override int GetHashCode()
    {
        var hashCode = 1474027755;
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(a);
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(b);
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(c);
        return hashCode;
    }

    public static Triangle operator -(Triangle t)
    {
        return new Triangle(t.c, t.b, t.a);
    }

    public static bool operator ==(Triangle u, Triangle v)
        => (u.a == v.a && u.b == v.b && u.c == v.c)
        || (u.a == v.b && u.b == v.c && u.c == v.a)
        || (u.a == v.c && u.b == v.a && u.c == v.b);

    public static bool operator !=(Triangle u, Triangle v)
        => !(u == v);

    public static Triangle operator +(Triangle t, Vector3 delta)
        => new Triangle(t.a + delta, t.b + delta, t.c + delta);
    public static Triangle operator -(Triangle t, Vector3 delta)
        => new Triangle(t.a - delta, t.b - delta, t.c - delta);

    public (int, int) IsAdjoin(Triangle other)
    {
        if (u == other.u)
            return (0, 0);
        else if (u == other.v)
            return (0, 1);
        else if (u == other.w)
            return (0, 2);
        else if (v == other.u)
            return (1, 0);
        else if (v == other.v)
            return (1, 1);
        else if (v == other.w)
            return (1, 2);
        else if (w == other.u)
            return (2, 0);
        else if (w == other.v)
            return (2, 1);
        else if (w == other.w)
            return (2, 2);
        return (-1, -1);
    }
}

public struct TriangleData<T>
{
    public T a;
    public T b;
    public T c;
    public TriangleData(T a, T b, T c)
    {
        this.a = a;
        this.b = b;
        this.c = c;
    }
    public TriangleData(T value) : this(value, value, value) { }
}


public struct TriangleEdge
{
    public Vector3 a;
    public Vector3 b;

    public TriangleEdge(Vector3 a, Vector3 b)
    {
        this.a = a;
        this.b = b;
    }

    public override bool Equals(object obj)
    {
        return obj is TriangleEdge edge &&
               (a.Equals(edge.a) && b.Equals(edge.b) || a.Equals(edge.b) && b.Equals(edge.a));
    }

    public override int GetHashCode()
    {
        long x = (long)a.GetHashCode() * (long)b.GetHashCode();

        return x.GetHashCode();
    }

    public static bool operator ==(TriangleEdge u, TriangleEdge v)
        => (u.a == v.a && u.b == v.b) || (u.a == v.b && u.b == v.a);
    public static bool operator !=(TriangleEdge u, TriangleEdge v)
        => !(u == v);

    public static TriangleEdge operator +(TriangleEdge edge, Vector3 delta)
        => new TriangleEdge(edge.a + delta, edge.b + delta);

    public static TriangleEdge operator -(TriangleEdge edge, Vector3 delta)
        => new TriangleEdge(edge.a - delta, edge.b - delta);
}