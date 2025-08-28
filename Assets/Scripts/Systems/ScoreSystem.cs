using System.Collections;
using UnityEngine;
using TMPro;

public class ScoreSystem : MonoBehaviour
{
    [Header("Score Configuration")]
    [SerializeField] private float basePointsPerSecond = 0.5f; // Much slower - only 0.5 points per second
    [SerializeField] private bool enableMultipliers = true; // Toggle multiplier system on/off
    [SerializeField] private float difficultyMultiplierRate = 0.1f; // Increases every 10 seconds
    [SerializeField] private float difficultyMultiplierCap = 5f;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    
    [Header("Visual Effects")]
    [SerializeField] private Color normalScoreColor = Color.white;
    [SerializeField] private Color multiplierColor = Color.cyan;
    
    // Internal state
    private float survivalTime = 0f;
    private int currentScore = 0;
    private float currentMultiplier = 1f;
    private bool isGameActive = false;
    private bool playerIsDead = false;
    
    // Score accumulation
    private float scoreAccumulator = 0f;
    
    // Animation coroutines
    private Coroutine multiplierAnimationCoroutine;
    
    void Start()
    {
        InitializeScoreSystem();
        SubscribeToEvents();
        StartGame();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeScoreSystem()
    {
        survivalTime = 0f;
        currentScore = 0;
        currentMultiplier = 1f;
        isGameActive = false;
        playerIsDead = false;
        scoreAccumulator = 0f;
        
        // Initialize UI
        if (scoreText) scoreText.text = "0";
        if (timeText) timeText.text = "0:00";
        if (multiplierText) multiplierText.text = "x1.0";
        if (gameOverPanel) gameOverPanel.SetActive(false);
        
        Debug.Log("[ScoreSystem]: Initialized with slower scoring");
    }
    
    private void SubscribeToEvents()
    {
        GameEvents.OnPlayerDeath += OnPlayerDeath;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnPlayerDeath -= OnPlayerDeath;
    }
    
    void Update()
    {
        if (!isGameActive || playerIsDead) return;
        
        // Accumulate survival time
        survivalTime += Time.deltaTime;
        
        // Calculate current difficulty multiplier
        UpdateDifficultyMultiplier();
        
        // Accumulate score based on time survived (slower now)
        AccumulateTimeBasedScore();
        
        // Update UI displays
        UpdateScoreDisplay();
        UpdateTimeDisplay();
        UpdateMultiplierDisplay();
    }
    
    private void StartGame()
    {
        isGameActive = true;
        playerIsDead = false;
        Debug.Log("[ScoreSystem]: Game started - slower time-based scoring begins");
    }
    
    private void OnPlayerDeath()
    {
        if (!isGameActive) return;
        
        playerIsDead = true;
        isGameActive = false;
        
        Debug.Log($"[ScoreSystem]: Game ended - Final Score: {currentScore}, Survival Time: {FormatTime(survivalTime)}");
        
        // Show game over screen with final stats
        ShowGameOverScreen();
    }
    
    private void UpdateDifficultyMultiplier()
    {
        if (!enableMultipliers)
        {
            currentMultiplier = 1f; // Keep multiplier at 1x when disabled
            return;
        }
        
        // Increase multiplier every 10 seconds, capped at max value
        float targetMultiplier = 1f + (survivalTime * difficultyMultiplierRate / 10f);
        float newMultiplier = Mathf.Min(targetMultiplier, difficultyMultiplierCap);
        
        // Trigger animation if multiplier increased significantly
        if (newMultiplier - currentMultiplier >= 0.1f)
        {
            TriggerMultiplierAnimation();
        }
        
        currentMultiplier = newMultiplier;
    }
    
    private void AccumulateTimeBasedScore()
    {
        // Accumulate fractional score points (much slower now)
        scoreAccumulator += basePointsPerSecond * currentMultiplier * Time.deltaTime;
        
        // Convert whole points to actual score
        int pointsToAdd = Mathf.FloorToInt(scoreAccumulator);
        if (pointsToAdd > 0)
        {
            currentScore += pointsToAdd;
            scoreAccumulator -= pointsToAdd;
        }
    }
    
    #region UI Updates
    
    private void UpdateScoreDisplay()
    {
        if (scoreText)
        {
            scoreText.text = $"{currentScore:N0}";
            scoreText.color = normalScoreColor;
        }
    }
    
    private void UpdateTimeDisplay()
    {
        if (timeText)
        {
            timeText.text = $"{FormatTime(survivalTime)}";
        }
    }
    
    private void UpdateMultiplierDisplay()
    {
        if (multiplierText)
        {
            if (enableMultipliers)
            {
                multiplierText.text = $"x{currentMultiplier:F1}";
                multiplierText.color = Color.Lerp(normalScoreColor, multiplierColor, (currentMultiplier - 1f) / (difficultyMultiplierCap - 1f));
            }
            else
            {
                multiplierText.text = ""; // Hide multiplier display when disabled
            }
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes}:{seconds:D2}";
    }
    
    #endregion
    
    #region Multiplier Visual Effects
    
    private void TriggerMultiplierAnimation()
    {
        if (multiplierAnimationCoroutine != null)
            StopCoroutine(multiplierAnimationCoroutine);
            
        multiplierAnimationCoroutine = StartCoroutine(MultiplierBounceAnimation());
    }
    
    private IEnumerator MultiplierBounceAnimation()
    {
        if (!multiplierText) yield break;
        
        Vector3 originalScale = multiplierText.transform.localScale;
        
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Elastic bounce effect
            float bounce = EaseOutElastic(progress);
            float scale = Mathf.Lerp(1.3f, 1f, bounce);
            multiplierText.transform.localScale = originalScale * scale;
            
            yield return null;
        }
        
        multiplierText.transform.localScale = originalScale;
    }
    
    private float EaseOutElastic(float t)
    {
        float c4 = (2f * Mathf.PI) / 3f;
        return t == 0f ? 0f : t == 1f ? 1f : 
               Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
    
    #endregion
    
    #region Game Over
    
    private void ShowGameOverScreen()
    {
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText)
                finalScoreText.text = $"Final Score: {currentScore:N0}";
                
            if (survivalTimeText)
                survivalTimeText.text = $"Survived: {FormatTime(survivalTime)}";
                
            DisplayGameOverStats();
        }
    }
    
    private void DisplayGameOverStats()
    {
        float averagePointsPerSecond = survivalTime > 0 ? currentScore / survivalTime : 0f;
        Debug.Log($"Game Over Stats - Score: {currentScore}, Time: {FormatTime(survivalTime)}, Avg PPS: {averagePointsPerSecond:F1}");
    }
    
    #endregion
    
    #region Public API
    
    public void RestartGame()
    {
        InitializeScoreSystem();
        StartGame();
    }
    
    public int GetCurrentScore() => currentScore;
    public float GetSurvivalTime() => survivalTime;
    public float GetCurrentMultiplier() => currentMultiplier;
    public bool IsGameActive() => isGameActive && !playerIsDead;
    
    // Multiplier control methods
    public void EnableMultipliers(bool enable)
    {
        enableMultipliers = enable;
        Debug.Log($"ScoreSystem: Multipliers {(enable ? "enabled" : "disabled")}");
    }
    
    public bool AreMultipliersEnabled() => enableMultipliers;
    
    #endregion
}