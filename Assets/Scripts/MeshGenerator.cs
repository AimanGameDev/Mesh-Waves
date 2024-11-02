using UnityEngine;

public abstract class MeshGenerator : MonoBehaviour
{
    [ContextMenu("Show Vertices")]
    protected void ShowVertices()
    {
        var vertices = GetComponent<MeshFilter>().mesh.vertices;
        foreach (var vertex in vertices)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = transform.TransformPoint(vertex);
            cube.transform.localScale = Vector3.one * 0.1f;
            cube.transform.parent = transform;
        }
        Debug.Log($"Vertices: {vertices.Length}");
    }

    [ContextMenu("Destroy Vertices")]
    protected void DestroyVertices()
    {
        for (int i = transform.childCount - 1; i >= 0 ; i--)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}
