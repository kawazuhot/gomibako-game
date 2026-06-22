using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RefreshItemSpriteDatabase
{
    private const string DatabasePath = "Assets/_Project/Data/ItemSpriteDatabase.asset";

    private static readonly string[] SpriteFolders =
    {
        "Assets/_Project/Art/Items/Home",
        "Assets/_Project/Art/Items/Street",
        "Assets/_Project/Art/Items/City",
        "Assets/_Project/Art/Items/Space",
        "Assets/_Project/Art/Items/Special",
        "Assets/_Project/Art/Effects"
    };

    [MenuItem("GanbareAirPurifier/Refresh Item Sprite Database")]
    public static void Refresh()
    {
        var database = AssetDatabase.LoadAssetAtPath<ItemSpriteDatabase>(DatabasePath);
        if (database == null)
        {
            EnsureDirectory(Path.GetDirectoryName(DatabasePath));
            database = ScriptableObject.CreateInstance<ItemSpriteDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        var entries = new List<ItemSpriteDatabase.SpriteEntry>();
        foreach (var folder in SpriteFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                continue;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                ConfigureSpriteImport(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in assets)
                    {
                        if (asset is Sprite childSprite)
                        {
                            sprite = childSprite;
                            break;
                        }
                    }
                }

                if (sprite == null)
                {
                    Debug.LogWarning($"[RefreshItemSpriteDatabase] Sprite not found at {path}");
                    continue;
                }

                entries.Add(new ItemSpriteDatabase.SpriteEntry
                {
                    spriteName = Path.GetFileNameWithoutExtension(path),
                    sprite = sprite
                });
            }
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.spriteName, b.spriteName));
        database.SetEntries(entries);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log($"[RefreshItemSpriteDatabase] Registered {entries.Count} sprites to {DatabasePath}");
    }

    private static void ConfigureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static void EnsureDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || Directory.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(path);
        AssetDatabase.ImportAsset(path);
    }
}
