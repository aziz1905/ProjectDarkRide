using UnityEngine;

/// <summary>
/// Mekanik Mengganti Bohlam Lampu Rusak / Berkedip.
/// Menangani pencahayaan (Light) serta visual mesh (BolaPijar).
/// Syarat: Player cukup memegang item 'Bulb' di tangan dan Menahan tombol [E].
/// </summary>
public class LampBulbFixture : MonoBehaviour, IInteractable
{
    [Header("Bulb State")]
    [Tooltip("CENTANG (TRUE) jika bohlam dalam kondisi rusak/berkedip saat game dimulai.")]
    [SerializeField] private bool isBulbBroken = true;
    [SerializeField] private string requiredItemName = "Bulb";

    [Header("Visual & Light Components")]
    [Tooltip("Drag komponen Spot Light / Point Light lampu di sini")]
    [SerializeField] private Light bulbLight;               

    [Tooltip("Drag Objek 'BolaPijar' dari Hierarchy ke sini")]
    [SerializeField] private GameObject bolaPijarObject;   

    [Header("Flicker Light Settings")]
    [SerializeField] private float flickerSpeed = 3.5f;
    [SerializeField] private float minIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 3.0f;

    [Header("Hold Interaction Settings")]
    [SerializeField] private float replaceHoldTime = 2.0f;

    [Header("Task Manager Integration")]
    [Tooltip("Isi dengan ID Tugas di TaskManager Notebook TAB (misal: FIX_BULB / LAMP_FIX)")]
    [SerializeField] private string taskId = "FIX_BULB";

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (!isBulbBroken)
            return "Bohlam Lampu [NORMAL]";

        // Cek Alat di Tangan
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasRequiredItem)
        {
            return $"[BUTUH ALAT] Membutuhkan '{requiredItemName}' di Tangan!";
        }

        return $"Tahan [E] {replaceHoldTime:F0}d Ganti Bohlam ({requiredItemName})";
    }

    public float HoldDuration => isBulbBroken ? replaceHoldTime : 0f;

    public void OnInteract()
    {
        if (!isBulbBroken) return;

        // CEK ALAT DI TANGAN PLAYER
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
        bool hasRequiredItem = currentItem.Trim().Equals(requiredItemName.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (!hasRequiredItem) return;

        // BERHASIL GANTI BOHLAM
        isBulbBroken = false;
        Debug.Log("[LAMP REPAIRED] Bohlam lampu rusak berhasil diganti!");

        // Konsumsi / Hapus Bohlam dari Inventory Sabuk setelah dipasang
        if (toolBelt != null)
        {
            toolBelt.ConsumeActiveItem();
        }

        // Laporkan ke TaskManager (Notebook TAB)
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
        if (isBulbBroken)
        {
            // 🟡 Bohlam Rusak = Kedap-Kedip (Flickering)
            if (bulbLight != null)
            {
                float flicker = Mathf.PingPong(Time.time * flickerSpeed, 1.0f);
                bulbLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, flicker);
                bulbLight.enabled = true;
            }
            if (bolaPijarObject != null) bolaPijarObject.SetActive(true);
        }
        else
        {
            // 🟡 Bohlam Normal = Nyala Solid Konstan
            if (bulbLight != null)
            {
                bulbLight.intensity = maxIntensity;
                bulbLight.enabled = true;
            }
            if (bolaPijarObject != null) bolaPijarObject.SetActive(true);
        }
    }

    public void SetBrokenState(bool broken) => isBulbBroken = broken;
    public bool IsBulbBroken => isBulbBroken;
}