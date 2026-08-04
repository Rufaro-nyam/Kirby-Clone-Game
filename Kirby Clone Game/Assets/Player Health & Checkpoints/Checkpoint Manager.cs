using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("State")]
    private int currentCheckpointID = -1;
    private Vector2 lastRespawnPosition;
    private bool hasCheckpoint = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Updates the current checkpoint if the new ID is valid or higher.
    /// </summary>
    public bool SetCurrentCheckpoint(int id, Vector2 spawnPosition)
    {
        // Set new checkpoint if it's the first one or a higher ID
        if (!hasCheckpoint || id > currentCheckpointID)
        {
            currentCheckpointID = id;
            lastRespawnPosition = spawnPosition;
            hasCheckpoint = true;
            return true;
        }

        return false;
    }

    public Vector2 GetRespawnPosition(Vector2 defaultPosition)
    {
        return hasCheckpoint ? lastRespawnPosition : defaultPosition;
    }
}