using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Simple UI component to display score information in a clean, animated format
/// Works in conjunction with SurvivalScoreSystem
/// </summary>
public class ScoreDisplayUI : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI mainScoreText;
    [SerializeField] private TextMeshProUGUI timeAliveText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private TextMeshProUGUI bonusScoreText; // Shows temporary bonus notifications
    
    [Header("Display Settings")]
    [SerializeField] private bool showLeadingZeros = true;
    [SerializeField] private bool showDecimalMultiplier = true;
    [SerializeField] private float bonusTextDuration = 2f;
    
    [Header("Animation Settings")]
    [SerializeField] private float counterAnimationSpeed = 500f; // Points per second when animating
    [SerializeField] private float scaleBounceDuration = 0.3f;
    [SerializeField] private float scaleIntensity = 1.15f;
    
    [Header("Colors")]
    [SerializeField] private Color normalScoreColor = Color.white;
    [SerializeField] private Color highlightScoreColor = Color.yellow;
    [SerializeField] private Color timeColor = new Color(0.8f, 0.8f, 1f, 1f); // Light blue
    [SerializeField] private Color multiplierColor = new Color(1f, 0.6f, 0.2f, 1f); // Orange
    [SerializeField] private Color bonusColor = new Color(0.2f, 1f, 0.2f, 1f); // Green
    
    // Internal state
    private int displayedScore = 0;
    private int targetScore = 0;
    private Coroutine scoreCounterCoroutine;
    private Coroutine bonusTextCoroutine;
    
    void Start()
    {
        InitializeDisplay();
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeDisplay()
    {
        // Set initial text values and colors
        if (mainScoreText)
        {
            mainScoreText.text = FormatScore(0);
            mainScoreText.color = normalScoreColor;
        }
        
        if (timeAliveText)
        {
            timeAliveText.text = "0:00";
            timeAliveText.color = timeColor;
        }
        
        if (multiplierText)
        {
            multiplierText.text = "x1.0";
            multiplierText.color = multiplierColor;
        }
        
        if (bonusScoreText)
        {
            bonusScoreText.gameObject.SetActive(false);
        }
    }
    
    private void SubscribeToEvents()
    {
        GameEvents.OnScoreAdded += OnScoreAdded;
        GameEvents.OnScoreUpdated += OnScoreUpdated;
        GameEvents.OnSurvivalTimeUpdated += OnSurvivalTimeUpdated;
        GameEvents.OnMultiplierChanged += OnMultiplierChanged;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnScoreAdded -= OnScoreAdded;
        GameEvents.OnScoreUpdated -= OnScoreUpdated;
        GameEvents.OnSurvivalTimeUpdated -= OnSurvivalTimeUpdated;
        GameEvents.OnMultiplierChanged -= OnMultiplierChanged;
    }
    
    #region Event Handlers
    
    private void OnScoreAdded(int pointsAdded, string reason)
    {
        // Show bonus text for score additions
        ShowBonusText($"+{pointsAdded}", reason);
        
        // Trigger scale animation
        if (mainScoreText)
        {
            StartCoroutine(ScaleBounceAnimation(mainScoreText.transform));
        }
    }
    
    private void OnScoreUpdated(int newScore)
    {
        targetScore = newScore;
        
        // Animate score counter if there's a significant difference
        if (Mathf.Abs(newScore - displayedScore) > 10)
        {
            if (scoreCounterCoroutine != null)
                StopCoroutine(scoreCounterCoroutine);
            scoreCounterCoroutine = StartCoroutine(AnimateScoreCounter());
        }
        else
        {
            // Small changes, update immediately
            displayedScore = newScore;
            UpdateScoreText();
        }
    }
    
    private void OnSurvivalTimeUpdated(float survivalTime)
    {
        if (timeAliveText)
        {
            timeAliveText.text = FormatTime(survivalTime);
        }
    }
    
    private void OnMultiplierChanged(float newMultiplier)
    {
        if (multiplierText)
        {
            string multiplierFormat = showDecimalMultiplier ? "F1" : "F0";
            multiplierText.text = $"x{newMultiplier.ToString(multiplierFormat)}";
            
            // Scale bounce for multiplier changes
            StartCoroutine(ScaleBounceAnimation(multiplierText.transform));
        }
    }
    
    #endregion
    
    #region Display Formatting
    
    private string FormatScore(int score)
    {
        if (showLeadingZeros && score < 100000)
        {
            return score.ToString("D6"); // 6-digit format with leading zeros
        }
        else
        {
            return score.ToString("N0"); // Thousands separator format
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes}:{seconds:D2}";
    }
    
    #endregion
    
    #region Animations
    
    private IEnumerator AnimateScoreCounter()
    {
        int startScore = displayedScore;
        float animationDuration = Mathf.Abs(targetScore - startScore) / counterAnimationSpeed;
        animationDuration = Mathf.Clamp(animationDuration, 0.1f, 2f); // Reasonable bounds
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            
            // Use smooth easing for the counter animation
            float easedProgress = EaseOutQuad(progress);
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, easedProgress));
            
            UpdateScoreText();
            yield return null;
        }
        
        displayedScore = targetScore;
        UpdateScoreText();
        scoreCounterCoroutine = null;
    }
    
    private IEnumerator ScaleBounceAnimation(Transform targetTransform)
    {
        if (!targetTransform) yield break;
        
        Vector3 originalScale = targetTransform.localScale;
        
        float elapsed = 0f;
        while (elapsed < scaleBounceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / scaleBounceDuration;
            
            // Bounce effect using sine wave
            float scaleMultiplier = 1f + (Mathf.Sin(progress * Mathf.PI) * (scaleIntensity - 1f));
            targetTransform.localScale = originalScale * scaleMultiplier;
            
            yield return null;
        }
        
        targetTransform.localScale = originalScale;
    }
    
    private void ShowBonusText(string bonusAmount, string reason)
    {
        if (!bonusScoreText) return;
        
        // Stop any existing bonus text animation
        if (bonusTextCoroutine != null)
            StopCoroutine(bonusTextCoroutine);
            
        bonusTextCoroutine = StartCoroutine(BonusTextAnimation(bonusAmount, reason));
    }
    
    private IEnumerator BonusTextAnimation(string bonusAmount, string reason)
    {
        bonusScoreText.gameObject.SetActive(true);
        bonusScoreText.text = $"{bonusAmount} ({reason})";
        bonusScoreText.color = bonusColor;
        
        Transform textTransform = bonusScoreText.transform;
        Vector3 originalScale = textTransform.localScale;
        Vector3 originalPosition = textTransform.localPosition;
        
        // Phase 1: Scale up and move up slightly
        float phase1Duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / phase1Duration;
            
            float scale = Mathf.Lerp(0.5f, 1.2f, EaseOutBack(progress));
            textTransform.localScale = originalScale * scale;
            
            float moveY = Mathf.Lerp(0f, 20f, progress);
            textTransform.localPosition = originalPosition + Vector3.up * moveY;
            
            yield return null;
        }
        
        // Phase 2: Hold and fade
        float holdDuration = bonusTextDuration - 0.8f;
        yield return new WaitForSeconds(holdDuration);
        
        // Phase 3: Fade out
        float fadeOutDuration = 0.5f;
        Color startColor = bonusScoreText.color;
        elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            
            Color fadeColor = startColor;
            fadeColor.a = Mathf.Lerp(1f, 0f, progress);
            bonusScoreText.color = fadeColor;
            
            float scale = Mathf.Lerp(1.2f, 0.8f, progress);
            textTransform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        // Reset and hide
        bonusScoreText.gameObject.SetActive(false);
        textTransform.localScale = originalScale;
        textTransform.localPosition = originalPosition;
        bonusTextCoroutine = null;
    }
    
    #endregion
    
    #region Utility Methods
    
    private void UpdateScoreText()
    {
        if (mainScoreText)
        {
            mainScoreText.text = FormatScore(displayedScore);
        }
    }
    
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
    
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Manually update the displayed score (useful for initialization)
    /// </summary>
    public void SetScore(int score, bool animate = false)
    {
        if (animate)
        {
            targetScore = score;
            OnScoreUpdated(score);
        }
        else
        {
            displayedScore = score;
            targetScore = score;
            UpdateScoreText();
        }
    }
    
    /// <summary>
    /// Force an immediate update of all display elements
    /// </summary>
    public void RefreshDisplay(int score, float time, float multiplier)
    {
        SetScore(score, false);
        OnSurvivalTimeUpdated(time);
        OnMultiplierChanged(multiplier);
    }
    
    #endregion
}