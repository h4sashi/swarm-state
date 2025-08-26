
public interface IHealth : IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsInvulnerable { get; }
    void Heal(int amount);
    
    // Events
    System.Action<int> OnHealthChanged { get; set; }
    System.Action OnDeath { get; set; }
}