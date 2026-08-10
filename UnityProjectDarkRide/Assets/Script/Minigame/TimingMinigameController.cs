using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;

/// <summary>
/// Controller Minigame Timing Lingkaran Dinamis.
/// Format Teks Bersih:
/// - Petunjuk: ( Klik Kiri ) Ketuk Lingkaran / ( ESC ) Keluar
/// - Feedback: Murni 'PERFECT' (Hijau) vs 'MISS' (Merah)
/// </summary>
public class TimingMinigameController : MonoBehaviour
{
    private static TimingMinigameController _instance;
    public static TimingMinigameController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimingMinigameController>(true);
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private RectTransform targetRing;
    [SerializeField] private RectTransform approachRing;
    [SerializeField] private TextMeshProUGUI feedbackText;     // Teks PERFECT / MISS (Di titik klik)
    [SerializeField] private TextMeshProUGUI progressText;     // Teks Hit Counter (2/3) di Tengah Atas
    [SerializeField] private TextMeshProUGUI instructionText;  // Teks Petunjuk di Pojok Kiri Tengah

    [Header("Flashlight Auto Dimmer")]
    [Tooltip("CENTANG agar intensitas senter otomatis meredup saat minigame dimulai")]
    [SerializeField] private bool autoDimFlashlight = true;
    [Tooltip("Tingkat intensitas senter saat minigame (Default: 3.0 agar redup nyaman)")]
    [SerializeField] private float dimmedIntensity = 3.0f;

    [Header("Post-Processing Dark Dimmer (Opsional)")]
    [Tooltip("Drag objek Global Volume jika ingin dikombinasikan dengan Post-Processing")]
    [SerializeField] private Volume minigameDarkVolume;

    [Header("Minigame Settings")]
    [SerializeField] private float approachSpeed = 1.2f;    // Kecepatan menyusut (detik)
    [SerializeField] private float startScale = 2.5f;       // Ukuran awal lingkaran luar
    [SerializeField] private float hitTolerance = 0.35f;    // Toleransi zona timing

    [Header("Random Screen Position")]
    [Tooltip("CENTANG agar posisi lingkaran berpindah secara acak di layar pada setiap ketukan")]
    [SerializeField] private bool randomizePosition = true;
    [Tooltip("Batas jarak acak horizontal (Kiri - Kanan)")]
    [SerializeField] private float spawnRadiusX = 260f;
    [Tooltip("Batas jarak acak vertikal (Atas - Bawah)")]
    [SerializeField] private float spawnRadiusY = 160f;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSuccessSound;     // Suara CLANG! dentang besi
    [SerializeField] private AudioClip hitFailSound;        // Suara meleset / gagal

    // Internal State
    private bool isPlaying = false;
    private int currentHits = 0;
    private int targetHits = 3;
    private float currentScale = 2.5f;
    private Action onCompleteCallback;
    private Action onFailedCallback;
    private Coroutine feedbackCoroutine;

    // Flashlight Dimmer Cache
    private Light playerFlashlightLight;
    private float originalFlashlightIntensity = 25f;
    private bool isFlashlightDimmed = false;

