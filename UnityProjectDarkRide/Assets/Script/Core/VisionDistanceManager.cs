using UnityEngine;

/// <summary>
/// Pengelola Jarak Pandang Horor (Vision Distance & Fog Controller).
/// Secara otomatis menyinkronkan:
/// 1. Kabut Hitam Pekat Unity (Linear Fog)
/// 2. Batas Render Kamera (Far Clip Plane)
/// 3. Jangkauan Cahaya Senter (Flashlight Range)
/// Semua diatur presisi sama agar cahaya senter tidak tembus melebihi kabut kegelapan.
/// Mendukung pengeditan live langsung di Inspector (OnValidate)!
/// </summary>
[ExecuteAlways]
public class VisionDistanceManager : MonoBehaviour
{
    private static VisionDistanceManager _instance;
    public static VisionDistanceManager Instance => _instance;

    public enum FogStyle
    {
        ExponentialSquared, // Paling realistis & merata menutup lantai + dinding tanpa batas patah
        Linear              // Memudar lurus dari start ke end
    }

    [Header("=== VISION DISTANCE (JARAK PANDANG) ===")]
    [Tooltip("Gaya Kabut: Linear memberikan transisi gradien halus yang jelas dari titik dekat ke batas akhir.")]
    [SerializeField] private FogStyle fogType = FogStyle.Linear;

    [Tooltip("Batas maksimal jarak pandang pemain dalam meter. Di atas jarak ini, pandangan menjadi kabut pekat.")]
    [Range(3.0f, 40.0f)]
    [SerializeField] private float maxVisionDistance = 12.0f;

    [Tooltip("Titik awal kabut kegelapan (Untuk mode Linear). Objek lebih dekat dari ini terlihat 100% jernih.")]
    [Range(0.0f, 15.0f)]
    [SerializeField] private float fogStartDistance = 2.0f;

    [Tooltip("Ketebalan kabut (Hanya untuk mode ExponentialSquared).")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float fogDensity = 0.15f;

    [Header("=== WARNA KEGELAPAN / FOG ===")]
    [Tooltip("Warna kabut atmosfer horor (Gunakan abu-abu arang malam gelap agar efek kabutnya benar-benar terlihat di mata).")]
    [SerializeField] private Color darknessColor = new Color(0.07f, 0.08f, 0.09f, 1.0f);

    [Header("=== PENERANGAN DASAR LINGKUNGAN (AMBIENT LIGHT) ===")]
    [Tooltip("Tingkat kecerahan suasana remang-remang saat senter mati (0 = Hitam pekat total, 0.15 - 0.35 = Remang-remang terlihat jelas).")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float ambientBrightness = 0.25f;

    [Tooltip("Warna suasana remang-remang lingkungan (abu-abu malam arang kebiruan).")]
    [SerializeField] private Color ambientColor = new Color(0.22f, 0.24f, 0.28f, 1.0f);

    [Header("=== KOMPONEN TARGET (OPSIONAL / AUTO DETECT) ===")]
    [Tooltip("Kamera Utama pemain (Otomatis mendeteksi Camera.main jika kosong).")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Komponen Light Senter Pemain (Otomatis mendeteksi EquipableFlashlight jika kosong).")]
    [SerializeField] private Light playerFlashlight;

    [Header("=== FLASHLIGHT TUNING ===")]
    [Tooltip("Intensitas cahaya senter")]
    [SerializeField] private float flashlightIntensity = 3.5f;

    [Tooltip("Sudut lebar sorot senter (Spot Angle)")]
    [Range(20f, 90f)]
    [SerializeField] private float flashlightSpotAngle = 45f;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        ApplyVisionSettings();
    }

    private void Start()
    {
        AutoFindReferences();
        ApplyVisionSettings();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (playerFlashlight == null)
            {
                AutoFindReferences();
            }

            // Selalu kunci pengaturan fog & kamera setiap frame agar tidak tertimpa oleh lighting scene
            ApplyVisionSettings();
        }
    }

    /// <summary>
    /// Otomatis terpanggil saat kamu menggeser slider nilai di Inspector!
    /// </summary>
    private void OnValidate()
    {
        if (fogStartDistance >= maxVisionDistance)
        {
            fogStartDistance = Mathf.Max(0.5f, maxVisionDistance - 2f);
        }
        ApplyVisionSettings();
    }

    /// <summary>
    /// Mencari referensi kamera dan senter jika belum di-drag di Inspector
    /// </summary>
    public void AutoFindReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (playerFlashlight == null)
        {
            EquipableFlashlight flashlightScript = FindObjectOfType<EquipableFlashlight>();
            if (flashlightScript != null)
            {
                playerFlashlight = flashlightScript.SpotLightComponent;
            }
            else
            {
                // Fallback cari Spot Light di Player
                Light[] lights = FindObjectsOfType<Light>();
                foreach (var l in lights)
                {
                    if (l.type == LightType.Spot && (l.name.ToLower().Contains("senter") || l.name.ToLower().Contains("flash")))
                    {
                        playerFlashlight = l;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Menerapkan settingan jarak pandang ke Fog, Camera, dan Flashlight
    /// </summary>
    public void ApplyVisionSettings()
    {
        // 1. TERAPKAN UNITY FOG (KABUT KEGELAPAN PEKAT)
        RenderSettings.fog = true;
        RenderSettings.fogColor = darknessColor;

        // Terapkan Penerangan Dasar Lingkungan (Remang-remang) agar objek & tekstur tetap terlihat di dekat pemain
        Color effectiveAmbient = ambientColor * ambientBrightness;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = effectiveAmbient;
        RenderSettings.ambientSkyColor = effectiveAmbient;
        RenderSettings.ambientIntensity = ambientBrightness;

        if (fogType == FogStyle.ExponentialSquared)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }
        else
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = maxVisionDistance;
        }

        // 2. KAMERA: JANGAN DIPOTONG PENDEK (FAR CLIP HARUS CUKUP TINGGI AGAR TIDAK BOCOR / SEGITIGA TERPOTONG)
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = darknessColor;
            targetCamera.farClipPlane = 200f;
        }

        // 3. TERAPKAN KE JANGKAUAN SENTER (FLASHLIGHT RANGE)
        if (playerFlashlight != null)
        {
            playerFlashlight.range = maxVisionDistance;
            playerFlashlight.intensity = flashlightIntensity;
            playerFlashlight.spotAngle = flashlightSpotAngle;
        }
    }

    /// <summary>
    /// Fungsi publik untuk mengubah jarak pandang secara dinamis (misal Day 1 vs Day 3)
    /// </summary>
    public void SetVisionDistance(float distance)
    {
        maxVisionDistance = distance;
        ApplyVisionSettings();
    }
}
