using System.Collections;
using UnityEngine;

/// <summary>
/// Script Pintu 2 Arah Presisi Tinggi (Two-Way Dynamic Push Door).
/// Dilengkapi Anti-Spam: Tombol [E] terkunci selama pintu sedang bergerak membuka / menutup.
/// </summary>
public class TwoWayDoor : MonoBehaviour, IInteractable
{
    public enum DoorFacingAxis
    {
        X_Axis_Right,   // Untuk pintu dengan tebal di sumbu X (Paling Umum)
        Z_Axis_Forward  // Untuk pintu dengan tebal di sumbu Z
    }

    [Header("Door Status")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string requiredKeyName = "Key";

    [Header("Rotation Settings")]
    [Tooltip("Sudut terbuka pintu (misal: 90 derajat)")]
    [SerializeField] private float openAngle = 90f;
    
    [Tooltip("Kecepatan ayunan pintu membuka/menutup")]
    [SerializeField] private float swingSpeed = 4f;

    [Tooltip("CENTANG jika ingin membalik arah ayunan pintu")]
    [SerializeField] private bool invertSwingDirection = false;

    [Header("Door Plane Alignment")]
    [Tooltip("Sumbu ketebalan pintu (Tegak lurus daun pintu). Default: X_Axis_Right")]
    [SerializeField] private DoorFacingAxis doorThicknessAxis = DoorFacingAxis.X_Axis_Right;

    [Header("Pivot Transform (Opsional)")]
    [Tooltip("Drag Transform Engsel/Pivot Pintu jika pintu ini bukan objek engsel utamanya")]
    [SerializeField] private Transform doorHingeTransform;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;

    // Internal State
    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private Coroutine swingCoroutine;
    private Transform hinge;
    private bool isAnimating = false; // 🛡️ Kunci Anti-Spam (True saat pintu sedang berayun)

    private void Start()
    {
        hinge = doorHingeTransform != null ? doorHingeTransform : transform;
        closedRotation = hinge.localRotation;
        targetRotation = closedRotation;
    }

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        // 1. JIKA SEDANG BERAYUN (MEMBUKA / MENUTUP): Sembunyikan Prompt agar tidak di-spam
        if (isAnimating)
        {
            return "";
        }

        // 2. JIKA PINTU TERKUNCI: Gunakan Klik Kiri [LMB]
        if (isLocked)
        {
            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
            bool hasKey = currentItem.Trim().Equals(requiredKeyName.Trim(), System.StringComparison.OrdinalIgnoreCase);

            return hasKey ? $"Klik Kiri [LMB] Buka Gembok ({requiredKeyName})" : $"[TERKUNCI] Membutuhkan '{requiredKeyName}' di Tangan!";
        }

        // 3. JIKA PINTU TIDAK TERKUNCI: Gunakan Tombol [E]
        return isOpen ? "Tekan [E] Tutup Pintu" : "Tekan [E] Buka Pintu";
    }

    public float HoldDuration => 0f; // Tap Instan

    public void OnInteract()
    {
        // 🛡️ ANTI-SPAM: Jika pintu sedang berayun membuka/menutup, TOLAK input tombol [E]!
        if (isAnimating) return;

        // 1. CEK KUNCI JIKA PINTU TERKUNCI
        if (isLocked)
        {
            ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
            string currentItem = toolBelt != null ? toolBelt.ActiveItemName : "Kosong";
            bool hasKey = currentItem.Trim().Equals(requiredKeyName.Trim(), System.StringComparison.OrdinalIgnoreCase);

            if (!hasKey)
            {
                Debug.LogWarning("[DOOR LOCKED] Pintu terkunci! Butuh kunci.");
                if (audioSource != null && lockedSound != null) audioSource.PlayOneShot(lockedSound);
                return;
            }

            // Berhasil Buka Kunci
            isLocked = false;
            Debug.Log("[DOOR UNLOCKED] Gembok pintu berhasil dibuka menggunakan Kunci!");

            // Hapus Kunci dari Sabuk
            if (toolBelt != null)
            {
                toolBelt.ConsumeActiveItem();
            }

            if (audioSource != null && openSound != null) audioSource.PlayOneShot(openSound);
            return;
        }

        // 2. TOGGLE BUKA / TUTUP PINTU 2 ARAH (Via Tombol E)
        isOpen = !isOpen;

        if (isOpen)
        {
            Transform playerCamera = Camera.main != null ? Camera.main.transform : null;
            Vector3 playerPos = playerCamera != null ? playerCamera.position : transform.position;

            Vector3 toPlayer = playerPos - hinge.position;
            toPlayer.y = 0f;

            Vector3 doorNormal = (doorThicknessAxis == DoorFacingAxis.X_Axis_Right) ? hinge.right : hinge.forward;
            float side = Vector3.Dot(doorNormal, toPlayer);

            float calculatedAngle = (side >= 0f) ? -openAngle : openAngle;

            if (invertSwingDirection)
            {
                calculatedAngle = -calculatedAngle;
            }

            targetRotation = closedRotation * Quaternion.Euler(0f, calculatedAngle, 0f);
            
            if (audioSource != null && openSound != null) audioSource.PlayOneShot(openSound);
            Debug.Log($"[DOOR OPEN] Pintu terbuka mendorong ke depan dengan sudut {calculatedAngle} derajat!");
        }
        else
        {
            targetRotation = closedRotation;
            
            if (audioSource != null && closeSound != null) audioSource.PlayOneShot(closeSound);
            Debug.Log("[DOOR CLOSE] Pintu ditutup kembali.");
        }

        if (swingCoroutine != null) StopCoroutine(swingCoroutine);
        swingCoroutine = StartCoroutine(AnimateDoorSwing());
    }

    private IEnumerator AnimateDoorSwing()
    {
        isAnimating = true; // 🔒 KUNCI PINTU (Sedang bergerak)

        while (Quaternion.Angle(hinge.localRotation, targetRotation) > 0.5f)
        {
            hinge.localRotation = Quaternion.Slerp(hinge.localRotation, targetRotation, Time.deltaTime * swingSpeed);
            yield return null;
        }

        hinge.localRotation = targetRotation;
        isAnimating = false; // 🔓 BUKA KUNCI (Gerakan selesai, tombol [E] siap ditekan lagi)
    }

    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked;
    public bool IsAnimating => isAnimating;
}
