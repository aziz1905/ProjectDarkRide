using System.Collections;
using UnityEngine;

/// <summary>
/// Script Interaksi Direct Click Push & Pull Gurney (Single Tap Click).
/// Fitur:
/// 1. Wajib Tangan Kosong (ToolBelt Active Item == Empty).
/// 2. SEKALI KLIK KIRI (1x Tap LMB) -> Gurney terdorong maju 1 langkah halus.
/// 3. SEKALI KLIK KANAN (1x Tap RMB) -> Gurney ditarik mundur 1 langkah halus.
/// 4. Otomatis mengikuti naik/turun permukaan lantai & trotoar (Floor Alignment Raycast).
/// 5. Saat parkir selesai: Klik di-LOCK total (tidak bisa didorong lagi) dan Y TETAP MENEMPEL DI LANTAI.
/// 6. Task Manager Notebook TAB langsung tercoret [X]!
/// </summary>
[RequireComponent(typeof(Collider))]
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
    [SerializeField] private float clickCooldown = 0.4f;

    [Header("Target Parking Spot Settings")]
    [Tooltip("Drag objek GurneyAwal (titik parkir yang benar di trotoar) ke kolom ini")]
    [SerializeField] private Transform targetParkingSpot;

    [Tooltip("Radius toleransi kedekatan dengan titik parkir agar dianggap rapi (meter)")]
    [SerializeField] private float parkingRadius = 1.5f;

    [Tooltip("CENTANG jika gurney otomatis merapikan posisi & rotasi saat sudah masuk area parkir")]
    [SerializeField] private bool autoSnapToPark = true;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wheelStepSFX;   // Suara roda berderit per dorongan klik
    [SerializeField] private AudioClip parkSuccessSFX; // Suara klik parkir berhasil

    // Internal State
    private bool isCleared = false;
    private bool isPlayerAimingAtThis = false;
    private float lastClickTime = -999f;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Reset flag penatapan setiap awal frame
        bool wasAiming = isPlayerAimingAtThis;
        isPlayerAimingAtThis = false;

        // Cek input KLIK saat pemain menatap gurney dengan tangan kosong
        if (wasAiming && IsPlayerHandsEmpty())
        {
            HandleClickInput();
        }

        // Cek apakah sudah digeser cukup dekat ke area parkir
        CheckTrackClearance();
    }

    private void HandleClickInput()
    {
        // 🛑 1. JIKA SUDAH SELESAI DIPARKIR RAPI -> LOCK KLIK TOTAL! (TIDAK BISA DIDORONG/DITARIK LAGI)
        if (isCleared) return;

        if (Camera.main == null) return;
        
        // 🛑 2. JEDA ANTI-SPAM
        if (Time.time - lastClickTime < clickCooldown) return;

        // Ambil arah horizontal dari kamera pemain
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;

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

    [Header("Obstacle Collision Settings")]
    [Tooltip("Layer rintangan seperti Tembok, Bangunan, Prop padat")]
    [SerializeField] private LayerMask obstacleLayer = ~0; // Default Everything
    [Tooltip("Margin jarak pengaman dari tembok/objek")]
    [SerializeField] private float collisionSkinWidth = 0.08f;

    private Coroutine activeStepRoutine;

    private IEnumerator StepMoveRoutine(Vector3 direction)
    {
        if (direction == Vector3.zero || isCleared) yield break;

        Vector3 startPos = transform.position;
        Bounds bounds = GetGurneyTotalBounds();
        Vector3 halfExtents = (bounds.size * 0.5f) - Vector3.one * collisionSkinWidth;
        halfExtents.x = Mathf.Max(halfExtents.x, 0.1f);
        halfExtents.y = Mathf.Max(halfExtents.y, 0.1f);
        halfExtents.z = Mathf.Max(halfExtents.z, 0.1f);

        // Matikan sementara collider gurney sendiri agar BoxCast tidak mendeteksi diri sendiri
        Collider[] allCols = GetComponentsInChildren<Collider>();
        foreach (var c in allCols) c.enabled = false;

        float maxAllowedMove = stepDistance;

        // BoxCast dari titik tengah ranjang ke arah pergerakan
        Vector3 boxCenter = bounds.center;
        // Sedikit naikkan boxCenter agar tidak mendeteksi gundukan lantai/trotoar datar sebagai tembok
        boxCenter.y += 0.1f;
        halfExtents.y = Mathf.Max(0.1f, halfExtents.y - 0.15f);

        if (Physics.BoxCast(boxCenter, halfExtents, direction, out RaycastHit hit, transform.rotation, stepDistance + collisionSkinWidth, obstacleLayer))
        {
            // Jangan anggap tanah/trotoar tempat berdiri sebagai obstacle
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                float distToObstacle = Mathf.Max(0f, hit.distance - collisionSkinWidth);
                maxAllowedMove = Mathf.Min(maxAllowedMove, distToObstacle);
            }
        }

        foreach (var c in allCols) c.enabled = true;

        // Jika benar-benar terhalang (jarak gerak hampir 0), hentikan dorongan
        if (maxAllowedMove <= 0.03f)
        {
            activeStepRoutine = null;
            yield break;
        }

        // SFX per dorongan klik
        if (audioSource != null && wheelStepSFX != null)
        {
            audioSource.PlayOneShot(wheelStepSFX);
        }

        Vector3 targetPos = startPos + (direction * maxAllowedMove);
        // Pertahankan Y agar tetap rata horizontal tanpa raycast tanah berulang
        targetPos.y = startPos.y;

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            if (isCleared) yield break; // JIKA SUDAH PARKIR, JANGAN GERAKKAN LAGI!

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        if (!isCleared)
        {
            transform.position = targetPos;
        }
        activeStepRoutine = null;
    }

    private void CheckTrackClearance()
    {
        if (isCleared) return;

        if (targetParkingSpot != null)
        {
            // 🎯 1. SENSOR SELURUH KOTAK BADAN GURNEY (COMPOUND BOUNDING BOX OUTLINE)
            Bounds totalBounds = GetGurneyTotalBounds();
            Vector3 closestEdgePoint = totalBounds.ClosestPoint(targetParkingSpot.position);

            Vector2 edgeXZ = new Vector2(closestEdgePoint.x, closestEdgePoint.z);
            Vector2 targetXZ = new Vector2(targetParkingSpot.position.x, targetParkingSpot.position.z);
            float distToParking = Vector2.Distance(edgeXZ, targetXZ);

            if (distToParking <= parkingRadius)
            {
                isCleared = true;

                if (activeStepRoutine != null)
                {
                    StopCoroutine(activeStepRoutine);
                    activeStepRoutine = null;
                }

                // 🎯 2. RAPIKAN LANGSUNG KE POSISI TARGET:
                // Menyamakan TITIK TENGAH KASUR (Center) tepat di atas target parkir!
                if (autoSnapToPark)
                {
                    // Ambil titik tengah relatif lokal terhadap transform pivot
                    Vector3 localCenter = transform.InverseTransformPoint(totalBounds.center);
                    transform.rotation = targetParkingSpot.rotation;
                    // Kurangkan offset lokal yang sudah dirotasi sesuai rotasi target
                    transform.position = targetParkingSpot.position - (transform.rotation * localCenter);
                }

                if (audioSource != null && parkSuccessSFX != null)
                {
                    audioSource.PlayOneShot(parkSuccessSFX);
                }

                Debug.Log($"<color=green>Gurney berhasil diparkir rapi & terkunci di titik semula ({distToParking:F2}m)!</color>");

                // Laporkan ke Task Manager Notebook TAB
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
        isPlayerAimingAtThis = true;

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

    public Bounds GetGurneyTotalBounds()
    {
        Collider[] cols = GetComponentsInChildren<Collider>();
        if (cols != null && cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++)
            {
                b.Encapsulate(cols[i].bounds);
            }
            return b;
        }

        Renderer[] rens = GetComponentsInChildren<Renderer>();
        if (rens != null && rens.Length > 0)
        {
            Bounds b = rens[0].bounds;
            for (int i = 1; i < rens.Length; i++)
            {
                b.Encapsulate(rens[i].bounds);
            }
            return b;
        }

        return new Bounds(transform.position, Vector3.one);
    }

    public Vector3 GetGurneyCenterPoint()
    {
        return GetGurneyTotalBounds().center;
    }

    private void OnDrawGizmosSelected()
    {
        if (targetParkingSpot != null)
        {
            Bounds totalBounds = GetGurneyTotalBounds();
            Vector3 edgePoint = totalBounds.ClosestPoint(targetParkingSpot.position);

            Gizmos.color = isCleared ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(targetParkingSpot.position, parkingRadius);
            Gizmos.DrawLine(edgePoint, targetParkingSpot.position);
            Gizmos.DrawWireSphere(edgePoint, 0.15f); // Titik terluar aktif yang paling dekat dengan target

            // Gambar kotak garis pembungkus (Bounding Box Outline) ranjang Gurney
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f); // Biru muda transparan
            Gizmos.DrawWireCube(totalBounds.center, totalBounds.size);
        }
    }
}
