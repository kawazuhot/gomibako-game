using UnityEngine;

public static class LevelColorUtility
{
    public static Color GetLevelColor(int level)
    {
        var levelMod = level % 3;
        if (levelMod == 1)
        {
            return new Color(0.10f, 0.48f, 1.00f, 0.96f);
        }

        if (levelMod == 2)
        {
            return new Color(0.16f, 0.72f, 0.30f, 0.96f);
        }

        return new Color(1.00f, 0.36f, 0.08f, 0.96f);
    }
}
