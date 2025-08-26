using UnityEngine;
// Generic ability interface for future power-ups/abilities
public interface IAbility
{
    string AbilityName { get; }
    bool CanUse { get; }
    float CooldownProgress { get; }
    void Use();
    void Use(Vector3 target);
}