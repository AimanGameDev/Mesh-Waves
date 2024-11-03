using UnityEngine;

[RequireComponent(typeof(MeshWaveController))]
public class MeshWaveDisturbanceInputController : MonoBehaviour
{
    private Mesh m_mesh;
    private Vector3[] m_vertices;
    private Transform m_meshTransform;

    private MeshWaveController m_meshWaveController;

    void Start()
    {
        m_meshWaveController = GetComponent<MeshWaveController>();
        m_mesh = GetComponent<MeshFilter>().mesh;
        m_vertices = m_mesh.vertices;
        m_meshTransform = transform;
    }

    public int FindClosestVertexLocal(Vector3 localPoint)
    {
        float minDistance = float.MaxValue;
        int closestVertex = 0;

        for (int i = 0; i < m_vertices.Length; i++)
        {
            float distance = Vector3.Distance(m_vertices[i], localPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestVertex = i;
            }
        }

        return closestVertex;
    }

    public int FindClosestVertexWorld(Vector3 worldPoint)
    {
        // Convert world point to local space
        Vector3 localPoint = m_meshTransform.InverseTransformPoint(worldPoint);
        return FindClosestVertexLocal(localPoint);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    int closestVertex = FindClosestVertexWorld(hit.point);
                    m_meshWaveController?.AddDisturbedVertex(closestVertex);
                }
            }
        }
    }

}
