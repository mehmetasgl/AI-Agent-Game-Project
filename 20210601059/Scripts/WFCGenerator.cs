using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace Mehmet{
public class WFCGenerator : MonoBehaviour
{
    [System.Serializable]
    public class DungeonTemplate
    {
        public string templateName;
        
        [Header("Base Tiles")]
        public TileBase floorTile;
        public TileBase wallTile;
        
        [Header("Decoration Tiles")]
        public TileBase pillarTile;
        public TileBase decorationWallTile;
        public TileBase rubbleTile;
        public TileBase obstacleTile;
        public TileBase trapTile;
        
        [Header("Template Settings")]
        [Range(0f, 1f)]
        public float pillarDensity = 0.15f;
        
        [Range(0f, 1f)]
        public float rubbleDensity = 0.2f;
        
        [Range(0f, 1f)]
        public float obstacleDensity = 0.1f;
        
        [Range(0f, 1f)]
        public float trapDensity = 0.05f;
        
        public bool usePillarsInCorners = true;
        public bool useWallDecorations = true;
    }

    [Header("Templates")]
    [Tooltip("Medieval Dungeon Template")]
    public DungeonTemplate medievalTemplate;
    
    [Tooltip("Dangerous Cave Template")]
    public DungeonTemplate caveTemplate;

    [Header("Tilemap References")]
    public Tilemap floorTilemap;
    public Tilemap wallsTilemap;
    public Tilemap decorTilemap;
    public Tilemap objectsTilemap;

    [Header("WFC Settings")]
    [Tooltip("Minimum distance from doors")]
    [Range(1, 5)]
    public int doorClearance = 2;
    
    [Tooltip("Minimum path width to maintain")]
    [Range(1, 3)]
    public int minPathWidth = 2;

    private System.Random random;
    private HashSet<Vector2Int> protectedTiles = new HashSet<Vector2Int>();
    private List<DungeonTemplate> availableTemplates = new List<DungeonTemplate>();

    void Start()
    {
        InitializeTemplates();
    }

    void InitializeTemplates()
    {
        availableTemplates.Clear();
        
        if (medievalTemplate != null && medievalTemplate.floorTile != null)
            availableTemplates.Add(medievalTemplate);
        
        if (caveTemplate != null && caveTemplate.floorTile != null)
            availableTemplates.Add(caveTemplate);
        
        if (availableTemplates.Count == 0)
        {
            Debug.LogWarning("No valid templates configured!");
        }
    }

    public void ApplyWFCToAllRooms(List<RectInt> rooms, List<Vector2Int> doorPositions, List<Vector2Int> corridors)
    {
        InitializeTemplates();
        
        if (availableTemplates.Count == 0)
        {
            Debug.LogError("No templates available!");
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            DungeonTemplate template = availableTemplates[i % availableTemplates.Count];
            ApplyWFCToRoom(rooms[i], doorPositions, corridors, template);
        }

        Debug.Log($"WFC applied to {rooms.Count} rooms using {availableTemplates.Count} templates");
    }

    public void ApplyWFCToRoom(RectInt room, List<Vector2Int> doorPositions, List<Vector2Int> corridors, DungeonTemplate template)
    {
        random = new System.Random(room.x * 10000 + room.y);
        
        SetupProtectedTiles(room, doorPositions, corridors);
        
        ApplyFloorVariation(room, template);
        
        if (template.usePillarsInCorners)
            PlacePillars(room, template);
        
        if (template.useWallDecorations)
            PlaceWallDecorations(room, template);
        
        PlaceRubble(room, template);
        PlaceObstacles(room, template);
        PlaceTraps(room, template);
    }

    void SetupProtectedTiles(RectInt room, List<Vector2Int> doorPositions, List<Vector2Int> corridors)
    {
        protectedTiles.Clear();

        foreach (Vector2Int door in doorPositions)
        {
            if (room.Contains(door))
            {
                AddProtectedArea(door, doorClearance);
            }
        }

        foreach (Vector2Int corridor in corridors)
        {
            if (room.Contains(corridor))
            {
                AddProtectedArea(corridor, minPathWidth);
            }
        }
    }

    void AddProtectedArea(Vector2Int center, int radius)
    {
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                protectedTiles.Add(new Vector2Int(x, y));
            }
        }
    }

    void ApplyFloorVariation(RectInt room, DungeonTemplate template)
    {
        for (int x = room.x; x < room.x + room.width; x++)
        {
            for (int y = room.y; y < room.y + room.height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                
                if (floorTilemap.HasTile(pos))
                {
                    floorTilemap.SetTile(pos, template.floorTile);
                }
            }
        }
    }

    void PlacePillars(RectInt room, DungeonTemplate template)
    {
        if (template.pillarTile == null) return;

        List<Vector2Int> cornerPositions = new List<Vector2Int>
        {
            new Vector2Int(room.x + 1, room.y + 1),
            new Vector2Int(room.x + room.width - 2, room.y + 1),
            new Vector2Int(room.x + 1, room.y + room.height - 2),
            new Vector2Int(room.x + room.width - 2, room.y + room.height - 2)
        };

        foreach (Vector2Int pos in cornerPositions)
        {
            if (!protectedTiles.Contains(pos) && random.NextDouble() < template.pillarDensity)
            {
                decorTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), template.pillarTile);
            }
        }

        int innerPillars = Mathf.FloorToInt((room.width * room.height) * template.pillarDensity * 0.3f);
        
        for (int i = 0; i < innerPillars; i++)
        {
            Vector2Int pos = GetRandomRoomPosition(room);
            
            if (!protectedTiles.Contains(pos) && !IsNearEdge(pos, room, 1))
            {
                decorTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), template.pillarTile);
            }
        }
    }

    void PlaceWallDecorations(RectInt room, DungeonTemplate template)
    {
        if (template.decorationWallTile == null) return;

        for (int x = room.x + 1; x < room.x + room.width - 1; x++)
        {
            TryPlaceWallDecor(new Vector2Int(x, room.y), template);
            TryPlaceWallDecor(new Vector2Int(x, room.y + room.height - 1), template);
        }

        for (int y = room.y + 1; y < room.y + room.height - 1; y++)
        {
            TryPlaceWallDecor(new Vector2Int(room.x, y), template);
            TryPlaceWallDecor(new Vector2Int(room.x + room.width - 1, y), template);
        }
    }

    void TryPlaceWallDecor(Vector2Int pos, DungeonTemplate template)
    {
        if (protectedTiles.Contains(pos) || random.NextDouble() > 0.3f)
            return;

        Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
        
        if (floorTilemap.HasTile(tilePos))
        {
            decorTilemap.SetTile(tilePos, template.decorationWallTile);
        }
    }

    void PlaceRubble(RectInt room, DungeonTemplate template)
    {
        if (template.rubbleTile == null) return;

        int count = Mathf.FloorToInt((room.width * room.height) * template.rubbleDensity);
        
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = GetRandomRoomPosition(room);
            
            if (!protectedTiles.Contains(pos))
            {
                decorTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), template.rubbleTile);
            }
        }
    }

    void PlaceObstacles(RectInt room, DungeonTemplate template)
    {
        if (template.obstacleTile == null) return;

        int count = Mathf.FloorToInt((room.width * room.height) * template.obstacleDensity);
        int attempts = 0;
        int placed = 0;

        while (placed < count && attempts < count * 3)
        {
            attempts++;
            Vector2Int pos = GetRandomRoomPosition(room);
            
            if (!protectedTiles.Contains(pos) && !IsNearDoor(pos, doorClearance + 1))
            {
                objectsTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), template.obstacleTile);
                placed++;
            }
        }
    }

    void PlaceTraps(RectInt room, DungeonTemplate template)
    {
        if (template.trapTile == null) return;

        int count = Mathf.FloorToInt((room.width * room.height) * template.trapDensity);
        
        Vector2Int center = new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
        
        for (int i = 0; i < count; i++)
        {
            Vector2Int offset = new Vector2Int(
                random.Next(-room.width / 4, room.width / 4),
                random.Next(-room.height / 4, room.height / 4)
            );
            
            Vector2Int pos = center + offset;
            
            if (room.Contains(pos) && !protectedTiles.Contains(pos))
            {
                objectsTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), template.trapTile);
            }
        }
    }

    Vector2Int GetRandomRoomPosition(RectInt room)
    {
        return new Vector2Int(
            random.Next(room.x + 1, room.x + room.width - 1),
            random.Next(room.y + 1, room.y + room.height - 1)
        );
    }

    bool IsNearEdge(Vector2Int pos, RectInt room, int distance)
    {
        return pos.x <= room.x + distance || 
               pos.x >= room.x + room.width - distance - 1 ||
               pos.y <= room.y + distance || 
               pos.y >= room.y + room.height - distance - 1;
    }

    bool IsNearDoor(Vector2Int pos, int distance)
    {
        foreach (Vector2Int protectedPos in protectedTiles)
        {
            if (Vector2Int.Distance(pos, protectedPos) < distance)
                return true;
        }
        return false;
    }
}
}