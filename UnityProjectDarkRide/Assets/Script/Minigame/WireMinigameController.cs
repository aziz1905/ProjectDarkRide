using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;

/// <summary>
/// Controller Minigame Sambung Kabel (Fix Wiring) ala Among Us.
/// 1. Dilengkapi efek MinigameDarkVolume (Post-Processing) dan Auto Dim Flashlight agar fokus & sinematik.
/// 2. Mengacak posisi 4 slot kabel warna di sisi kanan (Merah, Biru, Kuning, Hijau).
/// 3. Menarik garis kabel dinamis mengikuti kursor mouse secara realtime.
/// 4. Mengunci kabel saat tersambung ke warna yang cocok.
/// 5. Menyelesaikan tugas kelistrikan dan membuka kembali kontrol player saat ke-4 kabel terpasang.
/// </summary>
public class WireMinigameController : MonoBehaviour
{
    private static WireMinigameController _instance;
    public static WireMinigameController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WireMinigameController>(true);
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class WireColorData
    {
        public string colorName;
        public Color colorValue;
    }

    [Header("UI Panel & Nodes")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Flashlight Auto Dimmer & Dark Volume")]
    [Tooltip("CENTANG agar intensitas senter otomatis meredup saat minigame kabel dimulai")]
    [SerializeField] private bool autoDimFlashlight = true;
    [Tooltip("Tingkat intensitas senter saat minigame (Default: 3.0 agar redup nyaman)")]
    [SerializeField] private float dimmedIntensity = 3.0f;
    [Tooltip("Drag objek Global Volume (MinigameDarkVolume) untuk meredupkan dunia 3D di belakang UI")]
    [SerializeField] private Volume minigameDarkVolume;

    [Header("Wire Color Definitions")]
    [SerializeField] private List<WireColorData> wireColors = new List<WireColorData>()
    {
        new WireColorData { colorName = "Red", colorValue = new Color(0.95f, 0.2f, 0.2f) },
        new WireColorData { colorName = "Blue", colorValue = new Color(0.2f, 0.5f, 0.95f) },
        new WireColorData { colorName = "Yellow", colorValue = new Color(0.95f, 0.85f, 0.2f) },
        new WireColorData { colorName = "Green", colorValue = new Color(0.2f, 0.85f, 0.3f) }
    };

    [Header("Left Wire Nodes (Titik Awal Kabel Kiri)")]
    [SerializeField] private List<RectTransform> leftNodes; // 4 Node di kiri

    [Header("Right Wire Slots (Slot Tujuan Kanan)")]
    [SerializeField] private List<RectTransform> rightNodes; // 4 Node di kanan

    [Header("Wire Line Container")]
    [Tooltip("Prefab Image atau RectTransform untuk menggambar garis kabel")]
    [SerializeField] private RectTransform wireContainer;
    [SerializeField] private Sprite wireLineSprite;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip connectSound;   // SFX saat kabel berhasil tersambung
    [SerializeField] private AudioClip mismatchSound;  // SFX saat salah sambung
    [SerializeField] private AudioClip completedSound; // SFX saat semua kabel selesai

    // Internal State
    private bool isPlaying = false;
    private int connectedCount = 0;
    private Action onCompleteCallback;
    private Action onFailedCallback;

    // Flashlight Dimmer Cache
    private Light playerFlashlightLight;
    private float originalFlashlightIntensity = 25f;
    private bool isFlashlightDimmed = false;

    // Active Drag State
    private int activeDraggingIndex = -1;
    private Image activeDraggingWireImage;
    private RectTransform activeDraggingWireRect;

    // Completed Connections
    private bool[] isLeftConnected = new bool[4];
    private int[] rightSlotColorIndices = new int[4];
    private Image[] completedWireImages = new Image[4];

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
    /// Memulai Minigame Sambung Kabel
    /// </summary>
    public void StartMinigame(Action onComplete, Action onFailed = null)
    {
        onCompleteCallback = onComplete;
        onFailedCallback = onFailed;
        isPlaying = true;
        connectedCount = 0;

        // 🔦 1. OTOMATIS REDUPKAN INTENSITAS SENTER
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

        // 🌑 2. AKTIFKAN VOLUME POST-PROCESSING (GELAPKAN BACKGROUND)
        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 1f;
        }

        // 🛑 3. Kunci input universal (WASD & Kamera) dan buka kursor mouse
        GameInputLock.LockInput();

        if (minigamePanel != null) minigamePanel.SetActive(true);
        gameObject.SetActive(true);

        if (instructionText != null)
        {
            instructionText.text = "• Tarik kabel ke warna yang cocok\n• [ESC] Keluar";
        }

        SetupWires();
        Debug.Log("[WIRE MINIGAME] Minigame Sambung Kabel Dimulai (Layar Redup Sinematik)!");
    }

