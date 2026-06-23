using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GanbareAirPurifier/SFX Database", fileName = "SfxDatabase")]
public class SfxDatabase : ScriptableObject
{
    [Serializable]
    public class SfxEntry
    {
        public string key;
        public AudioClip clip;
    }

    [SerializeField] private List<SfxEntry> entries = new List<SfxEntry>();

    private Dictionary<string, AudioClip> clipByKey;
    private readonly HashSet<string> warnedMissingKeys = new HashSet<string>();

    public IReadOnlyList<SfxEntry> Entries => entries;

    public AudioClip GetClip(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        EnsureCache();
        if (clipByKey.TryGetValue(key, out var clip))
        {
            return clip;
        }

        if (warnedMissingKeys.Add(key))
        {
            Debug.LogWarning($"[SfxDatabase] SFX key not found: {key}");
        }

        return null;
    }

    public void SetEntries(IEnumerable<SfxEntry> newEntries)
    {
        entries.Clear();
        if (newEntries != null)
        {
            entries.AddRange(newEntries);
        }

        clipByKey = null;
        warnedMissingKeys.Clear();
    }

    private void EnsureCache()
    {
        if (clipByKey != null)
        {
            return;
        }

        clipByKey = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.clip == null)
            {
                continue;
            }

            clipByKey[entry.key] = entry.clip;
        }
    }
}
