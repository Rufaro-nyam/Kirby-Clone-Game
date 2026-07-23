using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GroundedDashingEnemy : MonoBehaviour
{
    private enum EnemyState { Patrol, Telegraph, Dashing, Cooldown }

    [Header("Graphics & Face References")]
    [SerializeField] private Transform graphicsTransform; // Child object containing sprite/animator
    [SerializeField] private Transform faceTransform;     // Child object for the face (flips X from 1 to -1)

    [Header("Patrol Settings")]
    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float pauseAtEndpoints = 1.0f;

    [Header("Player Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float detectionHeight = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Telegraph Settings")]
    [SerializeField] private float telegraphDuration = 0.6f;
    [SerializeField] private Vector3 expandedScale = new Vector3(1.3f, 1.3f, 1f);

    [Header("Dash Attack Settings")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float maxDashDistance = 8f;
    [SerializeField] private float attackCooldown = 1.5f;

    private EnemyState currentState = EnemyState.Patrol;
    private Rigidbody2D rb;
    private Collider2D myCollider;

    private Vector2 startPoint;
    private Vector2 leftPatrolPoint;
    private Vector2 rightPatrolPoint;
    private Vector2 targetPatrolPoint;

    private Vector3 originalGraphicsScale;
    private bool facingRight = true;
    private bool isWaitingAtPoint = false;
    private float cooldownTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        rb.isKinematic = true;
        startPoint = rb.position;

        leftPatrolPoint = startPoint + Vector2.left * patrolDistance;
        rightPatrolPoint = startPoint + Vector2.right * patrolDistance;
        targetPatrolPoint = rightPatrolPoint;

        if (graphicsTransform != null)
        {
            originalGraphicsScale = graphicsTransform.localScale;
        }
        else
        {
            originalGraphicsScale = transform.localScale;
        }

        // Initialize default facing
        SetFacingDirection(facingRight);
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
        if (currentState == EnemyState.Patrol)
        {
            HandlePatrol();

            if (cooldownTimer <= 0)
            {
                CheckForPlayer();
            }
        }
    }

    private void HandlePatrol()
    {
        if (isWaitingAtPoint) return;

        // Ensure facing direction stays aligned with actual movement direction
        bool movingRight = targetPatrolPoint.x > rb.position.x;
        if (movingRight != facingRight)
        {
            SetFacingDirection(movingRight);
        }

        // Move towards target patrol endpoint
        Vector2 newPos = Vector2.MoveTowards(rb.position, targetPatrolPoint, patrolSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // Reached endpoint check
        if (Vector2.Distance(rb.position, targetPatrolPoint) < 0.05f)
        {
            StartCoroutine(nameof(PatrolPauseRoutine));
        }
    }

    private IEnumerator PatrolPauseRoutine()
    {
        isWaitingAtPoint = true;
        yield return new WaitForSeconds(pauseAtEndpoints);

        // Switch patrol direction
        if (targetPatrolPoint == rightPatrolPoint)
        {
            targetPatrolPoint = leftPatrolPoint;
            SetFacingDirection(false);
        }
        else
        {
            targetPatrolPoint = rightPatrolPoint;
            SetFacingDirection(true);
        }

        isWaitingAtPoint = false;
    }

    private void CheckForPlayer()
    {
        // Detection box ALWAYS points in the direction the enemy is currently facing/walking
        float dir = facingRight ? 1f : -1f;
        Vector2 boxSize = new Vector2(detectionRange, detectionHeight);
        Vector2 boxCenter = rb.position + (Vector2.right * (detectionRange / 2f) * dir);

        Collider2D playerHit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerLayer);

        if (playerHit != null)
        {
            StartCoroutine(AttackSequence(playerHit.transform));
        }
    }

    private IEnumerator AttackSequence(Transform playerTransform)
    {
        currentState = EnemyState.Telegraph;

        StopCoroutine(nameof(PatrolPauseRoutine));
        isWaitingAtPoint = false;

        // Immediately face player location
        bool playerToRight = playerTransform.position.x > rb.position.x;
        SetFacingDirection(playerToRight);

        // --- 1. TELEGRAPH PHASE ---
        float timer = 0f;
        Transform targetVisual = graphicsTransform != null ? graphicsTransform : transform;

        while (timer < telegraphDuration)
        {
            targetVisual.localScale = Vector3.Lerp(originalGraphicsScale, expandedScale, timer / telegraphDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        targetVisual.localScale = expandedScale;

        // Calculate dash destination at end of telegraph
        float dashDirection = facingRight ? 1f : -1f;
        Vector2 startPos = rb.position;
        Vector2 targetDashPos = startPos + (Vector2.right * dashDirection * maxDashDistance);

        // Raycast against walls/ground to avoid dashing through obstacles
        RaycastHit2D wallHit = Physics2D.Raycast(startPos, Vector2.right * dashDirection, maxDashDistance, groundLayer);
        if (wallHit.collider != null)
        {
            float padding = myCollider != null ? myCollider.bounds.extents.x : 0.3f;
            targetDashPos.x = wallHit.point.x - (dashDirection * (padding + 0.1f));
        }

        // --- 2. DASH PHASE ---
        currentState = EnemyState.Dashing;
        targetVisual.localScale = originalGraphicsScale;

        while (Mathf.Abs(rb.position.x - targetDashPos.x) > 0.05f)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetDashPos, dashSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetDashPos);

        // --- 3. COOLDOWN & PATROL RESUME ---
        currentState = EnemyState.Cooldown;
        cooldownTimer = attackCooldown;

        // Set next patrol goal to whichever patrol point is ahead in the facing direction
        targetPatrolPoint = facingRight ? rightPatrolPoint : leftPatrolPoint;
        currentState = EnemyState.Patrol;
    }

    private void SetFacingDirection(bool faceRight)
    {
        facingRight = faceRight;

        // Flip the Face child object's X scale between 1 and -1
        if (faceTransform != null)
        {
            Vector3 faceScale = faceTransform.localScale;
            faceScale.x = facingRight ? 1f : -1f;
            faceTransform.localScale = faceScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw Patrol Bounds
        Vector2 center = Application.isPlaying ? startPoint : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + Vector2.left * patrolDistance, center + Vector2.right * patrolDistance);

        // Draw Directional Detection Box
        Gizmos.color = Color.red;
        float dir = facingRight ? 1f : -1f;
        Vector2 boxCenter = (Vector2)transform.position + (Vector2.right * (detectionRange / 2f) * dir);
        Gizmos.DrawWireCube(boxCenter, new Vector2(detectionRange, detectionHeight));
    }
}