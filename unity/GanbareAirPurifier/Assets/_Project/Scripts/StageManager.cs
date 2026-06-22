using System.Collections.Generic;
using UnityEngine;

public class StageManager
{
    private readonly List<ItemData> spawnPoolBuffer = new List<ItemData>();
    private ItemDatabase itemDatabase;
    private PurifierStage lastLoggedStage;
    private int lastLoggedLevel = -1;
    private int lastLoggedPoolCount = -1;

    public string CurrentStageName { get; private set; } = "家ステージ";
    public PurifierStage CurrentStage { get; private set; } = PurifierStage.Home;

    public void Initialize(ItemDatabase database)
    {
        itemDatabase = database;
        lastLoggedLevel = -1;
        lastLoggedPoolCount = -1;
        Debug.Log($"[StageManager] ItemDatabase initialized. Total={itemDatabase?.TotalCount ?? 0}, Home={itemDatabase?.GetCount(PurifierStage.Home) ?? 0}, Street={itemDatabase?.GetCount(PurifierStage.Street) ?? 0}");
    }

    public bool ApplyLevel(int suctionLevel)
    {
        var nextStage = GetStageForLevel(suctionLevel);
        var changed = nextStage != CurrentStage;
        CurrentStage = nextStage;
        CurrentStageName = GetStageDisplayName(nextStage);
        Debug.Log($"[StageManager] Current Stage: {CurrentStage}, Lv={suctionLevel}");
        return changed;
    }

    public IReadOnlyList<ItemData> GetSpawnPool(int suctionLevel)
    {
        spawnPoolBuffer.Clear();
        if (itemDatabase == null)
        {
            Debug.LogError("[StageManager] ItemDatabase is not initialized. No spawn candidates.");
            return spawnPoolBuffer;
        }

        var stage = GetStageForLevel(suctionLevel);
        var maxLevel = GetMaxPreviewLevel(suctionLevel, stage);
        var items = itemDatabase.GetItems(stage);
        foreach (var item in items)
        {
            if (item.RequiredLevel <= maxLevel)
            {
                spawnPoolBuffer.Add(item);
            }
        }

        if (stage != lastLoggedStage || suctionLevel != lastLoggedLevel || spawnPoolBuffer.Count != lastLoggedPoolCount)
        {
            Debug.Log($"[StageManager] Spawn candidates count: {spawnPoolBuffer.Count} for stage={stage}, Lv={suctionLevel}, maxLevel={maxLevel}, stageItems={items.Count}");
            if (spawnPoolBuffer.Count == 0)
            {
                Debug.LogWarning($"[ItemSpawner] No spawn candidates for stage: {stage}");
            }

            lastLoggedStage = stage;
            lastLoggedLevel = suctionLevel;
            lastLoggedPoolCount = spawnPoolBuffer.Count;
        }

        return spawnPoolBuffer;
    }

    public static PurifierStage GetStageForLevel(int suctionLevel)
    {
        if (suctionLevel >= 10)
        {
            return PurifierStage.Space;
        }

        if (suctionLevel >= 7)
        {
            return PurifierStage.City;
        }

        if (suctionLevel >= 4)
        {
            return PurifierStage.Street;
        }

        return PurifierStage.Home;
    }

    private static int GetMaxPreviewLevel(int suctionLevel, PurifierStage stage)
    {
        switch (stage)
        {
            case PurifierStage.Home:
                return System.Math.Min(suctionLevel + 1, 3);
            case PurifierStage.Street:
                return System.Math.Min(suctionLevel + 1, 6);
            case PurifierStage.City:
                return System.Math.Min(suctionLevel + 1, 9);
            case PurifierStage.Space:
                return System.Math.Min(suctionLevel + 1, 12);
            default:
                return suctionLevel + 1;
        }
    }

    private static string GetStageDisplayName(PurifierStage stage)
    {
        switch (stage)
        {
            case PurifierStage.Home:
                return "家ステージ";
            case PurifierStage.Street:
                return "街ステージ";
            case PurifierStage.City:
                return "都市ステージ";
            case PurifierStage.Space:
                return "宇宙ステージ";
            default:
                return "ステージ";
        }
    }
}
