using System.Collections.Generic;
using UnityEngine;

public class StageManager
{
    private readonly List<ItemData> homeItems = new List<ItemData>
    {
        new ItemData("ホコリ", 1, PurifierStage.Home, 0.75f, new Color(0.74f, 0.72f, 0.68f)),
        new ItemData("紙くず", 1, PurifierStage.Home, 0.75f, new Color(0.90f, 0.88f, 0.78f)),
        new ItemData("ティッシュ", 2, PurifierStage.Home, 1.00f, new Color(0.75f, 0.90f, 1.00f)),
        new ItemData("ペン", 2, PurifierStage.Home, 1.00f, new Color(0.35f, 0.62f, 1.00f)),
        new ItemData("缶", 2, PurifierStage.Home, 1.00f, new Color(0.90f, 0.72f, 0.35f)),
        new ItemData("クッション", 3, PurifierStage.Home, 1.75f, new Color(1.00f, 0.62f, 0.78f)),
        new ItemData("椅子", 3, PurifierStage.Home, 1.75f, new Color(0.70f, 0.45f, 0.26f)),
        new ItemData("ぬいぐるみ", 3, PurifierStage.Home, 1.75f, new Color(1.00f, 0.70f, 0.50f))
    };

    private readonly List<ItemData> cityItems = new List<ItemData>
    {
        new ItemData("看板", 4, PurifierStage.City, 0.75f, new Color(1.00f, 0.78f, 0.24f)),
        new ItemData("小さい植木", 4, PurifierStage.City, 0.75f, new Color(0.34f, 0.78f, 0.38f)),
        new ItemData("空き缶", 4, PurifierStage.City, 0.75f, new Color(0.78f, 0.82f, 0.90f)),
        new ItemData("自転車", 5, PurifierStage.City, 1.00f, new Color(0.32f, 0.72f, 0.92f)),
        new ItemData("信号", 5, PurifierStage.City, 1.00f, new Color(0.18f, 0.22f, 0.28f)),
        new ItemData("ベンチ", 5, PurifierStage.City, 1.00f, new Color(0.68f, 0.44f, 0.26f)),
        new ItemData("車", 6, PurifierStage.City, 1.75f, new Color(1.00f, 0.35f, 0.35f)),
        new ItemData("小さい家", 6, PurifierStage.City, 1.75f, new Color(0.82f, 0.54f, 0.35f))
    };

    public string CurrentStageName { get; private set; } = "家ステージ";
    public PurifierStage CurrentStage { get; private set; } = PurifierStage.Home;

    public bool ApplyLevel(int suctionLevel)
    {
        var nextStage = suctionLevel >= 4 ? PurifierStage.City : PurifierStage.Home;
        var changed = nextStage != CurrentStage;
        CurrentStage = nextStage;
        CurrentStageName = nextStage == PurifierStage.Home ? "家ステージ" : "街ステージ";
        return changed;
    }

    public IReadOnlyList<ItemData> GetSpawnPool(int suctionLevel)
    {
        var source = suctionLevel >= 4 ? cityItems : homeItems;
        var maxLevel = Mathf.Min(suctionLevel + 1, suctionLevel >= 4 ? 6 : 3);
        var pool = new List<ItemData>();
        foreach (var item in source)
        {
            if (item.RequiredLevel <= maxLevel)
            {
                pool.Add(item);
            }
        }
        return pool;
    }
}
