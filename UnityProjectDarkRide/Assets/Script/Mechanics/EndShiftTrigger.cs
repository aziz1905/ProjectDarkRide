using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Trigger Akhir Shift / Transisi Hari (End Shift Transition Trigger).
/// Dipasang di area titik awal (pos jaga/stasiun awal/gerbang).
/// 
/// ALUR KERJA:
/// 1. Jika pemain masuk ke area dan TaskManager BELUM selesai:
///    -> Memunculkan pesan peringatan di UI: "Tugas belum selesai! Cek Notebook [TAB]".
///    
/// 2. Jika pemain masuk ke area dan TaskManager SUDAH SELESAI SEMUA [✓]:
///    -> Kunci kontrol pemain (WASD & Kamera).
///    -> Bunyikan SFX Walkie Talkie (Radio static + suara dialog supervisor).
///    -> Tampilkan teks dialog / radio transmission di layar.
///    -> Fade out ke layar hitam pekat.
///    -> Muat scene berikutnya (misal Day 2).
/// </summary>
[RequireComponent(typeof(Collider))]
public class EndShiftTrigger : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("Nama scene berikutnya yang akan dimuat (misal: 'Day 2')")]
    [SerializeField] private string nextDaySceneName = "Day 2";

    [Tooltip("Lama waktu layar hitam sebelum scene baru dimuat (detik)")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Walkie Talkie Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("SFX suara kresek radio saat walkie talkie menyala")]
    [SerializeField] private AudioClip radioChirpSFX;
    [Tooltip("Suara rekaman orang / supervisor bicara dari walkie talkie")]
    [SerializeField] private AudioClip walkieVoiceClip;

    [Header("Walkie Talkie Subtitle / Dialog UI (Opsional)")]
    [Tooltip("Teks UI untuk menampilkan dialog radio (bisa menggunakan TextMeshPro yang ada di Canvas)")]
    [SerializeField] private TextMeshProUGUI radioSubtitleText;
    [TextArea(2, 5)]
    [SerializeField] private string radioDialogText = "\"Shift malammu selesai. Kembalilah beristirahat, besok kita lanjutkan.\"";
    [SerializeField] private float dialogDuration = 4.0f;

    private bool isTransitioning = false;

    private void Awake()
    {
        // Pastikan collider adalah trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (radioSubtitleText != null)
        {
            radioSubtitleText.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;

        // Cek apakah yang masuk adalah Player
        bool isPlayer = other.CompareTag("Player") ||
                        other.GetComponent<CharacterController>() != null ||
                        other.name.ToLower().Contains("player");

        if (!isPlayer) return;

        // 1. PERIKSA APAKAH SELURUH TUGAS SUDAH SELESAI
        bool allCompleted = false;
        if (TaskManager.Instance != null)
        {
            allCompleted = TaskManager.Instance.IsAllTasksCompleted();
        }

        if (allCompleted)
        {
            // 🟢 TUGAS SELESAI -> MULAI TRANSISI AKHIR SHIFT (WALKIE TALKIE & PINDAH HARI)
            isTransitioning = true;
            StartCoroutine(EndShiftSequenceRoutine());
        }
        // Jika tugas belum selesai: diamkan saja, tidak ada teks apapun (biarkan player mencari sendiri)
    }

    private IEnumerator EndShiftSequenceRoutine()
    {
        Debug.Log("<color=green>[END SHIFT] Seluruh tugas selesai! Memulai transisi ke " + nextDaySceneName + "</color>");

        // 1. Kunci kontrol pemain agar tenang mendengarkan walkie talkie
        GameInputLock.LockInput();

        // 2. Bunyikan suara radio chirp
        if (audioSource != null && radioChirpSFX != null)
        {
            audioSource.PlayOneShot(radioChirpSFX);
            yield return new WaitForSeconds(0.4f);
        }

        // 3. Bunyikan suara walkie-talkie orang bicara
        if (audioSource != null && walkieVoiceClip != null)
        {
            audioSource.PlayOneShot(walkieVoiceClip);
        }

        // 4. Tampilkan teks radio di layar
        if (radioSubtitleText != null)
        {
            radioSubtitleText.gameObject.SetActive(true);
            radioSubtitleText.text = radioDialogText;
        }

        // Hitung durasi dialog berdasarkan audio atau durasi fallback
        float waitTime = dialogDuration;
        if (walkieVoiceClip != null && walkieVoiceClip.length > waitTime)
        {
            waitTime = walkieVoiceClip.length;
        }

        yield return new WaitForSeconds(waitTime);

        // 5. Fade Layar ke Hitam Pekat
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToBlack(fadeDuration);
            yield return new WaitForSeconds(fadeDuration + 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // 6. Lepas kunci input & Muat Scene Berikutnya
        GameInputLock.UnlockInput();

        if (!string.IsNullOrEmpty(nextDaySceneName))
        {
            SceneManager.LoadScene(nextDaySceneName);
        }
    }
}
