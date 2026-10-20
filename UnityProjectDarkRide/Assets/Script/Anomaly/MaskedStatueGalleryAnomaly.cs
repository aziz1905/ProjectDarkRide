using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anomali #3 & #4: MaskedStatueGalleryAnomaly (Patung Elit Bertopeng di Zona Galeri & Ruang Panel).
/// Alur Kejadian:
/// 1. Di Zona Galeri, patung-patung elit global bertopeng (MaskMan) berdiri membeku sebagai patung pajangan.
/// 2. Player masuk ke Ruang Panel 1 dan mengganti sekring (FuseBox).
/// 3. Begitu sekring selesai dipasang:
///    - Kereta Wahana (Dark Ride Cart) otomatis diposisikan & siap menunggu di rel depan pintu Ruang Panel!
///    - Sosok bertopeng mendadak muncul di sudut gelap ruang panel mengawasi player!
/// 4. Saat player keluar dari pintu ruang panel (atau naik ke kereta):
///    - Sosok di sudut ruang panel lenyap.
///    - Pasukan orang bertopeng di belakang langsung mengejar!
///    - Patung-patung di depan tetap diam di tempatnya dulu; begitu pemain (atau kereta) sejajar/melewatinya,
///      patung tersebut baru bangun dan ikut mengejar dari belakang!
/// 5. Pemain naik ke kereta wahana dan mengendarainya melintasi rute galeri untuk melarikan diri.
/// 6. Jika berhasil keluar dari Zona Galeri, pengejaran berhenti (semua patung membeku/lenyap, musik fade out).
/// 7. Jika tertangkap -> Jumpscare & Sanksi Kematian Perusahaan (Sanction & Retry).
/// </summary>
public class MaskedStatueGalleryAnomaly : MonoBehaviour
{
    [System.Serializable]
    public class GalleryChaser
    {
        [Tooltip("Label pengenal (misal: Pasukan Belakang 1, Patung Depan Tikungan 1)")]
        public string label = "Chaser";

        [Tooltip("Objek/Prefab orang bertopeng (MaskMan)")]
        public GameObject chaserObject;

        [Tooltip("CENTANG jika ini pasukan belakang (langsung mengejar seketika saat keluar ruang panel).\nHILANGKAN CENTANG jika ini patung depan (diam dulu, baru bangun saat player sejajar/lewat).")]
        public bool isImmediateChaser = false;

        [Tooltip("Jarak pemicu bangun (meter). Untuk patung depan: saat player mendekat/sejajar dalam jarak ini, patung bangun mengejar.")]
        public float awakenDistance = 6.0f;

        [Tooltip("Jeda waktu diam/bereaksi sebelum mulai berlari mengejar setelah patung bangun (detik). Jika 0, memakai nilai default frontStatueAwakenDelay.")]
        public float awakenReactionDelay = 0f;

        [HideInInspector] public bool isAwakened = false;
        [HideInInspector] public float awakenTimer = 0f;
        [HideInInspector] public float activeChaseTime = 0f;
        [HideInInspector] public Vector3 initialPosition;
        [HideInInspector] public Quaternion initialRotation;
    }

    [Header("1. Ruang Panel 1 (Corner Stalker)")]
    [Tooltip("Drag FuseBoxController dari Ruang Panel 1 ke sini")]
    [SerializeField] private FuseBoxController panelFuseBox;

    [Tooltip("Sosok orang bertopeng di pojok ruang panel (akan aktif setelah fuse diganti)")]
    [SerializeField] private GameObject cornerMaskMan;

    [Tooltip("Lampu di ruang panel yang akan berkedip sesaat saat sosok di pojok muncul (Opsional)")]
    [SerializeField] private Light roomPanelLight;

    [Tooltip("Audio sting / bisikan seram saat sosok muncul di pojok ruang panel")]
    [SerializeField] private AudioClip cornerStalkerSFX;

    [Header("2. Pintu Keluar Ruang Panel")]
    [Tooltip("Trigger di ambang pintu keluar ruang panel menuju galeri")]
    [SerializeField] private Collider exitDoorTrigger;

    [Header("3. Kereta Wahana (Dark Ride Cart)")]
    [Tooltip("Drag DarkRideCartController ke sini agar kereta otomatis diposisikan di depan ruang panel saat darurat")]
    [SerializeField] private DarkRideCartController darkRideCart;

    [Tooltip("Titik rel di depan pintu Ruang Panel tempat kereta menunggu pemain (Opsional, jika kosong menggunakan posisi pintu)")]
    [SerializeField] private Transform cartWaitPoint;

    [Header("4. Formasi Pasukan Belakang (Cluster di Belakang Kereta)")]
    [Tooltip("Opsional: Drag Transform titik kumpul pasukan di belakang kereta (misal: GameObject point (1)). Jika diisi, formasi akan selalu berpusat tepat di titik ini!")]
    [SerializeField] private Transform rearClusterPoint;

    [Tooltip("Jarak baris terdepan pasukan di belakang ekor kereta wahana (meter) jika rearClusterPoint kosong")]
    [SerializeField] private float rearClusterDistance = 6.5f;

    [Tooltip("Jarak kedalaman antar baris patung di belakang kereta (meter)")]
    [SerializeField] private float rearRowSpacing = 1.8f;

    [Tooltip("Lebar sebaran horizontal patung di belakang kereta (meter)")]
    [SerializeField] private float rearSpreadWidth = 2.6f;

    [Header("5. Pasukan Pengejar & Patung Ambush")]
    [Tooltip("Daftar seluruh patung/orang bertopeng di galeri")]
    [SerializeField] private List<GalleryChaser> chasers = new List<GalleryChaser>();

    [Tooltip("Kecepatan lari patung pengejar (m/s). Kecepatan maksimal kereta wahana adalah 4.0 m/s.")]
    [SerializeField] private float chaseSpeed = 3.85f;

    [Tooltip("Jarak tangkap yang memicu jumpscare & kematian pemain (meter)")]
    [SerializeField] private float catchDistance = 1.35f;

    [Tooltip("Jeda waktu diam/bereaksi sebelum patung depan mulai berlari mengejar setelah terbangun (detik).")]
    [SerializeField] private float frontStatueAwakenDelay = 1.2f;

