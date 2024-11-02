using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SphereMeshGenerator : MeshGenerator
{
    public float radius = 1f;
    [Range(0, 4)]
    public int subdivisions = 2;

    private Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private Mesh mesh;

    [ContextMenu("Generate")]
    public void Generate()
    {
        vertexMap.Clear();
        vertices.Clear();
        triangles.Clear();

        var meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        mesh.name = "Geodesic Sphere";

        GenerateIcosahedron();

        for (int i = 0; i < subdivisions; i++)
        {
            Subdivide();
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = vertices[i].normalized * radius;
        }

        var uvs = new List<Vector2>();
        foreach (Vector3 vertex in vertices)
        {
            Vector3 normalized = vertex.normalized;
            float u = 0.5f + Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI);
            float v = 0.5f - Mathf.Asin(normalized.y) / Mathf.PI;
            uvs.Add(new Vector2(u, v));
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        Debug.Log($"Generated sphere with {vertices.Count} vertices and {triangles.Count/3} triangles");
    }

    private int AddVertex(Vector3 vertex)
    {
        // Round vertex position to reduce floating point errors
        Vector3 key = new Vector3(
            Mathf.Round(vertex.x * 1000000f) / 1000000f,
            Mathf.Round(vertex.y * 1000000f) / 1000000f,
            Mathf.Round(vertex.z * 1000000f) / 1000000f
        );

        // Return existing vertex if found
        if (vertexMap.TryGetValue(key, out int index))
        {
            return index;
        }

        // Add new vertex
        index = vertices.Count;
        vertexMap[key] = index;
        vertices.Add(vertex);
        return index;
    }

    private void GenerateIcosahedron()
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        // Add vertices
        AddVertex(new Vector3(-1,  t,  0).normalized);
        AddVertex(new Vector3( 1,  t,  0).normalized);
        AddVertex(new Vector3(-1, -t,  0).normalized);
        AddVertex(new Vector3( 1, -t,  0).normalized);
        AddVertex(new Vector3( 0, -1,  t).normalized);
        AddVertex(new Vector3( 0,  1,  t).normalized);
        AddVertex(new Vector3( 0, -1, -t).normalized);
        AddVertex(new Vector3( 0,  1, -t).normalized);
        AddVertex(new Vector3( t,  0, -1).normalized);
        AddVertex(new Vector3( t,  0,  1).normalized);
        AddVertex(new Vector3(-t,  0, -1).normalized);
        AddVertex(new Vector3(-t,  0,  1).normalized);

        // Add faces
        AddFace(0, 11, 5);
        AddFace(0, 5, 1);
        AddFace(0, 1, 7);
        AddFace(0, 7, 10);
        AddFace(0, 10, 11);
        AddFace(1, 5, 9);
        AddFace(5, 11, 4);
        AddFace(11, 10, 2);
        AddFace(10, 7, 6);
        AddFace(7, 1, 8);
        AddFace(3, 9, 4);
        AddFace(3, 4, 2);
        AddFace(3, 2, 6);
        AddFace(3, 6, 8);
        AddFace(3, 8, 9);
        AddFace(4, 9, 5);
        AddFace(2, 4, 11);
        AddFace(6, 2, 10);
        AddFace(8, 6, 7);
        AddFace(9, 8, 1);
    }

    private void AddFace(int v1, int v2, int v3)
    {
        triangles.Add(v1);
        triangles.Add(v2);
        triangles.Add(v3);
    }

    private void Subdivide()
    {
        var newTriangles = new List<int>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            Vector3 m1 = ((v1 + v2) / 2f).normalized;
            Vector3 m2 = ((v2 + v3) / 2f).normalized;
            Vector3 m3 = ((v3 + v1) / 2f).normalized;

            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];
            int m1i = AddVertex(m1);
            int m2i = AddVertex(m2);
            int m3i = AddVertex(m3);

            AddFaceToList(newTriangles, a, m1i, m3i);
            AddFaceToList(newTriangles, b, m2i, m1i);
            AddFaceToList(newTriangles, c, m3i, m2i);
            AddFaceToList(newTriangles, m1i, m2i, m3i);
        }

        triangles = newTriangles;
    }

    private void AddFaceToList(List<int> list, int v1, int v2, int v3)
    {
        list.Add(v1);
        list.Add(v2);
        list.Add(v3);
    }

    public int[] GetAdjacentVertices(int vertexIndex)
    {
        HashSet<int> adjacent = new HashSet<int>();
        
        for (int i = 0; i < triangles.Count; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                if (triangles[i + j] == vertexIndex)
                {
                    adjacent.Add(triangles[i + ((j + 1) % 3)]);
                    adjacent.Add(triangles[i + ((j + 2) % 3)]);
                }
            }
        }

        return new List<int>(adjacent).ToArray();
    }

    public Mesh GetMesh()
    {
        return mesh;
    }
}