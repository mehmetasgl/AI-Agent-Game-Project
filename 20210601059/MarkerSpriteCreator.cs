using UnityEngine;
using UnityEditor;
using System.IO;

public class MarkerSpriteCreator
{
    [MenuItem("Assets/Create/Colored Marker Sprites")]
    static void CreateMarkerSprites()
    {
        int size = 32;
        
        CreateColoredSprite("SpawnMarker", size, Color.green);
        
        CreateColoredSprite("GoalMarker", size, Color.red);
        
        AssetDatabase.Refresh();
        Debug.Log("✓ Spawn ve Goal marker sprites generated!");
    }
    
    static void CreateColoredSprite(string name, int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                {
                    texture.SetPixel(x, y, color * 0.7f);
                }
                else
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
        
        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        string path = "Assets/Sprites/" + name + ".png";
        
        if (!Directory.Exists("Assets/Sprites"))
        {
            Directory.CreateDirectory("Assets/Sprites");
        }
        
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.WriteImportSettingsIfDirty(path);
        }
        
        Debug.Log($"✓ {name} generated: {path}");
    }
}