    [Tooltip("Toleransi waktu minimal patung harus sudah aktif berlari mengejar sebelum bisa menangkap pemain (detik) untuk mencegah jumpscare mendadak/flick.")]
    [SerializeField] private float chaserCatchGracePeriod = 1.2f;

    [Tooltip("Offset ketinggian patung dari lantai (meter). Jika patung tampak tenggelam di bawah permukaan ubin/rel, sesuaikan nilai ini (misal: 0.0 atau 0.05).")]
    [SerializeField] private float chaserVerticalOffset = 0.0f;

    [Header("6. Batas Zona Galeri (Safe Zone)")]
    [Tooltip("Trigger volume batas Zona Galeri. Jika player keluar dari area ini, pengejaran berhenti!")]
    [SerializeField] private Collider galleryZoneBoundary;

    [Header("7. Audio & Jumpscare")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Musik tegang / deru langkah kaki cepat saat dikejar di galeri")]
    [SerializeField] private AudioClip chaseThemeSFX;
    [Tooltip("Suara patung depan terbangun / retakan batu saat patung depan hidup mengejar")]
    [SerializeField] private AudioClip statueAwakenSFX;
    [Tooltip("Suara teriakan / sting keras jika pemain tertangkap oleh orang bertopeng")]
    [SerializeField] private AudioClip jumpscareCatchSFX;

    [Header("8. Kematian / Sanksi Korporat")]
    [SerializeField] private string sanctionReason = "Diserang dan dilumpuhkan oleh anomali patung elit bertopeng di Zona Galeri saat menjalankan shift malam.";

    // Internal State
    private Transform cachedPlayerTransform;
    private bool isFuseReplaced = false;
    private bool isCornerManSpawned = false;
    private bool isChasing = false;
    private bool isAnomalyCompleted = false;
    private bool hasCaughtPlayer = false;
    private bool hasPlayerEnteredGallery = false;
    private bool hasClusteredRearChasers = false;
    private float chaseTimer = 0f;
    private Coroutine activeChaseCoroutine;

    private void Awake()
    {
        // Pastikan sosok pojok dinormalkan pivotnya dan awalnya NONAKTIF
        if (cornerMaskMan != null)
        {
            cornerMaskMan.SetActive(true);
            NormalizeChaserPivot(cornerMaskMan);
            cornerMaskMan.SetActive(false);
        }

        // Catat posisi awal patung-patung
        if (chasers != null)
        {
            foreach (var chaser in chasers)
            {
                if (chaser != null && chaser.chaserObject != null)
                {
                    // Pastikan patung aktif di galeri dan seluruh anak objeknya aktif
                    chaser.chaserObject.SetActive(true);
                    foreach (var t in chaser.chaserObject.GetComponentsInChildren<Transform>(true))
                    {
                        t.gameObject.SetActive(true);
                        t.gameObject.layer = 0;
                    }
                    foreach (var r in chaser.chaserObject.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled = true;
                        if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                    }
                    foreach (var anim in chaser.chaserObject.GetComponentsInChildren<Animator>(true))
                    {
                        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    }

                    // Normalkan pivot ke posisi fisik visual mesh tanpa mengubah posisi dunianya di galeri
                    NormalizeChaserPivot(chaser.chaserObject);

                    chaser.initialPosition = chaser.chaserObject.transform.position;
                    chaser.initialRotation = chaser.chaserObject.transform.rotation;
                    chaser.isAwakened = false;
                    chaser.awakenTimer = 0f;
                    chaser.activeChaseTime = 0f;
                }
            }
        }
    }

    private void Start()
    {
        // Hubungkan event saat fuse berhasil diganti
        if (panelFuseBox != null)
        {
            panelFuseBox.OnFuseReplaced.AddListener(OnFuseReplacedHandler);
        }

        if (darkRideCart == null)
        {
            darkRideCart = FindObjectOfType<DarkRideCartController>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupExitDoorTriggerListener();
    }

    private void OnDestroy()
    {
        if (panelFuseBox != null)
        {
            panelFuseBox.OnFuseReplaced.RemoveListener(OnFuseReplacedHandler);
        }
    }

    private void SetupExitDoorTriggerListener()
    {
        if (exitDoorTrigger != null)
        {
            var listener = exitDoorTrigger.GetComponent<GalleryDoorTriggerBridge>();
            if (listener == null) listener = exitDoorTrigger.gameObject.AddComponent<GalleryDoorTriggerBridge>();
            listener.onPlayerEnter = () =>
            {
                if (isFuseReplaced && !isChasing)
                {
                    StartGalleryChase();
                }
            };
        }
    }

    /// <summary>
    /// Mencari posisi Player atau Kamera Player secara akurat tanpa terikat pada root scene.
    /// </summary>
    public Vector3 GetPlayerPosition()
    {
        // Jika pemain sedang naik kereta, prioritaskan kamera/kursi mata pemain
        if (darkRideCart != null && darkRideCart.IsPlayerSeated)
        {
            if (Camera.main != null) return Camera.main.transform.position;
            if (cachedPlayerTransform != null) return cachedPlayerTransform.position;
        }

        if (cachedPlayerTransform != null && cachedPlayerTransform.gameObject.activeInHierarchy)
        {
            return cachedPlayerTransform.position;
        }

        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null)
        {
            cachedPlayerTransform = pm.transform;
            return cachedPlayerTransform.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            cachedPlayerTransform = p.transform;
            return cachedPlayerTransform.position;
        }

        GameObject p3 = GameObject.Find("playerDay3");
        if (p3 != null)
        {
            cachedPlayerTransform = p3.transform;
            return cachedPlayerTransform.position;
        }

        return transform.position;
    }

    private void Update()
    {
        if (isAnomalyCompleted) return;

        Vector3 playerPos = GetPlayerPosition();

        // 1. POJOK RUANG PANEL: Sosok bertopeng menatap dingin ke arah player
        if (isCornerManSpawned && cornerMaskMan != null && cornerMaskMan.activeSelf)
        {
            Vector3 lookDir = playerPos - cornerMaskMan.transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                // Kompensasi 180 derajat karena orientasi model FBX menghadap ke belakang
                Quaternion targetRot = Quaternion.LookRotation(lookDir) * Quaternion.Euler(0f, 180f, 0f);
                cornerMaskMan.transform.rotation = Quaternion.Slerp(cornerMaskMan.transform.rotation, targetRot, Time.deltaTime * 6f);
            }
        }

        // 2. CEK PEMICU KELUAR PINTU (Jika pemain melintasi trigger pintu atau mendekatinya)
        if (isFuseReplaced && !isChasing)
        {
            // Jika pemain langsung naik ke kereta di depan pintu -> Langsung picu pengejaran!
            if (darkRideCart != null && darkRideCart.IsPlayerSeated)
            {
                StartGalleryChase();
                return;
            }

            // Pengecekan jarak ke pintu keluar
            if (exitDoorTrigger != null)
            {
                Vector3 doorCenter = exitDoorTrigger.bounds.center;
                Vector2 pXZ = new Vector2(playerPos.x, playerPos.z);
                Vector2 dXZ = new Vector2(doorCenter.x, doorCenter.z);
                float distToDoor = Vector2.Distance(pXZ, dXZ);

                float triggerRadius = Mathf.Max(exitDoorTrigger.bounds.extents.x, exitDoorTrigger.bounds.extents.z) + 1.8f;

                if (distToDoor <= triggerRadius || exitDoorTrigger.bounds.Contains(playerPos))
                {
                    StartGalleryChase();
                    return;
                }
            }
        }

        // 3. CEK BATAS ZONA GALERI: Jika player berhasil keluar dari galeri setelah dikejar, hentikan pengejaran
        if (isChasing && galleryZoneBoundary != null)
        {
            if (galleryZoneBoundary.bounds.Contains(playerPos))
            {
                hasPlayerEnteredGallery = true;
            }

            // Hanya hentikan jika pemain sudah berada di galeri dan melarikan diri keluar
            if (hasPlayerEnteredGallery && chaseTimer > 3.0f)
            {
                if (!galleryZoneBoundary.bounds.Contains(playerPos))
                {
                    StopGalleryChase(playerEscaped: true);
                }
            }
        }
    }

