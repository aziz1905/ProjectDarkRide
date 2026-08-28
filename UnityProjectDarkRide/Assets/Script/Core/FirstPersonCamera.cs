using System.Collections;
using UnityEngine;

/// <summary>
/// First Person Camera Controller Interaktif & Presisi Roller Coaster POV Realistis.
/// Timeline Koreografi Nanjak Presisi 14 Detik (9s Nanjak + 5s Rel Datar):
/// - 0-0.3s: Hold Lurus Ke Depan (0° Pitch)
/// - 0.3-0.8s: Fast Snap Pitch Up ke -25°
/// - 0.8-1.1s: REALISTIC HUMAN HEAD BOUNCE (Efek Membal / Mantul Kepala Alami saat Hentakan Snap)
/// - 1.1-2.5s: Hold Pitch Up ke -25°
/// - 2.5-5.5s: Melihat KIRI BAWAH (+20° Pitch, -20° Yaw) & Kembali ke Depan
/// - 5.5-8.5s: Tahan Lurus ke Depan (-25° Pitch)
/// - 8.5-11s: Melirik LEBIH KE KANAN (+35° Yaw Right) & Kembali Lurus ke Depan Pas Detik 11
/// - 11-14s: Rel Datar, Merata Mulus ke 0° Pitch & 0° Yaw (Zero Flick)
/// </summary>
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Target & Sensitivity")]
    [SerializeField] private Transform playerBody; // Drag GameObject [Player] ke sini
    [SerializeField] private float mouseSensitivity = 200f;

    [Header("Camera Pitch Constraints")]
    [SerializeField] private float minPitch = -85f; // Batas maksimal lihat ke bawah
    [SerializeField] private float maxPitch = 85f;  // Batas maksimal lihat ke atas

    [Header("Realistic Human Breathing Settings")]
    [Tooltip("Aktifkan animasi napas & berat kepala alami manusia")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathingFrequency = 1.5f;
    [SerializeField] private float breathingAmplitudeY = 0.012f;
    [SerializeField] private float breathingAmplitudeX = 0.006f;
    [SerializeField] private float breathingTiltAngle = 0.35f;

    private float xRotation = 0f;
    private Vector3 initialLocalPos;
    private Coroutine smoothRotateRoutine;
    private Coroutine cutsceneGazeRoutine;

    private void Start()
    {
        LockCursor();
        
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }

        initialLocalPos = transform.localPosition;
    }

    private void Update()
    {
        // 🫁 SIMULASI NAPAS MANUSIA ORGANIK
        ApplyNaturalBreathing();

        // 🛑 JIKA CUTSCENE DINO-COASTER AKTIF, IKUTI KONTROL GAZE COROUTINE
        if (GameInputLock.IsInputLocked)
        {
            return;
        }

        // 🖱️ AMBIL INPUT MOUSE DARI PEMAIN SAAT GAMEPLAY NORMAL
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void ApplyNaturalBreathing()
    {
        if (!enableBreathing) return;

        float breatheTime = Time.time * breathingFrequency;
        
        float breatheY = Mathf.Sin(breatheTime) * breathingAmplitudeY;
        float breatheX = Mathf.Cos(breatheTime * 0.5f) * breathingAmplitudeX;

        transform.localPosition = initialLocalPos + new Vector3(breatheX, breatheY, 0f);

        if (GameInputLock.IsInputLocked && cutsceneGazeRoutine == null)
        {
            float tilt = Mathf.Sin(breatheTime * 0.8f) * breathingTiltAngle;
            transform.localRotation = Quaternion.Euler(xRotation, 0f, tilt);
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResetRotation(Quaternion targetRotation)
    {
        if (cutsceneGazeRoutine != null) { StopCoroutine(cutsceneGazeRoutine); cutsceneGazeRoutine = null; }
        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
        if (playerBody != null)
        {
            playerBody.rotation = targetRotation;
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    public void SmoothRotateToTarget(Quaternion targetWorldRotation, float duration = 0.6f)
    {
        if (smoothRotateRoutine != null) StopCoroutine(smoothRotateRoutine);
        smoothRotateRoutine = StartCoroutine(SmoothRotateRoutine(targetWorldRotation, duration));
    }

    private IEnumerator SmoothRotateRoutine(Quaternion targetWorldRotation, float duration)
    {
        float timer = 0f;

        Vector3 camForward = transform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(camForward);
            if (playerBody != null) playerBody.rotation = targetYaw;
            xRotation = transform.localEulerAngles.x;
            if (xRotation > 180f) xRotation -= 360f;
        }

        Quaternion startPlayerRot = playerBody != null ? playerBody.rotation : transform.rotation;
        float startXRot = xRotation;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);

            xRotation = Mathf.Lerp(startXRot, 0f, t);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.rotation = Quaternion.Slerp(startPlayerRot, targetWorldRotation, t);
            }
            yield return null;
        }

        ResetRotation(targetWorldRotation);
        smoothRotateRoutine = null;
    }

    /// <summary>
    /// 🧗 KOREOGRAFI NANJAK PRESISI (14s Total: 9s Nanjak + 5s Rel Datar):
    /// Detik 0-0.3s Hold Lurus, Detik 0.3-0.8s Fast Snap Pitch Up, Detik 0.8-1.1s ELASTIC HEAD BOUNCE MANTUL ALAMI!
    /// </summary>
    public void PlayClimbGazeSequence(DarkRideCartController cart, float totalDuration = 14.0f)
    {
        if (cutsceneGazeRoutine != null) StopCoroutine(cutsceneGazeRoutine);
        cutsceneGazeRoutine = StartCoroutine(ClimbGazeRoutine(cart, 14.0f));
    }

    private IEnumerator ClimbGazeRoutine(DarkRideCartController cart, float totalDuration)
    {
        float duration = 14.0f; // Total durasi presisi 14 detik
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Quaternion cartRot = cart != null ? cart.ForwardRotation : transform.rotation;

            float gazePitch = 0f;
            float gazeYaw = 0f;
            float rattleMultiplier = 1.0f;

            // 🎥 CHOREOGRAPHY TIMELINE PRESISI (TOTAL 14 DETIK: 9s NANJAK + 5s REL DATAR):
            // 1. Detik 0.0s - 0.3s: Hold Lurus ke Depan (0° Pitch, 0° Yaw)
            if (elapsed < 0.3f)
            {
                gazePitch = 0f;
                gazeYaw = 0f;
            }
            // 2. Detik 0.3s - 0.8s: Fast Snap Pitch Up (Menengadah sedikit ke -12° di Detik 0.8s)
            else if (elapsed < 0.8f)
            {
                float t = (elapsed - 0.3f) / 0.5f;
                gazePitch = Mathf.Lerp(0f, -12f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = 0f;
                // Strong front/back bounce with vertical jitter during snap (0.3‑0.8s)
                float snapT = (elapsed - 0.3f) / 0.5f;
                // Larger Z amplitude for noticeable forward/back movement
                float bounceOffsetZ = Mathf.Sin(snapT * Mathf.PI * 4.0f) * Mathf.Lerp(0.12f, 0f, snapT);
                // Small Y jitter to simulate shake
                float bounceOffsetY = Mathf.Sin(snapT * Mathf.PI * 6.0f) * Mathf.Lerp(0.04f, 0f, snapT);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceOffsetY, bounceOffsetZ);
            }
            // 2b. Detik 0.8s - 1.5s: REALISTIC HUMAN HEAD BOUNCE (Efek Membal/Mantul Kepala saat Snap -12°)
            else if (elapsed < 1.5f)
            {
                // Forward/back bounce during snap (0.8‑1.5s) with stronger amplitude
                float bounceT = (elapsed - 0.8f) / 0.7f;
                float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 3.0f) * Mathf.Lerp(0.08f, 0f, bounceT);
                transform.localPosition = initialLocalPos + new Vector3(0f, 0f, bounceOffsetZ);
                gazePitch = -12f; // keep pitch stable after snap
                gazeYaw = 0f;
            }
            // 3. Detik 1.5s - 2.5s: Hold Pitch Up -12° Lurus Depan
            else if (elapsed < 2.5f)
            {
                gazePitch = -12f;
                gazeYaw = 0f;
            }
            // 4. Detik 2.5s - 5.5s: Melihat KIRI BAWAH (+30° Pitch Down, -20° Yaw) & Balik ke Depan (-12° Pitch Up, 0° Yaw)
            else if (elapsed < 5.5f)
            {
                // Added forward/back bounce during this descent
                float t = (elapsed - 2.5f) / 3.0f;
                float gazeSin = Mathf.Sin(t * Mathf.PI);
                gazePitch = Mathf.Lerp(-12f, 30f, gazeSin);
                gazeYaw = -20.0f * gazeSin;
                // Bounce with stronger forward/back movement and slight vertical jitter
                float bounceT = (elapsed - 2.5f) / 3.0f;
                float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 4.0f) * Mathf.Lerp(0.12f, 0f, bounceT);
                float bounceOffsetY = Mathf.Sin(bounceT * Mathf.PI * 6.0f) * Mathf.Lerp(0.04f, 0f, bounceT);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceOffsetY, bounceOffsetZ);
            }
            // 5. Detik 5.5s - 7.5s: Tahan Lurus ke Depan (-12° Pitch Up)
            else if (elapsed < 7.5f)
            {
                gazePitch = -12f;
                gazeYaw = 0f;
            }
            // 7.5s - 8.5s: Kembalikan Pitch Up -12° ke 0° Pitch di detik 8.5. Terjadi EFEK BOUNCE DI DETIK 7.5 (7.5s - 8.0s)
            else if (elapsed < 8.5f)
            {
                float t = (elapsed - 7.5f) / 1.0f;
                gazePitch = Mathf.Lerp(-12f, 0f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = 0f;

                // Efek bounce mulai di detik 7.5 (7.5s - 8.0s)
                if (elapsed < 8.0f)
                {
                    float bounceT = (elapsed - 7.5f) / 0.5f;
                    float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 3.0f) * Mathf.Lerp(0.08f, 0f, bounceT);
                    transform.localPosition = initialLocalPos + new Vector3(0f, 0f, bounceOffsetZ);
                }
            }
            // 8.5s - 9.5s: Menoleh cepat ke kanan (40° Yaw Right) & menengadah sedikit (-10° Pitch Up - lebih keatas)
            else if (elapsed < 9.5f)
            {
                float t = (elapsed - 8.5f) / 1.0f;
                gazePitch = Mathf.Lerp(0f, -10f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = Mathf.Lerp(0f, 40.0f, Mathf.SmoothStep(0f, 1f, t));
            }
            // 9.5s - 11.0s: Tahan POV puncak (40° Yaw Right, -10° Pitch Up) hingga detik 11.0s
            else if (elapsed < 11.0f)
            {
                gazePitch = -10f;
                gazeYaw = 40.0f;
            }
            // 11.0s - 12.0s: Pandangan KEMBALI LURUS KE DEPAN (0° Pitch, 0° Yaw) di detik 12.0s
            else if (elapsed < 12.0f)
            {
                float t = (elapsed - 11.0f) / 1.0f;
                gazePitch = Mathf.Lerp(-10f, 0f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = Mathf.Lerp(40.0f, 0f, Mathf.SmoothStep(0f, 1f, t));
            }
            // 12.0s - 14.0s (Rel Datar): Normalisasi Mulus Perlin Rattle ke 0
            else
            {
                float normT = (elapsed - 12.0f) / 2.0f;
                gazePitch = 0f;
                gazeYaw = 0f;
                rattleMultiplier = Mathf.Lerp(1.0f, 0f, normT);
            }

            // Base chain rattle
            float baseRattle = (Mathf.PerlinNoise(Time.time * 18f, 0f) - 0.5f) * 0.4f;
            // Occasional stronger vibration during climb (0‑9s) – random chance ~12%
            float extraVibe = 0f;
            if (elapsed < 9.0f && Random.value < 0.12f)
            {
                // Faster perlin noise for a brief spike
                extraVibe = (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 0.6f;
            }
            float chainRattle = (baseRattle + extraVibe) * rattleMultiplier;

            xRotation = gazePitch + chainRattle;
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.rotation = cartRot * Quaternion.Euler(0f, gazeYaw, 0f);
            }

            yield return null;
        }

        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
        // Reset position to original to avoid drift on replay
        transform.localPosition = initialLocalPos;
        if (cart != null && playerBody != null)
        {
            playerBody.rotation = cart.ForwardRotation;
        }

        cutsceneGazeRoutine = null;
    }

    /// <summary>
    /// 🎢 CUTSCENE TURUN:
    /// </summary>
    public void PlayDropGazeSequence(DarkRideCartController cart, float pauseDuration, float dropDuration)
    {
        if (cutsceneGazeRoutine != null) StopCoroutine(cutsceneGazeRoutine);
        cutsceneGazeRoutine = StartCoroutine(DropGazeRoutine(cart, pauseDuration, dropDuration));
    }

    private IEnumerator DropGazeRoutine(DarkRideCartController cart, float pauseDuration, float dropDuration)
    {
        // 1. Pause at summit phase (0 -> pauseDuration)
        float pauseTimer = 0f;
        while (pauseTimer < pauseDuration)
        {
            pauseTimer += Time.deltaTime;
            float t = pauseTimer / pauseDuration;
            Quaternion cartRot = cart != null ? cart.ForwardRotation : transform.rotation;

            // Looking downward slightly into the drop precipice (+20° Pitch Down)
            xRotation = Mathf.Lerp(0f, 20f, t);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (playerBody != null) playerBody.rotation = cartRot;
            yield return null;
        }

        // 2. Main Drop Gaze Sequence (Total 14 Seconds: 11s Gaze Choreography + 3s Flat Rattle Normalization)
        float totalDropTime = 14.0f;
        float elapsed = 0f;

        while (elapsed < totalDropTime)
        {
            elapsed += Time.deltaTime;
            Quaternion cartRot = cart != null ? cart.ForwardRotation : transform.rotation;

            float gazePitch = 0f;
            float gazeYaw = 0f;
            float rattleMultiplier = 1.0f;

            // 🎥 CHOREOGRAPHY TIMELINE (MIRRORED FOR DESCENT/DROP):
            // Baseline pitch for drop is DOWN (+20° Pitch Down instead of Up -20°)

            // 0.0s - 0.8s: Instant Start Pitch Down (+20°) with strong front/back bounce & vertical shake from SECOND 0!
            if (elapsed < 0.8f)
            {
                float t = elapsed / 0.8f;
                gazePitch = Mathf.Lerp(0f, 20f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = 0f;

                float snapT = elapsed / 0.8f;
                float bounceOffsetZ = Mathf.Sin(snapT * Mathf.PI * 4.0f) * Mathf.Lerp(0.12f, 0f, snapT);
                float bounceOffsetY = Mathf.Sin(snapT * Mathf.PI * 6.0f) * Mathf.Lerp(0.04f, 0f, snapT);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceOffsetY, bounceOffsetZ);
            }
            // 0.8s - 1.5s: Front/back head bounce continuation
            else if (elapsed < 1.5f)
            {
                float bounceT = (elapsed - 0.8f) / 0.7f;
                float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 3.0f) * Mathf.Lerp(0.08f, 0f, bounceT);
                transform.localPosition = initialLocalPos + new Vector3(0f, 0f, bounceOffsetZ);
                gazePitch = 20f; // Hold pitch down (+20°)
                gazeYaw = 0f;
            }
            // 1.5s - 2.5s: Hold Pitch Down +20° straight ahead
            else if (elapsed < 2.5f)
            {
                gazePitch = 20f;
                gazeYaw = 0f;
            }
            // 2.5s - 5.5s: Look DOWN RIGHT (+10° Pitch Down - dinaikkan lagi agar tidak terlalu bawah, +20° Yaw Right)
            else if (elapsed < 5.5f)
            {
                float t = (elapsed - 2.5f) / 3.0f;
                float gazeSin = Mathf.Sin(t * Mathf.PI);
                gazePitch = Mathf.Lerp(20f, 10f, gazeSin); // Dinaikkan lagi (dari 30 ke 10)
                gazeYaw = 20.0f * gazeSin;

                float bounceT = (elapsed - 2.5f) / 3.0f;
                float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 4.0f) * Mathf.Lerp(0.12f, 0f, bounceT);
                float bounceOffsetY = Mathf.Sin(bounceT * Mathf.PI * 6.0f) * Mathf.Lerp(0.04f, 0f, bounceT);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceOffsetY, bounceOffsetZ);
            }
            // 5.5s - 7.5s: Hold Pitch Down (+20°) lurus ke depan
            else if (elapsed < 7.5f)
            {
                gazePitch = 20f;
                gazeYaw = 0f;
            }
            // 7.5s - 8.5s: Kembalikan Pitch Down +20° ke 0° Pitch di detik 8.5. Terjadi EFEK BOUNCE DI DETIK 7.5 (7.5s - 8.0s)
            else if (elapsed < 8.5f)
            {
                float t = (elapsed - 7.5f) / 1.0f;
                gazePitch = Mathf.Lerp(20f, 0f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = 0f;

                // Efek bounce mulai di detik 7.5 (7.5s - 8.0s)
                if (elapsed < 8.0f)
                {
                    float bounceT = (elapsed - 7.5f) / 0.5f;
                    float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 3.0f) * Mathf.Lerp(0.08f, 0f, bounceT);
                    transform.localPosition = initialLocalPos + new Vector3(0f, 0f, bounceOffsetZ);
                }
            }
            // 8.5s - 9.5s: Menoleh cepat ke kanan (90° Yaw Right) & menengadah ke atas (-20° Pitch Up) dari detik 8.5 sampai detik 9.5
            else if (elapsed < 9.5f)
            {
                float t = (elapsed - 8.5f) / 1.0f;
                gazePitch = Mathf.Lerp(0f, -20f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = Mathf.Lerp(0f, 90.0f, Mathf.SmoothStep(0f, 1f, t));
            }
            // 9.5s - 10.5s: Tahan POV puncak (90° Yaw Right, -20° Pitch Up) hingga detik 10.5s
            else if (elapsed < 10.5f)
            {
                gazePitch = -20f;
                gazeYaw = 90.0f;
            }
            // 10.5s - 11.5s: Pandangan KEMBALI LURUS KE DEPAN (0° Pitch, 0° Yaw) di detik 11.5s
            else if (elapsed < 11.5f)
            {
                float t = (elapsed - 10.5f) / 1.0f;
                gazePitch = Mathf.Lerp(-20f, 0f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = Mathf.Lerp(90.0f, 0f, Mathf.SmoothStep(0f, 1f, t));
            }
            // 11.5s - 14.0s (Rel Datar): Normalisasi Mulus Perlin Rattle ke 0
            else
            {
                float normT = (elapsed - 11.5f) / 2.5f;
                gazePitch = 0f;
                gazeYaw = 0f;
                rattleMultiplier = Mathf.Lerp(1.0f, 0f, normT);
            }

            // Wind/drop rattle vibration
            float baseRattle = (Mathf.PerlinNoise(Time.time * 22f, 0f) - 0.5f) * 0.5f;
            float extraVibe = 0f;
            if (elapsed < 10.0f && Random.value < 0.15f)
            {
                extraVibe = (Mathf.PerlinNoise(Time.time * 35f, 0f) - 0.5f) * 0.7f;
            }
            float dropRattle = (baseRattle + extraVibe) * rattleMultiplier;

            xRotation = gazePitch + dropRattle;
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.rotation = cartRot * Quaternion.Euler(0f, gazeYaw, 0f);
            }

            yield return null;
        }

        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
        transform.localPosition = initialLocalPos;

        if (cart != null && playerBody != null)
        {
            playerBody.rotation = cart.ForwardRotation;
        }

        cutsceneGazeRoutine = null;
    }

    /// <summary>
    /// 🚪 CUTSCENE PERPINDAHAN ZONA (ZONE TRANSITION GAZE SEQUENCE)
    /// </summary>
    public void PlayZoneTransitionGazeSequence(DarkRideCartController cart, float duration, float targetPitch, float targetYaw)
    {
        if (cutsceneGazeRoutine != null) StopCoroutine(cutsceneGazeRoutine);
        cutsceneGazeRoutine = StartCoroutine(ZoneTransitionGazeRoutine(cart, duration, targetPitch, targetYaw));
    }

    private IEnumerator ZoneTransitionGazeRoutine(DarkRideCartController cart, float duration, float targetPitch, float targetYaw)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            Quaternion cartRot = cart != null ? cart.ForwardRotation : transform.rotation;

            // Kurva gerakan tatapan: menoleh mulus ke arah sudut (targetPitch, targetYaw) lalu kembali ke (0, 0)
            float gazeSin = Mathf.Sin(t * Mathf.PI);
            float gazePitch = targetPitch * gazeSin;
            float gazeYaw = targetYaw * gazeSin;

            // Sinar getaran ringan rel kereta
            float rattle = (Mathf.PerlinNoise(Time.time * 15f, 0f) - 0.5f) * 0.2f;

            xRotation = gazePitch + rattle;
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.rotation = cartRot * Quaternion.Euler(0f, gazeYaw, 0f);
            }

            yield return null;
        }

        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
        transform.localPosition = initialLocalPos;

        if (cart != null && playerBody != null)
        {
            playerBody.rotation = cart.ForwardRotation;
        }

        cutsceneGazeRoutine = null;
    }
}