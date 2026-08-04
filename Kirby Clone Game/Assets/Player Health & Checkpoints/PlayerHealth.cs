using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("UI References")]
    [SerializeField] private Image[] healthBars;

    [Header("i-Frames (Invulnerability)")]
    [SerializeField] private float invincibilityDuration = 1.0f;
    [SerializeField] private SpriteRenderer playerSprite;
    private bool isInvincible = false;

    private Vector2 initialSpawnPosition;
    private Rigidbody2D rb;
    private KirbyController kirbyController; // Added reference to KirbyController

    private void Awake()
    {
        currentHealth = maxHealth;
        initialSpawnPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        kirbyController = GetComponent<KirbyController>(); // Get the controller on awake
        UpdateUI();
    }

    public void TakeDamage(int damageAmount = 1)
    {
        // 1. Check if we should block damage (i-frames, dead, or INHALING)
        if (isInvincible || currentHealth <= 0 || (kirbyController != null && kirbyController.IsInhaling))
        {
            return;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        UpdateUI();

        // --- LOSE ABILITY LOGIC ---
        if (kirbyController != null && kirbyController.currentEquippedAbility != CopyAbility.None)
        {
            kirbyController.currentEquippedAbility = CopyAbility.None;
            kirbyController.abilityUIText.text = "None";
            kirbyController.abilityUISprite.sprite = kirbyController.noneIMG;
            Debug.Log("Kirby took damage and lost his ability!");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private void Die()
    {
        Debug.Log("Player died! Respawning at checkpoint...");
        Respawn();
    }

    public void Respawn()
    {
        // 1. Reset Health & UI
        currentHealth = maxHealth;
        UpdateUI();

        // 2. Stop any remaining momentum/velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Reset ability on death too, just in case
        if (kirbyController != null)
        {
            kirbyController.currentEquippedAbility = CopyAbility.None;
            kirbyController.abilityUIText.text = "None";
            kirbyController.abilityUISprite.sprite = kirbyController.noneIMG;
        }

        // 3. Teleport to Checkpoint (or initial spawn position if no checkpoint hit yet)
        Vector2 targetSpawnPos = CheckpointManager.Instance != null
            ? CheckpointManager.Instance.GetRespawnPosition(initialSpawnPosition)
            : initialSpawnPosition;

        transform.position = targetSpawnPos;

        // 4. Brief invincibility after respawn to prevent immediate re-death
        StartCoroutine(InvincibilityRoutine());
    }

    private void UpdateUI()
    {
        if (healthBars == null || healthBars.Length == 0) return;

        for (int i = 0; i < healthBars.Length; i++)
        {
            if (healthBars[i] != null)
            {
                healthBars[i].enabled = (i < currentHealth);
            }
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        float flashInterval = 0.1f;

        while (elapsed < invincibilityDuration)
        {
            if (playerSprite != null)
            {
                playerSprite.enabled = !playerSprite.enabled;
            }
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }

        isInvincible = false;
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }

    // Call this when starting a dash
    public void TriggerDashInvincibility(float duration)
    {
        StartCoroutine(DashInvincibilityRoutine(duration));
    }

    private IEnumerator DashInvincibilityRoutine(float duration)
    {
        isInvincible = true;

        float elapsed = 0f;
        float flashInterval = 0.05f; // Fast flashing for high speed dash

        while (elapsed < duration)
        {
            if (playerSprite != null)
            {
                playerSprite.enabled = !playerSprite.enabled;
            }
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        // Ensure sprite is restored and invincibility is turned off
        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }

        isInvincible = false;
    }
}