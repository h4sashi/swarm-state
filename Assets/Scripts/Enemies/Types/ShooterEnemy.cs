using UnityEngine;
using Enemies.Core;
using Enemies.Weapons;

namespace Enemies.Types
{
    [RequireComponent(typeof(EnemyController))]
    public class ShooterEnemy : MonoBehaviour, IEnemyAttack
    {
        [Header("Shooting Config")]
        [SerializeField] private ProjectileConfig projectileConfig;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float keepDistanceRange = 5f;
        [SerializeField] private float optimalShootingRange = 6f;
        
        private EnemyConfig config;
        private float lastAttackTime;
        private ProjectilePool projectilePool;
        private EnemyController controller;
        private bool isInitialized = false;
        
        public float AttackRange => config ? config.attackRange : 6f;
        public float AttackCooldown => config ? config.attackCooldown : 1.5f;
        public bool CanAttack => Time.time - lastAttackTime >= AttackCooldown;
        
        void Awake()
        {
            Debug.Log($"[ShooterEnemy] Awake called for {name}");
            controller = GetComponent<EnemyController>();
            if (controller == null)
            {
                Debug.LogError($"[ShooterEnemy] {name}: No EnemyController found!");
            }
        }
        
        void Start()
        {
            InitializeShooter();
            
            #if UNITY_EDITOR
            DiagnoseShooter();
            #endif
        }
        
        private void InitializeShooter()
        {
            if (controller != null)
            {
                config = controller.Config;
                Debug.Log($"[ShooterEnemy] {name}: Config assigned: {(config != null ? config.name : "NULL")}");
            }
            
            // Create fire point if not assigned
            if (!firePoint)
            {
                GameObject firePointObj = new GameObject("FirePoint");
                firePointObj.transform.SetParent(transform);
                firePointObj.transform.localPosition = Vector3.forward * 0.6f;
                firePoint = firePointObj.transform;
                Debug.Log($"[ShooterEnemy] {name}: Created fire point");
            }
            
            // Get or create projectile pool
            projectilePool = FindObjectOfType<ProjectilePool>();
            if (!projectilePool)
            {
                Debug.LogWarning($"[ShooterEnemy] {name}: No ProjectilePool found! Creating one...");
                GameObject poolObj = new GameObject("ProjectilePool");
                projectilePool = poolObj.AddComponent<ProjectilePool>();
                if (projectileConfig != null)
                {
                    projectilePool.Initialize(projectileConfig);
                }
            }
            
            isInitialized = true;
            Debug.Log($"[ShooterEnemy] {name}: Initialization complete. Pool: {projectilePool != null}, Config: {projectileConfig != null}");
        }
        
        void Update()
        {
            if (isInitialized && controller != null)
            {
                HandleShooterBehavior();
            }
        }
        
        private void HandleShooterBehavior()
        {
            if (!controller.Target) return;
            
            float distanceToTarget = Vector3.Distance(transform.position, controller.Target.position);
            
            // Only apply kiting behavior when in Attack state (already in shooting range)
            if (controller.CurrentState == EnemyState.Attack)
            {
                // If too close, move away to maintain optimal distance
                if (distanceToTarget < keepDistanceRange)
                {
                    Vector3 awayDirection = (transform.position - controller.Target.position).normalized;
                    Vector3 retreatPosition = transform.position + awayDirection * 2f;
                    controller.Movement.MoveTo(retreatPosition);
                }
                // If too far for optimal shooting, move a bit closer
                else if (distanceToTarget > optimalShootingRange && distanceToTarget <= AttackRange)
                {
                    Vector3 towardDirection = (controller.Target.position - transform.position).normalized;
                    Vector3 approachPosition = transform.position + towardDirection * 1f;
                    controller.Movement.MoveTo(approachPosition);
                }
                else
                {
                    // At optimal distance, stop moving and focus on shooting
                    controller.Movement.Stop();
                }
            }
        }
        
