using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Dipasang pada Objek Tugas (Rel Kereta / Lukisan Jatuh).
/// Membuka Minigame Timing saat pemain menekan [E] membawa alat yang sesuai.
/// </summary>
public class TimingTaskInteractable : MonoBehaviour, IInteractable
{
    [Header("Task Settings")]
    [SerializeField] private string requiredItemName = "Hammer";
    [SerializeField] private int requiredHits = 3; // Butuh 3x ketukan palu tepat
    [SerializeField] private string taskId = "FIX_RAIL";

    [Header("Options After Completed")]
    [SerializeField] private bool disableObjectOnComplete = false;
    
    [Tooltip("CENTANG jika ingin seluruh mesh objek anak berubah warna saat tugas selesai (Prototype)")]
    [SerializeField] private bool changeColorOnComplete = false;
    [SerializeField] private Color completedColor = Color.green;

    [Header("Custom Actions (Events)")]
    public UnityEvent OnTaskCompleted;

    // Internal State Guard (Otomatis di balik layar)
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

        // 🎨 1. UBAH WARNA MATERIAL SEMUA MESH (PROTOTYPE VISUAL)
        if (changeColorOnComplete)
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
            if (allRenderers != null && allRenderers.Length > 0)
            {
                foreach (Renderer r in allRenderers)
                {
                    if (r != null)
                    {
                        r.material.color = completedColor;
                    }
                }
            }
        }

        // 📋 2. LAPORKAN KE TASKMANAGER (Notebook TAB)
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null) taskMgr.CompleteTask(taskId);
        }

        // ⚡ 3. EKSEKUSI EVENT CUSTOM (misal ResetRotationToZero)
        OnTaskCompleted?.Invoke();

        // 🚫 4. MATIKAN OBJEK JIKA DIPERLUKAN
        if (disableObjectOnComplete)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Meluruskan rotasi objek ke (0,0,0) saat tugas selesai
    /// </summary>
    public void ResetRotationToZero()
    {
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        Debug.Log($"[ROTATE RESET] Rotasi '{gameObject.name}' diluruskan ke (0,0,0) via Minigame!");
    }
}