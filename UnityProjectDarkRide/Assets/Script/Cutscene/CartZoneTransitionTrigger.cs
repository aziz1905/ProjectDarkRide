using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger Cutscene Perpindahan Zona (Zone Transition Trigger).
/// Hanya terpicu jika tersentuh KEPALA KERETA (Cart_head) saat pemain duduk di dalam kereta.
/// Memunculkan bilah hitam sinematik (CutsceneLetterboxUI) & UI Nama Zona Baru secara mulus.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CartZoneTransitionTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("Durasi cutscene perpindahan zona dalam detik (default: 4.0s)")]
    [SerializeField] private float transitionDuration = 4.0f;

    [Tooltip("Kecepatan sementara kereta saat melintasi perpindahan zona (m/s). Set 0 jika tidak ingin mengubah kecepatan.")]
    [SerializeField] private float transitionCartSpeed = 3.5f;

    [Header("Camera Gaze (Opsional)")]
    [Tooltip("Arah sudut menoleh horizontal saat memasuki zona (misal: +35° menoleh kanan, -35° menoleh kiri, 0 = lurus)")]
    [SerializeField] private float gazeYawAngle = 35.0f;

    [Tooltip("Arah sudut pitch vertikal saat memasuki zona (misal: -10° agak ke atas, +10° agak ke bawah, 0 = lurus)")]
    [SerializeField] private float gazePitchAngle = -10.0f;

    [Header("Trigger Mode")]
    [Tooltip("Jika CENTANG, trigger ini hanya terpicu 1x per shift")]
    [SerializeField] private bool triggerOnce = true;

    [Header("SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip zoneTransitionSFX;

    private bool hasTriggered = false;
    private bool isRoutineRunning = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRoutineRunning) return;
        if (triggerOnce && hasTriggered) return;

        // Cari controller kereta dari collider yang masuk
        DarkRideCartController cart = other.GetComponentInParent<DarkRideCartController>();
        if (cart == null) cart = other.GetComponentInChildren<DarkRideCartController>();
        if (cart == null) cart = other.GetComponent<DarkRideCartController>();

        if (cart == null || !cart.IsPlayerSeated) return;

        // FILTER HANYA KEPALA KERETA (Cart_head) / PLAYER
        bool isHeadOrPlayer = other.CompareTag("Player") ||
                             other.name.ToLower().Contains("head") ||
                             other.name.ToLower().Contains("player") ||
                             (cart.HeadCartTransform != null && (other.transform == cart.HeadCartTransform || other.transform.IsChildOf(cart.HeadCartTransform)));

        if (!isHeadOrPlayer) return;

        // KUNCI LANGSUNG AGAR GERBONG BELAKANG TIDAK MEMICU ULANG
        hasTriggered = true;
        isRoutineRunning = true;

        StartCoroutine(ZoneTransitionRoutine(cart));
    }

    private IEnumerator ZoneTransitionRoutine(DarkRideCartController cart)
    {
        GameInputLock.LockInput();
        CutsceneLetterboxUI.Show(); // Bilah hitam instan muncul di awal (letterbox saja)

        // SFX Transisi Zona
        if (zoneTransitionSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(zoneTransitionSFX);
        }

        // Atur kecepatan sementara kereta jika diisi
        if (transitionCartSpeed > 0f)
        {
            cart.SetTemporarySpeed(transitionCartSpeed);
        }

        // Panggil gerakan tatapan kamera di FirstPersonCamera
        FirstPersonCamera fpc = cart.GetComponentInChildren<FirstPersonCamera>();
        if (fpc == null) fpc = FindObjectOfType<FirstPersonCamera>();
        if (fpc != null)
        {
            fpc.PlayZoneTransitionGazeSequence(cart, transitionDuration, gazePitchAngle, gazeYawAngle);
        }

        // Tahan sesuai durasi transisi zona
        float transTimer = 0f;
        while (transTimer < transitionDuration)
        {
            if (Time.timeScale > 0f) transTimer += Time.deltaTime;
            yield return null;
        }

        // Kembalikan ke kontrol normal
        if (transitionCartSpeed > 0f)
        {
            cart.ResetSpeedToDefault();
        }

        CutsceneLetterboxUI.Hide(); // Bilah hitam fade out di akhir
        GameInputLock.UnlockInput();

        isRoutineRunning = false;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        isRoutineRunning = false;
    }
}
