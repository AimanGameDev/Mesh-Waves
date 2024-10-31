using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshAnimator : MonoBehaviour
{
    public ComputeShader computeShader;
    public float damping = 0.5f;
    public ComputeBuffer amplitudesBuffer;
    public ComputeBuffer previousAmplitudesBuffer;
    private Mesh mesh;
    public Material waveVisualizerMaterial;
    private int m_kernelHandle;
    private uint m_threadGroupSize;
    private int vertexCount;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertexCount = mesh.vertexCount;
        amplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        previousAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));

        var previousAmplitudes = new float[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            previousAmplitudes[i] = UnityEngine.Random.Range(0.0f, 1.0f);
        }
        previousAmplitudesBuffer.SetData(previousAmplitudes);

        m_kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(m_kernelHandle, out m_threadGroupSize, out _, out _);

        computeShader.SetBuffer(m_kernelHandle, "amplitudes", amplitudesBuffer);
        computeShader.SetBuffer(m_kernelHandle, "previousAmplitudes", previousAmplitudesBuffer);
        computeShader.SetInt("_Width", vertexCount / 2);
        computeShader.SetInt("_Height", vertexCount / 2);

        waveVisualizerMaterial.SetBuffer("amplitudes", amplitudesBuffer);
    }

    void Update()
    {
        computeShader.SetFloat("_Damping", damping);
        computeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

        int threadGroups = Mathf.CeilToInt(vertexCount / (float)m_threadGroupSize);
        computeShader.Dispatch(m_kernelHandle, threadGroups, 1, 1);
    
        var amplitudes = new float[vertexCount];
        amplitudesBuffer.GetData(amplitudes);

        for (int i = 0; i < vertexCount; i++)
        {
            Debug.Log(amplitudes[i]);
        }
    }

    void OnDestroy()
    {
        amplitudesBuffer?.Release();
        previousAmplitudesBuffer?.Release();
    }
}