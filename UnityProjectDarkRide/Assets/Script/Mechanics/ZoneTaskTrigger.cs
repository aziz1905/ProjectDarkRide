using UnityEngine;

/// <summary>
/// Trigger Area di Lorong / Pintu Masuk Zona.
/// Saat pemain berjalan melangkah melewati trigger ini:
/// 1. Jika TIDAK ada kerusakan (hasBrokenCable = false): Langsung mencentang [✓] tugas 'Periksa & Pastikan Panel Aman'.
/// 2. Jika ADA kerusakan (hasBrokenCable = true): Memicu sub-task dinamis 'Perbaiki Kabel Putus' di Notebook TAB!
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneTaskTrigger : MonoBehaviour
{
    [Header("Task Settings")]
    [Tooltip("ID Tugas Utama di TaskManager (misal: CHECK_PANEL_ZONE_1)")]
    [SerializeField] private string zoneCheckTaskId = "CHECK_PANEL_ZONE_1";

    [Header("Broken Cable Conditional Trigger")]
    [Tooltip("CENTANG jika di zona ini ditentukan ada kabel yang rusak/putus")]
    [SerializeField] private bool hasBrokenCable = false;

    [Tooltip("ID Sub-Task kabel rusak yang akan dimunculkan jika hasBrokenCable = true")]
    [SerializeField] private string brokenCableSubTaskId = "FIX_CABLE_ZONE_1";

    [Header("Visual & Audio Feedback (Opsional)")]
    [SerializeField] private GameObject brokenCableVisual;  // 3D Objek kabel konslet
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sparkSound;          // Suara percikan konslet (BZZZT!)

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Periksa apakah yang menginjak trigger adalah Player
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.name.ToLower().Contains("player"))
        {
            hasTriggered = true;

            if (hasBrokenCable)
            {
                // ⚠️ KASUS 1: ADA KABEL RUSAK -> Picu Sub-Task Kabel Rusak di Notebook TAB!
                Debug.Log($"<color=yellow>[ZONE TRIGGER] Pemain memasuki zona dengan kabel rusak! Memicu sub-task: {brokenCableSubTaskId}</color>");
                
                if (TaskManager.Instance != null && !string.IsNullOrEmpty(brokenCableSubTaskId))
                {
                    TaskManager.Instance.RevealSubTask(brokenCableSubTaskId);
                }

                if (brokenCableVisual != null)
                {
                    brokenCableVisual.SetActive(true);
                }

                if (audioSource != null && sparkSound != null)
                {
                    audioSource.PlayOneShot(sparkSound);
                }
            }
            else
            {
                // 🟢 KASUS 2: ZONA AMAN (TIDAK ADA KERUSAKAN) -> Langsung Centang [✓] Tugas Utama!
                Debug.Log($"<color=green>[ZONE TRIGGER] Pemain memasuki zona aman. Tugas '{zoneCheckTaskId}' SELESAI [✓]!</color>");

                if (TaskManager.Instance != null && !string.IsNullOrEmpty(zoneCheckTaskId))
                {
                    TaskManager.Instance.CompleteTask(zoneCheckTaskId);
                }
            }
        }
    }
}
