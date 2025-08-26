using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;   // Assign PlayerHealth component
    [SerializeField] private Image healthFillImage;       // The UI Image (set to Filled)
    [SerializeField] private TextMeshProUGUI healthText;  // The UI text
    [SerializeField] private Image bloodOverlayImage;     // Fullscreen UI image with blood texture
    
    [Header("Settings")]
    [SerializeField] private float updateSpeed = 5f;      // Higher = faster animation
    [SerializeField] private float bloodFadeSpeed = 2f;   // How fast blood fades after damage
    [SerializeField] private float bloodMaxAlpha = 0.6f;  // Max opacity when hit

    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;   // > 50%
    [SerializeField] private Color warningColor = Color.yellow;  // 15%–50%
    [SerializeField] private Color criticalColor = Color.red;    // < 15%

    private float targetFill = 1f;
    private float bloodAlpha = 0f; // current blood opacity
    private int lastHealth;

    void Awake()
    {
        if (!playerHealth) playerHealth = GetComponentInParent<PlayerHealth>();
        if (bloodOverlayImage != null) bloodOverlayImage.color = new Color(1, 0, 0, 0); // start transparent
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            lastHealth = playerHealth.CurrentHealth;
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

        // Smoothly fade blood overlay
        if (bloodOverlayImage != null && bloodAlpha > 0f)
        {
            bloodAlpha = Mathf.Lerp(bloodAlpha, 0f, bloodFadeSpeed * Time.deltaTime);
            bloodOverlayImage.color = new Color(1f, 0f, 0f, bloodAlpha);
        }
    }

    private void OnHealthChanged(int currentHealth)
    {
        if (playerHealth == null) return;

        targetFill = (float)currentHealth / playerHealth.MaxHealth;

        if (healthText)
            healthText.text = $"{currentHealth}/{playerHealth.MaxHealth}";

        UpdateBarColor(targetFill);

        // Trigger blood splash if player lost health
        if (currentHealth < lastHealth)
        {
            bloodAlpha = bloodMaxAlpha; // flash red overlay
            if (bloodOverlayImage != null)
                bloodOverlayImage.color = new Color(1f, 0f, 0f, bloodAlpha);
        }

        lastHealth = currentHealth;
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
