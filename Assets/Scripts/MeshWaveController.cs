using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Info = MeshWaveInfo;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshWaveController : MonoBehaviour
{
    public enum InputBufferStep
    {
        None,
        UpdateDisturbances,
        ResetDisturbances,
    }

    [Range(0.0f, 1.0f)]
    public float damping = 0.95f;
    public bool useObjectCenterAsCenterOfRepulsion;
    public int maxNeighboringVertices;

    [SerializeField]
    private ComputeShader m_computeShaderTemplate;
    private ComputeShader m_computeShaderInstance;
    [SerializeField]
    private Material m_waveVisualizerMaterialTemplate;
    private Material m_waveVisualizerMaterialInstance;

    private ComputeBuffer m_adjacentVertexIndicesBuffer;
    private ComputeBuffer m_inputBuffer;
    private ComputeBuffer m_currentBuffer;
    private ComputeBuffer m_previousBuffer;
    private ComputeBuffer m_visualizerBuffer;
    private Mesh m_mesh;

    private int m_kernelHandle;
    private uint m_threadGroupSize;
    private int m_vertexCount;
    private int m_currentBufferSelector;
    private InputBufferStep m_inputBufferStep;
    private float[] m_inputAmplitudes;
    private List<int> m_disturbedVertexIndices;
    private List<int> m_disturbedVertexIndicesProcessing;
    private NativeArray<int> m_adjacentVertexIndices;
    private Vector3 m_centerOfRepulsionBasedOnAverageNormals;

    private void Awake()
    {
        Application.targetFrameRate = 60;

        m_computeShaderInstance = Instantiate(m_computeShaderTemplate);
        m_waveVisualizerMaterialInstance = Instantiate(m_waveVisualizerMaterialTemplate);
        GetComponent<MeshRenderer>().material = m_waveVisualizerMaterialInstance;
        m_mesh = GetComponent<MeshFilter>().mesh;
        m_vertexCount = m_mesh.vertexCount;

        maxNeighboringVertices = Mathf.Clamp(maxNeighboringVertices, 4, 8);
        var adjacentIndicesBufferSize = maxNeighboringVertices + 1;
        m_adjacentVertexIndicesBuffer = new ComputeBuffer(m_vertexCount * adjacentIndicesBufferSize, sizeof(int));
        m_inputBuffer = new ComputeBuffer(m_vertexCount, sizeof(float));
        m_currentBuffer = new ComputeBuffer(m_vertexCount, sizeof(float));
        m_previousBuffer = new ComputeBuffer(m_vertexCount, sizeof(float));
        m_visualizerBuffer = new ComputeBuffer(m_vertexCount, sizeof(float));

        m_disturbedVertexIndices = new List<int>(Info.MAX_DISTURBED_VERTEX_COUNT);
        m_disturbedVertexIndicesProcessing = new List<int>(Info.MAX_DISTURBED_VERTEX_COUNT);
        m_adjacentVertexIndices = new NativeArray<int>(m_vertexCount * adjacentIndicesBufferSize, Allocator.Persistent);

        MeshUtils.GenerateAdjacentVertexIndices(in m_mesh, ref m_adjacentVertexIndices, adjacentIndicesBufferSize);
        m_adjacentVertexIndicesBuffer.SetData(m_adjacentVertexIndices);

        m_kernelHandle = m_computeShaderInstance.FindKernel(Info.CS_MAIN_KERNEL);
        m_computeShaderInstance.GetKernelThreadGroupSizes(m_kernelHandle, out m_threadGroupSize, out _, out _);

        m_computeShaderInstance.SetBuffer(m_kernelHandle, Info.Buffers.ADJACENT_INDICES_BUFFER, m_adjacentVertexIndicesBuffer);
        m_computeShaderInstance.SetBuffer(m_kernelHandle, Info.Buffers.INPUT_BUFFER, m_inputBuffer);
        m_computeShaderInstance.SetBuffer(m_kernelHandle, Info.Buffers.CURRENT_BUFFER, m_currentBuffer);
        m_computeShaderInstance.SetBuffer(m_kernelHandle, Info.Buffers.PREVIOUS_BUFFER, m_previousBuffer);
        m_computeShaderInstance.SetBuffer(m_kernelHandle, Info.Buffers.VISUALIZER_BUFFER, m_visualizerBuffer);
        m_computeShaderInstance.SetInt(Info.Parameters.CURRENT_BUFFER, m_currentBufferSelector);
        m_computeShaderInstance.SetFloat(Info.Parameters.DAMPING, damping);
        m_computeShaderInstance.SetInt(Info.Parameters.VERTEX_COUNT, m_vertexCount);
        m_computeShaderInstance.SetInt(Info.Parameters.MAX_NEIGHBORS, maxNeighboringVertices);

        m_waveVisualizerMaterialInstance.SetBuffer(Info.Buffers.MATERIAL_AMPLITUDES, m_visualizerBuffer);
        m_waveVisualizerMaterialInstance.SetVector(Info.Parameters.CENTER_OF_REPULSION, GetCenterOfRepulsion());

        m_inputAmplitudes = new float[m_vertexCount];

        m_inputBufferStep = InputBufferStep.None;

        for (int i = 0; i < m_mesh.normals.Length; i++)
            m_centerOfRepulsionBasedOnAverageNormals += m_mesh.normals[i];

        m_centerOfRepulsionBasedOnAverageNormals /= m_mesh.normals.Length;
    }

    private void Update()
    {
        Debug.Log("Input Buffer Step: " + m_inputBufferStep);
        if(m_inputBufferStep == InputBufferStep.None)
        {
            if(m_disturbedVertexIndices.Count > 0)
            {
                m_inputBufferStep = InputBufferStep.UpdateDisturbances;
                for (int i = 0; i < m_disturbedVertexIndices.Count; i++)
                    m_disturbedVertexIndicesProcessing.Add(m_disturbedVertexIndices[i]);

                m_disturbedVertexIndices.Clear();
            }
        }
        else if(m_inputBufferStep == InputBufferStep.UpdateDisturbances)
        {
            for (int i = 0; i < m_disturbedVertexIndicesProcessing.Count; i++)
                m_inputAmplitudes[m_disturbedVertexIndicesProcessing[i]] = 1f;

            m_inputBuffer.SetData(m_inputAmplitudes);
            m_inputBufferStep = InputBufferStep.ResetDisturbances;
        }
        else if(m_inputBufferStep == InputBufferStep.ResetDisturbances)
        {
            for (int i = 0; i < m_disturbedVertexIndicesProcessing.Count; i++)
                m_inputAmplitudes[m_disturbedVertexIndicesProcessing[i]] = 0f;

            m_disturbedVertexIndicesProcessing.Clear();
            m_inputBuffer.SetData(m_inputAmplitudes);
            m_inputBufferStep = InputBufferStep.None;
        }

        m_computeShaderInstance.SetFloat(Info.Parameters.DAMPING, damping);
        m_computeShaderInstance.SetInt(Info.Parameters.CURRENT_BUFFER, m_currentBufferSelector);
        m_currentBufferSelector = 1 - m_currentBufferSelector;
    
        int threadGroups = Mathf.CeilToInt(m_vertexCount / (float)m_threadGroupSize);
        m_computeShaderInstance.Dispatch(m_kernelHandle, threadGroups, 1, 1);

        m_waveVisualizerMaterialInstance.SetVector(Info.Parameters.CENTER_OF_REPULSION, GetCenterOfRepulsion());
    }

    public void AddDisturbedVertex(int vertexIndex)
    {
        if (m_disturbedVertexIndices.Count >= Info.MAX_DISTURBED_VERTEX_COUNT)
            return;

        if (vertexIndex < 0 || vertexIndex >= m_vertexCount)
            return;

        m_disturbedVertexIndices.Add(vertexIndex);
    }

    private void OnDestroy()
    {
        Destroy(m_waveVisualizerMaterialInstance);
        Destroy(m_computeShaderInstance);
        
        m_adjacentVertexIndices.Dispose();

        m_adjacentVertexIndicesBuffer?.Release();
        m_inputBuffer?.Release();
        m_currentBuffer?.Release();
        m_previousBuffer?.Release();
        m_visualizerBuffer?.Release();
    }

    private Vector3 GetCenterOfRepulsion()
    {
        return useObjectCenterAsCenterOfRepulsion ? transform.position : m_centerOfRepulsionBasedOnAverageNormals * 1000000f;
    }
}
