using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script Interaksi Direct Click Push & Pull Gurney (Single Tap Click).
/// Menggunakan BoxCollider murni (100% bebas CharacterController):
/// 1. Tidak ada kapsul yang mengganggu / tampak seperti collider player.
/// 2. Deteksi naik undakan trotoar seketika saat ujung roda BoxCollider menyentuh perron.
/// 3. Ketinggian roda dan kemiringan (pitch & roll) otomatis disesuaikan secara dinamis via 4 roda.
/// 4. Tidak tembus lantai atau melayang.
/// </summary>
public class GurneyDirectPushPull : MonoBehaviour, IInteractable
{
    [Header("Task Manager Settings")]
    [Tooltip("ID Tugas di Notebook TAB (misal: CLEAR_GURNEY / PUSH_GURNEY)")]
    [SerializeField] private string taskId = "CLEAR_GURNEY";

    [Header("Step Distance Settings (Per Sekali Klik)")]
    [Tooltip("Jarak dorongan per 1x klik mouse (meter)")]
    [SerializeField] private float stepDistance = 0.6f;

    [Tooltip("Durasi animasi pergeseran per langkah (detik)")]
    [SerializeField] private float stepDuration = 0.25f;

    [Tooltip("Jeda waktu minimal antar klik (detik) agar tidak bisa di-spam terlalu cepat")]
    [SerializeField] private float clickCooldown = 0.35f;

    [Header("Target Parking Spot Settings")]
    [Tooltip("Drag objek GurneyAwal / titik parkir di trotoar ke kolom ini")]
    [SerializeField] private Transform targetParkingSpot;

    [Tooltip("Radius toleransi kedekatan dengan titik parkir agar dianggap rapi (meter)")]
    [SerializeField] private float parkingRadius = 1.5f;

    [Tooltip("CENTANG jika gurney otomatis merapikan posisi & rotasi saat sudah masuk area parkir")]
    [SerializeField] private bool autoSnapToPark = true;

    [Header("Stairs & Step Settings")]
    [Tooltip("Tinggi maksimal undakan trotoar yang bisa dinaiki gurney (meter)")]
    [SerializeField] private float stepOffset = 0.6f;

    [Tooltip("Offset ketinggian roda untuk menyetel jika roda sedikit melayang atau tenggelam (misal: -0.05 atau +0.05)")]
    [SerializeField] private float heightOffset = 0f;

    [Header("Floor Alignment Settings")]
    [Tooltip("Layer tanah/lantai/trotoar/tangga (Default: Everything)")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wheelStepSFX;   // Suara roda berderit per dorongan klik
    [SerializeField] private AudioClip parkSuccessSFX; // Suara klik parkir berhasil

    // Internal Components
    private BoxCollider boxCollider;

    // Internal State
    private bool isCleared = false;
    private float lastAimTime = -999f;
    private float lastClickTime = -999f;
    private Coroutine activeStepRoutine;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        // Bersihkan CharacterController lama jika masih terpasang
        CharacterController oldCC = GetComponent<CharacterController>();
        if (oldCC != null)
        {
            Destroy(oldCC);
        }
    }

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        IgnorePlayerCollision();
        SnapToGroundInstant();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Otomatis hapus CharacterController jika tidak sengaja terpasang di Editor
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    CharacterController c = GetComponent<CharacterController>();
                    if (c != null) DestroyImmediate(c);
                }
            };
        }
    }
