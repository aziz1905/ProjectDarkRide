using System.Collections;
using UnityEngine;

/// <summary>
/// Anomali #1: RedEyesStareAnomaly (Mata Merah Memandang di Kegelapan Kuburan).
/// Fitur:
/// 1. Muncul saat lampu padam/blackout di area kuburan.
/// 2. Menatap selalu ke arah kamera pemain (LookAt Player).
/// 3. Efek nafas/denyut pupil menyala merah terang (HDR Pulse Glow).
/// 4. Berkedip sesekali (Blinking) untuk memberi kesan makhluk hidup yang mengintai.
/// 5. Seketika lenyap saat lampu kuburan menyala kembali atau setelah durasi habis.
/// 6. Auto-generate visual mata merah prosedural jika user tidak memasang model khusus.
/// 7. Mendukung mode Head Socket (Day 4): Bola mata diam di soket tengkorak, hanya pupil/bola mata yang berputar di tempat mengikuti player!
/// </summary>
public class RedEyesStareAnomaly : MonoBehaviour
{
    [Header("Target & Eye Transform")]
    [Tooltip("Target pandangan (otomatis mencari Main Camera/Player jika kosong)")]
    [SerializeField] private Transform lookTarget;

    [Tooltip("Dua objek mata merah (Kiri & Kanan). Jika dibiarkan kosong, script akan membuat bola mata merah menyala otomatis!")]
    [SerializeField] private Transform leftEye;
    [SerializeField] private Transform rightEye;

    [Header("Visual Eye Settings")]
    [Tooltip("Jarak antara mata kiri dan kanan (meter)")]
    [SerializeField] private float eyeSpacing = 0.22f;

    [Tooltip("Ukuran bola mata")]
    [SerializeField] private float eyeSize = 0.12f;

    [Tooltip("Warna mata merah menyala")]
    [ColorUsage(true, true)]
    [SerializeField] private Color eyeEmissionColor = new Color(3.5f, 0.05f, 0.05f);

    [Header("Behavior Settings")]
    [Tooltip("Kecepatan denyut pupil/cahaya mata")]
    [SerializeField] private float pulseSpeed = 2.5f;

    [Tooltip("Rentang intensitas denyut (Min, Max)")]
    [SerializeField] private Vector2 pulseIntensity = new Vector2(0.8f, 1.4f);

    [Tooltip("Apakah mata sesekali berkedip memejam?")]
    [SerializeField] private bool enableBlink = true;

    [Header("Head Socket Tracking (Khusus Kepala Tengkorak / Day 4)")]
    [Tooltip("Jika TRUE: kedua bola mata tetap diam di soket tengkorak/kepala, dan hanya pupil/bola mata di tempatnya yang berputar mengikuti pemain. Tidak akan mengubah anomali mata merah standalone di Day 3.")]
    [SerializeField] private bool headSocketMode = false;

    [Tooltip("Kecepatan pupil melirik mengikuti target")]
    [SerializeField] private float pupilTrackSpeed = 15f;

    [Header("Audio SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Suara ambience seram/hembusan nafas/bisikan halus saat mata muncul")]
    [SerializeField] private AudioClip stareAmbienceSFX;

    private Material eyeMaterialInstance;
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
    private Coroutine activeStareRoutine;
    private Coroutine activeBlinkRoutine;
    private bool isStaring = false;
    private Vector3 originalEyeScale;

    public bool IsHeadSocketMode => headSocketMode;

    /// <summary>
    /// Mengaktifkan mode soket kepala (digunakan otomatis oleh StalkingSkeletonAnomaly)
    /// </summary>
    public void SetHeadSocketMode(bool enabled)
    {
        headSocketMode = enabled;
        ResetPupilPosition(leftEye);
        ResetPupilPosition(rightEye);
    }

    private void Awake()
    {
        // Otomatis cari child LeftRedEye & RightRedEye jika belum di-assign di inspector
        if (leftEye == null)
        {
            Transform found = transform.Find("LeftRedEye");
            if (found != null) leftEye = found;
        }

        if (rightEye == null)
        {
            Transform found = transform.Find("RightRedEye");
            if (found != null) rightEye = found;
        }

        SetupEyesIfNeeded();
        ResetPupilPosition(leftEye);
        ResetPupilPosition(rightEye);
        HideEyesInstant();
    }

