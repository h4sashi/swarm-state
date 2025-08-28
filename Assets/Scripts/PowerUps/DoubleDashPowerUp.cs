using UnityEngine;

public class DoubleDashPowerUp : BasePowerUp
{
    private float originalCooldown;
    private int availableCharges = 0;
    private PlayerDash playerDash; // Direct reference to avoid repeated GetComponent calls
    private bool modificationsApplied = false;
    
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
            // Force an immediate cooldown reset by temporarily modifying cooldown
            if (playerDash != null)
            {
                StartCoroutine(InstantCooldownReset(playerDash));
            }
            Debug.Log($"Instant dash charge used! Remaining: {availableCharges}");
        }
    }
    
    private System.Collections.IEnumerator InstantCooldownReset(PlayerDash dash)
    {
        float tempCooldown = dash.Config.dashCooldown;
        dash.Config.dashCooldown = 0f; // Make next dash immediately available
        yield return new WaitForFixedUpdate(); // Wait one frame
        dash.Config.dashCooldown = tempCooldown; // Restore normal cooldown
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