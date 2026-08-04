using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Image healthBarFill;

    public void Setup(string enemyName, float currentHealth, float maxHealth)
    {
        if (enemyNameText != null)
        {
            enemyNameText.text = enemyName;
        }

        UpdateHealth(currentHealth, maxHealth);
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }
}