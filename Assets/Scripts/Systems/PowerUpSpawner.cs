using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private List<PowerUpConfig> spawnablePowerUps;
    [SerializeField] private int maxActivePickups = 8;
    [SerializeField] private float spawnRadius = 25f;
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private LayerMask obstacleLayer = 0;
    
    [Header("Spawn Timing")]
    [SerializeField] private float initialSpawnDelay = 5f;
    [SerializeField] private float baseSpawnInterval = 15f;
    [SerializeField] private float spawnIntervalVariation = 5f;
    [SerializeField] private bool spawnOnEnemyKill = true;
    [SerializeField] private float enemyKillSpawnChance = 0.3f;
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform spawnContainer;
    
    private List<GameObject> activePickups = new List<GameObject>();
    private Coroutine spawnCoroutine;
    
    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerController>()?.transform;
        if (!spawnContainer)
        {
            spawnContainer = new GameObject("PowerUp Pickups").transform;
            spawnContainer.SetParent(transform);
        }
        
        // Subscribe to enemy deaths for bonus spawning
        if (spawnOnEnemyKill)
        {
            GameEvents.OnEnemyKilled += OnEnemyKilled;
        }
        
        // Start the spawning routine
        spawnCoroutine = StartCoroutine(SpawnRoutine());
        
        Debug.Log($"PowerUpSpawner initialized with {spawnablePowerUps.Count} power-up types");
    }
    
    void OnDestroy()
    {
        if (spawnOnEnemyKill)
        {
            GameEvents.OnEnemyKilled -= OnEnemyKilled;
        }
        
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
    
    private System.Collections.IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        
        while (true)
        {
            if (activePickups.Count < maxActivePickups && spawnablePowerUps.Count > 0)
            {
                TrySpawnPowerUp();
            }
            
            float nextSpawnTime = baseSpawnInterval + Random.Range(-spawnIntervalVariation, spawnIntervalVariation);
            yield return new WaitForSeconds(nextSpawnTime);
        }
    }
    
    private void OnEnemyKilled(GameObject enemy)
    {
        if (Random.value <= enemyKillSpawnChance && activePickups.Count < maxActivePickups)
        {
            // Try to spawn near the enemy's death location
            TrySpawnPowerUp(enemy.transform.position);
        }
    }
    
    public void TrySpawnPowerUp(Vector3? preferredCenter = null)
    {
        if (spawnablePowerUps.Count == 0)
        {
            Debug.LogWarning("No spawnable power-ups configured!");
            return;
        }
        
        // Choose spawn center (near player or preferred location)
        Vector3 spawnCenter = preferredCenter ?? (player ? player.position : transform.position);
        
        // Find valid spawn position using NavMesh and overlap checks
        Vector3? spawnPosition = FindValidSpawnPosition(spawnCenter);
        
        if (spawnPosition.HasValue)
        {
            // Select power-up type based on spawn chances
            PowerUpConfig selectedConfig = SelectPowerUpToSpawn();
            
            if (selectedConfig != null)
            {
                SpawnPowerUpPickup(selectedConfig, spawnPosition.Value);
            }
        }
        else
        {
            Debug.Log($"PowerUpSpawner: Could not find valid spawn position near {spawnCenter}");
        }
    }
    
    private Vector3? FindValidSpawnPosition(Vector3 center)
    {
        int maxAttempts = 20;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generate random point within spawn radius
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Try to find nearest point on NavMesh
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Vector3 candidatePosition = hit.position + Vector3.up * 0.5f; // Slightly above ground
                
                // Check for overlaps with existing pickups and obstacles
                if (IsPositionValid(candidatePosition))
                {
                    return candidatePosition;
                }
            }
        }
        
        return null; // Could not find valid position
    }
    
    private bool IsPositionValid(Vector3 position)
    {
        float checkRadius = 2f;
        
        // Check for existing pickups nearby
        foreach (GameObject pickup in activePickups)
        {
            if (pickup != null && Vector3.Distance(position, pickup.transform.position) < checkRadius * 2f)
            {
                return false; // Too close to existing pickup
            }
        }
        
        // Check for obstacles using OverlapSphere
        Collider[] obstacles = Physics.OverlapSphere(position, checkRadius, obstacleLayer);
        if (obstacles.Length > 0)
        {
            return false; // Position blocked by obstacles
        }
        
        // Ensure there's ground below
        if (Physics.Raycast(position, Vector3.down, out RaycastHit groundHit, 10f, groundLayer))
        {
            return true; // Valid position with ground support
        }
        
        return false; // No ground found
    }
    
    private PowerUpConfig SelectPowerUpToSpawn()
    {
        // Calculate total spawn chance
        float totalChance = spawnablePowerUps.Sum(config => config.spawnChance);
        
        if (totalChance <= 0f)
        {
            // If no spawn chances set, pick randomly
            return spawnablePowerUps[Random.Range(0, spawnablePowerUps.Count)];
        }
        
        // Weighted random selection
        float randomValue = Random.Range(0f, totalChance);
        float currentChance = 0f;
        
        foreach (PowerUpConfig config in spawnablePowerUps)
        {
            currentChance += config.spawnChance;
            if (randomValue <= currentChance)
            {
                return config;
            }
        }
        
        // Fallback to first power-up
        return spawnablePowerUps[0];
    }
    
    private void SpawnPowerUpPickup(PowerUpConfig config, Vector3 position)
    {
        if (config.prefab == null)
        {
            Debug.LogError($"PowerUpConfig '{config.displayName}' has no prefab assigned! Cannot spawn power-up.");
            return;
        }
        
        // Instantiate the assigned prefab directly
        GameObject spawnedPickup = Instantiate(config.prefab, position, Quaternion.identity, spawnContainer);
        
        // Ensure the prefab has a PowerUpPickup component
        PowerUpPickup pickupComponent = spawnedPickup.GetComponent<PowerUpPickup>();
        if (pickupComponent == null)
        {
            Debug.LogError($"PowerUp prefab '{config.prefab.name}' must have a PowerUpPickup component! Adding one automatically.");
            pickupComponent = spawnedPickup.AddComponent<PowerUpPickup>();
        }
        
        // Initialize with the config (this sets up the pickup behavior)
        pickupComponent.Initialize(config);
        
        // Track spawned pickup
        activePickups.Add(spawnedPickup);
        
        // Set up cleanup when collected
        StartCoroutine(MonitorPickup(spawnedPickup));
        
        Debug.Log($"Spawned prefab '{config.prefab.name}' for {config.displayName} at {position}");
    }
    
    private System.Collections.IEnumerator MonitorPickup(GameObject pickup)
    {
        // Wait until pickup is destroyed/collected
        while (pickup != null)
        {
            yield return new WaitForSeconds(1f);
        }
        
        // Remove from active list
        activePickups.Remove(pickup);
    }
    
    // Public API for manual spawning
    public void ForceSpawnNearPlayer()
    {
        if (player != null)
        {
            TrySpawnPowerUp(player.position);
        }
    }
    
    public void ForceSpawnAtPosition(Vector3 position)
    {
        TrySpawnPowerUp(position);
    }
    
    void OnDrawGizmosSelected()
    {
        Vector3 center = player ? player.position : transform.position;
        
        // Draw spawn radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, spawnRadius);
        
        // Draw active pickup positions
        Gizmos.color = Color.yellow;
        foreach (GameObject pickup in activePickups)
        {
            if (pickup != null)
            {
                Gizmos.DrawWireCube(pickup.transform.position, Vector3.one);
            }
        }
    }
}