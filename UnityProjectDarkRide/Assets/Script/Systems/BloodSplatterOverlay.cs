using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengelola Visual Bercak Darah Layar (Blood Splatter Overlay - Granny Style).
/// Auto-Setup Fullscreen: Otomatis meretangkan RectTransform bercak darah menjadi 100% Fullscreen tanpa kotak kecil.
/// Semakin tinggi Sanksi / Retry (1/5 -> 4/5), semakin merah & pekat bercak darah di layar HUD.
/// 0% CPU Overhead & Fleksibel.
/// </summary>
public class BloodSplatterOverlay : MonoBehaviour
{
    private static BloodSplatterOverlay _instance;
    public static BloodSplatterOverlay Instance => _instance;

    [Header("UI Reference")]
    [Tooltip("Drag UI Image Bercak Darah (Blood Splatter Texture) di Canvas HUD ke sini")]
    [SerializeField] private Image bloodImage;

    [Header("Blood Intensity Settings")]
    [Tooltip("Warna dasar bercak darah (Default: Merah Pekat Darah)")]
    [SerializeField] private Color bloodColor = new Color(0.85f, 0.05f, 0.05f, 1f);

    [Tooltip("Tingkat kepekatan alpha bercak darah per tingkat sanksi (0/5 = 0, 1/5 = 0.25, 2/5 = 0.45, 3/5 = 0.65, 4/5 = 0.85)")]
    [SerializeField] private float[] sanctionAlphas = new float[] { 0f, 0.25f, 0.45f, 0.65f, 0.85f, 1.0f };

    private void Awake()
    {
        if (_instance == null) _instance = this;
        AutoSetupBloodImage();
    }

    private void AutoSetupBloodImage()
    {
        if (bloodImage == null)
        {
            bloodImage = GetComponent<Image>();
            if (bloodImage == null) bloodImage = GetComponentInChildren<Image>(true);
        }

        // 🎯 OTOMATIS MERETANGKAN RECTTRANSFORM PARENT JADI 100% FULLSCREEN
        RectTransform parentRt = GetComponent<RectTransform>();
        if (parentRt != null)
        {
            parentRt.anchorMin = Vector2.zero;
            parentRt.anchorMax = Vector2.one;
            parentRt.offsetMin = Vector2.zero;
            parentRt.offsetMax = Vector2.one;
        }

        if (bloodImage != null)
        {
            bloodImage.raycastTarget = false; // Bebas klik tidak menghalangi interaksi
            Color c = bloodColor;
            c.a = 0f; // Transparan di awal game (0 sanksi)
            bloodImage.color = c;

            // 🎯 OTOMATIS MERETANGKAN RECTTRANSFORM IMAGE JADI 100% FULLSCREEN
            RectTransform rt = bloodImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.one;
        }
    }

    /// <summary>
    /// Update intensitas kepekatan bercak darah berdasarkan hitungan sanksi (0 s/d 5)
    /// </summary>
    public void UpdateBloodIntensity(int currentSanctionCount, int maxSanctions = 5)
    {
        if (bloodImage == null) AutoSetupBloodImage();
        if (bloodImage == null) return;

        int index = Mathf.Clamp(currentSanctionCount, 0, sanctionAlphas.Length - 1);
        float targetAlpha = sanctionAlphas[index];

        Color c = bloodColor;
        c.a = targetAlpha;
        bloodImage.color = c;
    }

    public void ResetBlood()
    {
        UpdateBloodIntensity(0);
    }
}
