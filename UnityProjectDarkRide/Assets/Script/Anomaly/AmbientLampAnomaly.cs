using System.Collections;
using UnityEngine;

/// <summary>
/// Script Pengatur Lampu Lorong & Efek Anomali Horor (Mati Mendadak & Menyala Kembali).
/// Sinkron 100% antara Sinar Lampu (Light) DENGAN Pijar Kaca 3D (BolaPijar Emission).
/// TIDAK mengganggu sistem interaksi (Murni Lampu Lingkungan & Atmosfer).
/// 0% Beban CPU & 0 B/frame GC Alloc.
/// </summary>
public class AmbientLampAnomaly : MonoBehaviour
{
    [Header("Visual & Light Components")]
    [Tooltip("Drag komponen Spot Light / Point Light lampu di sini")]
    [SerializeField] private Light lampLight;

    [Tooltip("Drag Objek 3D 'BolaPijar' dari Hierarchy ke sini")]
    [SerializeField] private GameObject bolaPijarObject;

    [Header("Normal Lighting Settings")]
    [SerializeField] private float normalIntensity = 3.0f;
    [SerializeField] private Color emissionColor = new Color(1f, 0.85f, 0.5f); // Warna kuning warm
    [SerializeField] private float normalEmissionGlow = 2.0f;

    [Header("Auto Anomaly Blackout (Opsional)")]
    [Tooltip("CENTANG jika ingin lampu ini sesekali mati mendadak secara otomatis")]
    [SerializeField] private bool enableAutoAnomaly = true;

    [Tooltip("Berapa detik lampu MATI TOTAL saat anomali terjadi")]
    [SerializeField] private float blackoutDuration = 2.0f;

    [Tooltip("Jeda waktu acak (detik) antar kejadian anomali (Min - Max)")]
    [SerializeField] private float minIntervalSeconds = 15.0f;
    [SerializeField] private float maxIntervalSeconds = 35.0f;

    [Tooltip("CENTANG jika ingin ada kedipan cepat (glitch) sebelum lampu mati total")]
    [SerializeField] private bool flickerBeforeBlackout = true;

    [Header("Audio SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip glitchSound; // Suara listrik mati / cetek

    // Internal State
    private Renderer bolaPijarRenderer;
    private Material bolaPijarMaterial;
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
    private bool isBlackout = false;
    private float nextAnomalyTimer = 0f;

    private void Start()
    {
        // Cache Komponen & Material Instance BolaPijar
        if (bolaPijarObject != null)
        {
            bolaPijarRenderer = bolaPijarObject.GetComponent<Renderer>();
            if (bolaPijarRenderer != null)
            {
                bolaPijarMaterial = bolaPijarRenderer.material;
                bolaPijarMaterial.EnableKeyword("_EMISSION");
            }
        }

        // Set Kondisi Awal: Lampu & BolaPijar Menyala Normal
        SetLampState(true, normalIntensity, normalEmissionGlow);

        // Atur timer acak untuk anomali berikutnya
        ResetAnomalyTimer();
    }

    private void Update()
    {
        if (!enableAutoAnomaly || isBlackout) return;

        nextAnomalyTimer -= Time.deltaTime;
        if (nextAnomalyTimer <= 0f)
        {
            StartCoroutine(PerformBlackoutRoutine(blackoutDuration));
            ResetAnomalyTimer();
        }
    }

    private Coroutine blackoutCoroutine;

    /// <summary>
    /// Fungsi Publik untuk mematikan lampu dari Trigger / Event Horor luar (misal saat monster lewat)
    /// </summary>
    public void TriggerBlackout(float duration = 2.0f)
    {
        if (!gameObject.activeInHierarchy) return;
        if (blackoutCoroutine != null) StopCoroutine(blackoutCoroutine);
        blackoutCoroutine = StartCoroutine(PerformBlackoutRoutine(duration));
    }

    /// <summary>
    /// Fungsi Publik untuk MENYALAKAN KEMBALI semua lampu seketika saat pemain melewati trigger pemulih (Restore Trigger)
    /// </summary>
    public void RestoreLights(bool withSparkEffect = true)
    {
        if (!gameObject.activeInHierarchy) return;
        if (blackoutCoroutine != null) StopCoroutine(blackoutCoroutine);
        StartCoroutine(PerformRestoreRoutine(withSparkEffect));
    }

    private IEnumerator PerformBlackoutRoutine(float duration)
    {
        isBlackout = true;

        // ⚡ 1. Efek Kedipan Glitch Sebelum Mati (0.3 detik)
        if (flickerBeforeBlackout)
        {
            if (audioSource != null && glitchSound != null)
            {
                audioSource.PlayOneShot(glitchSound);
            }

            for (int i = 0; i < 3; i++)
            {
                SetLampState(false, 0f, 0f);
                yield return new WaitForSeconds(0.05f);
                SetLampState(true, normalIntensity * 0.4f, normalEmissionGlow * 0.4f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        // 🌑 2. LAMPU & BOLA PIJAR MATI TOTAL (Gelap Gulita)
        SetLampState(false, 0f, 0f);

        // Tunggu selama durasi blackout (bisa sangat lama / 9999 detik)
        yield return new WaitForSeconds(duration);

        // 💡 3. LAMPU & BOLA PIJAR MENYALA NORMAL KEMBALI (Jika durasi habis sebelum trigger pemulih)
        SetLampState(true, normalIntensity, normalEmissionGlow);
        isBlackout = false;
    }

    private IEnumerator PerformRestoreRoutine(bool withSparkEffect)
    {
        // ⚡ Efek Percikan Listrik saat Menyala Kembali
        if (withSparkEffect)
        {
            for (int i = 0; i < 2; i++)
            {
                SetLampState(true, normalIntensity * 0.5f, normalEmissionGlow * 0.5f);
                yield return new WaitForSeconds(0.06f);
                SetLampState(false, 0f, 0f);
                yield return new WaitForSeconds(0.06f);
            }
        }

        // 💡 MENYALA TERANG STABIL NORMAL KEMBALI
        SetLampState(true, normalIntensity, normalEmissionGlow);
        isBlackout = false;
        Debug.Log($"<color=green>[LIGHTS RESTORED] Lampu '{gameObject.name}' berhasil dinyalakan normal kembali!</color>");
    }

    /// <summary>
    /// Mengatur intensitas Sinar Lampu dan Cahaya Pijar Kaca BolaPijar secara serentak
    /// </summary>
    private void SetLampState(bool isLightOn, float intensity, float emissionMultiplier)
    {
        if (lampLight != null)
        {
            lampLight.enabled = isLightOn;
            lampLight.intensity = intensity;
        }

        if (bolaPijarMaterial != null)
        {
            if (isLightOn && emissionMultiplier > 0f)
            {
                Color activeColor = emissionColor * Mathf.LinearToGammaSpace(emissionMultiplier);
                bolaPijarMaterial.SetColor(EmissionColorProp, activeColor);
            }
            else
            {
                // Padam total (Hitam gelap)
                bolaPijarMaterial.SetColor(EmissionColorProp, Color.black);
            }
        }
    }

    private void ResetAnomalyTimer()
    {
        nextAnomalyTimer = Random.Range(minIntervalSeconds, maxIntervalSeconds);
    }
}
