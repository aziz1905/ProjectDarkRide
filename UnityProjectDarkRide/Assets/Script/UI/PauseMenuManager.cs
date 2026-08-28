using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clean Pause Menu Manager.
/// Bertanggung jawab mengontrol fungsi Pause (ESC), pembekuan game (Time.timeScale),
/// penguncian input, pengaktifan DarkVolume, dan pemicuan tombol Resume buatan desainer.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    private static PauseMenuManager _instance;
    public static PauseMenuManager Instance => _instance;

    [Header("Keybind Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Custom UI References (Drag Desain Kamu ke Sini)")]
    [Tooltip("Drag CanvasGroup dari Canvas / Panel Pause buatanmu")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    [Tooltip("Drag Tombol Resume buatanmu ke slot ini")]
    [SerializeField] private Button resumeButton;

    [Header("Visual Effects (Opsional)")]
    [Tooltip("Drag Objek Dark Volume / Global Volume (Post Processing) untuk efek layar redup/gelap saat Pause")]
    [SerializeField] private GameObject darkVolumeObject;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    private void Start()
    {
        SetupResumeButton();

        // Pastikan game dimulai dalam keadaan Unpaused
        ResumeGame();
    }

    private void SetupResumeButton()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
    }

    private void Update()
    {
        // Tekan ESC untuk Toggle Pause / Resume
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                // Jangan izinkan pause jika layar sedang pekat hitam (misal Death Fade)
                if (ScreenFader.Instance != null && ScreenFader.Instance.IsFadingOrBlack) return;

                PauseGame();
            }
        }
    }

    /// <summary>
    /// Pause Game: Hentikan Waktu & Tampilkan UI Pause
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Bekukan waktu game (fisika & coroutine berbasis Time.deltaTime)

        // Reset delta input mouse & aksis agar tidak ada lonjakan hentakan
        Input.ResetInputAxes();

        // Pastikan EventSystem ada di scene agar Tombol UI bisa diklik
        EnsureEventSystem();

        GameInputLock.LockInput(); // Buka kursor mouse & bekukan WASD/kamera

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 1f;
            pauseCanvasGroup.blocksRaycasts = true;
            pauseCanvasGroup.interactable = true;
        }

        if (darkVolumeObject != null)
        {
            darkVolumeObject.SetActive(true);
        }
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    /// <summary>
    /// Resume Game: Kembalikan Waktu & Sembunyikan UI Pause
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Kembalikan waktu game normal

        // Reset delta input mouse & aksis agar kamera/pergerakan tidak kaget saat resume
        Input.ResetInputAxes();

        GameInputLock.UnlockInput(); // Kunci kembali kursor ke tengah layar

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.blocksRaycasts = false;
            pauseCanvasGroup.interactable = false;
        }

        if (darkVolumeObject != null)
        {
            darkVolumeObject.SetActive(false);
        }
    }
}
