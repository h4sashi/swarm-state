using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpConfig config;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.5f;
    
    private Vector3 startPosition;
    private PowerUpManager powerUpManager;
    private Renderer[] renderers; // Cache all renderers
    private Material[] originalMaterials; // Store original materials
    
    void Start()
    {
        startPosition = transform.position;
        powerUpManager = FindObjectOfType<PowerUpManager>();
        
        // Cache renderers and materials to preserve originals
        CacheOriginalMaterials();
        
        // Setup visual enhancements while preserving originals
        SetupVisualEffects();
        
        // Make sure trigger is enabled
        GetComponent<Collider>().isTrigger = true;
    }
    
    private void CacheOriginalMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
            
            // Validate URP shader compatibility
            ValidateShaderCompatibility(renderers[i]);
        }
    }
    
    private void ValidateShaderCompatibility(Renderer renderer)
    {
        Material mat = renderer.material;
        
        // Check if shader is URP compatible
        if (mat.shader.name.Contains("Standard") && !mat.shader.name.Contains("Universal"))
        {
            Debug.LogWarning($"PowerUp '{name}': Material uses Built-in shader '{mat.shader.name}'. " +
                           "Consider upgrading to URP shader for best compatibility.");
            
            // Attempt automatic conversion to URP Lit shader
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null)
            {
                // Create new material with URP shader while preserving properties
                Material newMat = new Material(urpShader);
                
                // Copy common properties
                if (mat.HasProperty("_MainTex"))
                    newMat.SetTexture("_BaseMap", mat.GetTexture("_MainTex"));
                if (mat.HasProperty("_Color"))
                    newMat.SetColor("_BaseColor", mat.GetColor("_Color"));
                if (mat.HasProperty("_EmissionColor"))
                {
                    newMat.SetColor("_EmissionColor", mat.GetColor("_EmissionColor"));
                    newMat.EnableKeyword("_EMISSION");
                }
                
                renderer.material = newMat;
                Debug.Log($"Auto-converted material to URP shader for '{name}'");
            }
        }
    }
    
    void Update()
    {
        // Rotate and bob for visual appeal
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        Vector3 pos = startPosition;
        pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = pos;
    }
    
    private void SetupVisualEffects()
    {
        if (!config) return;
        
        // Only enhance if glow color is specified and not default
        if (config.glowColor != Color.clear && config.glowColor != Color.white)
        {
            ApplyGlowEffect();
        }
        
        // Instantiate additional effects if specified
        if (config.activeEffect != null)
        {
            GameObject effect = Instantiate(config.activeEffect, transform);
            effect.transform.localPosition = Vector3.zero;
        }
    }
    
    private void ApplyGlowEffect()
    {
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            
            // Apply glow enhancement based on current shader
            if (mat.shader.name.Contains("Universal") || mat.shader.name.Contains("URP"))
            {
                // URP Lit shader properties
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", config.glowColor * 0.5f);
                    mat.EnableKeyword("_EMISSION");
                }
                
                // Enhance base color slightly
                if (mat.HasProperty("_BaseColor"))
                {
                    Color baseColor = mat.GetColor("_BaseColor");
                    Color enhancedColor = Color.Lerp(baseColor, config.glowColor, 0.3f);
                    mat.SetColor("_BaseColor", enhancedColor);
                }
            }
            else if (mat.shader.name.Contains("Standard"))
            {
                // Built-in Standard shader properties
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", config.glowColor * 0.5f);
                    mat.EnableKeyword("_EMISSION");
                }
                
                if (mat.HasProperty("_Color"))
                {
                    Color baseColor = mat.GetColor("_Color");
                    Color enhancedColor = Color.Lerp(baseColor, config.glowColor, 0.3f);
                    mat.SetColor("_Color", enhancedColor);
                }
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectPowerUp();
        }
    }
    
    private void CollectPowerUp()
    {
        if (powerUpManager != null)
        {
            bool success = powerUpManager.ActivatePowerUp(config);
            
            if (success)
            {
                // Play pickup effects
                if (config.pickupEffect != null)
                {
                    Instantiate(config.pickupEffect, transform.position, transform.rotation);
                }

                SoundManager.Instance.PlayCollectibleSound();
                // Trigger screen flash
                GameEvents.OnScreenFlash?.Invoke(config.glowColor, 0.1f);
                
                Debug.Log($"Collected power-up: {config.displayName}");
                
                Destroy(gameObject);
            }
        }
    }
    
    public void Initialize(PowerUpConfig config)
    {
        this.config = config;
        
        // Re-setup visuals if initialized at runtime
        if (Application.isPlaying && config != null)
        {
            StartCoroutine(DelayedSetup());
        }
    }
    
    // Delayed setup to ensure all components are ready
    private System.Collections.IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        
        CacheOriginalMaterials();
        SetupVisualEffects();
    }
}