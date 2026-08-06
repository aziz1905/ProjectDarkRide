using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. Cek apakah pemain berada di atas tanah
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // 2. Ambil Input WASD / Arrow Keys
        float x = Input.GetAxis("Horizontal"); // A (-1) dan D (+1)
        float z = Input.GetAxis("Vertical");   // S (-1) dan W (+1)

        // 3. Arah pergerakan relatif terhadap rotasi karakter
        Vector3 move = transform.right * x + transform.forward * z;

        // 4. Gerakkan Karakter
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 5. Fitur Lompat (Tombol Space)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 6. Aplikasi Gravitasi
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}