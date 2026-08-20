using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Kontroler Kereta Wahana (Dark Ride Cart) Interaktif Mulus.
/// ZERO TELEPORTASI saat Start: Posisi kereta 100% diam di tempat Anda ditaruh di Editor.
/// </summary>
public class DarkRideCartController : MonoBehaviour, IInteractable
{
    [Header("Spline Tracks (Rel Kereta)")]
    [Tooltip("Drag Objek ST.Spline (Rel Stasiun Keberangkatan) ke sini")]
    [SerializeField] private SplineContainer stationSpline;

    [Tooltip("Drag Objek MainSpline (Rel Utama Wahana) ke sini")]
    [SerializeField] private SplineContainer mainSpline;

    [Header("Rotation Offset Fix")]
    [Tooltip("Sudut koreksi rotasi jika posisi kereta miring/melintang dari arah rel (misal Y = 90 atau -90)")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Header("Cart Driver & Passenger Seat")]
    [Tooltip("Transform Posisi Kursi Penumpang/Driver di dalam Kereta")]
    [SerializeField] private Transform seatTransform;

    [Header("Cart Speed & Physics")]
    [Tooltip("Kecepatan maksimal kereta (m/s)")]
    [SerializeField] private float maxSpeed = 3.5f;
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float deceleration = 3.0f;
    [SerializeField] private float rotationSpeed = 6.0f;

    [Header("Cart Headlight")]
    [SerializeField] private Light frontHeadlight;

    // Internal State
    private bool isPlayerSeated = false;
    private float currentSpeed = 0f;
    private float currentDistance = 0f;
    private SplineContainer activeContainer;
    private bool isMainLoopActive = false;

    // Cached References
    private GameObject playerObj;
    private CharacterController playerController;

    private void Start()
    {
        activeContainer = stationSpline != null ? stationSpline : mainSpline;
        if (frontHeadlight != null) frontHeadlight.enabled = false;

        // Nonaktifkan SplineAnimate jika ada agar tidak bentrok
        SplineAnimate sa = GetComponent<SplineAnimate>();
        if (sa != null) sa.enabled = false;

        // 🛡️ SANGAT PENTING: DILARANG MEMINDAHKAN TRANSFORM SAAT START!
        // Posisi kereta 100% DIJAMIN KUNCI DIAM di tempat Anda menaruhnya di Editor!
        CalculateInitialSplineOffset();
    }

    private void CalculateInitialSplineOffset()
    {
        if (activeContainer == null) return;

        float totalSplineLength = activeContainer.CalculateLength();
        if (totalSplineLength <= 0.1f) return;

        // Hitung jarak offset pada spline terdekat TANPA MEMINDAHKAN POSISI transform!
        Unity.Mathematics.float3 nearestLocalPoint;
        float normalizedT;
        SplineUtility.GetNearestPoint(activeContainer.Spline, activeContainer.transform.InverseTransformPoint(transform.position), out nearestLocalPoint, out normalizedT);

        currentDistance = normalizedT * totalSplineLength;
        Debug.Log($"<color=green>[CART INITIALIZED] Kereta terkunci di stasiun Editor! (Offset: {currentDistance:F1}m)</color>");
    }

    private void Update()
    {
        HandlePlayerInput();
        UpdateCartMovement();
    }

    private void HandlePlayerInput()
    {
        if (!isPlayerSeated) return;

        // 1. INPUT KONTROL KERETA: W = Maju, S = Berhenti
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            moveInput = 1f;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            moveInput = -0.5f; // Rem / Mundur lambat
        }

