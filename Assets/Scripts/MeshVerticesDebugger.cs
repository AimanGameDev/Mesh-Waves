using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshVerticesDebugger : MonoBehaviour
{
    public int vertexIndex;

    public List<int> adjacentVertexIndices;

    [ContextMenu("Debug Vertex")]
    public void DebugVertex()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;

        adjacentVertexIndices = new List<int>();

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                if(mesh.triangles[i + j] == vertexIndex)
                {
                    int v1 = mesh.triangles[i + ((j + 1) % 3)];
                    int v2 = mesh.triangles[i + ((j + 2) % 3)];

                    adjacentVertexIndices.Add(v1);
                    adjacentVertexIndices.Add(v2);
                }
            }
        }
    }

    [ContextMenu("Find Vertices At Same Position")]
    public void FindVerticesAtSamePosition()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        var vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            if(vertices[i] == vertices[vertexIndex])
            {
                Debug.Log("Vertex at index: " + i);
            }
        }
    }

    private void OnDrawGizmos()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(mesh.vertices[vertexIndex]), 0.1f);

        foreach (var adjacentVertexIndex in adjacentVertexIndices)
        {
            Gizmos.DrawLine(transform.TransformPoint(mesh.vertices[vertexIndex]), transform.TransformPoint(mesh.vertices[adjacentVertexIndex]));
        }
    }
}
