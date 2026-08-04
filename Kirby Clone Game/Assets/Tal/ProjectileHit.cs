using UnityEngine;

public class ProjectileHit : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore Kirby
        if (other.CompareTag("Player"))
        {
            return;
        }

        // Check if the thing we hit is an enemy
        InhalableEnemy enemy = other.GetComponent<InhalableEnemy>();
        if (enemy != null)
        {
            // We hit an enemy! Destroy it.
            Destroy(other.gameObject);
        }

        // Destroy the projectile (whether it hit an enemy, a wall, or the floor)
        Destroy(gameObject);
    }
}