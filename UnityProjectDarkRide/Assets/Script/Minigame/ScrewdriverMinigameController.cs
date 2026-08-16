using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller Minigame Obeng 2 Baut (Circular Mouse Drag Rotation).
/// - 100% Ringan: 0 B/frame GC Alloc & Kompatibel Spek Minimum Low-End PC.
/// - Menampilkan 2 Baut (Baut Kiri & Baut Kanan).
/// - Pemain menahan [Klik Kiri] + memutar mouse melingkar mengikuti panah untuk mengencangkan baut.
/// - Begitu kedua baut 100% kencang -> Minigame selesai & tugas tercoret di Notebook TAB!
/// </summary>
public class ScrewdriverMinigameController : MonoBehaviour
{
    private static ScrewdriverMinigameController _instance;
    public static ScrewdriverMinigameController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ScrewdriverMinigameController>(true);
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private TextMeshProUGUI instructionText;  // Teks Petunjuk Kontrol
    [SerializeField] private TextMeshProUGUI progressText;     // Teks Counter (Baut 1/2)

    [Header("Screw 1 (Kiri) UI")]
    [SerializeField] private RectTransform leftScrewTransform;
    [SerializeField] private Image leftProgressRing;
    [SerializeField] private GameObject leftArrowIndicator;
    [SerializeField] private GameObject leftSuccessCheckmark;

    [Header("Screw 2 (Kanan) UI")]
    [SerializeField] private RectTransform rightScrewTransform;
    [SerializeField] private Image rightProgressRing;
    [SerializeField] private GameObject rightArrowIndicator;
    [SerializeField] private GameObject rightSuccessCheckmark;

