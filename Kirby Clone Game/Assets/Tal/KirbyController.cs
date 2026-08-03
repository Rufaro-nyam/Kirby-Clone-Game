using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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
    public GameObject starProjectilePrefab;
    public float starProjectileSpeed = 12f;

    [Header("Inhale Settings")]
    public Transform inhalePoint;
    public Vector2 inhaleBoxSize = new Vector2(4f, 2f);
    public LayerMask inhalableLayer;
    public float inhalePullSpeed = 5f;
    public float eatDistance = 0.8f;

    [Header("Ability UI Settings")]
    public TextMeshProUGUI abilityUIText;

    [Header("Copy Abilities")]
    public CopyAbility currentEquippedAbility = CopyAbility.None;
    public float flyAbilitySpeed = 4f;

    [Header("Bow Settings")]
    public GameObject arrowPrefab;
    public float arrowSpeed = 15f;
    public float arrowLifetime = 1f;
    public float bowCooldown = 0.5f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isGrounded;
    private float defaultGravity;

    // --- State Tracking Variables ---
    private bool isSprinting;
    private bool isCrouching;
    private bool isFloating;
    private bool isCupidFlying;
    private bool isChargingBow;
    private bool isDashing;
    private float facingDirection = 1f;
    private float lastTapTime;
    private float lastTapDirection;
    private int currentFloatJumps = 0;

    // --- Cooldown Tracking ---
    private float nextBowFireTime = 0f;
    private float dashTimeLeft = 0f;
    private float nextDashTime = 0f;

    // --- Mouth Variables ---
    private bool isInhaling;
    private bool hasSomethingInMouth;
    private CopyAbility currentlyInhaledAbility = CopyAbility.None;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        defaultGravity = rb.gravityScale;
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

                if (isCupidFlying)
                {
                    isCupidFlying = false;
                    rb.gravityScale = defaultGravity;
                }
            }
        }

        // Prevent crouching if charging the bow or dashing
        if (verticalInput < -0.5f && isGrounded && !isCupidFlying && !isChargingBow && !isDashing)
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
            isCrouching = false;
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

    void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        horizontalInput = input.x;
        verticalInput = input.y;

        // Stop flipping facing direction visually if charging the bow or dashing
        if (horizontalInput != 0 && !isCrouching && !isInhaling && !isChargingBow && !isDashing)
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
        else if (!isChargingBow && !isDashing)
        {
            isSprinting = false;
        }
    }

    void OnJump(InputValue value)
    {
        // Prevent jumping while aiming the bow or dashing
        if (value.isPressed && !isCrouching && !isInhaling && !isChargingBow && !isDashing)
        {
            if (currentEquippedAbility == CopyAbility.Fly && !isGrounded && !hasSomethingInMouth)
            {
                isCupidFlying = !isCupidFlying;
                rb.gravityScale = isCupidFlying ? 0f : defaultGravity;

                if (isCupidFlying)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                return;
            }

            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (currentFloatJumps < maxFloatJumps && !hasSomethingInMouth && !isCupidFlying)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, floatJumpForce);
                currentFloatJumps++;
                isFloating = true;
            }
        }
    }

    void OnPrimaryAction(InputValue value)
    {
        // Don't allow new actions if currently dashing
        if (isDashing) return;

        if (value.isPressed)
        {
            if (currentEquippedAbility == CopyAbility.Fly)
            {
                rb.AddForce(new Vector2(facingDirection * 15f, 0f), ForceMode2D.Impulse);
                return;
            }

            if (currentEquippedAbility == CopyAbility.Bow)
            {
                if (Time.time >= nextBowFireTime)
                {
                    isChargingBow = true;
                }
                return;
            }

            // DASH LOGIC: Check cooldown and initiate dash
            if (currentEquippedAbility == CopyAbility.Dash)
            {
                if (Time.time >= nextDashTime && !isDashing)
                {
                    isDashing = true;
                    dashTimeLeft = dashDuration;
                    nextDashTime = Time.time + dashCooldown;

                    PlayerHealth health = GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.TriggerDashInvincibility(dashDuration);
                    }
                }
                return;
            }

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
            else if (hasSomethingInMouth)
            {
                GroundSpit();
            }
            else if (isGrounded && !isCrouching && currentEquippedAbility == CopyAbility.None)
            {
                isInhaling = true;
            }
        }
        else
        {
            isInhaling = false;

            if (isChargingBow)
            {
                FireBow();
                isChargingBow = false;
            }
        }
    }

    void FireBow()
    {
        if (arrowPrefab != null)
        {
            Vector2 aimDirection = new Vector2(horizontalInput, verticalInput).normalized;

            if (aimDirection == Vector2.zero)
            {
                aimDirection = new Vector2(facingDirection, 0f);
            }
            else if (aimDirection.x != 0)
            {
                facingDirection = Mathf.Sign(aimDirection.x);
            }

            Vector3 spawnPos = spitSpawnPoint != null ? spitSpawnPoint.position : transform.position;
            GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

            Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
            if (arrowRb != null)
            {
                arrowRb.linearVelocity = aimDirection * arrowSpeed;
            }

            nextBowFireTime = Time.time + bowCooldown;
            Destroy(arrow, arrowLifetime);
        }
    }

    void ProcessInhaleSuction()
    {
        Vector2 boxCenter = (Vector2)inhalePoint.position + new Vector2(facingDirection * (inhaleBoxSize.x / 2f), 0);
        Collider2D[] objectsToSuck = Physics2D.OverlapBoxAll(boxCenter, inhaleBoxSize, 0f, inhalableLayer);

        foreach (Collider2D obj in objectsToSuck)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, inhalePoint.position, inhalePullSpeed * Time.deltaTime);

            if (Vector2.Distance(obj.transform.position, inhalePoint.position) < eatDistance)
            {
                InhalableEnemy enemyData = obj.GetComponent<InhalableEnemy>();
                if (enemyData != null)
                {
                    currentlyInhaledAbility = enemyData.abilityType;
                }
                else
                {
                    currentlyInhaledAbility = CopyAbility.None;
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

        if (currentlyInhaledAbility != CopyAbility.None)
        {
            currentEquippedAbility = currentlyInhaledAbility;
            Debug.Log($"Kirby equipped: {currentEquippedAbility}!");
            abilityUIText.text = currentEquippedAbility.ToString();
        }

        currentlyInhaledAbility = CopyAbility.None;
    }

    void GroundSpit()
    {
        hasSomethingInMouth = false;
        currentlyInhaledAbility = CopyAbility.None;

        if (starProjectilePrefab != null)
        {
            Vector3 spawnPos = spitSpawnPoint != null ? spitSpawnPoint.position : transform.position;
            GameObject star = Instantiate(starProjectilePrefab, spawnPos, Quaternion.identity);

            Rigidbody2D starRb = star.GetComponent<Rigidbody2D>();
            if (starRb != null)
            {
                starRb.linearVelocity = new Vector2(facingDirection * starProjectileSpeed, 0f);
            }
            Destroy(star, 2f);
        }
    }

    void FixedUpdate()
    {
        // Highest Priority: Dashing
        if (isDashing)
        {
            if (dashTimeLeft > 0)
            {
                // Force a straight horizontal line, effectively ignoring gravity for the duration
                rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);
                dashTimeLeft -= Time.fixedDeltaTime;
                return; // Skip all other movement logic while dashing
            }
            else
            {
                isDashing = false;
            }
        }

        // Secondary Priority: Immobilizing actions
        if (isCrouching || isInhaling || isChargingBow)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else if (isCupidFlying)
        {
            rb.linearVelocity = new Vector2(horizontalInput * flyAbilitySpeed, verticalInput * flyAbilitySpeed);
        }
        else
        {
            // Normal Movement
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

    // --- COMBAT & COLLISION LOGIC ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleEnemyContact(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnemyContact(other.gameObject);
    }

    private void HandleEnemyContact(GameObject touchedObject)
    {
        // Check if the object we touched is an enemy
        InhalableEnemy enemy = touchedObject.GetComponent<InhalableEnemy>();

        if (enemy != null)
        {
            if (isDashing)
            {
                // Kirby is invincible and lethal during a dash!
                Debug.Log("Dash Attack! Enemy destroyed.");
                Destroy(touchedObject);
            }
            else if (!isInhaling)
            {
                // Kirby is NOT dashing. 
                // This is where we will eventually make Kirby take damage or get knocked back!
                Debug.Log("Kirby got hurt!");
            }
        }
    }
}