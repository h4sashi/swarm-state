using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(DashFXController))]
public class PlayerDash : MonoBehaviour, IDashAbility, IAbility, IConfigurable<PlayerConfig>
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private LayerMask enemyLayerMask = 1 << 6;

    [Header("Effects")]
    [SerializeField] private DashFXController dashFX;
    [SerializeField] private bool autoCreateFXController = true;

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

    // Public access to FX controller
    public DashFXController FXController => dashFX;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SetupFXController();
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

    private void SetupFXController()
    {
        if (dashFX == null)
        {
            dashFX = GetComponent<DashFXController>();

            if (dashFX == null && autoCreateFXController)
            {
                dashFX = gameObject.AddComponent<DashFXController>();
                if (debugMode)
                {
                    Debug.Log("PlayerDash: Auto-created DashFXController component");
                }
            }
        }

        if (dashFX != null)
        {
            // Subscribe to FX events if needed
            dashFX.OnEffectStarted += OnDashFXStarted;
            dashFX.OnEffectEnded += OnDashFXEnded;
        }
    }

    #region Dash Input & Execution
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

        // Start FX using modular system
        if (dashFX != null)
        {
            dashFX.StartDashEffects();
        }

        if (debugMode)
        {
            Debug.Log($"DASH START: Position={transform.position}, Direction={direction}, Speed={config.dashSpeed}");
        }

        dashCoroutine = StartCoroutine(DashSequence(direction));
        GameEvents.OnPlayerDash?.Invoke(transform.position);
    }

    public void Use() => PerformDash();
    public void Use(Vector3 target) => PerformDash((target - transform.position).normalized);
    #endregion

    #region FX Event Handlers
    private void OnDashFXStarted()
    {
        if (debugMode)
        {
            Debug.Log("PlayerDash: FX effects started");
        }
    }

    private void OnDashFXEnded()
    {
        if (debugMode)
        {
            Debug.Log("PlayerDash: FX effects ended");
        }
    }
    #endregion

    public void ApplyConfig()
    {
        if (config == null) return;

        // Update FX settings based on dash config
        if (dashFX != null)
        {
            var fxSettings = dashFX.Settings;

            // Sync trail duration with dash duration
            fxSettings.trailDuration = Mathf.Max(fxSettings.trailDuration, config.dashDuration * 1.2f);

            // You could also sync other settings here based on your config
            // fxSettings.volume = config.dashSoundVolume;
            // fxSettings.trailStartColor = config.dashColor;

            dashFX.UpdateSettings(fxSettings);
        }
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

        // Stop FX using modular system
        if (dashFX != null)
        {
            dashFX.StopDashEffects();
        }

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

                // Use the existing screen shake or let the FX controller handle it
                if (dashFX != null && dashFX.Settings.enableScreenShake)
                {
                    // Screen shake is handled by FX controller
                }
                else
                {
                    // Fallback to existing system
                    FindObjectOfType<Hanzo.Utils.ScreenShake>()?.TriggerShake(0.2f, 0.35f);
                }
            }
        }
    }

    private bool IsEnemy(GameObject obj)
    {
        return ((1 << obj.layer) & enemyLayerMask) != 0;
    }

    #region Public Utility Methods
    /// <summary>
    /// Customize FX settings at runtime
    /// </summary>
    public void SetTrailColor(Color startColor, Color endColor)
    {
        if (dashFX != null)
        {
            var settings = dashFX.Settings;
            settings.trailStartColor = startColor;
            settings.trailEndColor = endColor;
            dashFX.UpdateSettings(settings);
        }
    }

    /// <summary>
    /// Enable/disable specific effects
    /// </summary>
    public void SetEffectEnabled(string effectType, bool enabled)
    {
        if (dashFX == null) return;

        var settings = dashFX.Settings;

        switch (effectType.ToLower())
        {
            case "trail":
                settings.enableTrail = enabled;
                break;
            case "audio":
                settings.enableAudio = enabled;
                break;
            case "particles":
                settings.enableParticles = enabled;
                break;
            case "shake":
                settings.enableScreenShake = enabled;
                break;
            case "flash":
                settings.enableFlash = enabled;
                break;
        }

        dashFX.UpdateSettings(settings);
    }

    /// <summary>
    /// Get reference to specific FX component
    /// </summary>
    public T GetFXComponent<T>() where T : Component
    {
        if (dashFX == null) return null;

        if (typeof(T) == typeof(TrailRenderer))
            return dashFX.Trail as T;
        else if (typeof(T) == typeof(AudioSource))
            return dashFX.Audio as T;
        else if (typeof(T) == typeof(ParticleSystem))
            return dashFX.Particles as T;

        return null;
    }
    #endregion

    void OnDestroy()
    {
        if (dashCoroutine != null)
            StopCoroutine(dashCoroutine);

        // Unsubscribe from FX events
        if (dashFX != null)
        {
            dashFX.OnEffectStarted -= OnDashFXStarted;
            dashFX.OnEffectEnded -= OnDashFXEnded;
        }
    }

    #region Editor Utilities
#if UNITY_EDITOR
    [ContextMenu("Test Dash Effects Only")]
    private void TestDashEffectsOnly()
    {
        if (Application.isPlaying && dashFX != null)
        {
            dashFX.StartDashEffects();
            StartCoroutine(TestFXSequence());
        }
    }

    private IEnumerator TestFXSequence()
    {
        yield return new WaitForSeconds(1f);
        if (dashFX != null)
            dashFX.StopDashEffects();
    }
#endif
    #endregion
}