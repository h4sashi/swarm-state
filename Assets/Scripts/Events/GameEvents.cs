using System;
using UnityEngine;

public static class GameEvents
{
    // Player Events
    public static Action<Vector3> OnPlayerDash;
    public static Action<int> OnPlayerHealthChanged;
    public static Action OnPlayerDeath;
    public static Action<float> OnDashCooldownUpdate;
    
    // Combat Events
    public static Action<GameObject> OnEnemyKilled;
    public static Action<int> OnScoreChanged;
    
    // UI Events
    public static Action<string> OnUIUpdate;
}