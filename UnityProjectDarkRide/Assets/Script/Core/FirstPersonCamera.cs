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
            // 2. Detik 0.3s - 0.8s: Fast Snap Pitch Up (Ngadap keatas ke -25° di Detik 0.8s)
            else if (elapsed < 0.8f)
            {
                float t = (elapsed - 0.3f) / 0.5f;
                gazePitch = Mathf.Lerp(0f, -20f, Mathf.SmoothStep(0f, 1f, t));
                gazeYaw = 0f;
                // Strong front/back bounce with vertical jitter during snap (0.3‑0.8s)
                float snapT = (elapsed - 0.3f) / 0.5f;
                // Larger Z amplitude for noticeable forward/back movement
                float bounceOffsetZ = Mathf.Sin(snapT * Mathf.PI * 4.0f) * Mathf.Lerp(0.12f, 0f, snapT);
                // Small Y jitter to simulate shake
                float bounceOffsetY = Mathf.Sin(snapT * Mathf.PI * 6.0f) * Mathf.Lerp(0.04f, 0f, snapT);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceOffsetY, bounceOffsetZ);
            }
            // 2b. Detik 0.8s - 1.5s: REALISTIC HUMAN HEAD BOUNCE (Efek Membal/Mantul Kepala saat Snap -25°, lebih lama)
            else if (elapsed < 1.5f)
            {
                // Forward/back bounce during snap (0.8‑1.5s) with stronger amplitude
                float bounceT = (elapsed - 0.8f) / 0.7f;
                float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 3.0f) * Mathf.Lerp(0.08f, 0f, bounceT);
                transform.localPosition = initialLocalPos + new Vector3(0f, 0f, bounceOffsetZ);
                gazePitch = -20f; // keep pitch stable after snap
                gazeYaw = 0f;
            }
            // 3. Detik 1.5s - 2.5s: Hold Pitch Up -25° Lurus Depan
            // 3. Detik 1.1s - 2.5s: Hold Pitch Up -25° Lurus Depan
            else if (elapsed < 2.5f)
            {
                gazePitch = -20f;
                gazeYaw = 0f;
            }
            // 4. Detik 2.5s - 5.5s: Melihat KIRI BAWAH (+30° Pitch Down, -20° Yaw) & Balik ke Depan (-20° Pitch Up, 0° Yaw)
            else if (elapsed < 5.5f)
            {
                // Added forward/back bounce during this descent
                float t = (elapsed - 2.5f) / 3.0f;
                float gazeSin = Mathf.Sin(t * Mathf.PI);
                gazePitch = Mathf.Lerp(-20f, 30f, gazeSin);
                gazeYaw = -20.0f * gazeSin;
                // Bounce with stronger forward/back movement and slight vertical jitter
                float bounceT = (elapsed - 2.5f) / 3.0f;
                float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 4.0f) * Mathf.Lerp(0.12f, 0f, bounceT);
                float bounceOffsetY = Mathf.Sin(bounceT * Mathf.PI * 6.0f) * Mathf.Lerp(0.04f, 0f, bounceT);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceOffsetY, bounceOffsetZ);
            }
            // 5. Detik 5.5s - 8.5s: Tahan Lurus ke Depan (-25° Pitch Up)
            else if (elapsed < 8.5f)
            {
                gazePitch = -20f;
                gazeYaw = 0f;
            }
            // 6. Detik 8.5s - 11.0s: Melirik LEBIH KE KANAN (+35° Yaw Right) & Balik ke Depan Lurus Pas Detik 11
            else if (elapsed < 11.0f)
            {
                float t = (elapsed - 8.5f) / 2.5f;
                float gazeSin = Mathf.Sin(t * Mathf.PI);
                float basePitch = Mathf.Lerp(-20f, 0f, (elapsed - 8.5f) / 2.5f);
                                // Additional forward/back bounce between 9.5s‑10.0s
                if (elapsed >= 9.5f && elapsed < 10.0f)
                {
                    float bounceT = (elapsed - 9.5f) / 0.5f;
                    float bounceOffsetZ = Mathf.Sin(bounceT * Mathf.PI * 3.0f) * Mathf.Lerp(0.08f, 0f, bounceT);
                    transform.localPosition = initialLocalPos + new Vector3(0f, 0f, bounceOffsetZ);
                    // keep pitch unchanged
                }
                gazePitch = basePitch;
                gazeYaw = 35.0f * gazeSin;
            }
            // 7. Detik 11.0s - 14.0s (5 Detik Rel Datar): Normalisasi Mulus Perlin Rattle & Pitch/Yaw ke 0°
            else
            {
                float normT = (elapsed - 11.0f) / 3.0f;
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
        // Phase 0: Pause at summit, looking down gradually (pitch from 0 to +25)
        float pauseTimer = 0f;
        while (pauseTimer < pauseDuration)
        {
            pauseTimer += Time.deltaTime;
            float t = pauseTimer / pauseDuration;
            xRotation = Mathf.Lerp(0f, 25f, t); // look down
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            if (playerBody != null) playerBody.rotation = cart != null ? cart.ForwardRotation : transform.rotation;
            yield return null;
        }

        // Scale original 14‑second climb timeline to actual drop duration
        float scale = dropDuration / 14f;
        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            Quaternion cartRot = cart != null ? cart.ForwardRotation : transform.rotation;

            // 0‑0.3s (scaled): Hold straight (pitch 0)
            if (elapsed < 0.3f * scale)
            {
                xRotation = 0f;
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            // 0.3‑0.8s: Snap pitch down (+20) with forward/back bounce
            else if (elapsed < 0.8f * scale)
            {
                float t = (elapsed - 0.3f * scale) / (0.5f * scale);
                xRotation = Mathf.Lerp(0f, 20f, Mathf.SmoothStep(0f, 1f, t));
                // Bounce during snap
                float snapT = t;
                float bounceZ = Mathf.Sin(snapT * Mathf.PI * 4f) * Mathf.Lerp(0.12f, 0f, snapT);
                float bounceY = Mathf.Sin(snapT * Mathf.PI * 6f) * Mathf.Lerp(0.04f, 0f, snapT);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceY, bounceZ);
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            // 0.8‑1.5s: Head bounce, keep pitch at +20
            else if (elapsed < 1.5f * scale)
            {
                float bounceT = (elapsed - 0.8f * scale) / (0.7f * scale);
                float bounceZ = Mathf.Sin(bounceT * Mathf.PI * 3f) * Mathf.Lerp(0.08f, 0f, bounceT);
                transform.localPosition = initialLocalPos + new Vector3(0f, 0f, bounceZ);
                xRotation = 20f;
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            // 1.5‑2.5s: Hold pitch +20 straight ahead
            else if (elapsed < 2.5f * scale)
            {
                xRotation = 20f;
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            // 2.5‑5.5s: Look right‑down (+30 pitch, +20 yaw) then back to straight (mirrored from left‑down)
            else if (elapsed < 5.5f * scale)
            {
                float t = (elapsed - 2.5f * scale) / (3f * scale);
                float pitchSin = Mathf.Sin(t * Mathf.PI);
                xRotation = Mathf.Lerp(20f, -30f, pitchSin); // down then up
                float yaw = 20f * pitchSin; // look right
                // Bounce
                float bounceZ = Mathf.Sin(t * Mathf.PI * 4f) * Mathf.Lerp(0.12f, 0f, t);
                float bounceY = Mathf.Sin(t * Mathf.PI * 6f) * Mathf.Lerp(0.04f, 0f, t);
                transform.localPosition = initialLocalPos + new Vector3(0f, bounceY, bounceZ);
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                if (playerBody != null) playerBody.rotation = cartRot * Quaternion.Euler(0f, yaw, 0f);
            }
            // 5.5‑8.5s: Hold straight (pitch 0)
            else if (elapsed < 8.5f * scale)
            {
                xRotation = 0f;
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            // 8.5‑10s (or until end): Quick yaw left while returning to level pitch
            else
            {
                float t = (elapsed - 8.5f * scale) / (dropDuration - 8.5f * scale);
                float yaw = Mathf.Lerp(0f, -35f, Mathf.Sin(t * Mathf.PI));
                xRotation = Mathf.Lerp(0f, -25f, t); // slight upward look as we finish
                if (playerBody != null) playerBody.rotation = cartRot * Quaternion.Euler(0f, yaw, 0f);
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }

            // Subtle rattle during drop
            float rattle = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 0.4f;
            transform.localRotation *= Quaternion.Euler(rattle, 0f, 0f);
            yield return null;
        }

        // After descent, keep camera steady (flat section handled by trigger)
        if (cart != null) ResetRotation(cart.ForwardRotation);
        cutsceneGazeRoutine = null;
    }
}