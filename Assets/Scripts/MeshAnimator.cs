using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshAnimator : MonoBehaviour
{
    public ComputeShader computeShader;
    [Range(0.0f, 1.0f)]
    public float damping = 0.5f;
    public ComputeBuffer currentAmplitudesBuffer;
    public ComputeBuffer previousAmplitudesBuffer;
    public ComputeBuffer visualizerAmplitudesBuffer;
    private Mesh mesh;
    public Material waveVisualizerMaterial;
    private int m_kernelHandle;
    private uint m_threadGroupSize;
    private int vertexCount;
    private int m_currentBuffer;

    void Start()
    {
        Application.targetFrameRate = 60;

        mesh = GetComponent<MeshFilter>().mesh;
        vertexCount = mesh.vertexCount;
        
        currentAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        previousAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        visualizerAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));

        var initialAmplitudes = new float[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            initialAmplitudes[i] = Random.Range(0f, 1f);  // Random initial disturbance
        }
        currentAmplitudesBuffer.SetData(initialAmplitudes);
        previousAmplitudesBuffer.SetData(initialAmplitudes);
        visualizerAmplitudesBuffer.SetData(initialAmplitudes);

        m_kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(m_kernelHandle, out m_threadGroupSize, out _, out _);

        computeShader.SetBuffer(m_kernelHandle, "currentAmplitudes", currentAmplitudesBuffer);
        computeShader.SetBuffer(m_kernelHandle, "previousAmplitudes", previousAmplitudesBuffer);
        computeShader.SetBuffer(m_kernelHandle, "visualizerAmplitudes", visualizerAmplitudesBuffer);
        var vertexSqrt = (int)Mathf.Sqrt(vertexCount);
        computeShader.SetInt("_Width", vertexSqrt);
        computeShader.SetInt("_Height", vertexSqrt);
        computeShader.SetInt("_CurrentBuffer", m_currentBuffer);

        waveVisualizerMaterial.SetBuffer("amplitudes", visualizerAmplitudesBuffer);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var initialAmplitudes = new float[vertexCount];
            initialAmplitudes[vertexCount / 2] = 1.0f;
            currentAmplitudesBuffer.SetData(initialAmplitudes);
            previousAmplitudesBuffer.SetData(initialAmplitudes);
        }

        computeShader.SetFloat("_Damping", damping);
        computeShader.SetInt("_CurrentBuffer", m_currentBuffer);
        m_currentBuffer = 1 - m_currentBuffer;
    
        int threadGroups = Mathf.CeilToInt(vertexCount / (float)m_threadGroupSize);
        computeShader.Dispatch(m_kernelHandle, threadGroups, 1, 1);
    }

    void OnDestroy()
    {
        currentAmplitudesBuffer?.Release();
        previousAmplitudesBuffer?.Release();
        visualizerAmplitudesBuffer?.Release();
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
        Debug.Log(vertices.Length);
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