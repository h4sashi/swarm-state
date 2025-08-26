using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Hanzo/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 8f;
    
    [Header("Dash Settings")]
    [Range(10f, 50f)]
    public float dashSpeed = 30f;           // Units per second during dash
    [Range(0.1f, 0.5f)]
    public float dashDuration = 0.2f;
    [Range(0.5f, 3f)]
    public float dashCooldown = 1f;
    public AnimationCurve dashCurve = AnimationCurve.Constant(0, 1, 1); // Consistent speed
    
    [Header("Combat Settings")]
    public int dashDamage = 1;
    public float invulnerabilityDuration = 0.5f;
    
    [Header("Health Settings")]
    public int maxHealth = 3;
    
    // Helper to calculate expected dash distance
    [Header("Debug Info (Read Only)")]
    [SerializeField, Tooltip("Expected dash distance")] 
    private float expectedDashDistance;
    
    void OnValidate()
    {
        // Calculate average curve value for distance estimation
        float curveAverage = 0.7f; // Approximate for most curves
        expectedDashDistance = dashSpeed * dashDuration * curveAverage;
    }
}