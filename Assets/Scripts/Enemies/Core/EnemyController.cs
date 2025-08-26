using UnityEngine;
using UnityEngine.AI;
using Enemies.States;
using Enemies.Types;
namespace Enemies.Core
{
    [RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth), typeof(EnemyStateMachine))]
    [RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour, IEnemy
    {
        [Header("Configuration")]
        [SerializeField] private EnemyConfig config;

        [Header("Components")]
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Chaser Settings")]
        [SerializeField] private bool immediatePlayerTargeting = true;
        [SerializeField] private float playerSearchRadius = 50f;

        // Interfaces for polymorphism
        public IEnemyMovement Movement => movement;
        public IEnemyAttack Attack { get; private set; }

        // IEnemy implementation
        public EnemyType EnemyType => config ? config.enemyType : EnemyType.Chaser;
        public EnemyState CurrentState => stateMachine ? stateMachine.CurrentState : EnemyState.Idle;
        public Transform Target { get; set; }
        public bool IsActive { get; private set; } = true;
        public bool IsAlive => health ? health.IsAlive : false;
        public EnemyConfig Config => config;

        private void Awake()
        {
            Debug.Log($"[EnemyController] Awake called for {name}");

            // Auto-assign components
            if (!movement) movement = GetComponent<EnemyMovement>();
            if (!health) health = GetComponent<EnemyHealth>();
            if (!stateMachine) stateMachine = GetComponent<EnemyStateMachine>();
            if (!navMeshAgent) navMeshAgent = GetComponent<NavMeshAgent>();

            // Get attack component - try multiple approaches
            Attack = GetComponent<IEnemyAttack>();

            if (Attack == null)
            {
                Debug.LogWarning($"[EnemyController] {name}: IEnemyAttack not found via GetComponent, trying specific types...");

                // Try to find specific attack components
                var shooterAttack = GetComponent<ShooterEnemy>();
                if (shooterAttack != null)
                {
                    Attack = shooterAttack;
                    Debug.Log($"[EnemyController] {name}: Found ShooterEnemy attack component");
                }

                // Add other attack types here if needed
                // var meleeAttack = GetComponent<MeleeEnemy>();
                // if (meleeAttack != null) Attack = meleeAttack;
            }

            if (Attack != null)
            {
                Debug.Log($"[EnemyController] {name}: Attack component found: {Attack.GetType().Name}");
            }
            else
            {
                Debug.LogError($"[EnemyController] {name}: No IEnemyAttack component found! Make sure enemy has appropriate attack component.");
            }
        }

        // Add this method to your EnemyController class for debugging
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DiagnoseComponents()
        {
            Debug.Log($"=== DIAGNOSTIC FOR {name} ===");
            Debug.Log($"Config: {(config != null ? config.name : "NULL")}");
            Debug.Log($"Movement: {(movement != null ? "OK" : "NULL")}");
            Debug.Log($"Health: {(health != null ? "OK" : "NULL")}");
            Debug.Log($"StateMachine: {(stateMachine != null ? "OK" : "NULL")}");
            Debug.Log($"NavMeshAgent: {(navMeshAgent != null ? "OK" : "NULL")}");
            Debug.Log($"Attack: {(Attack != null ? "OK" : "NULL")}");
            Debug.Log($"Target: {(Target != null ? Target.name : "NULL")}");
            Debug.Log($"IsActive: {IsActive}");
            Debug.Log($"IsAlive: {IsAlive}");
            Debug.Log($"EnemyType: {EnemyType}");

            if (navMeshAgent != null)
            {
                Debug.Log($"NavMeshAgent enabled: {navMeshAgent.enabled}");
                Debug.Log($"NavMeshAgent isOnNavMesh: {navMeshAgent.isOnNavMesh}");
                Debug.Log($"NavMeshAgent hasPath: {navMeshAgent.hasPath}");
            }

            Debug.Log("=== END DIAGNOSTIC ===");
        }

        // Call this in Start() method for debugging
        private void Start()
        {
            if (config)
            {
                Initialize(config);

                 // Subscribe to death
                if (health != null)
                {
                    health.OnDeath += HandleDeath;
                }
                

                // Add diagnostic call for debugging
#if UNITY_EDITOR
                DiagnoseComponents();
#endif
            }
            else
            {
                Debug.LogError($"[EnemyController] {name}: No config assigned!");
            }
        }

           private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
        }

         private void HandleDeath(EnemyController enemy)
        {
            // Run delayed cleanup (let death anim play)
            StartCoroutine(DeathCleanupRoutine());
        }

         private System.Collections.IEnumerator DeathCleanupRoutine()
        {
            if (config != null)
            {
                yield return new WaitForSeconds(config.deathDelay);
            }
            Deactivate();

            // Fire global event so spawner knows
            GameEvents.OnEnemyKilled?.Invoke(gameObject);
        }

        public void Initialize(EnemyConfig config)
        {
            this.config = config;

            // Initialize components
            movement?.Initialize(config);
            health?.Initialize(config);

            // Setup visual
            SetupVisual();

            Debug.Log($"[EnemyController] Initializing {config.enemyType} enemy: {name}");

            // Both Chaser and Shooter enemies should immediately find and target player
            if ((config.enemyType == EnemyType.Chaser || config.enemyType == EnemyType.Shooter) && immediatePlayerTargeting)
            {
                FindAndTargetPlayer();

                // Start chasing immediately if player found
                if (Target != null)
                {
                    Debug.Log($"[EnemyController] {config.enemyType} {name} starting immediate chase of {Target.name}");
                    stateMachine.ChangeState(EnemyState.Chase);
                }
                else
                {
                    Debug.LogWarning($"[EnemyController] {config.enemyType} {name} could not find player to target immediately");
                }
            }
            else
            {
                // For other enemy types, use normal detection
                FindPlayerIfInRange();
            }

            IsActive = true;
        }


        private void FindAndTargetPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetTarget(player.transform);
                Debug.Log($"[EnemyController] Chaser {name} immediately targeting player: {player.name}");
            }
            else
            {
                // Search in area if player not found by tag
                Collider[] playersInArea = Physics.OverlapSphere(transform.position, playerSearchRadius, LayerMask.GetMask("Player"));
                if (playersInArea.Length > 0)
                {
                    SetTarget(playersInArea[0].transform);
                    Debug.Log($"[EnemyController] Chaser {name} found player in search area: {playersInArea[0].name}");
                }
            }
        }

        private void FindPlayerIfInRange()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player && config)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= config.detectionRange)
                {
                    SetTarget(player.transform);
                }
            }
        }

        public void SetTarget(Transform target)
        {
            Target = target;

            // If this is a Chaser and we just got a target, start chasing immediately
            if (target != null && config.enemyType == EnemyType.Chaser && CurrentState == EnemyState.Idle)
            {
                stateMachine.ChangeState(EnemyState.Chase);
            }
        }

        public bool CanSeeTarget()
        {
            if (!Target || !config) return false;

            float effectiveDetectionRange;

            // Both Chasers and Shooters are aggressive and can "see" the player from far away
            if (config.enemyType == EnemyType.Chaser)
            {
                effectiveDetectionRange = config.detectionRange * 3f;
            }
            else if (config.enemyType == EnemyType.Shooter)
            {
                // Shooters should also have extended detection for chasing, but will stop at attack range
                effectiveDetectionRange = config.detectionRange * 2.5f;
            }
            else
            {
                effectiveDetectionRange = config.detectionRange;
            }

            float distance = Vector3.Distance(transform.position, Target.position);
            bool canSee = distance <= effectiveDetectionRange;

            if (config.enemyType == EnemyType.Shooter || config.enemyType == EnemyType.Chaser)
            {
                Debug.Log($"[CanSeeTarget] {config.enemyType} {name}: Distance={distance:F1}, Range={effectiveDetectionRange:F1}, CanSee={canSee}");
            }

            return canSee;
        }

        public void Deactivate()
        {
            IsActive = false;
            gameObject.SetActive(false);
        }

        public void TakeDamage(float damage)
        {
            health?.TakeDamage(damage);
        }

        private void SetupVisual()
        {
            if (!config) return;

            // Set scale
            transform.localScale = config.scale;

            // Don't override materials - let prefabs use their assigned materials
            // If you need color customization, add it to the EnemyConfig as an optional override

            // Set layer and tag
            gameObject.layer = LayerMask.NameToLayer("Enemy");
            gameObject.tag = "Enemy";
        }

        private void OnDrawGizmosSelected()
        {
            if (!config) return;

            // Draw detection range (larger for Chasers)
            float effectiveDetectionRange = config.enemyType == EnemyType.Chaser
                ? config.detectionRange * 3f
                : config.detectionRange;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, effectiveDetectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, config.attackRange);

            // Draw player search radius for Chasers
            if (config.enemyType == EnemyType.Chaser)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, playerSearchRadius);
            }
        }
    }
}