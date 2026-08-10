using UnityEngine;

/// <summary>
/// Sistem Interaksi First-Person Player (Raycast Laser + SphereCast Fleksibel).
/// Mendukung tombol [E] untuk interaksi umum dunia dan [Klik Kiri] untuk interaksi alat khusus.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast / SphereCast Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactDistance = 2.5f; // Jarak jangkauan (2.5 meter)
    
    [Tooltip("Radius bantuan jika pandangan agak meleset dari objek (0.3 - 0.5 meter)")]
    [SerializeField] private float interactRadius = 0.35f;
    
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("Keybindings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;          // Tombol E untuk interaksi umum (Buka Pintu, dll)
    [SerializeField] private KeyCode toolUseKey = KeyCode.Mouse0;       // Klik Kiri untuk interaksi alat khusus (Buka Kunci, dll)

    // Internal State
    private IInteractable currentInteractable;
    private float holdTimer = 0f;
    private bool isHolding = false;
    private bool hasTriggered = false;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main != null ? Camera.main.transform : transform;
    }

    private void Update()
    {
        DetectInteractable();
        HandleInteraction();
    }

    private void DetectInteractable()
    {
        Vector3 origin = cameraTransform.position;
        Vector3 forward = cameraTransform.forward;
        RaycastHit hit;
        IInteractable foundInteractable = null;

        // 🎯 TAHAP 1: Raycast Tajam (Laser Presisi)
        if (Physics.Raycast(origin, forward, out hit, interactDistance, interactableLayer, QueryTriggerInteraction.Collide))
        {
            foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
            if (foundInteractable == null)
                foundInteractable = hit.collider.GetComponentInChildren<IInteractable>();
            if (foundInteractable == null)
                foundInteractable = hit.collider.GetComponent<IInteractable>();
        }

        // 🌐 TAHAP 2: SphereCast Bantuan
        if (foundInteractable == null && interactRadius > 0f)
        {
            if (Physics.SphereCast(origin, interactRadius, forward, out hit, interactDistance, interactableLayer, QueryTriggerInteraction.Collide))
            {
                foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
                if (foundInteractable == null)
                    foundInteractable = hit.collider.GetComponentInChildren<IInteractable>();
                if (foundInteractable == null)
                    foundInteractable = hit.collider.GetComponent<IInteractable>();
            }
        }

        // Simpan Status Interactable yang Ditemukan
        if (foundInteractable != null)
        {
            if (currentInteractable != foundInteractable)
            {
                ResetHold();
                currentInteractable = foundInteractable;
            }
            return;
        }

        ResetHold();
    }

    private void HandleInteraction()
    {
        if (currentInteractable == null) return;

        string prompt = currentInteractable.GetInteractPrompt();

        // 🔴 PROTEKSI: Jika UI Prompt bertuliskan [BUTUH ALAT] atau [BAHAYA] atau [TERKUNCI],
        // BLOKIR TOTAL seluruh aksi!
        if (prompt.StartsWith("[BUTUH ALAT]") || prompt.StartsWith("[BAHAYA]") || prompt.StartsWith("[TERKUNCI]"))
        {
            ResetHold();
            return;
        }

        // 🟢 DETEKSI TOMBOL: Apakah interaksi ini meminta [Klik Kiri] / [LMB] atau tombol [E]?
        bool isToolClick = prompt.Contains("Klik Kiri") || prompt.Contains("[LMB]") || prompt.Contains("Click");
        KeyCode activeKey = isToolClick ? toolUseKey : interactKey;

        // MODE 1: TAP INSTAN
        if (currentInteractable.HoldDuration <= 0.05f)
        {
            if (Input.GetKeyDown(activeKey))
            {
                Debug.Log("[INTERACT SUCCESS] Interaksi berhasil pada: " + prompt);
                currentInteractable.OnInteract();
            }
        }
        // MODE 2: HOLD TAHAN
        else
        {
            if (Input.GetKeyDown(activeKey))
            {
                isHolding = true;
                hasTriggered = false;
                holdTimer = 0f;
            }

            if (Input.GetKey(activeKey) && isHolding && !hasTriggered)
            {
                holdTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(holdTimer / currentInteractable.HoldDuration);
                Debug.Log($"[HOLD PROGRESS] {(progress * 100):F0}%");

                if (holdTimer >= currentInteractable.HoldDuration)
                {
                    hasTriggered = true;
                    Debug.Log("[HOLD SUCCESS] 100% Selesai pada: " + prompt);
                    currentInteractable.OnInteract();
                    ResetHold();
                }
            }

            if (Input.GetKeyUp(activeKey))
            {
                ResetHold();
            }
        }
    }

    private void ResetHold()
    {
        currentInteractable = null;
        isHolding = false;
        hasTriggered = false;
        holdTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null) return;
        
        Gizmos.color = Color.cyan;
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward * interactDistance;
        Gizmos.DrawRay(origin, direction);
        Gizmos.DrawRay(origin + cameraTransform.right * 0.015f, direction);
        Gizmos.DrawRay(origin - cameraTransform.right * 0.015f, direction);
        Gizmos.DrawRay(origin + cameraTransform.up * 0.015f, direction);
        Gizmos.DrawRay(origin - cameraTransform.up * 0.015f, direction);
    }
}