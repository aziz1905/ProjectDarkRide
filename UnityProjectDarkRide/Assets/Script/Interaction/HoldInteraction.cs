using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Script Interaksi Tahan [E] Serbaguna (Palu, Kain, Obeng, Lampu, Pintu, dll).
/// Otomatis mengecek kesesuaian alat di Tool Belt sebelum mengizinkan Tahan [E].
/// </summary>
public class HoldInteraction : MonoBehaviour, IInteractable
{
    [Header("Interaction Prompts")]
    [SerializeField] private string promptText = "Tahan [E] 2d Perbaiki Objek";
    [SerializeField] private float holdTime = 2.0f; // Durasi Tahan E (detik)

    [Header("Item Requirements (Opsional)")]
    [Tooltip("Kosongkan jika interaksi ini bisa dilakukan tanpa membawa alat apa pun (misal: Buka Pintu / Luruskan Lukisan).")]
    [SerializeField] private string requiredItemName = ""; 

    [Header("Task Manager Synergy (Opsional)")]
    [Tooltip("Isi dengan Task ID di Notebook TAB (misal: CLEAN_TRASH, FIX_PANEL, WIPE_MIRROR, FIX_RAIL)")]
    [SerializeField] private string taskId = "";

    [Header("Options After Hold Completed")]
    [SerializeField] private bool isCompleted = false;
    [SerializeField] private bool disableObjectOnComplete = false;
    [SerializeField] private bool changeColorOnComplete = false;
    [SerializeField] private Color completedColor = Color.cyan;

    [Header("Custom Actions (Events)")]
    [Tooltip("Dipanggil saat 100% Selesai.")]
    public UnityEvent OnHoldCompleted;

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (isCompleted) return "[SELESAI]";

        // Jika butuh alat tertentu:
        if (!string.IsNullOrEmpty(requiredItemName))
        {
            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
            bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

            if (!hasRequiredItem)
            {
                return $"[BUTUH ALAT] Membutuhkan '{requiredItemName}' di Tangan!";
            }
        }

        return promptText;
    }

    public float HoldDuration => isCompleted ? 0f : holdTime;

    public void OnInteract()
    {
        if (isCompleted) return;

        // Periksa alat jika requiredItemName diisi
        if (!string.IsNullOrEmpty(requiredItemName))
        {
            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
            bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

            if (!hasRequiredItem) return;
        }

        // BERHASIL
        isCompleted = true;
        Debug.Log($"[HOLD COMPLETED] Interaksi pada '{gameObject.name}' Berhasil!");

        // Laporkan ke TaskManager (Notebook TAB)
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null)
            {
                taskMgr.CompleteTask(taskId);
            }
        }

        // Eksekusi Event/Aksi Custom
        OnHoldCompleted?.Invoke();

        if (changeColorOnComplete)
        {
            // 🟢 UBAH WARNA SELURUH MESH ANAK (Kepala & Badan)!
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                if (rend != null)
                {
                    rend.material.color = completedColor;
                }
            }
        }

        if (disableObjectOnComplete)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Fungsi Universal 1-klik untuk meluruskan ROTASI objek saja ke (0,0,0).
    /// </summary>
    public void ResetRotationToZero()
    {
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        Debug.Log($"[ROTATE RESET] Rotasi '{gameObject.name}' telah diluruskan ke (0,0,0)!");
    }

    /// <summary>
    /// Fungsi Universal 1-klik untuk mereset POSISI dan ROTASI objek sekaligus ke (0,0,0) lokal.
    /// Sangat cocok untuk mengembalikan Lukisan Jatuh / Komponen Copot ke posisi awal induknya.
    /// </summary>
    public void ResetTransformToZero()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        Debug.Log($"[TRANSFORM RESET] Posisi dan Rotasi '{gameObject.name}' di-reset ke (0,0,0)!");
    }

    // Reset status (opsional)
    public void ResetInteraction()
    {
        isCompleted = false;
    }
}