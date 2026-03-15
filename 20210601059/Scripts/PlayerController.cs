using UnityEngine;
using System.Collections.Generic;

namespace Mehmet{
public class PlayerController : MonoBehaviour
{
    [Header("Player Properties")]
    public float moveSpeed = 3f;
    public float health = 100f;
    public float maxHealth = 100f;
    
    [Header("Pathfinding")]
    public AStarPathfinding pathfinding;
    public bool autoMove = true;
    public float pathUpdateInterval = 0.5f;
    
    [Header("Combat")]
    public float attackDamage = 20f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public GameObject projectilePrefab;
    public float shootCooldown = 0.8f;
    public float shootRange = 8f;
    
    [Header("Decision Making")]
    [Tooltip("Evading cooldown - kaçtıktan sonra bu süre düşmanı ignore et")]
    public float evadingCooldown = 5f;
    
    [Tooltip("Strong enemy HP threshold")]
    public float strongEnemyThreshold = 70f;
    
    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Color playerColor = Color.green;
    
    private List<Vector2Int> currentPath;
    private int currentPathIndex;
    private Vector2Int goalPosition;
    private float lastPathUpdateTime;
    private float lastAttackTime;
    private BSPDungeonGenerator dungeonGenerator;
    private float evadingStartTime;
    private float lastSuccessfulEvadeTime;
    private Dictionary<EnemyBase, float> ignoredEnemies = new Dictionary<EnemyBase, float>();
    
    public bool isAlive = true;
    
    public System.Action OnGoalReached;
    public System.Action OnPlayerDeath;
    
    private enum AIState
    {
        Moving,
        Evading,
        Attacking,
        ReachedGoal
    }
    
    private AIState currentState = AIState.Moving;
    
    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (spriteRenderer != null)
            spriteRenderer.color = playerColor;
        
        gameObject.tag = "Player";
        Debug.Log("⏳ Player waiting for dungeon initialization...");
    }
    
    public void Initialize(BSPDungeonGenerator generator)
    {
        dungeonGenerator = generator;
        
        if (dungeonGenerator != null)
        {
            goalPosition = dungeonGenerator.goalPosition;
            Vector2Int spawnPos = dungeonGenerator.spawnPosition;
            transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);
            
            Debug.Log($"✅ Player initialized! Spawn: {spawnPos}, Goal: {goalPosition}");
        }
        
        if (pathfinding == null)
            pathfinding = gameObject.AddComponent<AStarPathfinding>();
        
        pathfinding.Initialize(dungeonGenerator);
        
        currentState = AIState.Moving;
        isAlive = true;
        health = maxHealth;
        ignoredEnemies.Clear();
        evadingStartTime = 0;
        lastSuccessfulEvadeTime = 0;
        
