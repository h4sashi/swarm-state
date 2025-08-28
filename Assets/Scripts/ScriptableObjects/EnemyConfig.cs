using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Hanzo/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Basic Info")]
    public EnemyType enemyType;
    public string enemyName = "Enemy";
    
    [Header("Health")]
    public int maxHealth = 1;
    public float deathDelay = 0.5f;
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float acceleration = 8f;
    public float rotationSpeed = 180f;
    public float stopDistance = 0.5f;
    
    [Header("Detection")]
    public float detectionRange = 8f;
    public float loseTargetRange = 12f;
    public LayerMask targetLayers = 1 << 7; // Player layer
    
    [Header("Attack")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public float attackDamage = 1f;
    
    [Header("AI Behavior")]
    public float stateChangeDelay = 0.1f;
    public float idleTime = 2f;
    public bool canPatrol = false;
    public float patrolRadius = 5f;
    
    [Header("Visual")]
    public Color enemyColor = Color.red;
    public Vector3 scale = Vector3.one;
}

