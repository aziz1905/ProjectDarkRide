using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger Khusus Cutscene Nanjak Tebing (Climb Hill Trigger).
/// Menggunakan cart.ForwardRotation agar kamera berputar 100% langsung lurus menatap depan terowongan tanpa belok kiri dulu.
/// </summary>
public class CartClimbHillTrigger : MonoBehaviour
{
    [Header("Hill Climb Settings (Nanjak)")]
    [Tooltip("Kecepatan kereta saat nanjak tebing (m/s). Default: 1.5 m/s")]
    [SerializeField] private float climbSpeed = 1.5f;

    [Tooltip("Durasi nanjak tebing dalam detik sebelum kecepatan kembali normal di puncak")]
    [SerializeField] private float climbDurationInSeconds = 3.5f;

    [Header("Trigger Mode")]
    [Tooltip("Jika CENTANG, trigger ini hanya terpicu 1x per shift")]
    [SerializeField] private bool triggerOnce = true;
    private bool hasTriggered = false;

    [Header("SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip climbChainSFX;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;

        DarkRideCartController cart = other.GetComponentInParent<DarkRideCartController>();
        if (cart == null) cart = other.GetComponentInChildren<DarkRideCartController>();
        if (cart == null) cart = other.GetComponent<DarkRideCartController>();

        if (cart == null)
        {
            DarkRideCartController activeCart = FindObjectOfType<DarkRideCartController>();
            if (activeCart != null && activeCart.IsPlayerSeated)
            {
                if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.name.ToLower().Contains("player") || other.GetComponentInParent<PlayerMovement>() != null)
                {
                    cart = activeCart;
                }
            }
        }

        if (cart == null || !cart.IsPlayerSeated) return;

        StartCoroutine(ClimbHillRoutine(cart));
    }

    private IEnumerator ClimbHillRoutine(DarkRideCartController cart)
    {
        hasTriggered = true;

        // 🛑 Kunci input driver saat di tebing
        GameInputLock.LockInput();

        // 🎥 ROTASI KAMERA 100% LURUS KE DEPAN TEROWONGAN (MENGGUNAKAN FORWARD ROTATION)
        FirstPersonCamera fpc = cart.GetComponentInChildren<FirstPersonCamera>();
        if (fpc == null) fpc = FindObjectOfType<FirstPersonCamera>();
        if (fpc != null)
        {
            fpc.SmoothRotateToTarget(cart.ForwardRotation, 0.8f);
        }

        // 🧗 Memutar SFX Rantai Nanjak
        if (climbChainSFX != null && audioSource != null)
        {
            audioSource.clip = climbChainSFX;
            audioSource.loop = true;
            audioSource.Play();
        }

        cart.SetTemporarySpeed(climbSpeed);

        yield return new WaitForSeconds(climbDurationInSeconds);

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        cart.ResetSpeedToDefault();
        GameInputLock.UnlockInput();
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
