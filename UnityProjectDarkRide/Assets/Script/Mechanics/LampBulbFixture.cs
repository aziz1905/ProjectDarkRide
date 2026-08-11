using UnityEngine;

/// <summary>
/// Mekanik Mengganti Bohlam Lampu Rusak / Berkedip.
/// Menangani sinkronisasi kedap-kedip Sinar Lampu (Light) DENGAN Kaca 3D (BolaPijar).
/// Dilengkapi Pengaturan JEDA DIAM REDUP / GELAP (Horror Blackout Pause).
/// 0% Beban CPU & 0 B/frame GC Alloc.
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
    [SerializeField] private float flickerSpeed = 5.0f;
    [SerializeField] private float minIntensity = 0.1f;    // Intensitas saat redup/mati
    [SerializeField] private float maxIntensity = 3.0f;    // Intensitas saat terang maksimal

    [Header("Horror Pause Settings (Jeda Diam Redup)")]
    [Tooltip("Berapa detik lampu DIAM REDUP / GELAP sebelum menyala kembali")]
    [SerializeField] private float dimPauseDuration = 2.0f;  // Jeda diam redup (2 detik)
    
    [Tooltip("Berapa detik lampu menyala/berkedip sebelum masuk jeda redup")]
    [SerializeField] private float brightBurstDuration = 1.5f; // Durasi terang berkedip (1.5 detik)

    [Header("3D BolaPijar Emission Settings (Glow Kaca)")]
    [SerializeField] private Color baseEmissionColor = new Color(1f, 0.85f, 0.5f); // Kuning Warm Pijar
    [SerializeField] private float minEmissionGlow = 0.02f; // Kusam/mati saat jeda redup
    [SerializeField] private float maxEmissionGlow = 2.0f;  // Terang menyala saat terang

    [Header("Hold Interaction Settings")]
    [SerializeField] private float replaceHoldTime = 2.0f;

    [Header("Task Manager Integration")]
    [Tooltip("Isi dengan ID Tugas di TaskManager Notebook TAB (misal: FIX_BULB / LAMP_FIX)")]
    [SerializeField] private string taskId = "FIX_BULB";

    // Cache Internal & Timer
    private Renderer bolaPijarRenderer;
    private Material bolaPijarMaterial;
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
    
    private float stateTimer = 0f;
    private bool isInDimPause = false;

    private void Start()
    {
        if (bolaPijarObject != null)
        {
            bolaPijarRenderer = bolaPijarObject.GetComponent<Renderer>();
            if (bolaPijarRenderer != null)
            {
                bolaPijarMaterial = bolaPijarRenderer.material;
                bolaPijarMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

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
            stateTimer += Time.deltaTime;

            if (isInDimPause)
            {
                // 🌑 1. STATE JEDA DIAM REDUP / GELAP (Berlangsung selama dimPauseDuration detik)
                SetLightAndGlow(minIntensity, minEmissionGlow);

                if (stateTimer >= dimPauseDuration)
                {
                    isInDimPause = false;
                    stateTimer = 0f;
                }
            }
            else
            {
                // 🟡 2. STATE TERANG BERKEDIP (FLICKER BURST)
                float flicker = Mathf.PingPong(Time.time * flickerSpeed, 1.0f);
                float currentLight = Mathf.Lerp(minIntensity, maxIntensity, flicker);
                float currentGlow = Mathf.Lerp(minEmissionGlow, maxEmissionGlow, flicker);

                SetLightAndGlow(currentLight, currentGlow);

                if (stateTimer >= brightBurstDuration)
                {
                    isInDimPause = true;
                    stateTimer = 0f;
                }
            }
        }
        else
        {
            // 💡 3. STATE BOHLAM NORMAL (TERANG SOLID KONSTAN)
            SetLightAndGlow(maxIntensity, maxEmissionGlow);
        }
    }

    private void SetLightAndGlow(float lightIntensity, float glowIntensity)
    {
        if (bulbLight != null)
        {
            bulbLight.intensity = lightIntensity;
            bulbLight.enabled = lightIntensity > 0.05f;
        }

        if (bolaPijarMaterial != null)
        {
            bolaPijarMaterial.SetColor(EmissionColorProp, baseEmissionColor * Mathf.LinearToGammaSpace(glowIntensity));
        }
    }

    public void SetBrokenState(bool broken) => isBulbBroken = broken;
    public bool IsBulbBroken => isBulbBroken;
}