using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Info")]
    [SerializeField] private string enemyName = "Waddle Dee";
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Engagement Range")]
    [SerializeField] private float engagementDistance = 8f;
    [SerializeField] private LayerMask playerLayer;

    [Header("UI Prefab & Canvas")]
    [SerializeField] private GameObject enemyHudPrefab; // Your bottom-left HUD prefab

    private GameObject activeHudInstance;
    private EnemyHUD activeHudScript;
    private Canvas mainCanvas;
    private Transform playerTransform;
    private bool playerInRange = false;

    private void Awake()
    {
        currentHealth = maxHealth;

        // Locate main UI Canvas automatically in scene
        mainCanvas = FindFirstObjectByType<Canvas>();
    }

    private void Update()
    {
        CheckPlayerDistance();
        if (Input.GetKeyDown(KeyCode.E))
        {
            TakeDamage(20);
        }
    }

    private void CheckPlayerDistance()
    {
        // Search for player if reference is lost
        if (playerTransform == null)
        {
            Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, engagementDistance, playerLayer);
            if (playerCollider != null)
            {
                playerTransform = playerCollider.transform;
            }
            else
            {
                if (playerInRange) RemoveHUD();
                return;
            }
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= engagementDistance)
        {
            if (!playerInRange)
            {
                SpawnHUD();
            }
        }
        else
        {
            if (playerInRange)
            {
                RemoveHUD();
            }
        }
    }

    private void SpawnHUD()
    {
        if (enemyHudPrefab == null || mainCanvas == null) return;

        playerInRange = true;

        // Instantiate inside Canvas so UI layout anchors work
        activeHudInstance = Instantiate(enemyHudPrefab, mainCanvas.transform);
        activeHudScript = activeHudInstance.GetComponent<EnemyHUD>();

        if (activeHudScript != null)
        {
            activeHudScript.Setup(enemyName, currentHealth, maxHealth);
        }
    }

    private void RemoveHUD()
    {
        playerInRange = false;
        if (activeHudInstance != null)
        {
            Destroy(activeHudInstance);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        // Update active UI
        if (activeHudScript != null)
        {
            activeHudScript.UpdateHealth(currentHealth, maxHealth);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        RemoveHUD();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Cleanup UI if enemy gets destroyed by other means
        RemoveHUD();
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmo for inspecting engagement distance
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, engagementDistance);
    }
}