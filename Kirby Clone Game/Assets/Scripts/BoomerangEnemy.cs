using System.Collections;
using UnityEngine;

public class BoomerangEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform faceTransform;     // Face object that flips (X: 1 or -1)
    [SerializeField] private Transform throwPoint;        // Where the boomerang spawns
    [SerializeField] private GameObject boomerangPrefab;  // Boomerang prefab to instantiate

    [Header("Scanning / Idle")]
    [SerializeField] private float scanInterval = 2.0f;    // How often it switches looking left/right

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Attack & Cooldown")]
    [SerializeField] private float attackCooldown = 2.5f;

    private bool facingRight = true;
    private bool isAttacking = false;
    private float scanTimer = 0f;
    private float cooldownTimer = 0f;

    private void Start()
    {
        SetFacingDirection(facingRight);
    }

    private void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (isAttacking) return;

        // Idle behavior: Scan back and forth
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            SetFacingDirection(!facingRight);
        }

        // Raycast detection
        if (cooldownTimer <= 0)
        {
            CheckForPlayer();
        }
    }

    private void CheckForPlayer()
    {
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        // Raycast straight ahead to look for the player
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, detectionDistance, playerLayer | groundLayer);

        // Check if we hit the player (and wall didn't block line of sight)
        if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
        {
            StartCoroutine(ThrowSequence(hit.transform));
        }
    }

    private IEnumerator ThrowSequence(Transform playerTransform)
    {
        isAttacking = true;

        // Spawn Boomerang
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position;
        GameObject boomerangObj = Instantiate(boomerangPrefab, spawnPos, Quaternion.identity);

        BoomerangProjectile boomerang = boomerangObj.GetComponent<BoomerangProjectile>();
        if (boomerang != null)
        {
            // Calculate throw target (towards player direction)
            Vector2 throwDir = facingRight ? Vector2.right : Vector2.left;

            // Pass enemy transform so boomerang can return even if enemy moves
            boomerang.Initialize(transform, throwDir);
        }

        // Wait until boomerang returns or finishes before starting cooldown
        while (boomerangObj != null)
        {
            yield return null;
        }

        // Start cooldown after boomerang returns
        cooldownTimer = attackCooldown;
        isAttacking = false;
    }

    private void SetFacingDirection(bool faceRight)
    {
        facingRight = faceRight;

        if (faceTransform != null)
        {
            Vector3 scale = faceTransform.localScale;
            scale.x = facingRight ? 1f : -1f;
            faceTransform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection raycast in editor
        Gizmos.color = Color.red;
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        Gizmos.DrawRay(transform.position, dir * detectionDistance);
    }
}