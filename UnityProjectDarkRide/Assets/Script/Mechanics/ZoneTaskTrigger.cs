using UnityEngine;

/// <summary>
/// Trigger Area Inspeksi Zona / Ruang Panel.
/// Saat pemain berjalan melewati trigger ini:
/// 1. Jika TIDAK ada masalah kelistrikan (hasWiringTask = false):
///    Langsung menyelesaikan / menambah progres tugas 'Inspeksi Seluruh Panel Listrik' di Notebook TAB.
/// 2. Jika ADA masalah kelistrikan/kabel (hasWiringTask = true):
///    Memunculkan tugas lanjutan 'Betulkan Kabel Listrik di Zona Ini' di Notebook TAB dan mengaktifkan objek JunctionBox/CablePannel.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneTaskTrigger : MonoBehaviour
{
    [Header("Task Settings (Inspeksi)")]
    [Tooltip("ID Tugas Inspeksi di TaskManager (misal: INSPECT_PANELS / CHECK_ZONE_3)")]
    [SerializeField] private string inspectionTaskId = "INSPECT_PANELS";

    [Header("Wiring Maintenance Conditional")]
    [Tooltip("CENTANG jika di zona ini ditentukan ada tugas sambung kabel yang perlu dikerjakan")]
    [SerializeField] private bool hasWiringTask = false;

    [Tooltip("ID Tugas Sambung Kabel yang akan dimunculkan di Notebook jika hasWiringTask = true")]
    [SerializeField] private string wiringTaskId = "FIX_WIRE";

    [Header("Connected 3D Object (Opsional)")]
    [Tooltip("Drag Objek CablePannel / JunctionBox yang akan diaktifkan")]
    [SerializeField] private GameObject wiringPanelObject;

    [Header("Audio & Efek (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip anomalySound; // Suara korsleting / peringatan (BZZZT!)

    private bool hasTriggered = false;

    private void Start()
    {
        // Pastikan collider diatur sebagai Trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Periksa apakah yang melintas adalah Player
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.name.ToLower().Contains("player"))
        {
            hasTriggered = true;

            // ⚠️ KASUS 1: ZONA BUTUH PERBAIKAN KABEL
            if (hasWiringTask)
            {
                Debug.Log($"<color=yellow>[ZONE TRIGGER] Pemain memasuki zona kabel bermasalah! Membuka tugas: {wiringTaskId}</color>");

                if (TaskManager.Instance != null && !string.IsNullOrEmpty(wiringTaskId))
                {
                    TaskManager.Instance.RevealSubTask(wiringTaskId);
                }

                if (wiringPanelObject != null)
                {
                    wiringPanelObject.SetActive(true);
                }

                if (audioSource != null && anomalySound != null)
                {
                    audioSource.PlayOneShot(anomalySound);
                }
            }
            // 🟢 KASUS 2: ZONA AMAN / INSPEKSI BERHASIL
            else
            {
                Debug.Log($"<color=green>[ZONE TRIGGER] Inspeksi Zona Berhasil! Progres tugas '{inspectionTaskId}' bertambah [✓].</color>");

                if (TaskManager.Instance != null && !string.IsNullOrEmpty(inspectionTaskId))
                {
                    TaskManager.Instance.CompleteTask(inspectionTaskId);
                }
            }
        }
    }
}
