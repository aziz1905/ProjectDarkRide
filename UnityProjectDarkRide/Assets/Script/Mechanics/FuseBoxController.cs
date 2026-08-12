using UnityEngine;

/// <summary>
/// Kontroler Kotak Fuse (FuseBox).
/// Mengelola penggantian fuse dengan FusePack.
/// Syarat Keselamatan: Sakelar Listrik WAJIB OFF terlebih dahulu sebelum fuse diganti!
/// </summary>
public class FuseBoxController : MonoBehaviour, IInteractable
{
    [Header("Fuse Status")]
    [Tooltip("CENTANG (TRUE) jika fuse dalam kondisi rusak saat game dimulai.")]
    [SerializeField] private bool isFuseBroken = true;
    [SerializeField] private string requiredItemName = "FusePack";

    [Header("Safety Power Switch")]
    [Tooltip("Drag Objek Switch / Sakelar dari SaklarBox ke sini")]
    [SerializeField] private PowerSwitchInteractable powerSwitch;
    [SerializeField] private bool requirePowerOff = true;

    [Header("Hold Duration")]
    [SerializeField] private float replaceHoldTime = 2.0f;

    [Header("Task Manager Integration")]
    [Tooltip("Isi dengan ID Tugas di TaskManager Notebook TAB (misal: FIX_FUSE / FIX_PANEL)")]
    [SerializeField] private string taskId = "FIX_FUSE";

    public bool IsFuseBroken => isFuseBroken;
    public bool IsSwitchOn => powerSwitch != null && powerSwitch.IsPowerOn;

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (!isFuseBroken)
            return "FuseBox [NORMAL]";

        // 1. CEK KESELAMATAN: Sakelar Listrik masih ON?
        if (requirePowerOff && IsSwitchOn)
        {
            return "[BAHAYA] Matikan Sakelar Listrik Terlebih Dahulu!";
        }

        // 2. CEK ALAT DI TANGAN
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasRequiredItem)
        {
            return $"[BUTUH ALAT] Membutuhkan '{requiredItemName}' di Tangan!";
        }

        return $"Tahan [E] {replaceHoldTime:F0}d Ganti Fuse ({requiredItemName})";
    }

    public float HoldDuration
    {
        get
        {
            if (!isFuseBroken) return 0f;
            if (requirePowerOff && IsSwitchOn) return 0f;

            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
            bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

            return hasRequiredItem ? replaceHoldTime : 0f;
        }
    }

    public void OnInteract()
    {
        if (!isFuseBroken) return;

        // 1. PENGAMAN KESELAMATAN
        if (requirePowerOff && IsSwitchOn)
        {
            Debug.LogWarning("[FUSE BLOCKED] Sakelar masih ON! Matikan sakelar terlebih dahulu!");
            return;
        }

        // 2. CEK ALAT DI TANGAN
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasRequiredItem) return;

        // BERHASIL GANTI FUSE
        isFuseBroken = false;
        Debug.Log("[FUSE REPAIRED] Fuse baru berhasil dipasang!");

        // Konsumsi / Hapus FusePack dari sabuk
        if (toolBelt != null)
        {
            toolBelt.ConsumeActiveItem();
        }

        // Laporkan ke TaskManager
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null) taskMgr.CompleteTask(taskId);
        }
    }

    public void SetFuseBrokenState(bool broken) => isFuseBroken = broken;
}