    /// <summary>
    /// Dipanggil otomatis saat pemain selesai memasang fuse baru di Ruang Panel 1.
    /// </summary>
    public void OnFuseReplacedHandler()
    {
        if (isFuseReplaced || isAnomalyCompleted) return;
        isFuseReplaced = true;

        // 🚂 SIAPKAN KERETA WAHANA: Posisikan kereta di rel depan pintu ruang panel
        RepositionCartToFrontOfPanel();

        // 👥 KUMPULKAN PASUKAN BELAKANG DI BELAKANG KERETA
        ClusterImmediateChasersBehindCart();

        // 👁️ MUNCULKAN SOSOK DI POJOK RUANG PANEL
        StartCoroutine(SpawnCornerStalkerRoutine());
    }

    private void RepositionCartToFrontOfPanel()
    {
        if (darkRideCart == null)
        {
            darkRideCart = FindObjectOfType<DarkRideCartController>();
        }

        if (darkRideCart != null)
        {
            Transform waitSpot = cartWaitPoint != null ? cartWaitPoint : (exitDoorTrigger != null ? exitDoorTrigger.transform : transform);
            darkRideCart.RepositionToTargetTransform(waitSpot);
        }
    }

    /// <summary>
    /// Mengumpulkan seluruh pasukan belakang (isImmediateChaser = true) langsung di belakang kereta wahana atau di titik rearClusterPoint.
    /// Membentuk formasi kerumunan gerombolan bertopeng yang padat, rapi, dan mengerikan di atas jalur rel.
    /// </summary>
    [ContextMenu("Kumpulkan Pasukan Belakang di Belakang Kereta")]
    public void ClusterImmediateChasersBehindCart()
    {
        if (chasers == null || chasers.Count == 0) return;

        List<GalleryChaser> rearChasers = new List<GalleryChaser>();
        foreach (var chaser in chasers)
        {
            if (chaser != null && chaser.chaserObject != null && chaser.isImmediateChaser)
            {
                rearChasers.Add(chaser);
            }
        }

        if (rearChasers.Count == 0) return;

        Vector3 baseCenter;
        Vector3 forwardDir;

        // 1. TENTUKAN PUSAT FORMASI & ARAH HADAP
        // Prioritaskan posisi tepat di rel di belakang ekor kereta wahana agar selalu berjarak ideal di belakang kereta
        if (darkRideCart == null)
        {
            darkRideCart = FindObjectOfType<DarkRideCartController>();
        }

        if (rearClusterPoint != null)
        {
            baseCenter = rearClusterPoint.position;
            forwardDir = rearClusterPoint.forward;
        }
        else if (darkRideCart != null)
        {
            baseCenter = darkRideCart.GetSplinePositionBehind(rearClusterDistance);
            forwardDir = darkRideCart.GetSplineForwardDirection();
        }
        else
        {
            baseCenter = transform.position;
            forwardDir = transform.forward;
        }

        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 0.001f) forwardDir = Vector3.forward;
        forwardDir.Normalize();

        Vector3 backDir = -forwardDir;
        Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;

        // Tabel sebaran organik (tidak berbaris rapi seperti tentara, melainkan gerombolan seram yang berantakan & padat)
        // (xRatio: sebaran kiri-kanan [-1 s.d. +1], zRatio: kedalaman mundur [0 s.d. 1], angleOffset: variasi sudut hadap)
        (float xRatio, float zRatio, float angleOffset)[] organicOffsets = new (float, float, float)[]
        {
            (-0.30f, 0.05f, -8f),   // Barisan depan tengah-kiri
            ( 0.40f, 0.00f, +6f),   // Paling depan kanan
            (-0.80f, 0.16f, +14f),  // Sayap kiri depan
            ( 0.05f, 0.22f, -4f),   // Tengah depan
            ( 0.85f, 0.26f, -12f),  // Sayap kanan depan
            (-0.48f, 0.38f, +8f),   // Tengah kiri
            ( 0.38f, 0.44f, -6f),   // Tengah kanan
            (-0.10f, 0.52f, +10f),  // Tengah
            ( 0.72f, 0.60f, -10f),  // Belakang kanan
            (-0.78f, 0.66f, +12f),  // Belakang kiri
            ( 0.20f, 0.74f, -5f),   // Belakang tengah
            (-0.38f, 0.84f, +7f),   // Belakang kiri dalam
            ( 0.55f, 0.90f, -9f),   // Paling belakang kanan
            (-0.02f, 0.96f, +3f),   // Paling belakang tengah
            (-0.65f, 0.88f, +6f),   // Cadangan
            ( 0.75f, 0.94f, -8f)    // Cadangan
        };

