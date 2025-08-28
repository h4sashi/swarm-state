using UnityEngine;
public class SpeedBoostPowerUp : BasePowerUp
{
    private float originalSpeed;
    private float originalAcceleration;
    private PlayerMovement playerMovement; // Direct reference to avoid repeated GetComponent calls
    private bool modificationsApplied = false;
    
    public override void ApplyModification(PlayerController player)
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement?.Config == null)
        {
            Debug.LogError("SpeedBoostPowerUp: PlayerMovement or Config not found!");
            return;
        }
        
        // Store original values ONLY if not already stored
        if (!modificationsApplied)
        {
            originalSpeed = playerMovement.Config.moveSpeed;
            originalAcceleration = playerMovement.Config.acceleration;
            modificationsApplied = true;
            
            Debug.Log($"Speed boost stored originals: Speed={originalSpeed}, Acceleration={originalAcceleration}");
        }
        
        // Calculate stack multiplier
        float stackMultiplier = config.stacksExtendDuration ? 1f : stackCount;
        
        // Apply modifications
        playerMovement.Config.moveSpeed = originalSpeed * (config.speedMultiplier * stackMultiplier);
        playerMovement.Config.acceleration = originalAcceleration * 1.5f;
        
        Debug.Log($"Speed boost applied: {originalSpeed} -> {playerMovement.Config.moveSpeed} (Stack: {stackCount})");
    }
    
    public override void RemoveModification(PlayerController player)
    {
        if (playerMovement?.Config != null && modificationsApplied)
        {
            // Restore original values
            playerMovement.Config.moveSpeed = originalSpeed;
            playerMovement.Config.acceleration = originalAcceleration;
            
            Debug.Log($"Speed boost removed: restored Speed={originalSpeed}, Acceleration={originalAcceleration}");
            
            // Reset flag
            modificationsApplied = false;
        }
    }
    
    protected override void OnDestroy()
    {
        // Ensure modifications are removed even if Deactivate wasn't called
        if (modificationsApplied && playerMovement?.Config != null)
        {
            playerMovement.Config.moveSpeed = originalSpeed;
            playerMovement.Config.acceleration = originalAcceleration;
            Debug.Log("SpeedBoostPowerUp: Force-restored values on destroy");
        }
        
        base.OnDestroy();
    }
}