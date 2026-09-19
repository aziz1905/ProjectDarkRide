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
    [Tooltip("Gaya Kabut: ExponentialSquared sangat disarankan untuk game horor karena menutup semua permukaan secara merata!")]
    [SerializeField] private FogStyle fogType = FogStyle.ExponentialSquared;

    [Tooltip("Batas maksimal jarak pandang pemain dalam meter. Di atas jarak ini, pandangan menjadi hitam pekat.")]
    [Range(3.0f, 40.0f)]
    [SerializeField] private float maxVisionDistance = 12.0f;

    [Tooltip("Ketebalan kabut (Hanya untuk mode ExponentialSquared). Makin besar makin gelap & dekat (Default: 0.12 - 0.18).")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float fogDensity = 0.15f;

    [Tooltip("Titik awal kabut kegelapan (Hanya untuk mode Linear).")]
    [Range(0.0f, 15.0f)]
    [SerializeField] private float fogStartDistance = 2.0f;

    [Header("=== WARNA KEGELAPAN / FOG ===")]
    [Tooltip("Warna kabut kegelapan (Default: Hitam Pekat untuk atmosfer horor murni).")]
    [SerializeField] private Color darknessColor = Color.black;

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

        // MATIKAN PENERANGAN LANGIT (AMBIENT LIGHT) SECARA OTOMATIS LEWAT SCRIPT
        // Inilah yang membuat lantai/tanah tetap terlihat terang benderang meskipun tidak ada Directional Light!
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;

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
