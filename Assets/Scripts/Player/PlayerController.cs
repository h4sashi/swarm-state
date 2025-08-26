using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement), typeof(PlayerDash), typeof(PlayerHealth))]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerConfig playerConfig;
    
    // Use concrete types for RequireComponent, but cast to interfaces internally
    [Header("Components")]
    [SerializeField] private PlayerMovement movementComponent;
    [SerializeField] private PlayerDash dashComponent;
    [SerializeField] private PlayerHealth healthComponent;
    
    // Interface references for internal use
    private IMovementController movement;
    private IDashAbility dash;
    private IHealth health;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject dashTrailEffect;
    [SerializeField] private ParticleSystem deathParticles;
    
    private PlayerInput playerInput;
    
    void Awake()
    {
        // Auto-assign concrete components
        if (!movementComponent) movementComponent = GetComponent<PlayerMovement>();
        if (!dashComponent) dashComponent = GetComponent<PlayerDash>();
        if (!healthComponent) healthComponent = GetComponent<PlayerHealth>();
        
        // Cast to interfaces for internal use
        movement = movementComponent;
        dash = dashComponent;
        health = healthComponent;
        
        playerInput = GetComponent<PlayerInput>();
        
        if (!playerConfig)
        {
            Debug.LogError("PlayerConfig is not assigned!");
        }
    }
    
    void Start()
    {
        // Subscribe to interface events as well as game events
        health.OnDeath += OnPlayerDeath;
        GameEvents.OnPlayerDash += OnPlayerDashPerformed;
        
        SetupPlayerVisual();
    }
    
    void OnDestroy()
    {
        health.OnDeath -= OnPlayerDeath;
        GameEvents.OnPlayerDash -= OnPlayerDashPerformed;
    }
    
    private void SetupPlayerVisual()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer && renderer.material.name.Contains("Default"))
        {
            Material playerMat = new Material(Shader.Find("Standard"));
            playerMat.color = Color.blue;
            renderer.material = playerMat;
        }
        
        gameObject.tag = "Player";
        gameObject.layer = LayerMask.NameToLayer("Player");
    }
    
    private void OnPlayerDashPerformed(Vector3 dashPosition)
    {
        if (dashTrailEffect)
        {
            dashTrailEffect.SetActive(true);
            Invoke(nameof(DisableDashTrail), playerConfig.dashDuration);
        }
        
        Debug.Log($"Player dashed at position: {dashPosition}");
    }
    
    private void OnPlayerDeath()
    {
        if (deathParticles)
        {
            deathParticles.Play();
        }
        
        if (playerInput)
        {
            playerInput.enabled = false;
        }
        
        Debug.Log("Player Controller: Player has died!");
    }
    
    private void DisableDashTrail()
    {
        if (dashTrailEffect)
        {
            dashTrailEffect.SetActive(false);
        }
    }

     void OnGUI()
{
    if (!Application.isEditor) return;
    
    // Create a new GUIStyle with larger font
    GUIStyle largeStyle = new GUIStyle(GUI.skin.label);
    largeStyle.fontSize = 22; // Increased from default (usually 12-14)
    largeStyle.fontStyle = FontStyle.Bold; // Optional: make it bold
    
    GUILayout.BeginArea(new Rect(10, 10, 400, 250)); // Increased area size to accommodate larger text
    GUILayout.Label($"Health: {health.CurrentHealth}/{health.MaxHealth}", largeStyle);
    GUILayout.Label($"Is Dashing: {dash.IsDashing}", largeStyle);
    GUILayout.Label($"Dash Cooldown: {(dash.CanDash ? "Ready" : $"{(1 - dash.DashCooldownProgress):F1}s")}", largeStyle);
    GUILayout.Label($"Is Invulnerable: {health.IsInvulnerable}", largeStyle);
    GUILayout.EndArea();
}
    // Public API now using interfaces
    public bool IsAlive() => health.IsAlive;
    public bool IsDashing() => dash.IsDashing;
    public float GetDashCooldown() => dash.DashCooldownProgress;
    public int GetHealth() => health.CurrentHealth;
    public Vector3 GetPosition() => transform.position;
    public Vector3 GetVelocity() => movement.GetVelocity();
}