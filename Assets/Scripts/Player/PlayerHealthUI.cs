using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;   // Assign PlayerHealth component
    [SerializeField] private Image healthFillImage;       // The UI Image (set to Filled)
    [SerializeField] private TextMeshProUGUI healthText;  // The UI text
   
    [Header("Settings")]
    [SerializeField] private float updateSpeed = 5f;      // Higher = faster animation
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0); // Offset above player
    
    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;   // > 50%
    [SerializeField] private Color warningColor = Color.yellow;  // 15%–50%
    [SerializeField] private Color criticalColor = Color.red;    // < 15%

    private float targetFill = 1f;

    void Awake()
    {
        if (!playerHealth) playerHealth = GetComponentInParent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            OnHealthChanged(playerHealth.CurrentHealth);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnHealthChanged;
    }

    void LateUpdate()
    {
        if (healthFillImage != null)
        {
            // Smoothly animate fill
            healthFillImage.fillAmount = Mathf.Lerp(
                healthFillImage.fillAmount, targetFill, updateSpeed * Time.deltaTime
            );
        }
    }

    private void OnHealthChanged(int currentHealth)
    {
        if (playerHealth == null) return;

        targetFill = (float)currentHealth / playerHealth.MaxHealth;

        if (healthText)
            healthText.text = $"{currentHealth}/{playerHealth.MaxHealth}";

        UpdateBarColor(targetFill);
    }

    private void UpdateBarColor(float normalizedHealth)
    {
        if (normalizedHealth <= 0.30f)
            healthFillImage.color = criticalColor; // Critical
        else if (normalizedHealth <= 0.75f)
            healthFillImage.color = warningColor;  // Warning
        else
            healthFillImage.color = healthyColor;  // Healthy
    }
}
