using UnityEngine;

public class MeshUtils : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private Transform meshTransform;

    public PlaneMeshAnimator planeMeshAnimator;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        meshTransform = transform;
    }

    // Find closest vertex in local space
    public int FindClosestVertexLocal(Vector3 localPoint)
    {
        float minDistance = float.MaxValue;
        int closestVertex = 0;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestVertex = i;
            }
        }

        return closestVertex;
    }

    // Find closest vertex in world space
    public int FindClosestVertexWorld(Vector3 worldPoint)
    {
        // Convert world point to local space
        Vector3 localPoint = meshTransform.InverseTransformPoint(worldPoint);
        return FindClosestVertexLocal(localPoint);
    }

    // Example usage with mouse position
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                int closestVertex = FindClosestVertexWorld(hit.point);
                planeMeshAnimator.AddDisturbedVertex(closestVertex);
            }
        }
    }
}