using System.Collections;
using UnityEngine;

/// <summary>
/// First Person Camera Controller.
/// Mendukung Transisi Rotasi Kamera Slerp Mulus Tanpa Wobble / Tanpa Berputar Salah Arah.
/// </summary>
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Target & Sensitivity")]
    [SerializeField] private Transform playerBody; // Drag GameObject [Player] ke sini
    [SerializeField] private float mouseSensitivity = 200f;

    [Header("Camera Pitch Constraints")]
    [SerializeField] private float minPitch = -85f; // Batas maksimal lihat ke bawah
    [SerializeField] private float maxPitch = 85f;  // Batas maksimal lihat ke atas

    private float xRotation = 0f;
    private Coroutine smoothRotateRoutine;

    private void Start()
    {
        LockCursor();
        
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }
    }

    private void Update()
    {
        // 🛑 SISTEM KUNCI UNIVERSAL: Jika ada Minigame/Menu yang aktif, tahan kamera!
        if (GameInputLock.IsInputLocked)
        {
            return;
        }

        // Ambil input pergerakan mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Hitung rotasi vertikal (Pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Terapkan rotasi horizontal ke Badan Player
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }

        // Buka kunci kursor jika tekan Escape (Utility Testing)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
        // Kunci kembali jika klik di dalam layar game
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
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

    /// <summary>
    /// Resets camera pitch to 0 and aligns player body to the specified target rotation.
    /// </summary>
    public void ResetRotation(Quaternion targetRotation)
    {
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

    /// <summary>
    /// Smoothly rotates camera pitch to level (0) and player body to face target rotation over time.
    /// Eliminates combined pitch/yaw wobble so camera rotates directly towards target.
    /// </summary>
    public void SmoothRotateToTarget(Quaternion targetWorldRotation, float duration = 0.8f)
    {
        if (smoothRotateRoutine != null) StopCoroutine(smoothRotateRoutine);
        smoothRotateRoutine = StartCoroutine(SmoothRotateRoutine(targetWorldRotation, duration));
    }

    private IEnumerator SmoothRotateRoutine(Quaternion targetWorldRotation, float duration)
    {
        float timer = 0f;

        // 🎯 LURUSKAN DULU ALIGNMENT BADAN & MATA SEMENTARA KE ARAH PANDANGAN REAL-TIME
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

            // Level pitch secara mulus ke 0
            xRotation = Mathf.Lerp(startXRot, 0f, t);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Rotasikan badan player secara langsung & lurus ke rotasi target kereta (Slerp Mulus)
            if (playerBody != null)
            {
                playerBody.rotation = Quaternion.Slerp(startPlayerRot, targetWorldRotation, t);
            }
            yield return null;
        }

        ResetRotation(targetWorldRotation);
        smoothRotateRoutine = null;
    }
}