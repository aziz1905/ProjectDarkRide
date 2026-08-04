using UnityEngine;

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

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (isFuseBroken)
            return $"Tahan [E] 2d Ganti Fuse ({requiredItemName})";
        else
            return "Panel Listrik [NORMAL / SOLID YELLOW]";
    }

    public float HoldDuration => isFuseBroken ? repairHoldTime : 0f;

    public void OnInteract()
    {
        if (!isFuseBroken)
        {
            Debug.Log("[Panel System] Panel ini sudah normal!");
            return;
        }

        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";

        // PERIKSA NAMA 
        if (toolBelt != null && currentItem.Trim() == requiredItemName.Trim())
        {
            isFuseBroken = false; // Fuse diganti -> Lampu berhenti berkedip
            Debug.Log("[PANEL REPAIRED] Fuse lama berhasil diganti! Lampu berubah menjadi SOLID YELLOW!");
        }
        else
        {
            Debug.LogWarning($"[PANEL REPAIR FAILED] Gagal mengganti fuse! Anda harus memegang '{requiredItemName}' di Tool Belt. (Item di tangan saat ini: '{currentItem}')");
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
}