using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Core
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour, IEnemyMovement
    {
        private NavMeshAgent agent;
        private EnemyConfig config;
        private Vector3 lastTargetPosition;
        private float pathUpdateTimer;
        private const float PATH_UPDATE_FREQUENCY = 0.1f; // Update path 10 times per second
        
        [Header("NavMesh Settings")]
        [SerializeField] private bool enablePathOptimization = true;
        [SerializeField] private float pathfindingTimeout = 5f;
        [SerializeField] private bool showDebugPath = false;
        
        public float MovementSpeed 
        { 
            get => agent ? agent.speed : 0f;
            set 
            { 
                if (agent != null) 
                {
                    agent.speed = Mathf.Max(0.1f, value);
                }
            }
        }
        
        public Vector3 Velocity => agent ? agent.velocity : Vector3.zero;
        public bool IsMoving => agent && agent.hasPath && agent.remainingDistance > 0.1f;
        public bool HasPath => agent && agent.hasPath;
        public float RemainingDistance => agent ? agent.remainingDistance : float.MaxValue;
        public bool PathPending => agent && agent.pathPending;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ConfigureAgent();
        }
        
        private void Update()
        {
            if (enablePathOptimization)
            {
                OptimizePathfinding();
            }
            
            if (showDebugPath && agent && agent.hasPath)
            {
                DrawDebugPath();
            }
        }
        
        private void ConfigureAgent()
        {
            if (agent == null) return;
            
            // Basic configuration
            agent.updateRotation = true;
            agent.updateUpAxis = false;
            agent.autoBraking = false; // For continuous chasing behavior
        }
        
        public void Initialize(EnemyConfig config)
        {
            this.config = config;
            
            if (agent != null && config != null)
            {
                agent.speed = config.moveSpeed;
                agent.acceleration = config.acceleration;
                agent.angularSpeed = config.rotationSpeed;
                agent.stoppingDistance = config.stopDistance;
            }
        }
        
        public void MoveTo(Vector3 target)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }
            
            // Only update path if target has moved significantly or enough time has passed
            if (ShouldUpdatePath(target))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(target, path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
                    {
                        agent.SetPath(path);
                        lastTargetPosition = target;
                        pathUpdateTimer = 0f;
                    }
                }
            }
        }
        
        private bool ShouldUpdatePath(Vector3 target)
        {
            // Update path if:
            // 1. No current path
            // 2. Target moved significantly
            // 3. Enough time has passed for regular updates
            // 4. Current path is complete but we haven't reached the target
            
            if (!agent.hasPath) return true;
            
            float distanceMoved = Vector3.Distance(target, lastTargetPosition);
            pathUpdateTimer += Time.deltaTime;
            
            return distanceMoved > 2f || 
                   pathUpdateTimer >= PATH_UPDATE_FREQUENCY ||
                   (agent.remainingDistance < 0.5f && Vector3.Distance(transform.position, target) > 2f);
        }
        
        public void Stop()
        {
            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }
        
        public void SetDestination(Vector3 destination)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                lastTargetPosition = destination;
            }
        }
        
        private void OptimizePathfinding()
        {
            if (agent == null || !agent.enabled) return;
            
            // Handle agent falling off NavMesh
            if (!agent.isOnNavMesh)
            {
                TryReturnToNavMesh();
                return;
            }
            
            // Handle stuck situations
            if (IsAgentStuck())
            {
                HandleStuckAgent();
            }
        }
        
        private void TryReturnToNavMesh()
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                if (agent.enabled)
                {
                    agent.Warp(hit.position);
                }
            }
        }
        
        private bool IsAgentStuck()
        {
            return agent.velocity.magnitude < 0.1f && 
                   agent.hasPath && 
                   agent.remainingDistance > 1f &&
                   !agent.pathPending;
        }
        
        private void HandleStuckAgent()
        {
            // Try to recalculate path
            if (lastTargetPosition != Vector3.zero)
            {
                agent.ResetPath();
                StartCoroutine(DelayedPathRecalculation());
            }
        }
        
        private System.Collections.IEnumerator DelayedPathRecalculation()
        {
            yield return new WaitForSeconds(0.1f);
            MoveTo(lastTargetPosition);
        }
        
        private void DrawDebugPath()
        {
            if (agent.path.corners.Length < 2) return;
            
            for (int i = 1; i < agent.path.corners.Length; i++)
            {
                Debug.DrawLine(agent.path.corners[i - 1], agent.path.corners[i], Color.blue, 0.1f);
            }
        }
        
        public NavMeshAgent GetAgent() => agent;
        
        private void OnDrawGizmosSelected()
        {
            if (agent == null || !agent.hasPath) return;
            
            Gizmos.color = Color.blue;
            Vector3[] corners = agent.path.corners;
            
            for (int i = 1; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i - 1], corners[i]);
                Gizmos.DrawWireSphere(corners[i], 0.2f);
            }
        }
    }
}
