using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger Khusus Cutscene Nanjak Tebing (Climb Hill Trigger).
/// Kecepatan Kereta = 5 m/s, Total Durasi Cutscene = 14 Detik (9s Nanjak + 5s Rel Datar).
/// Fast Snap Pitch Up Selesai di Detik 0.8s (Detik 0-0.3s Hold Lurus)!
/// </summary>
public class CartClimbHillTrigger : MonoBehaviour
{
    [Header("Hill Climb Settings (Nanjak)")]
    [Tooltip("Kecepatan kereta saat nanjak tebing (m/s). Default Presisi: 5.0 m/s")]
    [SerializeField] private float climbSpeed = 5.0f;

    [Tooltip("Total durasi cutscene nanjak dalam detik. Default Presisi: 14.0 detik (9s Nanjak + 5s Datar)")]
    [SerializeField] private float climbDurationInSeconds = 14.0f;

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

        StartCoroutine(ClimbHillRoutine(cart));
    }

    private IEnumerator ClimbHillRoutine(DarkRideCartController cart)
    {
        hasTriggered = true;

        GameInputLock.LockInput();
        CutsceneLetterboxUI.Show();

        // 🎥 PLAY KOREOGRAFI TIMELINE 14 DETIK (9s NANJAK + 5s REL DATAR, SNAP PITCH UP DETIK 0.8s)
        FirstPersonCamera fpc = cart.GetComponentInChildren<FirstPersonCamera>();
        if (fpc == null) fpc = FindObjectOfType<FirstPersonCamera>();
        if (fpc != null)
        {
            fpc.PlayClimbGazeSequence(cart, climbDurationInSeconds);
        }

        if (climbChainSFX != null && audioSource != null)
        {
            audioSource.clip = climbChainSFX;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 1. 🧗 FASE 9 DETIK AWAL: NANJAK DENGAN KECEPATAN 5 M/S
        cart.SetTemporarySpeed(climbSpeed);
        float climbTimer = 0f;
        while (climbTimer < 9.0f)
        {
            if (Time.timeScale > 0f) climbTimer += Time.deltaTime;
            yield return null;
        }

        // 2. 🛤️ FASE 5 DETIK TERAKHIR: SUDAH DATAR DI LINTASAN ATAS & PERLAMBATAN MULUS
        float decelTimer = 0f;
        float decelDuration = 5.0f;
        float startSpeed = climbSpeed;

        while (decelTimer < decelDuration)
        {
            decelTimer += Time.deltaTime;
            float t = decelTimer / decelDuration;
            float currentDecelSpeed = Mathf.Lerp(startSpeed, 4.0f, Mathf.SmoothStep(0f, 1f, t));
            cart.SetTemporarySpeed(currentDecelSpeed);
            yield return null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 3. 🏁 SELESAI CUTSCENE DATAR, KEMBALI KE KONTROL NORMAL
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
