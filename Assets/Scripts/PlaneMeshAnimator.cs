using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PlaneMeshAnimator : MeshAnimator
{
    public ComputeShader computeShader;
    [Range(0.0f, 1.0f)]
    public float damping = 0.5f;
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

    private List<int> m_disturbedVertices = new List<int>();

    void Start()
    {
        Application.targetFrameRate = 60;

        mesh = GetComponent<MeshFilter>().mesh;
        vertexCount = mesh.vertexCount;

        inputAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        currentAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        previousAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));
        visualizerAmplitudesBuffer = new ComputeBuffer(vertexCount, sizeof(float));

        m_inputAmplitudes = new float[vertexCount];

        m_kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(m_kernelHandle, out m_threadGroupSize, out _, out _);

        computeShader.SetBuffer(m_kernelHandle, "inputAmplitudes", inputAmplitudesBuffer);
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
    }

    public override void AddDisturbedVertex(int vertexIndex)
    {
        m_disturbedVertices.Add(vertexIndex);
    }

    void OnDestroy()
    {
        inputAmplitudesBuffer?.Release();
        currentAmplitudesBuffer?.Release();
        previousAmplitudesBuffer?.Release();
        visualizerAmplitudesBuffer?.Release();
    }
}