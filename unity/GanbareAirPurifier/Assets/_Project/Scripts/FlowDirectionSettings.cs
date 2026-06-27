using UnityEngine;

public enum ItemFlowDirection
{
    RightToLeft = 0,
    LeftToRight = 1
}

public static class FlowDirectionSettings
{
    public const string PlayerPrefsKey = "FlowDirection";

    public static ItemFlowDirection Load()
    {
        var value = PlayerPrefs.GetInt(PlayerPrefsKey, (int)ItemFlowDirection.RightToLeft);
        return value == (int)ItemFlowDirection.LeftToRight
            ? ItemFlowDirection.LeftToRight
            : ItemFlowDirection.RightToLeft;
    }

    public static void Save(ItemFlowDirection direction)
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, (int)direction);
        PlayerPrefs.Save();
    }

    public static string GetDisplayName(ItemFlowDirection direction)
    {
        return direction == ItemFlowDirection.LeftToRight ? "左 → 右" : "右 → 左";
    }
}
