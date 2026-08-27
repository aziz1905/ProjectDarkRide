using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Kontroler Kereta Wahana (Dark Ride Cart) Interaktif Mulus & Presisi.
/// Menghilangkan pemaksaan rotasi di LateUpdate agar animasi tatapan mata bebas melihat sekitar saat cutscene!
/// </summary>
public class DarkRideCartController : MonoBehaviour, IInteractable
{
    [Header("Spline Tracks (Rel Kereta Utama)")]
    [Tooltip("Drag Objek ST.Spline ke sini")]
    [SerializeField] private SplineContainer stationSpline;

    [Tooltip("Drag Objek MainSpline ke sini")]
    [SerializeField] private SplineContainer mainSpline;

    [Header("Position & Rotation Offset Fix")]
    [Tooltip("Sudut koreksi rotasi jika posisi kereta miring/melintang dari arah rel (default: Y = -90)")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0f, -90f, 0f);

    [Tooltip("Ketinggian posisi Y kereta dari lantai rel agar tidak tenggelam (default: 0.25 meter)")]
    [SerializeField] private float verticalHeightOffset = 0.25f;

    [Header("Train Carriage Snake Mechanics")]
    [Tooltip("Transform Gerbong Depan / Kepala (Cart_head)")]
    [SerializeField] private Transform headCartTransform;

    [Tooltip("Transform Gerbong Tengah (Cart_middle)")]
    [SerializeField] private Transform middleCartTransform;

    [Tooltip("Transform Gerbong Belakang / Ekor (Cart_tail)")]
    [SerializeField] private Transform tailCartTransform;

    [Tooltip("Jarak fisik antar gerbong kereta dalam meter (default: 1.8 meter)")]
    [SerializeField] private float carriageSpacing = 1.8f;

    [Header("Cart Driver & Passenger Seat")]
    [Tooltip("Transform Posisi Kursi Penumpang/Driver di dalam Kereta")]
    [SerializeField] private Transform seatTransform;

    [Tooltip("Ketinggian tambahan pandangan mata driver dari lantai kursi (default: 0.4 meter)")]
    [SerializeField] private float seatEyeHeight = 0.4f;

    [Header("Cart Speed & Physics")]
    [Tooltip("Kecepatan maksimal kereta (m/s)")]
    [SerializeField] private float maxSpeed = 4.0f;
    [SerializeField] private float acceleration = 3.0f;
    [SerializeField] private float deceleration = 4.0f;
    [SerializeField] private float rotationSpeed = 6.0f;

    [Header("Cart Headlight")]
    [SerializeField] private Light frontHeadlight;

    [Header("UI Control Prompt (Opsional)")]
    [Tooltip("Drag TextMeshProUGUI untuk petunjuk berkendara di sini")]
    [SerializeField] private TMPro.TextMeshProUGUI cartPromptUI;

    // Public Property untuk PlayerInteraction System & Cutscene Triggers
    public bool IsPlayerSeated => isPlayerSeated;
    public Quaternion ForwardRotation => seatTransform != null ? seatTransform.rotation : (headCartTransform != null ? headCartTransform.rotation : transform.rotation);

    // Internal State
    private bool isPlayerSeated = false;
    private float currentSpeed = 0f;
    private float currentNormalizedT = 0f;
    private SplineContainer activeContainer;
    private int activeSplineIndex = 0;
    private float boardTimer = 0f;

    // Dynamic Cutscene Speed Override
    private float defaultMaxSpeed = 4.0f;
    private bool isSpeedOverridden = false;

    // Cached References
    private GameObject playerObj;
    private CharacterController playerController;
    private InteractableOutline cartOutline;

    private const string CONTROL_PROMPT_MULTILINE = "<b>[W]</b> Maju\n<b>[S]</b> Rem\n<b>[E]</b> Turun Kereta";

    private void Awake()
    {
        SplineAnimate sa = GetComponent<SplineAnimate>();
        if (sa != null) Destroy(sa);

        cartOutline = GetComponent<InteractableOutline>();
        if (cartOutline == null) cartOutline = GetComponentInChildren<InteractableOutline>();

        AutoFindSplines();
        AutoFindCarriages();
        AutoFindPromptUI();
    }

