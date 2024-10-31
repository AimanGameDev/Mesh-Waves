using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SphereMeshGenerator : MonoBehaviour
{
    public float radius = 1f;
    public int resolution = 32;
    public int rings = 16;

    [ContextMenu("Generate")]
    public void Generate()
    {
        var meshFilter = GetComponent<MeshFilter>();
        var mesh = new Mesh();
        meshFilter.mesh = mesh;
        mesh.name = "Procedural Sphere";

        // Calculate vertices
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        
        vertices.Add(Vector3.up * radius);
        uv.Add(new Vector2(0.5f, 1f));

        for (int lat = 1; lat < rings; lat++)
        {
            float phi = Mathf.PI * lat / rings;
            float y = Mathf.Cos(phi);
            float r = Mathf.Sin(phi);

            for (int lon = 0; lon < resolution; lon++)
            {
                float theta = 2f * Mathf.PI * lon / resolution;
                float x = r * Mathf.Sin(theta);
                float z = r * Mathf.Cos(theta);

                vertices.Add(new Vector3(x, y, z) * radius);
                uv.Add(new Vector2((float)lon / resolution, 1f - (float)lat / rings));
            }
        }

        vertices.Add(Vector3.down * radius);
        uv.Add(new Vector2(0.5f, 0f));

        var triangles = new List<int>();

        for (int i = 0; i < resolution; i++)
        {
            triangles.Add(0);
            triangles.Add(1 + i);
            triangles.Add(1 + (i + 1) % resolution);
        }

        for (int lat = 0; lat < rings - 2; lat++)
        {
            int ringStart = 1 + lat * resolution;
            int nextRingStart = 1 + (lat + 1) * resolution;

            for (int lon = 0; lon < resolution; lon++)
            {
                int current = ringStart + lon;
                int next = ringStart + (lon + 1) % resolution;
                int nextRingCurrent = nextRingStart + lon;
                int nextRingNext = nextRingStart + (lon + 1) % resolution;

                triangles.Add(current);
                triangles.Add(nextRingCurrent);
                triangles.Add(next);

                triangles.Add(next);
                triangles.Add(nextRingCurrent);
                triangles.Add(nextRingNext);
            }
        }

        int lastVertex = vertices.Count - 1;
        int lastRingStart = lastVertex - resolution;
        for (int i = 0; i < resolution; i++)
        {
            triangles.Add(lastVertex);
            triangles.Add(lastRingStart + (i + 1) % resolution);
            triangles.Add(lastRingStart + i);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();
    }
}
