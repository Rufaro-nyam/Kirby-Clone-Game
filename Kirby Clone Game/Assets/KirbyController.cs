using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KirbyController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float walkSpeed = 5f;
    public float dashSpeed = 8f;
    public float jumpForce = 12f;
    public float highJumpForce = 16f;
    public float floatFlapForce = 6f;
    public float slideSpeed = 10f;
    public float slideDuration = 0.4f;

    [Header("State Flags")]
    public bool isGrounded = false;
    public bool isFloating = false;
    public bool isCrouching = false;
    public bool isSliding = false;
    public bool isDashing = false;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    private Rigidbody2D rb;

    // Timers for double taps and holds
    private float lastRightTapTime;
    private float lastLeftTapTime;
    private float lastJumpTapTime;
    private float doubleTapWindow = 0.25f;
    private float jumpHoldTime = 0f;
    private float jumpHoldThreshold = 0.2f; // Time needed to hold for a high jump
    private float slideTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        CheckGrounded();
        HandleInputs();
    }

    private void CheckGrounded()
    {
        // Simple tag-based collision check using OverlapCircle (create an empty GameObject at Kirby's feet and assign it to groundCheck)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        isGrounded = false;
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Ground"))
            {
                isGrounded = true;
                if (isFloating) ExitFloat(); // Landing cancels float
                break;
            }
        }
    }

    private void HandleInputs()
    {
        // 9) Crouch & 10) Slide
        if (isGrounded && !isFloating)
        {
            if (Input.GetKey(KeyCode.DownArrow))
            {
                isCrouching = true;
                isDashing = false; // Crouching cancels dash

                if (Input.GetKeyDown(KeyCode.X) && !isSliding) // Assuming 'B' button is mapped to X
                {
                    StartSlide();
                }
            }
            else
            {
                isCrouching = false;
            }
        }

        if (isSliding)
        {
            HandleSlideTimer();
            return; // Lock out other inputs while sliding
        }

        // 1) Walk & 2) Dash (Double Tap detection)
        HandleHorizontalMovement();

        // 3), 4), 5), 6), 7) Jump and Float Logic
        HandleJumpAndFloat();

        // 8) Float Spit
        if (isFloating && Input.GetKeyDown(KeyCode.X)) // 'B' Button
        {
            FloatSpit();
        }
    }

    private void HandleHorizontalMovement()
    {
        if (isCrouching)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Double tap logic for Left/Right
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Time.time - lastRightTapTime < doubleTapWindow) isDashing = true;
            lastRightTapTime = Time.time;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Time.time - lastLeftTapTime < doubleTapWindow) isDashing = true;
            lastLeftTapTime = Time.time;
        }

        // Stop dashing if we stop pressing the direction
        if (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.LeftArrow))
        {
            isDashing = false;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = isDashing ? dashSpeed : walkSpeed;

        // Apply movement (allow air movement but at normal speed if floating)
        if (isFloating) currentSpeed = walkSpeed;

        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    private void HandleJumpAndFloat()
    {
        bool jumpPressed = Input.GetKeyDown(KeyCode.Z); // 'A' button
        bool jumpHeld = Input.GetKey(KeyCode.Z);
        bool jumpReleased = Input.GetKeyUp(KeyCode.Z);
        bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
        bool upHeld = Input.GetKey(KeyCode.UpArrow);

        // Track how long jump is held for High Jump (4)
        if (jumpHeld && !isFloating && isGrounded)
        {
            jumpHoldTime += Time.deltaTime;
        }

        // 3) Regular Jump & 4) High Jump (Triggered on release or threshold met)
        if (jumpReleased && isGrounded && !isFloating)
        {
            float applyForce = (jumpHoldTime >= jumpHoldThreshold) ? highJumpForce : jumpForce;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, applyForce);
            jumpHoldTime = 0f;
        }

        // 5) Enter Float
        if (!isGrounded && !isFloating)
        {
            // Enter float on double tap 'A' in air, or holding UP
            if (jumpPressed)
            {
                if (Time.time - lastJumpTapTime < doubleTapWindow) EnterFloat();
                lastJumpTapTime = Time.time;
            }
            else if (upHeld)
            {
                EnterFloat();
            }
        }

        // 6) Single Float Jump & 7) Infinite Float Jump
        if (isFloating)
        {
            // Tap to flap, hold to keep going up
            if (jumpPressed || upPressed || jumpHeld || upHeld)
            {
                // Give a slight upward boost
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, floatFlapForce);
            }
        }
    }

    private void EnterFloat()
    {
        isFloating = true;
        isDashing = false;
        rb.gravityScale = 0.5f; // Kirby falls slower while floating

        // Initial puff upward
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, floatFlapForce);
    }

    private void ExitFloat()
    {
        isFloating = false;
        rb.gravityScale = 1f; // Return to normal gravity
    }

    private void FloatSpit()
    {
        // TODO: Instantiate star projectile here
        Debug.Log("Float Spit!");
        ExitFloat();
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

        // Determine direction based on facing (simplification: using input or velocity)
        float direction = Input.GetAxisRaw("Horizontal");
        if (direction == 0) direction = 1; // Default to right if no input

        rb.linearVelocity = new Vector2(direction * slideSpeed, rb.linearVelocity.y);
    }

    private void HandleSlideTimer()
    {
        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0)
        {
            isSliding = false;
        }
    }
}