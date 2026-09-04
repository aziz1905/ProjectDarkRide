using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kontroler Anomali Lampu Ruangan (AmbientLampAnomaly).
/// Mode Kedip Acak Murni 2 State (Terang Penuh 100% & Mati Total 0% Secara Acak Asinkron).
/// Menghasilkan kontras pencahayaan horor yang tajam, dramatis, dan sangat bersih dilihat!
/// </summary>
public class AmbientLampAnomaly : MonoBehaviour
{
    public enum AnomalyType
    {
        [Tooltip("Lampu berganti Terang Penuh & Mati Total secara acak tidak serempak")]
        AsynchronousFlicker,

        [Tooltip("Semua lampu padam mati total gelap gulita")]
        BlackoutMatiTotal
    }

    [Header("Pilihan Mode Anomali")]
    [Tooltip("Pilih tipe anomali yang diinginkan untuk ruangan ini")]
    [SerializeField] private AnomalyType anomalyMode = AnomalyType.AsynchronousFlicker;

    [Tooltip("CENTANG jika ingin lampu KEDIP TERUS / MATI TERUS sampai pemain menginjak LampRestoreTrigger untuk memulihkannya!")]
    [SerializeField] private bool stayUntilRestored = false;

    [Header("Pengatur Kecepatan Kedip (Khusus Mode Asynchronous Flicker)")]
    [Tooltip("Kecepatan kedipan tercepat (Detik).")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float minFlickerSpeed = 0.20f;

    [Tooltip("Kecepatan kedipan terlambat (Detik).")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float maxFlickerSpeed = 0.45f;

    [Header("Glitch Effect (Khusus Mode Blackout Mati Total)")]
    [Tooltip("CENTANG jika ingin ada kedipan glitch cepat 3x sesaat sebelum lampu mati total")]
    [SerializeField] private bool flickerBeforeBlackout = true;

    [Header("Visual & Emission Glow Settings")]
    [Tooltip("Warna pijar emisi kaca bola lampu 3D")]
    [SerializeField] private Color emissionColor = new Color(1f, 0.85f, 0.5f); // Kuning Warm
    [SerializeField] private float normalEmissionGlow = 2.5f;

    [Header("Audio SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip glitchSound;
    [SerializeField] private AudioClip restoreSound;

    private class LampPair
    {
        public Light lightComponent;
        public float originalIntensity;
        public Material bulbMaterial;
    }

    private List<LampPair> lampPairs = new List<LampPair>();
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
    private bool isAnomalyActive = false;
    private List<Coroutine> activeFlickerCoroutines = new List<Coroutine>();

    public bool IsAnomalyActive => isAnomalyActive;

    private void Awake()
    {
        PairLampsAndBulbs();
    }

    public void PairLampsAndBulbs()
    {
        lampPairs.Clear();

        Light[] allLights = GetComponentsInChildren<Light>(true);

        foreach (Light l in allLights)
        {
            if (l == null) continue;

            LampPair pair = new LampPair();
            pair.lightComponent = l;
            pair.originalIntensity = l.intensity;

            Transform bulbFolder = l.transform.parent != null ? l.transform.parent : l.transform;
            Renderer bulbRend = null;

            Transform bpChild = bulbFolder.Find("BolaPijar");
            if (bpChild != null)
            {
                bulbRend = bpChild.GetComponent<Renderer>();
            }

            if (bulbRend == null)
            {
                Renderer[] rends = bulbFolder.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    if (r != null && r.gameObject.name.ToLower().Contains("bolapijar"))
                    {
                        bulbRend = r;
                        break;
                    }
                }
            }

            if (bulbRend != null)
            {
                pair.bulbMaterial = bulbRend.material;
                pair.bulbMaterial.EnableKeyword("_EMISSION");
                pair.bulbMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            lampPairs.Add(pair);
        }

        SetAllPairsNormal();
    }

    private void SetAllPairsNormal()
    {
        foreach (var pair in lampPairs)
        {
            if (pair != null)
            {
                SetPairState(pair, true, pair.originalIntensity, normalEmissionGlow);
            }
        }
    }

    public void TriggerAnomaly(float duration = 3.5f)
    {
        if (!gameObject.activeInHierarchy) return;

        if (lampPairs.Count == 0) PairLampsAndBulbs();

        StopAllActiveFlickers();

        if (anomalyMode == AnomalyType.AsynchronousFlicker)
        {
            StartCoroutine(ExecuteFlickerRoutine(duration));
        }
        else
        {
            StartCoroutine(ExecuteBlackoutRoutine(duration));
        }
    }

    private IEnumerator ExecuteFlickerRoutine(float duration)
    {
        isAnomalyActive = true;

        if (audioSource != null && glitchSound != null)
        {
            audioSource.PlayOneShot(glitchSound);
        }

        foreach (LampPair pair in lampPairs)
        {
            if (pair == null || pair.lightComponent == null) continue;

            Coroutine r = StartCoroutine(IndividualLampFlicker(pair, duration, stayUntilRestored));
            activeFlickerCoroutines.Add(r);
        }

        if (!stayUntilRestored)
        {
            yield return new WaitForSeconds(duration);
            RestoreAllLights(true);
        }
    }

    /// <summary>
    /// Kedip Acak Murni 2 State (TERANG 100% atau MATI TOTAL 0%)
    /// </summary>
    private IEnumerator IndividualLampFlicker(LampPair pair, float duration, bool loopForever)
    {
        float timer = 0f;
        yield return new WaitForSeconds(Random.Range(0.01f, 0.15f));

        bool isCurrentlyOn = true;

        while (loopForever || timer < duration)
        {
            if (pair == null || pair.lightComponent == null || !isAnomalyActive) yield break;

            // ⚡ GANTI STATE: 50% Peluang Nyala Terang 100%, 50% Peluang Mati Total 0%
            isCurrentlyOn = !isCurrentlyOn;

            if (isCurrentlyOn)
            {
                // 💡 TERANG PENUH 100%
                SetPairState(pair, true, pair.originalIntensity, normalEmissionGlow);
            }
            else
            {
                // 🌑 MATI TOTAL GELAP 0%
                SetPairState(pair, false, 0f, 0f);
            }

            // Durasi acak tiap step (Min - Max speed)
            float speedMin = Mathf.Min(minFlickerSpeed, maxFlickerSpeed);
            float speedMax = Mathf.Max(minFlickerSpeed, maxFlickerSpeed);
            float stepTime = Random.Range(speedMin, speedMax);

            timer += stepTime;
            yield return new WaitForSeconds(stepTime);
        }

        if (!loopForever)
        {
            SetPairState(pair, true, pair.originalIntensity, normalEmissionGlow);
        }
    }

    private IEnumerator ExecuteBlackoutRoutine(float duration)
    {
        isAnomalyActive = true;

        if (audioSource != null && glitchSound != null)
        {
            audioSource.PlayOneShot(glitchSound);
        }

        if (flickerBeforeBlackout)
        {
            for (int i = 0; i < 3; i++)
            {
                foreach (var pair in lampPairs) if (pair != null) SetPairState(pair, false, 0f, 0f);
                yield return new WaitForSeconds(minFlickerSpeed * 0.7f);

                foreach (var pair in lampPairs) if (pair != null) SetPairState(pair, true, pair.originalIntensity, normalEmissionGlow);
                yield return new WaitForSeconds(minFlickerSpeed * 0.7f);
            }
        }

        foreach (LampPair pair in lampPairs)
        {
            if (pair != null) SetPairState(pair, false, 0f, 0f);
        }

        if (!stayUntilRestored)
        {
            yield return new WaitForSeconds(duration);
            RestoreAllLights(true);
        }
    }

    public void RestoreAllLights(bool withSparkFlicker = true)
    {
        isAnomalyActive = false;
        StopAllActiveFlickers();
        StartCoroutine(PerformRestoreSequence(withSparkFlicker));
    }

    private IEnumerator PerformRestoreSequence(bool withSparkFlicker)
    {
        if (audioSource != null && restoreSound != null)
        {
            audioSource.PlayOneShot(restoreSound);
        }

        if (withSparkFlicker)
        {
            for (int i = 0; i < 2; i++)
            {
                foreach (var pair in lampPairs) if (pair != null) SetPairState(pair, true, pair.originalIntensity, normalEmissionGlow);
                yield return new WaitForSeconds(0.08f);

                foreach (var pair in lampPairs) if (pair != null) SetPairState(pair, false, 0f, 0f);
                yield return new WaitForSeconds(0.08f);
            }
        }

        SetAllPairsNormal();
    }

    private void SetPairState(LampPair pair, bool isLightOn, float intensity, float emissionMultiplier)
    {
        if (pair.lightComponent != null)
        {
            pair.lightComponent.enabled = isLightOn;
            pair.lightComponent.intensity = intensity;
        }

        if (pair.bulbMaterial != null)
        {
            if (isLightOn && emissionMultiplier > 0.01f)
            {
                Color activeColor = emissionColor * Mathf.LinearToGammaSpace(emissionMultiplier);
                pair.bulbMaterial.SetColor(EmissionColorProp, activeColor);
            }
            else
            {
                pair.bulbMaterial.SetColor(EmissionColorProp, Color.black);
            }
        }
    }

    private void StopAllActiveFlickers()
    {
        foreach (var r in activeFlickerCoroutines)
        {
            if (r != null) StopCoroutine(r);
        }
        activeFlickerCoroutines.Clear();
    }
}
