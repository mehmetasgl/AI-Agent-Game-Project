using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class TileSpritesCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create Dungeon Tile Sprites")]
    public static void CreateTileSprites()
    {
        // Sprites klasörü yoksa oluştur
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }

        // Tile tipleri ve renkleri
        CreateTileSprite("Floor", new Color(0.7f, 0.7f, 0.7f)); // Gri zemin
        CreateTileSprite("Wall", new Color(0.3f, 0.3f, 0.3f));  // Koyu duvar
        CreateTileSprite("Door", new Color(0.6f, 0.3f, 0.1f));  // Kahverengi kapı
        CreateTileSprite("Corridor", new Color(0.5f, 0.5f, 0.5f)); // Orta gri koridor
        CreateTileSprite("Empty", new Color(0.1f, 0.1f, 0.1f)); // Siyah boşluk

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Generated Tile Sprit! in the Assets/Sprites.");
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
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                {
                    pixels[y * size + x] = color * 0.7f; // %30 daha koyu
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        string path = $"Assets/Sprites/{name}.png";
        System.IO.File.WriteAllBytes(path, bytes);
        
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32; // 1 Unity unit = 32 pixel
            importer.filterMode = FilterMode.Point; // Pixel art için
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
#endif
}
