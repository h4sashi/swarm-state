using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PowerUpManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<PowerUpConfig> availablePowerUps;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform powerUpContainer;
    
    [Header("Settings")]
    [SerializeField] private int maxActivePowerUps = 5;
    
    private Dictionary<PowerUpType, BasePowerUp> activePowerUps = new Dictionary<PowerUpType, BasePowerUp>();
    private List<GameObject> activePowerUpObjects = new List<GameObject>();
    
    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerController>();
        if (!powerUpContainer) 
        {
            powerUpContainer = new GameObject("Active PowerUps").transform;
            powerUpContainer.SetParent(transform);
        }
    }
    
    public bool ActivatePowerUp(PowerUpType type)
    {
        var config = availablePowerUps.FirstOrDefault(p => p.powerUpType == type);
        if (config == null)
        {
            Debug.LogWarning($"PowerUp config not found for type: {type}");
            return false;
        }
        
        return ActivatePowerUp(config);
    }
    
    public bool ActivatePowerUp(PowerUpConfig config)
    {
        if (activePowerUps.Count >= maxActivePowerUps && !activePowerUps.ContainsKey(config.powerUpType))
        {
            Debug.Log("Maximum active power-ups reached!");
            return false;
        }
        
        // Check if we already have this power-up active
        if (activePowerUps.TryGetValue(config.powerUpType, out BasePowerUp existingPowerUp))
        {
            if (config.isStackable)
            {
                existingPowerUp.Stack();
                return true;
            }
            else
            {
                // Refresh duration for non-stackable power-ups
                existingPowerUp.Activate();
                return true;
            }
        }
        
        // Create new power-up
        BasePowerUp newPowerUp = CreatePowerUp(config);
        if (newPowerUp != null)
        {
            activePowerUps[config.powerUpType] = newPowerUp;
            newPowerUp.Activate();
            return true;
        }
        
        return false;
    }
    
    private BasePowerUp CreatePowerUp(PowerUpConfig config)
    {
        // Create a persistent GameObject that won't be destroyed during power-up lifetime
        GameObject powerUpObj = new GameObject($"PowerUp_{config.powerUpType}");
        powerUpObj.transform.SetParent(powerUpContainer);
        activePowerUpObjects.Add(powerUpObj);
        
        // Add the appropriate power-up component
        BasePowerUp powerUp = config.powerUpType switch
        {
            PowerUpType.SpeedBoost => powerUpObj.AddComponent<SpeedBoostPowerUp>(),
            PowerUpType.DoubleDash => powerUpObj.AddComponent<DoubleDashPowerUp>(),
            // Add other power-up types here
            _ => null
        };
        
        if (powerUp != null)
        {
            powerUp.Initialize(config, player);
            
            // Set up automatic cleanup when power-up deactivates
            powerUp.OnPowerUpDeactivated = () => CleanupPowerUp(powerUp.PowerUpType, powerUpObj);
        }
        else
        {
            Debug.LogError($"Failed to create power-up component for type: {config.powerUpType}");
            Destroy(powerUpObj);
        }
        
        return powerUp;
    }
    
    private void CleanupPowerUp(PowerUpType type, GameObject powerUpObj)
    {
        activePowerUps.Remove(type);
        activePowerUpObjects.Remove(powerUpObj);
        
        if (powerUpObj != null)
        {
            Destroy(powerUpObj);
        }
    }
    
    public void DeactivatePowerUp(PowerUpType type)
    {
        if (activePowerUps.TryGetValue(type, out BasePowerUp powerUp))
        {
            powerUp.Deactivate();
        }
    }
    
    public void DeactivateAllPowerUps()
    {
        foreach (var powerUp in activePowerUps.Values)
        {
            powerUp.Deactivate();
        }
    }
    
    public List<IPowerUp> GetActivePowerUps()
    {
        return activePowerUps.Values.Cast<IPowerUp>().ToList();
    }
    
    public bool HasPowerUp(PowerUpType type)
    {
        return activePowerUps.ContainsKey(type);
    }
    
    // Public API for external systems
    public void AddSpeedBoost() => ActivatePowerUp(PowerUpType.SpeedBoost);
    public void AddDoubleDash() => ActivatePowerUp(PowerUpType.DoubleDash);
    
    void OnDestroy()
    {
        DeactivateAllPowerUps();
    }
}