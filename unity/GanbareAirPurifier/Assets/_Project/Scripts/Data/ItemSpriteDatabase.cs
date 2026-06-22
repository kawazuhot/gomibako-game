using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GanbareAirPurifier/Item Sprite Database", fileName = "ItemSpriteDatabase")]
public class ItemSpriteDatabase : ScriptableObject
{
    [Serializable]
    public class SpriteEntry
    {
        public string spriteName;
        public Sprite sprite;
    }

    [SerializeField] private List<SpriteEntry> entries = new List<SpriteEntry>();

    private Dictionary<string, Sprite> spriteByName;

    public IReadOnlyList<SpriteEntry> Entries => entries;

    public Sprite GetSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        EnsureCache();
        return spriteByName.TryGetValue(spriteName, out var sprite) ? sprite : null;
    }

    public void SetEntries(IEnumerable<SpriteEntry> newEntries)
    {
        entries.Clear();
        if (newEntries != null)
        {
            entries.AddRange(newEntries);
        }

        spriteByName = null;
    }

    private void EnsureCache()
    {
        if (spriteByName != null)
        {
            return;
        }

        spriteByName = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.spriteName) || entry.sprite == null)
            {
                continue;
            }

            spriteByName[entry.spriteName] = entry.sprite;
        }
    }
}
