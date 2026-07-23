using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private int jumpsRemaining;
    private const int MAX_JUMPS = 2; // Allows initial jump + 1 extra (double jump)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Get horizontal input (A/D, Left/Right Arrows)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Check if player is standing on the ground
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.transform.position, groundCheckRadius, groundLayer);

        // Reset jumps when landing
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = MAX_JUMPS;
        }

        // Jump Input (Space bar or W / Up Arrow if configured in Input Manager)
        if (Input.GetButtonDown("Jump"))
        {
            TryJump();
        }
    }

    private void FixedUpdate()
    {
        // Apply horizontal velocity (preserves vertical velocity for falling/jumping)
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    private void TryJump()
    {
        if (isGrounded || jumpsRemaining > 0)
        {
            // Reset vertical velocity before jump so double jump feels crisp
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
        }
    }

    // Visual helper to see the ground check circle in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            //Gizmos.ra = false;
            Gizmos.DrawWireSphere(groundCheck.transform.position, groundCheckRadius);
        }
    }
}