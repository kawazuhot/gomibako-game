using System;
using UnityEngine;

public enum PurifierStage
{
    Home,
    City
}

[Serializable]
public class ItemData
{
    public string DisplayName;
    public int RequiredLevel;
    public PurifierStage Stage;
    public float BaseScale;
    public Color Color;

    public ItemData(string displayName, int requiredLevel, PurifierStage stage, float baseScale, Color color)
    {
        DisplayName = displayName;
        RequiredLevel = requiredLevel;
        Stage = stage;
        BaseScale = baseScale;
        Color = color;
    }
}
