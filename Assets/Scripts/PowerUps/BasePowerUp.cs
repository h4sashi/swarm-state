using System.Collections;
using UnityEngine;

public abstract class BasePowerUp : MonoBehaviour, IPowerUp, IStatModifier
{
    [SerializeField] protected PowerUpConfig config;
    [SerializeField] protected PlayerController targetPlayer;
    
    protected float remainingTime;
    protected int stackCount = 1;
    protected bool isActive = false;
    protected Coroutine durationCoroutine;
    protected Coroutine uiUpdateCoroutine;
    
    // Callback for when power-up deactivates
    public System.Action OnPowerUpDeactivated { get; set; }
    
    // IAbility Implementation
    public string AbilityName => config.displayName;
    public bool CanUse => !isActive;
    public float CooldownProgress => 1f - (remainingTime / config.duration);
    
    // IPowerUp Implementation
    public PowerUpType PowerUpType => config.powerUpType;
    public float Duration => config.duration;
    public float RemainingTime => remainingTime;
    public bool IsActive => isActive;
    public bool IsStackable => config.isStackable;
    public int StackCount => stackCount;
    
    protected virtual void Awake()
    {
        if (!config)
        {
            Debug.LogError($"PowerUp {name}: No config assigned!");
        }
    }
    
    public virtual void Initialize(PowerUpConfig config, PlayerController player)
    {
        this.config = config;
        this.targetPlayer = player;
        remainingTime = config.duration;
    }
    
    public virtual void Activate()
    {
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