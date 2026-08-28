using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger Khusus Cutscene Meluncur Turun Tebing (Drop Hill Trigger).
/// Pelambatan Mulus Di Puncak (Smooth Deceleration 0.6s) Sebelum Meluncur Tajam!
/// </summary>
public class CartDropHillTrigger : MonoBehaviour
{
    [Header("Summit Suspense Settings (Puncak)")]
    [Tooltip("Waktu diam/pause sejenak di puncak tebing sebelum meluncur (detik). Default: 0.5 detik")]
    [SerializeField] private float summitPauseDuration = 0.5f;

    [Header("Coaster Drop Settings (Meluncur Turun)")]
    [Tooltip("Kecepatan meluncur tajam saat turun tebing (m/s). Default: 8.0 m/s")]
    [SerializeField] private float dropSpeed = 8.0f;

    [Tooltip("Durasi meluncur turun tebing dalam detik (descent). Default: 8.0 detik (Total cutscene 13 detik = 8s drop + 5s flat)")]
    [SerializeField] private float dropDurationInSeconds = 8.0f;

    [Header("Flat Section Settings (Datar)")]
    [Tooltip("Durasi flat setelah descent sebelum kembali normal. Default: 3.0 detik")]
    [SerializeField] private float flatDurationInSeconds = 3.0f;

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
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private bool isRoutineRunning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isRoutineRunning) return;
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

        // Hanya izinkan pemicuan jika objek yang menyentuh collider trigger adalah Kepala Kereta (Head Cart) / Player
        bool isHeadOrPlayer = other.CompareTag("Player") ||
                             other.name.ToLower().Contains("head") ||
                             other.name.ToLower().Contains("player") ||
                             (cart.HeadCartTransform != null && (other.transform == cart.HeadCartTransform || other.transform.IsChildOf(cart.HeadCartTransform)));

        if (!isHeadOrPlayer) return;

        // KUNCI MULTIPLE SAFETY
        hasTriggered = true;
        isRoutineRunning = true;

        StartCoroutine(DropHillRoutine(cart));
    }

    private IEnumerator DropHillRoutine(DarkRideCartController cart)
    {
        hasTriggered = true;

        GameInputLock.LockInput();
        CutsceneLetterboxUI.Show();

        FirstPersonCamera fpc = cart.GetComponentInChildren<FirstPersonCamera>();
        if (fpc == null) fpc = FindObjectOfType<FirstPersonCamera>();
        if (fpc != null)
        {
            fpc.PlayDropGazeSequence(cart, summitPauseDuration, dropDurationInSeconds);
        }

        // 1. 🛑 Smooth deceleration to stop before summit (optional)
        float decelTimer = 0f;
        float decelDuration = 0.6f;
        float startSpeed = 2.5f;
        while (decelTimer < decelDuration)
        {
            decelTimer += Time.deltaTime;
            float t = decelTimer / decelDuration;
            float currentDecelSpeed = Mathf.Lerp(startSpeed, 0f, Mathf.SmoothStep(0f, 1f, t));
            cart.SetTemporarySpeed(currentDecelSpeed);
            yield return null;
        }
        cart.SetTemporarySpeed(0f); // Hold at summit

        // 2. ⏸️ Pause at summit (suspense)
        float pauseSummitTimer = 0f;
        while (pauseSummitTimer < summitPauseDuration)
        {
            if (Time.timeScale > 0f) pauseSummitTimer += Time.deltaTime;
            yield return null;
        }

        // 3. 🎢 Steep descent
        if (dropWindSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(dropWindSFX);
        }
        cart.SetTemporarySpeed(dropSpeed);
        float dropTimer = 0f;
        while (dropTimer < dropDurationInSeconds)
        {
            if (Time.timeScale > 0f) dropTimer += Time.deltaTime;
            yield return null;
        }

        // 4. 🛤️ Flat section for 5 seconds (decelerate smoothly to normal speed 4.0 m/s)
        float flatTimer = 0f;
        float flatDuration = flatDurationInSeconds;
        float startFlatSpeed = dropSpeed;

        while (flatTimer < flatDuration)
        {
            flatTimer += Time.deltaTime;
            float t = flatTimer / flatDuration;
            float currentFlatSpeed = Mathf.Lerp(startFlatSpeed, 4.0f, Mathf.SmoothStep(0f, 1f, t));
            cart.SetTemporarySpeed(currentFlatSpeed);
            yield return null;
        }

        // 5. 🏁 Return to normal on flat track
        cart.ResetSpeedToDefault();
        CutsceneLetterboxUI.Hide();
        GameInputLock.UnlockInput();
        isRoutineRunning = false;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        isRoutineRunning = false;
    }
}
