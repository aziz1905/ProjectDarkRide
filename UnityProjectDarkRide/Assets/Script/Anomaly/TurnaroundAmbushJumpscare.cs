using System.Collections;
using UnityEngine;

/// <summary>
/// Anomali #2: TurnaroundAmbushJumpscare (Trap & Turnaround Jumpscare di Lorong RS).
/// Cara Kerja:
/// 1. Player menginjak trigger di lorong RS -> Sosok hantu muncul di depan (The Bait).
/// 2. Player yang ketakutan mencoba berbalik arah / mundur (putar haluan > 110-180 derajat).
/// 3. Begitu player berbalik arah -> BAM!! Sosok mendadak muncul tepat di depan muka player di arah mundur!
/// 4. Disertai suara Jumpscare + Getaran Kamera (Camera Shake) + sosok menerjang/menghilang setelah 1-2 detik.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TurnaroundAmbushJumpscare : MonoBehaviour
{
    [Header("1. Objek Sosok Penampakan")]
    [Tooltip("Objek sosok hantu yang berdiri di depan lorong sebagai umpan pertama")]
    [SerializeField] private GameObject frontApparition;

    [Tooltip("Objek sosok hantu jumpscare di belakang. Jika kosong, script otomatis memakai frontApparition yang diteleport!")]
    [SerializeField] private GameObject ambushApparition;

    [Header("2. Pengaturan Putar Balik (Turnaround Detection)")]
    [Tooltip("Sudut putar balik kamera player untuk memicu jumpscare (derajat, misal 110-150)")]
    [Range(90f, 170f)]
    [SerializeField] private float turnaroundAngleThreshold = 120f;

    [Tooltip("Jika player nekat maju mendekati sosok depan sampai radius jarak ini, jumpscare LANGSUNG terpicu! (meter)")]
    [SerializeField] private float closeProximityDistance = 5.0f;

    [Tooltip("Jarak kemunculan sosok jumpscare tepat di depan muka pemain (meter)")]
    [SerializeField] private float ambushDistanceInFrontOfPlayer = 1.3f;

    [Tooltip("Tinggi offset sosok dari tanah")]
    [SerializeField] private float ambushHeightOffset = 0f;

    [Header("3. Pencegah Tembok/Pojokan (Obstruction Check)")]
    [Tooltip("Layer tembok, pintu, dan objek padat")]
    [SerializeField] private LayerMask obstructionLayer = ~0;

    [Tooltip("Jarak minimal pandangan ke depan bebas halangan agar sosok bisa muncul pas di depan muka")]
    [SerializeField] private float minClearDistance = 1.0f;

    [Header("3. Jumpscare Effect (Audio & Camera Shake)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("SFX Ambience tegang saat sosok pertama kali muncul di depan (Opsional)")]
    [SerializeField] private AudioClip baitAmbienceSFX;
    [Tooltip("SFX Jeritan / Sting ledakan keras saat jumpscare menyergap di belakang")]
    [SerializeField] private AudioClip jumpscareScreechSFX;

    [Tooltip("Berapa lama kamera bergetar saat jumpscare (detik)")]
    [SerializeField] private float cameraShakeDuration = 0.8f;
    [Tooltip("Kekuatan getaran kamera")]
    [SerializeField] private float cameraShakeMagnitude = 0.25f;

    [Tooltip("Berapa detik sosok jumpscare bertahan sebelum lenyap menghilang (detik)")]
    [SerializeField] private float ambushDuration = 1.5f;

    [Header("4. Trigger Settings")]
    [Tooltip("CENTANG jika anomali ini hanya terjadi 1x")]
    [SerializeField] private bool triggerOnce = true;

    // Internal State
    private bool isTrapArmed = false;
    private bool hasSeenFrontGhost = false;
    private bool hasJumpscared = false;
    private Transform playerCameraTransform;
    private Vector3 initialFacingDirection;
    private Coroutine activeShakeCoroutine;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Pastikan sosok dalam keadaan mati sebelum trigger diinjak
        if (frontApparition != null) frontApparition.SetActive(false);
        if (ambushApparition != null) ambushApparition.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasJumpscared || isTrapArmed) return;

        bool isPlayer = other.CompareTag("Player")
            || other.GetComponentInParent<CharacterController>() != null
            || other.name.ToLower().Contains("player")
            || other.transform.root.name.ToLower().Contains("player");

        if (isPlayer)
        {
            ArmTheTrap();
        }
    }

    private void ArmTheTrap()
    {
        isTrapArmed = true;
        hasSeenFrontGhost = false;

        if (Camera.main != null)
        {
            playerCameraTransform = Camera.main.transform;
        }

        // 👻 MUNCULKAN SOSOK PERTAMA DI DEPAN SEBAGAI UMPAN
        if (frontApparition != null)
        {
            frontApparition.SetActive(true);
        }

        if (audioSource != null && baitAmbienceSFX != null)
        {
            audioSource.PlayOneShot(baitAmbienceSFX);
        }
    }

    private void Update()
    {
        if (!isTrapArmed || hasJumpscared) return;

        if (playerCameraTransform == null && Camera.main != null)
        {
            playerCameraTransform = Camera.main.transform;
            if (playerCameraTransform == null) return;
        }

        // Arah lurus horizontal dari posisi player ke arah hantu depan
        Vector3 toGhost = Vector3.forward;
        if (frontApparition != null)
        {
            toGhost = Vector3.ProjectOnPlane(frontApparition.transform.position - playerCameraTransform.position, Vector3.up).normalized;
        }

        // Arah pandangan horizontal kamera player saat ini
        Vector3 camFacing = Vector3.ProjectOnPlane(playerCameraTransform.forward, Vector3.up).normalized;

        // Sudut antara pandangan kamera player dengan posisi hantu depan
        float angleToGhost = Vector3.Angle(toGhost, camFacing);

        // Jarak dari player ke hantu depan
        float distToGhost = float.MaxValue;
        if (frontApparition != null)
        {
            distToGhost = Vector3.Distance(playerCameraTransform.position, frontApparition.transform.position);
        }

        // 🚨 KONDISI A: Player nekat MAJU MENDEKATI sosok di depan (Jarak <= closeProximityDistance)
        if (distToGhost <= closeProximityDistance)
        {
            ExecuteAmbushJumpscare();
            return;
        }

        // 👁️ FASE 1: Player HARUS melihat ke arah hantu depan terlebih dahulu (Sudut < 65 derajat)
        if (!hasSeenFrontGhost)
        {
            if (angleToGhost <= 65f)
            {
                hasSeenFrontGhost = true;
            }
            return; // JANGAN jumpscare putar balik sebelum player melihat sosok depan
        }

        // 🚨 KONDISI B: Player ketakutan dan BERBALIK ARAH KABUR/MUNDUR (Sudut >= turnaroundAngleThreshold):
        if (angleToGhost >= turnaroundAngleThreshold)
        {
            // Cek apakah pandangan player terhalang tembok/pintu/objek sangat mepet (di pojokan)
            if (IsPlayerStaringAtWall())
            {
                // Player sedang menatap tembok/pojokan mepet! TAHAN DULU, tunggu sampai pandangannya terbuka ke lorong
                return;
            }

            ExecuteAmbushJumpscare();
        }
    }

    private bool IsPlayerStaringAtWall()
    {
        if (playerCameraTransform == null) return false;

        // Tembakkan raycast dari kamera ke arah depan pandangan sejauh minClearDistance
        if (Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit hit, minClearDistance, obstructionLayer))
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                // Ada tembok / pintu / benda padat sangat dekat di depan mata
                return true;
            }
        }

        return false;
    }

    private void ExecuteAmbushJumpscare()
    {
        hasJumpscared = true;
        isTrapArmed = false;

        // 1. Hilangkan sosok di depan seketika
        if (frontApparition != null)
        {
            frontApparition.SetActive(false);
        }

        // 2. Posisikan sosok ambush tepat di depan kamera pemain (arah mundur)
        GameObject activeAmbushObj = ambushApparition != null ? ambushApparition : frontApparition;

        if (activeAmbushObj != null && playerCameraTransform != null)
        {
            Vector3 camForwardHorizontal = Vector3.ProjectOnPlane(playerCameraTransform.forward, Vector3.up).normalized;
            float finalSpawnDist = ambushDistanceInFrontOfPlayer;
            // Jika ada tembok/rintangan antara mata player dan titik spawn, geser sosok lebih dekat ke muka pemain agar tidak tertanam di dalam tembok
            if (Physics.Raycast(playerCameraTransform.position, camForwardHorizontal, out RaycastHit wallHit, ambushDistanceInFrontOfPlayer + 0.3f, obstructionLayer))
            {
                if (wallHit.collider != null && !wallHit.collider.isTrigger)
                {
                    finalSpawnDist = Mathf.Max(0.5f, wallHit.distance - 0.25f);
                }
            }

            // Posisikan tepat sejajar di depan kamera pemain
            Vector3 spawnPos = playerCameraTransform.position + (camForwardHorizontal * finalSpawnDist);
            // Sesuaikan tinggi mata agar tidak melayang atau tenggelam di lantai
            spawnPos.y = playerCameraTransform.position.y + ambushHeightOffset;

            activeAmbushObj.transform.position = spawnPos;

            // Hadapkan sosok hantu tepat menatap ke arah kamera player
            Vector3 lookDir = (playerCameraTransform.position - spawnPos).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                activeAmbushObj.transform.rotation = Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z));
            }

            activeAmbushObj.SetActive(true);
        }

        // 3. Mainkan Audio Jumpscare
        if (audioSource != null && jumpscareScreechSFX != null)
        {
            audioSource.PlayOneShot(jumpscareScreechSFX);
        }

        // 4. Efek Camera Shake
        if (playerCameraTransform != null && cameraShakeDuration > 0)
        {
            if (activeShakeCoroutine != null) StopCoroutine(activeShakeCoroutine);
            activeShakeCoroutine = StartCoroutine(CameraShakeRoutine(cameraShakeDuration, cameraShakeMagnitude));
        }

        // 5. Hilangkan sosok setelah durasi jumpscare selesai
        StartCoroutine(DismissAmbushRoutine(activeAmbushObj, ambushDuration));

        if (triggerOnce)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    private IEnumerator DismissAmbushRoutine(GameObject ambushObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ambushObj != null)
        {
            ambushObj.SetActive(false);
        }
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalLocalPos = playerCameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = 1f - (elapsed / duration); // Getaran makin lama makin mengecil
            float x = Random.Range(-1f, 1f) * magnitude * percent;
            float y = Random.Range(-1f, 1f) * magnitude * percent;

            playerCameraTransform.localPosition = originalLocalPos + new Vector3(x, y, 0);
            yield return null;
        }

        playerCameraTransform.localPosition = originalLocalPos;
        activeShakeCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>() != null ? GetComponent<Collider>().bounds.size : Vector3.one * 2f);

        if (frontApparition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, frontApparition.transform.position);
            Gizmos.DrawWireSphere(frontApparition.transform.position, 0.4f);

            // Tampilkan radius area mendekat (Close Proximity Area) dengan warna merah
            Gizmos.color = new Color(1f, 0f, 0f, 0.45f);
            Gizmos.DrawWireSphere(frontApparition.transform.position, closeProximityDistance);
        }
    }
}