    [Header("Colors (Warna Baut)")]
    [SerializeField] private Color normalColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.2f); // Kuning terang saat aktif
    [SerializeField] private Color completedColor = new Color(0.2f, 1f, 0.3f); // Hijau saat selesai

    [Header("Flashlight Auto Dimmer")]
    [Tooltip("CENTANG agar intensitas senter otomatis meredup saat minigame dimulai")]
    [SerializeField] private bool autoDimFlashlight = true;
    [Tooltip("Tingkat intensitas senter saat minigame (Default: 3.0 agar redup nyaman)")]
    [SerializeField] private float dimmedIntensity = 3.0f;

    [Header("Post-Processing Dark Dimmer (Opsional)")]
    [Tooltip("Drag objek Global Volume / MinigameDarkVolume di sini")]
    [SerializeField] private UnityEngine.Rendering.Volume minigameDarkVolume;

    [Header("Rotation Tuning (Sensasi Berat & Resistansi)")]
    [Tooltip("Total derajat putaran mouse untuk 1 baut (Default: 2160 = butuh 6 putaran melingkar penuh)")]
    [SerializeField] private float degreesNeededPerScrew = 2160f;

    [Tooltip("Faktor berat gesekan (0.1 - 1.0). Semakin kecil nilainya, semakin BERAT putarannya!")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float rotationResistance = 0.45f;

    [Tooltip("Batas maksimal kecepatan putaran per frame agar tidak bisa di-flick kencang")]
    [SerializeField] private float maxDegreesPerFrame = 12.0f;

    [Tooltip("Arah putaran baut (TRUE: Searah jarum jam / Clockwise)")]
    [SerializeField] private bool rotateClockwise = true;

    [Header("Audio SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip screwRatchetingSFX; // Suara gesekan ulir baut saat diputar
    [SerializeField] private AudioClip screwLockedSFX;     // Suara CLANK! saat baut 100% terkunci
    [SerializeField] private AudioClip allCompletedSFX;    // Suara notifikasi selesai

    // Internal State
    private bool isPlaying = false;
    private int currentScrewIndex = 0; // 0 = Baut Kiri, 1 = Baut Kanan
    private float leftRotatedDegrees = 0f;
    private float rightRotatedDegrees = 0f;
    private float lastMouseAngle = 0f;
    private bool isDragging = false;
    private Action onCompleteCallback;
    private Action onCancelCallback;

    // Flashlight Dimmer Cache
    private Light playerFlashlightLight;
    private float originalFlashlightIntensity = 25f;
    private bool isFlashlightDimmed = false;

    private void Awake()
    {
        if (_instance == null) _instance = this;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);
        else
            gameObject.SetActive(false);

        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 0f;
        }
    }

    /// <summary>
    /// Membuka Minigame Obeng 2 Baut
    /// </summary>
    public void OpenMinigame(Action onCompleted = null, Action onCancelled = null)
    {
        if (isPlaying) return;

        isPlaying = true;
        onCompleteCallback = onCompleted;
        onCancelCallback = onCancelled;
        currentScrewIndex = 0;
        leftRotatedDegrees = 0f;
        rightRotatedDegrees = 0f;
        isDragging = false;

        // 🌑 1. REDUPKAN INTENSITAS SENTER PEMAIN
        if (autoDimFlashlight)
        {
            EquipableFlashlight playerFlashlight = FindObjectOfType<EquipableFlashlight>();
            if (playerFlashlight != null && playerFlashlight.IsLightOn)
            {
                playerFlashlightLight = playerFlashlight.GetComponentInChildren<Light>(true);
                if (playerFlashlightLight != null)
                {
                    originalFlashlightIntensity = playerFlashlightLight.intensity;
                    playerFlashlightLight.intensity = dimmedIntensity;
                    isFlashlightDimmed = true;
                }
            }
        }

        // 🌑 2. AKTIFKAN VOLUME POST-PROCESSING (DARK VOLUME)
        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 1f;
        }

        // 🛑 3. KUNCI KONTROL PLAYER & BUKA KURSOR MOUSE
        GameInputLock.LockInput();

        if (minigamePanel != null) minigamePanel.SetActive(true);
        gameObject.SetActive(true);

        // Reset UI State Baut Kiri
        if (leftProgressRing != null)
        {
            leftProgressRing.fillAmount = 0f;
            leftProgressRing.color = activeColor;
        }
        if (leftSuccessCheckmark != null) leftSuccessCheckmark.SetActive(false);
        if (leftArrowIndicator != null) leftArrowIndicator.SetActive(true);

        // Reset UI State Baut Kanan
        if (rightProgressRing != null)
        {
            rightProgressRing.fillAmount = 0f;
            rightProgressRing.color = normalColor;
        }
        if (rightSuccessCheckmark != null) rightSuccessCheckmark.SetActive(false);
        if (rightArrowIndicator != null) rightArrowIndicator.SetActive(false); // Muncul setelah baut kiri selesai

        // Update Teks Petunjuk Ringkas & Bersih
        if (instructionText != null)
        {
            instructionText.text = "( Tahan Klik Kiri ) Putar Baut\n( ESC ) Batal";
        }

        UpdateProgressText();
        Debug.Log("<color=yellow>[SCREWDRIVER MINIGAME] Minigame Obeng Dimulai! Mengencangkan Baut 1 (Kiri)...</color>");
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 🚪 Tombol ESC untuk Membatalkan / Keluar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[SCREWDRIVER MINIGAME] Pemain menekan ESC untuk keluar.");
            EndMinigame(false);
            return;
        }

        HandleScrewDragging();
    }

    private void HandleScrewDragging()
    {
        RectTransform activeScrew = (currentScrewIndex == 0) ? leftScrewTransform : rightScrewTransform;
        if (activeScrew == null) return;

        Vector2 screwScreenPos = RectTransformUtility.WorldToScreenPoint(null, activeScrew.position);
        Vector2 mousePos = Input.mousePosition;
        Vector2 dir = mousePos - screwScreenPos;

        // Saat Pemain Menekan Klik Kiri (Mulai Drag)
        if (Input.GetMouseButtonDown(0))
        {
            // Cek apakah kursor mouse berada di dekat area baut aktif (~150px)
            if (dir.magnitude < 200f)
            {
                isDragging = true;
                lastMouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                PlayRatchetingSound();
            }
        }

        // Saat Pemain Melepas Klik Kiri
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            StopRatchetingSound();
        }

        // Saat Sedang Menahan Klik Kiri & Memutar Mouse Melingkar
        if (isDragging && Input.GetMouseButton(0))
        {
            if (dir.sqrMagnitude > 400f) // Jarak minimal 20px dari titik tengah
            {
                float currentMouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float deltaAngle = Mathf.DeltaAngle(lastMouseAngle, currentMouseAngle);

                // Filter arah putaran (Clockwise / Counter-Clockwise)
                bool validDirection = rotateClockwise ? (deltaAngle < 0f) : (deltaAngle > 0f);

                if (validDirection)
                {
                    // ⚙️ Terapkan Hambatan Gesekan Berat + Batasan Kecepatan Maksimal
                    float rawStep = Mathf.Abs(deltaAngle) * rotationResistance;
                    float angleStep = Mathf.Min(rawStep, maxDegreesPerFrame);

                    if (currentScrewIndex == 0)
                    {
                        leftRotatedDegrees += angleStep;
                        float progress = Mathf.Clamp01(leftRotatedDegrees / degreesNeededPerScrew);
                        
                        if (leftProgressRing != null) leftProgressRing.fillAmount = progress;
                        if (leftScrewTransform != null) leftScrewTransform.localEulerAngles = new Vector3(0, 0, -leftRotatedDegrees);

                        if (progress >= 1.0f)
                        {
                            OnScrewCompleted(0);
                        }
                    }
                    else if (currentScrewIndex == 1)
                    {
                        rightRotatedDegrees += angleStep;
                        float progress = Mathf.Clamp01(rightRotatedDegrees / degreesNeededPerScrew);

                        if (rightProgressRing != null) rightProgressRing.fillAmount = progress;
                        if (rightScrewTransform != null) rightScrewTransform.localEulerAngles = new Vector3(0, 0, -rightRotatedDegrees);

                        if (progress >= 1.0f)
                        {
                            OnScrewCompleted(1);
                        }
                    }
                }

                lastMouseAngle = currentMouseAngle;
            }
        }
    }

    private void OnScrewCompleted(int screwIndex)
    {
        isDragging = false;
        StopRatchetingSound();

        if (audioSource != null && screwLockedSFX != null)
        {
            audioSource.PlayOneShot(screwLockedSFX);
        }

        if (screwIndex == 0)
        {
            // Baut Kiri Selesai (Warna Hijau Selesai)
            if (leftProgressRing != null) leftProgressRing.color = completedColor;
            if (leftArrowIndicator != null) leftArrowIndicator.SetActive(false);
            if (leftSuccessCheckmark != null) leftSuccessCheckmark.SetActive(true);

            // Beralih ke Baut Kanan (Warna Kuning Aktif)
            currentScrewIndex = 1;
            if (rightProgressRing != null) rightProgressRing.color = activeColor;
            if (rightArrowIndicator != null) rightArrowIndicator.SetActive(true);
            UpdateProgressText();
            Debug.Log("<color=green>[SCREWDRIVER] Baut 1 (Kiri) Berhasil Terkunci! Beralih ke Baut 2 (Kanan)...</color>");
        }
        else if (screwIndex == 1)
        {
            // Baut Kanan Selesai -> SEMUA SELESAI! (Warna Hijau Selesai)
            if (rightProgressRing != null) rightProgressRing.color = completedColor;
            if (rightArrowIndicator != null) rightArrowIndicator.SetActive(false);
            if (rightSuccessCheckmark != null) rightSuccessCheckmark.SetActive(true);
            UpdateProgressText();
            Debug.Log("<color=green>[SCREWDRIVER] Baut 2 (Kanan) Selesai! Semua baut berhasil dikencangkan!</color>");

            StartCoroutine(FinishMinigameSequence());
        }
    }

    private IEnumerator FinishMinigameSequence()
    {
        if (audioSource != null && allCompletedSFX != null)
        {
            audioSource.PlayOneShot(allCompletedSFX);
        }

        yield return new WaitForSeconds(0.4f);
        EndMinigame(true);
    }

    private void EndMinigame(bool isSuccess)
    {
        isPlaying = false;
        isDragging = false;
        StopRatchetingSound();

        // 💡 1. KEMBALIKAN INTENSITAS SENTER KE SEMULA
        if (isFlashlightDimmed && playerFlashlightLight != null)
        {
            playerFlashlightLight.intensity = originalFlashlightIntensity;
            isFlashlightDimmed = false;
        }

        // 🌑 2. MATIKAN VOLUME POST-PROCESSING (DARK VOLUME)
        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 0f;
        }

        if (minigamePanel != null) minigamePanel.SetActive(false);
        gameObject.SetActive(false);

        // 🔓 3. BUKA KEMBALI KONTROL PLAYER
        GameInputLock.UnlockInput();

        if (isSuccess)
        {
            onCompleteCallback?.Invoke();
        }
        else
        {
            onCancelCallback?.Invoke();
        }
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = $"Baut Terkencangkan: {currentScrewIndex}/2";
        }
    }

    private void PlayRatchetingSound()
    {
        if (audioSource != null && screwRatchetingSFX != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = screwRatchetingSFX;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }

    private void StopRatchetingSound()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == screwRatchetingSFX)
        {
            audioSource.Stop();
        }
    }
}
