using UnityEngine;

public class ToolBeltManager : MonoBehaviour
{
    [System.Serializable]
    public struct InventoryMeshLink
    {
        public string itemName;        // Contoh: "TrashBag", "FusePack", "Cloth", "Bulb", "Key", "Hammer"
        public GameObject heldMesh;    // Drag 3D Mesh alat di TANGAN
        public GameObject worldPrefab; // Drag Prefab Fisik dari folder Assets/Prefabs/
    }

    [Header("Tool Belt Settings")]
    [SerializeField] private int maxSlots = 5;
    [SerializeField] private int activeEquippedSlotIndex = -1; // -1 = Tangan Kosong

    [Header("Drop Settings")]
    [SerializeField] private KeyCode dropKey = KeyCode.G;    // Tombol G untuk Drop Barang
    [SerializeField] private float dropForwardForce = 2.0f;  // Dorongan jatuh ke depan

    [Header("Tutorial Task Integration (Day 1)")]
    [Tooltip("ID Tugas di TaskManager untuk tutorial ambil alat (misal: TAKE_TOOLS)")]
    [SerializeField] private string tutorialTaskId = "TAKE_TOOLS";
    [Tooltip("Berapa alat yang harus diambil di Workshop untuk menyelesaikan tugas ini")]
    [SerializeField] private int requiredToolCount = 4;

    [Header("Dynamic 3D Mesh Lookup")]
    [Tooltip("Daftar pencocokan nama item dengan 3D Mesh di tangan & Prefab di folder Assets/Prefabs/")]
    [SerializeField] private InventoryMeshLink[] itemMeshList;

    // Array menyimpan nama item di masing-masing 5 slot (Kosong = "Empty")
    private string[] slotItemNames = new string[5];