    private void AutoFindSplines()
    {
        if (stationSpline == null)
        {
            GameObject stClean = GameObject.Find("ST.Spline_CleanWorld");
            GameObject stObj = stClean != null ? stClean : GameObject.Find("ST.Spline");
            if (stObj != null) stationSpline = stObj.GetComponent<SplineContainer>();
        }

        if (mainSpline == null)
        {
            GameObject mainClean = GameObject.Find("MainSpline_CleanWorld");
            GameObject mainObj = mainClean != null ? mainClean : GameObject.Find("MainSpline");
            if (mainObj != null) mainSpline = mainObj.GetComponent<SplineContainer>();
        }
    }

    private void AutoFindCarriages()
    {
        if (headCartTransform == null) headCartTransform = transform.Find("Cart_head");
        if (middleCartTransform == null) middleCartTransform = transform.Find("Cart_middle");
        if (tailCartTransform == null) tailCartTransform = transform.Find("Cart_tail");
    }

    private void AutoFindPromptUI()
    {
        if (cartPromptUI == null)
        {
            TMPro.TextMeshProUGUI[] tmps = FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.gameObject.name.ToLower().Contains("prompt") || tmp.gameObject.name.ToLower().Contains("interact") || tmp.gameObject.name.ToLower().Contains("text"))
                {
                    cartPromptUI = tmp;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        if (frontHeadlight != null) frontHeadlight.enabled = false;
        InitializeOnStationSpline();
    }

    private void InitializeOnStationSpline()
    {
        activeContainer = stationSpline != null ? stationSpline : mainSpline;
        if (activeContainer == null || activeContainer.Splines == null || activeContainer.Splines.Count == 0) return;

        activeSplineIndex = 0;
        currentNormalizedT = 0f;

        UpdateCarriagePositions();
    }

    private float GetSplineLength(ISpline spline)
    {
        if (spline == null) return 10f;
        float len = SplineUtility.CalculateLength(spline, activeContainer.transform.localToWorldMatrix);
        return Mathf.Max(0.5f, len);
    }

    private void Update()
    {
        if (isPlayerSeated)
        {
            DisableAllOutlines();

            if (cartPromptUI != null)
            {
                if (!cartPromptUI.gameObject.activeSelf) cartPromptUI.gameObject.SetActive(true);
                cartPromptUI.text = CONTROL_PROMPT_MULTILINE;
            }
        }

        HandlePlayerInput();
        UpdateCartMovement();
    }

    private void DisableAllOutlines()
    {
        InteractableOutline[] outlines = GetComponentsInChildren<InteractableOutline>(true);
        foreach (var outline in outlines)
        {
            if (outline != null)
            {
                outline.SetOutlineActive(false);
            }
        }
    }

    private void HandlePlayerInput()
    {
        if (!isPlayerSeated) return;

        if (boardTimer > 0f) boardTimer -= Time.deltaTime;

        if (isSpeedOverridden) return;

        float moveInput = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(moveInput) < 0.1f)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput = 1.0f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput = -0.5f;
        }

