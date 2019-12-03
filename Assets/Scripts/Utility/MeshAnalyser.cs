using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MeshAnalyser
{
    public class Surface
    {
        public Triangle triangle;
        public List<Surface> AdjacentSurfaces = new List<Surface>(3);
        public Edge[] Edges = new Edge[3];
    }

    public class Vertex
    {
        public Vector3 vert;
        public List<Edge> Edges = new List<Edge>(8);
        public List<Vertex> AdjacentVerts = new List<Vertex>(8);
    }

    public class Edge
    {
        public Vertex v1;
        public Vertex v2;
        public TriangleEdge edge;
        public List<Surface> Surfaces = new List<Surface>(2);
    }

    Dictionary<Triangle, Surface> surfaces;
    Dictionary<TriangleEdge, Edge> edges;
    Dictionary<Vector3, Vertex> vertices;

    public IEnumerable<Triangle> Triangles => surfaces.Keys;

    public MeshAnalyser(int triangles)
    {
        this.surfaces = new Dictionary<Triangle, Surface>(triangles);
        this.edges = new Dictionary<TriangleEdge, Edge>(triangles * 3);
        this.vertices = new Dictionary<Vector3, Vertex>(triangles * 3);
    }

    public Surface AddTriangle(Triangle triangle)
    {
        if (surfaces.ContainsKey(triangle))
            return surfaces[triangle];
        var surface = new Surface()
        {
            triangle = triangle
        };
        surfaces[triangle] = surface;
        surface.Edges[0] = AddEdge(surface, triangle.u);
        surface.Edges[1] = AddEdge(surface, triangle.v);
        surface.Edges[2] = AddEdge(surface, triangle.w);
        return surface;
    }

    public Edge AddEdge(Surface surface, TriangleEdge edge)
    {
        if(edges.ContainsKey(edge))
        {
            var e = edges[edge];
            e.Surfaces.ForEach(s => {
                s.AdjacentSurfaces.Add(surface);
                surface.AdjacentSurfaces.Add(s);
            });
            e.Surfaces.Add(surface);
            return e;
        }
        else
        {
            var e = new Edge()
            {
                edge = edge
            };
            e.Surfaces.Add(surface);
            edges[edge] = e;
            e.v1 = AddVertex(e, edge.a);
            e.v2 = AddVertex(e, edge.b);
            e.v1.Edges.Add(e);
            e.v1.AdjacentVerts.Add(e.v2);
            e.v2.Edges.Add(e);
            e.v2.AdjacentVerts.Add(e.v1);
            return e;
        }
    }

    public Vertex AddVertex(Edge edge, Vector3 vert)
    {
        if(vertices.ContainsKey(vert))
        {
            return vertices[vert];
        }
        else
        {
            Vertex v = new Vertex()
            {
                vert = vert,
            };
            vertices[vert] = v;
            return v;
        }
    }

    public IEnumerable<TriangleEdge> FindBorders()
    {
        Edge startup;
        Edge edge;
        startup = this.edges.Values
            .Where(e => e.Surfaces.Count <= 1)
            .FirstOrDefault();
        if (startup == null)
            yield break;
        edge = startup;

        do
        {
            yield return edge.edge;
            edge = edge.v2.Edges
                .Where(e => e.Surfaces.Count == 1 && e != edge)
                .FirstOrDefault();
        }
        while (edge != startup);
    }
}