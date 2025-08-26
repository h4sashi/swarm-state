using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IDashAbility
{
    bool IsDashing { get; }
    bool CanDash { get; }
    float DashCooldownProgress { get; }
    void PerformDash();
    void PerformDash(Vector3 direction);
}