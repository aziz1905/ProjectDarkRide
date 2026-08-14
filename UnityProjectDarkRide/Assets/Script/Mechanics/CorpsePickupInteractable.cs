using UnityEngine;

/// <summary>
/// Dipasang pada objek AnimatronikJatuh di lantai.
/// Saat pemain menekan/menahan [E]:
/// 1. Animatronik di lantai menghilang (SetActive false).
/// 2. Visual HeldAnimatronik di depan kamera player muncul (SetActive true).
/// 3. Sabuk alat 1-5 dikunci sementara (karena sedang menggotong mayat).
/// </summary>
public class CorpsePickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Hold Settings")]
    [SerializeField] private float pickupHoldTime = 1.0f; // Durasi tahan E (1 detik)
    [SerializeField] private string promptText = "Tahan [E] Angkat Mayat Animatronik";

    [Header("Held Corpse Visual (Di Kamera Player)")]
    [Tooltip("Drag objek HeldAnimatronik (yang ada di dalam player -> Main Camera) ke sini")]
    [SerializeField] private GameObject heldCorpseVisual;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSFX;

    private bool isPickedUp = false;

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (isPickedUp) return "";
        return promptText;
    }

    public float HoldDuration => isPickedUp ? 0f : pickupHoldTime;

    public void OnInteract()
    {
        if (isPickedUp) return;

        isPickedUp = true;

        // 1. Matikan objek mayat di lantai
        gameObject.SetActive(false);

        // 2. Munculkan visual mayat di depan dada/kamera player & MATIKAN COLLIDER-NYA agar tidak memblokir Raycast [E]
        if (heldCorpseVisual != null)
        {
            Collider[] colliders = heldCorpseVisual.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            heldCorpseVisual.SetActive(true);
        }

        // 3. Kunci sabuk alat (tombol 1-5) via ToolBeltManager
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        if (toolBelt != null)
        {
            toolBelt.SetHeavyCarry(true);
        }

        // 4. Mainkan SFX jika ada
        if (audioSource != null && pickupSFX != null)
        {
            audioSource.PlayOneShot(pickupSFX);
        }

        Debug.Log("<color=yellow>[CORPSE PICKUP] Mayat animatronik diangkat ke depan dada player!</color>");
    }
}
