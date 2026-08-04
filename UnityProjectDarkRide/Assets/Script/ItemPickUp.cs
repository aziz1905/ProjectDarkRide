using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Kosongkan jika ingin otomatis mengikuti nama objek di Hierarchy")]
    [SerializeField] private string customPrompt = "";

    public string GetInteractPrompt()
    {
        if (!string.IsNullOrEmpty(customPrompt))
            return customPrompt;
        
        return "Tekan [E] Ambil " + gameObject.name;
    }

    public float HoldDuration => 0f; // TAP Instan

    public void OnInteract()
    {
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        
        if (toolBelt != null)
        {
            string itemNameFromHierarchy = gameObject.name;
            
            // MASUKKAN KE SABUK (TANGAN TETAP KOSONG)
            bool success = toolBelt.AddItemToFirstAvailableSlot(itemNameFromHierarchy);
            
            if (success)
            {
                gameObject.SetActive(false); // Sembunyikan item
            }
        }
    }
}