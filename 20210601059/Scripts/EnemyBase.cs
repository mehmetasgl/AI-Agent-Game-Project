using UnityEngine;
namespace Mehmet{
public enum EnemyType
{
    Stationary,
    Patrol,
    Chaser
}

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Properties")]
    [Tooltip("Enemy type identifier")]
    public EnemyType enemyType;
    
    [Tooltip("Enemy health points")]
    public float health = 100f;
    
    [Tooltip("Enemy movement speed")]
    public float moveSpeed = 2f;
    
    [Tooltip("Detection range for player")]
    public float detectionRange = 5f;
    
    [Tooltip("Attack damage")]
    public float attackDamage = 10f;
    
    [Tooltip("Attack cooldown in seconds")]
    public float attackCooldown = 1f;

    [Header("Visual")]
    [Tooltip("Enemy sprite renderer")]
    public SpriteRenderer spriteRenderer;
    
    [Tooltip("Enemy color")]
    public Color enemyColor = Color.red;

    protected Transform player;
    protected float lastAttackTime;
    public bool isAlive { get; protected set; } = true;
    protected Vector3 spawnPosition;

    protected virtual void Start()
    {
        spawnPosition = transform.position;
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
            spriteRenderer.color = enemyColor;
        
        FindPlayer();
    }

    protected virtual void Update()
    {
        if (!isAlive) return;
        
        if (player == null)
            FindPlayer();
    }

    protected void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    protected bool IsPlayerInRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    protected bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    protected void PerformAttack()
    {
        if (!CanAttack()) return;
        
        lastAttackTime = Time.time;
        
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && !pc.isAlive)
            {
                return;
            }
            
            if (pc != null)
            {
                pc.TakeDamage(attackDamage);
                Debug.Log($"💥 {enemyType} dealt {attackDamage} damage to player!");
            }
            
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashAttack());
            }
        }
    }

    private System.Collections.IEnumerator FlashAttack()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;
    }

    public virtual void TakeDamage(float damage)
    {
        if (!isAlive) return;
        
        health -= damage;
        
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRed());
        }
        
        if (health <= 0)
        {
            Die();
        }
    }

    protected System.Collections.IEnumerator FlashRed()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    protected virtual void Die()
    {
        isAlive = false;
        Debug.Log($"{enemyType} enemy died!");
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f);
        }
        
        Destroy(gameObject, 1f);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    
    public virtual void Initialize()
    {
        Debug.Log($"{enemyType} initialized at {transform.position}");
    }
}
}