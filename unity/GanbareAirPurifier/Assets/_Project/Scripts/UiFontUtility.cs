using UnityEngine;

public static class UiFontUtility
{
    private const string JapaneseFontResourcePath = "Fonts/GanbareJapanese";

    private static Font cachedFont;

    public static Font GetDefaultFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        cachedFont = Resources.Load<Font>(JapaneseFontResourcePath);
        if (cachedFont != null)
        {
            return cachedFont;
        }

        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont != null)
        {
            return cachedFont;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
