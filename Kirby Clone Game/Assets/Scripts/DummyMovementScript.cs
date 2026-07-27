using System.Collections;
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
    private const int MAX_JUMPS = 2;

    // Track when player is being knocked back
    private bool isKnockedBack = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Don't take input while locked in knockback
        if (isKnockedBack) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.transform.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = MAX_JUMPS;
        }

        if (Input.GetButtonDown("Jump"))
        {
            TryJump();
        }
    }

    private void FixedUpdate()
    {
        // STOP OVERWRITING VELOCITY WHILE KNOCKED BACK!
        if (isKnockedBack) return;

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    private void TryJump()
    {
        if (isGrounded || jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;
        }
    }

    // --- PUBLIC METHOD CALLED BY DAMAGE SCRIPT ---
    public void ApplyKnockback(Vector2 force, float duration)
    {
        StartCoroutine(KnockbackRoutine(force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 force, float duration)
    {
        isKnockedBack = true;

        // Reset velocity so previous movement momentum doesn't counter knockback
        rb.linearVelocity = Vector2.zero;

        // Apply knockback impulse
        rb.AddForce(force, ForceMode2D.Impulse);

        // Wait for knockback duration before returning movement control
        yield return new WaitForSeconds(duration);

        isKnockedBack = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.transform.position, groundCheckRadius);
        }
    }
}