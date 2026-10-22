using System.Collections;
using UnityEngine;

/// <summary>
/// Anomali: StalkingSkeletonAnomaly (Tengkorak Bermata Merah yang Mengintai & Mengejar di Kegelapan).
/// Mekanik:
/// 1. Aktif saat lampu padam/blackout.
/// 2. Bergerak mendekati pemain (Player).
/// 3. Jika disorot dengan Senter (Flashlight SpotLight menyala & mengarah ke tengkorak tanpa terhalang dinding):
///    - Tengkorak membeku / FREEZE (berhenti bergerak, animasi membeku).
/// 4. Jika senter dimatikan atau player memalingkan pandangan:
///    - Tengkorak kembali bergerak mengejar player!
/// 5. Jika tengkorak berhasil mencapai player dalam jarak tangkap:
///    - Memicu jumpscare / tertangkap!
/// </summary>
public class StalkingSkeletonAnomaly : MonoBehaviour
{
    [Header("1. Target Player & Spawn Location")]
    [Tooltip("Target player (otomatis mencari Camera.main jika kosong)")]
    [SerializeField] private Transform targetPlayer;

    [Tooltip("Titik lokasi munculnya skeleton saat anomali aktif. Jika kosong, memakai posisi awal objek")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Kembalikan skeleton ke titik spawn awal saat anomali selesai / lampu menyala?")]
    [SerializeField] private bool resetToSpawnOnRestore = true;

    [Header("2. Movement Settings")]
    [Tooltip("Kecepatan jalan tengkorak mengejar player (meter/detik)")]
    [SerializeField] private float chaseSpeed = 2.0f;

    [Tooltip("Kecepatan rotasi tengkorak menghadap player")]
    [SerializeField] private float rotationSpeed = 5.0f;

    [Tooltip("CENTANG jika model 3D tengkorak ini menghadap terbalik (punggungnya yang maju saat mengejar)")]
    [SerializeField] private bool invertForwardDirection = false;

    [Tooltip("Jarak berhenti jika menangkap pemain (meter)")]
    [SerializeField] private float catchDistance = 1.2f;

    [Tooltip("Tinggi offset raycast untuk deteksi senter dari tubuh tengkorak")]
    [SerializeField] private float chestHeightOffset = 1.0f;

    [Header("3. Flashlight Detection")]
    [Tooltip("Layer rintangan yang bisa menghalangi cahaya senter ke tengkorak (tembok, pilar, dsb)")]
    [SerializeField] private LayerMask obstructionMask = ~0;

    [Tooltip("Batas sudut berkas cahaya senter (derajat). Jika 0, otomatis mengikuti spotAngle senter")]
    [SerializeField] private float customLightAngle = 0f;

    [Header("4. Red Eyes Link (Opsional)")]
    [Tooltip("Komponen RedEyesStareAnomaly di kepala tengkorak (otomatis dicari di child jika kosong)")]
    [SerializeField] private RedEyesStareAnomaly redEyes;

    [Header("5. Animation (Opsional jika ada Animator)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkParamName = "isWalking";

    [Header("6. Audio SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip footstepSFX;
    [SerializeField] private AudioClip catchJumpscareSFX;

    // Internal State
    private bool isAnomalyActive = false;
    private bool isFrozenByLight = false;
    private bool hasCaughtPlayer = false;
    private EquipableFlashlight playerFlashlight;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public bool IsFrozen => isFrozenByLight;
    public bool IsActive => isAnomalyActive;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (redEyes == null)
        {
            redEyes = GetComponentInChildren<RedEyesStareAnomaly>(true);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Mulai dalam keadaan non-aktif & tersembunyi
        gameObject.SetActive(false);
    }

    private void Start()
    {
        FindPlayerReferences();
    }

    private void FindPlayerReferences()
    {
        if (targetPlayer == null && Camera.main != null)
        {
            targetPlayer = Camera.main.transform;
        }

        if (playerFlashlight == null)
        {
            playerFlashlight = FindObjectOfType<EquipableFlashlight>();
        }
    }

    private void Update()
    {
        if (!isAnomalyActive || hasCaughtPlayer) return;

        if (targetPlayer == null || playerFlashlight == null)
        {
            FindPlayerReferences();
            if (targetPlayer == null) return;
        }

        // 1. Cek apakah tengkorak sedang disorot oleh senter player
        bool litByFlashlight = CheckIfLitByFlashlight();

        if (litByFlashlight)
        {
            // FREEZE / MEMATUNG!
            FreezeSkeleton();
        }
        else
        {
            // LANJUT MENGEJAR!
            UnfreezeAndChasePlayer();
        }
    }

    /// <summary>
    /// Memeriksa apakah berkas cahaya senter pemain sedang menyinari tubuh tengkorak
    /// </summary>
    private bool CheckIfLitByFlashlight()
    {
        if (playerFlashlight == null) return false;

        // Syarat 1: Senter harus sedang dipegang dan saklarnya MENYALA
        if (!playerFlashlight.IsEquipped || !playerFlashlight.IsLightOn)
        {
            return false;
        }

        Light spot = playerFlashlight.SpotLightComponent;
        Transform lightSource = spot != null ? spot.transform : playerFlashlight.transform;

        Vector3 skeletonTargetPoint = transform.position + Vector3.up * chestHeightOffset;
        Vector3 dirToSkeleton = skeletonTargetPoint - lightSource.position;
        float distToSkeleton = dirToSkeleton.magnitude;

        // Syarat 2: Jarak tidak melebihi jangkauan lampu senter (range)
        float maxRange = spot != null ? spot.range : 15.0f;
        if (distToSkeleton > maxRange)
        {
            return false;
        }

        // Syarat 3: Sudut tengkorak harus berada di dalam kerucut (cone) sorot senter
        float allowedAngle = customLightAngle > 0 ? customLightAngle : (spot != null ? spot.spotAngle * 0.5f : 30f);
        float angleToSkeleton = Vector3.Angle(lightSource.forward, dirToSkeleton.normalized);

        if (angleToSkeleton > allowedAngle)
        {
            return false; // Berada di luar kerucut cahaya senter
        }

        // Syarat 4: Tidak terhalang dinding/rintangan padat (Line of Sight Raycast)
        Ray ray = new Ray(lightSource.position, dirToSkeleton.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, distToSkeleton, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            // Jika raycast menabrak objek lain sebelum sampai ke tengkorak
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                return false; // Terhalang tembok/pilar
            }
        }

        // Semua syarat terpenuhi: Tengkorak sedang disorot senter!
        return true;
    }

    private void FreezeSkeleton()
    {
        isFrozenByLight = true;

        if (animator != null && !string.IsNullOrEmpty(walkParamName))
        {
            animator.SetBool(walkParamName, false);
            animator.speed = 0f; // Bekukan animasi di frame saat ini
        }
    }

    private void UnfreezeAndChasePlayer()
    {
        isFrozenByLight = false;

        if (animator != null)
        {
            animator.speed = 1f;
            if (!string.IsNullOrEmpty(walkParamName))
            {
                animator.SetBool(walkParamName, true);
            }
        }

        // 1. Hadapkan badan tengkorak ke arah player (hanya rotasi horizontal Y)
        Vector3 playerPosXZ = new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z);
        Vector3 moveDir = (playerPosXZ - transform.position).normalized;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 facingDir = invertForwardDirection ? -moveDir : moveDir;
            Quaternion targetRot = Quaternion.LookRotation(facingDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        // 2. Gerakkan posisi maju mendekati player (pasti melangkah ke arah player)
        float distToPlayer = Vector3.Distance(transform.position, playerPosXZ);

        if (distToPlayer > catchDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerPosXZ, chaseSpeed * Time.deltaTime);
        }
        else
        {
            // Jarak sudah sangat dekat: TERTANGKAP!
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        hasCaughtPlayer = true;

        if (audioSource != null && catchJumpscareSFX != null)
        {
            audioSource.PlayOneShot(catchJumpscareSFX);
        }

        // Panggil sistem kematian pemain (Surat Pemecatan / Sanction)
        if (SanctionAndRetryManager.Instance != null)
        {
            SanctionAndRetryManager.Instance.IssueSanction("Diserang dan tertangkap oleh entitas anomali di area kuburan saat bertugas.");
        }
        else
        {
            PlayerDeathTrigger deathTrigger = FindObjectOfType<PlayerDeathTrigger>();
            if (deathTrigger != null)
            {
                deathTrigger.TriggerPlayerDeath();
            }
        }
    }

    /// <summary>
    /// Dipanggil saat Anomali dimulai (misal: saat lampu kuburan padam)
    /// </summary>
    public void StartAnomaly()
    {
        // Pindahkan posisi tengkorak ke titik lokasi spawn jika diset
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        gameObject.SetActive(true);
        isAnomalyActive = true;
        hasCaughtPlayer = false;
        isFrozenByLight = false;

        if (redEyes != null)
        {
            redEyes.StartStare();
        }
    }

    /// <summary>
    /// Dipanggil saat Anomali dihentikan / dipulihkan (misal: lampu kembali normal)
    /// </summary>
    public void StopAnomaly(bool resetPosition = false)
    {
        isAnomalyActive = false;

        if (redEyes != null)
        {
            redEyes.StopStare();
        }

        if (resetPosition || resetToSpawnOnRestore)
        {
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }
            else
            {
                transform.position = initialPosition;
                transform.rotation = initialRotation;
            }
        }

        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        Gizmos.color = Color.yellow;
        Vector3 chestPos = transform.position + Vector3.up * chestHeightOffset;
        Gizmos.DrawSphere(chestPos, 0.15f);

        if (spawnPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}
