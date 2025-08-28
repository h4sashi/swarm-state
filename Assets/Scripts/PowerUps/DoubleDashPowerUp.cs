
using UnityEngine;
public class DoubleDashPowerUp : BasePowerUp
{
    private float originalCooldown;
    private int availableCharges = 0;

    public override void ApplyModification(PlayerController player)
    {
        var dash = player.GetComponent<PlayerDash>();
        if (dash?.Config != null)
        {
            originalCooldown = dash.Config.dashCooldown;

            // Significantly reduce cooldown for rapid dashing
            dash.Config.dashCooldown = originalCooldown * config.dashCooldownReduction;

            // Grant immediate charges based on stacks
            availableCharges = config.bonusDashCharges * stackCount;

            // Subscribe to dash events to provide instant recharge
            GameEvents.OnPlayerDash += OnDashUsed;

            Debug.Log($"Double Dash applied: Cooldown {originalCooldown} -> {dash.Config.dashCooldown}, Available charges: {availableCharges}");
        }
    }

    public override void RemoveModification(PlayerController player)
    {
        var dash = player.GetComponent<PlayerDash>();
        if (dash?.Config != null)
        {
            dash.Config.dashCooldown = originalCooldown;
            GameEvents.OnPlayerDash -= OnDashUsed;
            availableCharges = 0;

            Debug.Log("Double Dash removed: restored original cooldown");
        }
    }

    private void OnDashUsed(Vector3 position)
    {
        if (availableCharges > 0)
        {
            availableCharges--;

            // Force an immediate cooldown reset by temporarily modifying cooldown
            var dash = targetPlayer.GetComponent<PlayerDash>();
            if (dash != null)
            {
                StartCoroutine(InstantCooldownReset(dash));
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
        if (targetPlayer != null)
        {
            RemoveModification(targetPlayer);
        }
        
    }
}