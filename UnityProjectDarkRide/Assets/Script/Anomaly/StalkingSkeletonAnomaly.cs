using System.Collections;
using UnityEngine;

/// <summary>
/// Anomali: StalkingSkeletonAnomaly (Tengkorak Bermata Merah yang Mengintai & Mengejar di Kegelapan).
/// Mekanik:
/// 1. Aktif saat lampu padam/blackout di area kuburan (Day 4).
/// 2. Bergerak mendekati pemain (Player).
/// 3. Jika disorot dengan Senter (Flashlight SpotLight menyala & mengarah ke tengkorak tanpa terhalang dinding):
///    - Tengkorak membeku / FREEZE (berhenti bergerak, animasi membeku).
///    - Deteksi senter mencakup seluruh area tubuh (Kepala, Dada, Pinggang, Kaki, & Lebar Badan).
///    - Pemain yang mendekati skeleton sambil terus menyorot senter TIDAK AKAN MATI (skeleton tetap beku total!).
/// 4. Jika senter dimatikan atau player memalingkan pandangan:
///    - Tengkorak kembali bergerak mengejar player setelah jeda pemulihan singkat!
/// 5. Jika tengkorak berhasil mencapai player saat TIDAK disorot senter:
///    - Memicu jumpscare / tertangkap!
/// 6. Mata Merah (RedEyesStareAnomaly) di kepala tengkorak beroperasi dalam mode Head Socket:
///    - Posisi kedua bola mata tetap pas di soket batok kepala tengkorak.
///    - Hanya pupil hitam yang bergerak melirik mengikuti posisi pemain.
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

    [Header("3. Obstacle Avoidance & Collision")]
    [Tooltip("Jarak deteksi rintangan di depan untuk mencari jalan memutar (meter)")]
    [SerializeField] private float obstacleDetectionDistance = 1.5f;

    [Tooltip("Jarak aman jarak minimum tubuh dari permukaan dinding/rintangan (meter)")]
    [SerializeField] private float wallSkinBuffer = 0.35f;

    [Header("4. Flashlight Detection & Hitbox Area")]
    [Tooltip("Tinggi titik dada untuk deteksi senter (meter)")]
    [SerializeField] private float chestHeightOffset = 1.8f;

    [Tooltip("Tinggi titik kepala/atas untuk deteksi senter (meter). Mengakomodasi tubuh skeleton yang tinggi/besar.")]
    [SerializeField] private float headHeightOffset = 3.2f;

    [Tooltip("Tinggi titik kaki/bawah untuk deteksi senter (meter).")]
    [SerializeField] private float feetHeightOffset = 0.3f;

    [Tooltip("Lebar jangkauan tubuh (kiri-kanan) untuk deteksi senter (meter).")]
    [SerializeField] private float bodyWidth = 0.65f;

    [Tooltip("Jeda waktu pemulihan sebelum tengkorak bergerak kembali setelah senter dilepas (detik)")]
    [SerializeField] private float unfreezeDelay = 0.35f;

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
    private float unfreezeTimer = 0f;
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

        if (redEyes != null)
        {
            // Pastikan RedEyes mengaktifkan mode soket kepala (bola mata diam di kepala, hanya pupil yang melirik)
            redEyes.SetHeadSocketMode(true);

            // Jika objek RedEyes berada di luar hierarki skeleton di Scene, otomatis pasang sebagai child agar matanya ikut berjalan
            if (!redEyes.transform.IsChildOf(transform))
            {
                redEyes.transform.SetParent(transform, true);
            }
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        SetupChildTriggerProxies();

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

        // 1. Cek apakah tengkorak sedang disorot oleh senter player (mencakup kepala, dada, kaki, jarak dekat, dll)
        bool litByFlashlight = CheckIfLitByFlashlight();

        if (litByFlashlight)
        {
            // FREEZE / MEMATUNG! Selama disorot senter, skeleton TIDAK BISA MENGEJAR ATAUPUN MEMBUNUH PEMAIN!
            unfreezeTimer = unfreezeDelay; // Reset timer jeda beku
            FreezeSkeleton();
        }
        else
        {
            // Jika baru saja lepas dari senter, berikan sedikit jeda sebelum mengejar kembali
            if (unfreezeTimer > 0f)
            {
                unfreezeTimer -= Time.deltaTime;
                FreezeSkeleton(); // Tetap beku selama masa recovery jeda
            }
            else
            {
                // LANJUT MENGEJAR (Hanya jika benar-benar tidak tersorot senter)!
                UnfreezeAndChasePlayer();
            }
        }
    }

    /// <summary>
    /// Memeriksa apakah berkas cahaya senter pemain sedang menyinari tubuh tengkorak (Kepala, Dada, Kaki, atau Badan).
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

        float maxRange = spot != null ? spot.range : 15.0f;
        float allowedAngle = customLightAngle > 0 ? customLightAngle : (spot != null ? spot.spotAngle * 0.5f : 30f);

        Vector3 basePos = transform.position;
        Vector3 playerPos = lightSource.position;
        Vector3 toSkeletonXZ = new Vector3(basePos.x - playerPos.x, 0f, basePos.z - playerPos.z);
        float horizontalDist = toSkeletonXZ.magnitude;
        float verticalDiff = playerPos.y - basePos.y;

        // METODE 1: JARAK DEKAT (Close-Range Proximity, < 3.5 meter)
        // Ketika pemain mendekati skeleton sambil mengarahkan senter, tubuh skeleton memenuhi pandangan pemain.
        // Selama senter menyala dan pemain menghadap ke arah skeleton, skeleton 100% BEKU dan TIDAK BOLEH MEMBUNUH PEMAIN!
        if (horizontalDist < 3.5f && verticalDiff >= -0.8f && verticalDiff <= headHeightOffset + 1.2f)
        {
            Vector3 forwardXZ = new Vector3(lightSource.forward.x, 0f, lightSource.forward.z).normalized;
            float horizontalAngle = Vector3.Angle(forwardXZ, toSkeletonXZ.normalized);

            // Jika pemain sedang menghadap ke arah skeleton (sudut pandang senter < 65 derajat)
            if (horizontalAngle <= 65f)
            {
                if (!IsPointObstructed(lightSource.position, toSkeletonXZ.normalized, horizontalDist))
                {
                    return true;
                }
            }
        }

        // METODE 2: SOROTAN LANGSUNG (Direct Raycast Aim)
        // Jika crosshair / tengah sorot senter tepat mengenai collider skeleton (termasuk trigger box collider)
        Ray directRay = new Ray(lightSource.position, lightSource.forward);
        if (Physics.Raycast(directRay, out RaycastHit directHit, maxRange, obstructionMask, QueryTriggerInteraction.Collide))
        {
            if (directHit.transform == transform || directHit.transform.IsChildOf(transform))
            {
                return true; // Sorotan senter tepat mengenai collider tubuh/kepala/kaki skeleton!
            }
        }

        // METODE 3: MULTI-POINT BODY SAMPLING (Cone Volume Coverage)
        // Mengecek seluruh cakupan volume tubuh dari ujung kepala, dada, pinggang, kaki, hingga bentangan kedua tangan
        Vector3 rightDir = transform.right * bodyWidth;
        float armSpan = Mathf.Max(bodyWidth * 2.0f, 1.4f);
        Vector3 armRight = transform.right * armSpan;

        Vector3[] bodyPoints = new Vector3[]
        {
            basePos + Vector3.up * headHeightOffset,                                      // Kepala (Atas)
            basePos + Vector3.up * Mathf.Lerp(chestHeightOffset, headHeightOffset, 0.5f), // Leher / Bahu Atas
            basePos + Vector3.up * chestHeightOffset,                                     // Dada (Tengah)
            basePos + Vector3.up * Mathf.Lerp(feetHeightOffset, chestHeightOffset, 0.5f),  // Pinggang / Paha
            basePos + Vector3.up * feetHeightOffset,                                      // Kaki / Lutut (Bawah)
            basePos + Vector3.up * chestHeightOffset - rightDir,                          // Rusuk / Pangkal Lengan Kiri
            basePos + Vector3.up * chestHeightOffset + rightDir,                          // Rusuk / Pangkal Lengan Kanan
            basePos + Vector3.up * chestHeightOffset - armRight * 0.6f,                   // Lengan / Siku Kiri
            basePos + Vector3.up * chestHeightOffset + armRight * 0.6f,                   // Lengan / Siku Kanan
            basePos + Vector3.up * chestHeightOffset - armRight,                          // Ujung Tangan Kiri
            basePos + Vector3.up * chestHeightOffset + armRight                           // Ujung Tangan Kanan
        };

        foreach (var targetPoint in bodyPoints)
        {
            Vector3 dirToPoint = targetPoint - lightSource.position;
            float distToPoint = dirToPoint.magnitude;

            if (distToPoint > maxRange) continue;

            float angleToPoint = Vector3.Angle(lightSource.forward, dirToPoint.normalized);
            if (angleToPoint > allowedAngle) continue;

            // Pastikan titik tubuh ini tidak terhalang oleh tembok/pilar padat
            if (!IsPointObstructed(lightSource.position, dirToPoint.normalized, distToPoint))
            {
                return true; // Salah satu bagian tubuh (kepala/dada/kaki) tersorot cahaya senter!
            }
        }

        return false;
    }

    private bool IsPointObstructed(Vector3 origin, Vector3 direction, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, obstructionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue; // Kena tubuh skeleton sendiri -> tembus
            if (hit.collider.CompareTag("Player") || (playerFlashlight != null && hit.transform.IsChildOf(playerFlashlight.transform.root))) continue; // Abaikan player/senter sendiri

            // Menabrak rintangan padat lain (dinding/pilar dsb) sebelum mencapai skeleton
            return true;
        }

        return false;
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

        Vector3 currentPos = transform.position;
        Vector3 playerPos = targetPlayer.position;
        Vector3 playerPosXZ = new Vector3(playerPos.x, currentPos.y, playerPos.z);
        float distToPlayer = Vector3.Distance(currentPos, playerPosXZ);

        if (distToPlayer <= catchDistance)
        {
            if (!isFrozenByLight)
            {
                CatchPlayer();
            }
            return;
        }

        // 1. Arah ideal menuju player (garis lurus)
        Vector3 idealDir = (playerPosXZ - currentPos).normalized;
        if (idealDir.sqrMagnitude < 0.001f) return;

        // 2. Navigasi Cerdas Menghindari Rintangan (Cari jalan terdekat jika terhalang dinding/batu nisan/pohon)
        Vector3 chosenDir = FindClearDirection(currentPos, idealDir);

        // 3. Hadapkan badan tengkorak ke arah pergerakan
        Vector3 facingDir = invertForwardDirection ? -chosenDir : chosenDir;
        if (facingDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(facingDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        // 4. Hitung perpindahan posisi & periksa tabrakan fisik dinding (Anti-Tembus Dinding & Objek Collider)
        float moveStep = chaseSpeed * Time.deltaTime;
        Vector3 desiredMove = chosenDir * moveStep;
        Vector3 finalMove = ResolveWallCollisions(currentPos, desiredMove);

        // 5. Terapkan posisi baru
        transform.position += finalMove;

        // 6. Anti-Clipping Push Out: Mencegah badan skeleton terjepit atau menembus ke dalam collider
        PreventClippingIntoColliders();

        // 7. Snap ke permukaan tanah / lereng (agar kaki selalu menapak di tanah)
        transform.position = SnapToGround(transform.position);
    }

    /// <summary>
    /// Mencari rute jalan terdekat ke arah pemain jika jalur lurus terhalang rintangan/tembok/batu nisan/pohon.
    /// </summary>
    private Vector3 FindClearDirection(Vector3 currentPos, Vector3 idealDir)
    {
        float checkDist = obstacleDetectionDistance;

        // 1. Jika jalur lurus ke arah pemain bebas hambatan, langsung gunakan arah ideal
        if (!CheckObstacleRaycast(currentPos, idealDir, checkDist, out RaycastHit forwardHit, out _))
        {
            return idealDir;
        }

        // 2. Jalur lurus terhalang! "Cari jalan terdekat" untuk memutar:
        // Langkah 2A: Coba sliding menyusuri permukaan rintangan (Wall Slide)
        Vector3 wallNormal = forwardHit.normal;
        wallNormal.y = 0f;
        if (wallNormal.sqrMagnitude > 0.001f)
        {
            wallNormal.Normalize();
            Vector3 slideDir = Vector3.ProjectOnPlane(idealDir, wallNormal);
            slideDir.y = 0f;
            if (slideDir.sqrMagnitude > 0.05f)
            {
                slideDir.Normalize();
                if (!CheckObstacleRaycast(currentPos, slideDir, checkDist * 0.75f, out _, out _))
                {
                    return slideDir; // Ditemukan rute menyusuri dinding menuju pemain!
                }
            }
        }

        // Langkah 2B: Multi-Angle Whisker Search (Mencari celah terdekat ke kiri atau kanan)
        // Cek sisi tabrakan untuk memprioritaskan arah putar yang paling terbuka
        Vector3 side = new Vector3(-idealDir.z, 0f, idealDir.x).normalized;
        float hitSide = Vector3.Dot(forwardHit.point - currentPos, side);
        float primarySign = hitSide < 0f ? 1f : -1f; // Jika terbentur di kiri, coba belok ke kanan dulu

        float[] testAngles = new float[] { 25f, 45f, 65f, 85f, 105f, 125f, 145f };
        foreach (float angle in testAngles)
        {
            // Tes sudut prioritas pertama
            Vector3 testDir1 = Quaternion.Euler(0f, primarySign * angle, 0f) * idealDir;
            testDir1.y = 0f;
            testDir1.Normalize();
            if (!CheckObstacleRaycast(currentPos, testDir1, checkDist * 0.75f, out _, out _))
            {
                return testDir1;
            }

            // Tes sudut kedua
            Vector3 testDir2 = Quaternion.Euler(0f, -primarySign * angle, 0f) * idealDir;
            testDir2.y = 0f;
            testDir2.Normalize();
            if (!CheckObstacleRaycast(currentPos, testDir2, checkDist * 0.75f, out _, out _))
            {
                return testDir2;
            }
        }

        // Jika semua sudut terhalang, berhenti sementara tanpa menembus dinding
        return Vector3.zero;
    }

    /// <summary>
    /// Memeriksa rintangan fisik di depan menggunakan Multi-Raycast Grid + Low Sweep.
    /// Mendeteksi SEMUA jenis collider termasuk non-convex MeshCollider (tembok), BoxCollider rendah (gundukan makam), dsb.
    /// </summary>
    private bool CheckObstacleRaycast(Vector3 currentPos, Vector3 direction, float distance, out RaycastHit closestHit, out float hitDistance)
    {
        closestHit = default;
        hitDistance = float.MaxValue;
        float minHitDist = float.MaxValue;
        bool foundHit = false;

        if (direction.sqrMagnitude < 0.001f) return false;
        Vector3 dirNorm = direction.normalized;

        Vector3 side = new Vector3(-dirNorm.z, 0f, dirNorm.x).normalized;
        float bodyRadius = Mathf.Clamp(bodyWidth * 0.75f, 0.35f, 0.55f);

        // Ketinggian sampling dari dasar tanah hingga atas kepala:
        // 0.06m: Sangat rendah (gundukan makam pendek, batu ceper, trotoar)
        // 0.20m: Rendah (batu nisan pendek, balok kayu, undakan)
        // 0.45m: Lutut (pagar rendah, nisan sedang)
        // 1.00m: Pinggang (pagar, cart/gerobak)
        // 1.70m: Dada (dinding, pilar)
        // 2.50m: Pundak/Kepala (kusen pintu atas, atap rendah)
        float[] heightOffsets = new float[]
        {
            0.06f,
            0.20f,
            0.45f,
            Mathf.Lerp(feetHeightOffset, chestHeightOffset, 0.5f),
            chestHeightOffset,
            Mathf.Lerp(chestHeightOffset, headHeightOffset, 0.6f)
        };

        // Offset horizontal (Kiri, Tengah, Kanan)
        float[] sideOffsets = new float[] { -bodyRadius, 0f, bodyRadius };
        float startBackOffset = 0.2f;

        // 1. Grid Raycast (Mendeteksi MeshCollider, BoxCollider, dan segala bentuk rintangan di tiap ketinggian)
        foreach (float vOff in heightOffsets)
        {
            foreach (float hOff in sideOffsets)
            {
                Vector3 origin = currentPos + side * hOff + Vector3.up * vOff;
                Vector3 rayStart = origin - dirNorm * startBackOffset;
                float rayTotalDist = distance + startBackOffset;

                RaycastHit[] hits = Physics.RaycastAll(rayStart, dirNorm, rayTotalDist, obstructionMask, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (h.collider.isTrigger) continue;
                    if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
                    if (h.collider.CompareTag("Player") || (playerFlashlight != null && h.transform.IsChildOf(playerFlashlight.transform.root))) continue;

                    // Abaikan permukaan lantai datar HANYA jika titik sentuh berada di level lantai dasar kaki (<= currentPos.y + 0.08m)
                    // Jika titik sentuh lebih tinggi dari kaki (> currentPos.y + 0.08m), itu adalah RINTANGAN PENDEK (seperti gundukan makam)!
                    if (h.normal.y >= 0.6f && h.point.y <= (currentPos.y + 0.08f)) continue;

                    float trueDist = h.distance - startBackOffset;
                    if (trueDist < minHitDist)
                    {
                        minHitDist = trueDist;
                        closestHit = h;
                        foundHit = true;
                    }
                }
            }
        }

        // 2. Low Capsule Sweep Tambahan (Memastikan tidak ada BoxCollider pendek yang terlewat di antara celah ray)
        Vector3 capBottom = currentPos + Vector3.up * 0.1f;
        Vector3 capTop = currentPos + Vector3.up * 1.2f;
        RaycastHit[] sweepHits = Physics.CapsuleCastAll(capBottom, capTop, bodyRadius * 0.85f, dirNorm, distance, obstructionMask, QueryTriggerInteraction.Ignore);
        foreach (var h in sweepHits)
        {
            if (h.collider.isTrigger) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h.collider.CompareTag("Player") || (playerFlashlight != null && h.transform.IsChildOf(playerFlashlight.transform.root))) continue;

            if (h.normal.y >= 0.6f && h.point.y <= (currentPos.y + 0.08f)) continue;

            if (h.distance < minHitDist)
            {
                minHitDist = h.distance;
                closestHit = h;
                foundHit = true;
            }
        }

        if (foundHit && minHitDist <= distance)
        {
            hitDistance = Mathf.Max(0f, minHitDist);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Membatasi pergerakan agar 100% TIDAK MENEMBUS COLLIDER dinding, batu nisan, atau objek padat lainnya.
    /// Mendukung sliding halus di permukaan dinding.
    /// </summary>
    private Vector3 ResolveWallCollisions(Vector3 currentPos, Vector3 desiredMove)
    {
        float moveDist = desiredMove.magnitude;
        if (moveDist < 0.0001f) return Vector3.zero;

        Vector3 moveDir = desiredMove / moveDist;
        float skin = Mathf.Max(0.15f, wallSkinBuffer);

        // Cek apakah langkah ini akan menabrak rintangan
        if (CheckObstacleRaycast(currentPos, moveDir, moveDist + skin, out RaycastHit hit, out float hitDist))
        {
            // Jarak yang masih aman untuk maju sebelum menabrak dinding
            float allowedDist = Mathf.Max(0f, hitDist - skin);
            Vector3 forwardStep = moveDir * Mathf.Min(moveDist, allowedDist);
            float remainingDist = moveDist - forwardStep.magnitude;

            if (remainingDist > 0.001f)
            {
                // Arahkan sisa pergerakan untuk menyusuri dinding (Wall Slide)
                Vector3 wallNormal = hit.normal;
                wallNormal.y = 0f;
                if (wallNormal.sqrMagnitude > 0.001f)
                {
                    wallNormal.Normalize();
                    Vector3 remainingVector = moveDir * remainingDist;
                    Vector3 slideMove = Vector3.ProjectOnPlane(remainingVector, wallNormal);
                    slideMove.y = 0f;

                    if (slideMove.sqrMagnitude > 0.0001f)
                    {
                        float slideDist = slideMove.magnitude;
                        Vector3 slideDir = slideMove / slideDist;

                        if (CheckObstacleRaycast(currentPos + forwardStep, slideDir, slideDist + skin, out RaycastHit slideHit, out float slideHitDist))
                        {
                            float allowedSlide = Mathf.Max(0f, slideHitDist - skin);
                            return forwardStep + slideDir * Mathf.Min(slideDist, allowedSlide);
                        }
                        else
                        {
                            return forwardStep + slideMove;
                        }
                    }
                }
            }

            return forwardStep;
        }

        return desiredMove;
    }

    /// <summary>
    /// Mencegah badan skeleton terjepit atau tertanam ke dalam collider (baik MeshCollider maupun Primitive Collider).
    /// </summary>
    private void PreventClippingIntoColliders()
    {
        Vector3 basePos = transform.position;
        float skin = Mathf.Max(0.2f, wallSkinBuffer);
        float probeDist = skin + 0.35f;

        // 8 arah mata angin untuk memeriksa penetrasi ke sekeliling tubuh
        Vector3[] probeDirs = new Vector3[]
        {
            Vector3.forward,
            (Vector3.forward + Vector3.right).normalized,
            Vector3.right,
            (Vector3.back + Vector3.right).normalized,
            Vector3.back,
            (Vector3.back + Vector3.left).normalized,
            Vector3.left,
            (Vector3.forward + Vector3.left).normalized
        };

        // Ketinggian cek: 0.12m (gundukan makam pendek), 0.4m (nisan/undakan), chestHeightOffset (dada/tembok)
        float[] checkHeights = new float[] { 0.12f, 0.40f, chestHeightOffset };

        foreach (float h in checkHeights)
        {
            Vector3 center = basePos + Vector3.up * h;

            foreach (var dir in probeDirs)
            {
                // Tembakkan ray dari luar ke arah dalam pusat skeleton
                Vector3 probeStart = center + dir * probeDist;
                Ray inwardRay = new Ray(probeStart, -dir);

                if (Physics.Raycast(inwardRay, out RaycastHit hit, probeDist, obstructionMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.isTrigger) continue;
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                    if (hit.collider.CompareTag("Player") || (playerFlashlight != null && hit.transform.IsChildOf(playerFlashlight.transform.root))) continue;

                    // Abaikan lantai datar hanya jika di level tanah dasar
                    if (hit.normal.y >= 0.6f && hit.point.y <= (basePos.y + 0.08f)) continue;

                    // Jika permukaan rintangan lebih dekat ke pusat tubuh daripada jarak skin minimum
                    float currentClearance = probeDist - hit.distance;
                    if (currentClearance < skin)
                    {
                        float pushAmount = skin - currentClearance;
                        Vector3 pushDir = hit.normal;
                        pushDir.y = 0f;
                        if (pushDir.sqrMagnitude > 0.001f)
                        {
                            transform.position += pushDir.normalized * Mathf.Min(pushAmount, 0.15f);
                            basePos = transform.position;
                        }
                    }
                }
            }
        }

        // Pengecekan overlap fisik untuk collider primitif (Box/Capsule/Sphere/Convex) di level bawah (kaki)
        float bodyRadius = Mathf.Clamp(bodyWidth * 0.75f, 0.35f, 0.55f);
        Collider[] lowCols = Physics.OverlapSphere(basePos + Vector3.up * 0.20f, bodyRadius, obstructionMask, QueryTriggerInteraction.Ignore);
        foreach (var col in lowCols)
        {
            if (col.isTrigger) continue;
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
            if (col.CompareTag("Player") || (playerFlashlight != null && col.transform.IsChildOf(playerFlashlight.transform.root))) continue;
            if (col is MeshCollider mc && !mc.convex) continue;

            Vector3 closest = col.ClosestPoint(basePos + Vector3.up * 0.20f);
            Vector3 push = (basePos + Vector3.up * 0.20f) - closest;
            push.y = 0f;
            float dist = push.magnitude;
            if (dist < bodyRadius && dist > 0.001f)
            {
                float pushDist = bodyRadius - dist;
                transform.position += push.normalized * pushDist;
                basePos = transform.position;
            }
        }
    }

    /// <summary>
    /// Menjaga agar kaki skeleton selalu menempel pas di permukaan tanah/lantai (tidak melayang atau tenggelam di lereng/undakan).
    /// </summary>
    private Vector3 SnapToGround(Vector3 position, bool immediate = false)
    {
        Vector3 rayStart = position + Vector3.up * 1.5f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 4.0f, obstructionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider.isTrigger) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h.collider.CompareTag("Player") || (playerFlashlight != null && h.transform.IsChildOf(playerFlashlight.transform.root))) continue;

            // Pastikan ini adalah lantai/tanah yang landai (normal tegak ke atas)
            if (h.normal.y >= 0.55f)
            {
                // Jangan snap ke atas objek rintangan yang tingginya di atas 0.45m dari posisi saat ini
                if (h.point.y > position.y + 0.45f) continue;

                if (immediate)
                {
                    position.y = h.point.y;
                }
                else
                {
                    position.y = Mathf.Lerp(position.y, h.point.y, Time.deltaTime * 15f);
                }
                break;
            }
        }

        return position;
    }

    private void CatchPlayer()
    {
        // Cegah kematian jika sedang disorot senter atau sudah menangkap
        if (isFrozenByLight || hasCaughtPlayer) return;

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

        transform.position = SnapToGround(transform.position, true);

        gameObject.SetActive(true);
        isAnomalyActive = true;
        hasCaughtPlayer = false;
        isFrozenByLight = false;

        if (redEyes != null)
        {
            redEyes.SetHeadSocketMode(true);
            if (!redEyes.transform.IsChildOf(transform))
            {
                redEyes.transform.SetParent(transform, true);
            }
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

        // Visualisasi hitbox multi-point deteksi senter pada tubuh skeleton
        Gizmos.color = Color.yellow;
        Vector3 basePos = transform.position;
        Vector3 rightDir = transform.right * bodyWidth;

        Vector3 headPos = basePos + Vector3.up * headHeightOffset;
        Vector3 neckPos = basePos + Vector3.up * Mathf.Lerp(chestHeightOffset, headHeightOffset, 0.5f);
        Vector3 chestPos = basePos + Vector3.up * chestHeightOffset;
        Vector3 waistPos = basePos + Vector3.up * Mathf.Lerp(feetHeightOffset, chestHeightOffset, 0.5f);
        Vector3 feetPos = basePos + Vector3.up * feetHeightOffset;

        Gizmos.DrawSphere(headPos, 0.15f);
        Gizmos.DrawSphere(neckPos, 0.12f);
        Gizmos.DrawSphere(chestPos, 0.15f);
        Gizmos.DrawSphere(waistPos, 0.12f);
        Gizmos.DrawSphere(feetPos, 0.15f);
        Gizmos.DrawSphere(chestPos - rightDir, 0.12f);
        Gizmos.DrawSphere(chestPos + rightDir, 0.12f);

        // Titik bentangan tangan (kiri & kanan)
        float armSpan = Mathf.Max(bodyWidth * 2.0f, 1.4f);
        Vector3 armRight = transform.right * armSpan;
        Gizmos.DrawSphere(chestPos - armRight * 0.6f, 0.10f); // Siku Kiri
        Gizmos.DrawSphere(chestPos + armRight * 0.6f, 0.10f); // Siku Kanan
        Gizmos.DrawSphere(chestPos - armRight, 0.12f);        // Ujung Tangan Kiri
        Gizmos.DrawSphere(chestPos + armRight, 0.12f);        // Ujung Tangan Kanan

        Gizmos.DrawLine(feetPos, headPos);
        Gizmos.DrawLine(chestPos - armRight, chestPos + armRight);

        // Visualisasi batas collision & deteksi rintangan
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(basePos + Vector3.up * (headHeightOffset * 0.5f), new Vector3(bodyWidth * 1.5f, headHeightOffset, wallSkinBuffer * 2f));

        if (spawnPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }

    /// <summary>
    /// Memasang proxy otomatis pada child collider bertipe Trigger (seperti Box Collider Trigger di Cylinder.004)
    /// </summary>
    private void SetupChildTriggerProxies()
    {
        Collider[] allCols = GetComponentsInChildren<Collider>(true);
        foreach (var col in allCols)
        {
            if (col.isTrigger)
            {
                StalkingSkeletonTriggerProxy proxy = col.GetComponent<StalkingSkeletonTriggerProxy>();
                if (proxy == null)
                {
                    proxy = col.gameObject.AddComponent<StalkingSkeletonTriggerProxy>();
                }
                proxy.parentAnomaly = this;
            }
        }
    }

    /// <summary>
    /// Menerima notifikasi dari Trigger Box Collider saat player menyentuh tubuh skeleton
    /// </summary>
    public void NotifyTriggerHit(Collider other)
    {
        if (!isAnomalyActive || hasCaughtPlayer || isFrozenByLight) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || (targetPlayer != null && other.transform.IsChildOf(targetPlayer.root)))
        {
            CatchPlayer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        NotifyTriggerHit(other);
    }
}

/// <summary>
/// Forwarder otomatis agar event OnTriggerEnter pada child mesh (Cylinder.004) diteruskan ke StalkingSkeletonAnomaly
/// </summary>
public class StalkingSkeletonTriggerProxy : MonoBehaviour
{
    public StalkingSkeletonAnomaly parentAnomaly;

    private void OnTriggerEnter(Collider other)
    {
        if (parentAnomaly != null)
        {
            parentAnomaly.NotifyTriggerHit(other);
        }
    }
}
