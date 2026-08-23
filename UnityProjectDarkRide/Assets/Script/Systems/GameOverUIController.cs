using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pengelola Tampilan Layar Game Over (Surat Pemutusan Hubungan Kerja).
/// Menampilkan alasan sanksi laporan & menyediakan tombol "Coba Lagi (Retry Shift)" dan "Menu Utama".
/// </summary>
public class GameOverUIController : MonoBehaviour
{
    [Header("UI Canvas References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI reasonTextUI;
    [SerializeField] private TextMeshProUGUI retryCountInfoText;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gameOverSFX;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Memunculkan Layar Game Over Surat Pemutusan Hubungan Kerja
    /// </summary>
    public void ShowGameOverScreen(string reasonMessage)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (reasonTextUI != null)
        {
            reasonTextUI.text = $"<b>ALASAN SANKSI:</b>\n{reasonMessage}";
        }

        if (retryCountInfoText != null)
        {
            retryCountInfoText.text = "Batas Sanksi Maksimal (5 / 5) Terlampaui.\nKontrak Kerja Anda Dibatalkan.";
        }

        if (gameOverSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSFX);
        }

        // Buka kunci kursor mouse agar pemain bisa menekan tombol UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Dipanggil saat pemain menekan tombol "Coba Lagi (Retry Shift)" di UI Game Over
    /// </summary>
    public void OnClickRetryShift()
    {
        Time.timeScale = 1f;

        if (SanctionAndRetryManager.Instance != null)
        {
            SanctionAndRetryManager.Instance.ResetSanctions();
        }

        if (ShiftTimerManager.Instance != null)
        {
            ShiftTimerManager.Instance.ResetShiftTimer();
        }

        GameInputLock.UnlockInput();

        // Reload Scene Aktif saat ini untuk memulai ulang shift
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Dipanggil saat pemain menekan tombol "Menu Utama" di UI Game Over
    /// </summary>
    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        GameInputLock.UnlockInput();

        // Load scene Menu Utama (Index 0)
        SceneManager.LoadScene(0);
    }
}