      public void Attack(Vector3 targetPosition)
{
    if (!CanAttack)
    {
        Debug.Log($"[ShooterEnemy] {name}: Attack on cooldown");
        return;
    }
    
    // Basic validation
    if (!isInitialized)
    {
        Debug.LogError($"[ShooterEnemy] {name}: Not initialized, cannot attack!");
        return;
    }
    
    if (projectilePool == null)
    {
        Debug.LogError($"[ShooterEnemy] {name}: ProjectilePool is null!");
        return;
    }
    
    if (firePoint == null)
    {
        Debug.LogError($"[ShooterEnemy] {name}: FirePoint is null!");
        return;
    }
    
    if (projectileConfig == null)
    {
        Debug.LogError($"[ShooterEnemy] {name}: ProjectileConfig is null!");
        return;
    }
    
    lastAttackTime = Time.time;
    
    try
    {
        // Calculate direction to target
        Vector3 direction = CalculateAttackDirection(targetPosition);
        
        // Get projectile from pool
        EnemyProjectile projectile = projectilePool.GetProjectile();
        
        // CRITICAL: Explicit null check with detailed logging
        if (projectile == null)
        {
            Debug.LogError($"[ShooterEnemy] {name}: ProjectilePool.GetProjectile() returned NULL! Pool may be exhausted or broken.");
            return;
        }
        
        // Additional safety check - ensure the projectile's gameObject exists
        if (projectile.gameObject == null)
        {
            Debug.LogError($"[ShooterEnemy] {name}: Projectile component exists but its gameObject is null!");
            return;
        }
        
        // NOW it's safe to initialize the projectile
        projectile.Initialize(firePoint.position, direction, projectileConfig);
        Debug.Log($"[ShooterEnemy] {name}: Successfully fired projectile at {targetPosition}");
        
        // Visual feedback
        StartCoroutine(MuzzleFlash());
    }
    catch (System.NullReferenceException ex)
    {
        Debug.LogError($"[ShooterEnemy] {name}: NULL REFERENCE during attack!");
        Debug.LogError($"- ProjectilePool exists: {projectilePool != null}");
        Debug.LogError($"- ProjectileConfig exists: {projectileConfig != null}"); 
        Debug.LogError($"- FirePoint exists: {firePoint != null}");
        Debug.LogError($"- Controller exists: {controller != null}");
        Debug.LogError($"- Exception: {ex.Message}");
        Debug.LogError($"- Stack trace: {ex.StackTrace}");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[ShooterEnemy] {name}: EXCEPTION during attack: {ex.Message}");
        Debug.LogError($"- Stack trace: {ex.StackTrace}");
    }
}

// Enhanced validation method
private bool ValidateAttackPreconditions(Vector3 targetPosition)
{
    if (!isInitialized)
    {
        Debug.LogError($"[ShooterEnemy] {name}: Not initialized, cannot attack!");
        return false;
    }
    
    if (projectilePool == null)
    {
        Debug.LogError($"[ShooterEnemy] {name}: ProjectilePool is null!");
        return false;
    }
    
    if (firePoint == null)
    {
        Debug.LogError($"[ShooterEnemy] {name}: FirePoint is null!");
        return false;
    }
    
    if (projectileConfig == null)
    {
        Debug.LogError($"[ShooterEnemy] {name}: ProjectileConfig is null!");
        return false;
    }
    
    // Check if pool has projectiles available
    try
    {
        // Test if pool can provide a projectile (peek without taking)
        var testProjectile = projectilePool.GetProjectile();
        if (testProjectile == null)
        {
            Debug.LogWarning($"[ShooterEnemy] {name}: ProjectilePool is empty or returning null!");
            return false;
        }
        else
        {
            // Return it immediately since this was just a test
            projectilePool.ReturnProjectile(testProjectile);
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[ShooterEnemy] {name}: Error testing projectile pool: {ex.Message}");
        return false;
    }
    
    return true;
}
        
    
        
        private Vector3 CalculateAttackDirection(Vector3 targetPosition)
        {
            // Calculate direction to target with slight prediction
            Vector3 direction = (targetPosition - firePoint.position).normalized;
            
            // Lead the target slightly if it's moving
            if (controller?.Target != null)
            {
                Rigidbody targetRb = controller.Target.GetComponent<Rigidbody>();
                if (targetRb != null && targetRb.linearVelocity.magnitude > 0.1f)
                {
                    float projectileSpeed = projectileConfig.speed;
                    float timeToTarget = Vector3.Distance(firePoint.position, targetPosition) / projectileSpeed;
                    Vector3 predictedPosition = targetPosition + targetRb.linearVelocity * timeToTarget;
                    direction = (predictedPosition - firePoint.position).normalized;
                }
            }
            
            return direction;
        }
        
        private System.Collections.IEnumerator MuzzleFlash()
        {
            if (firePoint != null)
            {
                try
                {
                    GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    flash.transform.position = firePoint.position;
                    flash.transform.localScale = Vector3.one * 0.3f;
                    
                    var renderer = flash.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = Color.yellow;
                    }
                    
                    var collider = flash.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Destroy(collider);
                    }
                    
                    Destroy(flash, 0.1f);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ShooterEnemy] {name}: Error creating muzzle flash: {ex.Message}");
                }
            }
            yield return null;
        }
        
        // Debug method
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DiagnoseShooter()
        {
            Debug.Log($"=== SHOOTER DIAGNOSTIC FOR {name} ===");
            Debug.Log($"Controller: {(controller != null ? "OK" : "NULL")}");
            Debug.Log($"Config: {(config != null ? config.name : "NULL")}");
            Debug.Log($"ProjectileConfig: {(projectileConfig != null ? projectileConfig.name : "NULL")}");
            Debug.Log($"ProjectilePool: {(projectilePool != null ? "OK" : "NULL")}");
            Debug.Log($"FirePoint: {(firePoint != null ? "OK" : "NULL")}");
            Debug.Log($"IsInitialized: {isInitialized}");
            Debug.Log($"AttackRange: {AttackRange}");
            Debug.Log($"AttackCooldown: {AttackCooldown}");
            Debug.Log($"CanAttack: {CanAttack}");
            Debug.Log("=== END SHOOTER DIAGNOSTIC ===");
        }
        
        // Additional validation method for debugging
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ValidateComponents()
        {
            if (controller == null) Debug.LogError($"[ShooterEnemy] {name}: Missing EnemyController!");
            if (projectileConfig == null) Debug.LogError($"[ShooterEnemy] {name}: Missing ProjectileConfig!");
            if (firePoint == null) Debug.LogError($"[ShooterEnemy] {name}: Missing FirePoint!");
            if (projectilePool == null) Debug.LogError($"[ShooterEnemy] {name}: Missing ProjectilePool!");
        }
    }

}