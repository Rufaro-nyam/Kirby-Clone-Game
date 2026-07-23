using System.Collections;
using UnityEngine;

public class EnemyDamageDealer : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private LayerMask playerLayer;

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

        // Check if collision object is on the Player layer
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            Vector2 hitPoint = collision.contacts[0].point;
            TriggerHitImpact(hitPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canDamage) return;

        // Check if trigger object is on the Player layer
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            TriggerHitImpact(hitPoint);
        }
    }

    private void TriggerHitImpact(Vector2 hitPoint)
    {
        // 1. Spawn Impact VFX at contact point
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            
        }

        // 2. Freeze Frame (Hit Stop)
        StartCoroutine(FrameFreeze(freezeDuration));

        // 3. Camera Shake
        if (mainCamera != null)
        {
            StartCoroutine(CameraShake(shakeDuration, shakeMagnitude));
        }

        // 4. Start local damage cooldown so it doesn't trigger every physics tick
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator FrameFreeze(float duration)
    {
        Time.timeScale = 0f;
        // Uses unscaled time so the yield finishes even though Time.timeScale is 0
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

            elapsed += Time.unscaledDeltaTime; // Unscaled delta time works during frame freeze!
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