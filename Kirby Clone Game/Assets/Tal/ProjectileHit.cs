using UnityEngine;

public class ProjectileHit : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // Don't destroy the arrow if it touches Kirby's own colliders
        if (other.CompareTag("Player"))
        {
            return;
        }

        // Destroy the arrow when it touches anything else (walls, floors, enemies)
        Destroy(gameObject);
    }
}