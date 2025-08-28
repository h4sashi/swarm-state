using UnityEngine;

/// <summary>
/// Base interface for all power-ups - extends IAbility for consistency
/// </summary>
public interface IPowerUp : IAbility
{
    PowerUpType PowerUpType { get; }
    float Duration { get; }
    float RemainingTime { get; }
    bool IsActive { get; }
    bool IsStackable { get; }
    int StackCount { get; }
    void Activate();
    void Deactivate();
    void Stack();
}

/// <summary>
/// Interface for power-up effects that modify player stats
/// </summary>
public interface IStatModifier
{
    void ApplyModification(PlayerController player);
    void RemoveModification(PlayerController player);
}

/// <summary>
/// Interface for visual effects on power-ups
/// </summary>
public interface IPowerUpVFX
{
    void StartEffect();
    void StopEffect();
    void UpdateEffect(float normalizedTime);
}