    private void Awake()
    {
        if (_instance == null) _instance = this;

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }
        else
        {
            minigamePanel = gameObject;
            minigamePanel.SetActive(false);
        }

        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 0f;
        }
    }

    /// <summary>
    /// Memulai Minigame dengan target jumlah ketukan
    /// </summary>
    public void StartMinigame(int requiredHits, Action onComplete, Action onFailed = null)
    {
        targetHits = requiredHits;
        currentHits = 0;
        onCompleteCallback = onComplete;
        onFailedCallback = onFailed;
        isPlaying = true;

        // 🔦 1. REDUPKAN INTENSITAS SENTER
        if (autoDimFlashlight)
        {
            EquipableFlashlight flashlight = FindObjectOfType<EquipableFlashlight>();
            if (flashlight != null)
            {
                playerFlashlightLight = flashlight.GetComponentInChildren<Light>();
                if (playerFlashlightLight != null && playerFlashlightLight.enabled)
                {
                    originalFlashlightIntensity = playerFlashlightLight.intensity;
                    playerFlashlightLight.intensity = dimmedIntensity;
                    isFlashlightDimmed = true;
                }
            }
        }

        // 🌑 2. AKTIFKAN VOLUME POST-PROCESSING
        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 1f;
        }

        // 🖱️ 3. BUKA KUNCI KURSOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (minigamePanel != null) minigamePanel.SetActive(true);
        gameObject.SetActive(true);

        // 📝 4. SET TEKS PETUNJUK BERSIH & RAPI
        if (instructionText != null)
        {
            instructionText.text = "( Klik Kiri ) Ketuk Lingkaran\n( ESC ) Keluar";
        }

        // 📊 5. TEKS HIT COUNTER (TENGAH ATAS LAYAR)
        UpdateProgressUI();

        // 🎯 6. BERSIHKAN FEEDBACK AWAL
        if (feedbackText != null) feedbackText.text = "";

        SpawnNewCircle();
        Debug.Log("[MINIGAME STARTED] Minigame Dimulai!");
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 🚪 1. TOMBOL ESC UNTUK KELUAR / MEMBATALKAN MINIGAME
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[MINIGAME CANCELLED] Pemain menekan ESC untuk keluar.");
            EndMinigame(false);
            return;
        }

        // ⏱️ 2. ANIMASI MENYUSUT LINGKARAN LUAR
        if (approachRing != null)
        {
            currentScale -= (Time.deltaTime / approachSpeed) * (startScale - 1.0f);
            approachRing.localScale = new Vector3(currentScale, currentScale, 1f);
        }

        // 🖱️ 3. DETEKSI INPUT KLIK KIRI MOUSE
        if (Input.GetMouseButtonDown(0))
        {
            CheckHitWithCursorPosition();
        }

        // ⌛ 4. JIKA WAKTU HABIS TANPA DIKLIK (TIMEOUT / MISS)
        if (currentScale < (1.0f - hitTolerance))
        {
            OnMiss("MISS");
        }
    }

    private void CheckHitWithCursorPosition()
    {
        Vector2 hitPosition = targetRing != null ? targetRing.anchoredPosition : Vector2.zero;

        bool isInsideTarget = false;
        if (targetRing != null)
        {
            isInsideTarget = RectTransformUtility.RectangleContainsScreenPoint(targetRing, Input.mousePosition);
        }

        if (!isInsideTarget)
        {
            // Klik di luar lingkaran -> Muncul teks MISS
            ShowFeedbackAtPosition("<color=red>MISS</color>", hitPosition);
            OnMissInternal();
            return;
        }

        float diff = Mathf.Abs(currentScale - 1.0f);

        if (diff <= hitTolerance)
        {
            // 🟢 HIT BERHASIL! (Muncul teks PERFECT di lokasi klik)
            currentHits++;
            UpdateProgressUI();
            Debug.Log($"[MINIGAME HIT] Ketukan Berhasil! ({currentHits}/{targetHits})");

            if (audioSource != null && hitSuccessSound != null)
                audioSource.PlayOneShot(hitSuccessSound);

            ShowFeedbackAtPosition("<color=green>PERFECT</color>", hitPosition);

            if (currentHits >= targetHits)
            {
                EndMinigame(true);
            }
            else
            {
                SpawnNewCircle();
            }
        }
        else
        {
            // Terlalu cepat -> Muncul teks MISS
            ShowFeedbackAtPosition("<color=red>MISS</color>", hitPosition);
            OnMissInternal();
        }
    }

    private void OnMiss(string message)
    {
        Vector2 hitPosition = targetRing != null ? targetRing.anchoredPosition : Vector2.zero;
        ShowFeedbackAtPosition("<color=red>MISS</color>", hitPosition);
        OnMissInternal();
    }

    private void OnMissInternal()
    {
        if (audioSource != null && hitFailSound != null)
            audioSource.PlayOneShot(hitFailSound);

        currentHits = Mathf.Max(0, currentHits - 1);
        UpdateProgressUI();
        SpawnNewCircle();
    }

    /// <summary>
    /// Memunculkan teks PERFECT/MISS tepat di posisi tempat lingkaran diklik, lalu otomatis menghilang perlahan
    /// </summary>
    private void ShowFeedbackAtPosition(string message, Vector2 position)
    {
        if (feedbackText == null) return;

        feedbackText.rectTransform.anchoredPosition = position + new Vector2(0f, 65f);
        feedbackText.text = message;

        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(ClearFeedbackAfterDelay(0.6f));
    }

    private IEnumerator ClearFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (feedbackText != null) feedbackText.text = "";
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"<b>KETUKAN: {currentHits} / {targetHits}</b>";
        }
    }

    private void SpawnNewCircle()
    {
        currentScale = startScale;

        Vector2 randomPos = Vector2.zero;
        if (randomizePosition)
        {
            randomPos = new Vector2(
                UnityEngine.Random.Range(-spawnRadiusX, spawnRadiusX),
                UnityEngine.Random.Range(-spawnRadiusY, spawnRadiusY)
            );
        }

        if (targetRing != null) targetRing.anchoredPosition = randomPos;
        if (approachRing != null)
        {
            approachRing.anchoredPosition = randomPos;
            approachRing.localScale = new Vector3(startScale, startScale, 1f);
        }
    }

    private void EndMinigame(bool isSuccess)
    {
        isPlaying = false;
        if (minigamePanel != null) minigamePanel.SetActive(false);

        // 🔦 1. KEMBALIKAN INTENSITAS SENTER
        if (isFlashlightDimmed && playerFlashlightLight != null)
        {
            playerFlashlightLight.intensity = originalFlashlightIntensity;
            isFlashlightDimmed = false;
        }

        // ☀️ 2. MATIKAN VOLUME POST-PROCESSING
        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 0f;
        }

        // 🔒 3. KUNCI KURSOR KEMBALI
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (isSuccess)
        {
            Debug.Log("[MINIGAME WON] Minigame Selesai 100%!");
            onCompleteCallback?.Invoke();
        }
        else
        {
            onFailedCallback?.Invoke();
        }
    }

    public bool IsPlaying => isPlaying;
}