    private void SetupWires()
    {
        // 1. Bersihkan kabel lama
        for (int i = 0; i < 4; i++)
        {
            isLeftConnected[i] = false;
            if (completedWireImages[i] != null)
            {
                Destroy(completedWireImages[i].gameObject);
                completedWireImages[i] = null;
            }
        }

        if (activeDraggingWireImage != null)
        {
            Destroy(activeDraggingWireImage.gameObject);
            activeDraggingWireImage = null;
        }
        activeDraggingIndex = -1;

        // 2. Warnai Node Kiri (Urutan Tetap: Merah, Biru, Kuning, Hijau)
        for (int i = 0; i < leftNodes.Count && i < wireColors.Count; i++)
        {
            if (leftNodes[i] != null)
            {
                Image nodeImg = leftNodes[i].GetComponent<Image>();
                if (nodeImg != null) nodeImg.color = wireColors[i].colorValue;
            }
        }

        // 3. Acak Posisi Warna di Node Kanan (Shuffle Slots)
        List<int> shuffledIndices = new List<int> { 0, 1, 2, 3 };
        for (int i = shuffledIndices.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            int temp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[rnd];
            shuffledIndices[rnd] = temp;
        }

        for (int i = 0; i < rightNodes.Count && i < shuffledIndices.Count; i++)
        {
            if (rightNodes[i] != null)
            {
                rightSlotColorIndices[i] = shuffledIndices[i];
                Image rightImg = rightNodes[i].GetComponent<Image>();
                if (rightImg != null) rightImg.color = wireColors[shuffledIndices[i]].colorValue;
            }
        }

        UpdateStatusText();
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 🚪 1. TOMBOL ESC UNTUK KELUAR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[WIRE CANCELLED] Pemain membatalkan minigame.");
            EndMinigame(false);
            return;
        }

