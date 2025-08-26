using UnityEngine;
using Enemies.Core;
using Enemies.Weapons;
using System.Collections.Generic;

namespace Enemies.States
{
    // Idle state for enemies - Fixed version supporting both Chaser and Shooter
    public class IdleState : EnemyStateBase
    {
        private float idleTimer;
        private Vector3 idlePosition;
        private bool hasInitialized = false;

        public IdleState(EnemyController enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void EnterState()
        {
            enemy.Movement?.Stop();
            idleTimer = 0f;
            idlePosition = enemy.transform.position;
            hasInitialized = false;
            
            Debug.Log($"[IdleState] {enemy.name} ({enemy.EnemyType}) entering idle state");

            // Both Chaser and Shooter enemies should actively search for player
            if (enemy.EnemyType == EnemyType.Chaser || enemy.EnemyType == EnemyType.Shooter)
            {
                SearchForPlayerTarget();
                
                // If we found a target immediately, start chasing
                if (enemy.Target != null)
                {
                    stateMachine.ChangeState(EnemyState.Chase);
                    return;
                }
            }
        }

        public override void UpdateState()
        {
            idleTimer += Time.deltaTime;

            // Both Chaser and Shooter enemies are aggressive about finding targets
            if (enemy.EnemyType == EnemyType.Chaser || enemy.EnemyType == EnemyType.Shooter)
            {
                HandleAggressiveIdleBehavior();
            }
            else
            {
                HandleStandardIdleBehavior();
            }
        }

        private void HandleAggressiveIdleBehavior()
        {
            // Aggressive enemies actively search for player every second
            if (idleTimer >= 1f)
            {
                SearchForPlayerTarget();
                idleTimer = 0f; // Reset timer
                
                if (enemy.Target != null)
                {
                    Debug.Log($"[IdleState] {enemy.EnemyType} {enemy.name} found target, switching to Chase");
                    stateMachine.ChangeState(EnemyState.Chase);
                    return;
                }
            }
        }

        private void HandleStandardIdleBehavior()
        {
            // Check for player in detection range
            if (enemy.CanSeeTarget())
            {
                stateMachine.ChangeState(EnemyState.Chase);
                return;
            }

            // After idle time, could transition to patrol
            if (idleTimer >= GetIdleTime() && enemy.Config?.canPatrol == true)
            {
                // Could add patrol state transition here
                // For now, just reset idle timer
                idleTimer = 0f;
            }
        }

        private void SearchForPlayerTarget()
        {
            if (enemy.Target != null) return; // Already have a target

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // For aggressive enemies (Chaser and Shooter), set target regardless of distance
                if (enemy.EnemyType == EnemyType.Chaser || enemy.EnemyType == EnemyType.Shooter)
                {
                    enemy.SetTarget(player.transform);
                    Debug.Log($"[IdleState] {enemy.EnemyType} {enemy.name} found player target during idle search");
                }
                else
                {
                    // For other enemies, respect detection range
                    float distance = Vector3.Distance(enemy.transform.position, player.transform.position);
                    if (distance <= enemy.Config.detectionRange)
                    {
                        enemy.SetTarget(player.transform);
                    }
                }
            }
        }

        private float GetIdleTime()
        {
            return enemy.Config?.idleTime ?? 3f;
        }

        public override void ExitState()
        {
            Debug.Log($"[IdleState] {enemy.name} exiting idle state");
        }

        public override EnemyState GetStateType() => EnemyState.Idle;
    }
}