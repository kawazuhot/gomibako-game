using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GanbareAirPurifierMvpSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    private const string AirPurifierNormalPath = "Assets/_Project/Art/AirPurifier/AirPurifier_Normal.png";
    private const string AirPurifierSuctionPath = "Assets/_Project/Art/AirPurifier/AirPurifier_Suction.png";
    private const string AirPurifierFailPath = "Assets/_Project/Art/AirPurifier/AirPurifier_Fail.png";
    private const string HomeStageBackgroundPath = "Assets/_Project/Art/Backgrounds/HomeStage_Background.png";
    private const string StreetStageBackgroundPath = "Assets/_Project/Art/Backgrounds/StreetStage_Background.png";

    [MenuItem("GanbareAirPurifier/Rebuild MVP Scene")]
    public static void RebuildMvpScene()
    {
        EnsureDirectory("Assets/_Project/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var manager = new GameObject("MVP_GameManager");
        ConfigureAirPurifierImportSettings();
        var gameManager = manager.AddComponent<GameManager>();
        gameManager.ConfigureAirPurifierSprites(
            AssetDatabase.LoadAssetAtPath<Sprite>(AirPurifierNormalPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(AirPurifierSuctionPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(AirPurifierFailPath));
        gameManager.ConfigureBackgroundSprites(
            AssetDatabase.LoadAssetAtPath<Sprite>(HomeStageBackgroundPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(StreetStageBackgroundPath));

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt GanbareAirPurifier MVP scene: {ScenePath}");
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.ImportAsset(path);
        }
    }

    private static void ConfigureAirPurifierImportSettings()
    {
        ConfigureSpriteImport(AirPurifierNormalPath);
        ConfigureSpriteImport(AirPurifierSuctionPath);
        ConfigureSpriteImport(AirPurifierFailPath);
        ConfigureSpriteImport(HomeStageBackgroundPath);
        ConfigureSpriteImport(StreetStageBackgroundPath);
    }

    private static void ConfigureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }
}
