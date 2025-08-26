using UnityEngine;
using Enemies.Core;
namespace Enemies.Weapons
{
    // Enemy Projectile Behavior
public class EnemyProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float spawnTime;
    private LayerMask targetLayers;
    
    private Rigidbody rb;
    private bool isActive = false;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.freezeRotation = true;
        }
    }
    
    public void Initialize(Vector3 startPos, Vector3 dir, ProjectileConfig config)
    {
        transform.position = startPos;
        direction = dir.normalized;
        speed = config.speed;
        damage = config.damage;
        lifetime = config.lifetime;
        targetLayers = config.targetLayers;
        spawnTime = Time.time;
        
        // Set visual
        transform.localScale = config.scale;
        var renderer = GetComponent<MeshRenderer>();
        if (renderer)
        {
            Material projectileMat = new Material(Shader.Find("Standard"));
            projectileMat.color = config.projectileColor;
            renderer.material = projectileMat;
        }
        
        // Set layer and tag
        gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
        gameObject.tag = "EnemyProjectile";
        
        isActive = true;
        gameObject.SetActive(true);
    }
    
    void FixedUpdate()
    {
        if (!isActive) return;
        
        // Move projectile
        rb.linearVelocity = direction * speed;
        
        // Check lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            DeactivateProjectile();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        // Check if hit valid target
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            // Hit player or other target
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            
            // Impact effect
            CreateImpactEffect();
            DeactivateProjectile();
        }
        // Hit wall or obstacle
        else if (other.gameObject.layer == LayerMask.NameToLayer("Environment") || 
                 other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            CreateImpactEffect();
            DeactivateProjectile();
        }
    }
    
    private void CreateImpactEffect()
    {
        // Simple impact effect
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.transform.position = transform.position;
        impact.transform.localScale = Vector3.one * 0.4f;
        impact.GetComponent<MeshRenderer>().material.color = Color.orange;
        Destroy(impact.GetComponent<Collider>());
        Destroy(impact, 0.2f);
    }
    
    private void DeactivateProjectile()
    {
        isActive = false;
        rb.linearVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }
}

}