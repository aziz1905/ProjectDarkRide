using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengelola Visual Bercak Darah Layar (Blood Splatter Overlay - Subtle Corner Granny Style).
/// Skala diperbesar (1.75x - 2.2x) agar bercak darah HANYA MENEMPEL DI UJUNG SUDUT LAYAR TERLUAR,
/// sehingga area tengah layar 90% TETAP LUAS, BENING, DAN BEBAS PANDANGAN.
/// Production-Ready: 0% Log Spam & Ultra Lightweight.
/// </summary>
public class BloodSplatterOverlay : MonoBehaviour
{
    private static BloodSplatterOverlay _instance;
    public static BloodSplatterOverlay Instance => _instance;

    [Header("UI Reference")]
    [Tooltip("Drag UI Image Bercak Darah (Blood Splatter Texture) di Canvas HUD ke sini")]
    [SerializeField] private Image bloodImage;

    [Header("Blood Color Settings")]
    [Tooltip("Warna dasar bercak darah (Default: Merah Darah Sinematik)")]
    [SerializeField] private Color bloodColor = new Color(0.85f, 0.05f, 0.05f, 1f);

    [Header("Blood Scale Progression (Besarkan Skala Agar Darah Terdorong ke Ujung Sudut)")]
    [Tooltip("Skala diperbesar agar bercak darah terdorong jauh ke sudut terluar layar (0/5 = 2.2x, 1/5 = 1.95x, 2/5 = 1.75x, 3/5 = 1.55x, 4/5 = 1.35x, 5/5 = 1.1x)")]
    [SerializeField] private float[] sanctionScales = new float[] { 2.2f, 1.95f, 1.75f, 1.55f, 1.35f, 1.10f };

    [Header("Blood Opacity Progression (Kepekatan Darah)")]
    [Tooltip("Kepekatan alpha bercak darah per tingkat sanksi kematian")]
    [SerializeField] private float[] sanctionAlphas = new float[] { 0f, 0.20f, 0.35f, 0.50f, 0.70f, 0.90f };

    private void Awake()
    {
        if (_instance == null) _instance = this;

        if (bloodImage == null)
        {
            bloodImage = GetComponent<Image>();
            if (bloodImage == null) bloodImage = GetComponentInChildren<Image>(true);
        }

        if (bloodImage != null)
        {
            bloodImage.raycastTarget = false; // Bebas klik
        }

        ResetBlood();
    }

    /// <summary>
    /// Update Kelebaran (Scale) & Kepekatan (Alpha) bercak darah berdasarkan sanksi kematian (0 s/d 5)
    /// </summary>
    public void UpdateBloodIntensity(int currentSanctionCount, int maxSanctions = 5)
    {
        if (bloodImage == null) return;

        int index = Mathf.Clamp(currentSanctionCount, 0, sanctionAlphas.Length - 1);
        
        float targetAlpha = sanctionAlphas[index];
        float targetScale = sanctionScales[index];

        // 1. Ubah Kepekatan Warna
        Color c = bloodColor;
        c.a = targetAlpha;
        bloodImage.color = c;

        // 2. Ubah Kelebaran Skala (Terdorong Jauh ke Ujung Sudut Layar)
        bloodImage.rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
    }

    public void ResetBlood()
    {
        UpdateBloodIntensity(0);
    }
}