        if (moveInput > 0.05f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed * moveInput, acceleration * Time.deltaTime);
        }
        else if (moveInput < -0.05f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed * moveInput, deceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        if (boardTimer <= 0f && Input.GetKeyDown(KeyCode.E))
        {
            UnboardPlayer();
        }
    }

    public void SetTemporarySpeed(float targetSpeed)
    {
        if (!isSpeedOverridden) defaultMaxSpeed = maxSpeed;
        isSpeedOverridden = true;
        maxSpeed = targetSpeed;
        currentSpeed = targetSpeed;
    }

    public void ResetSpeedToDefault()
    {
        if (isSpeedOverridden)
        {
            maxSpeed = defaultMaxSpeed;
            isSpeedOverridden = false;
        }
    }

    private void UpdateCartMovement()
    {
        if (activeContainer == null || activeContainer.Splines == null || activeContainer.Splines.Count == 0) return;

        if (Mathf.Abs(currentSpeed) > 0.001f)
        {
            if (activeSplineIndex >= activeContainer.Splines.Count) activeSplineIndex = 0;

            ISpline currentSpline = activeContainer.Splines[activeSplineIndex];
            float splineLength = GetSplineLength(currentSpline);

            float deltaT = (currentSpeed * Time.deltaTime) / splineLength;
            currentNormalizedT += deltaT;

            if (currentNormalizedT >= 1f)
            {
                currentNormalizedT -= 1f;
                activeSplineIndex++;

                if (activeSplineIndex >= activeContainer.Splines.Count)
                {
                    if (activeContainer == stationSpline && mainSpline != null)
                    {
                        activeContainer = mainSpline;
                        activeSplineIndex = 0;
                    }
                    else if (activeContainer == mainSpline && stationSpline != null)
                    {
                        activeContainer = stationSpline;
                        activeSplineIndex = 0;
                    }
                    else
                    {
                        activeSplineIndex = 0;
                    }
                }
            }
            else if (currentNormalizedT < 0f)
            {
                currentNormalizedT = 0f;
            }
        }

        UpdateCarriagePositions();

        if (frontHeadlight != null) frontHeadlight.enabled = isPlayerSeated;
    }

    private void UpdateCarriagePositions()
    {
        if (activeContainer == null || activeContainer.Splines == null || activeContainer.Splines.Count == 0) return;

        ISpline currentSpline = activeContainer.Splines[activeSplineIndex];
        float splineLength = GetSplineLength(currentSpline);

        // 1. GERBONG DEPAN / KEPALA (Cart_head)
        UpdateSingleCarriage(headCartTransform, activeSplineIndex, currentNormalizedT);
        if (headCartTransform != null) transform.position = headCartTransform.position;

        Vector3 headPos = headCartTransform != null ? headCartTransform.position : transform.position;
        Vector3 headForward = headCartTransform != null ? headCartTransform.forward : transform.forward;

        // 2. GERBONG TENGAH (Cart_middle) - PRESISI 1.8 METER DI BELAKANG KEPALA
        float headDistInMeters = currentNormalizedT * splineLength;
        float middleDistInMeters = headDistInMeters - carriageSpacing;

        if (middleDistInMeters >= 0f)
        {
            float middleT = middleDistInMeters / splineLength;
            UpdateSingleCarriage(middleCartTransform, activeSplineIndex, middleT);
        }
        else if (activeSplineIndex > 0)
        {
            int middleIndex = activeSplineIndex - 1;
            ISpline prevSpline = activeContainer.Splines[middleIndex];
            float prevLen = GetSplineLength(prevSpline);
            float middleT = Mathf.Clamp01(1.0f + (middleDistInMeters / prevLen));
            UpdateSingleCarriage(middleCartTransform, middleIndex, middleT);
        }
        else
        {
            if (middleCartTransform != null)
            {
                middleCartTransform.position = headPos - (headForward * carriageSpacing);
                middleCartTransform.rotation = headCartTransform != null ? headCartTransform.rotation : transform.rotation;
            }
        }

        // 3. GERBONG BELAKANG / EKOR (Cart_tail) - PRESISI 3.6 METER DI BELAKANG KEPALA
        float tailDistInMeters = headDistInMeters - (carriageSpacing * 2f);
        int tailIndex = activeSplineIndex;

        if (tailDistInMeters >= 0f)
        {
            float tailT = tailDistInMeters / splineLength;
            UpdateSingleCarriage(tailCartTransform, tailIndex, tailT);
        }
        else if (activeSplineIndex > 0)
        {
            tailIndex = activeSplineIndex - 1;
            ISpline prevSpline = activeContainer.Splines[tailIndex];
            float prevLen = GetSplineLength(prevSpline);
            float tailT = Mathf.Clamp01(1.0f + (tailDistInMeters / prevLen));
            UpdateSingleCarriage(tailCartTransform, tailIndex, tailT);
        }
        else
        {
            if (tailCartTransform != null)
            {
                tailCartTransform.position = headPos - (headForward * (carriageSpacing * 2f));
                tailCartTransform.rotation = headCartTransform != null ? headCartTransform.rotation : transform.rotation;
            }
        }
    }

    private void UpdateSingleCarriage(Transform carriage, int splineIndex, float normalizedT)
    {
        if (carriage == null || activeContainer == null || activeContainer.Splines == null || splineIndex >= activeContainer.Splines.Count) return;

        normalizedT = Mathf.Clamp01(normalizedT);
        Vector3 worldPos = (Vector3)activeContainer.EvaluatePosition(splineIndex, normalizedT);
        Vector3 worldTangent = (Vector3)activeContainer.EvaluateTangent(splineIndex, normalizedT);

        if (!float.IsNaN(worldPos.x) && !float.IsInfinity(worldPos.x))
        {
            worldPos += Vector3.up * verticalHeightOffset;
            carriage.position = worldPos;
        }

        if (worldTangent.sqrMagnitude > 0.001f && !float.IsNaN(worldTangent.x))
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldTangent) * Quaternion.Euler(rotationOffset);
            carriage.rotation = Quaternion.Slerp(carriage.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (isPlayerSeated && playerObj != null)
        {
            Transform seat = seatTransform != null ? seatTransform : (headCartTransform != null ? headCartTransform : transform);
            
            // 🎯 POSISI MATA DRIVER SELALU DUDUK DI KURSI KERETA (BEBAS DARI PEMAKSAAN ROTASI SETIAP FRAME)
            Vector3 targetPos = seat.position + Vector3.up * seatEyeHeight;
            playerObj.transform.position = targetPos;
        }
    }

    public string GetInteractPrompt()
    {
        if (!isPlayerSeated) return "Tekan [E] Naik Kereta Wahana";
        return CONTROL_PROMPT_MULTILINE;
    }

    public float HoldDuration => 0f;

    public void OnInteract()
    {
        if (!isPlayerSeated) BoardPlayer();
    }

    private void BoardPlayer()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = FindObjectOfType<PlayerMovement>()?.gameObject;
        if (playerObj == null) playerObj = GameObject.Find("playerDay3");
        if (playerObj == null) return;

        isPlayerSeated = true;
        boardTimer = 0.5f;

        ToolBeltManager toolBelt = playerObj.GetComponent<ToolBeltManager>();
        if (toolBelt == null) toolBelt = FindObjectOfType<ToolBeltManager>();
        if (toolBelt != null)
        {
            toolBelt.SetCartSeated(true);
        }

        AutoFindPromptUI();
        DisableAllOutlines();

        playerController = playerObj.GetComponent<CharacterController>();
        if (playerController != null) playerController.enabled = false;

        PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        playerObj.transform.SetParent(null);

        Transform seat = seatTransform != null ? seatTransform : (headCartTransform != null ? headCartTransform : transform);
        Vector3 eyePos = seat.position + Vector3.up * seatEyeHeight;
        playerObj.transform.position = eyePos;

        Quaternion targetForwardRotation = ForwardRotation;
        FirstPersonCamera fpc = playerObj.GetComponentInChildren<FirstPersonCamera>();
        if (fpc != null)
        {
            fpc.SmoothRotateToTarget(targetForwardRotation, 0.6f);
        }
        else
        {
            playerObj.transform.rotation = targetForwardRotation;
            Camera mainCam = playerObj.GetComponentInChildren<Camera>();
            if (mainCam != null) mainCam.transform.localRotation = Quaternion.identity;
        }

        if (frontHeadlight != null) frontHeadlight.enabled = true;

        if (cartPromptUI != null)
        {
            cartPromptUI.gameObject.SetActive(true);
            cartPromptUI.text = CONTROL_PROMPT_MULTILINE;
        }
    }

    private void UnboardPlayer()
    {
        if (!isPlayerSeated) return;

        isPlayerSeated = false;
        currentSpeed = 0f;
        ResetSpeedToDefault();

        if (playerObj != null)
        {
            ToolBeltManager toolBelt = playerObj.GetComponent<ToolBeltManager>();
            if (toolBelt == null) toolBelt = FindObjectOfType<ToolBeltManager>();
            if (toolBelt != null)
            {
                toolBelt.SetCartSeated(false);
            }
        }

        if (cartPromptUI != null)
        {
            cartPromptUI.text = "";
            cartPromptUI.gameObject.SetActive(false);
        }

        if (playerObj != null)
        {
            playerObj.transform.position = transform.position + (transform.right * 1.5f) + (Vector3.up * 0.2f);

            PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = true;

            if (playerController != null) playerController.enabled = true;
        }

        if (frontHeadlight != null) frontHeadlight.enabled = false;
    }
}
