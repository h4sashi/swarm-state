using UnityEngine;

namespace Enemies.Core
{
    // Enemy Health Component
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        private EnemyConfig config;
        private int currentHealth;
        private EnemyStateMachine stateMachine;
        public event System.Action<EnemyController> OnDeath;

        public bool IsAlive => currentHealth > 0;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => config ? config.maxHealth : 1;


        void Awake()
        {
            stateMachine = GetComponent<EnemyStateMachine>();
        }

        public void Initialize(EnemyConfig config)
        {
            this.config = config;
            currentHealth = config.maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(damage));
            StartCoroutine(DamageFlash());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

       private void Die()
        {
            SoundManager.Instance.PlayEnemyDamageSound();
            if (IsAlive) return;

            // Switch state if state machine exists
            if (stateMachine)
            {
                stateMachine.ChangeState(EnemyState.Death);
            }

            // Notify listeners (Spawner, etc.)
            OnDeath?.Invoke(GetComponent<EnemyController>());
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                renderer.material.color = originalColor;
            }
        }
    }
}