        if (autoMove)
        {
            RecalculatePath();
            Debug.Log($"✅ Initial path: {(currentPath != null ? currentPath.Count : 0)} nodes");
        }
    }
    
    void Update()
    {
        if (!isAlive || dungeonGenerator == null) return;
        
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🤖 STATE: {currentState}, Path: {(currentPath != null ? currentPath.Count : 0)}, Index: {currentPathIndex}, Evaded: {ignoredEnemies.Count}");
        }
        
        CleanupIgnoredEnemies();
        
        switch (currentState)
        {
            case AIState.Moving:
                UpdateMoving();
                break;
            case AIState.Evading:
                UpdateEvading();
                break;
            case AIState.Attacking:
                UpdateAttacking();
                break;
            case AIState.ReachedGoal:
                break;
        }
        
        if (currentState != AIState.ReachedGoal)
        {
            CheckForEnemies();
        }
    }
    
    void CleanupIgnoredEnemies()
    {
        List<EnemyBase> toRemove = new List<EnemyBase>();
        
        foreach (var kvp in ignoredEnemies)
        {
            if (Time.time >= kvp.Value || kvp.Key == null || !kvp.Key.isAlive)
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var enemy in toRemove)
        {
            ignoredEnemies.Remove(enemy);
            if (enemy != null)
                Debug.Log($"✅ Stopped ignoring {enemy.enemyType}");
        }
    }
    
    void UpdateMoving()
    {
        if (!autoMove) return;
        
        if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
        {
            RecalculatePath();
            lastPathUpdateTime = Time.time;
        }
        
        if (currentPath != null && currentPath.Count > 0)
        {
            FollowPath();
        }
    }
    
    void UpdateEvading()
    {
        EnemyBase nearestEnemy = FindNearestThreat();
        
        if (nearestEnemy == null)
        {
            Debug.Log("✅ No threat, returning to Moving");
            currentState = AIState.Moving;
            evadingStartTime = 0;
            RecalculatePath();
            return;
        }
        
        float distance = Vector3.Distance(transform.position, nearestEnemy.transform.position);
        
        if (evadingStartTime == 0)
        {
            evadingStartTime = Time.time;
            Debug.Log($"⏱️ Started evading from {nearestEnemy.enemyType}");
        }
        
        float evadeTime = Time.time - evadingStartTime;
        
        if (evadeTime > 3f)
        {
            Debug.LogWarning($"⏰ Evading timeout! Ignoring {nearestEnemy.enemyType} for {evadingCooldown}s");
            
            ignoredEnemies[nearestEnemy] = Time.time + evadingCooldown;
            
            currentState = AIState.Moving;
            evadingStartTime = 0;
            lastSuccessfulEvadeTime = Time.time;
            RecalculatePath();
            return;
        }
        
        if (distance > 6f)
        {
            Debug.Log($"✅ Successfully evaded! Distance: {distance:F1}, ignoring for {evadingCooldown}s");
            
            ignoredEnemies[nearestEnemy] = Time.time + evadingCooldown;
            
            currentState = AIState.Moving;
            evadingStartTime = 0;
            lastSuccessfulEvadeTime = Time.time;
            RecalculatePath();
            return;
        }
        
        Vector3 escapeDirection = (transform.position - nearestEnemy.transform.position).normalized;
        Vector3 targetPos = transform.position + escapeDirection * moveSpeed * 2.5f * Time.deltaTime;
        
        if (IsPositionSafe(targetPos))
        {
            transform.position = targetPos;
        }
        else
        {
            Vector3 alt1 = Quaternion.Euler(0, 0, 45) * escapeDirection;
            Vector3 altTarget1 = transform.position + alt1 * moveSpeed * 2f * Time.deltaTime;
            
            if (IsPositionSafe(altTarget1))
                transform.position = altTarget1;
            else
            {
                Vector3 alt2 = Quaternion.Euler(0, 0, -45) * escapeDirection;
                Vector3 altTarget2 = transform.position + alt2 * moveSpeed * 2f * Time.deltaTime;
                
                if (IsPositionSafe(altTarget2))
                    transform.position = altTarget2;
            }
        }
    }
    
    void UpdateAttacking()
    {
        EnemyBase target = FindNearestWeakEnemy();
        
        if (target == null || !target.isAlive)
        {
            Debug.Log("✅ Enemy eliminated, returning to Moving");
            currentState = AIState.Moving;
            RecalculatePath();
            return;
        }
        
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        if (distance > 10f)
        {
            Debug.Log($"Enemy escaped ({distance:F1} > 10), returning to goal");
            currentState = AIState.Moving;
            RecalculatePath();
            return;
        }
        
        if (target.health > strongEnemyThreshold)
        {
            Debug.Log($"Enemy too strong now ({target.health} HP), evading");
            currentState = AIState.Evading;
            return;
        }
        
        if (distance >= 2f && distance <= shootRange)
        {
            if (Time.time >= lastAttackTime + shootCooldown)
            {
                Vector3 direction = (target.transform.position - transform.position).normalized;
                ShootProjectile(direction, target);
                lastAttackTime = Time.time;
            }
        }
        else if (distance < 2f)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                MeleeAttack(target);
                lastAttackTime = Time.time;
            }
        }
        else
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
    
    void CheckForEnemies()
    {
        EnemyBase weakEnemy = FindNearestWeakEnemy();
        
        if (weakEnemy != null)
        {
            float distance = Vector3.Distance(transform.position, weakEnemy.transform.position);
            
            if (distance < 8f)
            {
                if (currentState != AIState.Attacking)
                {
                    Debug.Log($"⚔️ Weak enemy spotted! ({weakEnemy.health} HP at {distance:F1}), attacking!");
                    currentState = AIState.Attacking;
                }
                return;
            }
        }
        
        EnemyBase threat = FindNearestThreat();
        
        if (threat != null)
        {
            float distance = Vector3.Distance(transform.position, threat.transform.position);
            
            if (distance > 12f)
            {
                if (currentState == AIState.Evading)
                {
                    Debug.Log($"Threat far away ({distance:F1} > 12), returning to Moving");
                    currentState = AIState.Moving;
                    RecalculatePath();
                }
            }
            else if (distance < 12f)
            {
                if (Time.time - lastSuccessfulEvadeTime < 2f)
                {
                    
                }
                else if (currentState == AIState.Moving)
                {
                    Debug.Log($"⚠️ Strong enemy approaching! ({threat.health} HP at {distance:F1}), evading!");
                    currentState = AIState.Evading;
                }
            }
        }
        else
        {
            if (currentState == AIState.Evading)
            {
                Debug.Log("No threats detected, returning to Moving");
                currentState = AIState.Moving;
                RecalculatePath();
            }
        }
    }
    
    EnemyBase FindNearestWeakEnemy()
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        EnemyBase nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (EnemyBase enemy in enemies)
        {
            if (!enemy.isAlive) continue;
            if (ignoredEnemies.ContainsKey(enemy)) continue;
            
            if (enemy.health < 60f)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
        }
        
        return nearest;
    }
    
    EnemyBase FindNearestThreat()
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        EnemyBase nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (EnemyBase enemy in enemies)
        {
            if (!enemy.isAlive) continue;
            if (ignoredEnemies.ContainsKey(enemy)) continue;
            
            if (enemy.health >= strongEnemyThreshold)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
        }
        
        return nearest;
    }
    
    void ShootProjectile(Vector3 direction, EnemyBase target)
{
    PlayerShooting shooting = GetComponent<PlayerShooting>();
    
    if (shooting != null && shooting.HasAmmo())
    {
        Vector3 targetWorldPos = target.transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(targetWorldPos);
        
        shooting.ShootTowards(direction);
        
        lastAttackTime = Time.time;
        
        Debug.Log($"🔫 Shot at {target.enemyType}!");
        
        if (spriteRenderer != null)
            StartCoroutine(FlashWhite());
    }
    else
    {
        Debug.LogWarning("⚠️ Out of ammo! Switching to melee.");
        MeleeAttack(target);
    }
}
    
    void MeleeAttack(EnemyBase enemy)
    {
        if (enemy == null) return;
        
        enemy.TakeDamage(attackDamage);
        Debug.Log($"⚔️ Melee attack on {enemy.enemyType} for {attackDamage} damage!");
        
        if (spriteRenderer != null)
            StartCoroutine(FlashWhite());
    }
    
    void RecalculatePath()
    {
        if (pathfinding == null || dungeonGenerator == null) return;
        
        Vector2Int currentPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        
        if (!IsPositionWalkable(currentPos))
        {
            Vector2Int nearestWalkable = FindNearestWalkable(currentPos, 5);
            if (nearestWalkable != currentPos)
            {
                currentPos = nearestWalkable;
                transform.position = new Vector3(currentPos.x, currentPos.y, transform.position.z);
            }
        }
        
        currentPath = pathfinding.FindPath(currentPos, goalPosition);
        currentPathIndex = 0;
        
        if (currentPath != null && currentPath.Count > 0)
            Debug.Log($"✅ Path: {currentPath.Count} nodes");
    }
    
    void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0) return;
        
        if (currentPathIndex >= currentPath.Count)
        {
            currentState = AIState.ReachedGoal;
            Debug.Log("🎉 GOAL REACHED! Agent successfully navigated to the goal!");
            
            OnGoalReached?.Invoke();
            
            return;
        }
        
        Vector2Int targetTile = currentPath[currentPathIndex];
        Vector3 targetPos = new Vector3(targetTile.x, targetTile.y, 0);
        
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );
        
        if (Vector3.Distance(transform.position, targetPos) < 0.3f)
        {
            currentPathIndex++;
            
            if (currentPathIndex % 10 == 0)
                Debug.Log($"🚶 Progress: {currentPathIndex}/{currentPath.Count}");
        }
    }
    
    Vector2Int FindNearestWalkable(Vector2Int start, int searchRadius)
    {
        for (int radius = 1; radius <= searchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int testPos = new Vector2Int(start.x + x, start.y + y);
                    if (IsPositionWalkable(testPos))
                        return testPos;
                }
            }
        }
        return start;
    }
    
    bool IsPositionWalkable(Vector2Int pos)
    {
        if (dungeonGenerator == null) return false;
        
        Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
        bool hasFloor = dungeonGenerator.floorTilemap.HasTile(tilePos);
        bool hasWall = dungeonGenerator.wallsTilemap.HasTile(tilePos);
        
        return hasFloor && !hasWall;
    }
    
    bool IsPositionSafe(Vector3 worldPos)
    {
        Vector2Int tilePos = new Vector2Int(
            Mathf.RoundToInt(worldPos.x),
            Mathf.RoundToInt(worldPos.y)
        );
        return IsPositionWalkable(tilePos);
    }
    
    public void TakeDamage(float damage)
    {
        if (!isAlive) return;
        
        health -= damage;
        Debug.Log($"💥 Player took {damage} damage! Health: {health}/{maxHealth}");
        
        if (spriteRenderer != null)
            StartCoroutine(FlashRed());
        
        if (health <= 0)
            Die();
    }
    
    void Die()
    {
        isAlive = false;
        Debug.Log("💀 Player died!");
        
        if (spriteRenderer != null)
            spriteRenderer.color = Color.gray;
        
        currentPath = null;
        
        // UI'yı bilgilendir
        OnPlayerDeath?.Invoke();
        
        this.enabled = false;
    }
    
    System.Collections.IEnumerator FlashWhite()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }
    
    System.Collections.IEnumerator FlashRed()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = original;
    }
    
    public bool HasReachedGoal()
    {
        return currentState == AIState.ReachedGoal;
    }
    
    public float GetDistanceToGoal()
    {
        if (dungeonGenerator == null) return float.MaxValue;
        
        Vector2 goalPos = new Vector2(goalPosition.x, goalPosition.y);
        return Vector2.Distance(transform.position, goalPos);
    }
    
    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Vector3 from = new Vector3(currentPath[i].x, currentPath[i].y, 0);
                Vector3 to = new Vector3(currentPath[i + 1].x, currentPath[i + 1].y, 0);
                Gizmos.DrawLine(from, to);
            }
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        foreach (var kvp in ignoredEnemies)
        {
            if (kvp.Key != null)
            {
                Gizmos.DrawLine(transform.position, kvp.Key.transform.position);
            }
        }
    }
}
}