using System.Collections.Generic;
using UnityEngine;
using Enemies.Core;

namespace Enemies.Pooling
{
    [System.Serializable]
    public class EnemyPoolData
    {
        [Header("Pool Configuration")]
        public EnemyConfig enemyConfig;
        public GameObject enemyPrefab;
        public int initialPoolSize = 10;
        public int maxPoolSize = 50;
        public bool allowGrowth = true;
        
        [Header("Runtime Info")]
        [SerializeField, ReadOnly] public int activeCount;
        [SerializeField, ReadOnly] public int availableCount;
        [SerializeField, ReadOnly] public int totalPoolSize;
    }

    /// <summary>
    /// Object pool for managing enemy GameObjects efficiently
    /// Reduces garbage collection and improves performance for enemy spawning
    /// </summary>
    public class EnemyObjectPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private List<EnemyPoolData> poolConfigurations = new List<EnemyPoolData>();
        [SerializeField] private Transform poolParent;
        [SerializeField] private bool debugMode = false;
        
        // Pool storage
        private Dictionary<EnemyType, Queue<GameObject>> availablePools = new Dictionary<EnemyType, Queue<GameObject>>();
        private Dictionary<EnemyType, HashSet<GameObject>> activePools = new Dictionary<EnemyType, HashSet<GameObject>>();
        private Dictionary<EnemyType, EnemyPoolData> poolDataLookup = new Dictionary<EnemyType, EnemyPoolData>();
        
        // Events
        public System.Action<EnemyType, GameObject> OnEnemySpawned;
        public System.Action<EnemyType, GameObject> OnEnemyReturned;
        
        private void Awake()
        {
            // Create pool parent if not assigned
            if (poolParent == null)
            {
                GameObject poolParentObj = new GameObject("Enemy Pool");
                poolParent = poolParentObj.transform;
                poolParent.SetParent(transform);
            }
            
            InitializePools();
            
            // Subscribe to enemy death events for automatic return
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }
        