    private void Start()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            slotItemNames[i] = "Empty";
        }

        activeEquippedSlotIndex = -1; // Tangan Kosong di awal game
        UpdateHeldItemVisuals();
    }

    // Status Heavy Carry (Mayat Animatronik)
    private bool isCarryingHeavyObject = false;
    public bool IsCarryingHeavyObject => isCarryingHeavyObject;

    public void SetHeavyCarry(bool isCarrying)
    {
        isCarryingHeavyObject = isCarrying;
        if (isCarrying)
        {
            UnequipToEmptyHands(); // Sembunyikan alat palu/obeng saat menggotong mayat
        }
    }

    private void Update()
    {
        HandleSlotSelectionInput();
        HandleDropInput();
    }

    private void HandleSlotSelectionInput()
    {
        // Kunci tombol 1-5 saat kedua tangan sedang menggotong mayat animatronik!
        if (isCarryingHeavyObject) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleEquipSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleEquipSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleEquipSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleEquipSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleEquipSlot(4);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            int nextSlot = activeEquippedSlotIndex < 0 ? 0 : (activeEquippedSlotIndex - 1 + maxSlots) % maxSlots;
            EquipSlot(nextSlot);
        }
        else if (scroll < 0f)
        {
            int nextSlot = activeEquippedSlotIndex < 0 ? 0 : (activeEquippedSlotIndex + 1) % maxSlots;
            EquipSlot(nextSlot);
        }
    }

    private void HandleDropInput()
    {
        if (Input.GetKeyDown(dropKey))
        {
            DropActiveItem();
        }
    }

    public void ToggleEquipSlot(int slotIndex)
    {
        if (activeEquippedSlotIndex == slotIndex)
        {
            UnequipToEmptyHands();
        }
        else
        {
            EquipSlot(slotIndex);
        }
    }

    public void EquipSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;

        activeEquippedSlotIndex = slotIndex;
        string itemInSlot = slotItemNames[activeEquippedSlotIndex];
        
        Debug.Log($"[Tool Belt] Mengeluarkan Slot {activeEquippedSlotIndex + 1} ke Tangan. (Isi Item: '{itemInSlot}')");
        UpdateHeldItemVisuals();
    }

    public void UnequipToEmptyHands()
    {
        activeEquippedSlotIndex = -1;
        Debug.Log("[Tool Belt] Barang Disimpan ke Sabuk. Tangan Player KOSONG.");
        UpdateHeldItemVisuals();
    }

    public bool AddItemToFirstAvailableSlot(string itemName)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (slotItemNames[i] == "Empty")
            {
                slotItemNames[i] = itemName;
                Debug.Log($"[Tool Belt] Item '{itemName}' Disimpan di Sabuk (Slot {i + 1}).");
                
                UpdateHeldItemVisuals();

                // 🌟 CEK TUTORIAL TASK: Jika sudah mengambil jumlah alat yang ditentukan, selesaikan task!
                if (!string.IsNullOrEmpty(tutorialTaskId) && TaskManager.Instance != null)
                {
                    int filledSlots = 0;
                    for (int j = 0; j < maxSlots; j++)
                    {
                        if (slotItemNames[j] != "Empty") filledSlots++;
                    }

                    if (filledSlots >= requiredToolCount)
                    {
                        TaskManager.Instance.CompleteTask(tutorialTaskId);
                    }
                }

                return true;
            }
        }

        Debug.LogWarning("[Tool Belt Penuh] Sabuk Tool Belt Anda sudah penuh (5/5)!");
        return false;
    }

    public void DropActiveItem()
    {
        if (activeEquippedSlotIndex < 0) return; // Jika tangan kosong, abaikan
        
        string itemToDrop = slotItemNames[activeEquippedSlotIndex];
        if (itemToDrop == "Empty") return;

        string cleanItemName = itemToDrop.Replace("(Clone)", "").Trim();
        int spaceIdx = cleanItemName.IndexOf(' ');
        if (spaceIdx > 0) cleanItemName = cleanItemName.Substring(0, spaceIdx);

        // Cari Prefab fisik dari Inspector itemMeshList
        GameObject prefabToSpawn = null;
        if (itemMeshList != null)
        {
            foreach (var link in itemMeshList)
            {
                if (link.itemName.Trim().Equals(cleanItemName, System.StringComparison.OrdinalIgnoreCase))
                {
                    prefabToSpawn = link.worldPrefab;
                    break;
                }
            }
        }

        // Spawn Prefab jika di-drag di Inspector
        if (prefabToSpawn != null)
        {
            // Spawn 0.8m di depan & 0.1m di BAWAH mata kamera menghadap ke arah pandangan pemain
            Vector3 spawnPos = Camera.main.transform.position + (Camera.main.transform.forward * 0.8f) - (Vector3.up * 0.1f);
            Quaternion spawnRot = Camera.main.transform.rotation;
            GameObject droppedObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            
            droppedObj.name = cleanItemName; // Pastikan nama objek bersih agar bisa diambil lagi
            droppedObj.SetActive(true); // Pastikan aktif di scene

            Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
            if (rb == null) rb = droppedObj.AddComponent<Rigidbody>();
            
            // RESET KECEPATAN FISIKA & AKTIFKAN TABRAKAN FISIK CONTINUOUS
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // DORONGAN LEMPARAN KE DEPAN + LENGKUNGAN KE ATAS (PARABOLA LEMPARAN)
            Vector3 throwForce = (Camera.main.transform.forward * dropForwardForce) + (Vector3.up * 2.0f);
            rb.AddForce(throwForce, ForceMode.Impulse);

            // PUTARAN SPINNER LEMPARAN (TORQUE) AGAR TERLIHAT REALISTIS
            rb.AddTorque(Random.insideUnitSphere * 3.0f, ForceMode.Impulse);

            // 💡 SINKRONISASI CAHAYA LAMPU SENTER (JIKA SENTER DILEMPAR/DROP KE LANTAI)
            EquipableFlashlight heldFlashlight = FindObjectOfType<EquipableFlashlight>();
            if (heldFlashlight != null)
            {
                bool wasLightOn = heldFlashlight.IsLightOn;
                Light droppedLight = droppedObj.GetComponentInChildren<Light>(true);
                if (droppedLight != null)
                {
                    droppedLight.enabled = wasLightOn;
                    droppedLight.gameObject.SetActive(wasLightOn);
                }

                Debug.Log($"[DROP FLASHLIGHT] Status Lampu Senter di Lantai: {(wasLightOn ? "MENYALA (ON)" : "MATI (OFF)")}");
            }

            Debug.Log($"[LEMPAR SUCCESS] Item '{cleanItemName}' dilempar ke depan!");
        }
        else
        {
            Debug.LogWarning($"[DROP WARNING] Prefab untuk '{cleanItemName}' belum di-drag ke slot World Prefab di Inspector ToolBeltManager!");
        }

        // Reset slot di sabuk dan set tangan jadi kosong
        slotItemNames[activeEquippedSlotIndex] = "Empty";
        UnequipToEmptyHands();
    }

    private void UpdateHeldItemVisuals()
    {
        string currentActiveItem = ActiveItemName;

        if (itemMeshList == null) return;

        string cleanActive = currentActiveItem.Replace("(Clone)", "").Trim();
        int spaceIdx = cleanActive.IndexOf(' ');
        if (spaceIdx > 0) cleanActive = cleanActive.Substring(0, spaceIdx);

        foreach (var link in itemMeshList)
        {
            if (link.heldMesh != null)
            {
                bool isMatch = (currentActiveItem != "Empty") && (link.itemName.Trim().Equals(cleanActive, System.StringComparison.OrdinalIgnoreCase));
                link.heldMesh.SetActive(isMatch);
            }
        }
    }

    // Getters
    public int ActiveSlotIndex => activeEquippedSlotIndex;
    public string ActiveItemName => (activeEquippedSlotIndex >= 0 && activeEquippedSlotIndex < maxSlots) ? slotItemNames[activeEquippedSlotIndex] : "Empty";

    /// <summary>
    /// Menghapus / mengonsumsi item yang sedang dipegang di tangan saat ini (misal: Bulb / FusePack yang selesai dipakai).
    /// </summary>
    public void ConsumeActiveItem()
    {
        if (activeEquippedSlotIndex < 0) return;
        string consumedItem = slotItemNames[activeEquippedSlotIndex];
        Debug.Log($"[TOOL BELT] Item '{consumedItem}' di Slot {activeEquippedSlotIndex + 1} telah HABIS DIPAKAI!");
        // Kosongkan slot di sabuk dan kembalikan tangan ke kondisi kosong
        slotItemNames[activeEquippedSlotIndex] = "Empty";
        UnequipToEmptyHands();
    }
}