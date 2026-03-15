using UnityEngine;

namespace Mehmet{
public class StationaryEnemy : EnemyBase
{
    [Header("Stationary Settings")]
    [Tooltip("Rotation speed when tracking player")]
    public float rotationSpeed = 180f;
    
    [Tooltip("Projectile prefab for shooting (optional)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Shoot rate in seconds")]
    public float shootRate = 2f;
    
    private float lastShootTime;

    protected override void Start()
    {
        enemyType = EnemyType.Stationary;
        enemyColor = new Color(0.8f, 0.2f, 0.2f);
        detectionRange = 7f;
        
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        
        if (!isAlive) return;
        
        if (IsPlayerInRange())
        {
            LookAtPlayer();
            
            if (Time.time >= lastShootTime + shootRate)
            {
                ShootAtPlayer();
            }
        }
    }

    void LookAtPlayer()
    {
        if (player == null) return;
        
        Vector3 direction = player.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90f);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime
        );
    }

    void ShootAtPlayer()
{
    lastShootTime = Time.time;
    
    if (projectilePrefab != null && player != null)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        GameObject projectile = Instantiate(
            projectilePrefab, 
            transform.position + direction * 0.5f,  
            Quaternion.identity
        );
        
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(direction, "Enemy", attackDamage);
        }
        else
        {
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * 5f;
            }
        }
        
        Destroy(projectile, 3f);
        Debug.Log($"🔫 {enemyType} shot projectile at player!");
    }
    else
    {
        PerformAttack();
    }
}
}
}