using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WFCTileCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create WFC Decoration Tiles")]
    public static void CreateWFCTiles()
    {
        if (!AssetDatabase.IsValidFolder("Assets/20210601059/Sprites"))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }

        CreateTileSprite("Obstacle", new Color(0.8f, 0.4f, 0.2f)); 
        CreateTileSprite("Decoration_Wall", new Color(0.4f, 0.4f, 0.5f)); 
        CreateTileSprite("Trap", new Color(0.9f, 0.2f, 0.2f)); 
        CreateTileSprite("Pillar", new Color(0.6f, 0.5f, 0.4f)); 
        CreateTileSprite("Rubble", new Color(0.5f, 0.45f, 0.4f)); 
        
        CreateTileSprite("Floor_Dark", new Color(0.3f, 0.3f, 0.35f)); 
        CreateTileSprite("Floor_Light", new Color(0.8f, 0.75f, 0.7f)); 

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("WFC decoration tiles generated! Looking Assets/Sprites.");
    }

    private static void CreateTileSprite(string name, Color color)
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        
        Color[] pixels = new Color[size * size];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        if (name.Contains("Obstacle"))
        {
            
            for (int i = 0; i < size; i++)
            {
                pixels[i * size + i] = color * 0.5f; 
                pixels[i * size + (size - 1 - i)] = color * 0.5f; 
            }
        }
        else if (name.Contains("Trap"))
        {
            
            int center = size / 2;
            int radius = size / 6;
            for (int x = center - radius; x <= center + radius; x++)
            {
                for (int y = center - radius; y <= center + radius; y++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = color * 0.4f;
                    }
                }
            }
        }
        else if (name.Contains("Pillar"))
        {
            
            for (int x = 4; x < size; x += 8)
            {
                for (int y = 0; y < size; y++)
                {
                    pixels[y * size + x] = color * 0.7f;
                }
            }
        }
        else if (name.Contains("Rubble"))
        {
            
            System.Random rnd = new System.Random(name.GetHashCode());
            for (int i = 0; i < size * size / 8; i++)
            {
                int x = rnd.Next(0, size);
                int y = rnd.Next(0, size);
                pixels[y * size + x] = color * 0.6f;
            }
        }
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                {
                    pixels[y * size + x] = color * 0.6f;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        string path = $"Assets/20210601059/Sprites/{name}.png";
        System.IO.File.WriteAllBytes(path, bytes);
        
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
#endif
}