using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    [Tooltip("Nama bersih item untuk ToolBelt (misal: TrashBag, FusePack, Cloth, Bulb, Key, Hammer). Jika kosong, otomatis ambil nama Objek/Parent.")]
    [SerializeField] private string itemName = "";
    [SerializeField] private string customPrompt = "";

    public string GetInteractPrompt()
    {
        if (!string.IsNullOrEmpty(customPrompt))
            return customPrompt;
        
        string nameToShow = GetCleanItemName();
        return "Tekan [E] Ambil " + nameToShow;
    }

    public float HoldDuration => 0f; // TAP Instan

    public void OnInteract()
    {
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        
        if (toolBelt != null)
        {
            string nameToSave = GetCleanItemName();
            
            // MASUKKAN KE SABUK
            bool success = toolBelt.AddItemToFirstAvailableSlot(nameToSave);
            
            if (success)
            {
                // Sembunyikan induk utama jika dipasang di InteractionArea
                GameObject rootObj = transform.parent != null ? transform.parent.gameObject : gameObject;
                rootObj.SetActive(false); 
            }
        }
    }

    private string GetCleanItemName()
    {
        if (!string.IsNullOrEmpty(itemName))
            return itemName;

        // JIKA SCRIPT BERADA DI CHILD OBJECT APA PUN, OTOMATIS AMBIL NAMA PARENT INDUKNYA
        string rawName = (transform.parent != null) ? transform.parent.name : gameObject.name;
        return CleanName(rawName);
    }

    private string CleanName(string rawName)
    {
        string cleaned = rawName.Replace("(Clone)", "").Trim();
        int spaceIndex = cleaned.IndexOf(' ');
        if (spaceIndex > 0)
        {
            cleaned = cleaned.Substring(0, spaceIndex);
        }
        return cleaned;
    }
}