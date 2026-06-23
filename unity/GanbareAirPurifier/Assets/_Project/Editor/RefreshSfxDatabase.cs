using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RefreshSfxDatabase
{
    private const string DatabasePath = "Assets/_Project/Data/SfxDatabase.asset";
    private const string SfxFolder = "Assets/_Project/Audio/SFX";

    [MenuItem("GanbareAirPurifier/Refresh SFX Database")]
    public static void Refresh()
    {
        var database = AssetDatabase.LoadAssetAtPath<SfxDatabase>(DatabasePath);
        if (database == null)
        {
            EnsureDirectory(Path.GetDirectoryName(DatabasePath));
            database = ScriptableObject.CreateInstance<SfxDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        var entries = new List<SfxDatabase.SfxEntry>();
        if (AssetDatabase.IsValidFolder(SfxFolder))
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    continue;
                }

                entries.Add(new SfxDatabase.SfxEntry
                {
                    key = Path.GetFileNameWithoutExtension(path),
                    clip = clip
                });
            }
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
        AddAliasIfMissing(entries, "LightKick", "LightKick1");
        database.SetEntries(entries);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log($"[RefreshSfxDatabase] Registered {entries.Count} clips to {DatabasePath}");
    }

    private static void AddAliasIfMissing(List<SfxDatabase.SfxEntry> entries, string aliasKey, string sourceKey)
    {
        if (entries.Exists(entry => entry.key == aliasKey))
        {
            return;
        }

        var source = entries.Find(entry => entry.key == sourceKey);
        if (source == null || source.clip == null)
        {
            return;
        }

        entries.Add(new SfxDatabase.SfxEntry
        {
            key = aliasKey,
            clip = source.clip
        });
        entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.ImportAsset(path);
        }
    }
}
