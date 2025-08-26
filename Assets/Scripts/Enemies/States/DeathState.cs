using UnityEngine;
using Enemies.Core;
using Enemies.States;

namespace Enemies.States
{
    // Death State - Handles enemy death sequence and cleanup
    public class DeathState : EnemyStateBase
    {
        private float deathTimer;
        private bool hasTriggeredDeathEvent = false;
        private bool hasDisabledComponents = false;
        
        public DeathState(EnemyController enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }
        
        public override void EnterState()
        {
            Debug.Log($"[DeathState] {enemy.name} entering death state");
            
            // Stop all movement immediately
            enemy.Movement?.Stop();
            
            // Disable NavMeshAgent if present
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.enabled = false;
            }
            
            deathTimer = 0f;
            hasTriggeredDeathEvent = false;
            hasDisabledComponents = false;
            
            // Apply immediate death effects
            ApplyDeathVisuals();
            DisableComponentsSafely();
            
            // Trigger death event
            TriggerDeathEvent();
        }
        
        public override void UpdateState()
        {
            deathTimer += Time.deltaTime;
            
            // Additional death effects over time
            UpdateDeathEffects();
            
            // Clean up after death delay
            if (deathTimer >= GetDeathDelay())
            {
                enemy.Deactivate();
            }
        }
        
        public override void ExitState() 
        {
            // Final cleanup if needed
        }
        
        public override EnemyState GetStateType() => EnemyState.Death;
        
        private void ApplyDeathVisuals()
        {
            // Change visual appearance to indicate death
            var renderer = enemy.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                // Create a new material instance to avoid affecting other enemies
                Material deathMaterial = new Material(renderer.material);
                deathMaterial.color = Color.black;
                renderer.material = deathMaterial;
            }
            
            // Optional: Add death particle effect, sound, etc.
            PlayDeathEffects();
        }
        
        private void DisableComponentsSafely()
        {
            if (hasDisabledComponents) return;
            
            try
            {
                // Disable colliders to prevent further interactions
                var colliders = enemy.GetComponents<Collider>();
                foreach (var collider in colliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                }
                
                // Disable Rigidbody physics
                var rigidbody = enemy.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;
                    rigidbody.detectCollisions = false;
                }
                
                hasDisabledComponents = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DeathState] Error disabling components on {enemy.name}: {e.Message}");
            }
        }
        
        private void TriggerDeathEvent()
        {
            if (hasTriggeredDeathEvent) return;
            
            try
            {
                // Fire the death event for external systems (like spawner tracking)
                if (GameEvents.OnEnemyKilled != null)
                {
                    GameEvents.OnEnemyKilled.Invoke(enemy.gameObject);
                }
                
                hasTriggeredDeathEvent = true;
                Debug.Log($"[DeathState] Death event triggered for {enemy.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DeathState] Error triggering death event for {enemy.name}: {e.Message}");
            }
        }
        
        private void UpdateDeathEffects()
        {
            // Optional: Fade out effect, shrinking, etc.
            if (enemy.Config != null && deathTimer > 0.5f)
            {
                ApplyFadeEffect();
            }
        }
        
        private void ApplyFadeEffect()
        {
            float fadeProgress = (deathTimer - 0.5f) / (GetDeathDelay() - 0.5f);
            fadeProgress = Mathf.Clamp01(fadeProgress);
            
            // Gradually shrink the enemy
            Vector3 originalScale = enemy.Config.scale;
            Vector3 currentScale = Vector3.Lerp(originalScale, Vector3.zero, fadeProgress * 0.8f);
            enemy.transform.localScale = currentScale;
            
            // Fade transparency if material supports it
            var renderer = enemy.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material.HasProperty("_Color"))
            {
                Color currentColor = renderer.material.color;
                currentColor.a = Mathf.Lerp(1f, 0.2f, fadeProgress);
                renderer.material.color = currentColor;
            }
        }
        
        private void PlayDeathEffects()
        {
            // Placeholder for death effects
            // You can add:
            // - Particle systems
            // - Sound effects
            // - Screen shake
            // - Score notifications
            
            Debug.Log($"[DeathState] Playing death effects for {enemy.name}");
        }
        
        private float GetDeathDelay()
        {
            // Use config death delay if available, otherwise default to 2 seconds
            return enemy.Config?.deathDelay ?? 2f;
        }
    }
}
