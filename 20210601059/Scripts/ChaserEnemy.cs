using UnityEngine;

namespace Mehmet{
public class ChaserEnemy : EnemyBase
{
    [Header("Chaser Settings")]
    [Tooltip("Chase speed multiplier")]
    public float chaseSpeedMultiplier = 1.5f;
    
    [Tooltip("Stop chase distance from spawn")]
    public float maxChaseDistance = 15f;
    
    [Tooltip("Attack range")]
    public float attackRange = 1.5f;
    
    [Tooltip("Can shoot projectiles")]
    public bool canShoot = true;
    
    [Tooltip("Projectile prefab (optional)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Shoot cooldown")]
    public float shootCooldown = 1.5f;
    
    private float lastShootTime;
    private bool isChasing;
    private Vector3 idlePosition;

    protected override void Start()
    {
        enemyType = EnemyType.Chaser;
        enemyColor = new Color(0.8f, 0.2f, 0.8f);
        moveSpeed = 2.5f;
        detectionRange = 8f;
        
        base.Start();
        
        idlePosition = spawnPosition;
    }

    protected override void Update()
    {
        base.Update();
        
        if (!isAlive) return;
        
        if (IsPlayerInRange() && !IsTooFarFromSpawn())
        {
            isChasing = true;
            ChasePlayer();
        }
        else
        {
            if (isChasing)
            {
                ReturnToSpawn();
            }
            else
            {
                Idle();
            }
        }
    }

    bool IsTooFarFromSpawn()
    {
        return Vector3.Distance(transform.position, spawnPosition) > maxChaseDistance;
    }

    void ChasePlayer()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer > attackRange)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * chaseSpeedMultiplier * Time.deltaTime;
        }
        else
        {
            if (canShoot && Time.time >= lastShootTime + shootCooldown)
            {
                ShootAtPlayer();
            }
            else if (!canShoot)
            {
                PerformAttack();
            }
        }
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
            Debug.Log($"🔫 Chaser shot projectile! Damage: {attackDamage}");
        }
        else
        {
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * 6f;
            }
        }
        
        Destroy(projectile, 4f);
    }
    else
    {
        PerformAttack();
    }
}

    void ReturnToSpawn()
    {
        float distanceToSpawn = Vector3.Distance(transform.position, spawnPosition);
        
        if (distanceToSpawn > 0.5f)
        {
            Vector3 direction = (spawnPosition - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else
        {
            isChasing = false;
        }
    }

    void Idle()
    {
        float moveRange = 2f;
        float speed = 0.5f;
        
        float offsetX = Mathf.Sin(Time.time * speed) * moveRange;
        float offsetY = Mathf.Cos(Time.time * speed * 0.7f) * moveRange;
        
        idlePosition = spawnPosition + new Vector3(offsetX, offsetY, 0);
        
        transform.position = Vector3.MoveTowards(
            transform.position, 
            idlePosition, 
            moveSpeed * 0.5f * Time.deltaTime
        );
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Gizmos.color = Color.magenta;
        Vector3 spawnPos = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(spawnPos, maxChaseDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
}