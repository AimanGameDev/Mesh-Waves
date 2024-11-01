using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlaneMeshGenerator : MonoBehaviour
{
    public int width;
    public int height;
    public int resolution;

    public Material material;

    [ContextMenu("Generate Plane")]
    public void GeneratePlane()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = new Mesh();
        meshFilter.mesh = mesh;
        mesh.name = "Procedural Plane";

        var xCount = resolution + 1;
        var zCount = resolution + 1;
        var vertices = new Vector3[xCount * zCount];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(xCount - 1) * (zCount - 1) * 6];

        var xStep = (float)width / resolution;
        var zStep = (float)height / resolution;
    
        for (int z = 0, i = 0; z < zCount; z++)
        {
            for (int x = 0; x < xCount; x++, i++)
            {
                var xPos = x * xStep;
                var zPos = z * zStep;
                vertices[i] = new Vector3(xPos, 0, zPos);
                uv[i] = new Vector2((float)x / (xCount - 1), (float)z / (zCount - 1));
            }
        }

        var ti = 0;
        for (var z = 0; z < zCount - 1; z++)
        {
            for (var x = 0; x < xCount - 1; x++)
            {
                var vi = z * xCount + x;
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xCount;
                triangles[ti + 5] = vi + xCount + 1;
                ti += 6;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshRenderer>().material = material;
    }

    [ContextMenu("Show Vertices")]
    void ShowVertices()
    {
        var vertices = GetComponent<MeshFilter>().mesh.vertices;
        foreach (var vertex in vertices)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = vertex;
            cube.transform.localScale = Vector3.one * 0.1f;
            cube.transform.parent = transform;
        }
        Debug.Log($"Vertices: {vertices.Length}");
    }

    [ContextMenu("Destroy Vertices")]
    void DestroyVertices()
    {
        for (int i = transform.childCount - 1; i >= 0 ; i--)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}