using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace Mehmet{
public class BSPDungeonGenerator : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap floorTilemap;
    public Tilemap wallsTilemap;
    public Tilemap objectsTilemap;
    public Tilemap decorTilemap;
    public Tilemap debugTilemap;

    [Header("Tile Assets")]
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase doorTile;
    public TileBase corridorTile;
    public TileBase spawnTile;
    public TileBase goalTile;

    [Header("WFC Integration")]
    public WFCGenerator wfcGenerator;
    
    [Header("Enemy Spawner")]
    public EnemySpawner enemySpawner;

    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomWidth = 6;
    public int minRoomHeight = 6;
    public int maxRoomWidth = 15;
    public int maxRoomHeight = 15;
    
    [Range(3, 8)]
    public int maxDepth = 5;

    private class BSPNode
    {
        public RectInt rect;
        public BSPNode leftChild;
        public BSPNode rightChild;
        public RectInt room;
        public bool hasRoom;

        public BSPNode(RectInt rect)
        {
            this.rect = rect;
            this.hasRoom = false;
        }

        public bool IsLeaf()
        {
            return leftChild == null && rightChild == null;
        }
    }

    private BSPNode rootNode;
    private List<RectInt> rooms = new List<RectInt>();
    private List<Vector2Int> corridors = new List<Vector2Int>();
    private List<Vector2Int> doorPositions = new List<Vector2Int>();
    
    public Vector2Int spawnPosition;
    public Vector2Int goalPosition;
    
    public List<RectInt> GetRooms() => rooms;
    public List<Vector2Int> GetCorridors() => corridors;
    public List<Vector2Int> GetDoorPositions() => doorPositions;

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        ClearTilemaps();
        rooms.Clear();
        corridors.Clear();
        doorPositions.Clear();

        rootNode = new BSPNode(new RectInt(0, 0, dungeonWidth, dungeonHeight));
        SplitNode(rootNode, 0);
        CreateRooms(rootNode);
        CreateCorridors(rootNode);
        PlaceDoors();
        
        SetSpawnAndGoal();
        
        DrawDungeon();

        if (wfcGenerator != null)
        {
            wfcGenerator.ApplyWFCToAllRooms(rooms, doorPositions, corridors);
        }
        else
        {
            Debug.LogWarning("WFC Generator not assigned!");
        }
        
        if (enemySpawner != null)
        {
            enemySpawner.SpawnAllEnemies();
        }
        else
        {
            Debug.LogWarning("Enemy Spawner not assigned!");
        }

        MovePlayerToSpawn();

        Debug.Log($"✅ Dungeon generated! {rooms.Count} rooms, {corridors.Count} corridor tiles, {doorPositions.Count} doors");
        Debug.Log($"🟢 Spawn: {spawnPosition}, 🎯 Goal: {goalPosition}");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerController playerController = playerObj.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.Initialize(this);
                Debug.Log("✅ Player initialized by BSP!");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Player not found! Create a Player GameObject with 'Player' tag.");
        }
    }

    void SplitNode(BSPNode node, int depth)
    {
        if (depth >= maxDepth)
            return;

        bool splitHorizontal = Random.value > 0.5f;

        if (node.rect.width > node.rect.height && node.rect.width / node.rect.height >= 1.25f)
            splitHorizontal = false;
        else if (node.rect.height > node.rect.width && node.rect.height / node.rect.width >= 1.25f)
            splitHorizontal = true;

        int max = (splitHorizontal ? node.rect.height : node.rect.width) - minRoomHeight;
        if (max <= minRoomHeight)
            return;

        int split = Random.Range(minRoomHeight, max);

        if (splitHorizontal)
        {
            node.leftChild = new BSPNode(new RectInt(node.rect.x, node.rect.y, node.rect.width, split));
            node.rightChild = new BSPNode(new RectInt(node.rect.x, node.rect.y + split, node.rect.width, node.rect.height - split));
        }
        else
        {
            node.leftChild = new BSPNode(new RectInt(node.rect.x, node.rect.y, split, node.rect.height));
            node.rightChild = new BSPNode(new RectInt(node.rect.x + split, node.rect.y, node.rect.width - split, node.rect.height));
        }

        SplitNode(node.leftChild, depth + 1);
        SplitNode(node.rightChild, depth + 1);
    }

    void CreateRooms(BSPNode node)
    {
        if (node == null) return;

        if (node.IsLeaf())
        {
            int roomWidth = Random.Range(minRoomWidth, Mathf.Min(maxRoomWidth, node.rect.width - 2));
            int roomHeight = Random.Range(minRoomHeight, Mathf.Min(maxRoomHeight, node.rect.height - 2));

            int roomX = node.rect.x + Random.Range(1, node.rect.width - roomWidth - 1);
            int roomY = node.rect.y + Random.Range(1, node.rect.height - roomHeight - 1);

            node.room = new RectInt(roomX, roomY, roomWidth, roomHeight);
            node.hasRoom = true;
            rooms.Add(node.room);
        }
        else
        {
            CreateRooms(node.leftChild);
            CreateRooms(node.rightChild);
        }
    }

    void CreateCorridors(BSPNode node)
    {
        if (node == null || node.IsLeaf()) return;

        CreateCorridors(node.leftChild);
        CreateCorridors(node.rightChild);

        RectInt leftRoom = GetRoom(node.leftChild);
        RectInt rightRoom = GetRoom(node.rightChild);

        if (leftRoom.width > 0 && rightRoom.width > 0)
        {
            ConnectRooms(leftRoom, rightRoom);
        }
    }

    RectInt GetRoom(BSPNode node)
    {
        if (node == null) return new RectInt(-1, -1, 0, 0);

        if (node.hasRoom)
            return node.room;

        RectInt leftRoom = GetRoom(node.leftChild);
        if (leftRoom.width > 0)
            return leftRoom;

        return GetRoom(node.rightChild);
    }

    void ConnectRooms(RectInt room1, RectInt room2)
    {
        Vector2Int room1Center = new Vector2Int(room1.x + room1.width / 2, room1.y + room1.height / 2);
        Vector2Int room2Center = new Vector2Int(room2.x + room2.width / 2, room2.y + room2.height / 2);

        if (Random.value > 0.5f)
        {
            CreateHorizontalCorridor(room1Center.x, room2Center.x, room1Center.y);
            CreateVerticalCorridor(room1Center.y, room2Center.y, room2Center.x);
        }
        else
        {
            CreateVerticalCorridor(room1Center.y, room2Center.y, room1Center.x);
            CreateHorizontalCorridor(room1Center.x, room2Center.x, room2Center.y);
        }
    }

    void CreateHorizontalCorridor(int x1, int x2, int y)
    {
        int start = Mathf.Min(x1, x2);
        int end = Mathf.Max(x1, x2);

        for (int x = start; x <= end; x++)
        {
            corridors.Add(new Vector2Int(x, y));
        }
    }

    void CreateVerticalCorridor(int y1, int y2, int x)
    {
        int start = Mathf.Min(y1, y2);
        int end = Mathf.Max(y1, y2);

        for (int y = start; y <= end; y++)
        {
            corridors.Add(new Vector2Int(x, y));
        }
    }

    void PlaceDoors()
    {
        doorPositions.Clear();

        foreach (RectInt room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                CheckDoorAt(new Vector2Int(x, room.y - 1));
                CheckDoorAt(new Vector2Int(x, room.y));
            }
            
            for (int x = room.x; x < room.x + room.width; x++)
            {
                CheckDoorAt(new Vector2Int(x, room.y + room.height));
                CheckDoorAt(new Vector2Int(x, room.y + room.height - 1));
            }
            
            for (int y = room.y; y < room.y + room.height; y++)
            {
                CheckDoorAt(new Vector2Int(room.x - 1, y));
                CheckDoorAt(new Vector2Int(room.x, y));
            }
            
            for (int y = room.y; y < room.y + room.height; y++)
            {
                CheckDoorAt(new Vector2Int(room.x + room.width, y));
                CheckDoorAt(new Vector2Int(room.x + room.width - 1, y));
            }
        }
        
        Debug.Log($"✅ Placed {doorPositions.Count} doors");
    }

    void CheckDoorAt(Vector2Int pos)
    {
        if (corridors.Contains(pos) && !doorPositions.Contains(pos))
        {
            doorPositions.Add(pos);
        }
    }

    void CheckAndPlaceDoor(int x, int y, int length, bool isVertical)
    {
        for (int i = 0; i < length; i++)
        {
            Vector2Int checkPos = isVertical ? new Vector2Int(x, y + i) : new Vector2Int(x + i, y);
            
            if (corridors.Contains(checkPos))
            {
                Vector2Int doorPos = isVertical ? new Vector2Int(x + (x < 50 ? 1 : -1), y + i) : new Vector2Int(x + i, y + (y < 50 ? 1 : -1));
                
                if (IsInsideRoom(doorPos) && !doorPositions.Contains(doorPos))
                {
                    doorPositions.Add(doorPos);
                }
            }
        }
    }

    bool IsInsideRoom(Vector2Int pos)
    {
        foreach (RectInt room in rooms)
        {
            if (room.Contains(pos))
                return true;
        }
        return false;
    }

    void SetSpawnAndGoal()
    {
        if (rooms.Count < 2)
        {
            Debug.LogError("❌ Not enough rooms for spawn and goal!");
            spawnPosition = new Vector2Int(5, 5);
            goalPosition = new Vector2Int(95, 95);
            return;
        }

        RectInt spawnRoom = rooms[0];
        spawnPosition = new Vector2Int(
            spawnRoom.x + spawnRoom.width / 2, 
            spawnRoom.y + spawnRoom.height / 2
        );

        RectInt goalRoom = rooms[rooms.Count - 1];
        goalPosition = new Vector2Int(
            goalRoom.x + goalRoom.width / 2, 
            goalRoom.y + goalRoom.height / 2
        );

        Debug.Log($"🟢 Spawn set to: {spawnPosition} (Room 0 center)");
        Debug.Log($"🎯 Goal set to: {goalPosition} (Room {rooms.Count - 1} center)");
    }

    void MovePlayerToSpawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            player.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0);
            Debug.Log($"✅ Player moved to spawn: {spawnPosition}");
            
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("✅ PlayerController found and will auto-initialize");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Player GameObject not found! Make sure it has 'Player' tag.");
        }
    }

    void DrawDungeon()
    {
        foreach (RectInt room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
            }
        }

        foreach (Vector2Int pos in corridors)
        {
            floorTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), corridorTile != null ? corridorTile : floorTile);
        }
        
        foreach (RectInt room in rooms)
        {
            DrawRoomWalls(room);
        }
        
        foreach (Vector2Int pos in corridors)
        {
            DrawCorridorWalls(pos);
        }

        foreach (Vector2Int doorPos in doorPositions)
        {
            Vector3Int tilePos = new Vector3Int(doorPos.x, doorPos.y, 0);
            
            wallsTilemap.SetTile(tilePos, null);
            
            floorTilemap.SetTile(tilePos, floorTile);
            
            objectsTilemap.SetTile(tilePos, doorTile);
        }

        if (spawnTile != null && debugTilemap != null)
        {
            debugTilemap.SetTile(new Vector3Int(spawnPosition.x, spawnPosition.y, 0), spawnTile);
        }
        
        if (goalTile != null && debugTilemap != null)
        {
            debugTilemap.SetTile(new Vector3Int(goalPosition.x, goalPosition.y, 0), goalTile);
        }
    }

    void DrawRoomWalls(RectInt room)
    {
        for (int x = room.x - 1; x <= room.x + room.width; x++)
        {
            SetWallIfEmpty(x, room.y - 1);
            SetWallIfEmpty(x, room.y + room.height);
        }

        for (int y = room.y - 1; y <= room.y + room.height; y++)
        {
            SetWallIfEmpty(room.x - 1, y);
            SetWallIfEmpty(room.x + room.width, y);
        }
    }

    void DrawCorridorWalls(Vector2Int pos)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                SetWallIfEmpty(pos.x + dx, pos.y + dy);
            }
        }
    }

    void SetWallIfEmpty(int x, int y)
    {
        Vector3Int tilePos = new Vector3Int(x, y, 0);
        
        bool hasFloor = floorTilemap.HasTile(tilePos);
        bool hasDoor = objectsTilemap.HasTile(tilePos);
        
        if (!hasFloor && !hasDoor)
        {
            wallsTilemap.SetTile(tilePos, wallTile);
        }
    }
    
    public void ClearDungeon()
    {
        if (floorTilemap != null)
            floorTilemap.ClearAllTiles();
        
        if (wallsTilemap != null)
            wallsTilemap.ClearAllTiles();
        
        if (objectsTilemap != null)
            objectsTilemap.ClearAllTiles();
        
        rooms?.Clear();
        corridors?.Clear();
        
        Debug.Log("🧹 Dungeon cleared");
    }
    
    void ClearTilemaps()
    {
        if (floorTilemap) floorTilemap.ClearAllTiles();
        if (wallsTilemap) wallsTilemap.ClearAllTiles();
        if (objectsTilemap) objectsTilemap.ClearAllTiles();
        if (decorTilemap) decorTilemap.ClearAllTiles();
        if (debugTilemap) debugTilemap.ClearAllTiles();
    }
    
    [ContextMenu("Generate New Dungeon")]
    public void GenerateNewDungeon()
    {
        GenerateDungeon();
    }
}
}