using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

namespace Mehmet{
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject stationaryEnemyPrefab;
    public GameObject patrolEnemyPrefab;
    public GameObject chaserEnemyPrefab;

    [Header("Spawn Settings")]
    public int stationaryCount = 3;
    public int patrolCount = 4;
    public int chaserCount = 3;
    public float minDistanceFromSpawn = 15f;
    public float minDistanceFromGoal = 10f;
    public float minEnemyDistance = 5f; 

    [Header("Distribution Settings")]
    [Tooltip("Her odaya maksimum kaç düşman yerleşebilir")]
    public int maxEnemiesPerRoom = 2;
    
    [Tooltip("Farklı odalara dağıtmayı zorla")]
    public bool forceDifferentRooms = true;

    [Header("References")]
    public BSPDungeonGenerator dungeonGenerator;

    private List<Vector2Int> spawnedPositions = new List<Vector2Int>();
    private Dictionary<RectInt, int> roomEnemyCount = new Dictionary<RectInt, int>();
    private List<RectInt> availableRooms = new List<RectInt>();

    void Start()
    {
        if (dungeonGenerator == null)
            dungeonGenerator = GetComponent<BSPDungeonGenerator>();
        
        Invoke("SpawnAllEnemies", 0.5f);
    }

    public void SpawnAllEnemies()
    {
        if (dungeonGenerator == null)
        {
            Debug.LogError("Dungeon Generator not found!");
            return;
        }

        ClearAllEnemies();
        spawnedPositions.Clear();
        roomEnemyCount.Clear();
        
        PrepareRooms();

        SpawnEnemiesDistributed(stationaryEnemyPrefab, stationaryCount, "Stationary");
        SpawnEnemiesDistributed(patrolEnemyPrefab, patrolCount, "Patrol");
        SpawnEnemiesDistributed(chaserEnemyPrefab, chaserCount, "Chaser");

        Debug.Log($"✅ Spawned {spawnedPositions.Count} enemies across {roomEnemyCount.Count} rooms");
        
        foreach (var pair in roomEnemyCount)
        {
            Debug.Log($"Room at ({pair.Key.x},{pair.Key.y}) has {pair.Value} enemies");
        }
    }

    void PrepareRooms()
    {
        List<RectInt> rooms = dungeonGenerator.GetRooms();
        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogError("No rooms found!");
            return;
        }

        availableRooms = new List<RectInt>(rooms);
        
        foreach (RectInt room in rooms)
        {
            roomEnemyCount[room] = 0;
        }

        Vector2 spawnPos = dungeonGenerator.spawnPosition;
        Vector2 goalPos = dungeonGenerator.goalPosition;
        
        availableRooms.RemoveAll(room => 
            room.Contains(new Vector2Int((int)spawnPos.x, (int)spawnPos.y)) ||
            room.Contains(new Vector2Int((int)goalPos.x, (int)goalPos.y))
        );

