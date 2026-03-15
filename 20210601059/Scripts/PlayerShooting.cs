using UnityEngine;

namespace Mehmet{
public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("Projectile prefab to shoot")]
    public GameObject projectilePrefab;
    
    [Tooltip("Shoot damage")]
    public float shootDamage = 25f;
    
    [Tooltip("Projectile speed")]
    public float projectileSpeed = 10f;
    
    [Tooltip("Shoot cooldown")]
    public float shootCooldown = 0.3f;
    
    [Header("Ammo System")]
    [Tooltip("Current ammo count")]
    public int currentAmmo = 30;
    
    [Tooltip("Maximum ammo capacity")]
    public int maxAmmo = 30;
    
    [Tooltip("Infinite ammo mode")]
    public bool infiniteAmmo = false;

    private float lastShootTime;
    private Camera mainCamera;
    private PlayerController playerController;

    void Start()
    {
        mainCamera = Camera.main;
        playerController = GetComponent<PlayerController>();
        
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    void Update()
    {
        if (playerController != null && !playerController.isAlive)
            return;
        
        if (Input.GetMouseButton(0) && CanShoot())
        {
            Shoot();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
    }

    bool CanShoot()
    {
        if (Time.time < lastShootTime + shootCooldown)
            return false;
        
        if (!infiniteAmmo && currentAmmo <= 0)
        {
            return false;
        }
        
        return true;
    }

    void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("⚠️ Projectile prefab not assigned to PlayerShooting!");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("❌ Main Camera not found!");
            return;
        }

        lastShootTime = Time.time;
        
        if (!infiniteAmmo)
        {
            currentAmmo--;
        }

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector2 direction = (mousePos - transform.position).normalized;

        Vector3 spawnPos = transform.position + (Vector3)direction * 0.5f;

        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Initialize(direction, "Player", shootDamage);
            projScript.speed = projectileSpeed;
        }
        else
        {
            Debug.LogWarning("⚠️ Projectile script not found on projectile prefab!");
            
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * projectileSpeed;
            }
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        Debug.Log($"🔫 Shot fired! Ammo: {currentAmmo}/{maxAmmo}");
    }

    void Reload()
    {
        if (currentAmmo < maxAmmo)
        {
            currentAmmo = maxAmmo;
            Debug.Log($"🔄 Reloaded! Ammo: {currentAmmo}/{maxAmmo}");
        }
        else
        {
            Debug.Log("Already full ammo!");
        }
    }

    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        Debug.Log($"➕ Added {amount} ammo. Current: {currentAmmo}/{maxAmmo}");
    }

    public bool HasAmmo()
    {
        return infiniteAmmo || currentAmmo > 0;
    }

    public float GetAmmoPercentage()
    {
        if (maxAmmo == 0) return 0f;
        return (float)currentAmmo / maxAmmo;
    }

    public void ShootTowards(Vector2 direction)
{
    if (projectilePrefab == null)
    {
        Debug.LogWarning("⚠️ Projectile prefab not assigned!");
        return;
    }
    
    if (!CanShoot())
    {
        return;
    }
    
    lastShootTime = Time.time;
    
    if (!infiniteAmmo)
    {
        currentAmmo--;
    }
    
    Vector3 spawnPos = transform.position + (Vector3)direction.normalized * 0.5f;
    
    GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
    
    Projectile projScript = projectile.GetComponent<Projectile>();
    if (projScript != null)
    {
        projScript.Initialize(direction, "Player", shootDamage);
        projScript.speed = projectileSpeed;
    }
    
    Debug.Log($"🔫 AI Shot! Ammo: {currentAmmo}/{maxAmmo}");
}
}
}