        // Akselerasi & Deselerasi Mulus
        if (moveInput > 0)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed * moveInput, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // 2. TURUN DARI KERETA (Tekan E saat kereta hampir berhenti)
        if (Input.GetKeyDown(KeyCode.E) && currentSpeed < 0.5f)
        {
            UnboardPlayer();
        }
    }

    private void UpdateCartMovement()
    {
        if (activeContainer == null) return;

        // 🛡️ HANYA PINDAHKAN POSISI KERETA JIKA KECEPATAN > 0.01 (SAAT W DITEKAN / MAJU)
        // Saat diam, posisi kereta 100% UNTOUCHED (NOL TELEPORTASI)!
        if (currentSpeed > 0.01f)
        {
            float totalSplineLength = activeContainer.CalculateLength();
            if (totalSplineLength <= 0.1f) totalSplineLength = 10f;

            // Tambah jarak tempuh di sepanjang seluruh rel
            currentDistance += currentSpeed * Time.deltaTime;

            // 🔀 DUA TRACK TRANSITION: ST.Spline -> MainSpline
            if (!isMainLoopActive && stationSpline != null && mainSpline != null)
            {
                if (currentDistance >= totalSplineLength)
                {
                    isMainLoopActive = true;
                    activeContainer = mainSpline;
                    currentDistance = 0f;
                    totalSplineLength = activeContainer.CalculateLength();
                    Debug.Log("<color=green>[CART TRACK] Kereta berhasil berpindah dari Stasiun (ST.Spline) ke Rel Utama (MainSpline)!</color>");
                }
            }
            else if (isMainLoopActive)
            {
                // Looping di MainSpline
                if (currentDistance >= totalSplineLength)
                {
                    currentDistance %= totalSplineLength;
                }
            }

            // Hitung Posisi & Rotasi Mulus di SplineContainer
            float normalizedTime = Mathf.Clamp01(currentDistance / totalSplineLength);
            Vector3 localPos = activeContainer.EvaluatePosition(normalizedTime);
            Vector3 localTangent = activeContainer.EvaluateTangent(normalizedTime);

            Vector3 worldPos = activeContainer.transform.TransformPoint(localPos);
            Vector3 worldTangent = activeContainer.transform.TransformDirection(localTangent);

            transform.position = worldPos;

            if (worldTangent != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(worldTangent) * Quaternion.Euler(rotationOffset);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Nyalakan lampu saat player naik & kereta jalan
        if (frontHeadlight != null)
        {
            frontHeadlight.enabled = isPlayerSeated;
        }
    }

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (!isPlayerSeated)
            return "Tekan [E] Naik Kereta Wahana";

        return "<b>[W]</b> Maju | <b>[S]</b> Rem | <b>[E]</b> Turun Kereta";
    }

    public float HoldDuration => 0f; // Tap [E] Instan

    public void OnInteract()
    {
        if (!isPlayerSeated)
        {
            BoardPlayer();
        }
    }

    private void BoardPlayer()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = FindObjectOfType<PlayerMovement>()?.gameObject;

        if (playerObj == null) return;

        isPlayerSeated = true;
        Debug.Log("[CART BOARDED] Player naik ke Kereta Wahana!");

        // Matikan kontrol pergerakan WASD biasa
        GameInputLock.LockInput();

        // Pindahkan player tepat ke kursi kereta
        if (seatTransform != null)
        {
            playerController = playerObj.GetComponent<CharacterController>();
            if (playerController != null) playerController.enabled = false;

            playerObj.transform.SetParent(seatTransform);
            playerObj.transform.localPosition = Vector3.zero;
            playerObj.transform.localRotation = Quaternion.identity;
        }

        if (frontHeadlight != null) frontHeadlight.enabled = true;
    }

    private void UnboardPlayer()
    {
        if (!isPlayerSeated) return;

        isPlayerSeated = false;
        currentSpeed = 0f;
        Debug.Log("[CART UNBOARDED] Player turun dari Kereta Wahana.");

        if (playerObj != null)
        {
            playerObj.transform.SetParent(null);

            // Geser sedikit ke samping kereta agar tidak tersangkut
            playerObj.transform.position = transform.position + (transform.right * 1.5f);

            if (playerController != null) playerController.enabled = true;
        }

        if (frontHeadlight != null) frontHeadlight.enabled = false;

        // Kembalikan kontrol player
        GameInputLock.UnlockInput();
    }
}
