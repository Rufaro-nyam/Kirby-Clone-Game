using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingEnemy : MonoBehaviour
{
    private enum EnemyState { Idle, Telegraph, Charging }

    [Header("Detection & Obstacles")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Hover & Ground Check")]
    [SerializeField] private float hoverDistance = 0.5f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float minGroundClearance = 1.0f; // Distance above floor
    [SerializeField] private float groundPushSmoothness = 10f; // Controls ease-up speed

    [Header("Attack Telegraph Settings")]
    [SerializeField] private float telegraphDuration = 0.8f;
    [SerializeField] private Vector3 expandedScale = new Vector3(1.3f, 1.3f, 1f);

    [Header("Charge & Cooldown Settings")]
    [SerializeField] private float chargeSpeed = 16f;
    [SerializeField] private float cooldownDuration = 2.0f;

    private EnemyState currentState = EnemyState.Idle;
    private Rigidbody2D rb;
    private CircleCollider2D myCollider;
    private Vector3 originalScale;
    private Vector3 hoverCenter;
    private Vector2 targetPosition;

    private float cooldownTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<CircleCollider2D>();

        rb.isKinematic = true;
        originalScale = transform.localScale;
        hoverCenter = transform.position;
    }

    private void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == EnemyState.Idle)
        {
            HandleIdleHover();

            if (cooldownTimer <= 0)
            {
                CheckForPlayer();
            }
        }
    }

    private void HandleIdleHover()
    {
        // Pure sine-wave target
        float rawY = hoverCenter.y + Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;
        Vector2 targetY = new Vector2(hoverCenter.x, rawY);

        // Ground raycast check
        float radius = myCollider != null ? myCollider.radius * transform.localScale.x : 0.2f;
        float rayDistance = minGroundClearance + hoverDistance + radius;

        RaycastHit2D groundHit = Physics2D.Raycast(rb.position, Vector2.down, rayDistance, groundLayer);

        if (groundHit.collider != null)
        {
            float floorLimit = groundHit.point.y + minGroundClearance + radius;

            // If sine wave dips below floorLimit, push upwards smoothly using Lerp
            if (targetY.y < floorLimit)
            {
                targetY.y = Mathf.Lerp(rb.position.y, floorLimit, groundPushSmoothness * Time.fixedDeltaTime);
            }
        }

        // Apply position update smoothly
        rb.MovePosition(Vector2.Lerp(rb.position, targetY, groundPushSmoothness * Time.fixedDeltaTime));
    }

    private void CheckForPlayer()
    {
        Collider2D player = Physics2D.OverlapCircle(rb.position, detectionRadius, playerLayer);
        if (player != null)
        {
            StartCoroutine(AttackSequence(player.transform));
        }
    }

    private IEnumerator AttackSequence(Transform playerTransform)
    {
        currentState = EnemyState.Telegraph;

        // --- 1. TELEGRAPH PHASE ---
        float timer = 0f;
        while (timer < telegraphDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, expandedScale, timer / telegraphDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = expandedScale;

        // --- TARGET CALCULATION ---
        Vector2 startPos = rb.position;
        Vector2 intendedTarget = playerTransform != null ? (Vector2)playerTransform.position : startPos + Vector2.right;

        Vector2 direction = (intendedTarget - startPos).normalized;
        float fullDistance = Vector2.Distance(startPos, intendedTarget);

        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, fullDistance, groundLayer);
        float colliderPadding = myCollider != null ? myCollider.radius * transform.localScale.x : 0.2f;

        if (hit.collider != null)
        {
            targetPosition = hit.point - (direction * (colliderPadding + 0.15f));
        }
        else
        {
            targetPosition = intendedTarget;
        }

        // --- 2. CHARGE PHASE ---
        currentState = EnemyState.Charging;
        transform.localScale = originalScale;

        while (Vector2.Distance(rb.position, targetPosition) > 0.05f)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPosition, chargeSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPosition);

        // --- 3. RECOVERY (SMOOTH EASE UP) ---
        Vector2 endPos = targetPosition;
        RaycastHit2D immediateGround = Physics2D.Raycast(endPos, Vector2.down, minGroundClearance + colliderPadding, groundLayer);

        if (immediateGround.collider != null)
        {
            endPos.y = immediateGround.point.y + minGroundClearance + colliderPadding;
        }

        // Set new hover center to the safe height (the smooth Lerp in HandleIdleHover handles rising visually)
        hoverCenter = endPos;

        cooldownTimer = cooldownDuration;
        currentState = EnemyState.Idle;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector2.down * minGroundClearance);
    }
}