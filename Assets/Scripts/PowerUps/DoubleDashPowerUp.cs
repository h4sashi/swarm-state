using UnityEngine;

public class DoubleDashPowerUp : BasePowerUp
{
    private float originalCooldown;
    private int availableCharges = 0;
    private PlayerDash playerDash;
    private bool modificationsApplied = false;
    
    // Track the last dash time to override cooldown calculation
    private float lastInstantDashTime = -1f;
    
    public override void ApplyModification(PlayerController player)
    {
        playerDash = player.GetComponent<PlayerDash>();
        if (playerDash?.Config == null)
        {
            Debug.LogError("DoubleDashPowerUp: PlayerDash or Config not found!");
            return;
        }
        
        // Store original values ONLY if not already stored
        if (!modificationsApplied)
        {
            originalCooldown = playerDash.Config.dashCooldown;
            modificationsApplied = true;
            Debug.Log($"Double Dash stored original cooldown: {originalCooldown}");
        }
        
        // Apply cooldown reduction
        playerDash.Config.dashCooldown = originalCooldown * config.dashCooldownReduction;
        
        // Grant immediate charges based on stacks
        availableCharges = config.bonusDashCharges * stackCount;
        
        // Subscribe to dash events to provide instant recharge
        GameEvents.OnPlayerDash += OnDashUsed;
        
        Debug.Log($"Double Dash applied: Cooldown {originalCooldown} -> {playerDash.Config.dashCooldown}, Available charges: {availableCharges}");
    }
    
    public override void RemoveModification(PlayerController player)
    {
        if (playerDash?.Config != null && modificationsApplied)
        {
            // Restore original values
            playerDash.Config.dashCooldown = originalCooldown;
            GameEvents.OnPlayerDash -= OnDashUsed;
            availableCharges = 0;
            lastInstantDashTime = -1f; // Reset override
            
            Debug.Log($"Double Dash removed: restored cooldown={originalCooldown}");
            
            // Reset flag
            modificationsApplied = false;
        }
    }
    
    private void OnDashUsed(Vector3 position)
    {
        if (availableCharges > 0)
        {
            availableCharges--;
            
            // Instead of modifying cooldown, override the UI calculation
            lastInstantDashTime = Time.time;
            
            // Send a direct UI update to show instant availability
            GameEvents.OnDashCooldownUpdate?.Invoke(1f); // Force UI to show ready state
            
            Debug.Log($"Instant dash charge used! Remaining: {availableCharges}");
        }
    }
    
    // Provide custom cooldown progress that accounts for instant charges
    public float GetModifiedCooldownProgress()
    {
        if (playerDash == null) return 1f;
        
        // If we just used an instant charge, show as ready
        if (lastInstantDashTime > 0f && Time.time - lastInstantDashTime < 0.1f)
        {
            return 1f;
        }
        
        // Otherwise use normal calculation
        return playerDash.DashCooldownProgress;
    }
    
    protected override void OnDestroy()
    {
        // Ensure modifications are removed even if Deactivate wasn't called
        if (modificationsApplied && playerDash?.Config != null)
        {
            playerDash.Config.dashCooldown = originalCooldown;
            GameEvents.OnPlayerDash -= OnDashUsed;
            Debug.Log("DoubleDashPowerUp: Force-restored values on destroy");
        }
        
        base.OnDestroy();
    }
}