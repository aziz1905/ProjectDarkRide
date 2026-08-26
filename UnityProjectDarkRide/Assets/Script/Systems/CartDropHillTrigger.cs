using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger Khusus Cutscene Meluncur Turun Tebing (Drop Hill Trigger).
/// Menggunakan cart.ForwardRotation agar kamera berputar 100% langsung lurus menatap depan terowongan tanpa belok kiri dulu.
/// </summary>
public class CartDropHillTrigger : MonoBehaviour
{
    [Header("Summit Suspense Settings (Puncak)")]
    [Tooltip("Waktu diam/pause sejenak di puncak tebing sebelum meluncur (detik). Default: 0.5 detik")]
    [SerializeField] private float summitPauseDuration = 0.5f;

    [Header("Coaster Drop Settings (Meluncur Turun)")]
    [Tooltip("Kecepatan meluncur tajam saat turun tebing (m/s). Default: 8.0 m/s")]
    [SerializeField] private float dropSpeed = 8.0f;

    [Tooltip("Durasi meluncur turun tebing dalam detik sebelum kecepatan kembali normal")]
    [SerializeField] private float dropDurationInSeconds = 2.5f;

    [Header("Trigger Mode")]
    [Tooltip("Jika CENTANG, trigger ini hanya terpicu 1x per shift")]
    [SerializeField] private bool triggerOnce = true;
    private bool hasTriggered = false;

    [Header("SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dropWindSFX;

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

        StartCoroutine(DropHillRoutine(cart));
    }

    private IEnumerator DropHillRoutine(DarkRideCartController cart)
    {
        hasTriggered = true;

        // 🛑 Kunci input driver
        GameInputLock.LockInput();

        // 🎥 ROTASI KAMERA 100% LURUS KE DEPAN TEROWONGAN (MENGGUNAKAN FORWARD ROTATION)
        FirstPersonCamera fpc = cart.GetComponentInChildren<FirstPersonCamera>();
        if (fpc == null) fpc = FindObjectOfType<FirstPersonCamera>();
        if (fpc != null)
        {
            fpc.SmoothRotateToTarget(cart.ForwardRotation, 0.8f);
        }

        // 1. ⏸️ Tahan sejenak di puncak tebing (Suspense Moment)
        cart.SetTemporarySpeed(0f);
        yield return new WaitForSeconds(summitPauseDuration);

        // 2. 🎢 Meluncur Tajam Turun Tebing
        if (dropWindSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(dropWindSFX);
        }

        cart.SetTemporarySpeed(dropSpeed);
        yield return new WaitForSeconds(dropDurationInSeconds);

        // 3. 🛤️ Kembali ke Kecepatan Normal di Rel Datar
        cart.ResetSpeedToDefault();
        GameInputLock.UnlockInput();
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
