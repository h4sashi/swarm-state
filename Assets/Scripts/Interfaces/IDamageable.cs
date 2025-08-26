// IDamageable Interface
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
    bool IsAlive { get; }
}
