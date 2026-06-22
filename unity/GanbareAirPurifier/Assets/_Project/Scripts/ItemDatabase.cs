using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemDatabase
{
    private const string CsvRelativePath = "_Project/Data/CSV/ItemMaster.csv";
    private static readonly string[] SpriteSearchFolders =
    {
        "Assets/_Project/Art/Items",
        "Assets/_Project/Art/Effects"
    };
    private static readonly HashSet<string> WarnedMissingSprites = new HashSet<string>();
    private static ItemSpriteDatabase spriteDatabase;

    private readonly Dictionary<PurifierStage, List<ItemData>> itemsByStage = new Dictionary<PurifierStage, List<ItemData>>();

    public int TotalCount { get; private set; }

    public static void SetSpriteDatabase(ItemSpriteDatabase database)
    {
        spriteDatabase = database;
        WarnedMissingSprites.Clear();
    }

    public static ItemDatabase LoadDefault(TextAsset csvAsset, ItemSpriteDatabase database)
    {
        SetSpriteDatabase(database);
        var itemDatabase = new ItemDatabase();
        if (csvAsset != null)
        {
            itemDatabase.LoadCsv(csvAsset.text, csvAsset.name);
            return itemDatabase;
        }

        Debug.LogError("[ItemDatabase] ItemMaster TextAsset is not assigned. Falling back to development file path.");
        return LoadDefault();
    }

    public static ItemDatabase LoadDefault()
    {
        var database = new ItemDatabase();
        var path = Path.Combine(Application.dataPath, CsvRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogError($"[ItemDatabase] ItemMaster.csv not found: {path}");
            return database;
        }

        database.LoadCsv(File.ReadAllText(path), path);
        return database;
    }

    public int GetCount(PurifierStage stage)
    {
        return itemsByStage.TryGetValue(stage, out var items) ? items.Count : 0;
    }

    public IReadOnlyList<ItemData> GetItems(PurifierStage stage)
    {
        return itemsByStage.TryGetValue(stage, out var items) ? items : Array.Empty<ItemData>();
    }

    private void LoadCsv(string csv, string path)
    {
        itemsByStage.Clear();
        TotalCount = 0;
        if (string.IsNullOrWhiteSpace(csv))
        {
            Debug.LogError($"[ItemDatabase] ItemMaster.csv is empty: {path}");
            return;
        }

        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var columns = line.Split(',');
            if (columns.Length < 9)
            {
                Debug.LogWarning($"[ItemDatabase] Skipping invalid ItemMaster row {i + 1}: {line}");
                continue;
            }

            for (var c = 0; c < columns.Length; c++)
            {
                columns[c] = columns[c].Trim().Trim('\uFEFF');
            }

            if (!Enum.TryParse(columns[1], out PurifierStage stage))
            {
                Debug.LogWarning($"[ItemDatabase] Skipping ItemMaster row {i + 1}: invalid stage {columns[1]}");
                continue;
            }

            if (!Enum.TryParse(columns[4], out SizeCategory sizeCategory))
            {
                Debug.LogWarning($"[ItemDatabase] Skipping ItemMaster row {i + 1}: invalid sizeCategory {columns[4]}");
                continue;
            }

            if (!int.TryParse(columns[3], out var requiredLevel) ||
                !int.TryParse(columns[5], out var score) ||
                !float.TryParse(columns[6], out var gaugeGain) ||
                !int.TryParse(columns[7], out var spawnWeight))
            {
                Debug.LogWarning($"[ItemDatabase] Skipping ItemMaster row {i + 1}: invalid numeric value");
                continue;
            }

            var data = new ItemData(
                columns[0],
                stage,
                columns[2],
                requiredLevel,
                sizeCategory,
                score,
                gaugeGain,
                spawnWeight,
                columns[8]);

            data.Sprite = LoadSpriteOrNull(data.SpriteName);
            data.SuctionSprite = LoadOptionalSpriteOrNull($"{data.SpriteName}_Surprised");
            if (!itemsByStage.TryGetValue(data.Stage, out var stageItems))
            {
                stageItems = new List<ItemData>();
                itemsByStage.Add(data.Stage, stageItems);
            }

            stageItems.Add(data);
            TotalCount++;
        }

        Debug.Log($"[ItemDatabase] ItemMaster.csv loaded: {TotalCount} items. Home={GetCount(PurifierStage.Home)}, Street={GetCount(PurifierStage.Street)}, City={GetCount(PurifierStage.City)}, Space={GetCount(PurifierStage.Space)}");
    }

    public static Sprite LoadSpriteOrNull(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        var sprite = spriteDatabase != null ? spriteDatabase.GetSprite(spriteName) : null;
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>(spriteName);
        }
#if UNITY_EDITOR
        if (sprite == null)
        {
            sprite = LoadEditorSprite(spriteName);
        }
#endif
        if (sprite == null && WarnedMissingSprites.Add(spriteName))
        {
            Debug.LogWarning($"[ItemDatabase] Item sprite not found, using placeholder: {spriteName}");
        }

        return sprite;
    }

    public static Sprite LoadOptionalSpriteOrNull(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        var sprite = spriteDatabase != null ? spriteDatabase.GetSprite(spriteName) : null;
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>(spriteName);
        }
#if UNITY_EDITOR
        if (sprite == null)
        {
            sprite = LoadEditorSprite(spriteName);
        }
#endif
        return sprite;
    }

#if UNITY_EDITOR
    private static Sprite LoadEditorSprite(string spriteName)
    {
        var guids = AssetDatabase.FindAssets($"{spriteName} t:Texture2D", SpriteSearchFolders);
        if (guids == null || guids.Length == 0)
        {
            return null;
        }

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(fileName, spriteName, StringComparison.Ordinal))
            {
                continue;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite childSprite)
                {
                    return childSprite;
                }
            }
        }

        return null;
    }
#endif
}
