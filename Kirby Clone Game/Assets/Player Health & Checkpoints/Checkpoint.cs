using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Config")]
    [SerializeField] private int checkpointID = 1;          // Set unique ID in Inspector
    [SerializeField] private Transform spawnPoint;           // Where player spawns (defaults to this transform)

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private Sprite activeSprite;            // E.g., Flag raised or lamp lit
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool isActivated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Register this checkpoint with the manager
            Vector2 respawnPosition = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
            bool newlyActivated = CheckpointManager.Instance.SetCurrentCheckpoint(checkpointID, respawnPosition);

            if (newlyActivated)
            {
                ActivateCheckpoint();
            }
        }
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;

        if (spriteRenderer != null && activeSprite != null)
        {
            spriteRenderer.sprite = activeSprite;
        }

        Debug.Log($"Checkpoint {checkpointID} Activated!");
    }
}