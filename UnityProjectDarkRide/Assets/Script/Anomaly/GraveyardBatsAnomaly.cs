using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anomali: GraveyardBatsAnomaly (Jumpscare Sekawanan Kelelawar di Kuburan).
/// Fitur:
/// 1. Trigger saat Player atau Cart melintas di titik kuburan.
/// 2. Sekawanan kelelawar mendadak melesat keluar dari tempat sembunyi (pohon/balik nisan)
///    menerjang ke arah kamera player dengan kepakan sayap cepat.
/// 3. Mendukung asset 3D Bat.fbx bawaan project, ATAU otomatis membuat mesh kelelawar bersayap jika prefab kosong!
/// 4. Efek kepakan sayap prosedural (Flapping animation) & gerakan terbang melengkung (swarming arc).
/// 5. Dilengkapi audio kepakan/jeritan kelelawar & getaran kamera sesaat (Camera Shake).
/// </summary>
[RequireComponent(typeof(Collider))]
public class GraveyardBatsAnomaly : MonoBehaviour
{
    [Header("1. Bat Prefab / Model")]
    [Tooltip("Drag file Bat.fbx atau prefab kelelawar Anda ke sini. Jika kosong, script otomatis membuat kelelawar 3D prosedural bersayap!")]
    [SerializeField] private GameObject batPrefab;

    [Tooltip("Jumlah kelelawar dalam kawanan (Swarm Count)")]
    [Range(5, 30)]
    [SerializeField] private int batCount = 14;

    [Header("2. Spawn & Target Flight Settings")]
    [Tooltip("Titik awal sarang kelelawar (misal: ditaruh di pohon atau balik nisan). Jika kosong, memakai posisi trigger ini")]
    [SerializeField] private Transform spawnOrigin;

    [Tooltip("Target arah terbang (otomatis menuju Camera.main jika kosong)")]
    [SerializeField] private Transform targetPlayer;

    [Tooltip("Kecepatan terbang kelelawar menerjang ke arah player")]
    [SerializeField] private float flightSpeed = 16.0f;

    [Tooltip("Total durasi kelelawar melesat terbang sampai menghilang (detik). Sesuaikan dengan panjang durasi audio SFX Anda!")]
    [SerializeField] private float flightDuration = 2.0f;

    [Tooltip("Kecepatan kepakan sayap")]
    [SerializeField] private float wingFlapSpeed = 35.0f;

    [Tooltip("Jarak sebaran kemunculan awal kelelawar (meter)")]
    [SerializeField] private float spawnSpreadRadius = 1.2f;

    [Header("3. Jumpscare Effects")]
    [Tooltip("Durasi getaran kamera saat kelelawar menerjang wajah player (detik)")]
    [SerializeField] private float cameraShakeDuration = 0.45f;
    [SerializeField] private float cameraShakeMagnitude = 0.15f;