#endif

    private void IgnorePlayerCollision()
    {
        if (boxCollider == null) return;

        Collider playerCollider = null;
        if (Camera.main != null)
        {
            playerCollider = Camera.main.GetComponentInParent<Collider>();
        }
        if (playerCollider == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerCollider = p.GetComponent<Collider>();
        }

        if (playerCollider != null)
        {
            Physics.IgnoreCollision(boxCollider, playerCollider, true);
        }
    }

    private void Update()
    {
        // Cek apakah pemain sedang menatap gurney dengan tangan kosong dalam 0.25 detik terakhir
        bool isAiming = (Time.time - lastAimTime) < 0.25f;

        if (isAiming && IsPlayerHandsEmpty())
        {
            HandleClickInput();
        }

        // Cek kedekatan dengan area parkir
        CheckTrackClearance();

        // Kemiringan alami (tilt) dan penyesuaian ketinggian 4 roda
        UpdateGroundPositionAndTilt();
    }

    private float GetFloorAt(Vector3 worldPos, float fallbackFloorY)
    {
        // Raycast dari atas posisi roda ke arah bawah
        Vector3 origin = new Vector3(worldPos.x, worldPos.y + 0.8f, worldPos.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 2.5f, groundLayer, QueryTriggerInteraction.Ignore);

        float highestFloor = float.MinValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            // Abaikan collider milik gurney ini sendiri
            if (hit.collider == boxCollider || hit.transform.IsChildOf(transform))
                continue;

            if (hit.point.y > highestFloor)
            {
                highestFloor = hit.point.y;
                found = true;
            }
        }

        return found ? highestFloor : fallbackFloorY;
    }

    private void SnapToGroundInstant()
    {
        if (boxCollider == null) return;

        Vector3 ext = boxCollider.size * 0.5f;
        Vector3 c = boxCollider.center;

        Vector3 localBottom = new Vector3(c.x, c.y - ext.y, c.z);
        float currentWorldBottomY = transform.TransformPoint(localBottom).y;
        float offsetFromPivot = currentWorldBottomY - transform.position.y;

        Vector3 wFL = transform.TransformPoint(c + new Vector3(-ext.x * 0.85f, -ext.y,  ext.z * 0.85f));
        Vector3 wFR = transform.TransformPoint(c + new Vector3( ext.x * 0.85f, -ext.y,  ext.z * 0.85f));
        Vector3 wBL = transform.TransformPoint(c + new Vector3(-ext.x * 0.85f, -ext.y, -ext.z * 0.85f));
        Vector3 wBR = transform.TransformPoint(c + new Vector3( ext.x * 0.85f, -ext.y, -ext.z * 0.85f));

        float avgFloor = (GetFloorAt(wFL, currentWorldBottomY) + 
                          GetFloorAt(wFR, currentWorldBottomY) + 
                          GetFloorAt(wBL, currentWorldBottomY) + 
                          GetFloorAt(wBR, currentWorldBottomY)) * 0.25f;

        Vector3 pos = transform.position;
        pos.y = avgFloor + heightOffset - offsetFromPivot;
        transform.position = pos;
    }

    private void UpdateGroundPositionAndTilt()
    {
        if (boxCollider == null || isCleared) return;

        Vector3 ext = boxCollider.size * 0.5f;
        Vector3 c = boxCollider.center;

        // Dasar roda ranjang saat ini di world space
        Vector3 localBottom = new Vector3(c.x, c.y - ext.y, c.z);
        float currentWorldBottomY = transform.TransformPoint(localBottom).y;
        float offsetFromPivot = currentWorldBottomY - transform.position.y;

        // Ambil posisi 4 sudut roda di dunia
        Vector3 wFL = transform.TransformPoint(c + new Vector3(-ext.x * 0.85f, -ext.y,  ext.z * 0.85f));
        Vector3 wFR = transform.TransformPoint(c + new Vector3( ext.x * 0.85f, -ext.y,  ext.z * 0.85f));
        Vector3 wBL = transform.TransformPoint(c + new Vector3(-ext.x * 0.85f, -ext.y, -ext.z * 0.85f));
        Vector3 wBR = transform.TransformPoint(c + new Vector3( ext.x * 0.85f, -ext.y, -ext.z * 0.85f));

        // Cari ketinggian lantai di bawah masing-masing roda (fallback ke ketinggian roda saat ini)
        float floorFL = GetFloorAt(wFL, currentWorldBottomY);
        float floorFR = GetFloorAt(wFR, currentWorldBottomY);
        float floorBL = GetFloorAt(wBL, currentWorldBottomY);
        float floorBR = GetFloorAt(wBR, currentWorldBottomY);

        // Ketinggian rata-rata lantai di bawah 4 roda
        float avgFloor = (floorFL + floorFR + floorBL + floorBR) * 0.25f;

        // Ketinggian transform agar roda menempel pas di lantai
        float desiredY = avgFloor + heightOffset - offsetFromPivot;

        Vector3 curPos = transform.position;
        curPos.y = Mathf.Lerp(curPos.y, desiredY, Time.deltaTime * 12f);
        transform.position = curPos;

        // Kemiringan Roll (samping kiri-kanan): tanjakan di kanan akan mengangkat sisi kanan naik
        float leftFloor = (floorFL + floorBL) * 0.5f;
        float rightFloor = (floorFR + floorBR) * 0.5f;
        float width = Mathf.Max(0.5f, boxCollider.size.x * Mathf.Abs(transform.lossyScale.x));
        float rollDiff = leftFloor - rightFloor;
        float targetRoll = Mathf.Clamp(Mathf.Atan2(rollDiff, width) * Mathf.Rad2Deg, -14f, 14f);

        // Kemiringan Pitch (depan-belakang): tanjakan di depan akan mengangkat bagian depan naik
        float frontFloor = (floorFL + floorFR) * 0.5f;
        float rearFloor = (floorBL + floorBR) * 0.5f;
        float length = Mathf.Max(0.5f, boxCollider.size.z * Mathf.Abs(transform.lossyScale.z));
        float pitchDiff = rearFloor - frontFloor;
        float targetPitch = Mathf.Clamp(Mathf.Atan2(pitchDiff, length) * Mathf.Rad2Deg, -12f, 12f);

        Vector3 localEuler = transform.localEulerAngles;
        float currentRoll = localEuler.z > 180f ? localEuler.z - 360f : localEuler.z;
        float currentPitch = localEuler.x > 180f ? localEuler.x - 360f : localEuler.x;

        localEuler.z = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 8f);
        localEuler.x = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * 8f);
        transform.localEulerAngles = localEuler;
    }

    private void HandleClickInput()
    {
        if (isCleared) return;
        if (Camera.main == null) return;
        if (Time.time - lastClickTime < clickCooldown) return;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;
        if (horizontalForward.sqrMagnitude < 0.01f) return;

        // 🖱️ 1x KLIK KIRI = DORONG MAJU 1 LANGKAH
        if (Input.GetMouseButtonDown(0))
        {
            lastClickTime = Time.time;
            if (activeStepRoutine != null) StopCoroutine(activeStepRoutine);
            activeStepRoutine = StartCoroutine(StepMoveRoutine(horizontalForward));
        }
        // 🖱️ 1x KLIK KANAN = TARIK MUNDUR 1 LANGKAH
        else if (Input.GetMouseButtonDown(1))
        {
            lastClickTime = Time.time;
            if (activeStepRoutine != null) StopCoroutine(activeStepRoutine);
            activeStepRoutine = StartCoroutine(StepMoveRoutine(-horizontalForward));
        }
    }

    private float GetMaxAllowedDistance(Vector3 direction, float desiredDistance)
    {
        if (boxCollider == null) return desiredDistance;

        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 lossy = new Vector3(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, lossy);

        // Angkat dasar pemeriksaan di atas stepOffset agar undakan trotoar tidak dideteksi sebagai dinding
        float bottomY = center.y - halfExtents.y;
        float obstacleCheckBottomY = bottomY + stepOffset + 0.05f;

        Vector3 checkCenter = center;
        Vector3 checkExtents = halfExtents;
        float newHeight = (center.y + halfExtents.y) - obstacleCheckBottomY;

        if (newHeight > 0.15f)
        {
            checkExtents.y = newHeight * 0.5f;
            checkCenter.y = obstacleCheckBottomY + checkExtents.y;
        }

        checkExtents.x *= 0.90f;
        checkExtents.z *= 0.90f;

        RaycastHit[] hits = Physics.BoxCastAll(
            checkCenter,
            checkExtents,
            direction.normalized,
            transform.rotation,
            desiredDistance + 0.1f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float maxDist = desiredDistance;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider == boxCollider) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            // Abaikan tubuh pemain saat menarik/mendorong
            if (hit.collider.CompareTag("Player") || 
                hit.collider.GetComponentInParent<CharacterController>() != null ||
                hit.collider.name.ToLower().Contains("player"))
                continue;

            // Abaikan titik parkir
            if (targetParkingSpot != null && (hit.transform == targetParkingSpot || hit.transform.IsChildOf(targetParkingSpot)))
                continue;

            // Periksa jika mengenai permukaan tegak (dinding, pilar, lemari, objek padat lain)
            if (hit.normal.y < 0.6f)
            {
                float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
                if (safeDistance < maxDist)
                {
                    maxDist = safeDistance;
                }
            }
        }

        return maxDist;
    }

    private IEnumerator StepMoveRoutine(Vector3 direction)
    {
        if (direction == Vector3.zero || isCleared) yield break;

        // Cek jarak aman sebelum menabrak tembok atau objek lain
        float actualDistance = GetMaxAllowedDistance(direction, stepDistance);
        if (actualDistance <= 0.02f)
        {
            // Terhalang tembok/objek langsung di depan! Tidak bisa maju
            yield break;
        }

        // SFX roda berderit
        if (audioSource != null && wheelStepSFX != null)
        {
            audioSource.PlayOneShot(wheelStepSFX);
        }

        Vector3 startPos = transform.position;
        Vector3 moveStep = direction.normalized * actualDistance;

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            if (isCleared) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 nextPos = Vector3.Lerp(startPos, startPos + moveStep, smoothT);
            // Ketinggian Y dikendalikan oleh UpdateGroundPositionAndTilt()
            nextPos.y = transform.position.y;
            transform.position = nextPos;

            yield return null;
        }

        activeStepRoutine = null;
    }

    private void CheckTrackClearance()
    {
        if (isCleared) return;

        if (targetParkingSpot != null)
        {
            Vector3 bedCenter = (boxCollider != null) ? transform.TransformPoint(boxCollider.center) : transform.position;
            Vector2 curXZ = new Vector2(bedCenter.x, bedCenter.z);
            Vector2 targetXZ = new Vector2(targetParkingSpot.position.x, targetParkingSpot.position.z);
            float distToParking = Vector2.Distance(curXZ, targetXZ);

            float effectiveRadius = Mathf.Max(parkingRadius, 1.2f);

            if (distToParking <= effectiveRadius)
            {
                isCleared = true;

                if (activeStepRoutine != null)
                {
                    StopCoroutine(activeStepRoutine);
                    activeStepRoutine = null;
                }

                // Snap persis di tengah titik parkir (posisi pusat kasur jatuh tepat di tengah targetParkingSpot)
                if (autoSnapToPark)
                {
                    SnapToParkingCenter();
                }

                // Setelah parkir rapi di trotoar, netralkan rotasi kemiringan agar rata
                Vector3 euler = transform.localEulerAngles;
                euler.x = 0f;
                euler.z = 0f;
                transform.localEulerAngles = euler;

                if (boxCollider != null)
                {
                    boxCollider.isTrigger = false;
                }

                if (audioSource != null && parkSuccessSFX != null)
                {
                    audioSource.PlayOneShot(parkSuccessSFX);
                }

                Debug.Log($"<color=green>[GurneyDirectPushPull] Gurney berhasil diparkir tepat di tengah titik parkir ({distToParking:F2}m)!</color>");

                // Selesaikan tugas di Notebook TAB
                if (!string.IsNullOrEmpty(taskId))
                {
                    TaskManager taskMgr = FindObjectOfType<TaskManager>();
                    if (taskMgr != null)
                    {
                        taskMgr.CompleteTask(taskId);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Menempatkan titik tengah ranjang (boxCollider.center) presisi 100% di tengah targetParkingSpot.
    /// </summary>
    [ContextMenu("Test Snap ke Tengah Parkir")]
    public void SnapToParkingCenter()
    {
        if (targetParkingSpot == null) return;

        // 1. Samakan rotasi yaw dengan target
        transform.rotation = Quaternion.Euler(0f, targetParkingSpot.eulerAngles.y, 0f);

        // 2. Hitung jarak titik tengah boxCollider dari posisi transform saat rotasi target
        Vector3 worldCenterOffset = (boxCollider != null)
            ? (transform.TransformPoint(boxCollider.center) - transform.position)
            : Vector3.zero;

        // 3. Posisikan agar titik tengah ranjang jatuh tepat di tengah posisi targetParkingSpot
        Vector3 targetPos = targetParkingSpot.position - worldCenterOffset;
        targetPos.y = transform.position.y;
        transform.position = targetPos;

        Physics.SyncTransforms();
    }

    private bool IsPlayerHandsEmpty()
    {
        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        if (toolBelt == null) return true;

        return toolBelt.ActiveItemName.Trim().Equals("Empty", System.StringComparison.OrdinalIgnoreCase) ||
               toolBelt.ActiveItemName.Trim().Equals("Kosong", System.StringComparison.OrdinalIgnoreCase);
    }

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        lastAimTime = Time.time;

        if (isCleared)
        {
            return "Gurney RS [SUDAH DIPARKIR RAPI]";
        }

        if (!IsPlayerHandsEmpty())
        {
            return "Kosongkan Tangan untuk Mendorong Gurney";
        }

        return "[Klik Kiri] Dorong Maju\n[Klik Kanan] Tarik Mundur";
    }

    public float HoldDuration => 0f;

    public void OnInteract()
    {
    }

    private void OnDrawGizmosSelected()
    {
        if (targetParkingSpot != null)
        {
            Gizmos.color = isCleared ? Color.green : Color.yellow;
            float effectiveRadius = Mathf.Max(parkingRadius, 1.2f);
            Gizmos.DrawWireSphere(targetParkingSpot.position, effectiveRadius);
            Vector3 bedCenter = (boxCollider != null) ? transform.TransformPoint(boxCollider.center) : transform.position;
            Gizmos.DrawLine(bedCenter, targetParkingSpot.position);
        }
    }
}
