using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Enemies.Core;
using Unity.AI.Navigation;

namespace Enemies.Spawning
{
    [System.Serializable]
    public class EnemySpawnData
    {
        [Header("Enemy Configuration")]
        public EnemyConfig enemyConfig;
        public GameObject enemyPrefab;
        
        [Header("Spawn Probability")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;
        [Range(0f, 1f)]
        public float spawnChance = 1f;
        
        [Header("Wave Scaling")]
        public bool scaleWithWave = true;
        public int minWaveToSpawn = 1;
        public int maxSpawnPerWave = -1; // -1 = no limit
    }

    [System.Serializable]
    public class WaveData
    {
        [Header("Wave Configuration")]
        public int waveNumber = 1;
        public int enemyCount = 5;
        public float spawnInterval = 1.0f;
        public float waveDelay = 3.0f; // Time between waves
        
        [Header("Enemy Types")]
        public List<EnemySpawnData> availableEnemies = new List<EnemySpawnData>();
        
        [Header("Scaling Multipliers")]
        [Range(1f, 3f)]
        public float healthMultiplier = 1f;
        [Range(1f, 2f)]
        public float speedMultiplier = 1f;
        [Range(1f, 2f)]
        public float damageMultiplier = 1f;
    }

    [System.Serializable]
    public class SpawnerSettings
    {
        [Header("Spawning Behavior")]
        public bool continuousWaves = true;
        public bool autoStart = true;
        public float maxConcurrentEnemies = 20f;
        
        [Header("Spawn Area Configuration")]
        public float spawnRadius = 15f;
        public float minDistanceFromPlayer = 8f;
        public float maxDistanceFromPlayer = 25f;
        public LayerMask spawnObstacleLayers = 1;
        
        [Header("NavMesh Configuration")]
        public bool requireNavMeshSurface = true;
        public float navMeshSampleDistance = 2f;
        public int maxSpawnAttempts = 10;
        
        [Header("Progressive Difficulty")]
        public bool enableProgressiveDifficulty = true;
        public float difficultyScaleRate = 0.1f;
        public float maxDifficultyMultiplier = 3f;
        public AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0, 1, 1, 2);
    }

    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawner Configuration")]
        [SerializeField] private SpawnerSettings settings = new SpawnerSettings();
        
        [Header("Wave Configuration")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();
        [SerializeField] private WaveData baseWaveTemplate; // Template for generating new waves
        
        [Header("Runtime Info")]
        [SerializeField, ReadOnly] private int currentWaveNumber = 0;
        [SerializeField, ReadOnly] private int activeEnemyCount = 0;
        [SerializeField, ReadOnly] private float currentDifficultyMultiplier = 1f;
        [SerializeField, ReadOnly] private bool isSpawning = false;
        
        // Events
        public System.Action<int> OnWaveStarted;
        public System.Action<int> OnWaveCompleted;
        public System.Action<GameObject> OnEnemySpawned;
        
        // Private members
        private Transform playerTransform;
        private List<GameObject> activeEnemies = new List<GameObject>();
        private Coroutine spawnCoroutine;
        private NavMeshSurface navMeshSurface;
        private float gameStartTime;
        
        // Weighted spawn selection
        private Dictionary<EnemySpawnData, float> spawnWeights = new Dictionary<EnemySpawnData, float>();
        
        private void Awake()
        {
            // Find NavMeshSurface
            navMeshSurface = FindObjectOfType<NavMeshSurface>();
            if (settings.requireNavMeshSurface && navMeshSurface == null)
            {
                Debug.LogError("[EnemySpawner] NavMeshSurface required but not found!");
            }
            
            // Subscribe to enemy death events
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            
            gameStartTime = Time.time;
        }
        
        private void Start()
        {
            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] No player found with 'Player' tag!");
            }
            
            // Initialize base wave template if waves list is empty
            if (waves.Count == 0 && baseWaveTemplate != null)
            {
                GenerateInitialWaves();
            }
            
