using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable, IHealth, IConfigurable<PlayerConfig>
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private MeshRenderer meshRenderer;
    
    private int currentHealth;
    private bool isInvulnerable;
    private Coroutine invulnerabilityCoroutine;
    private Material originalMaterial;
    
    // Reference to dash component to check dash state
    private IDashAbility dashAbility;
    
    // Interface Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => config.maxHealth;
    public bool IsAlive => currentHealth > 0;
    public bool IsInvulnerable => isInvulnerable;
    public PlayerConfig Config { get => config; set => config = value; }
    
    // Interface Events
    public System.Action<int> OnHealthChanged { get; set; }
    public System.Action OnDeath { get; set; }
    
    void Awake()
    {
        if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
        originalMaterial = meshRenderer.material;
        
        // Get reference to dash ability
        dashAbility = GetComponent<IDashAbility>();
        if (dashAbility == null)
        {
            Debug.LogWarning("PlayerHealth: No IDashAbility found! Player won't be invulnerable during dash.");
        }
        
        ApplyConfig();
    }
    
    void Start()
    {
        currentHealth = config.maxHealth;
        
        // Subscribe to both interface events and game events
        OnHealthChanged += (health) => GameEvents.OnPlayerHealthChanged?.Invoke(health);
        OnDeath += () => GameEvents.OnPlayerDeath?.Invoke();
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void TakeDamage(float damage)
    {
        SoundManager.Instance.PlayPlayerDamageSound();
        // Check if player is invulnerable OR dashing
        if (isInvulnerable || !IsAlive || (dashAbility != null && dashAbility.IsDashing))
        {
            if (dashAbility != null && dashAbility.IsDashing)
            {
                Debug.Log("PlayerHealth: Damage blocked - Player is dashing!");
            }
            return;
        }
        
        currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(damage));
        FindObjectOfType<Hanzo.Utils.ScreenShake>()?.TriggerShake();
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartInvulnerability();
        }
    }
    
    public void Heal(int amount)
    {
        if (!IsAlive) return;
        currentHealth = Mathf.Min(config.maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    public void ApplyConfig()
    {
        if (config == null) return;
        if (Application.isPlaying && currentHealth == 0)
        {
            currentHealth = config.maxHealth;
            OnHealthChanged?.Invoke(currentHealth);
        }
    }
    
    private void StartInvulnerability()
    {
        if (invulnerabilityCoroutine != null)
            StopCoroutine(invulnerabilityCoroutine);
        invulnerabilityCoroutine = StartCoroutine(InvulnerabilitySequence());
    }
    
    private IEnumerator InvulnerabilitySequence()
    {
        isInvulnerable = true;
        float flashDuration = 0.1f;
        int flashCount = Mathf.RoundToInt(config.invulnerabilityDuration / (flashDuration * 2));
        
        for (int i = 0; i < flashCount; i++)
        {
            meshRenderer.material.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            meshRenderer.material.color = originalMaterial.color;
            yield return new WaitForSeconds(flashDuration);
        }
        
        isInvulnerable = false;
        invulnerabilityCoroutine = null;
    }
    
    private void Die()
    {
        OnDeath?.Invoke();
        meshRenderer.material.color = Color.black;
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerDash>().enabled = false;
        Debug.Log("Player Died!");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("EnemyProjectile"))
        {
            // The TakeDamage method now handles dash invulnerability check internally
            TakeDamage(1f);
        }
    }
}