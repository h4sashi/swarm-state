using UnityEngine;
using Enemies.Core;
using Enemies.States;

// Attack State - Enhanced with comprehensive null safety
public class AttackState : EnemyStateBase
{
    private float lastAttackTime;
    
    public AttackState(EnemyController enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
    
    public override void EnterState()
    {
        if (enemy?.Movement != null)
        {
            enemy.Movement.Stop();
        }
        Debug.Log($"[AttackState] {enemy?.name} entering attack state");
        
        // Check if enemy has attack capability
        if (enemy?.Attack == null)
        {
            Debug.LogError($"[AttackState] {enemy?.name}: No attack component found! Cannot attack.");
            // Fall back to chase state
            stateMachine?.ChangeState(EnemyState.Chase);
            return;
        }
    }
    
    public override void UpdateState()
    {
        // Comprehensive null checks
        if (enemy == null)
        {
            Debug.LogError("[AttackState] Enemy is null!");
            return;
        }
        
        if (stateMachine == null)
        {
            Debug.LogError($"[AttackState] {enemy.name}: StateMachine is null!");
            return;
        }
        
        if (enemy.Target == null)
        {
            Debug.Log($"[AttackState] {enemy.name}: No target, switching to idle");
            stateMachine.ChangeState(EnemyState.Idle);
            return;
        }
        
        if (enemy.Config == null)
        {
            Debug.LogError($"[AttackState] {enemy.name}: No config found!");
            stateMachine.ChangeState(EnemyState.Idle);
            return;
        }
        
        if (enemy.transform == null)
        {
            Debug.LogError($"[AttackState] {enemy.name}: Transform is null!");
            return;
        }
        
        if (enemy.Target.transform == null)
        {
            Debug.LogError($"[AttackState] {enemy.name}: Target transform is null!");
            stateMachine.ChangeState(EnemyState.Idle);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(enemy.transform.position, enemy.Target.position);
        
        // Target moved out of attack range
        if (distanceToTarget > enemy.Config.attackRange)
        {
            Debug.Log($"[AttackState] {enemy.name}: Target out of range ({distanceToTarget:F1} > {enemy.Config.attackRange:F1}), switching to chase");
            stateMachine.ChangeState(EnemyState.Chase);
            return;
        }
        
        // Face the target
        Vector3 directionToTarget = (enemy.Target.position - enemy.transform.position).normalized;
        if (directionToTarget != Vector3.zero)
        {
            enemy.transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
        
        // Attack if cooldown is ready and attack component exists
        if (enemy.Attack != null && Time.time - lastAttackTime >= enemy.Config.attackCooldown)
        {
            try
            {
                // Additional validation before attacking
                if (enemy.Target != null && enemy.Target.transform != null)
                {
                    // Verify the attack component is still valid
                    var attackComponent = enemy.Attack as MonoBehaviour;
                    if (attackComponent != null && attackComponent.gameObject != null && attackComponent.enabled)
                    {
                        enemy.Attack.Attack(enemy.Target.position);
                        lastAttackTime = Time.time;
                        Debug.Log($"[AttackState] {enemy.name}: Executed attack on {enemy.Target.name}");
                    }
                    else
                    {
                        Debug.LogError($"[AttackState] {enemy.name}: Attack component is invalid or disabled!");
                        stateMachine.ChangeState(EnemyState.Chase);
                    }
                }
                else
                {
                    Debug.LogError($"[AttackState] {enemy.name}: Target became null during attack!");
                    stateMachine.ChangeState(EnemyState.Idle);
                }
            }
            catch (System.NullReferenceException ex)
            {
                Debug.LogError($"[AttackState] {enemy.name}: Null reference during attack: {ex.Message}");
                Debug.LogError($"[AttackState] Stack trace: {ex.StackTrace}");
                // Try to recover by switching to chase
                stateMachine.ChangeState(EnemyState.Chase);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AttackState] {enemy.name}: Error during attack: {ex.Message}");
                Debug.LogError($"[AttackState] Stack trace: {ex.StackTrace}");
                // Try to recover by switching to chase
                stateMachine.ChangeState(EnemyState.Chase);
            }
        }
        else if (enemy.Attack == null)
        {
            Debug.LogError($"[AttackState] {enemy.name}: Attack component is null, cannot attack!");
            // Try to continue chasing instead
            stateMachine.ChangeState(EnemyState.Chase);
        }
    }
    
    public override void ExitState()
    {
        Debug.Log($"[AttackState] {enemy?.name} exiting attack state");
    }
    
    public override EnemyState GetStateType() => EnemyState.Attack;
}