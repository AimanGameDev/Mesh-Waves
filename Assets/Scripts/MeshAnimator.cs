using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshAnimator : MonoBehaviour
{
    public ComputeShader computeShader;
    [Range(0, 2)] public float waveHeight = 0.5f;
    [Range(0, 5)] public float waveFrequency = 1f;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer originalVertexBuffer;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] originalVertices;
    private int kernelHandle;
    private uint threadGroupSize;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        originalVertices = mesh.vertices;

        vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        originalVertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        
        vertexBuffer.SetData(vertices);
        originalVertexBuffer.SetData(originalVertices);

        kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSize, out _, out _);
    }

    void Update()
    {
        computeShader.SetBuffer(kernelHandle, "vertices", vertexBuffer);
        computeShader.SetBuffer(kernelHandle, "originalVertices", originalVertexBuffer);
        computeShader.SetFloat("_Time", Time.time);
        computeShader.SetFloat("_WaveHeight", waveHeight);
        computeShader.SetFloat("_WaveFrequency", waveFrequency);
        computeShader.SetInt("_VertexCount", vertices.Length);
        computeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

        int threadGroups = Mathf.CeilToInt(vertices.Length / (float)threadGroupSize);
        computeShader.Dispatch(kernelHandle, threadGroups, 1, 1);

        vertexBuffer.GetData(vertices);
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        vertexBuffer?.Release();
        originalVertexBuffer?.Release();
    }
}