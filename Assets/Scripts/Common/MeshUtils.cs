using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public static class MeshUtils
{
    public static void GenerateAdjacentVertexIndicesBuffer(in NativeArray<float3> vertices, ref NativeArray<int> adjacentVertexIndices, int maxNumberOfNeighbors)
    {
        for (int i = 0; i < adjacentVertexIndices.Length; i++)
            adjacentVertexIndices[i] = -1;

        for (int i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var index = i * maxNumberOfNeighbors;
            adjacentVertexIndices[index] = i;
            var nearestVerticesIndices = GetNearestVerticesIndices(vertex, vertices);
            for (int j = 0; j < nearestVerticesIndices.Length; j++)
            {
                var nearestVertexIndex = nearestVerticesIndices[j];
                adjacentVertexIndices[index + j + 1] = nearestVertexIndex;
            }
        }
    }

    private static int[] GetNearestVerticesIndices(Vector3 vertex, in NativeArray<float3> vertices)
    {
        var result = new int[6] { -1, -1, -1, -1, -1, -1 };
        var nearestVertices = new List<(int index, Vector3 position)>(vertices.Length);

        for (int i = 0; i < vertices.Length; i++)
            nearestVertices.Add((i, vertices[i]));

        nearestVertices.Sort((a, b) => Vector3.Distance(vertex, a.position).CompareTo(Vector3.Distance(vertex, b.position)));

        var arrayIndex = 0;
        for (int i = 1; i <= 6; i++)
        {
            result[arrayIndex] = nearestVertices[i].index;
            arrayIndex++;
        }
        return result;
    }
}