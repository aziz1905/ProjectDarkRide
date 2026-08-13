using System.Collections;
using UnityEngine;

/// <summary>
/// Script Interaksi Menarik Gurney (Ranjang Pasien RS) Keluar dari Rel.
/// Versi Bersih & Simpel:
/// - Kamera & Player TETAP BEBAS / TIDAK DIKUNCI.
/// - Hanya objek Gurney yang meluncur mulus dari posisi rel (GurneyJatuh) ke trotoar (GurneyAwal).
/// - Selesai ditarik -> Jalur rel bersih & Task Manager tercoret [X]!
/// </summary>
public class GurneyPullInteractable : MonoBehaviour, IInteractable
{
    [Header("Task Manager Settings")]
    [Tooltip("ID Tugas di Notebook TAB (misal: PULL_GURNEY)")]
    [SerializeField] private string taskId = "PULL_GURNEY";
    [SerializeField] private string promptText = "Tahan [E] Tarik Gurney Keluar Rel";

    [Header("Pull Motion Settings")]
    [Tooltip("Durasi tarikan gurney (detik)")]
    [SerializeField] private float pullDuration = 3.0f; 

    [Header("Pull Target Position")]
    [Tooltip("Drag Cube GurneyAwal (posisi target di trotoar) ke kolom ini")]
    [SerializeField] private Transform targetPullSpot;

    [Tooltip("Jika targetPullSpot kosong, gunakan offset pergeseran otomatis ini (X, Y, Z)")]
    [SerializeField] private Vector3 fallbackPullOffset = new Vector3(2.5f, 0.2f, 0.5f);

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pullWheelSound; // Suara roda besi berderit berat di rel

    // Internal State
    private bool isPulled = false;
    private bool isAnimating = false;

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (isPulled) return "Gurney RS [SUDAH DITEPIKAN]";
        if (isAnimating) return "[SEDANG MENARIK GURNEY...]";
        return promptText;
    }

    public float HoldDuration => isPulled ? 0f : pullDuration;

    public void OnInteract()
    {
        if (isPulled || isAnimating) return;

        StartCoroutine(PullGurneyRoutine());
    }

    private IEnumerator PullGurneyRoutine()
    {
        isAnimating = true;

        // Mainkan Suara Roda Berderit jika ada
        if (audioSource != null && pullWheelSound != null)
        {
            audioSource.clip = pullWheelSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Tentukan Posisi & Rotasi Awal/Akhir
        Vector3 gurneyStartPos = transform.position;
        Vector3 gurneyEndPos = targetPullSpot != null ? targetPullSpot.position : gurneyStartPos + transform.TransformDirection(fallbackPullOffset);
        Quaternion gurneyStartRot = transform.rotation;
        Quaternion gurneyEndRot = targetPullSpot != null ? targetPullSpot.rotation : gurneyStartRot;

        float elapsed = 0f;

        // PROSES MENARIK GURNEY SECARA MULUS DARI REL KE TROTOAR ATAS
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pullDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Geser Gurney & Rotasi secara halus
            transform.position = Vector3.Lerp(gurneyStartPos, gurneyEndPos, smoothT);
            transform.rotation = Quaternion.Slerp(gurneyStartRot, gurneyEndRot, smoothT);

            yield return null;
        }

        // Pastikan Gurney tepat 100% di posisi target GurneyAwal
        transform.position = gurneyEndPos;
        transform.rotation = gurneyEndRot;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isPulled = true;
        isAnimating = false;
        Debug.Log($"<color=green>[GURNEY PULLED] Gurney berhasil berpindah dari rel ke trotoar atas!</color>");

        // LAPORKAN KE TASK MANAGER NOTEBOOK TAB
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null)
            {
                taskMgr.CompleteTask(taskId);
            }
        }
    }
}
