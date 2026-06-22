using System;
using UnityEngine;

public enum PurifierStage
{
    Home,
    Street,
    City,
    Space
}

public enum SizeCategory
{
    Small,
    Medium,
    Large,
    Huge
}

public enum ItemType
{
    Normal,
    Bomb
}

[Serializable]
public class ItemData
{
    public string Id;
    public PurifierStage Stage;
    public string DisplayName;
    public int RequiredLevel;
    public SizeCategory SizeCategory;
    public int Score;
    public float GaugeGain;
    public int SpawnWeight;
    public string SpriteName;
    public Sprite Sprite;
    public Sprite SuctionSprite;
    public float BaseScale;
    public Color Color;
    public ItemType ItemType;
    public bool IsBomb => ItemType == ItemType.Bomb;

    public ItemData(string id, PurifierStage stage, string displayName, int requiredLevel, SizeCategory sizeCategory, int score, float gaugeGain, int spawnWeight, string spriteName)
    {
        Id = id;
        Stage = stage;
        DisplayName = displayName;
        RequiredLevel = requiredLevel;
        SizeCategory = sizeCategory;
        Score = score;
        GaugeGain = gaugeGain;
        SpawnWeight = Mathf.Max(0, spawnWeight);
        SpriteName = spriteName;
        BaseScale = (stage == PurifierStage.Home ? GetHomeScale(requiredLevel) : GetScale(sizeCategory)) * GetStageScaleMultiplier(stage) * GetItemScaleMultiplier(id);
        Color = GetPlaceholderColor(stage, requiredLevel);
        ItemType = ItemType.Normal;
    }

    public static ItemData CreateBomb(Sprite sprite)
    {
        var data = new ItemData("special_bomb", PurifierStage.Home, "爆弾", 999, SizeCategory.Medium, 0, 0f, 0, "Item_Bomb");
        data.Sprite = sprite;
        data.BaseScale = 2.875f;
        data.Color = new Color(0.08f, 0.08f, 0.10f);
        data.ItemType = ItemType.Bomb;
        return data;
    }

    public static float GetScale(SizeCategory category)
    {
        switch (category)
        {
            case SizeCategory.Small:
                return 0.75f;
            case SizeCategory.Medium:
                return 1.00f;
            case SizeCategory.Large:
                return 1.75f;
            case SizeCategory.Huge:
                return 2.20f;
            default:
                return 1.00f;
        }
    }

    private static float GetHomeScale(int requiredLevel)
    {
        switch (requiredLevel)
        {
            case 1:
                return 1.00f;
            case 2:
                return 2.00f;
            case 3:
                return 3.00f;
            default:
                return GetScale(SizeCategory.Huge);
        }
    }

    private static float GetStageScaleMultiplier(PurifierStage stage)
    {
        return stage == PurifierStage.Street || stage == PurifierStage.City || stage == PurifierStage.Space ? 2.5f : 1.0f;
    }

    private static float GetItemScaleMultiplier(string id)
    {
        switch (id)
        {
            case "street_high_school_girl":
            case "street_college_student":
            case "street_drunk_man":
                return 1.5f;
            default:
                return 1.0f;
        }
    }

    private static Color GetPlaceholderColor(PurifierStage stage, int requiredLevel)
    {
        var levelTint = Mathf.Clamp01((requiredLevel - 1) / 13f);
        switch (stage)
        {
            case PurifierStage.Home:
                return Color.Lerp(new Color(0.82f, 0.92f, 1f), new Color(1f, 0.72f, 0.86f), levelTint);
            case PurifierStage.Street:
                return Color.Lerp(new Color(1f, 0.88f, 0.36f), new Color(0.42f, 0.82f, 0.52f), levelTint);
            case PurifierStage.City:
                return Color.Lerp(new Color(0.48f, 0.78f, 1f), new Color(0.38f, 0.42f, 0.76f), levelTint);
            case PurifierStage.Space:
                return Color.Lerp(new Color(0.72f, 0.56f, 1f), new Color(1f, 0.62f, 0.24f), levelTint);
            default:
                return Color.white;
        }
    }
}
