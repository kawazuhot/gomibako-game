using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GanbareAirPurifierMvpSceneBuilder
{
    private const string TitleScenePath = "Assets/_Project/Scenes/TitleScene.unity";
    private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
    private const string GameplaySceneName = "Main";
    private const string TitleMainVisualPath = "Assets/_Project/Art/Title/Title_MainVisual.png";
    private const string TitleLogoPath = "Assets/_Project/Art/UI/Title/title_logo.png";
    private const string AirPurifierNormalPath = "Assets/_Project/Art/AirPurifier/AirPurifier_Normal.png";
    private const string AirPurifierSuctionPath = "Assets/_Project/Art/AirPurifier/AirPurifier_Suction.png";
    private const string AirPurifierFailPath = "Assets/_Project/Art/AirPurifier/AirPurifier_Fail.png";
    private const string HomeStageBackgroundPath = "Assets/_Project/Art/Backgrounds/HomeStage_Background.png";
    private const string StreetStageBackgroundPath = "Assets/_Project/Art/Backgrounds/StreetStage_Background.png";
    private const string CityStageBackgroundPath = "Assets/_Project/Art/Backgrounds/Background_City_Aerial.png";
    private const string SpaceStageBackgroundPath = "Assets/_Project/Art/Backgrounds/Background_Space.png";
    private const string BottomVisibilityOverlayPath = "Assets/_Project/Art/UI/BottomVisibilityOverlay.png";
    private const string ItemMasterCsvPath = "Assets/_Project/Data/CSV/ItemMaster.csv";
    private const string ItemSpriteDatabasePath = "Assets/_Project/Data/ItemSpriteDatabase.asset";
    private const string SfxDatabasePath = "Assets/_Project/Data/SfxDatabase.asset";
    private const string GameplayBgmPath = "Assets/_Project/Audio/BGM/BGM_Gameplay_Main.mp3";

    [MenuItem("GanbareAirPurifier/Rebuild MVP Scene")]
    public static void RebuildMvpScene()
    {
        EnsureDirectory("Assets/_Project/Scenes");
        ConfigureAirPurifierImportSettings();
        RefreshItemSpriteDatabase.Refresh();
        RefreshSfxDatabase.Refresh();
        RebuildGameplayScene();
        RebuildTitleScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt GanbareAirPurifier scenes: {TitleScenePath}, {ScenePath}");
    }

    [MenuItem("GanbareAirPurifier/Rebuild Title Scene")]
    public static void RebuildTitleSceneOnly()
    {
        EnsureDirectory("Assets/_Project/Scenes");
        ConfigureSpriteImport(TitleMainVisualPath);
        ConfigureSpriteImport(TitleLogoPath);
        RebuildTitleScene();
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt GanbareAirPurifier title scene: {TitleScenePath}");
    }

    private static void RebuildGameplayScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var manager = new GameObject("MVP_GameManager");
        var gameManager = manager.AddComponent<GameManager>();
        gameManager.ConfigureAirPurifierSprites(
            AssetDatabase.LoadAssetAtPath<Sprite>(AirPurifierNormalPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(AirPurifierSuctionPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(AirPurifierFailPath));
        gameManager.ConfigureBackgroundSprites(
            AssetDatabase.LoadAssetAtPath<Sprite>(HomeStageBackgroundPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(StreetStageBackgroundPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(CityStageBackgroundPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(SpaceStageBackgroundPath));
        gameManager.ConfigureBottomVisibilityOverlay(AssetDatabase.LoadAssetAtPath<Sprite>(BottomVisibilityOverlayPath));
        gameManager.ConfigureDataAssets(
            AssetDatabase.LoadAssetAtPath<TextAsset>(ItemMasterCsvPath),
            AssetDatabase.LoadAssetAtPath<ItemSpriteDatabase>(ItemSpriteDatabasePath),
            AssetDatabase.LoadAssetAtPath<SfxDatabase>(SfxDatabasePath));
        gameManager.ConfigureBgmAsset(AssetDatabase.LoadAssetAtPath<AudioClip>(GameplayBgmPath));

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Rebuilt GanbareAirPurifier MVP scene: {ScenePath}");
    }

    private static void RebuildTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvasObject = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasObject.GetComponent<RectTransform>();
        var background = CreatePanel("TitleBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0.72f, 0.92f, 1f, 1f), false);

        var titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleMainVisualPath);
        var titleImage = CreatePanel("Title_MainVisual", root, Vector2.zero, new Vector2(1080f, 1920f), Color.white, false);
        titleImage.sprite = titleSprite;
        titleImage.preserveAspect = true;
        titleImage.color = titleSprite != null ? Color.white : new Color(1f, 0.94f, 0.52f, 1f);
        titleImage.gameObject.AddComponent<AspectFillImage>();
        titleImage.transform.SetAsLastSibling();

        var titleLogoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleLogoPath);
        var titleLogo = CreatePanel("TitleLogo", root, new Vector2(25f, -390f), new Vector2(880f, 424f), Color.white, false);
        AnchorTopCenter(titleLogo.rectTransform);
        titleLogo.sprite = titleLogoSprite;
        titleLogo.preserveAspect = true;
        titleLogo.raycastTarget = false;
        titleLogo.color = titleLogoSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        titleLogo.transform.SetAsLastSibling();

        var startText = CreateText("Start_Text", root, "タップでスタート", new Vector2(0f, -760f), new Vector2(620f, 90f), 46, Color.white, TextAnchor.MiddleCenter);
        var startOutline = startText.GetComponent<Outline>();
        if (startOutline != null)
        {
            startOutline.effectColor = new Color(0.08f, 0.18f, 0.32f, 0.95f);
            startOutline.effectDistance = new Vector2(5f, -5f);
        }

        var fadePanel = CreatePanel("FadePanel", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0f, 0f, 0f, 0f), false);
        StretchToParent(fadePanel.rectTransform);
        fadePanel.transform.SetAsLastSibling();

        var controllerObject = new GameObject("TitleSceneController");
        var controller = controllerObject.AddComponent<TitleSceneController>();
        controller.Configure(titleImage, titleLogo, startText, fadePanel, GameplaySceneName, 0.32f);

        background.transform.SetAsFirstSibling();
        EditorSceneManager.SaveScene(scene, TitleScenePath);
        Debug.Log($"Rebuilt GanbareAirPurifier title scene: {TitleScenePath}");
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
        ConfigureSpriteImport(TitleMainVisualPath);
        ConfigureSpriteImport(TitleLogoPath);
        ConfigureSpriteImport(HomeStageBackgroundPath);
        ConfigureSpriteImport(StreetStageBackgroundPath);
        ConfigureSpriteImport(CityStageBackgroundPath);
        ConfigureSpriteImport(SpaceStageBackgroundPath);
        ConfigureSpriteImport(BottomVisibilityOverlayPath);
        ConfigureSpriteImportsInFolder("Assets/_Project/Art/Items/Home");
        ConfigureSpriteImportsInFolder("Assets/_Project/Art/Items/Street");
        ConfigureSpriteImportsInFolder("Assets/_Project/Art/Items/City");
        ConfigureSpriteImportsInFolder("Assets/_Project/Art/Items/Space");
        ConfigureSpriteImportsInFolder("Assets/_Project/Art/Items/Special");
        ConfigureSpriteImportsInFolder("Assets/_Project/Art/Effects");
    }

    private static Image CreatePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, bool raycastTarget)
    {
        var rect = CreateRect(name, parent, anchoredPosition, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AnchorTopCenter(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static Text CreateText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAnchor alignment)
    {
        var rect = CreateRect(name, parent, anchoredPosition, size);
        var label = rect.gameObject.AddComponent<Text>();
        label.text = text;
        label.font = UiFontUtility.GetDefaultFont();
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        rect.gameObject.AddComponent<Outline>();
        return label;
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static void ConfigureSpriteImportsInFolder(string folder)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            ConfigureSpriteImport(AssetDatabase.GUIDToAssetPath(guid));
        }
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