        // 🖱️ 2. KLIK KIRI MOUSE UNTUK MEMULAI TARIK KABEL
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDraggingWire();
        }

        // ➰ 3. SAAT MEN-DRAG KABEL: Gambar garis ke kursor mouse
        if (activeDraggingIndex != -1 && activeDraggingWireRect != null)
        {
            Vector2 localMousePos;
            RectTransform targetContainer = wireContainer != null ? wireContainer : panelRect;
            if (targetContainer == null && minigamePanel != null) targetContainer = minigamePanel.GetComponent<RectTransform>();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetContainer,
                Input.mousePosition,
                null,
                out localMousePos
            );

            Vector2 startPos = leftNodes[activeDraggingIndex].anchoredPosition;
            DrawWireLine(activeDraggingWireRect, startPos, localMousePos);
        }

        // 🖱️ 4. LEPAS KLIK KIRI MOUSE UNTUK MENYAMBUNG KABEL
        if (Input.GetMouseButtonUp(0) && activeDraggingIndex != -1)
        {
            TryConnectWire();
        }
    }

    private void TryStartDraggingWire()
    {
        for (int i = 0; i < leftNodes.Count; i++)
        {
            if (isLeftConnected[i]) continue; // Sudah tersambung

            if (leftNodes[i] != null && RectTransformUtility.RectangleContainsScreenPoint(leftNodes[i], Input.mousePosition))
            {
                activeDraggingIndex = i;

                // Buat Objek Garis Kabel Dinamis
                GameObject lineObj = new GameObject($"DraggingWire_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform targetContainer = wireContainer != null ? wireContainer : panelRect;
                if (targetContainer == null && minigamePanel != null) targetContainer = minigamePanel.GetComponent<RectTransform>();

                lineObj.transform.SetParent(targetContainer, false);

                activeDraggingWireRect = lineObj.GetComponent<RectTransform>();
                activeDraggingWireImage = lineObj.GetComponent<Image>();
                activeDraggingWireImage.color = wireColors[i].colorValue;
                activeDraggingWireImage.raycastTarget = false;

                Debug.Log($"[WIRE DRAG] Menarik kabel: {wireColors[i].colorName}");
                return;
            }
        }
    }

    private void TryConnectWire()
    {
        int targetRightIndex = -1;

        // Cek apakah kursor dilepas di atas salah satu slot kanan
        for (int i = 0; i < rightNodes.Count; i++)
        {
            if (rightNodes[i] != null && RectTransformUtility.RectangleContainsScreenPoint(rightNodes[i], Input.mousePosition))
            {
                targetRightIndex = i;
                break;
            }
        }

        // Cek Kesesuaian Warna
        if (targetRightIndex != -1 && rightSlotColorIndices[targetRightIndex] == activeDraggingIndex)
        {
            // 🟢 SAMBUNGAN BENAR!
            isLeftConnected[activeDraggingIndex] = true;
            completedWireImages[activeDraggingIndex] = activeDraggingWireImage;

            // Kunci posisi garis tepat menempel dari Node Kiri ke Node Kanan
            Vector2 startPos = leftNodes[activeDraggingIndex].anchoredPosition;
            Vector2 endPos = rightNodes[targetRightIndex].anchoredPosition;
            DrawWireLine(activeDraggingWireRect, startPos, endPos);

            connectedCount++;
            UpdateStatusText();

            if (audioSource != null && connectSound != null) audioSource.PlayOneShot(connectSound);
            Debug.Log($"[WIRE CONNECTED] Kabel {wireColors[activeDraggingIndex].colorName} berhasil tersambung! ({connectedCount}/4)");

            activeDraggingIndex = -1;
            activeDraggingWireImage = null;
            activeDraggingWireRect = null;

            // Cek Menang (Semua 4 kabel tersambung)
            if (connectedCount >= 4)
            {
                StartCoroutine(CompleteWithDelay());
            }
        }
        else
        {
            // 🔴 SALAH SAMBUNG / LEPAS DI RUANG KOSONG
            if (audioSource != null && mismatchSound != null) audioSource.PlayOneShot(mismatchSound);
            Debug.LogWarning("[WIRE MISMATCH] Kabel tidak cocok atau terlepas.");

            if (activeDraggingWireImage != null)
            {
                Destroy(activeDraggingWireImage.gameObject);
                activeDraggingWireImage = null;
                activeDraggingWireRect = null;
            }
            activeDraggingIndex = -1;
        }
    }

    private void DrawWireLine(RectTransform lineRect, Vector2 startPoint, Vector2 endPoint)
    {
        Vector2 direction = endPoint - startPoint;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.sizeDelta = new Vector2(distance, 18f); // Tebal kabel 18 pixel (proporsional dengan node 60x60)
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = startPoint;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = $"<b>KABEL TERSAMBUNG: {connectedCount} / 4</b>";
        }
    }

    private IEnumerator CompleteWithDelay()
    {
        if (audioSource != null && completedSound != null) audioSource.PlayOneShot(completedSound);
        if (statusText != null) statusText.text = "<color=green><b>SEMUA KABEL TERSAMBUNG</b></color>";

        yield return new WaitForSeconds(0.8f);
        EndMinigame(true);
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

        // ☀️ 2. MATIKAN VOLUME POST-PROCESSING (KEMBALIKAN CAHAYA DUNIA)
        if (minigameDarkVolume != null)
        {
            minigameDarkVolume.weight = 0f;
        }

        // 🔓 3. Buka kunci input universal (Pulihkan WASD & Kamera)
        GameInputLock.UnlockInput();

        if (isSuccess)
        {
            Debug.Log("[WIRE MINIGAME WON] Minigame Sambung Kabel Selesai 100%!");
            onCompleteCallback?.Invoke();
        }
        else
        {
            onFailedCallback?.Invoke();
        }
    }

    public bool IsPlaying => isPlaying;
}
