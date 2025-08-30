using UnityEngine;
using UnityEngine.UI;

public class PlayerDashUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image cooldownFillImage; // The UI Image (set to Filled)
    [SerializeField] private PlayerDash playerDash; // Drag in the PlayerDash component
    
    [Header("Settings")]
    [SerializeField] private float updateSpeed = 5f; // Lerp speed for smoothness
    [SerializeField] private bool validateValues = true; // Safety validation
    
    private float targetFill = 1f; // 1 = Ready, 0 = Cooling
    private float lastValidProgress = 1f; // Cache last valid value
    
    void Awake()
    {
        if (!playerDash) playerDash = GetComponentInParent<PlayerDash>();
        
        // Validate UI references
        if (cooldownFillImage == null)
        {
            Debug.LogWarning("PlayerDashUI: No cooldownFillImage assigned!");
        }
    }
    
    void OnEnable()
    {
        GameEvents.OnDashCooldownUpdate += OnCooldownUpdate;
        
        if (playerDash != null)
        {
            OnCooldownUpdate(playerDash.DashCooldownProgress);
        }
    }
    
    void OnDisable()
    {
        GameEvents.OnDashCooldownUpdate -= OnCooldownUpdate;
    }
    
    void Update()
    {
        if (cooldownFillImage != null)
        {
            // Use safe lerp with validation
            float currentFill = cooldownFillImage.fillAmount;
            float newFill = Mathf.Lerp(currentFill, targetFill, updateSpeed * Time.deltaTime);
            
            // Validate the new fill value before applying
            if (IsValidFillAmount(newFill))
            {
                cooldownFillImage.fillAmount = newFill;
            }
            else
            {
                // Use last known good value and log warning
                cooldownFillImage.fillAmount = lastValidProgress;
                if (validateValues)
                {
                    Debug.LogWarning($"PlayerDashUI: Invalid fill value {newFill}, using cached {lastValidProgress}");
                }
            }
        }
    }
    
    private void OnCooldownUpdate(float progress)
    {
        // Validate incoming progress value
        if (IsValidProgress(progress))
        {
            targetFill = progress;
            lastValidProgress = progress; // Cache valid value
        }
        else
        {
            if (validateValues)
            {
                Debug.LogWarning($"PlayerDashUI: Invalid progress value {progress}, ignoring update");
            }
            // Keep using the last valid target
        }
    }
    
    /// <summary>
    /// Validates that a progress value is within acceptable bounds
    /// </summary>
    private bool IsValidProgress(float progress)
    {
        return !float.IsNaN(progress) && 
               !float.IsInfinity(progress) && 
               progress >= -0.01f && 
               progress <= 1.01f; // Small tolerance for floating point precision
    }
    
    /// <summary>
    /// Validates that a fill amount is safe for UI rendering
    /// </summary>
    private bool IsValidFillAmount(float fillAmount)
    {
        return !float.IsNaN(fillAmount) && 
               !float.IsInfinity(fillAmount) && 
               fillAmount >= 0f && 
               fillAmount <= 1f;
    }
    
    /// <summary>
    /// Force update the UI with a specific value (useful for PowerUps)
    /// </summary>
    public void ForceUpdate(float progress)
    {
        if (IsValidProgress(progress))
        {
            targetFill = progress;
            lastValidProgress = progress;
            
            // Apply immediately without lerp for instant feedback
            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = progress;
            }
        }
    }
    
    /// <summary>
    /// Reset UI to ready state (useful when PowerUps provide instant charges)
    /// </summary>
    public void SetReady()
    {
        ForceUpdate(1f);
    }
    
    /// <summary>
    /// Manual refresh from PlayerDash component
    /// </summary>
    [ContextMenu("Refresh from PlayerDash")]
    public void RefreshFromPlayerDash()
    {
        if (playerDash != null)
        {
            OnCooldownUpdate(playerDash.DashCooldownProgress);
        }
    }
}