        Debug.Log($"📍 Available rooms for enemies: {availableRooms.Count}/{rooms.Count}");
    }

    void SpawnEnemiesDistributed(GameObject prefab, int count, string typeName)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{typeName} prefab not assigned!");
            return;
        }

        if (availableRooms.Count == 0)
        {
            Debug.LogError("No available rooms for spawning!");
            return;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 50;

        List<RectInt> shuffledRooms = new List<RectInt>(availableRooms);
        ShuffleList(shuffledRooms);

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            RectInt selectedRoom = GetLeastPopulatedRoom(shuffledRooms);
            
            if (forceDifferentRooms && roomEnemyCount[selectedRoom] >= maxEnemiesPerRoom)
            {
                shuffledRooms.Remove(selectedRoom);
                if (shuffledRooms.Count == 0)
                {
                    shuffledRooms = new List<RectInt>(availableRooms);
                    ShuffleList(shuffledRooms);
                }
                continue;
            }

            Vector2Int spawnPos = GetRandomPositionInRoom(selectedRoom);

            if (IsValidSpawnPosition(spawnPos) && IsOnFloor(spawnPos))
            {
                Vector3 worldPos = new Vector3(spawnPos.x, spawnPos.y, 0);
                GameObject enemy = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                enemy.name = $"{typeName}_{spawned + 1}";
                
                EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
                if (enemyBase != null)
                {
                    if (typeName == "Stationary")
                        enemyBase.health = Random.Range(30f, 70f);
                    else if (typeName == "Patrol")
                        enemyBase.health = Random.Range(40f, 80f);
                    else if (typeName == "Chaser")
                        enemyBase.health = Random.Range(60f, 100f);
                    
                    enemyBase.Initialize();
                }
                
                spawnedPositions.Add(spawnPos);
                roomEnemyCount[selectedRoom]++;
                spawned++;
                
                Debug.Log($"🎯 Spawned {typeName} in room at ({selectedRoom.x},{selectedRoom.y})");
            }
        }

        if (spawned < count)
        {
            Debug.LogWarning($"⚠️ Only spawned {spawned}/{count} {typeName} enemies after {attempts} attempts");
        }
        else
        {
            Debug.Log($"✅ Spawned {spawned}/{count} {typeName} enemies");
        }
    }

    RectInt GetLeastPopulatedRoom(List<RectInt> rooms)
    {
        if (rooms.Count == 0)
            return availableRooms[Random.Range(0, availableRooms.Count)];

        RectInt leastPopulated = rooms[0];
        int minCount = roomEnemyCount[leastPopulated];

        foreach (RectInt room in rooms)
        {
            if (roomEnemyCount[room] < minCount)
            {
                minCount = roomEnemyCount[room];
                leastPopulated = room;
            }
        }

        return leastPopulated;
    }

    Vector2Int GetRandomPositionInRoom(RectInt room)
    {
        int margin = Random.Range(2, 4);
        int x = Random.Range(room.x + margin, room.x + room.width - margin);
        int y = Random.Range(room.y + margin, room.y + room.height - margin);
        
        x = Mathf.Clamp(x, room.x + 1, room.x + room.width - 1);
        y = Mathf.Clamp(y, room.y + 1, room.y + room.height - 1);
        
        return new Vector2Int(x, y);
    }

    bool IsValidSpawnPosition(Vector2Int position)
    {
        Vector2 spawnPos = dungeonGenerator.spawnPosition;
        if (Vector2.Distance(position, spawnPos) < minDistanceFromSpawn)
            return false;

        Vector2 goalPos = dungeonGenerator.goalPosition;
        if (Vector2.Distance(position, goalPos) < minDistanceFromGoal)
            return false;

        foreach (Vector2Int existingPos in spawnedPositions)
        {
            if (Vector2.Distance(position, existingPos) < minEnemyDistance)
                return false;
        }

        return true;
    }

    bool IsOnFloor(Vector2Int position)
    {
        Vector3Int tilePos = new Vector3Int(position.x, position.y, 0);
        
        if (dungeonGenerator.floorTilemap != null)
        {
            bool hasFloor = dungeonGenerator.floorTilemap.HasTile(tilePos);
            bool hasWall = false;
            
            if (dungeonGenerator.wallsTilemap != null)
                hasWall = dungeonGenerator.wallsTilemap.HasTile(tilePos);
            
            return hasFloor && !hasWall;
        }
        
        return false;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void ClearAllEnemies()
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase enemy in enemies)
        {
            if (Application.isPlaying)
                Destroy(enemy.gameObject);
            else
                DestroyImmediate(enemy.gameObject);
        }
        
        spawnedPositions.Clear();
        roomEnemyCount.Clear();
    }

    [ContextMenu("Spawn Enemies")]
    public void SpawnEnemiesManual()
    {
        SpawnAllEnemies();
    }

    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || spawnedPositions.Count == 0) return;

        Gizmos.color = Color.red;
        foreach (Vector2Int pos in spawnedPositions)
        {
            Gizmos.DrawWireSphere(new Vector3(pos.x, pos.y, 0), 0.5f);
        }
    }
}
}