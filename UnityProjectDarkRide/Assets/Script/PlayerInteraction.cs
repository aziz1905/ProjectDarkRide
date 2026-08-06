using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast / SphereCast Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactDistance = 2.5f; // Jarak jangkauan (2.5 meter)
    
    [Tooltip("Ubah nilai ini untuk memperlebar/memperkecil area deteksi pandangan!")]
    [SerializeField] private float interactRadius = 0.3f;   // LEBAR DETEKSI (0.3 meter)
    
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
        DetectInteractable();
        HandleInteraction();
    }

    private void DetectInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        // MENGGUNAKAN SPHERECAST AGAR DETEKSI PANDANGAN JAUH LEBIH LEBAR & MUDAH!
        if (Physics.SphereCast(ray, interactRadius, out hit, interactDistance, interactableLayer))
        {
            // PENCARIAN BULLETPROOF: Cek di Objek Collider, Parent, maupun Child!
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInChildren<IInteractable>();
            }

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

        // MODE 1: TAP [E] INSTAN
        if (currentInteractable.HoldDuration <= 0.05f)
        {
            if (Input.GetKeyDown(interactKey))
            {
                Debug.Log("[TAP SUCCESS] Tekan E Instan pada: " + currentInteractable.GetInteractPrompt());
                currentInteractable.OnInteract();
            }
        }
        // MODE 2: HOLD [E] TAHAN
        else
        {
            if (Input.GetKeyDown(interactKey))
            {
                isHolding = true;
                hasTriggered = false;
                holdTimer = 0f;
            }

            if (Input.GetKey(interactKey) && isHolding && !hasTriggered)
            {
                holdTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(holdTimer / currentInteractable.HoldDuration);
                Debug.Log($"[HOLD E] Progress: {(progress * 100):F0}%");

                if (holdTimer >= currentInteractable.HoldDuration)
                {
                    hasTriggered = true;
                    Debug.Log("[HOLD SUCCESS] Tahan E 100% Selesai pada: " + currentInteractable.GetInteractPrompt());
                    currentInteractable.OnInteract();
                    ResetHold();
                }
            }

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

    // Visualisasi area bola deteksi di Scene View
    private void OnDrawGizmosSelected()
    {
         if (cameraTransform == null) return;
        
        Gizmos.color = Color.cyan; // Warna laser Cyan terang
        
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward * interactDistance;
        // Menggambar garis laser lurus tebal dari kamera ke depan (2.5 meter)
        Gizmos.DrawRay(origin, direction);
        Gizmos.DrawRay(origin + cameraTransform.right * 0.015f, direction);
        Gizmos.DrawRay(origin - cameraTransform.right * 0.015f, direction);
        Gizmos.DrawRay(origin + cameraTransform.up * 0.015f, direction);
        Gizmos.DrawRay(origin - cameraTransform.up * 0.015f, direction);
    }
}