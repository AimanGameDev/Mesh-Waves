public readonly struct MeshWaveInfo
{
    public const int MAX_DISTURBED_VERTEX_COUNT = 8;

    public const string CS_MAIN_KERNEL = "CSMain";
    public readonly struct Buffers
    {
        public const string ADJACENT_INDICES_BUFFER = "adjacentIndicesBuffer";
        public const string INPUT_BUFFER = "inputBuffer";
        public const string CURRENT_BUFFER = "currentBuffer";
        public const string PREVIOUS_BUFFER = "previousBuffer";
        public const string VISUALIZER_BUFFER = "visualizerBuffer";
        public const string MATERIAL_AMPLITUDES = "amplitudes";
    }

    public readonly struct Parameters
    {
        public const string DAMPING = "_Damping";
        public const string VERTEX_COUNT = "_VertexCount";
        public const string CENTER_OF_REPULSION = "_CenterOfRepulsion";
        public const string CURRENT_BUFFER_SELECTOR = "_CurrentBufferSelector";
        public const string MAX_NEIGHBORS = "_MaxNeighbors";
        public const string MIN_WAVE_HEIGHT = "_MinWaveHeight";
        public const string MAX_WAVE_HEIGHT = "_MaxWaveHeight";
        public const string WAVE_HEIGHT = "_WaveHeight";
        public const string COLOR_SHARPNESS = "_ColorSharpness";
    }
}