            if (settings.autoStart)
            {
                StartSpawning();
            }
        }
        
        private void OnDestroy()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            StopSpawning();
        }
        
        #region Public Interface
        
        public void StartSpawning()
        {
            if (isSpawning) return;
            
            isSpawning = true;
            currentWaveNumber = 0;
            
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            
            spawnCoroutine = StartCoroutine(SpawnManager());
            Debug.Log("[EnemySpawner] Started spawning system");
        }
        
        public void StopSpawning()
        {
            isSpawning = false;
            
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            Debug.Log("[EnemySpawner] Stopped spawning system");
        }
        
        public void ForceNextWave()
        {
            if (isSpawning)
            {
                StartCoroutine(StartNextWave());
            }
        }
        
        public void ClearAllEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            activeEnemies.Clear();
            activeEnemyCount = 0;
        }
        
        #endregion
        
        #region Wave Management
        
        private IEnumerator SpawnManager()
        {
            while (isSpawning)
            {
                // Check if we should start a new wave
                if (ShouldStartNewWave())
                {
                    yield return StartCoroutine(StartNextWave());
                }
                
                // Update difficulty
                UpdateDifficulty();
                
                yield return new WaitForSeconds(0.5f); // Check every half second
            }
        }
        
        private bool ShouldStartNewWave()
        {
            if (!settings.continuousWaves && activeEnemyCount > 0)
            {
                return false; // Wait for all enemies to be defeated in non-continuous mode
            }
            
            return activeEnemyCount < settings.maxConcurrentEnemies;
        }
        
        private IEnumerator StartNextWave()
        {
            currentWaveNumber++;
            
            // Get or generate wave data
            WaveData waveData = GetWaveData(currentWaveNumber);
            
            Debug.Log($"[EnemySpawner] Starting Wave {currentWaveNumber}: {waveData.enemyCount} enemies, interval: {waveData.spawnInterval}s");
            
            OnWaveStarted?.Invoke(currentWaveNumber);
            
            // Wait for wave delay (except for first wave)
            if (currentWaveNumber > 1)
            {
                yield return new WaitForSeconds(waveData.waveDelay);
            }
            
            // Spawn enemies for this wave
            yield return StartCoroutine(SpawnWave(waveData));
            
            OnWaveCompleted?.Invoke(currentWaveNumber);
        }
        
        private IEnumerator SpawnWave(WaveData waveData)
        {
            int enemiesToSpawn = waveData.enemyCount;
            int enemiesSpawned = 0;
            
            Debug.Log($"[EnemySpawner] SpawnWave: Attempting to spawn {enemiesToSpawn} enemies");
            Debug.Log($"[EnemySpawner] Available enemy types: {waveData.availableEnemies?.Count ?? 0}");
            
            // Validate wave data
            if (waveData.availableEnemies == null || waveData.availableEnemies.Count == 0)
            {
                Debug.LogError("[EnemySpawner] No available enemies in wave data!");
                yield break;
            }
            
            // Build weighted selection table
            BuildSpawnWeights(waveData.availableEnemies);
            Debug.Log($"[EnemySpawner] Built spawn weights for {spawnWeights.Count} enemy types");
            
            if (spawnWeights.Count == 0)
            {
                Debug.LogError("[EnemySpawner] No valid enemies can be spawned! Check spawn conditions.");
                yield break;
            }
            
            while (enemiesSpawned < enemiesToSpawn && isSpawning)
            {
                Debug.Log($"[EnemySpawner] Spawn attempt {enemiesSpawned + 1}/{enemiesToSpawn}");
                
                // Check concurrent enemy limit
                if (activeEnemyCount >= settings.maxConcurrentEnemies)
                {
                    Debug.Log($"[EnemySpawner] Hit concurrent enemy limit ({activeEnemyCount}/{settings.maxConcurrentEnemies})");
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                // Select enemy type to spawn
                EnemySpawnData selectedEnemy = SelectEnemyToSpawn(waveData.availableEnemies);
                if (selectedEnemy != null)
                {
                    Debug.Log($"[EnemySpawner] Selected enemy: {selectedEnemy.enemyConfig?.name ?? "NULL CONFIG"}");
                    
                    Vector3 spawnPosition = FindValidSpawnPosition();
                    if (spawnPosition != Vector3.zero)
                    {
                        Debug.Log($"[EnemySpawner] Found spawn position: {spawnPosition}");
                        
                        GameObject enemy = SpawnEnemy(selectedEnemy, spawnPosition, waveData);
                        if (enemy != null)
                        {
                            enemiesSpawned++;
                            Debug.Log($"[EnemySpawner] Successfully spawned enemy {enemiesSpawned}/{enemiesToSpawn}");
                            OnEnemySpawned?.Invoke(enemy);
                        }
                        else
                        {
                            Debug.LogError("[EnemySpawner] SpawnEnemy returned null!");
                        }
                    }
                    else
                    {
                        Debug.LogError("[EnemySpawner] Could not find valid spawn position!");
                    }
                }
                else
                {
                    Debug.LogError("[EnemySpawner] SelectEnemyToSpawn returned null!");
                }
                
                yield return new WaitForSeconds(waveData.spawnInterval);
            }
            
            Debug.Log($"[EnemySpawner] Wave completed. Spawned {enemiesSpawned}/{enemiesToSpawn} enemies");
        }
        
        #endregion
        
        #region Enemy Selection and Spawning
        
        private void BuildSpawnWeights(List<EnemySpawnData> availableEnemies)
        {
            spawnWeights.Clear();
            
            foreach (var spawnData in availableEnemies)
            {
                if (CanSpawnEnemy(spawnData))
                {
                    float weight = spawnData.spawnWeight;
                    
                    // Apply wave scaling
                    if (spawnData.scaleWithWave)
                    {
                        weight *= Mathf.Lerp(1f, 2f, currentWaveNumber / 20f);
                    }
                    
                    spawnWeights[spawnData] = weight;
                }
            }
        }
        
        private bool CanSpawnEnemy(EnemySpawnData spawnData)
        {
            if (currentWaveNumber < spawnData.minWaveToSpawn) return false;
            if (Random.value > spawnData.spawnChance) return false;
            if (spawnData.maxSpawnPerWave > 0)
            {
                // Count how many of this type are already spawned this wave
                int currentCount = CountEnemiesOfType(spawnData.enemyConfig.enemyType);
                if (currentCount >= spawnData.maxSpawnPerWave) return false;
            }
            
            return true;
        }
        
        private EnemySpawnData SelectEnemyToSpawn(List<EnemySpawnData> availableEnemies)
        {
            if (spawnWeights.Count == 0) return null;
            
            float totalWeight = 0f;
            foreach (var weight in spawnWeights.Values)
            {
                totalWeight += weight;
            }
            
            float randomValue = Random.value * totalWeight;
            float currentWeight = 0f;
            
            foreach (var kvp in spawnWeights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }
            
            // Fallback to first available
            foreach (var spawnData in availableEnemies)
            {
                if (CanSpawnEnemy(spawnData))
                {
                    return spawnData;
                }
            }
            
            return null;
        }
        
        private GameObject SpawnEnemy(EnemySpawnData spawnData, Vector3 position, WaveData waveData)
        {
            try
            {
                GameObject enemyObj = Instantiate(spawnData.enemyPrefab, position, Quaternion.identity);
                EnemyController controller = enemyObj.GetComponent<EnemyController>();
                
                if (controller != null)
                {
                    // Create scaled config for this enemy
                    EnemyConfig scaledConfig = CreateScaledConfig(spawnData.enemyConfig, waveData);
                    controller.Initialize(scaledConfig);
                    
                    activeEnemies.Add(enemyObj);
                    activeEnemyCount++;
                    
                    Debug.Log($"[EnemySpawner] Spawned {spawnData.enemyConfig.enemyType} at {position}");
                    return enemyObj;
                }
                else
                {
                    Debug.LogError($"[EnemySpawner] Spawned enemy prefab has no EnemyController!");
                    Destroy(enemyObj);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemySpawner] Error spawning enemy: {e.Message}");
            }
            
            return null;
        }
        
        private EnemyConfig CreateScaledConfig(EnemyConfig baseConfig, WaveData waveData)
        {
            // Create a runtime copy of the config with wave scaling applied
            EnemyConfig scaledConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            
            // Copy base values
            scaledConfig.enemyType = baseConfig.enemyType;
            scaledConfig.enemyName = baseConfig.enemyName;
            
            // Apply scaling
            float difficultyScale = currentDifficultyMultiplier;
            
            scaledConfig.maxHealth = Mathf.RoundToInt(baseConfig.maxHealth * waveData.healthMultiplier * difficultyScale);
            scaledConfig.moveSpeed = baseConfig.moveSpeed * waveData.speedMultiplier * difficultyScale;
            scaledConfig.attackDamage = baseConfig.attackDamage * waveData.damageMultiplier * difficultyScale;
            
            // Copy non-scaled values
            scaledConfig.deathDelay = baseConfig.deathDelay;
            scaledConfig.acceleration = baseConfig.acceleration;
            scaledConfig.rotationSpeed = baseConfig.rotationSpeed;
            scaledConfig.stopDistance = baseConfig.stopDistance;
            scaledConfig.detectionRange = baseConfig.detectionRange;
            scaledConfig.loseTargetRange = baseConfig.loseTargetRange;
            scaledConfig.targetLayers = baseConfig.targetLayers;
            scaledConfig.attackRange = baseConfig.attackRange;
            scaledConfig.attackCooldown = baseConfig.attackCooldown;
            scaledConfig.stateChangeDelay = baseConfig.stateChangeDelay;
            scaledConfig.idleTime = baseConfig.idleTime;
            scaledConfig.canPatrol = baseConfig.canPatrol;
            scaledConfig.patrolRadius = baseConfig.patrolRadius;
            scaledConfig.enemyColor = baseConfig.enemyColor;
            scaledConfig.scale = baseConfig.scale;
            
            return scaledConfig;
        }
        
        #endregion
        
        #region Spawn Position Finding
        
        private Vector3 FindValidSpawnPosition()
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("[EnemySpawner] No player transform available for spawn positioning");
                return Vector3.zero;
            }
            
            Debug.Log($"[EnemySpawner] FindValidSpawnPosition: Player at {playerTransform.position}");
            Debug.Log($"[EnemySpawner] Spawn distance range: {settings.minDistanceFromPlayer} - {settings.maxDistanceFromPlayer}");
            Debug.Log($"[EnemySpawner] NavMesh required: {settings.requireNavMeshSurface}, NavMeshSurface found: {navMeshSurface != null}");
            
            for (int attempt = 0; attempt < settings.maxSpawnAttempts; attempt++)
            {
                Vector3 candidatePosition = GenerateRandomSpawnPosition();
                Debug.Log($"[EnemySpawner] Attempt {attempt + 1}: Testing position {candidatePosition}");
                
                if (IsValidSpawnPosition(candidatePosition))
                {
                    Debug.Log($"[EnemySpawner] Found valid spawn position: {candidatePosition} on attempt {attempt + 1}");
                    return candidatePosition;
                }
            }
            
            Debug.LogError($"[EnemySpawner] Could not find valid spawn position after {settings.maxSpawnAttempts} attempts");
            return Vector3.zero;
        }
        
        private Vector3 GenerateRandomSpawnPosition()
        {
            Vector3 playerPos = playerTransform.position;
            
            // Generate position in ring around player
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(settings.minDistanceFromPlayer, settings.maxDistanceFromPlayer);
            
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            
            return playerPos + offset;
        }
        
        private bool IsValidSpawnPosition(Vector3 position)
        {
            Debug.Log($"[EnemySpawner] IsValidSpawnPosition: Checking {position}");
            
            // Check if position is on NavMesh
            if (settings.requireNavMeshSurface)
            {
                NavMeshHit hit;
                bool onNavMesh = NavMesh.SamplePosition(position, out hit, settings.navMeshSampleDistance, NavMesh.AllAreas);
                Debug.Log($"[EnemySpawner] NavMesh check: {onNavMesh}, Sample distance: {settings.navMeshSampleDistance}");
                
                if (!onNavMesh)
                {
                    Debug.Log("[EnemySpawner] Position rejected: Not on NavMesh");
                    return false;
                }
                position = hit.position; // Use the corrected NavMesh position
                Debug.Log($"[EnemySpawner] Corrected position to NavMesh: {position}");
            }
            
            // Check distance from player
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(position, playerTransform.position);
                Debug.Log($"[EnemySpawner] Distance to player: {distanceToPlayer} (required: {settings.minDistanceFromPlayer} - {settings.maxDistanceFromPlayer})");
                
                if (distanceToPlayer < settings.minDistanceFromPlayer || distanceToPlayer > settings.maxDistanceFromPlayer)
                {
                    Debug.Log("[EnemySpawner] Position rejected: Outside distance range");
                    return false;
                }
            }
            
            // Check for obstacles
            bool hasObstacles = Physics.CheckSphere(position, 1f, settings.spawnObstacleLayers);
            Debug.Log($"[EnemySpawner] Obstacle check: {hasObstacles} (layers: {settings.spawnObstacleLayers})");
            if (hasObstacles)
            {
                Debug.Log("[EnemySpawner] Position rejected: Obstacles detected");
                return false;
            }
            
            // Check if too close to existing enemies
            int nearbyEnemies = 0;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && Vector3.Distance(position, enemy.transform.position) < 2f)
                {
                    nearbyEnemies++;
                }
            }
            Debug.Log($"[EnemySpawner] Nearby enemies: {nearbyEnemies}");
            if (nearbyEnemies > 0)
            {
                Debug.Log("[EnemySpawner] Position rejected: Too close to existing enemies");
                return false;
            }
            
            Debug.Log("[EnemySpawner] Position ACCEPTED");
            return true;
        }
        
        #endregion
        
        #region Utility Methods
        
        private WaveData GetWaveData(int waveNumber)
        {
            // Try to find existing wave data
            foreach (var wave in waves)
            {
                if (wave.waveNumber == waveNumber)
                {
                    return wave;
                }
            }
            
            // Generate new wave data based on template
            return GenerateWaveData(waveNumber);
        }
        
        private WaveData GenerateWaveData(int waveNumber)
        {
            WaveData newWave = new WaveData();
            
            if (baseWaveTemplate != null)
            {
                newWave.waveNumber = waveNumber;
                newWave.enemyCount = Mathf.RoundToInt(baseWaveTemplate.enemyCount * (1f + (waveNumber - 1) * 0.2f));
                newWave.spawnInterval = Mathf.Max(0.2f, baseWaveTemplate.spawnInterval * (1f - (waveNumber - 1) * 0.05f));
                newWave.waveDelay = baseWaveTemplate.waveDelay;
                newWave.availableEnemies = new List<EnemySpawnData>(baseWaveTemplate.availableEnemies);
                
                // Scale multipliers with wave progression
                float waveScale = 1f + (waveNumber - 1) * 0.1f;
                newWave.healthMultiplier = baseWaveTemplate.healthMultiplier * waveScale;
                newWave.speedMultiplier = Mathf.Min(2f, baseWaveTemplate.speedMultiplier * (1f + (waveNumber - 1) * 0.05f));
                newWave.damageMultiplier = baseWaveTemplate.damageMultiplier * waveScale;
            }
            else
            {
                // Default wave generation
                newWave.waveNumber = waveNumber;
                newWave.enemyCount = 5 + waveNumber * 2;
                newWave.spawnInterval = Mathf.Max(0.5f, 2f - waveNumber * 0.1f);
                newWave.waveDelay = 3f;
                newWave.healthMultiplier = 1f + waveNumber * 0.1f;
                newWave.speedMultiplier = 1f + waveNumber * 0.05f;
                newWave.damageMultiplier = 1f + waveNumber * 0.1f;
            }
            
            return newWave;
        }
        
        private void GenerateInitialWaves()
        {
            for (int i = 1; i <= 5; i++)
            {
                waves.Add(GenerateWaveData(i));
            }
        }
        
        private void UpdateDifficulty()
        {
            if (!settings.enableProgressiveDifficulty) return;
            
            float gameTime = Time.time - gameStartTime;
            float difficultyProgress = gameTime * settings.difficultyScaleRate;
            
            currentDifficultyMultiplier = Mathf.Clamp(
                settings.difficultyCurve.Evaluate(difficultyProgress), 
                1f, 
                settings.maxDifficultyMultiplier
            );
        }
        
        private int CountEnemiesOfType(EnemyType type)
        {
            int count = 0;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    var controller = enemy.GetComponent<EnemyController>();
                    if (controller != null && controller.EnemyType == type)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        
        private void HandleEnemyKilled(GameObject enemy)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
                activeEnemyCount--;
                Debug.Log($"[EnemySpawner] Enemy killed. Active enemies: {activeEnemyCount}");
            }
        }
        
        #endregion
        
        #region Gizmos and Debug
        
        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;
            
            // Draw spawn area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, settings.minDistanceFromPlayer);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, settings.maxDistanceFromPlayer);
            
            // Draw spawn positions for active enemies
            Gizmos.color = Color.green;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawWireCube(enemy.transform.position, Vector3.one);
                }
            }
        }
        
        #endregion
    }
    
    // Custom ReadOnly attribute for inspector
    public class ReadOnlyAttribute : PropertyAttribute { }
    
    #if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
    #endif
}