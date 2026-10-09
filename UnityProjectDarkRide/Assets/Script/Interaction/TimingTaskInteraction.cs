using UnityEngine;

/// <summary>
/// Script Interaksi Minigame Timing (misal: Rel Rusak, Lukisan Jatuh, dll).
/// - Memerlukan alat tertentu di tangan (misal: Hammer / Palu).
/// - Membuka Minigame Timing saat pemain menekan [E].
/// - Saat selesai: Mematikan objek rusak/jatuh dan mengaktifkan objek rapi (seperti sistem Screwdriver).
/// - Tugas di TaskManager langsung tercoret [X].
/// </summary>
public class TimingTaskInteractable : MonoBehaviour, IInteractable
{
    [Header("Task Settings")]
    [Tooltip("Nama alat di sabuk yang dibutuhkan (misal: Hammer)")]
    [SerializeField] private string requiredItemName = "Hammer";

    [Tooltip("Jumlah ketukan timing tepat yang dibutuhkan")]
    [SerializeField] private int requiredHits = 3;

    [Tooltip("ID Tugas di Notebook TAB (misal: FIX_RAIL atau FIX_FALLEN_PAINTING)")]
    [SerializeField] private string taskId = "FIX_RAIL";

    [Header("Object Toggle Settings")]
    [Tooltip("Objek rusak/jatuh di lantai yang akan DIMATIKAN saat selesai (Jika kosong, otomatis objek ini sendiri)")]
    [SerializeField] private GameObject objectToDeactivate;

    [Tooltip("Objek rapi yang akan DIAKTIFKAN saat selesai (misal: lukisan rapi terpasang di dinding)")]
    [SerializeField] private GameObject objectToActivate;

    // Internal State Guard
    private bool isCompleted = false;

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (isCompleted) return "[SELESAI]";

        if (TimingMinigameController.Instance != null && TimingMinigameController.Instance.IsPlaying)
            return ""; // Sembunyikan prompt saat minigame aktif

        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        return hasItem ? $"Tekan [E] Ketuk {requiredItemName} ({requiredHits}x Hit)" : $"[BUTUH ALAT] Membutuhkan '{requiredItemName}' di Tangan!";
    }

    public float HoldDuration => 0f; // Tap [E] Instan untuk memulai Minigame

    public void OnInteract()
    {
        if (isCompleted) return;

        // Cek Alat di Tangan
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasItem)
        {
            Debug.LogWarning($"[TIMING TASK] Butuh alat '{requiredItemName}' di tangan untuk memulai!");
            return;
        }

        // Buka Minigame Timing Lingkaran!
        TimingMinigameController controller = TimingMinigameController.Instance;
        if (controller != null)
        {
            controller.StartMinigame(requiredHits, OnMinigameWon);
        }
        else
        {
            Debug.LogError("[TIMING ERROR] Objek 'TimingMinigameController' tidak ditemukan di Canvas! Pastikan script sudah terpasang di Canvas UI.");
        }
    }

    private void OnMinigameWon()
    {
        isCompleted = true;
        Debug.Log($"[TASK COMPLETED] Tugas '{gameObject.name}' Selesai via Minigame!");

        // 1. Matikan objek jatuh / rusak di lantai
        if (objectToDeactivate != null)
        {
            objectToDeactivate.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // 2. Aktifkan objek rapi / terpasang di dinding
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }

        // 3. Laporkan ke TaskManager (Notebook TAB)
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null) taskMgr.CompleteTask(taskId);
        }
    }
}