    private void Start()
    {
        if (lookTarget == null && Camera.main != null)
        {
            lookTarget = Camera.main.transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private Transform GetActiveLookTarget()
    {
        if (Camera.main != null) return Camera.main.transform;
        if (lookTarget != null) return lookTarget;
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) return pm.transform;
        return null;
    }

    private void Update()
    {
        // Lakukan tracking jika anomali sedang aktif ATAU jika mata terlihat di scene
        bool eyesVisible = (leftEye != null && leftEye.gameObject.activeInHierarchy) ||
                           (rightEye != null && rightEye.gameObject.activeInHierarchy);

        if (!isStaring && !eyesVisible) return;

        Transform target = GetActiveLookTarget();
        if (target != null)
        {
            if (!headSocketMode)
            {
                // Mode Standalone Mata Merah Melayang (Day 3): Rotasi transform root menghadap player
                Vector3 targetDir = target.position - transform.position;
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(targetDir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
                }
            }
            else
            {
                // Mode Soket Tengkorak / Kepala (Day 4):
                // Transform root & posisi bola mata DIAM 100% di soket kepala.
                // Masing-masing bola mata berputar di porosnya sendiri sehingga pupil hitam di depannya mengunci tatapan ke player!
                RotateEyeToPlayer(leftEye, target);
                RotateEyeToPlayer(rightEye, target);
            }
        }

        // Efek denyut cahaya (Pulse)
        if (eyeMaterialInstance != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(pulseIntensity.x, pulseIntensity.y, t);
            eyeMaterialInstance.SetColor(EmissionColorProp, eyeEmissionColor * intensity);
        }
    }

    private void RotateEyeToPlayer(Transform eye, Transform target)
    {
        if (eye == null || target == null) return;

        Vector3 toPlayer = target.position - eye.position;
        if (toPlayer.sqrMagnitude < 0.001f) return;

        // Bola mata berputar pada porosnya sendiri (posisinya di soket kepala tetap 100% diam)
        Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
        eye.rotation = Quaternion.Slerp(eye.rotation, targetRot, Time.deltaTime * pupilTrackSpeed);
    }

    private void ResetPupilPosition(Transform eye)
    {
        if (eye == null) return;
        Transform pupil = eye.Find("BlackPupil");
        if (pupil != null)
        {
            // Pastikan pupil berada di permukaan depan bola mata (+Z)
            pupil.localPosition = new Vector3(0f, 0f, 0.45f);
            pupil.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Dipanggil untuk mengaktifkan kemunculan mata merah saat lampu padam
    /// </summary>
    public void StartStare(float duration = 0f)
    {
        // Pastikan objek mata aktif di Hierarchy sebelum menjalankan Coroutine
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (activeStareRoutine != null) StopCoroutine(activeStareRoutine);
        activeStareRoutine = StartCoroutine(StareRoutine(duration));
    }

    /// <summary>
    /// Dipanggil untuk langsung menghilangkan mata merah (misal saat lampu menyala kembali)
    /// </summary>
    public void StopStare()
    {
        if (activeStareRoutine != null)
        {
            StopCoroutine(activeStareRoutine);
            activeStareRoutine = null;
        }

        if (activeBlinkRoutine != null)
        {
            StopCoroutine(activeBlinkRoutine);
            activeBlinkRoutine = null;
        }

        HideEyesInstant();
    }

    private IEnumerator StareRoutine(float duration)
    {
        isStaring = true;
        ShowEyes();

        if (audioSource != null && stareAmbienceSFX != null)
        {
            audioSource.clip = stareAmbienceSFX;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (enableBlink)
        {
            activeBlinkRoutine = StartCoroutine(BlinkRoutine());
        }

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
            StopStare();
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (isStaring)
        {
            // Berkedip setiap 2.5 sampai 5 detik sekali
            yield return new WaitForSeconds(Random.Range(2.5f, 5.0f));

            if (!isStaring) yield break;

            // Tutup mata (Skala Y mengecil sekejap)
            float elapsed = 0f;
            float blinkDuration = 0.07f;

            while (elapsed < blinkDuration)
            {
                elapsed += Time.deltaTime;
                float scaleY = Mathf.Lerp(originalEyeScale.y, 0.005f, elapsed / blinkDuration);
                SetEyesScaleY(scaleY);
                yield return null;
            }

            yield return new WaitForSeconds(0.08f);

            // Buka mata kembali
            elapsed = 0f;
            while (elapsed < blinkDuration)
            {
                elapsed += Time.deltaTime;
                float scaleY = Mathf.Lerp(0.005f, originalEyeScale.y, elapsed / blinkDuration);
                SetEyesScaleY(scaleY);
                yield return null;
            }

            SetEyesScaleY(originalEyeScale.y);
        }
    }

    private void SetEyesScaleY(float y)
    {
        if (leftEye != null)
        {
            Vector3 s = leftEye.localScale;
            s.y = y;
            leftEye.localScale = s;
        }

        if (rightEye != null)
        {
            Vector3 s = rightEye.localScale;
            s.y = y;
            rightEye.localScale = s;
        }
    }

    private void ShowEyes()
    {
        if (leftEye != null) leftEye.gameObject.SetActive(true);
        if (rightEye != null) rightEye.gameObject.SetActive(true);
        SetEyesScaleY(originalEyeScale.y);
    }

    private void HideEyesInstant()
    {
        isStaring = false;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (leftEye != null) leftEye.gameObject.SetActive(false);
        if (rightEye != null) rightEye.gameObject.SetActive(false);
    }

    private void SetupEyesIfNeeded()
    {
        // Jika belum ada mata, buat 2 bola mata merah prosedural
        if (leftEye == null || rightEye == null)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Mobile/Diffuse");

            eyeMaterialInstance = new Material(standardShader);
            eyeMaterialInstance.name = "M_RedEye_Glow";
            eyeMaterialInstance.color = Color.black;
            eyeMaterialInstance.EnableKeyword("_EMISSION");
            eyeMaterialInstance.SetColor(EmissionColorProp, eyeEmissionColor);
            eyeMaterialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            if (leftEye == null)
            {
                GameObject leftObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leftObj.name = "LeftRedEye";
                leftObj.transform.SetParent(transform, false);
                leftObj.transform.localPosition = new Vector3(-eyeSpacing * 0.5f, 0, 0);
                leftObj.transform.localScale = Vector3.one * eyeSize;

                Collider col = leftObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer rend = leftObj.GetComponent<Renderer>();
                if (rend != null) rend.material = eyeMaterialInstance;

                // Tambahkan Pupil Hitam di depan mata kiri
                CreatePupilOnEye(leftObj.transform, standardShader);

                leftEye = leftObj.transform;
            }

            if (rightEye == null)
            {
                GameObject rightObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rightObj.name = "RightRedEye";
                rightObj.transform.SetParent(transform, false);
                rightObj.transform.localPosition = new Vector3(eyeSpacing * 0.5f, 0, 0);
                rightObj.transform.localScale = Vector3.one * eyeSize;

                Collider col = rightObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer rend = rightObj.GetComponent<Renderer>();
                if (rend != null) rend.material = eyeMaterialInstance;

                // Tambahkan Pupil Hitam di depan mata kanan
                CreatePupilOnEye(rightObj.transform, standardShader);

                rightEye = rightObj.transform;
            }
        }
        else
        {
            // Jika objek sudah ada dari scene, pastikan pupil hitam dibuat jika belum ada
            EnsurePupilExists(leftEye);
            EnsurePupilExists(rightEye);

            Renderer rend = leftEye.GetComponent<Renderer>();
            if (rend != null)
            {
                eyeMaterialInstance = rend.material;
            }
        }

        ResetPupilPosition(leftEye);
        ResetPupilPosition(rightEye);

        if (leftEye != null)
        {
            originalEyeScale = leftEye.localScale;
        }
        else
        {
            originalEyeScale = Vector3.one * eyeSize;
        }
    }

    private void EnsurePupilExists(Transform eyeTransform)
    {
        if (eyeTransform == null) return;
        Transform pupil = eyeTransform.Find("BlackPupil");
        if (pupil == null)
        {
            Shader standardShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (standardShader == null) standardShader = Shader.Find("Unlit/Color");
            if (standardShader == null) standardShader = Shader.Find("Standard");
            CreatePupilOnEye(eyeTransform, standardShader);
        }
    }

    private void CreatePupilOnEye(Transform eyeTransform, Shader shader)
    {
        if (eyeTransform == null) return;

        GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "BlackPupil";
        pupil.transform.SetParent(eyeTransform, false);
        // Posisikan pupil sedikit menonjol di depan bola mata (+Z)
        pupil.transform.localPosition = new Vector3(0f, 0f, 0.45f);
        pupil.transform.localScale = new Vector3(0.45f, 0.75f, 0.25f); // Sedikit pipih vertikal seperti pupil predator/kucing seram

        Collider col = pupil.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Material pupilMat = new Material(shader != null ? shader : Shader.Find("Standard"));
        pupilMat.name = "M_BlackPupil";
        pupilMat.color = Color.black;
        pupil.GetComponent<Renderer>().material = pupilMat;
    }

    private void OnDrawGizmos()
    {
        Vector3 leftPos = transform.position - (transform.right * (eyeSpacing * 0.5f));
        Vector3 rightPos = transform.position + (transform.right * (eyeSpacing * 0.5f));

        // Gambar sepasang bola mata merah (Wireframe bersih di Scene View)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(leftPos, eyeSize * 0.5f);
        Gizmos.DrawWireSphere(rightPos, eyeSize * 0.5f);

        // Titik pusat mata menyala
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawSphere(leftPos, eyeSize * 0.25f);
        Gizmos.DrawSphere(rightPos, eyeSize * 0.25f);

        // Arah tatapan mata (Garis merah)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(leftPos, leftPos + transform.forward * 1.5f);
        Gizmos.DrawLine(rightPos, rightPos + transform.forward * 1.5f);

        // Garis penghubung kedua mata
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawLine(leftPos, rightPos);
    }
}
