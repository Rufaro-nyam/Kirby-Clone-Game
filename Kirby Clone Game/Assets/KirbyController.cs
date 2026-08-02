using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class KirbyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 5f;
    public float jumpForce = 4f;

    [Header("Double Tap Settings")]
    public float doubleTapThreshold = 0.3f;

    [Header("Float Jump Settings")]
    public int maxFloatJumps = 3;
    public float floatJumpForce = 4f;

    [Header("Crouch Settings")]
    public float crouchHeightMultiplier = 0.5f;

    [Header("Spit Settings")]
    public GameObject airPuffPrefab;
    public Transform spitSpawnPoint;
    public float airPuffSpeed = 8f;
    public float airPuffLifetime = 0.4f;

    [Header("Ground Spit Settings")]
    public GameObject starProjectilePrefab; // The heavy star he shoots when spitting an enemy
    public float starProjectileSpeed = 12f;

    [Header("Inhale Settings")]
    public Transform inhalePoint;
    public Vector2 inhaleBoxSize = new Vector2(4f, 2f);
    public LayerMask inhalableLayer;
    public float inhalePullSpeed = 5f;
    public float eatDistance = 0.8f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isGrounded;

    // --- State Tracking Variables ---
    private bool isSprinting;
    private bool isCrouching;
    private bool isFloating;
    private float facingDirection = 1f;
    private float lastTapTime;
    private float lastTapDirection;
    private int currentFloatJumps = 0;

    // --- Mouth & Ability Variables ---
    private bool isInhaling;
    private bool hasSomethingInMouth;
    private CopyAbility currentlyInhaledAbility = CopyAbility.None; // Remembers what is in his mouth

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (groundCheck != null)
        {
            float checkRadius = isCrouching ? groundCheckRadius + 0.5f : groundCheckRadius;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

            if (isGrounded)
            {
                currentFloatJumps = 0;
                isFloating = false;
            }
        }

        // --- SWALLOW & CROUCH LOGIC ---
        // Holding down triggers a swallow if mouth is full, or a crouch if empty
        if (verticalInput < -0.5f && isGrounded)
        {
            if (hasSomethingInMouth)
            {
                Swallow();
            }
            else if (!isInhaling)
            {
                isCrouching = true;
            }
        }
        else
        {
            isCrouching = false; // Stop crouching when let go
        }

        Vector3 currentScale = transform.localScale;
        currentScale.y = isCrouching ? crouchHeightMultiplier : 1f;
        currentScale.x = hasSomethingInMouth ? 1.3f : 1f;
        transform.localScale = currentScale;

        if (isInhaling)
        {
            ProcessInhaleSuction();
        }
    }

    // --- INPUT MESSAGES ---

    void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        horizontalInput = input.x;
        verticalInput = input.y;

        if (horizontalInput != 0 && !isCrouching && !isInhaling)
        {
            float currentDirection = Mathf.Sign(horizontalInput);
            facingDirection = currentDirection;

            if (!hasSomethingInMouth && Time.time - lastTapTime <= doubleTapThreshold && currentDirection == lastTapDirection)
            {
                isSprinting = true;
            }
            else
            {
                isSprinting = false;
            }

            lastTapTime = Time.time;
            lastTapDirection = currentDirection;
        }
        else
        {
            isSprinting = false;
        }
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && !isCrouching && !isInhaling)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (currentFloatJumps < maxFloatJumps && !hasSomethingInMouth)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, floatJumpForce);
                currentFloatJumps++;
                isFloating = true;
            }
        }
    }

    void OnPrimaryAction(InputValue value)
    {
        if (value.isPressed)
        {
            // 1. FLOAT SPIT
            if (isFloating && !hasSomethingInMouth)
            {
                isFloating = false;
                currentFloatJumps = maxFloatJumps;

                if (airPuffPrefab != null)
                {
                    Vector3 spawnPos = spitSpawnPoint != null ? spitSpawnPoint.position : transform.position;
                    GameObject puff = Instantiate(airPuffPrefab, spawnPos, Quaternion.identity);

                    Rigidbody2D puffRb = puff.GetComponent<Rigidbody2D>();
                    if (puffRb != null)
                    {
                        puffRb.linearVelocity = new Vector2(facingDirection * airPuffSpeed, 0f);
                    }
                    Destroy(puff, airPuffLifetime);
                }
            }
            // 2. GROUND SPIT (Shoot the star!)
            else if (hasSomethingInMouth)
            {
                GroundSpit();
            }
            // 3. INHALE
            else if (isGrounded && !isCrouching)
            {
                isInhaling = true;
            }
        }
        else
        {
            isInhaling = false;
        }
    }

    // --- ABILITY LOGIC ---

    void ProcessInhaleSuction()
    {
        Vector2 boxCenter = (Vector2)inhalePoint.position + new Vector2(facingDirection * (inhaleBoxSize.x / 2f), 0);
        Collider2D[] objectsToSuck = Physics2D.OverlapBoxAll(boxCenter, inhaleBoxSize, 0f, inhalableLayer);

        foreach (Collider2D obj in objectsToSuck)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, inhalePoint.position, inhalePullSpeed * Time.deltaTime);

            if (Vector2.Distance(obj.transform.position, inhalePoint.position) < eatDistance)
            {
                // Check if the enemy has a copy ability script before we destroy it
                InhalableEnemy enemyData = obj.GetComponent<InhalableEnemy>();
                if (enemyData != null)
                {
                    currentlyInhaledAbility = enemyData.abilityType; // Save the ability!
                }
                else
                {
                    currentlyInhaledAbility = CopyAbility.None; // Normal enemy
                }

                Destroy(obj.gameObject);
                hasSomethingInMouth = true;
                isInhaling = false;
                break;
            }
        }
    }

    void Swallow()
    {
        hasSomethingInMouth = false;

        // This is where we will trigger the actual ability change later!
        if (currentlyInhaledAbility != CopyAbility.None)
        {
            Debug.Log($"SWALLOWED! Kirby gained the {currentlyInhaledAbility} ability!");
            // TODO: Equip ability logic goes here
        }
        else
        {
            Debug.Log("Swallowed a normal enemy. Yummy!");
        }

        // Reset the tracked ability so his mouth is completely empty
        currentlyInhaledAbility = CopyAbility.None;
    }

    void GroundSpit()
    {
        hasSomethingInMouth = false;
        currentlyInhaledAbility = CopyAbility.None; // We spit it out, so we lose the ability chance

        if (starProjectilePrefab != null)
        {
            Vector3 spawnPos = spitSpawnPoint != null ? spitSpawnPoint.position : transform.position;
            GameObject star = Instantiate(starProjectilePrefab, spawnPos, Quaternion.identity);

            Rigidbody2D starRb = star.GetComponent<Rigidbody2D>();
            if (starRb != null)
            {
                starRb.linearVelocity = new Vector2(facingDirection * starProjectileSpeed, 0f);
            }

            // Destroy the star after 2 seconds so it doesn't fly forever
            Destroy(star, 2f);
        }
    }

    // --- PHYSICS MOVEMENT ---

    void FixedUpdate()
    {
        if (isCrouching || isInhaling)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else
        {
            float currentSpeed = walkSpeed;

            if (hasSomethingInMouth)
            {
                currentSpeed = walkSpeed / 2f;
            }
            else if (isSprinting)
            {
                currentSpeed = sprintSpeed;
            }

            rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (inhalePoint != null)
        {
            Gizmos.color = Color.yellow;
            float facing = Application.isPlaying ? facingDirection : 1f;
            Vector2 boxCenter = (Vector2)inhalePoint.position + new Vector2(facing * (inhaleBoxSize.x / 2f), 0);
            Gizmos.DrawWireCube(boxCenter, inhaleBoxSize);
        }
    }
}