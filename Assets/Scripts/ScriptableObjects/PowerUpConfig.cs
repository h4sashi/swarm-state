using UnityEngine;

// Configuration for power-ups via Scriptable Objects

[CreateAssetMenu(fileName = "PowerUpConfig", menuName = "Hanzo/PowerUp Config")]
public class PowerUpConfig : ScriptableObject
{
    [Header("Basic Settings")]
    public PowerUpType powerUpType;
    public string displayName = "Power Up";
    public Sprite icon;
    
    [Header("Duration & Stacking")]
    public float duration = 10f;
    public bool isStackable = true;
    public int maxStacks = 3;
    public bool stacksExtendDuration = true; // If false, stacks increase effect instead
    
    [Header("Effect Values")]
    public float speedMultiplier = 1.5f;
    public float dashCooldownReduction = 0.5f;
    public int bonusDashCharges = 1;
    public float shieldDuration = 5f;
    public int healthRegenAmount = 1;
    public float healthRegenInterval = 2f;
    
    [Header("Visual")]
    public GameObject pickupEffect;
    public GameObject activeEffect;

    public Color glowColor = Color.cyan;
    
    [Header("Spawn Settings")]
    public float spawnChance = 0.3f;
    public GameObject prefab;
}