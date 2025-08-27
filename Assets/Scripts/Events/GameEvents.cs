using System;
using UnityEngine;

public static class GameEvents
{
    // Player Events
    public static System.Action<Vector3> OnPlayerDash;
    public static System.Action<int> OnPlayerHealthChanged;
    public static System.Action OnPlayerDeath;
    public static System.Action<float> OnDashCooldownUpdate;
    public static System.Action<Color, float> OnScreenFlash;
    
    // Combat Events
    public static System.Action<GameObject> OnEnemyKilled;
    public static System.Action<int> OnScoreChanged;
    
    // UI Events
    public static System.Action<string> OnUIUpdate;
    
    // Flash-specific events
    public static System.Action<Color, float, Vector3, float> OnRadialFlash; // color, duration, worldPos, radius
}