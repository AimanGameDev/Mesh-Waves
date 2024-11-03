using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public static class MeshUtils
{
    public static void GenerateAdjacentVertexIndices(in Mesh mesh, ref NativeArray<int> adjacentVertexIndices, int maxEntriesPerVertex)
    {
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        int vertexCount = vertices.Length;

        for (int i = 0; i < adjacentVertexIndices.Length; i++)
        {
            adjacentVertexIndices[i] = -1;
        }

        for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
        {
            int baseIndex = vertexIndex * maxEntriesPerVertex;
            adjacentVertexIndices[baseIndex] = vertexIndex;

            var connectedVertices = new List<(int index, float distance)>();
            var currentPos = vertices[vertexIndex];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (triangles[i + j] == vertexIndex)
                    {
                        int v1 = triangles[i + ((j + 1) % 3)];
                        int v2 = triangles[i + ((j + 2) % 3)];

                        float dist1 = Vector3.Distance(currentPos, vertices[v1]);
                        float dist2 = Vector3.Distance(currentPos, vertices[v2]);

                        if (!connectedVertices.Exists(x => x.index == v1))
                            connectedVertices.Add((v1, dist1));
                        if (!connectedVertices.Exists(x => x.index == v2))
                            connectedVertices.Add((v2, dist2));
                    }
                }
            }

            connectedVertices.Sort((a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < math.min(maxEntriesPerVertex - 1, connectedVertices.Count); i++)
            {
                adjacentVertexIndices[baseIndex + 1 + i] = connectedVertices[i].index;
            }
        }
    }

    public static void ConnectVerticesAtSamePosition(ref Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        var normals = mesh.normals;
        var uvs = mesh.uv;

        var uniqueVertices = new Dictionary<Vector3, int>(new Vector3Comparer());
        var vertexMapping = new int[vertices.Length];
        var newVertices = new List<Vector3>();
        var newNormals = new List<Vector3>();
        var newUVs = new List<Vector2>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            if (uniqueVertices.TryGetValue(vertex, out int existingIndex))
            {
                vertexMapping[i] = existingIndex;
            }
            else
            {
                int newIndex = newVertices.Count;
                uniqueVertices.Add(vertex, newIndex);
                vertexMapping[i] = newIndex;

                newVertices.Add(vertex);
                newNormals.Add(normals[i]);
                newUVs.Add(uvs[i]);
            }
        }

        var newTriangles = new int[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            newTriangles[i] = vertexMapping[triangles[i]];
        }

        mesh.Clear();
        mesh.SetVertices(newVertices);
        mesh.SetNormals(newNormals);
        mesh.SetUVs(0, newUVs);
        mesh.SetTriangles(newTriangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private class Vector3Comparer : IEqualityComparer<Vector3>
    {
        private const float Epsilon = 0.0001f;

        public bool Equals(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < Epsilon;
        }

        public int GetHashCode(Vector3 v)
        {
            return new Vector3(
                Mathf.Round(v.x / Epsilon) * Epsilon,
                Mathf.Round(v.y / Epsilon) * Epsilon,
                Mathf.Round(v.z / Epsilon) * Epsilon
            ).GetHashCode();
        }
    }
}