using UnityEngine;

/// <summary>
/// Dipasang pada ranjang Gurney RS (atau MeshGurney).
/// Mendeteksi apakah pemain sedang menggotong mayat animatronik.
/// Saat pemain menekan/menahan [E]:
/// 1. Visual HeldAnimatronik di depan kamera player dimatikan (SetActive false).
/// 2. Objek AnimatronikDiGurney di atas kasur dinyalakan (SetActive true).
/// 3. Sabuk alat 1-5 dibuka kembali (tangan bebas).
/// 4. Task Manager Notebook TAB langsung dicoret selesai [X]!
/// </summary>
public class GurneyCorpseReceiver : MonoBehaviour, IInteractable
{
    [Header("Task Manager Settings")]
    [Tooltip("ID Tugas di Notebook TAB (misal: PUT_ANIMATRONIC_GURNEY / CARRY_BODY)")]
    [SerializeField] private string taskId = "PUT_ANIMATRONIC_GURNEY";
    [SerializeField] private float placeHoldTime = 1.0f;

    [Header("Gurney Visual Target")]
    [Tooltip("Drag objek AnimatronikDiGurney (yang berbaring di atas kasur) ke sini")]
    [SerializeField] private GameObject animatronikDiGurney;

    [Tooltip("Drag objek HeldAnimatronik (yang ada di player -> Main Camera) ke sini")]
    [SerializeField] private GameObject heldCorpseVisual;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeSFX;

    private bool isCorpsePlaced = false;

    private void Start()
    {
        // Pastikan ada Collider agar bisa terkena Raycast [E]
        Collider col = GetComponent<Collider>();
        if (col == null && GetComponentInChildren<Collider>() == null)
        {
            Debug.LogWarning($"[GURNEY WARNING] Objek '{gameObject.name}' belum memiliki Collider (BoxCollider)! Tambahkan BoxCollider agar bisa diinteraksi.");
        }

        // Pastikan di awal game animatronik di atas kasur tersembunyi
        if (animatronikDiGurney != null)
        {
            animatronikDiGurney.SetActive(false);
        }
    }

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (isCorpsePlaced) return "Gurney RS [ANIMATRONIK SUDAH DILETAKKAN]";

        // Cek apakah player sedang menggotong mayat
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        bool isCarrying = toolBelt != null && toolBelt.IsCarryingHeavyObject;

        if (!isCarrying)
        {
            return "Gurney RS [BUTUH MEMBAWA MAYAT ANIMATRONIK]";
        }

        return $"Tahan [E] {placeHoldTime:F0}d Letakkan Animatronik di Gurney";
    }

    public float HoldDuration
    {
        get
        {
            if (isCorpsePlaced) return 0f;
            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            bool isCarrying = toolBelt != null && toolBelt.IsCarryingHeavyObject;
            return isCarrying ? placeHoldTime : 0f;
        }
    }

    public void OnInteract()
    {
        if (isCorpsePlaced) return;

        // Cek apakah pemain sedang menggotong mayat
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        bool isCarrying = toolBelt != null && toolBelt.IsCarryingHeavyObject;

        if (!isCarrying)
        {
            Debug.LogWarning("[GURNEY] Anda harus menggotong mayat animatronik terlebih dahulu!");
            return;
        }

        isCorpsePlaced = true;

        // 1. Matikan visual mayat di tangan/dada pemain
        if (heldCorpseVisual != null)
        {
            heldCorpseVisual.SetActive(false);
        }

        // 2. Munculkan mayat berbaring rapi di atas kasur Gurney
        if (animatronikDiGurney != null)
        {
            animatronikDiGurney.SetActive(true);
        }

        // 3. Buka kembali sabuk alat pemain (tombol 1-5 aktif lagi)
        if (toolBelt != null)
        {
            toolBelt.SetHeavyCarry(false);
        }

        // 4. Mainkan SFX
        if (audioSource != null && placeSFX != null)
        {
            audioSource.PlayOneShot(placeSFX);
        }

        Debug.Log("<color=green>[GURNEY SUCCESS] Mayat animatronik berhasil diletakkan berbaring di atas Gurney RS!</color>");

        // 5. Laporkan ke TaskManager Notebook TAB
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null)
            {
                taskMgr.CompleteTask(taskId);
            }
        }
    }
}
