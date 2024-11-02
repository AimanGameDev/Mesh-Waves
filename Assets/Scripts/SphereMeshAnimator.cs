using System.Collections.Generic;
using UnityEngine;

public class SphereMeshAnimator : MeshAnimator
{
    public ComputeShader computeShader;
    [Range(0.0f, 1.0f)]
    public float damping = 0.5f;
    public ComputeBuffer adjacentAmplitudesIndicesBuffer;
    public ComputeBuffer inputAmplitudesBuffer;
    public ComputeBuffer currentAmplitudesBuffer;
    public ComputeBuffer previousAmplitudesBuffer;
    public ComputeBuffer visualizerAmplitudesBuffer;
    private Mesh mesh;
    public Material waveVisualizerMaterial;
    private int m_kernelHandle;
    private uint m_threadGroupSize;
    private int vertexCount;
    private int m_currentBuffer;
    private float[] m_inputAmplitudes;
    private int[] m_adjacentAmplitudesIndices;

    private List<int> m_disturbedVertices = new List<int>();

    void Start()
    {
        Application.targetFrameRate = 60;

        mesh = GetComponent<MeshFilter>().mesh;
        vertexCount = mesh.vertexCount;

        var adjacentAmplitudesIndicesCount = vertexCount * 7;
        adjacentAmplitudesIndicesBuffer = new ComputeBuffer(adjacentAmplitudesIndicesCount, sizeof(int));
        inputAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        currentAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        previousAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        visualizerAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));

        m_inputAmplitudes = new float[vertexCount];
        m_adjacentAmplitudesIndices = new int[adjacentAmplitudesIndicesCount];
        for (int i = 0; i < adjacentAmplitudesIndicesCount; i++)
            m_adjacentAmplitudesIndices[i] = -1;

        var vertices = mesh.vertices;
        for (int i = 0; i < vertexCount; i++)
        {
            var vertex = vertices[i];
            var index = i * 7;
            m_adjacentAmplitudesIndices[index] = i;
            var nearestVerticesIndices = GetNearestVerticesIndices(vertex);
            for (int j = 0; j < nearestVerticesIndices.Length; j++)
            {
                var nearestVertexIndex = nearestVerticesIndices[j];
                m_adjacentAmplitudesIndices[index + j + 1] = nearestVertexIndex;
            }
        }

        adjacentAmplitudesIndicesBuffer.SetData(m_adjacentAmplitudesIndices);
        
        m_kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(m_kernelHandle, out m_threadGroupSize, out _, out _);

        computeShader.SetBuffer(m_kernelHandle, "adjacentAmplitudesIndices", adjacentAmplitudesIndicesBuffer);
        computeShader.SetBuffer(m_kernelHandle, "inputAmplitudes", inputAmplitudesBuffer);
        computeShader.SetBuffer(m_kernelHandle, "currentAmplitudes", currentAmplitudesBuffer);
        computeShader.SetBuffer(m_kernelHandle, "previousAmplitudes", previousAmplitudesBuffer);
        computeShader.SetBuffer(m_kernelHandle, "visualizerAmplitudes", visualizerAmplitudesBuffer);
        computeShader.SetInt("_CurrentBuffer", m_currentBuffer);
        computeShader.SetInt("_VertexCount", vertexCount);

        waveVisualizerMaterial.SetBuffer("amplitudes", visualizerAmplitudesBuffer);
        waveVisualizerMaterial.SetVector("_SphereCenter", transform.position);
    }

    void Update()
    {
        for (int i = 0; i < m_inputAmplitudes.Length; i++)
        {
            m_inputAmplitudes[i] = 0.0f;
        }
        for (int i = 0; i < m_disturbedVertices.Count; i++)
        {
            m_inputAmplitudes[m_disturbedVertices[i]] = 1.0f;
        }

        inputAmplitudesBuffer.SetData(m_inputAmplitudes);

        m_disturbedVertices.Clear();

        computeShader.SetFloat("_Damping", damping);
        computeShader.SetInt("_CurrentBuffer", m_currentBuffer);
        m_currentBuffer = 1 - m_currentBuffer;
    
        int threadGroups = Mathf.CeilToInt(vertexCount / (float)m_threadGroupSize);
        computeShader.Dispatch(m_kernelHandle, threadGroups, 1, 1);

        waveVisualizerMaterial.SetVector("_SphereCenter", transform.position);
    }

    public override void AddDisturbedVertex(int vertexIndex)
    {
        m_disturbedVertices.Add(vertexIndex);
    }

    void OnDestroy()
    {
        adjacentAmplitudesIndicesBuffer?.Release();
        inputAmplitudesBuffer?.Release();
        currentAmplitudesBuffer?.Release();
        previousAmplitudesBuffer?.Release();
        visualizerAmplitudesBuffer?.Release();
    }

    private int[] GetNearestVerticesIndices(Vector3 vertex)
    {
        var result = new int[6] { -1, -1, -1, -1, -1, -1 };
        var nearestVertices = new List<(int index, Vector3 position)>(mesh.vertexCount);
        var vertices = mesh.vertices;

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
