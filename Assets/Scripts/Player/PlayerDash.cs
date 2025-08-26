using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour, IDashAbility, IAbility, IConfigurable<PlayerConfig>
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private LayerMask enemyLayerMask = 1 << 6;
    
    private IMovementController movement;
    private Rigidbody rb;
    private float lastDashTime;
    private bool isDashing;
    private Coroutine dashCoroutine;
    
    // Debug variables
    [SerializeField] private bool debugMode = true;
    private Vector3 dashStartPos;
    
    // Interface Properties
    public bool IsDashing => isDashing;
    public bool CanDash => Time.time >= lastDashTime + config.dashCooldown;
    public bool CanUse => CanDash;
    public float DashCooldownProgress => Mathf.Clamp01((Time.time - lastDashTime) / config.dashCooldown);
    public float CooldownProgress => DashCooldownProgress;
    public string AbilityName => "Dash";
    public PlayerConfig Config { get => config; set => config = value; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        movement = GetComponent<IMovementController>();
        if (movement == null)
        {
            Debug.LogError("PlayerDash: No IMovementController found!");
        }
        
        StartCoroutine(UpdateCooldownUI());
        ApplyConfig();
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && CanDash && !isDashing)
        {
            PerformDash();
        }
    }

    public void PerformDash()
    {
        Vector3 dashDirection = movement.GetMovementDirection();
        if (dashDirection.magnitude < 0.1f)
        {
            dashDirection = transform.forward;
        }
        PerformDash(dashDirection);
    }

    public void PerformDash(Vector3 direction)
    {
        lastDashTime = Time.time;
        dashStartPos = transform.position;
        
        if (dashCoroutine != null)
            StopCoroutine(dashCoroutine);
        
        if (debugMode)
        {
            Debug.Log($"DASH START: Position={transform.position}, Direction={direction}, Speed={config.dashSpeed}");
        }
        
        dashCoroutine = StartCoroutine(DashSequence(direction));
        GameEvents.OnPlayerDash?.Invoke(transform.position);
    }

    public void Use() => PerformDash();
    public void Use(Vector3 target) => PerformDash((target - transform.position).normalized);

    public void ApplyConfig()
    {
        if (config == null) return;
    }

    // MOST RELIABLE DASH IMPLEMENTATION
    private IEnumerator DashSequence(Vector3 direction)
    {
        isDashing = true;
        movement.CanMove = false; // Stop normal movement
        
        // Store original values
        float originalDrag = rb.linearDamping;
        Vector3 originalVelocity = rb.linearVelocity;
        
        // Zero out drag for consistent dash
        rb.linearDamping = 0f;
        
        if (debugMode)
        {
            Debug.Log($"DASH SEQUENCE START: OriginalDrag={originalDrag}, Direction={direction}");
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < config.dashDuration)
        {
            float normalizedTime = elapsedTime / config.dashDuration;
            float curveValue = config.dashCurve.Evaluate(normalizedTime);
            
            // Calculate dash velocity
            Vector3 dashVelocity = direction * config.dashSpeed * curveValue;
            
            // FORCE the velocity - don't let anything override it
            rb.linearVelocity = new Vector3(dashVelocity.x, rb.linearVelocity.y, dashVelocity.z);
            
            if (debugMode && elapsedTime < 0.05f) // Log first few frames
            {
                Debug.Log($"DASH FRAME: Time={elapsedTime:F3}, Curve={curveValue:F2}, Velocity={rb.linearVelocity}, Position={transform.position}");
            }
            
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        
        // Restore original settings
        rb.linearDamping = originalDrag;
        
        if (debugMode)
        {
            float distanceTraveled = Vector3.Distance(dashStartPos, transform.position);
            Debug.Log($"DASH COMPLETE: Distance={distanceTraveled:F2}, Expected≈{config.dashSpeed * config.dashDuration:F2}");
        }
        
        isDashing = false;
        movement.CanMove = true;
        dashCoroutine = null;
    }

    private IEnumerator UpdateCooldownUI()
    {
        while (true)
        {
            GameEvents.OnDashCooldownUpdate?.Invoke(DashCooldownProgress);
            yield return new WaitForSeconds(0.1f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDashing && IsEnemy(other.gameObject))
        {
            var enemy = other.GetComponent<IDamageable>();
            if (enemy != null)
            {
                enemy.TakeDamage(config.dashDamage);
                GameEvents.OnEnemyKilled?.Invoke(other.gameObject);

                Debug.Log($"DASH HIT: Damaged {other.gameObject.name} for {config.dashDamage} damage.");
                    FindObjectOfType<Hanzo.Utils.ScreenShake>()?.TriggerShake(0.2f, 0.35f);

                
            }
        }
    }

    private bool IsEnemy(GameObject obj)
    {
        return ((1 << obj.layer) & enemyLayerMask) != 0;
    }

    void OnDestroy()
    {
        if (dashCoroutine != null)
            StopCoroutine(dashCoroutine);
    }
}
