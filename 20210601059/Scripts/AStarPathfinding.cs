using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

namespace Mehmet{
public class AStarPathfinding : MonoBehaviour
{
    private BSPDungeonGenerator dungeonGenerator;
    private Tilemap floorTilemap;
    private Tilemap wallsTilemap;
    
    private class Node
    {
        public Vector2Int position;
        public Node parent;
        public float gCost; 
        public float hCost; 
        public float fCost => gCost + hCost;
        
        public Node(Vector2Int pos)
        {
            position = pos;
        }
    }
    
    public void Initialize(BSPDungeonGenerator generator)
    {
        dungeonGenerator = generator;
        
        if (dungeonGenerator != null)
        {
            floorTilemap = dungeonGenerator.floorTilemap;
            wallsTilemap = dungeonGenerator.wallsTilemap;
            Debug.Log("✅ A* initialized with tilemap references");
        }
        else
        {
            Debug.LogError("❌ Cannot initialize A* - generator is null");
        }
    }
    
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (dungeonGenerator == null)
        {
            Debug.LogError("❌ Dungeon generator not initialized!");
            return null;
        }
        
        Debug.Log($"🔍 A* starting: {start} → {goal}");
        
        if (!IsWalkable(start))
        {
            Debug.LogError($"❌ Start position {start} is not walkable!");
            return null;
        }
        
        if (!IsWalkable(goal))
        {
            Debug.LogError($"❌ Goal position {goal} is not walkable!");
            return null;
        }
        
        Debug.Log($"✅ Both positions are walkable, searching...");
        
        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        
        Node startNode = new Node(start);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(start, goal);
        
        openList.Add(startNode);
        
        int maxIterations = 10000;
        int iterations = 0;
        int maxOpenListSize = 0;
        
        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            if (openList.Count > maxOpenListSize)
                maxOpenListSize = openList.Count;
            
            Node currentNode = openList.OrderBy(n => n.fCost).ThenBy(n => n.hCost).First();
            openList.Remove(currentNode);
            closedSet.Add(currentNode.position);
            
            if (currentNode.position == goal)
            {
                List<Vector2Int> path = ReconstructPath(currentNode);
                Debug.Log($"✅ Path found! {path.Count} nodes, {iterations} iterations, max open: {maxOpenListSize}");
                return path;
            }
            
            List<Vector2Int> walkableNeighbors = new List<Vector2Int>();
            foreach (Vector2Int neighbor in GetNeighbors(currentNode.position))
            {
                if (closedSet.Contains(neighbor))
                    continue;
                
                if (!IsWalkable(neighbor))
                    continue;
                
                walkableNeighbors.Add(neighbor);
                
                float newGCost = currentNode.gCost + GetDistance(currentNode.position, neighbor);
                
                Node neighborNode = openList.FirstOrDefault(n => n.position == neighbor);
                
                if (neighborNode == null)
                {
                    neighborNode = new Node(neighbor);
                    neighborNode.parent = currentNode;
                    neighborNode.gCost = newGCost;
                    neighborNode.hCost = GetDistance(neighbor, goal);
                    openList.Add(neighborNode);
                }
                else if (newGCost < neighborNode.gCost)
                {
                    neighborNode.parent = currentNode;
                    neighborNode.gCost = newGCost;
                }
            }
            
            if (iterations <= 3)
            {
                Debug.Log($"   Iteration {iterations}: pos={currentNode.position}, walkable neighbors={walkableNeighbors.Count}");
            }
        }
        
        Debug.LogError($"❌ No path found after {iterations} iterations! Max open list: {maxOpenListSize}, Closed nodes: {closedSet.Count}");
        Debug.LogError($"   This suggests the goal is unreachable from start.");
        return null;
    }
    
    List<Vector2Int> ReconstructPath(Node goalNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node current = goalNode;
        
        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }
        
        path.Reverse();
        return path;
    }
    
    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(pos.x + 1, pos.y),      
            new Vector2Int(pos.x - 1, pos.y),     
            new Vector2Int(pos.x, pos.y + 1),     
            new Vector2Int(pos.x, pos.y - 1),     
            new Vector2Int(pos.x + 1, pos.y + 1), 
            new Vector2Int(pos.x - 1, pos.y + 1), 
            new Vector2Int(pos.x + 1, pos.y - 1), 
            new Vector2Int(pos.x - 1, pos.y - 1)  
        };
        
        return neighbors;
    }
    
    bool IsWalkable(Vector2Int pos)
    {
        if (floorTilemap == null || wallsTilemap == null)
        {
            Debug.LogError("Tilemaps not assigned!");
            return false;
        }
        
        Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
        
        bool hasFloor = floorTilemap.HasTile(tilePos);
        
        bool hasWall = wallsTilemap.HasTile(tilePos);
        
        bool isDoor = false;
        if (dungeonGenerator.objectsTilemap != null && dungeonGenerator.doorTile != null)
        {
            TileBase objectTile = dungeonGenerator.objectsTilemap.GetTile(tilePos);
            if (objectTile != null && objectTile == dungeonGenerator.doorTile)
            {
                isDoor = true;
            }
        }
        
        return (hasFloor || isDoor) && !hasWall;
    }
    
    float GetDistance(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        
        return Mathf.Max(dx, dy);
    }
}
}