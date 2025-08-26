using System.Collections.Generic;
using UnityEngine;
using Enemies.States;

namespace Enemies.Core
{
    // State Machine - Fixed version with proper namespace imports
    public class EnemyStateMachine : MonoBehaviour, IStateMachine<EnemyState>
    {
        [SerializeField] private EnemyState currentState;
        [SerializeField] private bool debugMode = false;
        
        private Dictionary<EnemyState, EnemyStateBase> states;
        private EnemyController enemyController;
        private float lastStateChange;
        
        public EnemyState CurrentState => currentState;
        
        private void Awake()
        {
            enemyController = GetComponent<EnemyController>();
            if (enemyController == null)
            {
                Debug.LogError($"[EnemyStateMachine] No EnemyController found on {gameObject.name}");
                enabled = false;
                return;
            }
            
            InitializeStates();
        }
        
        private void Start()
        {
            ChangeState(EnemyState.Idle);
        }
        
        private void Update()
        {
            if (enemyController.IsActive && enemyController.IsAlive)
            {
                UpdateState();
            }
            else if (!enemyController.IsAlive && currentState != EnemyState.Death)
            {
                ChangeState(EnemyState.Death);
            }
        }
        
        private void InitializeStates()
        {
            try
            {
                states = new Dictionary<EnemyState, EnemyStateBase>
                {
                    { EnemyState.Idle, new IdleState(enemyController, this) },
                    { EnemyState.Chase, new ChaseState(enemyController, this) },
                    { EnemyState.Attack, new AttackState(enemyController, this) },
                    { EnemyState.Death, new DeathState(enemyController, this) }
                };
                
                if (debugMode)
                {
                    Debug.Log($"[EnemyStateMachine] Initialized {states.Count} states for {enemyController.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyStateMachine] Failed to initialize states for {gameObject.name}: {e.Message}");
                enabled = false;
            }
        }
        
        public void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            
            // Check state change delay (except for death state - that should be immediate)
            if (newState != EnemyState.Death && 
                enemyController.Config != null && 
                Time.time - lastStateChange < enemyController.Config.stateChangeDelay)
            {
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"[EnemyStateMachine] {enemyController.name}: {currentState} → {newState}");
            }
            
            try
            {
                // Exit current state
                if (states.ContainsKey(currentState))
                {
                    states[currentState]?.ExitState();
                }
                
                // Change to new state
                EnemyState previousState = currentState;
                currentState = newState;
                
                // Enter new state
                if (states.ContainsKey(currentState))
                {
                    states[currentState]?.EnterState();
                }
                else
                {
                    Debug.LogError($"[EnemyStateMachine] State {newState} not found in states dictionary!");
                    currentState = previousState; // Revert to previous state
                }
                
                lastStateChange = Time.time;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyStateMachine] Error changing state to {newState} for {enemyController.name}: {e.Message}");
            }
        }
        
       public void UpdateState()
{
    try
    {
        // Additional null checks
        if (enemyController == null)
        {
            Debug.LogError($"[EnemyStateMachine] EnemyController is null on {gameObject.name}");
            enabled = false;
            return;
        }
        
        if (states == null)
        {
            Debug.LogError($"[EnemyStateMachine] States dictionary is null on {enemyController.name}");
            enabled = false;
            return;
        }
        
        if (states.ContainsKey(currentState) && states[currentState] != null)
        {
            states[currentState].UpdateState();
        }
        else
        {
            Debug.LogError($"[EnemyStateMachine] State {currentState} not found or is null for {enemyController.name}");
            // Try to recover by going to Idle state
            if (states.ContainsKey(EnemyState.Idle) && states[EnemyState.Idle] != null)
            {
                ChangeState(EnemyState.Idle);
            }
        }
    }
    catch (System.NullReferenceException e)
    {
        Debug.LogError($"[EnemyStateMachine] Null reference in UpdateState for {(enemyController != null ? enemyController.name : "unknown")}: {e.Message}");
        Debug.LogError($"Stack trace: {e.StackTrace}");
        
        // Try to recover
        if (enemyController != null && enemyController.IsAlive)
        {
            ChangeState(EnemyState.Idle);
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[EnemyStateMachine] Error updating state {currentState} for {(enemyController != null ? enemyController.name : "unknown")}: {e.Message}");
        Debug.LogError($"Stack trace: {e.StackTrace}");
    }
}
        
        public void ForceState(EnemyState state)
        {
            // Force immediate state change without delay checks
            lastStateChange = 0f;
            ChangeState(state);
        }
        
        // Public getters for debugging
        public bool HasState(EnemyState state) => states.ContainsKey(state);
        public int StateCount => states.Count;
    }
}