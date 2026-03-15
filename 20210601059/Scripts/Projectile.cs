using UnityEngine;

namespace Mehmet{
public class Projectile : MonoBehaviour
{
    [Header("Projectile Properties")]
    [Tooltip("Damage dealt on hit")]
    public float damage = 25f;
    
    [Tooltip("Projectile speed")]
    public float speed = 8f;
    
    [Tooltip("Lifetime in seconds")]
    public float lifetime = 3f;
    
    [Tooltip("Who shot this projectile")]
    public string shooter = "Player"; 
    
    private Rigidbody2D rb;
    private Vector2 direction;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        Destroy(gameObject, lifetime);
    }
    
    public void Initialize(Vector2 dir, string shooterTag, float dmg = 25f)
    {
        direction = dir.normalized;
        shooter = shooterTag;
        damage = dmg;
        
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        rb.velocity = direction * speed;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("Wall"))
    {
        Debug.Log("💥 Projectile hit wall");
        Destroy(gameObject);
        return;
    }
    
    if (collision.CompareTag(shooter))
    {
        return;
    }
    
    if (shooter == "Player")
    {
        EnemyBase enemy = collision.GetComponent<EnemyBase>();
        if (enemy != null && enemy.isAlive)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"💥 Player projectile hit {enemy.enemyType}! Damage: {damage}");
            Destroy(gameObject);
            return;
        }
    }
    
    if (shooter == "Enemy")
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null && player.isAlive)
            {
                player.TakeDamage(damage);
                Debug.Log($"💥 Enemy projectile hit Player! Damage: {damage}");
                Destroy(gameObject);
                return;
            }
        }
    }
}
    
    void OnBecameInvisible()
    {
        Destroy(gameObject, 0.5f);
    }
}
}