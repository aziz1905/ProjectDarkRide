using UnityEngine;

/// <summary>
/// Script Interaksi Obeng pada Animatronik di Gurney / Lingkungan.
/// - Memerlukan alat 'ScrewDriver' (Obeng) di tangan.
/// - Menekan [E] membuka ScrewdriverMinigameController (Minigame Obeng 2 Baut).
/// - Begitu minigame selesai, tugas di TaskManager langsung tercoret [X]!
/// </summary>
public class ScrewdriverTaskInteractable : MonoBehaviour, IInteractable
{
    [Header("Task Manager Settings")]
    [Tooltip("ID Tugas di Notebook TAB (misal: FIX_ANIMATRONIC_SCREW)")]
    [SerializeField] private string taskId = "FIX_ANIMATRONIC_SCREW";

    [Header("Tool Requirement")]
    [Tooltip("Nama alat di sabuk yang dibutuhkan (Default: ScrewDriver / Obeng)")]
    [SerializeField] private string requiredTool = "ScrewDriver";

    [Header("Object Toggle Settings")]
    [Tooltip("Objek copot/rusak yang akan DIMATIKAN saat selesai (Jika kosong, otomatis objek ini sendiri)")]
    [SerializeField] private GameObject objectToDeactivate;

    [Tooltip("Objek awal/rapi yang akan DIAKTIFKAN saat selesai (misal: KomponenAwal di animatronik)")]
    [SerializeField] private GameObject objectToActivate;

    // Internal State
    private bool isTaskCompleted = false;

    public string GetInteractPrompt()
    {
        if (isTaskCompleted)
        {
            return "Animatronik [KOMPONEN SUDAH DIPERBAIKI]";
        }

        if (!IsHoldingRequiredTool())
        {
            return $"[BUTUH ALAT] Butuh {requiredTool} untuk Memperbaiki Baut Animatronik";
        }

        return "Tekan [E] Kencangkan Baut Animatronik (Obeng)";
    }

    public float HoldDuration => 0f; // Tap E Instan untuk Membuka Minigame

    public void OnInteract()
    {
        if (isTaskCompleted) return;

        if (!IsHoldingRequiredTool())
        {
            Debug.LogWarning($"[SCREWDRIVER TASK] Pemain tidak memegang alat '{requiredTool}'!");
            return;
        }

        // Buka Minigame Obeng 2 Baut
        ScrewdriverMinigameController controller = ScrewdriverMinigameController.Instance;
        if (controller != null)
        {
            controller.OpenMinigame(OnMinigameSuccess, null);
        }
        else
        {
            Debug.LogError("[SCREWDRIVER TASK] ScrewdriverMinigameController tidak ditemukan di Canvas!");
        }
    }

    private void OnMinigameSuccess()
    {
        isTaskCompleted = true;

        // 1. Matikan objek yang rusak / copot di lantai
        if (objectToDeactivate != null)
        {
            objectToDeactivate.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // 2. Aktifkan objek awal yang rapi dan terpasang
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }

        Debug.Log("<color=green>[SCREWDRIVER TASK COMPLETE] Komponen animatronik berhasil diperbaiki!</color>");

        // 3. Laporkan ke TaskManager Notebook TAB
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null)
            {
                taskMgr.CompleteTask(taskId);
            }
        }
    }

    private bool IsHoldingRequiredTool()
    {
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        if (toolBelt == null) return false;

        string currentItem = toolBelt.ActiveItemName.Trim();
        return currentItem.Equals(requiredTool, System.StringComparison.OrdinalIgnoreCase) ||
               currentItem.Equals("ScrewDriver", System.StringComparison.OrdinalIgnoreCase) ||
               currentItem.Equals("Obeng", System.StringComparison.OrdinalIgnoreCase);
    }
}
