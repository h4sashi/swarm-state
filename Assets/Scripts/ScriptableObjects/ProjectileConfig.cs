using UnityEngine;
// Configuration for enemy projectiles via Scriptable Objects

[CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Hanzo/Projectile Config")]
public class ProjectileConfig : ScriptableObject
{
    [Header("Movement")]
    public float speed = 10f;
    public float lifetime = 3f;

    [Header("Combat")]
    public float damage = 1f;
    public LayerMask targetLayers = 1 << 7; // Player layer

    [Header("Visual")]
    public Color projectileColor = Color.yellow;
    public Vector3 scale = new Vector3(0.2f, 0.2f, 0.2f);
}