using System.Collections;
using UnityEngine;

public class EnemyDamageDealer : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float upwardForce = 4f; // Gives a nice subtle arc away from hit

    [Header("Hit Impact Settings")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float freezeDuration = 0.08f; // Hit stop (~80ms)
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.3f;

    [Header("Cooldown / i-Frames")]
    [SerializeField] private float hitCooldown = 1.0f;
    private bool canDamage = true;

    private Transform mainCamera;
    private Vector3 originalCamPos;

    private void Awake()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canDamage) return;

        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            Vector2 hitPoint = collision.contacts[0].point;
            ApplyDamageAndImpact(collision.gameObject, hitPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canDamage) return;

        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            ApplyDamageAndImpact(other.gameObject, hitPoint);
        }
    }

    private void ApplyDamageAndImpact(GameObject playerObj, Vector2 hitPoint)
    {
        // 1. Trigger Knockback through Player Script
        PlayerMovement2D playerMovement = playerObj.GetComponent<PlayerMovement2D>();
        if (playerMovement != null)
        {
            Vector2 pushDirection = (playerObj.transform.position - transform.position).normalized;

            if (Mathf.Abs(pushDirection.x) < 0.1f)
            {
                pushDirection.x = transform.position.x < playerObj.transform.position.x ? 1f : -1f;
            }

            Vector2 knockbackImpulse = new Vector2(pushDirection.x * knockbackForce, upwardForce);

            // Pass the knockback force AND lockout duration (e.g., 0.2 seconds)
            playerMovement.ApplyKnockback(knockbackImpulse, 0.2f);
        }

        // 2. Spawn Impact VFX at contact point
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        }

        // 3. Freeze Frame (Hit Stop)
        StartCoroutine(FrameFreeze(freezeDuration));

        // 4. Camera Shake
        if (mainCamera != null)
        {
            StartCoroutine(CameraShake(shakeDuration, shakeMagnitude));
        }

        // 5. Cooldown trigger
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator FrameFreeze(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        originalCamPos = mainCamera.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            mainCamera.localPosition = new Vector3(originalCamPos.x + x, originalCamPos.y + y, originalCamPos.z);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCamera.localPosition = originalCamPos;
    }

    private IEnumerator CooldownRoutine()
    {
        canDamage = false;
        yield return new WaitForSeconds(hitCooldown);
        canDamage = true;
    }
}