using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller sederhana dan andal untuk Main Menu / Lobby.
/// Mengatur pembukaan kursor mouse, tombol Play untuk masuk ke Day 1,
/// tombol Quit untuk keluar dari game, dan opsi transisi fade halus.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Nama scene gameplay yang akan dimuat saat tombol Play diklik.")]
    [SerializeField] private string playSceneName = "Day 1";

    [Header("Button References (Opsional, bisa juga via OnClick di Inspector)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Transition Settings (Opsional)")]
    [Tooltip("CanvasGroup untuk efek fade out saat tombol Play diklik.")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isLoadingScene = false;

    private void Awake()
    {
        // Pastikan kamera selalu berlatar belakang HITAM PEKAT, bukan Skybox langit
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }
    }

    private void Start()
    {
        // Reset status kunci input player jika terbawa dari scene sebelumnya
        GameInputLock.ForceUnlockAll();

        // PENTING: Buka kursor mouse untuk menu SETELAH ForceUnlockAll agar kursor tidak terkunci ke tengah layar!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hubungkan event tombol otomatis jika diisi di Inspector
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(PlayGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void Update()
    {
        // Pastikan kursor mouse selalu bebas bergerak di Main Menu
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Memulai game dengan memuat scene Day 1
    /// </summary>
    public void PlayGame()
    {
        Debug.Log($"[MAIN MENU] Tombol PLAY diklik! Memuat scene '{playSceneName}'...");
        if (isLoadingScene) return;
        isLoadingScene = true;

        if (menuCanvasGroup != null && fadeDuration > 0f)
        {
            StartCoroutine(FadeAndLoadRoutine(playSceneName));
        }
        else
        {
            SceneManager.LoadScene(playSceneName);
        }
    }

    /// <summary>
    /// Keluar dari game (berfungsi saat sudah di-build maupun di Play Mode Unity Editor)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MAIN MENU] Tombol QUIT diklik! Menutup game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        float timer = 0f;
        float startAlpha = menuCanvasGroup.alpha;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }

        menuCanvasGroup.alpha = 0f;
        SceneManager.LoadScene(sceneName);
    }
}
