using UnityEditor;
using UnityEngine;

public static class WebGLAssetOptimizer
{
    public static void Apply()
    {
        var changedCount = 0;
        changedCount += ApplyTextureSettings("Assets/_Project/Art/Items", 512);
        changedCount += ApplyTextureSettings("Assets/_Project/Art/Effects", 512);
        changedCount += ApplyTextureSettings("Assets/_Project/Art/AirPurifier", 1024);
        changedCount += ApplyTextureSettings("Assets/_Project/Art/Backgrounds", 1024);
        changedCount += ApplyTextureSettings("Assets/_Project/Art/Result", 1024);
        changedCount += ApplyTextureSettings("Assets/_Project/Art/Title", 1024);
        changedCount += ApplyTextureSettings("Assets/_Project/Art/UI", 1024);
        changedCount += ApplyAudioSettings("Assets/_Project/Audio/BGM", AudioClipLoadType.Streaming, true);
        changedCount += ApplyAudioSettings("Assets/_Project/Audio/SFX", AudioClipLoadType.CompressedInMemory, false);

        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[WebGLAssetOptimizer] Applied mobile WebGL import settings. ChangedAssets={changedCount}");
    }

    private static int ApplyTextureSettings(string folder, int maxTextureSize)
    {
        var changedCount = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            var changed = false;
            if (importer.isReadable)
            {
                importer.isReadable = false;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            var settings = importer.GetPlatformTextureSettings("WebGL");
            if (!settings.overridden ||
                settings.maxTextureSize != maxTextureSize ||
                settings.textureCompression != TextureImporterCompression.Compressed ||
                settings.format != TextureImporterFormat.Automatic ||
                settings.compressionQuality != 50)
            {
                settings.overridden = true;
                settings.maxTextureSize = maxTextureSize;
                settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                settings.format = TextureImporterFormat.Automatic;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.compressionQuality = 50;
                settings.crunchedCompression = false;
                importer.SetPlatformTextureSettings(settings);
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            importer.SaveAndReimport();
            changedCount++;
        }

        return changedCount;
    }

    private static int ApplyAudioSettings(string folder, AudioClipLoadType loadType, bool loadInBackground)
    {
        var changedCount = 0;
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                continue;
            }

            var changed = false;
            var settings = importer.defaultSampleSettings;
            if (settings.loadType != loadType)
            {
                settings.loadType = loadType;
                changed = true;
            }

            if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                changed = true;
            }

            var quality = loadType == AudioClipLoadType.Streaming ? 0.6f : 0.75f;
            if (!Mathf.Approximately(settings.quality, quality))
            {
                settings.quality = quality;
                changed = true;
            }

            if (importer.loadInBackground != loadInBackground)
            {
                importer.loadInBackground = loadInBackground;
                changed = true;
            }

            if (settings.preloadAudioData)
            {
                settings.preloadAudioData = false;
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            changedCount++;
        }

        return changedCount;
    }
}
