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
        ResetToolBelt();
    }

    /// <summary>
    /// Mereset seluruh isi sabuk perkakas kembali ke kondisi kosong (Awal Shift)
    /// </summary>
    public void ResetToolBelt()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            slotItemNames[i] = "Empty";
        }
        activeEquippedSlotIndex = -1;
        isCarryingHeavyObject = false;
        isSeatedInCart = false;
        UpdateHeldItemVisuals();
    }

    // Status Heavy Carry (Mayat Animatronik) & Cart Seated
    private bool isCarryingHeavyObject = false;
    private bool isSeatedInCart = false;
    public bool IsCarryingHeavyObject => isCarryingHeavyObject;
    public bool IsSeatedInCart => isSeatedInCart;

    public void SetHeavyCarry(bool isCarrying)
    {
        isCarryingHeavyObject = isCarrying;
        if (isCarrying)
        {
            UnequipToEmptyHands();
        }
    }

    public void SetCartSeated(bool isSeated)
    {
        isSeatedInCart = isSeated;
        if (isSeated)
        {
            UnequipToEmptyHands();
        }
    }

    private void Update()
    {
        HandleSlotSelectionInput();
        HandleDropInput();
    }

    private void HandleSlotSelectionInput()
    {
        if (isCarryingHeavyObject || isSeatedInCart) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleEquipSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleEquipSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleEquipSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleEquipSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleEquipSlot(4);
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
        UpdateHeldItemVisuals();
    }

    public void UnequipToEmptyHands()
    {
        activeEquippedSlotIndex = -1;
        UpdateHeldItemVisuals();
    }

    public bool AddItemToFirstAvailableSlot(string itemName)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (slotItemNames[i] == "Empty")
            {
                slotItemNames[i] = itemName;
                UpdateHeldItemVisuals();

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

        return false;
    }

    public void DropActiveItem()
    {
        if (activeEquippedSlotIndex < 0) return;
        
        string itemToDrop = slotItemNames[activeEquippedSlotIndex];
        if (itemToDrop == "Empty") return;

        string cleanItemName = itemToDrop.Replace("(Clone)", "").Trim();
        int spaceIdx = cleanItemName.IndexOf(' ');
        if (spaceIdx > 0) cleanItemName = cleanItemName.Substring(0, spaceIdx);

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

        if (prefabToSpawn != null)
        {
            Vector3 spawnPos = Camera.main.transform.position + (Camera.main.transform.forward * 0.8f) - (Vector3.up * 0.1f);
            Quaternion spawnRot = Camera.main.transform.rotation;
            GameObject droppedObj = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            
            droppedObj.name = cleanItemName;
            droppedObj.SetActive(true);

            Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
            if (rb == null) rb = droppedObj.AddComponent<Rigidbody>();
            
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 throwForce = (Camera.main.transform.forward * dropForwardForce) + (Vector3.up * 2.0f);
            rb.AddForce(throwForce, ForceMode.Impulse);

            rb.AddTorque(Random.insideUnitSphere * 3.0f, ForceMode.Impulse);

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
            }
        }

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

    public int ActiveSlotIndex => activeEquippedSlotIndex;
    public string ActiveItemName => (activeEquippedSlotIndex >= 0 && activeEquippedSlotIndex < maxSlots) ? slotItemNames[activeEquippedSlotIndex] : "Empty";

    public void ConsumeActiveItem()
    {
        if (activeEquippedSlotIndex < 0) return;
        slotItemNames[activeEquippedSlotIndex] = "Empty";
        UnequipToEmptyHands();
    }
}