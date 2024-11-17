using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;

public static class MeshUtils
{
    [BurstCompile]
    public struct GeneratedAdjacentVertexIndicesJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> vertices;
        [ReadOnly]
        public NativeArray<int> triangles;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> adjacentVertexIndices;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> adjacentVertexDistances;
        [ReadOnly]
        public int maxEntriesPerVertex; // if 4 neighbours then this is 5. It includes the vertex itself.

        public void Execute(int vertexIndex)
        {
            int baseIndex = vertexIndex * maxEntriesPerVertex;
            adjacentVertexIndices[baseIndex] = vertexIndex;
            adjacentVertexDistances[baseIndex] = 0f;

            for (int i = 1; i < maxEntriesPerVertex; i++)
            {
                adjacentVertexIndices[baseIndex + i] = -1;
                adjacentVertexDistances[baseIndex + i] = float.MaxValue;
            }

            float3 currentPos = vertices[vertexIndex];
            int minAdjacentVertexDistanceIndex = baseIndex + 1;
            int maxAdjacentVertexDistanceIndex = baseIndex + maxEntriesPerVertex - 1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (triangles[i + j] == vertexIndex)
                    {
                        int v1 = triangles[i + ((j + 1) % 3)];
                        int v2 = triangles[i + ((j + 2) % 3)];

                        float dist1 = math.distancesq(currentPos, vertices[v1]);
                        bool canAdd = !Contains(v1, minAdjacentVertexDistanceIndex, maxAdjacentVertexDistanceIndex) && dist1 < adjacentVertexDistances[maxAdjacentVertexDistanceIndex];
                        if (canAdd)
                        {
                            adjacentVertexIndices[maxAdjacentVertexDistanceIndex] = v1;
                            adjacentVertexDistances[maxAdjacentVertexDistanceIndex] = dist1;
                            Sort(minAdjacentVertexDistanceIndex, maxAdjacentVertexDistanceIndex);
                        }

                        float dist2 = math.distancesq(currentPos, vertices[v2]);
                        canAdd = !Contains(v2, minAdjacentVertexDistanceIndex, maxAdjacentVertexDistanceIndex) && dist2 < adjacentVertexDistances[maxAdjacentVertexDistanceIndex];
                        if (canAdd)
                        {
                            adjacentVertexIndices[maxAdjacentVertexDistanceIndex] = v2;
                            adjacentVertexDistances[maxAdjacentVertexDistanceIndex] = dist2;
                            Sort(minAdjacentVertexDistanceIndex, maxAdjacentVertexDistanceIndex);
                        }
                    }
                }
            }
        }

        public void Sort(int startIndex, int endIndex)
        {
            for (int k = 0; k < endIndex - startIndex; k++)
            {
                for (int h = startIndex; h < endIndex; h++)
                {
                    if (adjacentVertexDistances[h] > adjacentVertexDistances[h + 1])
                    {
                        var temp = adjacentVertexDistances[h];
                        adjacentVertexDistances[h] = adjacentVertexDistances[h + 1];
                        adjacentVertexDistances[h + 1] = temp;

                        var tempIndex = adjacentVertexIndices[h];
                        adjacentVertexIndices[h] = adjacentVertexIndices[h + 1];
                        adjacentVertexIndices[h + 1] = tempIndex;
                    }
                }
            }
        }

        public bool Contains(int index, int startIndex, int endIndex)
        {
            bool found = false;
            for (int i = startIndex; i <= endIndex; i++)
            {
                found |= adjacentVertexIndices[i] == index;
            }
            return found;
        }
    }

    public struct GenerateAdjacentVertexIndicesContext
    {
        public Mesh mesh;
        public NativeArray<int> adjacentVertexIndices;
        public NativeArray<float> adjacentVertexDistances;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public int maxEntriesPerVertex;
    }

    public static void GenerateAdjacentVertexIndices(in GenerateAdjacentVertexIndicesContext context)
    {
        var triangles = context.triangles;
        var vertices = context.vertices;
        int vertexCount = vertices.Length;

        var job = new GeneratedAdjacentVertexIndicesJob
        {
            vertices = vertices,
            triangles = triangles,
            adjacentVertexIndices = context.adjacentVertexIndices,
            adjacentVertexDistances = context.adjacentVertexDistances,
            maxEntriesPerVertex = context.maxEntriesPerVertex,
        };

        job.Schedule(vertexCount, 64).Complete();
    }

    public static void ConnectVerticesAtSamePosition(ref Mesh mesh, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs)
    {
        var uniqueVertices = new Dictionary<Vector3, int>(vertices.Count, new Vector3Comparer());
        var vertexMapping = new int[vertices.Count];
        var newVertices = new List<Vector3>(vertices.Count);
        var newNormals = new List<Vector3>(normals.Count);
        var newUVs = new List<Vector2>(uvs.Count);

        for (int i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
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

        var newTriangles = new int[triangles.Count];
        for (int i = 0; i < triangles.Count; i++)
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

        UpdateMeshData(ref mesh, ref vertices, ref triangles, ref normals, ref uvs);
    }

    public static void UpdateMeshData(ref Mesh mesh, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals, ref List<Vector2> uvs)
    {
        mesh.GetVertices(vertices);
        mesh.GetTriangles(triangles, 0);
        mesh.GetNormals(normals);
        mesh.GetUVs(0, uvs);
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