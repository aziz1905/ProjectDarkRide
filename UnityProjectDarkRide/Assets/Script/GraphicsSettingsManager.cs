using UnityEngine;

public class GraphicsSettingsManager : MonoBehaviour
{
    private const string GRAPHICS_KEY = "GraphicsQualityPreset";

    private void Start()
    {
        int savedQuality = PlayerPrefs.GetInt(GRAPHICS_KEY, 0);
        ApplyQualityPreset(savedQuality);
    }

    public void ApplyQualityPreset(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);

        if (qualityIndex == 0)
            QualitySettings.globalTextureMipmapLimit = 2; // Low
        else if (qualityIndex == 1)
            QualitySettings.globalTextureMipmapLimit = 1; // Medium
        else
            QualitySettings.globalTextureMipmapLimit = 0; // High

        PlayerPrefs.SetInt(GRAPHICS_KEY, qualityIndex);
        PlayerPrefs.Save();
        Debug.Log("[Graphics] Quality Preset diterapkan: Level " + qualityIndex);
    }
}