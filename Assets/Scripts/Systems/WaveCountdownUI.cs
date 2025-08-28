using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WaveCountdownUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject waveCountdownPanel;
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI waveInfoText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image progressRing;
    
    [Header("Animation Settings")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float pulseIntensity = 0.2f;
    [SerializeField] private float shakeIntensity = 10f;
    
    [Header("Visual Effects")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color urgentColor = Color.red;
    [SerializeField] private Color waveStartColor = Color.green;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    // [Header("Audio")]
    // [SerializeField] private AudioSource audioSource;
    // [SerializeField] private AudioClip countdownTick;
    // [SerializeField] private AudioClip waveStartSound;
    // [SerializeField] private bool enableAudio = true;
    
    // Internal state
    private EnemySpawner enemySpawner;
    private Coroutine countdownCoroutine;
    private Vector3 originalPanelPosition;
    private Vector3 originalPanelScale;
    private CanvasGroup panelCanvasGroup;
    private bool isCountdownActive = false;
    
    // Animation cache
    private WaitForEndOfFrame waitFrame = new WaitForEndOfFrame();
    
    private void Awake()
    {
        // Setup UI components
        SetupUIComponents();
        
        // Find enemy spawner
        enemySpawner = FindObjectOfType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogError("[WaveCountdownUI] No EnemySpawner found in scene!");
        }
        
        // Cache original transforms
        if (waveCountdownPanel != null)
        {
            originalPanelPosition = waveCountdownPanel.transform.localPosition;
            originalPanelScale = waveCountdownPanel.transform.localScale;
        }
        
        // Initialize as hidden
        HidePanel();
    }
    
    private void Start()
    {
        // Subscribe to enemy spawner events
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted += OnWaveStarted;
        }
        
        Debug.Log("[WaveCountdownUI] Wave countdown UI initialized");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (enemySpawner != null)
        {
            enemySpawner.OnWaveStarted -= OnWaveStarted;
        }
        
        // Stop any running coroutines
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
    }
    
    private void SetupUIComponents()
    {
        // Add CanvasGroup for smooth fade animations if not present
        if (waveCountdownPanel != null)
        {
            panelCanvasGroup = waveCountdownPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = waveCountdownPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Setup audio source if not assigned
        // if (audioSource == null && enableAudio)
        // {
        //     audioSource = GetComponent<AudioSource>();
        //     if (audioSource == null)
        //     {
        //         audioSource = gameObject.AddComponent<AudioSource>();
        //         audioSource.playOnAwake = false;
        //         audioSource.volume = 0.7f;
        //     }
        // }
    }
    
    #region Public Interface
    
    /// <summary>
    /// Start countdown for the next wave
    /// </summary>
    /// <param name="waveNumber">The wave number starting</param>
    /// <param name="enemyCount">Number of enemies in the wave</param>
    /// <param name="delay">Countdown duration (optional override)</param>
    public void StartWaveCountdown(int waveNumber, int enemyCount = 0, float delay = -1f)
    {
        if (isCountdownActive)
        {
            Debug.LogWarning("[WaveCountdownUI] Countdown already active, stopping previous countdown");
            StopCountdown();
        }
        
        float actualDelay = delay > 0 ? delay : countdownDuration;
        
        Debug.Log($"[WaveCountdownUI] Starting countdown for Wave {waveNumber}, Duration: {actualDelay}s");
        
        countdownCoroutine = StartCoroutine(CountdownSequence(waveNumber, enemyCount, actualDelay));
    }
    
    /// <summary>
    /// Stop the current countdown
    /// </summary>
    public void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        isCountdownActive = false;
        HidePanel();
    }
    
    /// <summary>
    /// Show wave start notification without countdown
    /// </summary>
    public void ShowWaveStart(int waveNumber)
    {
        StartCoroutine(WaveStartNotification(waveNumber));
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnWaveStarted(int waveNumber)
    {
        // This gets called when a wave actually starts
        // We can show a brief "Wave X Started" message
        if (!isCountdownActive) // Only show if not currently in countdown
        {
            ShowWaveStart(waveNumber);
        }
        
        Debug.Log($"[WaveCountdownUI] Wave {waveNumber} started");
    }
    
    #endregion
    
    #region Animation Sequences
    
    private IEnumerator CountdownSequence(int waveNumber, int enemyCount, float duration)
    {
        isCountdownActive = true;
        
        // Setup initial UI state
        UpdateWaveInfo(waveNumber, enemyCount);
        
        // Fade in panel
        yield return StartCoroutine(FadeInPanel());
        
        // Countdown loop
        float remainingTime = duration;
        
        while (remainingTime > 0 && isCountdownActive)
        {
            // Update countdown display
            UpdateCountdownDisplay(remainingTime);
            
            // Play tick sound on whole seconds
            // if (enableAudio && Mathf.Ceil(remainingTime) != Mathf.Ceil(remainingTime - Time.deltaTime))
            // {
            //     PlayTickSound();
            // }
            
            // Animate based on remaining time
            float normalizedTime = remainingTime / duration;
            yield return StartCoroutine(AnimateCountdownFrame(normalizedTime, remainingTime));
            
            remainingTime -= Time.deltaTime;
            yield return waitFrame;
        }
        
        // Wave starting sequence
        if (isCountdownActive)
        {
            yield return StartCoroutine(WaveStartSequence(waveNumber));
        }
        
        // Fade out
        yield return StartCoroutine(FadeOutPanel());
        
        isCountdownActive = false;
        countdownCoroutine = null;
    }
    
    private IEnumerator FadeInPanel()
    {
        ShowPanel();
        
        float elapsed = 0f;
        Vector3 startScale = originalPanelScale * 0.8f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            
            // Smooth fade and scale in
            float smoothProgress = SmoothStep(0f, 1f, progress);
            
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = smoothProgress;
                
            if (waveCountdownPanel != null)
                waveCountdownPanel.transform.localScale = Vector3.Lerp(startScale, originalPanelScale, smoothProgress);
            
            yield return waitFrame;
        }
        
        // Ensure final state
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        if (waveCountdownPanel != null) waveCountdownPanel.transform.localScale = originalPanelScale;
    }
    
    private IEnumerator FadeOutPanel()
    {
        float elapsed = 0f;
        float startAlpha = panelCanvasGroup != null ? panelCanvasGroup.alpha : 1f;
        Vector3 startScale = waveCountdownPanel != null ? waveCountdownPanel.transform.localScale : originalPanelScale;
        Vector3 endScale = originalPanelScale * 1.1f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            float smoothProgress = SmoothStep(0f, 1f, progress);
            
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, smoothProgress);
                
            if (waveCountdownPanel != null)
                waveCountdownPanel.transform.localScale = Vector3.Lerp(startScale, endScale, smoothProgress);
            
            yield return waitFrame;
        }
        
        HidePanel();
    }
    
    private IEnumerator AnimateCountdownFrame(float normalizedTime, float remainingTime)
    {
        // Determine urgency level
        bool isUrgent = remainingTime <= 1f;
        bool isVeryUrgent = remainingTime <= 0.5f;
        
        // Color animation
        Color targetColor = isUrgent ? urgentColor : normalColor;
        if (countdownText != null)
        {
            countdownText.color = Color.Lerp(countdownText.color, targetColor, Time.deltaTime * 5f);
        }
        
        // Scale pulse effect
        if (isUrgent)
        {
            float pulseFreq = isVeryUrgent ? 8f : 4f;
            float pulse = 1f + Mathf.Sin(Time.time * pulseFreq) * pulseIntensity;
            
            if (countdownText != null)
            {
                countdownText.transform.localScale = Vector3.one * pulse;
            }
        }
        else
        {
            // Smooth scale back to normal
            if (countdownText != null)
            {
                countdownText.transform.localScale = Vector3.Lerp(countdownText.transform.localScale, Vector3.one, Time.deltaTime * 3f);
            }
        }
        
        // Screen shake effect for very urgent
        if (isVeryUrgent && waveCountdownPanel != null)
        {
            float shakeAmount = shakeIntensity * (1f - normalizedTime);
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );
            
            waveCountdownPanel.transform.localPosition = originalPanelPosition + shakeOffset;
        }
        else if (waveCountdownPanel != null)
        {
            // Smooth return to original position
            waveCountdownPanel.transform.localPosition = Vector3.Lerp(
                waveCountdownPanel.transform.localPosition, 
                originalPanelPosition, 
                Time.deltaTime * 5f
            );
        }
        
        // Update progress ring
        if (progressRing != null)
        {
            progressRing.fillAmount = 1f - normalizedTime;
            progressRing.color = targetColor;
        }
        
        yield return waitFrame;
    }
    
    private IEnumerator WaveStartSequence(int waveNumber)
    {
        // Play wave start sound
        // if (enableAudio && waveStartSound != null && audioSource != null)
        // {
        //     audioSource.PlayOneShot(waveStartSound);
        // }
        
        // Update text to "WAVE START!"
        if (countdownText != null)
        {
            countdownText.text = "START!";
            countdownText.color = waveStartColor;
        }
        
        // Big scale punch
        float elapsed = 0f;
        float punchDuration = 0.3f;
        Vector3 startScale = countdownText != null ? countdownText.transform.localScale : Vector3.one;
        Vector3 punchScale = Vector3.one * 1.5f;
        
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / punchDuration;
            
            // Use easing curve for punch effect
            float curveValue = scaleCurve.Evaluate(progress);
            Vector3 currentScale = Vector3.Lerp(startScale, punchScale, curveValue);
            
            if (countdownText != null)
                countdownText.transform.localScale = currentScale;
            
            yield return waitFrame;
        }
        
        // Quick scale back
        elapsed = 0f;
        float scaleBackDuration = 0.2f;
        
        while (elapsed < scaleBackDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / scaleBackDuration;
            
            if (countdownText != null)
            {
                countdownText.transform.localScale = Vector3.Lerp(punchScale, Vector3.one, progress);
            }
            
            yield return waitFrame;
        }
        
        yield return new WaitForSeconds(0.5f); // Brief pause to show "START!"
    }
    
    private IEnumerator WaveStartNotification(int waveNumber)
    {
        // Quick notification for wave start
        UpdateWaveInfo(waveNumber, 0);
        if (countdownText != null)
        {
            countdownText.text = "WAVE\nSTARTED!";
            countdownText.color = waveStartColor;
        }
        
        yield return StartCoroutine(FadeInPanel());
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(FadeOutPanel());
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateWaveInfo(int waveNumber, int enemyCount)
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"WAVE {waveNumber}";
        }
        
        if (waveInfoText != null && enemyCount > 0)
        {
            waveInfoText.text = $"Enemies: {enemyCount}";
        }
        else if (waveInfoText != null)
        {
            waveInfoText.text = "Prepare for battle!";
        }
    }
    
    private void UpdateCountdownDisplay(float remainingTime)
    {
        if (countdownText != null)
        {
            int seconds = Mathf.CeilToInt(remainingTime);
            countdownText.text = seconds.ToString();
        }
    }
    
    private void ShowPanel()
    {
        if (waveCountdownPanel != null)
        {
            waveCountdownPanel.SetActive(true);
        }
    }
    
    private void HidePanel()
    {
        if (waveCountdownPanel != null)
        {
            waveCountdownPanel.SetActive(false);
        }
        
        // Reset transforms
        if (waveCountdownPanel != null)
        {
            waveCountdownPanel.transform.localPosition = originalPanelPosition;
            waveCountdownPanel.transform.localScale = originalPanelScale;
        }
        
        if (countdownText != null)
        {
            countdownText.transform.localScale = Vector3.one;
            countdownText.color = normalColor;
        }
    }
    
    #endregion
    
    #region Audio
    
    // private void PlayTickSound()
    // {
    //     if (enableAudio && countdownTick != null && audioSource != null)
    //     {
    //         audioSource.PlayOneShot(countdownTick);
    //     }
    // }
    
    #endregion
    
    #region Utility Methods
    
    private float SmoothStep(float from, float to, float t)
    {
        t = Mathf.Clamp01(t);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(from, to, t);
    }
    
    /// <summary>
    /// Manual trigger for testing purposes
    /// </summary>
    [ContextMenu("Test Wave Countdown")]
    public void TestCountdown()
    {
        StartWaveCountdown(Random.Range(1, 10), Random.Range(5, 20));
    }
    
    #endregion
    
    #region Integration Helpers
    
    /// <summary>
    /// Call this to integrate with EnemySpawner's wave delay system
    /// Should be called before a wave starts spawning
    /// </summary>
    public void TriggerFromSpawner(int waveNumber, float waveDelay)
    {
        // Get additional info from spawner if available
        int enemyCount = 0;
        
        // You could extend this to get enemy count from the spawner
        // For example: enemyCount = enemySpawner.GetWaveEnemyCount(waveNumber);
        
        StartWaveCountdown(waveNumber, enemyCount, waveDelay);
    }
    
    /// <summary>
    /// Enable/disable audio at runtime
    /// </summary>
    // public void SetAudioEnabled(bool enabled)
    // {
    //     enableAudio = enabled;
    //     if (audioSource != null)
    //     {
    //         audioSource.enabled = enabled;
    //     }
    // }
    
    /// <summary>
    /// Update countdown duration setting
    /// </summary>
    public void SetCountdownDuration(float duration)
    {
        countdownDuration = Mathf.Max(0.5f, duration);
    }
    
    #endregion
}