using UnityEngine;

    // Core enemy interface
    public interface IEnemy : IDamageable
    {
        EnemyType EnemyType { get; }
        EnemyState CurrentState { get; }
        Transform Target { get; set; }
        bool IsActive { get; }
        void Initialize(EnemyConfig config);
        void SetTarget(Transform target);
        void Deactivate();
    }

    // Enemy movement interface
    public interface IEnemyMovement
    {
        float MovementSpeed { get; set; }
        Vector3 Velocity { get; }
        void MoveTo(Vector3 target);
        void Stop();
        bool IsMoving { get; }
    }

    // Enemy attack interface
    public interface IEnemyAttack
    {
        float AttackRange { get; }
        float AttackCooldown { get; }
        bool CanAttack { get; }
        void Attack(Vector3 targetPosition);
    }

    // State machine interface
    public interface IStateMachine<T>
    {
        T CurrentState { get; }
        void ChangeState(T newState);
        void UpdateState();
    }

