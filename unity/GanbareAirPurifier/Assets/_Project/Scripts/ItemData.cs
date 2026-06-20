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
    public float BaseScale;
    public Color Color;

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
        BaseScale = GetScale(sizeCategory);
        Color = GetPlaceholderColor(stage, requiredLevel);
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
