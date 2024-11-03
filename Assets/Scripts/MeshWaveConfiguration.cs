using System;
using UnityEngine;

public class MeshWaveConfiguration : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var go = new GameObject("MeshWaveConfiguration");
        DontDestroyOnLoad(go);
        var config = go.AddComponent<MeshWaveConfiguration>();
        config.InitializeDefaultValues();
        Instance = config;
    }

    public static MeshWaveConfiguration Instance { get; private set; }

    private void InitializeDefaultValues()
    {
        var config = configuration;

        config.targetFrameRate = 60;
        config.damping = 0.90f;
        config.waveHeight = 1.0f;
        config.minWaveHeight = -1.0f;
        config.maxWaveHeight = 1.0f;
        config.colorSharpness = 10f;

        ApplyConfiguration(config);
    }

    public void ApplyConfiguration(Configuration config)
    {
        Application.targetFrameRate = config.targetFrameRate;
        configuration = config;
    }

    [Serializable]
    public struct Configuration
    {
        public int targetFrameRate;
        public float damping;
        public float waveHeight;
        public float minWaveHeight;
        public float maxWaveHeight;
        public float colorSharpness;
    }

    public Configuration configuration { get; private set; }
}
