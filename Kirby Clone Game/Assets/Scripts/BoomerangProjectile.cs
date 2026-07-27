using UnityEngine;

public class BoomerangProjectile : MonoBehaviour
{
    [Header("Boomerang Settings")]
    [SerializeField] private float travelDistance = 7f;   // Max distance before coming back
    [SerializeField] private float speed = 12f;
    [SerializeField] private float rotationSpeed = 720f;  // Spin visual effect

    private Transform ownerEnemy;
    private Vector2 startPos;
    private Vector2 targetPos;
    private bool returning = false;

    public void Initialize(Transform enemy, Vector2 direction)
    {
        ownerEnemy = enemy;
        startPos = transform.position;

        // Calculate max forward point
        targetPos = startPos + (direction.normalized * travelDistance);
    }

    private void Update()
    {
        // Spin the boomerang graphics
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        if (!returning)
        {
            // Move Outward
            transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            // Turn around when max distance reached
            if (Vector2.Distance(transform.position, targetPos) < 0.1f)
            {
                returning = true;
            }
        }
        else
        {
            // If the enemy object was destroyed while boomerang was out, destroy boomerang
            if (ownerEnemy == null)
            {
                Destroy(gameObject);
                return;
            }

            // Move Back to Enemy
            transform.position = Vector2.MoveTowards(transform.position, ownerEnemy.position, speed * Time.deltaTime);

            // Destroy when returned to enemy
            if (Vector2.Distance(transform.position, ownerEnemy.position) < 0.2f)
            {
                Destroy(gameObject);
            }
        }
    }
}