    [Header("4. Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("SFX kepakan sayap banyak kelelawar / jeritan kelelawar (Opsional)")]
    [SerializeField] private AudioClip batScreechSFX;

    [Header("5. Trigger Mode")]
    [Tooltip("CENTANG jika anomali kelelawar ini hanya terjadi 1x")]
    [SerializeField] private bool onlyTriggerOnce = true;

    private bool hasTriggered = false;
    private static Material proceduralBatMaterial;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.35f; // Sedikit 3D tapi tetap jelas terdengar di kedua telinga
    }

    private void Start()
    {
        if (targetPlayer == null && Camera.main != null)
        {
            targetPlayer = Camera.main.transform;
        }

        if (spawnOrigin == null)
        {
            spawnOrigin = transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        bool isPlayerOrCart = other.CompareTag("Player")
            || other.GetComponentInParent<CharacterController>() != null
            || other.name.ToLower().Contains("player")
            || other.transform.root.name.ToLower().Contains("player")
            || other.name.ToLower().Contains("cart")
            || other.name.ToLower().Contains("camera");

        if (isPlayerOrCart)
        {
            hasTriggered = true;
            TriggerBatSwarm();

            if (onlyTriggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    public void TriggerBatSwarm()
    {
        if (targetPlayer == null && Camera.main != null)
        {
            targetPlayer = Camera.main.transform;
        }

        // SFX Suara Kelelawar
        if (batScreechSFX != null)
        {
            EnsureAudioSource();
            audioSource.PlayOneShot(batScreechSFX);
        }

        // Mulai kemunculan sekawanan kelelawar
        StartCoroutine(SpawnAndFlySwarmRoutine());
    }

    private IEnumerator SpawnAndFlySwarmRoutine()
    {
        Vector3 originPos = spawnOrigin != null ? spawnOrigin.position : transform.position;
        Vector3 playerPos = targetPlayer != null ? targetPlayer.position : (originPos + transform.forward * 5f);

        List<Transform> spawnedBats = new List<Transform>();
        List<Transform[]> wingsPerBat = new List<Transform[]>();

        // 1. Instansiasi kawanan kelelawar
        for (int i = 0; i < batCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * spawnSpreadRadius;
            Vector3 startPos = originPos + randomOffset;

            GameObject batObj = null;
            Transform[] wings = null;

            if (batPrefab != null)
            {
                batObj = Instantiate(batPrefab, startPos, Quaternion.identity);
                batObj.transform.localScale = batPrefab.transform.localScale;
            }
            else
            {
                // Buat 3D Kelelawar Prosedural Lengkap dengan Sayap
                batObj = CreateProceduralBat(startPos, out wings);
            }

            if (batObj != null)
            {
                spawnedBats.Add(batObj.transform);
                wingsPerBat.Add(wings);
            }
        }

        // 2. Efek Kamera Bergetar (Camera Shake) sesaat saat kelelawar menerjang
        if (targetPlayer != null && cameraShakeDuration > 0f)
        {
            StartCoroutine(PerformCameraShake());
        }

        // 3. Terbangkan kawanan kelelawar menerjang melintasi wajah player lalu menghilang ke langit
        float elapsed = 0f;

        // Vektor arah umum menerjang melewati kepala pemain
        Vector3 overshootTarget = playerPos + (playerPos - originPos).normalized * 12.0f + Vector3.up * 4.0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;

            for (int i = 0; i < spawnedBats.Count; i++)
            {
                Transform bat = spawnedBats[i];
                if (bat == null) continue;

                // Lintasan terbang melesat sedikit melengkung dan acak tiap kelelawar
                Vector3 currentTarget = Vector3.Lerp(playerPos, overshootTarget, Mathf.SmoothStep(0f, 1f, t));
                Vector3 targetDirection = (currentTarget - bat.position).normalized;

                // Tambahkan sedikit goyangan terbang ke kiri-kanan
                float wave = Mathf.Sin((Time.time * 15f) + (i * 1.5f)) * 0.4f;
                targetDirection += bat.right * wave;

                bat.position += targetDirection * (flightSpeed * Time.deltaTime);
                if (targetDirection.sqrMagnitude > 0.001f)
                {
                    bat.rotation = Quaternion.Slerp(bat.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * 12f);
                }

                // Kepakkan sayap jika kelelawar prosedural
                Transform[] batWings = wingsPerBat[i];
                if (batWings != null && batWings.Length == 2)
                {
                    float flapAngle = Mathf.Sin(Time.time * wingFlapSpeed + (i * 2f)) * 42.0f;
                    batWings[0].localRotation = Quaternion.Euler(0, 0, flapAngle);
                    batWings[1].localRotation = Quaternion.Euler(0, 0, -flapAngle);
                }
            }

            yield return null;
        }

        // 4. Hancurkan kelelawar setelah selesai melesat jauh
        foreach (var bat in spawnedBats)
        {
            if (bat != null) Destroy(bat.gameObject);
        }
    }

    /// <summary>
    /// Membuat Model 3D Kelelawar Prosedural (Badan Hitam + Sayap Bergerak)
    /// </summary>
    private GameObject CreateProceduralBat(Vector3 pos, out Transform[] wings)
    {
        if (proceduralBatMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");

            proceduralBatMaterial = new Material(shader);
            proceduralBatMaterial.name = "M_ProceduralBat";
            proceduralBatMaterial.color = new Color(0.08f, 0.08f, 0.08f); // Hitam pekat kelelawar
        }

        GameObject batRoot = new GameObject("ProceduralBat");
        batRoot.transform.position = pos;
        batRoot.transform.localScale = Vector3.one * 0.55f;

        // Badan Kelelawar (Ellipsoid)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(batRoot.transform, false);
        body.transform.localScale = new Vector3(0.28f, 0.22f, 0.55f);
        Collider bCol = body.GetComponent<Collider>();
        if (bCol != null) Destroy(bCol);
        body.GetComponent<Renderer>().material = proceduralBatMaterial;

        // Kepala
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(batRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.08f, 0.32f);
        head.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        Collider hCol = head.GetComponent<Collider>();
        if (hCol != null) Destroy(hCol);
        head.GetComponent<Renderer>().material = proceduralBatMaterial;

        // Sayap Kiri
        GameObject leftWingPivot = new GameObject("LeftWingPivot");
        leftWingPivot.transform.SetParent(batRoot.transform, false);
        leftWingPivot.transform.localPosition = new Vector3(-0.12f, 0.05f, 0.05f);

        GameObject leftWingMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWingMesh.transform.SetParent(leftWingPivot.transform, false);
        leftWingMesh.transform.localPosition = new Vector3(-0.45f, 0f, 0f);
        leftWingMesh.transform.localScale = new Vector3(0.85f, 0.015f, 0.45f);
        Collider lwCol = leftWingMesh.GetComponent<Collider>();
        if (lwCol != null) Destroy(lwCol);
        leftWingMesh.GetComponent<Renderer>().material = proceduralBatMaterial;

        // Sayap Kanan
        GameObject rightWingPivot = new GameObject("RightWingPivot");
        rightWingPivot.transform.SetParent(batRoot.transform, false);
        rightWingPivot.transform.localPosition = new Vector3(0.12f, 0.05f, 0.05f);

        GameObject rightWingMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWingMesh.transform.SetParent(rightWingPivot.transform, false);
        rightWingMesh.transform.localPosition = new Vector3(0.45f, 0f, 0f);
        rightWingMesh.transform.localScale = new Vector3(0.85f, 0.015f, 0.45f);
        Collider rwCol = rightWingMesh.GetComponent<Collider>();
        if (rwCol != null) Destroy(rwCol);
        rightWingMesh.GetComponent<Renderer>().material = proceduralBatMaterial;

        wings = new Transform[] { leftWingPivot.transform, rightWingPivot.transform };
        return batRoot;
    }

    private IEnumerator PerformCameraShake()
    {
        Vector3 originalPos = targetPlayer.localPosition;
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.deltaTime;
            float factor = 1f - (elapsed / cameraShakeDuration);
            float offsetX = Random.Range(-1f, 1f) * cameraShakeMagnitude * factor;
            float offsetY = Random.Range(-1f, 1f) * cameraShakeMagnitude * factor;

            targetPlayer.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            yield return null;
        }

        targetPlayer.localPosition = originalPos;
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        Vector3 spawn = spawnOrigin != null ? spawnOrigin.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawn, spawnSpreadRadius);

        if (targetPlayer != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawn, targetPlayer.position);
        }
    }
}
