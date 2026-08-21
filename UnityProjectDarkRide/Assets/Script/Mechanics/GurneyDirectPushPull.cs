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

    [Header("Floor Alignment")]
    [Tooltip("Layer tanah/lantai (Default: Everything)")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wheelStepSFX;   // Suara roda berderit per dorongan klik
    [SerializeField] private AudioClip parkSuccessSFX; // Suara klik parkir berhasil

    // Internal State
    private bool isCleared = false;
    private bool isPlayerAimingAtThis = false;
    private float lastClickTime = -999f;
    private float groundYOffset = 0f;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 🛡️ Hitung tinggi pivot gurney dari lantai (matikan SEMUA collider internal)
        Collider[] allCols = GetComponentsInChildren<Collider>();
        foreach (var c in allCols) c.enabled = false;

        if (Physics.Raycast(transform.position + Vector3.up * 3.0f, Vector3.down, out RaycastHit hit, 15.0f, groundLayer))
        {
            groundYOffset = transform.position.y - hit.point.y;
        }

        foreach (var c in allCols) c.enabled = true;
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
            StartCoroutine(StepMoveRoutine(horizontalForward));
        }
        // 🖱️ 1x KLIK KANAN = TARIK MUNDUR 1 LANGKAH
        else if (Input.GetMouseButtonDown(1))
        {
            lastClickTime = Time.time;
            StartCoroutine(StepMoveRoutine(-horizontalForward));
        }
    }

    private IEnumerator StepMoveRoutine(Vector3 direction)
    {
        if (direction == Vector3.zero) yield break;

        // SFX per dorongan klik
        if (audioSource != null && wheelStepSFX != null)
        {
            audioSource.PlayOneShot(wheelStepSFX);
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (direction * stepDistance);

        // 🛡️ Deteksi Ketinggian Lantai Baru (Tanpa Menembak Objek Sendiri)
        float targetY = GetGroundYAt(targetPos);
        targetPos.y = targetY;

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        transform.position = targetPos;
    }

    private float GetGroundYAt(Vector3 point)
    {
        Collider[] allCols = GetComponentsInChildren<Collider>();
        foreach (var c in allCols) c.enabled = false;

        float resultY = point.y;
        if (Physics.Raycast(point + Vector3.up * 3.0f, Vector3.down, out RaycastHit hit, 15.0f, groundLayer))
        {
            resultY = hit.point.y + groundYOffset;
        }

        foreach (var c in allCols) c.enabled = true;
        return resultY;
    }

    private void CheckTrackClearance()
    {
        if (isCleared) return;

        if (targetParkingSpot != null)
        {
            float distToParking = Vector3.Distance(transform.position, targetParkingSpot.position);
            if (distToParking <= parkingRadius)
            {
                isCleared = true;

                // 🎯 RAPIKAN KE KOORDINAT PARKIR XZ TARGET, TETAPI Y TETAP MENEMPEL PRESISI DI ATAS LANTAI!
                if (autoSnapToPark)
                {
                    float currentGroundY = transform.position.y;
                    Vector3 finalParkPos = new Vector3(targetParkingSpot.position.x, currentGroundY, targetParkingSpot.position.z);
                    
                    // Double check raycast lantai pada koordinat parkir akhir
                    float floorY = GetGroundYAt(finalParkPos);
                    finalParkPos.y = floorY;

                    transform.position = finalParkPos;
                    transform.rotation = targetParkingSpot.rotation;
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
}
