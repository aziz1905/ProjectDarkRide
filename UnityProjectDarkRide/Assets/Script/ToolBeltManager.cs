using UnityEngine;

public class ToolBeltManager : MonoBehaviour
{
    [Header("Tool Belt Settings")]
    [SerializeField] private int maxSlots = 5;
    
    // -1 = Tangan Kosong | 0 = Slot 1 | 1 = Slot 2, dst.
    [SerializeField] private int activeEquippedSlotIndex = -1;

    [Header("Held Items 3D Mesh Slots")]
    [Tooltip("Drag 5 GameObject 3D Mesh alat di tangan di sini")]
    [SerializeField] private GameObject[] heldItemMeshes = new GameObject[5];

    // Array menyimpan nama item di masing-masing slot (Kosong = "Empty")
    private string[] slotItemNames = new string[5];

    private void Start()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            slotItemNames[i] = "Empty";
        }

        activeEquippedSlotIndex = -1; // Tangan Kosong saat awal game
        UpdateHeldItemVisuals();
    }

    private void Update()
    {
        HandleSlotSelectionInput();
    }

    private void HandleSlotSelectionInput()
    {
        // Toggle Equip Slot 1 - 5 via Tombol Angka 1 - 5
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleEquipSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleEquipSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleEquipSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleEquipSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleEquipSlot(4);

        // Seleksi Slot via Scrollwheel Mouse
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

    public void ToggleEquipSlot(int slotIndex)
    {
        // Jika menekan tombol slot yang sedang dipegang -> Simpan item ke sabuk (Tangan jadi Kosong)
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
        activeEquippedSlotIndex = -1; // Set Tangan Kosong
        Debug.Log("[Tool Belt] Barang Disimpan ke Sabuk.");
        UpdateHeldItemVisuals();
    }

    /// <summary>
    /// Memasukkan barang dari meja ke slot kosong pertama di sabuk (TANGAN TETAP MEMEGANG ITEM SAAT INI)
    /// </summary>
    public bool AddItemToFirstAvailableSlot(string itemName)
    {
        // Cari slot kosong pertama (Slot 1, 2, 3, 4, 5)
        for (int i = 0; i < maxSlots; i++)
        {
            if (slotItemNames[i] == "Empty")
            {
                slotItemNames[i] = itemName;
                
                string currentHolding = ActiveItemName;
                Debug.Log($"[Tool Belt] Item '{itemName}' Disimpan di Sabuk (Slot {i + 1}). (Barang di tangan saat ini: '{currentHolding}')");
                
                // BARANG DI TANGAN TETAP UTUH SAMA PERSIS (TIDAK BERUBAH)
                UpdateHeldItemVisuals();
                return true;
            }
        }

        Debug.LogWarning("[Tool Belt Penuh] Sabuk Tool Belt Anda sudah penuh (5/5)! Tidak bisa mengambil barang lagi.");
        return false;
    }

    private void UpdateHeldItemVisuals()
    {
        for (int i = 0; i < heldItemMeshes.Length; i++)
        {
            if (heldItemMeshes[i] != null)
            {
                bool isEquipped = (i == activeEquippedSlotIndex) && (slotItemNames[i] != "Empty");
                heldItemMeshes[i].SetActive(isEquipped);
            }
        }
    }

    // Getters
    public int ActiveSlotIndex => activeEquippedSlotIndex;
    public string ActiveItemName => (activeEquippedSlotIndex >= 0 && activeEquippedSlotIndex < maxSlots) ? slotItemNames[activeEquippedSlotIndex] : "Empty";
}