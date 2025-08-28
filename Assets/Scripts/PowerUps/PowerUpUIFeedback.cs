using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PowerUpFeedbackUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI speedBoostText;
    [SerializeField] private TextMeshProUGUI dashBoostText;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float pulseIntensity = 1.2f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float stackBounceScale = 1.5f;
    [SerializeField] private float stackBounceDuration = 0.4f;
    
    [Header("Visual Configuration")]
    [SerializeField] private Color speedBoostColor = new Color(1f, 0.8f, 0f, 1f); // Gold
    [SerializeField] private Color dashBoostColor = new Color(0f, 0.8f, 1f, 1f);  // Cyan
    [SerializeField] private Color stackColor = new Color(1f, 0.4f, 0f, 1f);     // Orange for stacks
    
    // Internal state tracking
    private Dictionary<PowerUpType, PowerUpUIState> activeStates = new Dictionary<PowerUpType, PowerUpUIState>();
    private Dictionary<PowerUpType, Coroutine> activeAnimations = new Dictionary<PowerUpType, Coroutine>();
    
    // UI state data structure
    private class PowerUpUIState
    {
        public TextMeshProUGUI textComponent;
        public int stackCount;
        public float remainingTime;
        public bool isActive;
        public Color baseColor;
        public Vector3 originalScale;
        public Color originalColor;
        
        public PowerUpUIState(TextMeshProUGUI text, Color color)
        {
            textComponent = text;
            baseColor = color;
            originalScale = text.transform.localScale;
            originalColor = text.color;
            stackCount = 0;
            remainingTime = 0f;
            isActive = false;
        }
    }
    
    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeUI()
    {
        // Initialize UI states
        if (speedBoostText != null)
        {
            activeStates[PowerUpType.SpeedBoost] = new PowerUpUIState(speedBoostText, speedBoostColor);
            speedBoostText.gameObject.SetActive(false);
        }
        
        if (dashBoostText != null)
        {
            activeStates[PowerUpType.DoubleDash] = new PowerUpUIState(dashBoostText, dashBoostColor);
            dashBoostText.gameObject.SetActive(false);
        }
        
        Debug.Log("PowerUpFeedbackUI: Initialized with Speed and Dash text components");
    }
    
    private void SubscribeToEvents()
    {
        GameEvents.OnPowerUpStateChanged += OnPowerUpStateChanged;
        GameEvents.OnPowerUpStacked += OnPowerUpStacked;
        GameEvents.OnPowerUpTimeRemaining += OnPowerUpTimeUpdate;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnPowerUpStateChanged -= OnPowerUpStateChanged;
        GameEvents.OnPowerUpStacked -= OnPowerUpStacked;
        GameEvents.OnPowerUpTimeRemaining -= OnPowerUpTimeUpdate;
    }
    
    private void OnPowerUpStateChanged(PowerUpType type, bool activated)
    {
        if (!activeStates.ContainsKey(type)) return;
        
        var state = activeStates[type];
        
        if (activated)
        {
            ActivatePowerUpDisplay(type, state);
        }
        else
        {
            DeactivatePowerUpDisplay(type, state);
        }
    }
    
    private void OnPowerUpStacked(PowerUpType type, int stackCount)
    {
        if (!activeStates.ContainsKey(type)) return;
        
        var state = activeStates[type];
        state.stackCount = stackCount;
        
        // Trigger stack animation
        StartCoroutineWithCleanup(type, StackBounceAnimation(state));
        
        Debug.Log($"PowerUpFeedbackUI: {type} stacked to {stackCount}");
    }
    
    private void OnPowerUpTimeUpdate(PowerUpType type, float timeRemaining)
    {
        if (!activeStates.ContainsKey(type)) return;
        
        var state = activeStates[type];
        state.remainingTime = timeRemaining;
        
        // Update display if active
        if (state.isActive)
        {
            UpdatePowerUpText(state);
        }
    }
    
    private void ActivatePowerUpDisplay(PowerUpType type, PowerUpUIState state)
    {
        state.isActive = true;
        state.textComponent.gameObject.SetActive(true);
        
        // Initialize stack count to 1 for new activation
        state.stackCount = Mathf.Max(1, state.stackCount);
        
        // Start fade-in and pulse animation
        StartCoroutineWithCleanup(type, FadeInAnimation(state));
        
        Debug.Log($"PowerUpFeedbackUI: Activated display for {type}");
    }
    
    private void DeactivatePowerUpDisplay(PowerUpType type, PowerUpUIState state)
    {
        state.isActive = false;
        state.stackCount = 0;
        state.remainingTime = 0f;
        
        // Start fade-out animation
        StartCoroutineWithCleanup(type, FadeOutAnimation(state));
        
        Debug.Log($"PowerUpFeedbackUI: Deactivated display for {type}");
    }
    
    private void StartCoroutineWithCleanup(PowerUpType type, IEnumerator routine)
    {
        // Stop existing animation if running
        if (activeAnimations.ContainsKey(type) && activeAnimations[type] != null)
        {
            StopCoroutine(activeAnimations[type]);
        }
        
        // Start new animation
        activeAnimations[type] = StartCoroutine(routine);
    }
    
    private void UpdatePowerUpText(PowerUpUIState state)
    {
        if (state.textComponent == null) return;
        
        string powerUpName = GetPowerUpDisplayName(state);
        string stackDisplay = state.stackCount > 1 ? $" x{state.stackCount}" : "";
        string timeDisplay = state.remainingTime > 0 ? $" ({state.remainingTime:F1}s)" : "";
        
        state.textComponent.text = $"{powerUpName}{stackDisplay}{timeDisplay}";
    }
    
    private string GetPowerUpDisplayName(PowerUpUIState state)
    {
        if (state.textComponent == speedBoostText) return "SPEED BOOST";
        if (state.textComponent == dashBoostText) return "DASH BOOST";
        return "POWER UP";
    }
    
    #region Animation Coroutines
    
    private IEnumerator FadeInAnimation(PowerUpUIState state)
    {
        var text = state.textComponent;
        var transform = text.transform;
        
        // Reset initial state
        text.color = new Color(state.baseColor.r, state.baseColor.g, state.baseColor.b, 0f);
        transform.localScale = state.originalScale * 0.5f;
        
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            
            // Smooth fade-in curve
            float easedProgress = EaseOutBack(progress);
            
            // Fade alpha
            Color color = state.baseColor;
            color.a = easedProgress;
            text.color = color;
            
            // Scale up with overshoot
            float scale = Mathf.Lerp(0.5f, 1f, easedProgress);
            transform.localScale = state.originalScale * scale;
            
            // Update text content
            UpdatePowerUpText(state);
            
            yield return null;
        }
        
        // Ensure final state
        text.color = state.baseColor;
        transform.localScale = state.originalScale;
        
        // Start continuous pulse animation
        activeAnimations[GetPowerUpType(state)] = StartCoroutine(PulseAnimation(state));
    }
    
    private IEnumerator FadeOutAnimation(PowerUpUIState state)
    {
        var text = state.textComponent;
        var transform = text.transform;
        
        Color startColor = text.color;
        Vector3 startScale = transform.localScale;
        
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            
            // Smooth fade-out curve
            float easedProgress = EaseInQuad(progress);
            
            // Fade out and scale down
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, easedProgress);
            text.color = color;
            
            float scale = Mathf.Lerp(1f, 0.8f, easedProgress);
            transform.localScale = state.originalScale * scale;
            
            yield return null;
        }
        
        // Hide and reset
        text.gameObject.SetActive(false);
        text.color = state.originalColor;
        transform.localScale = state.originalScale;
    }
    
    private IEnumerator PulseAnimation(PowerUpUIState state)
    {
        var text = state.textComponent;
        var transform = text.transform;
        
        while (state.isActive)
        {
            float time = Time.time * pulseSpeed;
            
            // Pulse scale
            float pulse = 1f + (Mathf.Sin(time) * 0.1f * pulseIntensity);
            transform.localScale = state.originalScale * pulse;
            
            // Pulse color intensity
            Color baseColor = state.baseColor;
            float colorPulse = 1f + (Mathf.Sin(time * 1.5f) * 0.2f);
            Color pulseColor = new Color(
                baseColor.r * colorPulse,
                baseColor.g * colorPulse,
                baseColor.b * colorPulse,
                baseColor.a
            );
            text.color = pulseColor;
            
            // Update text content
            UpdatePowerUpText(state);
            
            // Color flash when time is running low
            if (state.remainingTime > 0 && state.remainingTime < 3f)
            {
                float urgencyFlash = Mathf.PingPong(Time.time * 4f, 1f);
                Color urgentColor = Color.Lerp(pulseColor, Color.red, urgencyFlash * 0.5f);
                text.color = urgentColor;
            }
            
            yield return null;
        }
        
        // Reset to original state
        transform.localScale = state.originalScale;
        text.color = state.baseColor;
    }
    
    private IEnumerator StackBounceAnimation(PowerUpUIState state)
    {
        var text = state.textComponent;
        var transform = text.transform;
        
        // Temporarily change color to indicate stack
        Color originalColor = text.color;
        text.color = stackColor;
        
        float elapsed = 0f;
        
        while (elapsed < stackBounceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / stackBounceDuration;
            
            // Bounce scale with elastic effect
            float bounce = EaseOutElastic(progress);
            float scale = Mathf.Lerp(stackBounceScale, 1f, bounce);
            transform.localScale = state.originalScale * scale;
            
            // Color transition back to original
            text.color = Color.Lerp(stackColor, originalColor, EaseOutQuad(progress));
            
            // Update text to show new stack count
            UpdatePowerUpText(state);
            
            yield return null;
        }
        
        // Ensure final state
        transform.localScale = state.originalScale;
        text.color = originalColor;
        
        Debug.Log($"PowerUpFeedbackUI: Stack bounce completed for stack count {state.stackCount}");
    }
    
    #endregion
    
    #region Utility Methods
    
    private PowerUpType GetPowerUpType(PowerUpUIState state)
    {
        if (state.textComponent == speedBoostText) return PowerUpType.SpeedBoost;
        if (state.textComponent == dashBoostText) return PowerUpType.DoubleDash;
        return PowerUpType.SpeedBoost; // Default fallback
    }
    
    // Custom easing functions for smooth animations
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    
    private float EaseInQuad(float t)
    {
        return t * t;
    }
    
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
    
    private float EaseOutElastic(float t)
    {
        float c4 = (2f * Mathf.PI) / 3f;
        
        return t == 0f ? 0f : t == 1f ? 1f : 
               Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
    
    #endregion
    
    #region Public API for Manual Testing
    
    [ContextMenu("Test Speed Boost Activation")]
    public void TestSpeedBoostActivation()
    {
        OnPowerUpStateChanged(PowerUpType.SpeedBoost, true);
        StartCoroutine(TestDurationCountdown(PowerUpType.SpeedBoost, 10f));
    }
    
    [ContextMenu("Test Dash Boost Stack")]
    public void TestDashBoostStack()
    {
        OnPowerUpStateChanged(PowerUpType.DoubleDash, true);
        OnPowerUpStacked(PowerUpType.DoubleDash, 2);
        StartCoroutine(TestDurationCountdown(PowerUpType.DoubleDash, 8f));
    }
    
    private IEnumerator TestDurationCountdown(PowerUpType type, float duration)
    {
        float remaining = duration;
        while (remaining > 0)
        {
            OnPowerUpTimeUpdate(type, remaining);
            remaining -= Time.deltaTime;
            yield return null;
        }
        OnPowerUpStateChanged(type, false);
    }
    
    #endregion
}