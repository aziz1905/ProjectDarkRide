using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactDistance = 2.5f; // Jarak jangkauan Raycast
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("Keybindings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

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
        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * interactDistance, Color.red);
        DetectInteractable();
        HandleInteraction();
    }

    private void DetectInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    ResetHold();
                    currentInteractable = interactable;
                }
                return;
            }
        }

        ResetHold();
    }

    private void HandleInteraction()
    {
        if (currentInteractable == null) return;

        // ===================================================
        // MODE 1: TAP [E] INSTAN (Untuk Ambil Barang / Saklar)
        // ===================================================
        if (currentInteractable.HoldDuration <= 0.05f)
        {
            if (Input.GetKeyDown(interactKey))
            {
                Debug.Log("[TAP SUCCESS] Tekan E Instan pada: " + currentInteractable.GetInteractPrompt());
                currentInteractable.OnInteract();
            }
        }
        // ===================================================
        // MODE 2: HOLD [E] TAHAN (Untuk Lap Cermin / Maintenance)
        // ===================================================
        else
        {
            // 1. Saat tombol [E] MULAI DITAHAN
            if (Input.GetKeyDown(interactKey))
            {
                isHolding = true;
                hasTriggered = false;
                holdTimer = 0f;
            }

            // 2. Selama tombol [E] SEDANG DITAHAN
            if (Input.GetKey(interactKey) && isHolding && !hasTriggered)
            {
                holdTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(holdTimer / currentInteractable.HoldDuration);

                // Log persentase Hold ke Console
                Debug.Log($"[HOLD E] Progress: {(progress * 100):F0}%");

                // 3. Saat Hold [E] SELESAI 100%
                if (holdTimer >= currentInteractable.HoldDuration)
                {
                    hasTriggered = true;
                    Debug.Log("[HOLD SUCCESS] Tahan E 100% Selesai pada: " + currentInteractable.GetInteractPrompt());
                    currentInteractable.OnInteract();
                    ResetHold();
                }
            }

            // 4. Jika tombol [E] DILEPAS sebelum 100% -> Reset Progress!
            if (Input.GetKeyUp(interactKey))
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

    // Visualisasi garis Raycast di Scene View
    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * interactDistance);
    }
}