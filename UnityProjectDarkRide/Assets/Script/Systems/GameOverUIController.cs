using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pengelola Tampilan Layar Game Over (Surat Pemutusan Hubungan Kerja).
/// 1. Atas Tengah: Judul Besar "GAME OVER".
/// 2. Tengah: Dokumen Surat Pemecatan Berisi Alasan Insiden & Daftar Tugas Yang Terbengkalai.
/// 3. Bawah: Tombol "Mulai Ulang Shift" & "Kembali ke Menu Utama".
/// Auto-Sort Last Sibling: Memastikan Panel Game Over selalu berada di posisi paling depan Canvas.
/// </summary>
public class GameOverUIController : MonoBehaviour
{
    [Header("UI Canvas References")]
    [Tooltip("Drag Objek Panel Utama Game Over")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Text Judul Utama Game Over di Atas Tengah")]
    [SerializeField] private TextMeshProUGUI titleTextUI;

    [Tooltip("Text Isi Surat Pemecatan di Tengah Layar")]
    [SerializeField] private TextMeshProUGUI reasonTextUI;

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
    /// Memunculkan Layar Game Over Surat Pemecatan Berdasarkan Insiden & Tugas Yang Belum Selesai
    /// </summary>
    public void ShowGameOverScreen(string customReason = "")
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        transform.SetAsLastSibling();

        if (titleTextUI != null)
        {
            titleTextUI.text = "<color=red><b>GAME OVER</b></color>";
        }

        // 🎯 OTOMATIS MENGUMPULKAN DAFTAR TUGAS DI NOTEBOOK TAB YANG BELUM SELESAI
        StringBuilder taskFailuresSB = new StringBuilder();

        // 1. Jika ada alasan khusus insiden (misal: Diserang Animatronik / Tersetrum)
        if (!string.IsNullOrEmpty(customReason))
        {
            taskFailuresSB.AppendLine($"• <color=#B22222><b>Insiden:</b> {customReason}</color>");
        }

        // 2. Kumpulkan daftar tugas di TaskManager yang belum selesai
        if (TaskManager.Instance != null && TaskManager.Instance.TaskList != null)
        {
            int uncompletedCount = 0;
            foreach (var task in TaskManager.Instance.TaskList)
            {
                if (task.isSubTask && !task.isRevealed) continue;

                if (!task.isCompleted)
                {
                    uncompletedCount++;
                    string taskName = !string.IsNullOrEmpty(task.title) ? task.title : task.id;
                    if (!string.IsNullOrEmpty(taskName))
                    {
                        taskFailuresSB.AppendLine($"• <color=#B22222>{taskName}</color>");
                    }
                }
            }

            if (uncompletedCount == 0 && string.IsNullOrEmpty(customReason))
            {
                taskFailuresSB.AppendLine("• <color=#B22222>Kelalaian Protokol Insiden Kelistrikan & Anomali Wahana</color>");
            }
        }
        else if (string.IsNullOrEmpty(customReason))
        {
            taskFailuresSB.AppendLine("• <color=#B22222>Kelalaian Tugas Perawatan Shift Malam</color>");
        }

        if (reasonTextUI != null)
        {
            reasonTextUI.text = 
                "<b><u>SURAT PEMBERHENTIAN KERJA (PEMECATAN)</u></b>\n\n" +
                "<b>Kepada:</b> Teknisi Maintenance Shift Malam\n" +
                "<b>Perihal:</b> Pemutusan Hubungan Kerja Instan\n\n" +
                "Dengan ini manajemen mengumumkan bahwa kontrak kerja Anda resmi <b>DIBATALKAN SEKETIKA</b>.\n\n" +
                "<b>Alasan Pelanggaran Resmi & Tugas Terbengkalai:</b>\n" +
                $"{taskFailuresSB}\n" +
                "Manajemen menilai Anda tidak kompeten dan telah melampaui batas akumulasi 5 sanksi insiden keselamatan. " +
                "Silakan kembalikan ID Card & kunci wahana, serta segera tinggalkan area fasilitas.";
        }

        if (gameOverSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSFX);
        }

        // Buka kunci input & kursor mouse agar pemain bisa menekan tombol UI Game Over
        GameInputLock.LockInput();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Dipanggil saat pemain menekan tombol "Mulai Ulang Shift" di UI Game Over
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

        // Reload Scene Shift Aktif saat ini
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Dipanggil saat pemain menekan tombol "Kembali ke Menu Utama" di UI Game Over
    /// </summary>
    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        GameInputLock.UnlockInput();

        // Load scene Menu Utama (Index 0)
        SceneManager.LoadScene(0);
    }
}
