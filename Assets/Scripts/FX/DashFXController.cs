using System.Collections;
using UnityEngine;

[System.Serializable]
public class DashFXSettings
{
    [Header("Trail Settings")]
    public bool enableTrail = true;
    public float trailDuration = 0.3f;
    public float trailStartWidth = 0.5f;
    public float trailEndWidth = 0.1f;
    public Color trailStartColor = new Color(0.3f, 0.7f, 1f, 0.8f);
    public Color trailEndColor = new Color(0.3f, 0.7f, 1f, 0f);
    public float trailFadeOutTime = 0.5f;
    public Material customTrailMaterial;
    public bool autoCreateTrail = true;
    
    [Header("Audio Settings")]
    public bool enableAudio = true;
    public AudioClip dashSound;
    public float volume = 0.7f;
    [Range(0.5f, 2f)] public float pitchMin = 0.95f;
    [Range(0.5f, 2f)] public float pitchMax = 1.05f;
    public float spatialBlend = 0.7f;
    public bool createDedicatedAudioSource = true;
    
    [Header("Particle Settings")]
    public bool enableParticles = false;
    public ParticleSystem dashParticleSystem;
    public bool autoCreateParticles = false;
    public int particleCount = 20;
    public Color particleColor = Color.cyan;
    public float particleLifetime = 0.5f;
    
    [Header("Screen Effects")]
    public bool enableScreenShake = false;
    public float shakeDuration = 0.2f;
    public float shakeIntensity = 0.35f;
    
    [Header("Additional Effects")]
    public bool enableFlash = false;
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;
}

/// <summary>
/// Modular, reusable dash effects controller that can be attached to any GameObject
/// Handles trail rendering, audio, particles, and other visual effects for dashing
/// </summary>
public class DashFXController : MonoBehaviour
{
    [SerializeField] private DashFXSettings fxSettings = new DashFXSettings();
    [SerializeField] private bool debugMode = false;
    
    // Components
    private TrailRenderer trailRenderer;
    private AudioSource audioSource;
    private ParticleSystem particles;
    
    // State tracking
    private bool isEffectActive = false;
    private Coroutine trailFadeCoroutine;
    private Coroutine flashCoroutine;
    
    // Original values for restoration
    private bool originalTrailEmitting;
    private Color originalTrailStartColor;
    private Color originalTrailEndColor;
    
    // Events
    public System.Action OnEffectStarted;
    public System.Action OnEffectEnded;
    
    #region Public Properties
    public DashFXSettings Settings => fxSettings;
    public bool IsEffectActive => isEffectActive;
    public TrailRenderer Trail => trailRenderer;
    public AudioSource Audio => audioSource;
    public ParticleSystem Particles => particles;
    #endregion
    
    #region Initialization
    void Awake()
    {
        InitializeComponents();
    }
    
    void Start()
    {
        ApplyCurrentSettings();
    }
    
    private void InitializeComponents()
    {
        if (fxSettings.enableTrail)
            SetupTrailRenderer();
            
        if (fxSettings.enableAudio)
            SetupAudioSource();
            
        if (fxSettings.enableParticles)
            SetupParticleSystem();
    }
    
    private void SetupTrailRenderer()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        
        if (trailRenderer == null && fxSettings.autoCreateTrail)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
            
