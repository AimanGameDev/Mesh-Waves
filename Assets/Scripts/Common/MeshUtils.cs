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

            for (int i = 0; i < math.min(6, connectedVertices.Count); i++)
            {
                adjacentVertexIndices[baseIndex + 1 + i] = connectedVertices[i].index;
            }
        }
    }
}