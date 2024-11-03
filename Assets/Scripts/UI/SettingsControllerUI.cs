using UnityEngine;

public class SettingsControllerUI : MonoBehaviour
{
    private MeshWaveConfiguration.Configuration m_meshWaveConfiguration;

    private void Start()
    {
        m_meshWaveConfiguration = MeshWaveConfiguration.Instance.configuration;
    }

    private void Update()
    {
        MeshWaveConfiguration.Instance.ApplyConfiguration(m_meshWaveConfiguration);
    }

    private void OnGUI()
    {
        GUILayout.Label("Settings");

        GUILayout.Label("Target Frame Rate : " + m_meshWaveConfiguration.targetFrameRate);
        m_meshWaveConfiguration.targetFrameRate = (int)GUILayout.HorizontalSlider(m_meshWaveConfiguration.targetFrameRate, 1, 120);

        GUILayout.Label("Damping : " + m_meshWaveConfiguration.damping);
        m_meshWaveConfiguration.damping = GUILayout.HorizontalSlider(m_meshWaveConfiguration.damping, 0f, 1f);

        GUILayout.Label("Wave Height : " + m_meshWaveConfiguration.waveHeight);
        m_meshWaveConfiguration.waveHeight = GUILayout.HorizontalSlider(m_meshWaveConfiguration.waveHeight, 0f, 10f);

        GUILayout.Label("Min Wave Height : " + m_meshWaveConfiguration.minWaveHeight);
        m_meshWaveConfiguration.minWaveHeight = GUILayout.HorizontalSlider(m_meshWaveConfiguration.minWaveHeight, -10f, 10f);

        GUILayout.Label("Max Wave Height : " + m_meshWaveConfiguration.maxWaveHeight);
        m_meshWaveConfiguration.maxWaveHeight = GUILayout.HorizontalSlider(m_meshWaveConfiguration.maxWaveHeight, -10f, 10f);

        GUILayout.Label("Color Sharpness : " + m_meshWaveConfiguration.colorSharpness);
        m_meshWaveConfiguration.colorSharpness = GUILayout.HorizontalSlider(m_meshWaveConfiguration.colorSharpness, 0f, 100f);

        if (GUILayout.Button("Apply Configuration"))
        {
            MeshWaveConfiguration.Instance.ApplyConfiguration(m_meshWaveConfiguration);
        }
    }
}
