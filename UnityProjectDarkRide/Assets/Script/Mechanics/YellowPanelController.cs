using UnityEngine;

/// <summary>
/// Kontroler Panel Listrik Kuning (Fuse Panel).
/// Mengelola lampu indikator kuning (kedap-kedip saat fuse rusak, nyala solid saat normal)
/// serta proses penggantian fuse dengan FusePack.
/// </summary>
public class YellowPanelController : MonoBehaviour, IInteractable
{
    [Header("Panel Fuse Status")]
    [Tooltip("CENTANG (TRUE) agar lampu KEDAP-KEDIP saat game mulai!")]
    [SerializeField] private bool isFuseBroken = true; 
    [SerializeField] private string requiredItemName = "FusePack";

    [Header("Yellow Light Settings")]
    [SerializeField] private Light yellowIndicatorLight; // Point Light Kuning
    [SerializeField] private float blinkSpeed = 6.0f;     // Kecepatan Kedap-Kedip
    [SerializeField] private float minIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 5.0f;

    [Header("Task Hold Duration")]
    [SerializeField] private float repairHoldTime = 2.0f;

    [Header("Task Manager Integration")]
    [Tooltip("Isi dengan ID Tugas di TaskManager Notebook TAB (misal: FIX_PANEL / FIX_FUSE)")]
    [SerializeField] private string taskId = "FIX_PANEL";

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (!isFuseBroken)
            return "Panel Listrik [NORMAL / SOLID YELLOW]";

        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (hasRequiredItem)
        {
            return $"Tahan [E] {repairHoldTime:F0}d Ganti Fuse ({requiredItemName})";
        }
        else
        {
            return $"[BUTUH ALAT] Membutuhkan '{requiredItemName}' di Tangan!";
        }
    }

    public float HoldDuration
    {
        get
        {
            if (!isFuseBroken) return 0f;

            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
            bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

            return hasRequiredItem ? repairHoldTime : 0f;
        }
    }

    public void OnInteract()
    {
        if (!isFuseBroken) return;

        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasRequiredItem) return;

        isFuseBroken = false;
        Debug.Log("[PANEL REPAIRED] Fuse lama berhasil diganti! Panel kembali NORMAL / SOLID YELLOW!");

        // Hapus FusePack dari Sabuk
        if (toolBelt != null)
        {
            toolBelt.ConsumeActiveItem();
        }

        // Checklist di TaskManager
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null)
            {
                taskMgr.CompleteTask(taskId);
            }
        }
    }

    private void Update()
    {
        if (yellowIndicatorLight == null) return;

        if (isFuseBroken)
        {
            // 🟡 1. KEDAP-KEDIP (Flickering) = Fuse Rusak
            float flicker = Mathf.PingPong(Time.time * blinkSpeed, 1.0f);
            yellowIndicatorLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, flicker);
            yellowIndicatorLight.enabled = true;
        }
        else
        {
            // 🟡 2. NYALA SOLID (Konstan) = Fuse Normal
            yellowIndicatorLight.intensity = maxIntensity;
            yellowIndicatorLight.enabled = true;
        }
    }

    public void SetFuseBrokenState(bool broken) => isFuseBroken = broken;
    public bool IsFuseBroken => isFuseBroken;
    public bool IsPowerOn => !isFuseBroken;
}