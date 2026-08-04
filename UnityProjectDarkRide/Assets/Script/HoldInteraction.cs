using UnityEngine;
using UnityEngine.Events;

public class HoldInteraction : MonoBehaviour, IInteractable
{
    [Header("Interaction Prompts")]
    [SerializeField] private string promptText = "Tahan [E] 2d Buka Pintu";
    [SerializeField] private float holdTime = 2.0f; // Durasi Tahan E (detik)

    [Header("Item Requirements (Opsional)")]
    [Tooltip("Kosongkan jika interaksi ini bisa dilakukan tanpa membawa alat apa pun")]
    [SerializeField] private string requiredItemName = ""; 

    [Header("Options After Hold Completed")]
    [SerializeField] private bool isCompleted = false;
    [SerializeField] private bool disableObjectOnComplete = false;
    [SerializeField] private bool changeColorOnComplete = false;
    [SerializeField] private Color completedColor = Color.cyan;

    [Header("Custom Action (Event)")]
    public UnityEvent OnHoldCompleted;

    // --- INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt() => isCompleted ? "[SELESAI]" : promptText;
    public float HoldDuration => isCompleted ? 0f : holdTime;

    public void OnInteract()
    {
        if (isCompleted) return;

        // Periksa alat jika requiredItemName diisi
        if (!string.IsNullOrEmpty(requiredItemName))
        {
            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            if (toolBelt == null || toolBelt.ActiveItemName != requiredItemName)
            {
                string itemInHand = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
                Debug.LogWarning($"[INTERACTION GAGAL] Butuh alat '{requiredItemName}' di Tool Belt! (Alat saat ini: '{itemInHand}')");
                return;
            }
        }

        // BERHASIL
        isCompleted = true;
        Debug.Log($"[HOLD COMPLETED] Interaksi Tahan E pada '{gameObject.name}' Berhasil!");

        // Eksekusi Event/Aksi Custom
        OnHoldCompleted?.Invoke();

        if (changeColorOnComplete)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.material.color = completedColor;
        }

        if (disableObjectOnComplete)
        {
            gameObject.SetActive(false);
        }
    }
}