using UnityEngine;

/// <summary>
/// Kontroler Panel Listrik Kuning (Fuse Panel).
/// Mengelola lampu indikator kuning serta proses penggantian fuse dengan FusePack.
/// Syarat Keselamatan: Sakelar Listrik (PowerSwitch) WAJIB MATI (OFF) terlebih dahulu sebelum fuse diganti!
/// Saat Sakelar Listrik MATI (OFF), Lampu Indikator Kuning Panel juga ikut MATI (PADAM).
/// </summary>
public class YellowPanelController : MonoBehaviour, IInteractable
{
    [Header("Panel Fuse Status")]
    [Tooltip("CENTANG (TRUE) agar lampu KEDAP-KEDIP saat game mulai!")]
    [SerializeField] private bool isFuseBroken = true; 
    [SerializeField] private string requiredItemName = "FusePack";

    [Header("Power Safety Settings")]
    [Tooltip("Drag Objek switchListrik / PowerSwitchCube di sini!")]
    [SerializeField] private PowerSwitchInteractable powerSwitch;
    
    [Tooltip("Jika CENTANG (TRUE), penggantian fuse WAJIB saat sakelar listrik MATI (OFF) demi keselamatan kerja.")]
    [SerializeField] private bool requirePowerOff = true;

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

    // Pengecekan apakah sakelar listrik masih ON
    public bool IsSwitchOn => (powerSwitch == null) || powerSwitch.IsPowerOn;

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (!isFuseBroken)
            return "Panel Listrik [NORMAL / SOLID YELLOW]";

        // 1. Pengecekan Keselamatan: Sakelar Listrik Masih ON?
        if (requirePowerOff && IsSwitchOn)
        {
            return "[BAHAYA] Matikan Sakelar Listrik Terlebih Dahulu!";
        }

        // 2. Cek Alat di Tangan
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasRequiredItem)
        {
            return $"[BUTUH ALAT] Membutuhkan '{requiredItemName}' di Tangan!";
        }

        return $"Tahan [E] {repairHoldTime:F0}d Ganti Fuse ({requiredItemName})";
    }

    public float HoldDuration => isFuseBroken ? repairHoldTime : 0f;

    public void OnInteract()
    {
        if (!isFuseBroken) return;

        // 1. CEK KESELAMATAN SAKELAR LISTRIK
        if (requirePowerOff && IsSwitchOn)
        {
            Debug.LogWarning("[FUSE REPAIR FAILED] Listrik masih menyala! Matikan Sakelar di Panel Listrik terlebih dahulu!");
            return;
        }

        // 2. CEK ALAT DI TANGAN
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasRequiredItem) return;

        // BERHASIL GANTI FUSE
        isFuseBroken = false;
        Debug.Log("[PANEL REPAIRED] Fuse lama berhasil diganti! Panel kembali NORMAL!");

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

        // 🔴 1. JIKA SAKELAR MATI (OFF): Lampu Indikator Kuning Panel PADAM TOTAL!
        if (powerSwitch != null && !powerSwitch.IsPowerOn)
        {
            yellowIndicatorLight.enabled = false;
            return;
        }

        // 🟢 2. JIKA SAKELAR NYALA (ON):
        if (isFuseBroken)
        {
            // 🟡 Kedap-kedip jika Fuse Rusak
            float flicker = Mathf.PingPong(Time.time * blinkSpeed, 1.0f);
            yellowIndicatorLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, flicker);
            yellowIndicatorLight.enabled = true;
        }
        else
        {
            // 🟡 Nyala Solid jika Fuse Sudah Diganti
            yellowIndicatorLight.intensity = maxIntensity;
            yellowIndicatorLight.enabled = true;
        }
    }

    public void SetFuseBrokenState(bool broken) => isFuseBroken = broken;
    public bool IsFuseBroken => isFuseBroken;
}