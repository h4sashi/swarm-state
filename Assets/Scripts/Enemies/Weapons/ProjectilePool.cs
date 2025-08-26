using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Weapons
{
    // Projectile Pool for reusing projectiles
public class ProjectilePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 50;
    [SerializeField] private GameObject projectilePrefab;
    
    private Queue<EnemyProjectile> projectilePool;
    public ProjectileConfig config;
    
    void Start()
    {
        if (!config)
        {
            // Try to find a default config
            config = Resources.Load<ProjectileConfig>("DefaultProjectileConfig");
        }
        
        if (config)
        {
            Initialize(config);
        }
    }
    
    public void Initialize(ProjectileConfig projectileConfig)
    {
        this.config = projectileConfig;
        CreatePool();
    }
    
    private void CreatePool()
    {
        projectilePool = new Queue<EnemyProjectile>();
        
        // Create projectile prefab if not assigned
        if (!projectilePrefab)
        {
            projectilePrefab = CreateProjectilePrefab();
        }
        
        // Populate pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectileObj = Instantiate(projectilePrefab, transform);
            EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
            projectileObj.SetActive(false);
            projectilePool.Enqueue(projectile);
        }
    }
    
    private GameObject CreateProjectilePrefab()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        prefab.name = "ProjectilePrefab";
        
        // Add components
        var collider = prefab.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        
        prefab.AddComponent<Rigidbody>();
        prefab.AddComponent<EnemyProjectile>();
        
        return prefab;
    }
    
    public EnemyProjectile GetProjectile()
    {
        if (projectilePool.Count > 0)
        {
            return projectilePool.Dequeue();
        }
        
        // Pool exhausted - create new projectile
        Debug.LogWarning("Projectile pool exhausted! Consider increasing pool size.");
        GameObject newProjectileObj = Instantiate(projectilePrefab, transform);
        return newProjectileObj.GetComponent<EnemyProjectile>();
    }
    
    public void ReturnProjectile(EnemyProjectile projectile)
    {
        if (projectile != null)
        {
            projectile.gameObject.SetActive(false);
            projectilePool.Enqueue(projectile);
        }
    }
}
}