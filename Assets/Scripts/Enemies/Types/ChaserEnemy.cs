using UnityEngine;
using UnityEngine.AI;
using Enemies.Core;

namespace Enemies.Types
{   
    // Chaser Enemy - Relentless pursuer with NavMesh pathfinding
    [RequireComponent(typeof(EnemyController), typeof(NavMeshAgent))]
    public class ChaserEnemy : MonoBehaviour, IEnemyAttack
    {
        [Header("Chaser-Specific Settings")]
        [SerializeField] private float aggressionLevel = 1.2f;
        [SerializeField] private bool enableLungeAttack = true;
        [SerializeField] private float lungeForce = 10f;
        [SerializeField] private bool showDebugInfo = false;
        
        private EnemyConfig config;
        private NavMeshAgent agent;
        private float lastAttackTime;
        private bool isLunging = false;
        
        public float AttackRange => config ? config.attackRange : 2f;
        public float AttackCooldown => config ? config.attackCooldown : 1f;
        public bool CanAttack => Time.time - lastAttackTime >= AttackCooldown && !isLunging;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ConfigureNavMeshAgent();
        }
        
        private void Start()
        {
            var controller = GetComponent<EnemyController>();
            config = controller.Config;
            
            // Apply aggression multiplier to movement speed
            if (config != null && agent != null)
            {
                agent.speed = config.moveSpeed * aggressionLevel;
                agent.acceleration = config.acceleration * 2f; // Faster acceleration for responsiveness
            }
        }
        
        private void ConfigureNavMeshAgent()
        {
            if (agent == null) return;
            
            // Configure for aggressive chasing behavior
            agent.updateRotation = true;
            agent.updateUpAxis = false;
            agent.stoppingDistance = 1f; // Close pursuit
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            
            // Aggressive pathfinding settings
            agent.angularSpeed = 720f; // Fast turning
            agent.autoBraking = false; // Don't slow down when approaching
        }
        
        public void Attack(Vector3 targetPosition)
        {
            if (!CanAttack) return;
            
            lastAttackTime = Time.time;
            
            if (showDebugInfo)
            {
                Debug.Log($"[ChaserEnemy] {name} attacking at position {targetPosition}!");
            }
            
            if (enableLungeAttack)
            {
                StartCoroutine(ExecuteLungeAttack(targetPosition));
            }
            else
            {
                StartCoroutine(ExecuteStandardAttack());
            }
        }
        
        private System.Collections.IEnumerator ExecuteLungeAttack(Vector3 targetPosition)
        {
            isLunging = true;
            
            // Stop NavMesh movement temporarily
            if (agent.enabled)
            {
                agent.enabled = false;
            }
            
            // Calculate lunge direction
            Vector3 lungeDirection = (targetPosition - transform.position).normalized;
            
            // Apply lunge force
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(lungeDirection * lungeForce, ForceMode.Impulse);
            }
            
            // Visual feedback
            yield return StartCoroutine(LungeVisualFeedback());
            
            // Re-enable NavMesh after lunge
            yield return new WaitForSeconds(0.3f);
            
            if (!agent.enabled && agent.isOnNavMesh)
            {
                agent.enabled = true;
            }
            else if (!agent.isOnNavMesh)
            {
                // Warp back to NavMesh if we fell off
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    agent.enabled = true;
                }
            }
            
            isLunging = false;
        }
        
        private System.Collections.IEnumerator ExecuteStandardAttack()
        {
            yield return StartCoroutine(AttackVisualFeedback());
        }
        
        private System.Collections.IEnumerator LungeVisualFeedback()
        {
            Vector3 originalScale = transform.localScale;
            
            // Wind up
            for (float t = 0; t < 0.1f; t += Time.deltaTime)
            {
                float scale = Mathf.Lerp(1f, 0.8f, t / 0.1f);
                transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // Lunge effect
            for (float t = 0; t < 0.2f; t += Time.deltaTime)
            {
                float scale = Mathf.Lerp(0.8f, 1.3f, t / 0.2f);
                transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // Return to normal
            for (float t = 0; t < 0.1f; t += Time.deltaTime)
            {
                float scale = Mathf.Lerp(1.3f, 1f, t / 0.1f);
                transform.localScale = originalScale * scale;
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
        
        private System.Collections.IEnumerator AttackVisualFeedback()
        {
            Vector3 originalScale = transform.localScale;
            
            // Quick attack animation
            transform.localScale = originalScale * 1.15f;
            yield return new WaitForSeconds(0.1f);
            transform.localScale = originalScale;
        }
        
        public NavMeshAgent GetNavMeshAgent() => agent;
        public bool IsLunging => isLunging;
        
        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo) return;
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
            
            // Draw lunge preview if enabled
            if (enableLungeAttack)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawRay(transform.position, transform.forward * (AttackRange + 2f));
            }
        }
    }
}