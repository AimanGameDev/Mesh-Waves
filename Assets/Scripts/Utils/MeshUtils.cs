using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using System.Linq;
using Unity.Burst;
public static class MeshUtils
{
    [BurstCompile]
    public struct GeneratedAdjacentVertexIndicesJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float3> vertices;
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

                        float dist1 = math.distance(currentPos, vertices[v1]);
                        if (!Contains(v1, minAdjacentVertexDistanceIndex, maxAdjacentVertexDistanceIndex)
                            && dist1 < adjacentVertexDistances[maxAdjacentVertexDistanceIndex])
                        {
                            adjacentVertexIndices[maxAdjacentVertexDistanceIndex] = v1;
                            adjacentVertexDistances[maxAdjacentVertexDistanceIndex] = dist1;
                            Sort(minAdjacentVertexDistanceIndex, maxAdjacentVertexDistanceIndex);
                        }

                        float dist2 = math.distance(currentPos, vertices[v2]);
                        if (!Contains(v2, minAdjacentVertexDistanceIndex, maxAdjacentVertexDistanceIndex)
                            && dist2 < adjacentVertexDistances[maxAdjacentVertexDistanceIndex])
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
                        // Swap distances
                        var temp = adjacentVertexDistances[h];
                        adjacentVertexDistances[h] = adjacentVertexDistances[h + 1];
                        adjacentVertexDistances[h + 1] = temp;

                        // Swap indices
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

    public static void GenerateAdjacentVertexIndices(in Mesh mesh, ref NativeArray<int> adjacentVertexIndices, ref NativeArray<float> adjacentVertexDistances, int maxEntriesPerVertex)
    {
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        int vertexCount = vertices.Length;

        var verticesNativeArray = new NativeArray<float3>(vertices.Select(v => new float3(v.x, v.y, v.z)).ToArray(), Allocator.TempJob);
        var trianglesNativeArray = new NativeArray<int>(triangles, Allocator.TempJob);

        var job = new GeneratedAdjacentVertexIndicesJob
        {
            vertices = verticesNativeArray,
            triangles = trianglesNativeArray,
            adjacentVertexIndices = adjacentVertexIndices,
            adjacentVertexDistances = adjacentVertexDistances,
            maxEntriesPerVertex = maxEntriesPerVertex,
        };

        job.Schedule(vertexCount, 64).Complete();

        verticesNativeArray.Dispose();
        trianglesNativeArray.Dispose();
    }

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