        float clusterDepth = Mathf.Clamp(rearRowSpacing * 1.8f, 2.5f, 3.5f);
        float halfWidth = Mathf.Clamp(rearSpreadWidth * 0.45f, 0.8f, 1.2f);

        for (int i = 0; i < rearChasers.Count; i++)
        {
            GalleryChaser chaser = rearChasers[i];
            var pattern = organicOffsets[i % organicOffsets.Length];

            // Variasi deterministik unik per indeks patung agar semakin organik
            float jitterX = ((i * 7) % 7 - 3) * 0.05f;
            float jitterZ = ((i * 11) % 7 - 3) * 0.07f;

            float xOffset = (pattern.xRatio * halfWidth) + jitterX;
            float zDist = (pattern.zRatio * clusterDepth) + jitterZ;

            Vector3 spawnPos = baseCenter + (backDir * zDist) + (rightDir * xOffset);

            // Tempelkan kaki ke permukaan lantai / rel (Raycast dari atas ke bawah)
            // Abaikan collider MaskMan lain / dinding vertikal agar patung tidak menumpuk ke atas atau melayang
            Vector3 rayStart = spawnPos;
            rayStart.y = baseCenter.y + 2.5f;
            RaycastHit[] spawnHits = Physics.RaycastAll(rayStart, Vector3.down, 6.0f, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
            System.Array.Sort(spawnHits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundFloor = false;
            foreach (var sHit in spawnHits)
            {
                if (sHit.collider.isTrigger) continue;
                if (sHit.collider.name.Contains("MaskMan") || sHit.collider.transform.IsChildOf(transform)) continue;
                if (sHit.normal.y < 0.5f) continue; // Abaikan dinding miring / vertikal

                spawnPos.y = sHit.point.y + chaserVerticalOffset;
                foundFloor = true;
                break;
            }

            if (!foundFloor)
            {
                // Fallback referensi elevasi lantai jika raycast meleset
                spawnPos.y = baseCenter.y + chaserVerticalOffset;
            }

            // 🛡️ ANTI-PENETRASI DINDING: Pastikan posisi kumpul awal tidak beririsan dengan tembok lorong
            Vector3 spawnWaist = spawnPos + Vector3.up * 0.9f;
            Collider[] spawnWallHits = Physics.OverlapSphere(spawnWaist, 0.42f, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
            foreach (var col in spawnWallHits)
            {
                if (col.isTrigger || col.transform.IsChildOf(chaser.chaserObject.transform)) continue;
                if (col.GetComponentInParent<DarkRideCartController>() != null) continue;
                if (col is MeshCollider mc && !mc.convex) continue;

                Vector3 closest = col.ClosestPoint(spawnWaist);
                Vector3 pDir = spawnWaist - closest;
                pDir.y = 0f;
                float d = pDir.magnitude;
                if (d < 0.42f && d > 0.001f)
                {
                    spawnPos += pDir.normalized * (0.42f - d);
                    spawnWaist = spawnPos + Vector3.up * 0.9f;
                }
            }

            // Hadap ke depan (ke arah rel/pemain) dengan variasi sudut alami, dikompensasi 180 derajat karena model FBX terbalik
            Quaternion lookRot = Quaternion.LookRotation(forwardDir, Vector3.up) * Quaternion.Euler(0f, 180f + pattern.angleOffset, 0f);

            // Normalkan titik tumpu (pivot) agar mesh berada tepat di root objek
            NormalizeChaserPivot(chaser.chaserObject);

            chaser.chaserObject.transform.SetPositionAndRotation(spawnPos, lookRot);
            chaser.chaserObject.SetActive(true);

            // Pastikan seluruh anak GameObject dan MeshRenderer aktif dan Layer Default (0)
            foreach (var t in chaser.chaserObject.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.SetActive(true);
                t.gameObject.layer = 0;
            }
            foreach (var r in chaser.chaserObject.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }
            foreach (var anim in chaser.chaserObject.GetComponentsInChildren<Animator>(true))
            {
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        hasClusteredRearChasers = true;
    }

    private IEnumerator SpawnCornerStalkerRoutine()
    {
        yield return new WaitForSeconds(0.4f);

        // Efek lampu berkedip sesaat (Flicker)
        if (roomPanelLight != null)
        {
            float origIntensity = roomPanelLight.intensity;
            roomPanelLight.intensity = origIntensity * 0.1f;
            yield return new WaitForSeconds(0.1f);
            roomPanelLight.intensity = origIntensity * 1.5f;
            yield return new WaitForSeconds(0.08f);
            roomPanelLight.intensity = origIntensity * 0.2f;
            yield return new WaitForSeconds(0.12f);
            roomPanelLight.intensity = origIntensity;
        }

        // Munculkan sosok di pojok ruang panel
        if (cornerMaskMan != null)
        {
            cornerMaskMan.SetActive(true);
            isCornerManSpawned = true;

            Vector3 toPlayer = GetPlayerPosition() - cornerMaskMan.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                cornerMaskMan.transform.rotation = Quaternion.LookRotation(toPlayer) * Quaternion.Euler(0f, 180f, 0f);
            }
            NormalizeChaserPivot(cornerMaskMan);

            foreach (var r in cornerMaskMan.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
            }
            foreach (var t in cornerMaskMan.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = 0;
            }

            if (audioSource != null && cornerStalkerSFX != null)
            {
                audioSource.PlayOneShot(cornerStalkerSFX);
            }
        }
    }

    /// <summary>
    /// Memulai aksi pengejaran di luar saat pemain melangkah keluar dari Ruang Panel 1.
    /// </summary>
    [ContextMenu("Test Start Chase")]
    public void StartGalleryChase()
    {
        if (isChasing || isAnomalyCompleted) return;
        isChasing = true;
        chaseTimer = 0f;

        // 1. Hilangkan sosok yang di pojok ruang panel
        if (cornerMaskMan != null)
        {
            cornerMaskMan.SetActive(false);
            isCornerManSpawned = false;
        }

        // 2. Pastikan pasukan belakang sudah terkumpul rapi di belakang kereta
        if (!hasClusteredRearChasers)
        {
            ClusterImmediateChasersBehindCart();
        }

        // 3. Inisialisasi status para pengejar
        if (chasers != null)
        {
            for (int i = 0; i < chasers.Count; i++)
            {
                GalleryChaser chaser = chasers[i];
                if (chaser == null || chaser.chaserObject == null) continue;

                chaser.chaserObject.SetActive(true);
                foreach (var t in chaser.chaserObject.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.SetActive(true);
                    t.gameObject.layer = 0;
                }
                foreach (var r in chaser.chaserObject.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                    if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                }
                foreach (var anim in chaser.chaserObject.GetComponentsInChildren<Animator>(true))
                {
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                if (chaser.isImmediateChaser)
                {
                    // Pasukan belakang langsung bangun seketika
                    chaser.isAwakened = true;
                    chaser.awakenTimer = 0f;
                    chaser.activeChaseTime = 0f;
                }
                else
                {
                    // Patung depan tetap diam membeku di tempatnya sampai player sejajar/lewat
                    chaser.isAwakened = false;
                    chaser.awakenTimer = 0f;
                    chaser.activeChaseTime = 0f;
                    if (chaser.initialPosition != Vector3.zero)
                    {
                        chaser.chaserObject.transform.SetPositionAndRotation(chaser.initialPosition, chaser.initialRotation);
                    }
                }
            }
        }

        // 4. Putar musik kejar-kejaran seram
        if (audioSource != null && chaseThemeSFX != null)
        {
            audioSource.clip = chaseThemeSFX;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (activeChaseCoroutine != null) StopCoroutine(activeChaseCoroutine);
        activeChaseCoroutine = StartCoroutine(ChaseRoutine());
    }

    private IEnumerator ChaseRoutine()
    {
        while (isChasing && !hasCaughtPlayer)
        {
            // Batasi delta time maksimal 50ms untuk mencegah efek loncat/blink jika terjadi lag spike
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            chaseTimer += dt;
            Vector3 playerPos = GetPlayerPosition();
            Vector3 cartRearPos = (darkRideCart != null && darkRideCart.IsPlayerSeated) ? darkRideCart.GetRearPosition() : playerPos;

            if (chasers != null)
            {
                for (int i = 0; i < chasers.Count; i++)
                {
                    GalleryChaser chaser = chasers[i];
                    if (chaser == null || chaser.chaserObject == null) continue;

                    if (!chaser.chaserObject.activeSelf) chaser.chaserObject.SetActive(true);

                    Transform chaserTrans = chaser.chaserObject.transform;
                    Vector3 chaserPos = chaserTrans.position;

                    // TARGET PENGEJARAN BERBEDA:
                    // - Pasukan Belakang (isImmediateChaser = true): Mengikuti rel mengejar ekor kereta dari belakang.
                    // - Patung Depan (isImmediateChaser = false): Ambush dari depan/samping, mengejar langsung ke kursi pemain (BUKAN ikut ke ekor belakang!).
                    Vector3 targetPursuitPos = chaser.isImmediateChaser ? cartRearPos : playerPos;

                    Vector3 toTarget = targetPursuitPos - chaserPos;
                    Vector3 horizontalToTarget = toTarget;
                    horizontalToTarget.y = 0f;
                    float horizontalDist = horizontalToTarget.magnitude;
                    float verticalDist = Mathf.Abs(toTarget.y);

                    // Jarak sebenarnya ke tubuh/kamera pemain (bukan ke bemper belakang)
                    Vector3 toPlayerHorizontal = playerPos - chaserPos;
                    toPlayerHorizontal.y = 0f;
                    float horizontalDistToPlayer = toPlayerHorizontal.magnitude;
                    float verticalDistToPlayer = Mathf.Abs(playerPos.y - chaserPos.y);

                    // 1. JIKA BELUM BANGUN (PATUNG DEPAN): Cek apakah player sudah mendekat
                    if (!chaser.isAwakened)
                    {
                        float distToPlayer = Vector3.Distance(playerPos, chaserPos);
                        if (distToPlayer <= chaser.awakenDistance)
                        {
                            chaser.isAwakened = true;
                            float delay = chaser.awakenReactionDelay > 0.05f ? chaser.awakenReactionDelay : frontStatueAwakenDelay;
                            chaser.awakenTimer = delay;
                            chaser.activeChaseTime = 0f;

                            if (audioSource != null && statueAwakenSFX != null)
                            {
                                audioSource.PlayOneShot(statueAwakenSFX);
                            }
                        }
                        else
                        {
                            // Tatap dingin ke player saat player mendekat di kejauhan
                            Vector3 lookAtDir = playerPos - chaserPos;
                            lookAtDir.y = 0f;
                            if (lookAtDir.sqrMagnitude > 0.01f)
                            {
                                Quaternion headTurn = Quaternion.LookRotation(lookAtDir, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
                                chaserTrans.rotation = Quaternion.Slerp(chaserTrans.rotation, headTurn, dt * 4f);
                            }
                            continue; // Belum mengejar, tetap diam di tempat
                        }
                    }

                    // 1b. JIKA SEDANG DALAM MASA JEDA REAKSI BANGUN:
                    // Tetap berdiri diam di posisinya, hanya menolehkan kepala/badan menatap tajam ke player/kereta yang melintas
                    if (chaser.awakenTimer > 0f)
                    {
                        chaser.awakenTimer -= dt;

                        Vector3 lookAtDir = playerPos - chaserPos;
                        lookAtDir.y = 0f;
                        if (lookAtDir.sqrMagnitude > 0.01f)
                        {
                            Quaternion headTurn = Quaternion.LookRotation(lookAtDir, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
                            chaserTrans.rotation = Quaternion.Slerp(chaserTrans.rotation, headTurn, dt * 12f);
                        }

                        // Selama masa jeda reaksi, patung TIDAK BERGERAK dan TIDAK BISA MENANGKAP PEMAIN!
                        continue;
                    }

                    // Tambahkan durasi waktu aktif berlari mengejar
                    chaser.activeChaseTime += dt;

                    // 2. JIKA SUDAH AKTIF BERLARI: Cek apakah berhasil menangkap target
                    // PENTING: Untuk patung depan/ambush, grace period minimal agar langsung bisa menangkap saat menyergap kereta.
                    // Jika player berada di kursi kereta, radius tangkap dibuat fleksibel (1.85m) agar tidak meleset saat patung melompat/naik ke kereta.
                    float effectiveGracePeriod = chaser.isImmediateChaser ? chaserCatchGracePeriod : 0.05f;
                    float effectiveCatchDistance = (darkRideCart != null && darkRideCart.IsPlayerSeated) ? Mathf.Max(catchDistance, 1.85f) : catchDistance;

                    if (chaseTimer > 1.5f && chaser.activeChaseTime > effectiveGracePeriod && horizontalDistToPlayer <= effectiveCatchDistance && verticalDistToPlayer <= 2.5f)
                    {
                        CatchPlayer(chaser.chaserObject);
                        yield break;
                    }

                    // 3. PEMISAHAN ANTAR PATUNG (Khusus Pasukan Belakang / Gerombolan agar tidak bertumpuk)
                    Vector3 separation = Vector3.zero;
                    if (chaser.isImmediateChaser)
                    {
                        for (int j = 0; j < chasers.Count; j++)
                        {
                            if (i == j) continue;
                            var other = chasers[j];
                            if (other == null || other.chaserObject == null || !other.chaserObject.activeSelf || !other.isAwakened) continue;

                            // Jangan terpengaruh atau mempengaruhi patung yang masih dalam jeda bangun
                            if (other.awakenTimer > 0f) continue;

                            Vector3 diff = chaserPos - other.chaserObject.transform.position;
                            diff.y = 0f;
                            float distBetween = diff.magnitude;

                            // 👥 HARD ANTI-OVERLAP: Jika jarak kurang dari 0.85m (badan saling menempel), saling tolak seketika agar tidak numpuk!
                            if (distBetween < 0.85f && distBetween > 0.001f)
                            {
                                float pushOut = 0.85f - distBetween;
                                chaserPos += (diff / distBetween) * (pushOut * 0.5f);
                            }

                            // 👥 SOFT SEPARATION: Jaga jarak radius 1.8m agar melebar organik seperti gerombolan
                            if (distBetween > 0.05f && distBetween < 1.8f)
                            {
                                float strength = 1.0f - (distBetween / 1.8f);
                                separation += diff.normalized * strength;
                            }
                        }
                        separation = Vector3.ClampMagnitude(separation, 1.2f);
                    }

                    // 4. LARI MENGEJAR TARGET & CEGAH MENEMBUS DINDING
                    // Patung Depan: Berlari lurus sesuai arah pandangannya mengincar pemain tanpa dibelokkan vektor separation
                    Vector3 moveDir = chaser.isImmediateChaser 
                        ? (horizontalToTarget.normalized + separation * 0.75f).normalized 
                        : horizontalToTarget.normalized;

                    Vector3 desiredMove = moveDir * (chaseSpeed * dt);
                    float desiredDist = desiredMove.magnitude;

                    // 🛡️ ANTI-PENETRASI DINDING: Deteksi dinding dengan CapsuleCastAll dan lakukan Wall Sliding
                    // PENTING: Abaikan Kereta Wahana (DarkRideCartController) agar patung bebas menerobos/naik ke kereta dan tidak terdorong belok ke depan!
                    if (desiredDist > 0.0001f)
                    {
                        Vector3 moveDirection = desiredMove / desiredDist;
                        Vector3 capsuleBottom = chaserPos + Vector3.up * 0.35f;
                        Vector3 capsuleTop = chaserPos + Vector3.up * 1.55f;
                        float bodyRadius = 0.32f;
                        int obstacleMask = ~LayerMask.GetMask("Ignore Raycast");

                        RaycastHit[] wallHits = Physics.CapsuleCastAll(capsuleBottom, capsuleTop, bodyRadius, moveDirection, desiredDist + 0.08f, obstacleMask, QueryTriggerInteraction.Ignore);
                        System.Array.Sort(wallHits, (a, b) => a.distance.CompareTo(b.distance));

                        bool hitWall = false;
                        RaycastHit solidWallHit = default;
                        foreach (var hit in wallHits)
                        {
                            if (hit.collider.isTrigger) continue;
                            if (hit.collider.transform.IsChildOf(chaserTrans)) continue;
                            if (hit.collider.GetComponentInParent<DarkRideCartController>() != null) continue; // Abaikan kereta wahana!
                            if (Mathf.Abs(hit.normal.y) >= 0.65f) continue;

                            solidWallHit = hit;
                            hitWall = true;
                            break;
                        }

                        if (hitWall)
                        {
                            Vector3 wallNormal = solidWallHit.normal;
                            wallNormal.y = 0f;
                            wallNormal.Normalize();

                            Vector3 slideMove = Vector3.ProjectOnPlane(desiredMove, wallNormal);
                            slideMove.y = 0f;

                            if (slideMove.sqrMagnitude > 0.0001f)
                            {
                                float slideDist = slideMove.magnitude;
                                Vector3 slideDir = slideMove / slideDist;

                                bool cornerBlocked = false;
                                RaycastHit[] cornerHits = Physics.CapsuleCastAll(capsuleBottom, capsuleTop, bodyRadius, slideDir, slideDist + 0.05f, obstacleMask, QueryTriggerInteraction.Ignore);
                                foreach (var cHit in cornerHits)
                                {
                                    if (cHit.collider.isTrigger) continue;
                                    if (cHit.collider.transform.IsChildOf(chaserTrans)) continue;
                                    if (cHit.collider.GetComponentInParent<DarkRideCartController>() != null) continue;
                                    if (Mathf.Abs(cHit.normal.y) >= 0.65f) continue;

                                    cornerBlocked = true;
                                    break;
                                }

                                desiredMove = cornerBlocked ? Vector3.zero : slideMove;
                            }
                            else
                            {
                                desiredMove = Vector3.zero;
                            }
                        }
                    }

                    Vector3 actualMoveDir = desiredMove.sqrMagnitude > 0.0001f ? desiredMove.normalized : moveDir;
                    if (actualMoveDir.sqrMagnitude > 0.001f)
                    {
                        // Hadap ke arah pergerakan lari (jika jalan membelok, patung otomatis ikut belok)
                        // Dikompensasi 180 derajat karena orientasi model FBX menghadap ke belakang
                        Quaternion lookRot = Quaternion.LookRotation(actualMoveDir, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
                        chaserTrans.rotation = Quaternion.Slerp(chaserTrans.rotation, lookRot, dt * 12f);
                    }

                    Vector3 nextPos = chaserPos + desiredMove;

                    // 🛡️ ANTI-CLIPPING PUSH-OUT: Mencegah patung beririsan atau tertembus ke dalam collider dinding
                    Vector3 bodyCenter = nextPos + Vector3.up * 0.9f;
                    Collider[] nearbyWalls = Physics.OverlapSphere(bodyCenter, 0.40f, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
                    foreach (var col in nearbyWalls)
                    {
                        if (col.isTrigger || col.transform.IsChildOf(chaserTrans)) continue;
                        if (col.GetComponentInParent<DarkRideCartController>() != null) continue;
                        if (col is MeshCollider mc && !mc.convex) continue;

                        Vector3 closest = col.ClosestPoint(bodyCenter);
                        Vector3 push = bodyCenter - closest;
                        push.y = 0f;
                        float dist = push.magnitude;
                        if (dist < 0.40f && dist > 0.001f)
                        {
                            nextPos += push.normalized * (0.40f - dist);
                            bodyCenter = nextPos + Vector3.up * 0.9f;
                        }
                    }

                    // Dynamic Ground Snapping: Selalu tempelkan kaki patung di atas permukaan lantai berjalan
                    // Abaikan collider MaskMan lain / diri sendiri / dinding vertikal agar tidak memanjat dan terbang ke udara
                    Vector3 groundRayOrigin = nextPos;
                    groundRayOrigin.y = chaserPos.y + 2.0f;
                    RaycastHit[] groundHits = Physics.RaycastAll(groundRayOrigin, Vector3.down, 5.0f, ~LayerMask.GetMask("Ignore Raycast"), QueryTriggerInteraction.Ignore);
                    System.Array.Sort(groundHits, (a, b) => a.distance.CompareTo(b.distance));

                    bool foundGround = false;
                    foreach (var gHit in groundHits)
                    {
                        if (gHit.collider.isTrigger) continue;
                        if (gHit.collider.transform.IsChildOf(chaserTrans)) continue;
                        if (gHit.collider.name.Contains("MaskMan") || gHit.collider.transform.IsChildOf(transform)) continue;
                        if (gHit.normal.y < 0.5f) continue; // Abaikan dinding miring / vertikal

                        nextPos.y = Mathf.Lerp(chaserPos.y, gHit.point.y + chaserVerticalOffset, dt * 15f);
                        foundGround = true;
                        break;
                    }

                    if (!foundGround)
                    {
                        nextPos.y = Mathf.Lerp(chaserPos.y, targetPursuitPos.y + chaserVerticalOffset, dt * 8f);
                    }

                    chaserTrans.position = nextPos;
                }
            }

            yield return null;
        }
    }

    private void CatchPlayer(GameObject killerChaser)
    {
        if (hasCaughtPlayer) return;
        hasCaughtPlayer = true;
        isChasing = false;

        if (activeChaseCoroutine != null)
        {
            StopCoroutine(activeChaseCoroutine);
            activeChaseCoroutine = null;
        }

        StartCoroutine(JumpscareCatchRoutine(killerChaser));
    }

    private IEnumerator JumpscareCatchRoutine(GameObject killerChaser)
    {
        // 1. Kunci pergerakan pemain seketika
        GameInputLock.LockInput();

        // 2. Hentikan musik pengejaran
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 3. Tampilkan sosok bertopeng langsung di hadapan wajah kamera pemain
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) mainCam = pm.GetComponentInChildren<Camera>();
        }

        if (killerChaser != null && mainCam != null)
        {
            // Pastikan GameObject aktif dan seluruh mesh renderer menyala
            killerChaser.SetActive(true);
            foreach (var t in killerChaser.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.SetActive(true);
                t.gameObject.layer = 0; // Layer Default agar 100% dirender kamera
            }
            foreach (var r in killerChaser.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }
            foreach (var anim in killerChaser.GetComponentsInChildren<Animator>(true))
            {
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            // Posisikan killer tepat 1.0 meter di depan lensa kamera, menatap langsung ke kamera
            Vector3 camForward = mainCam.transform.forward;
            Vector3 camPos = mainCam.transform.position;

            Vector3 jumpscarePos = camPos + camForward * 0.95f;
            // Sejajarkan mata/topeng killer dengan kamera pemain
            jumpscarePos.y = camPos.y - 1.45f;

            Quaternion lookAtCam = Quaternion.LookRotation(-camForward, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
            NormalizeChaserPivot(killerChaser);
            killerChaser.transform.SetPositionAndRotation(jumpscarePos, lookAtCam);

            // Putar audio jeritan jumpscare keras
            if (audioSource != null && jumpscareCatchSFX != null)
            {
                audioSource.PlayOneShot(jumpscareCatchSFX);
            }

            // Animasi lunge cepat: sosok menerjang semakin mendekat ke wajah (dari 0.95m ke 0.55m)
            float lungeDuration = 0.45f;
            float lungeElapsed = 0f;
            Vector3 startPos = jumpscarePos;
            Vector3 endPos = camPos + camForward * 0.55f;
            endPos.y = jumpscarePos.y;

            while (lungeElapsed < lungeDuration)
            {
                lungeElapsed += Time.deltaTime;
                float t = lungeElapsed / lungeDuration;
                t = Mathf.Sin(t * Mathf.PI * 0.5f); // Easing cepat
                if (killerChaser != null)
                {
                    killerChaser.transform.position = Vector3.Lerp(startPos, endPos, t);
                }
                yield return null;
            }

            // Tahan sesaat visual sosok di layar sebelum layar menjadi hitam
            yield return new WaitForSeconds(0.35f);
        }
        else
        {
            if (audioSource != null && jumpscareCatchSFX != null)
            {
                audioSource.PlayOneShot(jumpscareCatchSFX);
            }
            yield return new WaitForSeconds(0.4f);
        }

        // 4. Berikan sanksi korporat dan reset shift
        if (SanctionAndRetryManager.Instance != null)
        {
            SanctionAndRetryManager.Instance.IssueSanction(sanctionReason);
        }
    }

    /// <summary>
    /// Menghentikan pengejaran saat pemain berhasil melarikan diri keluar dari batas Zona Galeri.
    /// </summary>
    public void StopGalleryChase(bool playerEscaped)
    {
        if (!isChasing) return;
        isChasing = false;
        isAnomalyCompleted = true;

        if (activeChaseCoroutine != null)
        {
            StopCoroutine(activeChaseCoroutine);
            activeChaseCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutAudioRoutine(audioSource, 1.5f));
        }

        // Kembalikan semua patung ke posisi awal & membeku kembali
        if (chasers != null)
        {
            foreach (var chaser in chasers)
            {
                if (chaser != null && chaser.chaserObject != null)
                {
                    chaser.isAwakened = false;
                    chaser.awakenTimer = 0f;
                    chaser.activeChaseTime = 0f;
                    chaser.chaserObject.transform.position = chaser.initialPosition;
                    chaser.chaserObject.transform.rotation = chaser.initialRotation;
                }
            }
        }
    }

    private IEnumerator FadeOutAudioRoutine(AudioSource src, float duration)
    {
        if (src == null) yield break;
        float startVol = src.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        src.Stop();
        src.volume = startVol;
    }

    /// <summary>
    /// Menormalkan titik tumpu (pivot) model 3D patung agar kaki karakter tepat berada di (0, 0, 0) lokal objeknya.
    /// Ini memperbaiki issue jika model FBX diekspor dari software 3D dengan offset ratusan meter dari titik origin.
    /// </summary>
    private void NormalizeChaserPivot(GameObject chaserObj)
    {
        if (chaserObj == null) return;

        Renderer[] renderers = chaserObj.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        // Hitung bounding box visual dari seluruh mesh
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // Titik tumpu kaki mesh (tengah horizontal, dasar vertikal) dalam world space
        Vector3 visualFeetWorld = new Vector3(combinedBounds.center.x, combinedBounds.min.y, combinedBounds.center.z);
        // Konversi ke koordinat lokal chaserObj
        Vector3 visualFeetLocal = chaserObj.transform.InverseTransformPoint(visualFeetWorld);

        // Jika mesh bergeser jauh dari titik pivot root (> 0.2 meter)
        if (visualFeetLocal.sqrMagnitude > 0.04f)
        {
            List<Transform> directChildren = new List<Transform>();
            foreach (Transform child in chaserObj.transform)
            {
                directChildren.Add(child);
            }

            // Lepas sementara dengan worldPositionStays = true agar posisi fisik di dunia tidak berubah
            foreach (var child in directChildren)
            {
                child.SetParent(null, true);
            }

            // Pindahkan root GameObject tepat ke posisi fisik telapak kaki visual
            chaserObj.transform.position = visualFeetWorld;

            // Pasang kembali anak-anak ke root
            foreach (var child in directChildren)
            {
                child.SetParent(chaserObj.transform, true);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualisasi radius tangkap & radius awaken
        if (chasers != null)
        {
            foreach (var chaser in chasers)
            {
                if (chaser != null && chaser.chaserObject != null)
                {
                    Vector3 pos = chaser.chaserObject.transform.position + Vector3.up;
                    Renderer rnd = chaser.chaserObject.GetComponentInChildren<Renderer>();
                    if (rnd != null)
                    {
                        pos = rnd.bounds.center;
                    }

                    if (chaser.isImmediateChaser)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(pos, catchDistance);
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(pos, chaser.awakenDistance);
                    }
                }
            }
        }

        // Visualisasi batas zona galeri
        if (galleryZoneBoundary != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(galleryZoneBoundary.bounds.center, galleryZoneBoundary.bounds.size);
        }

        // Visualisasi titik tunggu kereta
        if (cartWaitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cartWaitPoint.position, 1.0f);
        }

        // Visualisasi jangkauan trigger pintu
        if (exitDoorTrigger != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(exitDoorTrigger.bounds.center, Mathf.Max(exitDoorTrigger.bounds.extents.x, exitDoorTrigger.bounds.extents.z) + 1.8f);
        }

        // Visualisasi formasi cluster di belakang kereta
        Vector3 clusterCenter = Vector3.zero;
        Vector3 clusterFwd = Vector3.forward;
        bool hasClusterCenter = false;

        if (rearClusterPoint != null)
        {
            clusterCenter = rearClusterPoint.position;
            clusterFwd = rearClusterPoint.forward;
            hasClusterCenter = true;
        }
        else if (darkRideCart != null)
        {
            clusterCenter = darkRideCart.GetSplinePositionBehind(rearClusterDistance);
            clusterFwd = darkRideCart.GetSplineForwardDirection();
            hasClusterCenter = true;
        }

        if (hasClusterCenter)
        {
            clusterFwd.y = 0f;
            if (clusterFwd.sqrMagnitude > 0.001f) clusterFwd.Normalize();
            Gizmos.color = new Color(1f, 0.45f, 0f, 0.85f); // Orange
            Gizmos.DrawWireCube(clusterCenter + Vector3.up, new Vector3(rearSpreadWidth, 2f, rearRowSpacing * 3f));
            Gizmos.DrawRay(clusterCenter + Vector3.up, clusterFwd * 3f);
        }
    }
}

/// <summary>
/// Komponen pembantu yang otomatis dipasang pada ExitDoorTrigger agar deteksi fisika OnTriggerEnter seketika memicu pengejaran.
/// </summary>
public class GalleryDoorTriggerBridge : MonoBehaviour
{
    public System.Action onPlayerEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || 
            other.GetComponentInParent<CharacterController>() != null || 
            other.GetComponentInParent<PlayerMovement>() != null ||
            other.name.ToLower().Contains("player"))
        {
            onPlayerEnter?.Invoke();
        }
    }
}
