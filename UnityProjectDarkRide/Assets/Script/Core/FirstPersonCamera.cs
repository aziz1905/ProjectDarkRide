using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Target & Sensitivity")]
    [SerializeField] private Transform playerBody; // Drag GameObject [Player] ke sini
    [SerializeField] private float mouseSensitivity = 200f;

    [Header("Camera Pitch Constraints")]
    [SerializeField] private float minPitch = -85f; // Batas maksimal lihat ke bawah
    [SerializeField] private float maxPitch = 85f;  // Batas maksimal lihat ke atas

    private float xRotation = 0f;

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
        // 🛑 JIKA MINIGAME TIMING SEDANG AKTIF: BEKUKAN KAMERA & JANGAN KUNCI KURSOR!
        if (TimingMinigameController.Instance != null && TimingMinigameController.Instance.IsPlaying)
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
}