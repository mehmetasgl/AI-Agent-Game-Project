using UnityEngine;

namespace Mehmet{
public class PatrolEnemy : EnemyBase
{
    [Header("Patrol Settings")]
    [Tooltip("Patrol range from spawn point")]
    public float patrolRange = 5f;
    
    [Tooltip("Wait time at patrol points")]
    public float waitTime = 2f;
    
    [Tooltip("Patrol pattern: Horizontal or Vertical")]
    public bool isHorizontalPatrol = true;
    
    private Vector3 patrolPointA;
    private Vector3 patrolPointB;
    private Vector3 currentTarget;
    private float waitTimer;
    private bool isWaiting;

    protected override void Start()
    {
        enemyType = EnemyType.Patrol;
        enemyColor = new Color(0.9f, 0.6f, 0.2f);
        moveSpeed = 1.5f;
        detectionRange = 4f;
        
        base.Start();
        
        SetupPatrolPoints();
        currentTarget = patrolPointB;
    }

    void SetupPatrolPoints()
    {
        if (isHorizontalPatrol)
        {
            patrolPointA = spawnPosition + Vector3.left * patrolRange;
            patrolPointB = spawnPosition + Vector3.right * patrolRange;
        }
        else
        {
            patrolPointA = spawnPosition + Vector3.down * patrolRange;
            patrolPointB = spawnPosition + Vector3.up * patrolRange;
        }
        
        patrolPointA = GetValidPosition(patrolPointA);
        patrolPointB = GetValidPosition(patrolPointB);
    }

    Vector3 GetValidPosition(Vector3 position)
    {
        Vector3Int tilePos = new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), 0);
        
        GameObject gridObj = GameObject.Find("Grid");
        if (gridObj != null)
        {
            BSPDungeonGenerator bsp = gridObj.GetComponent<BSPDungeonGenerator>();
            if (bsp != null)
            {
                foreach (RectInt room in bsp.GetRooms())
                {
                    if (room.Contains(new Vector2Int(tilePos.x, tilePos.y)))
                    {
                        return position;
                    }
                }
            }
        }
        
        return spawnPosition;
    }

    protected override void Update()
    {
        base.Update();
        
        if (!isAlive) return;
        
        if (IsPlayerInRange())
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                currentTarget = (currentTarget == patrolPointA) ? patrolPointB : patrolPointA;
            }
            return;
        }
        
        transform.position = Vector3.MoveTowards(
            transform.position, 
            currentTarget, 
            moveSpeed * Time.deltaTime
        );
        
        if (Vector3.Distance(transform.position, currentTarget) < 0.1f)
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;
        
        float distanceToSpawn = Vector3.Distance(transform.position, spawnPosition);
        
        if (distanceToSpawn > patrolRange * 2f)
        {
            return;
        }
        
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * moveSpeed * 1.5f * Time.deltaTime;
        
        if (Vector3.Distance(transform.position, player.position) < 1f)
        {
            PerformAttack();
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Gizmos.color = Color.cyan;
        Vector3 spawnPos = Application.isPlaying ? spawnPosition : transform.position;
        
        if (isHorizontalPatrol)
        {
            Gizmos.DrawLine(spawnPos + Vector3.left * patrolRange, spawnPos + Vector3.right * patrolRange);
        }
        else
        {
            Gizmos.DrawLine(spawnPos + Vector3.down * patrolRange, spawnPos + Vector3.up * patrolRange);
        }
    }
}
}