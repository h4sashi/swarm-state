using UnityEngine;
using UnityEngine.UI;

public class PlayerDashUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image cooldownFillImage;  // The UI Image (set to Filled)
    [SerializeField] private PlayerDash playerDash;    // Drag in the PlayerDash component

    [Header("Settings")]
    [SerializeField] private float updateSpeed = 5f;   // Lerp speed for smoothness

    private float targetFill = 1f; // 1 = Ready, 0 = Cooling

    void Awake()
    {
        if (!playerDash) playerDash = GetComponentInParent<PlayerDash>();
    }

    void OnEnable()
    {
        GameEvents.OnDashCooldownUpdate += OnCooldownUpdate;
        if (playerDash != null)
            OnCooldownUpdate(playerDash.DashCooldownProgress);
    }

    void OnDisable()
    {
        GameEvents.OnDashCooldownUpdate -= OnCooldownUpdate;
    }

    void Update()
    {
        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = Mathf.Lerp(
                cooldownFillImage.fillAmount,
                targetFill,
                updateSpeed * Time.deltaTime
            );
        }
    }

    private void OnCooldownUpdate(float progress)
    {
        // progress = 0 (just dashed) → 1 (ready)
        targetFill = progress;
    }
}
