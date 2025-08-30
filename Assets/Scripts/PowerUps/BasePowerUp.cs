using System.Collections;
using UnityEngine;

public abstract class BasePowerUp : MonoBehaviour, IPowerUp, IStatModifier
{
    [SerializeField] protected PowerUpConfig config;
    [SerializeField] protected PlayerController targetPlayer;
    
    protected float remainingTime;
    protected int stackCount = 1;
    protected bool isActive = false;
    protected bool isInitialized = false; // Track initialization state
    protected Coroutine durationCoroutine;
    protected Coroutine uiUpdateCoroutine;
    
    // Callback for when power-up deactivates
    public System.Action OnPowerUpDeactivated { get; set; }
    
    // IAbility Implementation
    public string AbilityName => config?.displayName ?? "Unknown PowerUp";
    public bool CanUse => !isActive && isInitialized;
    public float CooldownProgress => config != null ? 1f - (remainingTime / config.duration) : 0f;
    
    // IPowerUp Implementation
    public PowerUpType PowerUpType => config?.powerUpType ?? PowerUpType.SpeedBoost;
    public float Duration => config?.duration ?? 0f;
    public float RemainingTime => remainingTime;
    public bool IsActive => isActive;
    public bool IsStackable => config?.isStackable ?? false;
    public int StackCount => stackCount;
    
    protected virtual void Awake()
    {
        // Don't validate config here - it may not be assigned yet
        // Config validation happens in Initialize()
    }
    
    public virtual void Initialize(PowerUpConfig config, PlayerController player)
    {
        // Validate inputs
        if (!config)
        {
            Debug.LogError($"PowerUp {name}: No config provided to Initialize()!");
            return;
        }
        
        if (!player)
        {
            Debug.LogError($"PowerUp {name}: No player provided to Initialize()!");
            return;
        }
        
        this.config = config;
        this.targetPlayer = player;
        remainingTime = config.duration;
        isInitialized = true;
        
        // Update the GameObject name for easier debugging
        name = $"PowerUp_{config.displayName}";
        
        if (Application.isPlaying)
        {
            Debug.Log($"PowerUp {config.displayName} initialized successfully");
        }
    }
    
    public virtual void Activate()
    {
        if (!isInitialized)
        {
            Debug.LogError($"PowerUp {name}: Cannot activate - not properly initialized!");
            return;
        }
        
        if (isActive && config.isStackable)
        {
            Stack();
            return;
        }
        
        isActive = true;
        remainingTime = config.duration;
        ApplyModification(targetPlayer);
        
        // Start duration timer
        if (durationCoroutine != null) StopCoroutine(durationCoroutine);
        durationCoroutine = StartCoroutine(DurationTimer());
        
        // Start UI update routine
        if (uiUpdateCoroutine != null) StopCoroutine(uiUpdateCoroutine);
        uiUpdateCoroutine = StartCoroutine(UIUpdateRoutine());
        
        // Trigger activation events
        GameEvents.OnUIUpdate?.Invoke($"{config.displayName} activated!");
        GameEvents.OnPowerUpStateChanged?.Invoke(config.powerUpType, true);
        GameEvents.OnPowerUpStacked?.Invoke(config.powerUpType, stackCount);
        
        Debug.Log($"PowerUp {config.displayName} activated for {config.duration}s");
    }
    
    public virtual void Deactivate()
    {
        if (!isActive) return;
        
        isActive = false;
        stackCount = 1;
        RemoveModification(targetPlayer);
        
        if (durationCoroutine != null)
        {
            StopCoroutine(durationCoroutine);
            durationCoroutine = null;
        }
        
        if (uiUpdateCoroutine != null)
        {
            StopCoroutine(uiUpdateCoroutine);
            uiUpdateCoroutine = null;
        }
        
        // Trigger deactivation events
        GameEvents.OnPowerUpStateChanged?.Invoke(config.powerUpType, false);
        
        // Notify the manager to clean up
        OnPowerUpDeactivated?.Invoke();
        
        Debug.Log($"PowerUp {config.displayName} deactivated");
    }
    
    public virtual void Stack()
    {
        if (!config.isStackable || stackCount >= config.maxStacks) return;
        
        stackCount++;
        
        if (config.stacksExtendDuration)
        {
            // Extend duration
            remainingTime = Mathf.Max(remainingTime, config.duration);
        }
        else
        {
            // Refresh effect intensity
            RemoveModification(targetPlayer);
            ApplyModification(targetPlayer);
        }
        
        // Trigger stack event for UI feedback
        GameEvents.OnPowerUpStacked?.Invoke(config.powerUpType, stackCount);
        
        Debug.Log($"PowerUp {config.displayName} stacked! Count: {stackCount}");
    }
    
    // IAbility methods
    public virtual void Use() => Activate();
    public virtual void Use(Vector3 target) => Activate();
    
    // Abstract methods for derived classes
    public abstract void ApplyModification(PlayerController player);
    public abstract void RemoveModification(PlayerController player);
    
    protected virtual IEnumerator DurationTimer()
    {
        while (remainingTime > 0 && isActive)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }
        
        if (isActive)
        {
            Deactivate();
        }
    }
    
    /// <summary>
    /// Coroutine that continuously updates UI with remaining time
    /// Updates at 10fps to avoid unnecessary overhead while maintaining smooth countdown
    /// </summary>
    protected virtual IEnumerator UIUpdateRoutine()
    {
        const float updateInterval = 0.1f; // 10fps update rate
        
        while (isActive && remainingTime > 0)
        {
            // Send time update to UI
            GameEvents.OnPowerUpTimeRemaining?.Invoke(config.powerUpType, remainingTime);
            
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    protected virtual void OnDestroy()
    {
        if (isActive && targetPlayer != null)
        {
            RemoveModification(targetPlayer);
        }
        
        // Clean up coroutines
        if (durationCoroutine != null)
        {
            StopCoroutine(durationCoroutine);
        }
        
        if (uiUpdateCoroutine != null)
        {
            StopCoroutine(uiUpdateCoroutine);
        }
    }
}