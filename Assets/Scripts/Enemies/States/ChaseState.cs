using UnityEngine;
using UnityEngine.AI;
using Enemies.Core;
using Enemies.States;
using Enemies.Types;

public class ChaseState : EnemyStateBase
{
    private float lastPathUpdate;
    private float pathUpdateInterval = 0.1f;
    private Vector3 lastPlayerPosition;
    private bool hasInitialized = false;
    
    public ChaseState(EnemyController enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
    
    public override void EnterState()
    {
        // Null check for enemy
        if (enemy == null)
        {
            Debug.LogError("[ChaseState] Enemy is null on EnterState!");
            return;
        }
        
        hasInitialized = false;
        lastPathUpdate = 0f;
        
        Debug.Log($"[ChaseState] {enemy.name} ({enemy.EnemyType}) entering chase mode");
        
        // Both Chasers and Shooters start with aggressive pursuit
        if (enemy.EnemyType == EnemyType.Chaser || enemy.EnemyType == EnemyType.Shooter)
        {
            InitializeAggressiveChase();
        }
        
        // Visual feedback
        ApplyChaseVisualFeedback();
    }
    
    private void InitializeAggressiveChase()
    {
        if (enemy == null)
        {
            Debug.LogError("[ChaseState] Enemy is null in InitializeAggressiveChase!");
            return;
        }
        
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && enemy.Config != null)
        {
            agent.speed = enemy.Config.moveSpeed * 1.2f; // Speed boost during chase
            agent.acceleration = enemy.Config.acceleration * 1.5f;
            
            // Different stopping distances for different enemy types
            if (enemy.EnemyType == EnemyType.Shooter)
            {
                // Shooters stop further away to maintain shooting distance
                agent.stoppingDistance = enemy.Config.attackRange * 0.9f;
            }
            else
            {
                // Chasers get close for melee
                agent.stoppingDistance = enemy.Config.attackRange * 0.8f;
            }
            
            hasInitialized = true;
            Debug.Log($"[ChaseState] Initialized aggressive chase for {enemy.name}");
        }
        else
        {
            Debug.LogError($"[ChaseState] Missing components - Agent: {agent != null}, Config: {enemy.Config != null}");
        }
    }
    
    public override void UpdateState()
    {
        // Comprehensive null checks
        if (enemy == null)
        {
            Debug.LogError("[ChaseState] Enemy is null in UpdateState!");
            return;
        }
        
        if (stateMachine == null)
        {
            Debug.LogError("[ChaseState] StateMachine is null in UpdateState!");
            return;
        }
        
        if (enemy.Config == null)
        {
            Debug.LogError($"[ChaseState] {enemy.name} has null Config!");
            stateMachine.ChangeState(EnemyState.Idle);
            return;
        }
        
        if (enemy.Movement == null)
        {
            Debug.LogError($"[ChaseState] {enemy.name} has null Movement component!");
            stateMachine.ChangeState(EnemyState.Idle);
            return;
        }
        
        // Always pursue if target exists
        if (enemy.Target == null)
        {
            Debug.Log($"[ChaseState] {enemy.name} lost target, searching...");
            FindPlayerTarget();
            if (enemy.Target == null)
            {
                Debug.Log($"[ChaseState] {enemy.name} no target found, going idle");
                stateMachine.ChangeState(EnemyState.Idle);
                return;
            }
        }
        
        float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.Target.position);
        
        // Handle different behaviors based on enemy type
        if (enemy.EnemyType == EnemyType.Shooter)
        {
            HandleShooterChase(distanceToTarget);
        }
        else if (enemy.EnemyType == EnemyType.Chaser)
        {
            HandleChaserChase(distanceToTarget);
        }
        
        // Update path frequently for responsive chasing
        if (ShouldUpdatePath())
        {
            try
            {
                enemy.Movement.MoveTo(enemy.Target.position);
                lastPlayerPosition = enemy.Target.position;
                lastPathUpdate = Time.time;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ChaseState] Error updating path for {enemy.name}: {ex.Message}");
            }
        }
    }
    
    private void HandleShooterChase(float distanceToTarget)
    {
        if (enemy?.Config == null) return;
        
        // Shooter enters attack mode when in shooting range
        if (distanceToTarget <= enemy.Config.attackRange)
        {
            Debug.Log($"[ChaseState] Shooter {enemy.name} in attack range ({distanceToTarget:F1} <= {enemy.Config.attackRange:F1})");
            stateMachine.ChangeState(EnemyState.Attack);
            return;
        }
        
        // REMOVED: Shooters never lose target due to distance - they chase forever!
        // This makes shooters persistent and relentless until killed
        Debug.Log($"[ChaseState] Shooter {enemy.name} chasing target at distance {distanceToTarget:F1} - never giving up!");
    }
    
    private void HandleChaserChase(float distanceToTarget)
    {
        if (enemy?.Config == null) return;
        
        // Chaser attacks when in melee range
        if (distanceToTarget <= enemy.Config.attackRange)
        {
            Debug.Log($"[ChaseState] Chaser {enemy.name} in attack range ({distanceToTarget:F1} <= {enemy.Config.attackRange:F1})");
            stateMachine.ChangeState(EnemyState.Attack);
            return;
        }
        
        // Chasers are very persistent - only lose target at extreme distances
        // if (distanceToTarget > enemy.Config.loseTargetRange * 2f)
        // {
        //     Debug.Log($"[ChaseState] Chaser {enemy.name} lost target (too far: {distanceToTarget:F1})");
        //     enemy.SetTarget(null);
        //     stateMachine.ChangeState(EnemyState.Idle);
        //     return;
        // }
    }
    
    private void FindPlayerTarget()
    {
        if (enemy == null) return;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            enemy.SetTarget(player.transform);
            Debug.Log($"[ChaseState] {enemy.name} found player target: {player.name}");
        }
        else
        {
            Debug.LogWarning($"[ChaseState] {enemy.name} could not find player with tag 'Player'");
        }
    }
    
    private bool ShouldUpdatePath()
    {
        if (enemy?.Target == null) return false;
        if (!hasInitialized) return true;
        
        float timeSinceUpdate = Time.time - lastPathUpdate;
        float distancePlayerMoved = Vector3.Distance(enemy.Target.position, lastPlayerPosition);
        
        return timeSinceUpdate >= pathUpdateInterval ||
               distancePlayerMoved > 1f ||
               lastPathUpdate == 0f;
    }
    
    private void ApplyChaseVisualFeedback()
    {
        // Don't override material colors - let prefabs keep their original materials
        // If you want chase visual feedback, consider other effects like:
        // - Scale changes
        // - Particle effects
        // - Glow effects
        // - Animation speed changes
    }
    
    public override void ExitState() 
    {
        if (enemy == null)
        {
            Debug.LogError("[ChaseState] Enemy is null in ExitState!");
            return;
        }
        
        Debug.Log($"[ChaseState] {enemy.name} exiting chase state");
        
        // Reset NavMeshAgent settings
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && enemy.Config != null)
        {
            agent.speed = enemy.Config.moveSpeed;
            agent.acceleration = enemy.Config.acceleration;
            agent.stoppingDistance = enemy.Config.stopDistance;
        }
    }
    
    public override EnemyState GetStateType() => EnemyState.Chase;
}