            if (trailRenderer == null)
            {
                CreateTrailRenderer();
            }
        }
        
        if (trailRenderer != null)
        {
            StoreOriginalTrailSettings();
            ConfigureTrailRenderer();
        }
    }
    
    private void CreateTrailRenderer()
    {
        GameObject trailObject = new GameObject("DashTrail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = Vector3.zero;
        
        trailRenderer = trailObject.AddComponent<TrailRenderer>();
        
        if (debugMode)
            Debug.Log($"[DashFX] Created TrailRenderer on {name}");
    }
    
    private void SetupAudioSource()
    {
        if (fxSettings.createDedicatedAudioSource)
        {
            GameObject audioObject = new GameObject("DashAudio");
            audioObject.transform.SetParent(transform);
            audioObject.transform.localPosition = Vector3.zero;
            audioSource = audioObject.AddComponent<AudioSource>();
        }
        else
        {
            audioSource = GetComponent<AudioSource>() ?? GetComponentInChildren<AudioSource>();
        }
        
        if (audioSource != null)
            ConfigureAudioSource();
    }
    
    private void SetupParticleSystem()
    {
        if (fxSettings.dashParticleSystem != null)
        {
            particles = fxSettings.dashParticleSystem;
        }
        else if (fxSettings.autoCreateParticles)
        {
            CreateParticleSystem();
        }
        else
        {
            particles = GetComponent<ParticleSystem>() ?? GetComponentInChildren<ParticleSystem>();
        }
        
        if (particles != null)
            ConfigureParticleSystem();
    }
    #endregion
    
    #region Component Configuration
    private void StoreOriginalTrailSettings()
    {
        if (trailRenderer == null) return;
        
        originalTrailEmitting = trailRenderer.emitting;
        originalTrailStartColor = trailRenderer.startColor;
        originalTrailEndColor = trailRenderer.endColor;
    }
    
    private void ConfigureTrailRenderer()
    {
        if (trailRenderer == null) return;
        
        trailRenderer.time = fxSettings.trailDuration;
        trailRenderer.startWidth = fxSettings.trailStartWidth;
        trailRenderer.endWidth = fxSettings.trailEndWidth;
        trailRenderer.startColor = fxSettings.trailStartColor;
        trailRenderer.endColor = fxSettings.trailEndColor;
        trailRenderer.emitting = false;
        
        if (fxSettings.customTrailMaterial != null)
        {
            trailRenderer.material = fxSettings.customTrailMaterial;
        }
        else
        {
            trailRenderer.material = CreateURPTrailMaterial();
        }
    }
    
    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;
        
        audioSource.clip = fxSettings.dashSound;
        audioSource.volume = fxSettings.volume;
        audioSource.spatialBlend = fxSettings.spatialBlend;
        audioSource.playOnAwake = false;
    }
    
    private void ConfigureParticleSystem()
    {
        if (particles == null) return;
        
        var main = particles.main;
        main.startColor = fxSettings.particleColor;
        main.startLifetime = fxSettings.particleLifetime;
        main.maxParticles = fxSettings.particleCount;
        
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    
    private void CreateParticleSystem()
    {
        GameObject particleObject = new GameObject("DashParticles");
        particleObject.transform.SetParent(transform);
        particleObject.transform.localPosition = Vector3.zero;
        
        particles = particleObject.AddComponent<ParticleSystem>();
        
        // Configure basic particle settings
        var main = particles.main;
        main.startLifetime = fxSettings.particleLifetime;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = fxSettings.particleColor;
        main.maxParticles = fxSettings.particleCount;
        
        var emission = particles.emission;
        emission.enabled = false; // We'll trigger manually
        
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.5f, 0.5f, 0.1f);
        
        if (debugMode)
            Debug.Log($"[DashFX] Created ParticleSystem on {name}");
    }
    
    private Material CreateURPTrailMaterial()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit") ??
                          Shader.Find("Universal Render Pipeline/Lit") ??
                          Shader.Find("Sprites/Default");
        
        if (urpShader == null)
        {
            if (debugMode)
                Debug.LogWarning("[DashFX] No suitable URP shader found, using fallback");
            return new Material(Shader.Find("Standard"));
        }
        
        Material trailMat = new Material(urpShader);
        
        if (urpShader.name.Contains("Universal Render Pipeline/Unlit"))
        {
            trailMat.SetColor("_BaseColor", Color.white);
            trailMat.SetFloat("_Surface", 1);
            trailMat.SetFloat("_Blend", 0);
            trailMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            trailMat.SetInt("_ZWrite", 0);
            trailMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            trailMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        
        return trailMat;
    }
    #endregion
    
    #region Public Interface
    /// <summary>
    /// Start all enabled dash effects
    /// </summary>
    public void StartDashEffects()
    {
        if (isEffectActive) return;
        
        isEffectActive = true;
        
        if (fxSettings.enableTrail && trailRenderer != null)
            StartTrailEffect();
            
        if (fxSettings.enableAudio && audioSource != null)
            PlayDashAudio();
            
        if (fxSettings.enableParticles && particles != null)
            StartParticleEffect();
            
        if (fxSettings.enableScreenShake)
            TriggerScreenShake();
            
        if (fxSettings.enableFlash)
            StartFlashEffect();
        
        OnEffectStarted?.Invoke();
        
        if (debugMode)
            Debug.Log($"[DashFX] Started dash effects on {name}");
    }
    
    /// <summary>
    /// Stop all dash effects with smooth transitions
    /// </summary>
    public void StopDashEffects()
    {
        if (!isEffectActive) return;
        
        if (fxSettings.enableTrail && trailRenderer != null)
            StopTrailEffect();
            
        if (fxSettings.enableParticles && particles != null)
            StopParticleEffect();
        
        isEffectActive = false;
        OnEffectEnded?.Invoke();
        
        if (debugMode)
            Debug.Log($"[DashFX] Stopped dash effects on {name}");
    }
    
    /// <summary>
    /// Update settings at runtime
    /// </summary>
    public void UpdateSettings(DashFXSettings newSettings)
    {
        fxSettings = newSettings;
        ApplyCurrentSettings();
    }
    
    /// <summary>
    /// Apply current settings to all components
    /// </summary>
    public void ApplyCurrentSettings()
    {
        if (fxSettings.enableTrail && trailRenderer != null)
            ConfigureTrailRenderer();
            
        if (fxSettings.enableAudio && audioSource != null)
            ConfigureAudioSource();
            
        if (fxSettings.enableParticles && particles != null)
            ConfigureParticleSystem();
    }
    
    /// <summary>
    /// Force stop all effects immediately (useful for cleanup)
    /// </summary>
    public void ForceStopAllEffects()
    {
        if (trailFadeCoroutine != null)
        {
            StopCoroutine(trailFadeCoroutine);
            trailFadeCoroutine = null;
        }
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.startColor = originalTrailStartColor;
            trailRenderer.endColor = originalTrailEndColor;
        }
        
        if (particles != null)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        isEffectActive = false;
    }
    #endregion
    
    #region Effect Implementations
    private void StartTrailEffect()
    {
        trailRenderer.Clear();
        trailRenderer.emitting = true;
    }
    
    private void StopTrailEffect()
    {
        trailRenderer.emitting = false;
        
        if (trailFadeCoroutine != null)
            StopCoroutine(trailFadeCoroutine);
            
        trailFadeCoroutine = StartCoroutine(FadeTrail());
    }
    
    private void PlayDashAudio()
    {
        if (fxSettings.dashSound == null) return;
        
        audioSource.pitch = Random.Range(fxSettings.pitchMin, fxSettings.pitchMax);
        audioSource.PlayOneShot(fxSettings.dashSound, fxSettings.volume);
    }
    
    private void StartParticleEffect()
    {
        particles.Emit(fxSettings.particleCount);
    }
    
    private void StopParticleEffect()
    {
        var emission = particles.emission;
        emission.enabled = false;
    }
    
    private void TriggerScreenShake()
    {
        var screenShake = FindObjectOfType<Hanzo.Utils.ScreenShake>();
        if (screenShake != null)
        {
            screenShake.TriggerShake(fxSettings.shakeDuration, fxSettings.shakeIntensity);
        }
    }
    
    private void StartFlashEffect()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
            
        flashCoroutine = StartCoroutine(FlashEffect());
    }
    #endregion
    
    #region Coroutines
    private IEnumerator FadeTrail()
    {
        if (trailRenderer == null) yield break;
        
        float timer = 0f;
        Color startColorOriginal = fxSettings.trailStartColor;
        Color endColorOriginal = fxSettings.trailEndColor;
        
        while (timer < fxSettings.trailFadeOutTime)
        {
            float alpha = 1f - (timer / fxSettings.trailFadeOutTime);
            
            trailRenderer.startColor = new Color(
                startColorOriginal.r, startColorOriginal.g, startColorOriginal.b, 
                startColorOriginal.a * alpha);
            trailRenderer.endColor = new Color(
                endColorOriginal.r, endColorOriginal.g, endColorOriginal.b, 
                endColorOriginal.a * alpha);
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Restore original colors
        trailRenderer.startColor = startColorOriginal;
        trailRenderer.endColor = endColorOriginal;
        
        trailFadeCoroutine = null;
    }
    
    private IEnumerator FlashEffect()
    {
        // This would integrate with your UI flash system
        // For now, just a placeholder that could trigger UI flash
        GameEvents.OnScreenFlash?.Invoke(fxSettings.flashColor, fxSettings.flashDuration);
        
        yield return new WaitForSeconds(fxSettings.flashDuration);
        
        flashCoroutine = null;
    }
    #endregion
    
    #region Editor Utilities
    #if UNITY_EDITOR
    [ContextMenu("Test Dash Effects")]
    private void TestDashEffects()
    {
        if (Application.isPlaying)
        {
            StartDashEffects();
            StartCoroutine(TestEffectsSequence());
        }
    }
    
    private IEnumerator TestEffectsSequence()
    {
        yield return new WaitForSeconds(1f);
        StopDashEffects();
    }
    
    [ContextMenu("Force Stop All Effects")]
    private void TestForceStop()
    {
        if (Application.isPlaying)
            ForceStopAllEffects();
    }
    #endif
    #endregion
    
    void OnDestroy()
    {
        ForceStopAllEffects();
    }
}

// Extension for easier integration with existing systems
public static class DashFXExtensions
{
    /// <summary>
    /// Get or add DashFXController to a GameObject
    /// </summary>
    public static DashFXController GetOrAddDashFX(this GameObject gameObject)
    {
        var dashFX = gameObject.GetComponent<DashFXController>();
        if (dashFX == null)
            dashFX = gameObject.AddComponent<DashFXController>();
        return dashFX;
    }
}