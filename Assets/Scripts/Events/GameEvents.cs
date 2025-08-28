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

    // PowerUp Events
    public static System.Action<PowerUpType, bool> OnPowerUpStateChanged; // type, activated
    public static System.Action<PowerUpType, int> OnPowerUpStacked; // type, stack count
    public static System.Action<PowerUpType, float> OnPowerUpTimeRemaining; // type, time left

    // Scoring Events
    public static System.Action<int, string> OnScoreAdded; // points added, reason
    public static System.Action<int> OnScoreUpdated; // new total score
    public static System.Action<float> OnSurvivalTimeUpdated; // current survival time
    public static System.Action<float> OnMultiplierChanged; // new multiplier value
    public static System.Action<int, float, float> OnGameEnded; // final score, survival time, final multiplier

}