        private void OnDestroy()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }
        
        #region Pool Initialization
        
        private void InitializePools()
        {
            Debug.Log("[EnemyObjectPool] Initializing enemy pools...");
            
            foreach (var poolData in poolConfigurations)
            {
                if (poolData.enemyConfig == null || poolData.enemyPrefab == null)
                {
                    Debug.LogWarning($"[EnemyObjectPool] Invalid pool configuration - missing config or prefab");
                    continue;
                }
                
                EnemyType enemyType = poolData.enemyConfig.enemyType;
                
                // Initialize pool structures
                availablePools[enemyType] = new Queue<GameObject>();
                activePools[enemyType] = new HashSet<GameObject>();
                poolDataLookup[enemyType] = poolData;
                
                // Pre-instantiate initial pool
                CreateInitialPool(poolData);
                
                if (debugMode)
                {
                    Debug.Log($"[EnemyObjectPool] Initialized pool for {enemyType}: {poolData.initialPoolSize} objects");
                }
            }
            
            Debug.Log($"[EnemyObjectPool] Pool initialization complete. {poolConfigurations.Count} enemy types pooled.");
        }
        
        private void CreateInitialPool(EnemyPoolData poolData)
        {
            EnemyType enemyType = poolData.enemyConfig.enemyType;
            var availablePool = availablePools[enemyType];
            
            for (int i = 0; i < poolData.initialPoolSize; i++)
            {
                GameObject enemyObj = CreatePooledEnemy(poolData);
                if (enemyObj != null)
                {
                    availablePool.Enqueue(enemyObj);
                    poolData.availableCount++;
                    poolData.totalPoolSize++;
                }
            }
        }
        
        private GameObject CreatePooledEnemy(EnemyPoolData poolData)
        {
            try
            {
                GameObject enemyObj = Instantiate(poolData.enemyPrefab, poolParent);
                enemyObj.name = $"{poolData.enemyConfig.enemyType}_Pooled_{poolData.totalPoolSize}";
                
                // Ensure enemy has required components
                EnemyController controller = enemyObj.GetComponent<EnemyController>();
                if (controller == null)
                {
                    Debug.LogError($"[EnemyObjectPool] Prefab {poolData.enemyPrefab.name} missing EnemyController!");
                    Destroy(enemyObj);
                    return null;
                }
                
                // Add poolable component for tracking
                IPoolable poolable = enemyObj.GetComponent<IPoolable>();
                if (poolable == null)
                {
                    enemyObj.AddComponent<PoolableEnemy>();
                }
                
                // Initialize but don't activate
                controller.Initialize(poolData.enemyConfig);
                ResetEnemyToPoolState(enemyObj);
                
                return enemyObj;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnemyObjectPool] Error creating pooled enemy: {e.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Pool Operations
        
        /// <summary>
        /// Get an enemy from the pool for spawning
        /// </summary>
        public GameObject GetEnemy(EnemyType enemyType, Vector3 position, EnemyConfig scaledConfig = null)
        {
            if (!availablePools.ContainsKey(enemyType))
            {
                Debug.LogError($"[EnemyObjectPool] No pool configured for enemy type: {enemyType}");
                return null;
            }
            
            var availablePool = availablePools[enemyType];
            var activePool = activePools[enemyType];
            var poolData = poolDataLookup[enemyType];
            
            GameObject enemyObj = null;
            
            // Try to get from available pool
            if (availablePool.Count > 0)
            {
                enemyObj = availablePool.Dequeue();
                poolData.availableCount--;
            }
            // Create new if growth allowed and under max size
            else if (poolData.allowGrowth && poolData.totalPoolSize < poolData.maxPoolSize)
            {
                enemyObj = CreatePooledEnemy(poolData);
                if (enemyObj != null)
                {
                    poolData.totalPoolSize++;
                    if (debugMode)
                    {
                        Debug.Log($"[EnemyObjectPool] Grew pool for {enemyType}. New size: {poolData.totalPoolSize}");
                    }
                }
            }
            
            if (enemyObj == null)
            {
                Debug.LogWarning($"[EnemyObjectPool] No available {enemyType} enemies in pool!");
                return null;
            }
            
            // Setup enemy for spawning
            PrepareEnemyForSpawn(enemyObj, position, scaledConfig);
            
            // Move to active pool
            activePool.Add(enemyObj);
            poolData.activeCount++;
            
            OnEnemySpawned?.Invoke(enemyType, enemyObj);
            
            if (debugMode)
            {
                Debug.Log($"[EnemyObjectPool] Spawned {enemyType} from pool. Active: {poolData.activeCount}, Available: {poolData.availableCount}");
            }
            
            return enemyObj;
        }
        
        /// <summary>
        /// Return an enemy to the pool
        /// </summary>
        public void ReturnEnemy(GameObject enemyObj)
        {
            if (enemyObj == null) return;
            
            EnemyController controller = enemyObj.GetComponent<EnemyController>();
            if (controller == null)
            {
                Debug.LogWarning($"[EnemyObjectPool] Trying to return object without EnemyController: {enemyObj.name}");
                return;
            }
            
            EnemyType enemyType = controller.EnemyType;
            
            if (!activePools.ContainsKey(enemyType) || !activePools[enemyType].Contains(enemyObj))
            {
                Debug.LogWarning($"[EnemyObjectPool] Trying to return enemy not from this pool: {enemyObj.name}");
                return;
            }
            
            // Move from active to available pool
            var activePool = activePools[enemyType];
            var availablePool = availablePools[enemyType];
            var poolData = poolDataLookup[enemyType];
            
            activePool.Remove(enemyObj);
            availablePool.Enqueue(enemyObj);
            
            poolData.activeCount--;
            poolData.availableCount++;
            
            // Reset enemy state
            ResetEnemyToPoolState(enemyObj);
            
            OnEnemyReturned?.Invoke(enemyType, enemyObj);
            
            if (debugMode)
            {
                Debug.Log($"[EnemyObjectPool] Returned {enemyType} to pool. Active: {poolData.activeCount}, Available: {poolData.availableCount}");
            }
        }
        
        /// <summary>
        /// Return all active enemies to pool (for cleanup/reset)
        /// </summary>
        public void ReturnAllEnemies()
        {
            var enemiesToReturn = new List<GameObject>();
            
            foreach (var activePool in activePools.Values)
            {
                enemiesToReturn.AddRange(activePool);
            }
            
            foreach (var enemy in enemiesToReturn)
            {
                ReturnEnemy(enemy);
            }
            
            if (debugMode)
            {
                Debug.Log($"[EnemyObjectPool] Returned {enemiesToReturn.Count} enemies to pools");
            }
        }
        
        #endregion
        
        #region Enemy State Management
        
       private void PrepareEnemyForSpawn(GameObject enemyObj, Vector3 position, EnemyConfig scaledConfig = null)
{
    // Set position and rotation
    enemyObj.transform.position = position;
    enemyObj.transform.rotation = Quaternion.identity;
    
    // Activate the enemy first so NavMeshAgent can function properly
    enemyObj.SetActive(true);
    
    // Handle NavMeshAgent positioning
    var navAgent = enemyObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (navAgent != null)
    {
        navAgent.enabled = false; // Disable temporarily
        navAgent.enabled = true;  // Re-enable to refresh NavMesh placement
        
        // Try to warp to position if on NavMesh
        if (navAgent.isOnNavMesh)
        {
            navAgent.Warp(position);
            navAgent.isStopped = false;
        }
    }
    
    // Reset and reinitialize enemy
    EnemyController controller = enemyObj.GetComponent<EnemyController>();
    if (controller != null)
    {
        // Use scaled config if provided, otherwise use original
        EnemyConfig configToUse = scaledConfig ?? poolDataLookup[controller.EnemyType].enemyConfig;
        controller.Initialize(configToUse);
    }
    
    // Notify poolable component
    IPoolable poolable = enemyObj.GetComponent<IPoolable>();
    poolable?.OnSpawnFromPool();
}
        
        private void ResetEnemyToPoolState(GameObject enemyObj)
        {
            // Deactivate
            enemyObj.SetActive(false);
            
            // Reset transform
            enemyObj.transform.position = poolParent.position;
            enemyObj.transform.rotation = Quaternion.identity;
            
            // Reset components
            EnemyController controller = enemyObj.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.Target = null;
                // Don't call Deactivate() as that sets IsActive to false permanently
                
                // Reset health
                var health = controller.GetComponent<EnemyHealth>();
                if (health != null && controller.Config != null)
                {
                    health.Initialize(controller.Config);
                }
            }
            
            // Reset visual state
            ResetVisualState(enemyObj);
            
            // Notify poolable component
            IPoolable poolable = enemyObj.GetComponent<IPoolable>();
            poolable?.OnReturnToPool();
        }
        
       private void ResetVisualState(GameObject enemyObj)
{
    // Reset renderer materials and colors
    var renderer = enemyObj.GetComponent<MeshRenderer>();
    if (renderer != null)
    {
        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller != null && controller.Config != null)
        {
            // Reset scale
            enemyObj.transform.localScale = controller.Config.scale;
            
            // Reset material color if needed
            if (renderer.material.HasProperty("_Color"))
            {
                Color resetColor = controller.Config.enemyColor;
                resetColor.a = 1f; // Ensure full opacity
                renderer.material.color = resetColor;
            }
        }
    }
    
    // Re-enable colliders and physics
    var colliders = enemyObj.GetComponents<Collider>();
    foreach (var collider in colliders)
    {
        if (collider != null)
        {
            collider.enabled = true;
        }
    }
    
    var rigidbody = enemyObj.GetComponent<Rigidbody>();
    if (rigidbody != null)
    {
        rigidbody.isKinematic = false;
        rigidbody.detectCollisions = true;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }
    
    // Handle NavMeshAgent carefully
    var navAgent = enemyObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (navAgent != null)
    {
        // Only modify NavMeshAgent if it's properly initialized
        if (navAgent.isOnNavMesh)
        {
            navAgent.enabled = true;
            navAgent.isStopped = false;
            navAgent.ResetPath();
        }
        else
        {
            // If not on NavMesh, just ensure it's enabled for later use
            navAgent.enabled = true;
            // Don't set isStopped if not on NavMesh as it will cause the error
        }
    }
}
        
        #endregion
        
        #region Event Handlers
        
        private void HandleEnemyKilled(GameObject enemyObj)
        {
            if (enemyObj == null) return;
            
            // Check if this enemy belongs to our pools
            EnemyController controller = enemyObj.GetComponent<EnemyController>();
            if (controller != null && activePools.ContainsKey(controller.EnemyType))
            {
                // Delay return to allow death effects to play
                StartCoroutine(DelayedReturnToPool(enemyObj, controller.Config?.deathDelay ?? 0.5f));
            }
        }
        
        private System.Collections.IEnumerator DelayedReturnToPool(GameObject enemyObj, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (enemyObj != null)
            {
                ReturnEnemy(enemyObj);
            }
        }
        
        #endregion
        
        #region Public Interface
        
        public bool HasPool(EnemyType enemyType)
        {
            return availablePools.ContainsKey(enemyType);
        }
        
        public int GetActiveCount(EnemyType enemyType)
        {
            return poolDataLookup.ContainsKey(enemyType) ? poolDataLookup[enemyType].activeCount : 0;
        }
        
        public int GetAvailableCount(EnemyType enemyType)
        {
            return poolDataLookup.ContainsKey(enemyType) ? poolDataLookup[enemyType].availableCount : 0;
        }
        
        public int GetTotalPoolSize(EnemyType enemyType)
        {
            return poolDataLookup.ContainsKey(enemyType) ? poolDataLookup[enemyType].totalPoolSize : 0;
        }
        
        /// <summary>
        /// Add a new enemy type to the pool at runtime
        /// </summary>
        public void AddEnemyTypeToPool(EnemyPoolData poolData)
        {
            if (poolData?.enemyConfig == null || poolData.enemyPrefab == null)
            {
                Debug.LogError("[EnemyObjectPool] Invalid pool data provided");
                return;
            }
            
            EnemyType enemyType = poolData.enemyConfig.enemyType;
            
            if (availablePools.ContainsKey(enemyType))
            {
                Debug.LogWarning($"[EnemyObjectPool] Pool for {enemyType} already exists");
                return;
            }
            
            poolConfigurations.Add(poolData);
            
            // Initialize the new pool
            availablePools[enemyType] = new Queue<GameObject>();
            activePools[enemyType] = new HashSet<GameObject>();
            poolDataLookup[enemyType] = poolData;
            
            CreateInitialPool(poolData);
            
            Debug.Log($"[EnemyObjectPool] Added new pool for {enemyType}");
        }
        
        #endregion
        
        #region Debug and Gizmos
        
        private void OnDrawGizmosSelected()
        {
            if (poolParent != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(poolParent.position, Vector3.one * 2f);
            }
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogPoolStats()
        {
            Debug.Log("=== ENEMY POOL STATS ===");
            foreach (var poolData in poolConfigurations)
            {
                EnemyType type = poolData.enemyConfig.enemyType;
                Debug.Log($"{type}: Active={poolData.activeCount}, Available={poolData.availableCount}, Total={poolData.totalPoolSize}");
            }
            Debug.Log("========================");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Interface for objects that can be pooled
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }
    
    /// <summary>
    /// Component that makes enemies poolable
    /// </summary>
    public class PoolableEnemy : MonoBehaviour, IPoolable
    {
        public void OnSpawnFromPool()
        {
            // Reset any spawn-specific state here if needed
        }
        
        public void OnReturnToPool()
        {
            // Reset any pool-return-specific state here if needed
        }
    }
}