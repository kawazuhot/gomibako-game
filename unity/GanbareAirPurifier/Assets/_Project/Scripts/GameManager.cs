using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    private const float InitialTime = 90f;
    private const float PenaltySeconds = 5f;
    private const float GaugeGain = 34f;
    private const float NormalTimeScale = 1f;
    private const float FastForwardTimeScale = 2.5f;

    [Header("Runtime References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform itemLayer;
    [SerializeField] private RectTransform suctionZone;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private BackgroundController backgroundController;
    [SerializeField] private FadeController fadeController;
    [SerializeField] private Text timeText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text levelNumberText;
    [SerializeField] private Text stageText;
    [SerializeField] private Text resultText;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private SuctionHoldArea suctionHoldArea;
    [SerializeField] private Button fastForwardButton;
    [SerializeField] private AirPurifierController airPurifier;
    [SerializeField] private Sprite airPurifierNormalSprite;
    [SerializeField] private Sprite airPurifierSuctionSprite;
    [SerializeField] private Sprite airPurifierFailSprite;
    [SerializeField] private Sprite homeStageBackgroundSprite;
    [SerializeField] private ItemController itemTemplate;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private SuctionManager suctionManager;

    [Header("Suction Zone")]
    [SerializeField] private float suctionZoneRadius = 150f;

    private readonly List<ItemController> activeItems = new List<ItemController>();
    private readonly StageManager stageManager = new StageManager();
    private readonly GaugeManager gaugeManager = new GaugeManager();
    private readonly ScoreManager scoreManager = new ScoreManager();
    private readonly ComboManager comboManager = new ComboManager();
    private readonly TimerManager timerManager = new TimerManager();

    private ItemController highlightedItem;
    private bool keyboardFastForward;
    private bool isSuctionHeld;
    private bool isStageTransitioning;
    private bool isFastForwardEnabled;
    private int lastDisplayedSuctionLevel = -1;
    private float suppressPointerSuckUntilRealtime;

    public int CurrentSuctionLevel => gaugeManager.SuctionLevel;
    public bool IsTimeUp => timerManager.IsFinished;
    private float GameplaySpeedMultiplier => isFastForwardEnabled ? FastForwardTimeScale : NormalTimeScale;

    public void ConfigureAirPurifierSprites(Sprite normal, Sprite suction, Sprite fail)
    {
        airPurifierNormalSprite = normal;
        airPurifierSuctionSprite = suction;
        airPurifierFailSprite = fail;
    }

    public void ConfigureBackgroundSprites(Sprite homeBackground)
    {
        homeStageBackgroundSprite = homeBackground;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GameManager>() != null)
        {
            return;
        }

        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "SampleScene" || sceneName == "Main" || string.IsNullOrEmpty(sceneName))
        {
            new GameObject("MVP_GameManager").AddComponent<GameManager>();
        }
    }

    private void Awake()
    {
        Time.timeScale = NormalTimeScale;
        EnsureEventSystem();
        EnsureRuntimeView();
        EnsureComponents();
    }

    private void Start()
    {
        scoreManager.Reset();
        comboManager.Reset();
        gaugeManager.Reset();
        timerManager.Reset(InitialTime);
        stageManager.ApplyLevel(gaugeManager.SuctionLevel);
        ApplyStageVisuals(false);
        UpdateUi();
    }

    private void OnDestroy()
    {
        if (Time.timeScale != NormalTimeScale)
        {
            Time.timeScale = NormalTimeScale;
        }
    }

    private void Update()
    {
        if (isStageTransitioning)
        {
            UpdateUi();
            return;
        }

        timerManager.Tick(Time.unscaledDeltaTime);
        if (IsTimeUp && isSuctionHeld)
        {
            EndSuctionHold();
        }

        itemSpawner.Tick(Time.deltaTime * GameplaySpeedMultiplier);
        UpdateCandidateHighlight();
        HandleHeldSuction();
        HandleInput();
        UpdateUi();
    }

    public IReadOnlyList<ItemData> GetCurrentSpawnPool()
    {
        return stageManager.GetSpawnPool(gaugeManager.SuctionLevel);
    }

    public void RegisterItem(ItemController item)
    {
        if (item != null && !activeItems.Contains(item))
        {
            activeItems.Add(item);
            item.SetMoveSpeedMultiplier(GameplaySpeedMultiplier);
        }
    }

    public void HandleItemMissed(ItemController item)
    {
        activeItems.Remove(item);
        if (item != null)
        {
            item.KillTweens();
            Destroy(item.gameObject);
        }
    }

    public void TrySuck()
    {
        if (isStageTransitioning || suctionManager.IsBusy || IsTimeUp)
        {
            return;
        }

        var candidate = GetBestCandidate();
        if (candidate == null)
        {
            resultText.text = "空振り";
            return;
        }

        resultText.text = candidate.Data.RequiredLevel <= gaugeManager.SuctionLevel ? "吸引!" : "重すぎる!";
        suctionManager.TrySuck(candidate);
    }

    public void BeginSuctionHold()
    {
        if (isStageTransitioning || IsTimeUp)
        {
            return;
        }

        isSuctionHeld = true;
        if (!suctionManager.IsBusy)
        {
            airPurifier.StartSuctionHold();
        }
    }

    public void EndSuctionHold()
    {
        isSuctionHeld = false;
        if (!suctionManager.IsBusy)
        {
            airPurifier.StopSuctionHold();
        }
    }

    public void ResolveSuction(ItemController item, bool success)
    {
        activeItems.Remove(item);

        if (success)
        {
            var combo = comboManager.AddCombo();
            scoreManager.AddSuccessScore(item.Data.RequiredLevel, combo);
            var previousStage = stageManager.CurrentStage;
            var leveledUp = gaugeManager.AddGauge(GaugeGain);
            if (leveledUp)
            {
                var nextStage = gaugeManager.SuctionLevel >= 4 ? PurifierStage.City : PurifierStage.Home;
                var stageChanged = previousStage != nextStage;
                if (stageChanged)
                {
                    resultText.text = "ステージアップ!";
                    StartCoroutine(PlayStageTransition());
                }
                else
                {
                    stageManager.ApplyLevel(gaugeManager.SuctionLevel);
                    ApplyStageVisuals(false);
                    resultText.text = "Lv UP!";
                }
            }
            else
            {
                resultText.text = "SUCCESS";
            }
        }
        else
        {
            comboManager.Reset();
            timerManager.ApplyPenalty(PenaltySeconds);
            resultText.text = $"MISS -{PenaltySeconds:0}s";
        }

        if (item != null)
        {
            item.KillTweens();
            Destroy(item.gameObject);
        }

        RestoreAirPurifierState();
        UpdateUi();
    }

    public void SetFastForward(bool enabled)
    {
        if (isStageTransitioning && enabled)
        {
            return;
        }

        isFastForwardEnabled = enabled;
        ApplyFastForwardToActiveItems();
    }

    public void SuppressPointerSuckInput(float seconds = 0.2f)
    {
        suppressPointerSuckUntilRealtime = Mathf.Max(suppressPointerSuckUntilRealtime, Time.realtimeSinceStartup + seconds);
    }

    private void EnsureComponents()
    {
        if (itemSpawner == null)
        {
            itemSpawner = gameObject.AddComponent<ItemSpawner>();
        }

        if (suctionManager == null)
        {
            suctionManager = gameObject.AddComponent<SuctionManager>();
        }

        itemSpawner.Configure(this, itemLayer, itemTemplate);
        suctionManager.Configure(this, airPurifier);
    }

    private void EnsureRuntimeView()
    {
        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("MVP_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var root = canvasObject.GetComponent<RectTransform>();
        backgroundController = CreateBackgroundController(root, homeStageBackgroundSprite);
        CreatePanel("Play_Lane", root, new Vector2(0f, 0f), new Vector2(1080f, 520f), new Color(1f, 0.84f, 0.42f, 0.32f), false);

        suctionZone = CreatePanel("吸引ゾーン", root, new Vector2(0f, 30f), new Vector2(suctionZoneRadius * 2f, suctionZoneRadius * 2f), new Color(0.42f, 0.82f, 1f, 0.22f), false).rectTransform;
        var zoneOutline = suctionZone.gameObject.AddComponent<Outline>();
        zoneOutline.effectColor = new Color(0.05f, 0.55f, 1f, 0.8f);
        zoneOutline.effectDistance = new Vector2(8f, -8f);
        CreateText("Zone_Label", suctionZone, "吸引ゾーン", new Vector2(0f, 0f), new Vector2(240f, 80f), 36, Color.white, TextAnchor.MiddleCenter);

        itemLayer = CreateRect("ItemLayer", root, Vector2.zero, new Vector2(1080f, 1920f));
        itemTemplate = CreateItemTemplate(itemLayer);
        itemTemplate.gameObject.SetActive(false);

        airPurifier = CreateAirPurifier(root, airPurifierNormalSprite, airPurifierSuctionSprite, airPurifierFailSprite);

        timeText = CreateText("TIME_Text", root, "TIME 90", new Vector2(-390f, 830f), new Vector2(260f, 70f), 42, Color.white, TextAnchor.MiddleLeft);
        scoreText = CreateText("SCORE_Text", root, "SCORE 0", new Vector2(-390f, 760f), new Vector2(320f, 70f), 36, Color.white, TextAnchor.MiddleLeft);
        comboText = CreateText("COMBO_Text", root, "COMBO 0", new Vector2(360f, 760f), new Vector2(300f, 70f), 36, Color.white, TextAnchor.MiddleRight);
        var levelPanel = CreatePanel("SuctionLevel_Panel", root, new Vector2(430f, -390f), new Vector2(190f, 390f), Color.white, false);
        var levelPanelOutline = levelPanel.gameObject.AddComponent<Outline>();
        levelPanelOutline.effectColor = new Color(1f, 1f, 1f, 0.95f);
        levelPanelOutline.effectDistance = new Vector2(8f, -8f);
        CreatePanel("SuctionLevel_PanelFill", levelPanel.rectTransform, Vector2.zero, new Vector2(172f, 372f), new Color(1f, 0.89f, 0.22f), false);
        levelText = CreateText("SuctionLevel_Label", levelPanel.rectTransform, "吸引Lv", new Vector2(0f, 135f), new Vector2(160f, 60f), 28, new Color(0.12f, 0.18f, 0.28f), TextAnchor.MiddleCenter);
        levelNumberText = CreateText("SuctionLevel_Number", levelPanel.rectTransform, "1", new Vector2(0f, -30f), new Vector2(180f, 260f), 150, new Color(0.10f, 0.16f, 0.28f), TextAnchor.MiddleCenter);
        stageText = CreateText("Stage_Text", root, "家ステージ", new Vector2(0f, 720f), new Vector2(380f, 70f), 40, new Color(0.18f, 0.22f, 0.30f), TextAnchor.MiddleCenter);
        resultText = CreateText("Result_Text", root, "クリックで吸引", new Vector2(0f, -570f), new Vector2(520f, 80f), 38, new Color(0.16f, 0.20f, 0.28f), TextAnchor.MiddleCenter);

        var gaugeBack = CreatePanel("Gauge_Back", root, new Vector2(0f, 660f), new Vector2(520f, 34f), Color.white, false);
        gaugeFill = CreatePanel("Gauge_Fill", gaugeBack.rectTransform, new Vector2(-250f, 0f), new Vector2(500f, 22f), new Color(0.15f, 0.76f, 1f), false);
        gaugeFill.rectTransform.pivot = new Vector2(0f, 0.5f);

        suctionHoldArea = CreateSuctionHoldArea("AirPurifier_TapArea", root, new Vector2(0f, -590f), new Vector2(660f, 660f), this);

        fastForwardButton = CreateButton("FastForward_Button", root, "早送り\n長押し", new Vector2(-360f, -760f), new Vector2(300f, 130f), new Color(0.26f, 0.62f, 1f));
        var fastButton = fastForwardButton.gameObject.AddComponent<FastForwardButton>();
        fastButton.Configure(this);

        fadeController = CreateFadeController(root);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventObject = new GameObject("EventSystem");
        eventObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventObject.AddComponent<StandaloneInputModule>();
#endif
        DontDestroyOnLoad(eventObject);
    }

    private void UpdateCandidateHighlight()
    {
        var candidate = GetBestCandidate();
        if (highlightedItem != null && highlightedItem != candidate)
        {
            highlightedItem.SetHighlighted(false);
        }

        highlightedItem = candidate;
        if (highlightedItem != null)
        {
            highlightedItem.SetHighlighted(true);
        }
    }

    private ItemController GetBestCandidate()
    {
        ItemController best = null;
        var bestDistance = float.MaxValue;
        var center = suctionZone.anchoredPosition;

        for (var i = activeItems.Count - 1; i >= 0; i--)
        {
            var item = activeItems[i];
            if (item == null)
            {
                activeItems.RemoveAt(i);
                continue;
            }

            if (!item.IsAvailable)
            {
                continue;
            }

            var distance = Vector2.Distance(item.RectTransform.anchoredPosition, center);
            if (distance <= suctionZoneRadius && distance < bestDistance)
            {
                best = item;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void HandleInput()
    {
        if (WasSuckPressed())
        {
            TrySuck();
        }

        var fastHeld = IsFastForwardHeld();
        if (fastHeld != keyboardFastForward)
        {
            keyboardFastForward = fastHeld;
            SetFastForward(keyboardFastForward);
        }
    }

    private void HandleHeldSuction()
    {
        if (isStageTransitioning || !isSuctionHeld || suctionManager.IsBusy || IsTimeUp)
        {
            return;
        }

        var candidate = GetBestCandidate();
        if (candidate == null)
        {
            return;
        }

        resultText.text = candidate.Data.RequiredLevel <= gaugeManager.SuctionLevel ? "吸引!" : "重すぎる!";
        suctionManager.TrySuck(candidate);
    }

    private void RestoreAirPurifierState()
    {
        if (airPurifier == null)
        {
            return;
        }

        if (isSuctionHeld && !IsTimeUp)
        {
            airPurifier.StartSuctionHold();
        }
        else
        {
            airPurifier.SetNormal();
        }
    }

    private void ApplyFastForwardToActiveItems()
    {
        for (var i = activeItems.Count - 1; i >= 0; i--)
        {
            var item = activeItems[i];
            if (item == null)
            {
                activeItems.RemoveAt(i);
                continue;
            }

            item.SetMoveSpeedMultiplier(GameplaySpeedMultiplier);
        }
    }

    private IEnumerator PlayStageTransition()
    {
        isStageTransitioning = true;
        keyboardFastForward = false;
        SetFastForward(false);
        EndSuctionHold();
        resultText.text = "ステージアップ!";

        var zoomDone = false;
        backgroundController.PlayHomeStageZoomOut(() => zoomDone = true);
        while (!zoomDone)
        {
            yield return null;
        }

        if (fadeController != null)
        {
            yield return fadeController.FadeOut(0.4f);
        }

        ClearActiveItems();
        stageManager.ApplyLevel(gaugeManager.SuctionLevel);
        backgroundController.SetStreetBackground();
        resultText.text = "街ステージ!";
        UpdateUi();

        if (fadeController != null)
        {
            yield return fadeController.FadeIn(0.4f);
        }

        isStageTransitioning = false;
        resultText.text = "街ステージ!";
    }

    private void ClearActiveItems()
    {
        foreach (var item in activeItems)
        {
            if (item == null)
            {
                continue;
            }

            item.KillTweens();
            Destroy(item.gameObject);
        }

        activeItems.Clear();
        highlightedItem = null;
    }

    private bool WasSuckPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }
        return false;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private bool ShouldIgnorePointerSuckInput()
    {
        if (Time.realtimeSinceStartup < suppressPointerSuckUntilRealtime)
        {
            return true;
        }

        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsFastForwardHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.isPressed;
#else
        return Input.GetKey(KeyCode.F);
#endif
    }

    private void UpdateUi()
    {
        timeText.text = $"TIME {Mathf.CeilToInt(timerManager.TimeLeft):00}";
        scoreText.text = $"SCORE {scoreManager.Score}";
        comboText.text = $"COMBO {comboManager.Combo}";
        levelText.text = "吸引Lv";
        if (levelNumberText != null && lastDisplayedSuctionLevel != gaugeManager.SuctionLevel)
        {
            levelNumberText.text = gaugeManager.SuctionLevel.ToString();
            if (lastDisplayedSuctionLevel >= 0)
            {
                PlayLevelNumberBounce();
            }
            lastDisplayedSuctionLevel = gaugeManager.SuctionLevel;
        }
        stageText.text = stageManager.CurrentStageName;
        gaugeFill.rectTransform.sizeDelta = new Vector2(500f * gaugeManager.GaugeRate, 22f);

        if (IsTimeUp)
        {
            resultText.text = "TIME UP";
        }
    }

    private void ApplyStageVisuals(bool animateStageUp)
    {
        if (backgroundController == null)
        {
            return;
        }

        if (stageManager.CurrentStage == PurifierStage.Home)
        {
            backgroundController.SetHomeBackground();
            return;
        }

        if (animateStageUp)
        {
            backgroundController.PlayStageUpBackgroundTransition();
        }
        else
        {
            backgroundController.SetStreetBackground();
        }
    }

    private void PlayLevelNumberBounce()
    {
        if (levelNumberText == null)
        {
            return;
        }

        var target = levelNumberText.rectTransform;
        target.DOKill();
        target.localScale = Vector3.one;
        target.DOScale(1.22f, 0.12f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => target.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image CreatePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, bool raycastTarget)
    {
        var rect = CreateRect(name, parent, anchoredPosition, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static Text CreateText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAnchor alignment)
    {
        var rect = CreateRect(name, parent, anchoredPosition, size);
        var label = rect.gameObject.AddComponent<Text>();
        label.text = text;
        label.font = GetBuiltinFont();
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3f, -3f);
        return label;
    }

    private static Button CreateButton(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var image = CreatePanel(name, parent, anchoredPosition, size, Color.white, true);
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(6f, -6f);
        var inner = CreatePanel("Fill", image.rectTransform, Vector2.zero, size - new Vector2(14f, 14f), color, false);
        var label = CreateText("Label", image.rectTransform, text, Vector2.zero, size, 38, Color.white, TextAnchor.MiddleCenter);
        label.transform.SetAsLastSibling();
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = inner;
        return button;
    }

    private static BackgroundController CreateBackgroundController(RectTransform parent, Sprite homeSprite)
    {
        var root = CreateRect("BackgroundRoot", parent, Vector2.zero, new Vector2(1080f, 1920f));
        root.SetAsFirstSibling();

        var home = CreatePanel("HomeBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(1f, 0.92f, 0.74f), false);
        var street = CreatePanel("StreetBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0.70f, 0.88f, 1f), false);

        var controller = root.gameObject.AddComponent<BackgroundController>();
        controller.Configure(home, street, homeSprite);
        return controller;
    }

    private static FadeController CreateFadeController(RectTransform parent)
    {
        var overlay = CreatePanel("FadeOverlay", parent, Vector2.zero, new Vector2(1080f, 1920f), new Color(0f, 0f, 0f, 0f), true);
        overlay.rectTransform.SetAsLastSibling();
        var controller = overlay.gameObject.AddComponent<FadeController>();
        controller.Configure(overlay);
        return controller;
    }

    private static SuctionHoldArea CreateSuctionHoldArea(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, GameManager manager)
    {
        var image = CreatePanel(name, parent, anchoredPosition, size, new Color(1f, 1f, 1f, 0.01f), true);
        var holdArea = image.gameObject.AddComponent<SuctionHoldArea>();
        holdArea.Configure(manager);
        return holdArea;
    }

    private static ItemController CreateItemTemplate(RectTransform parent)
    {
        var border = CreatePanel("Item_Template", parent, Vector2.zero, new Vector2(150f, 92f), Color.white, false);
        var outline = border.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(5f, -5f);
        var fill = CreatePanel("Fill", border.rectTransform, Vector2.zero, new Vector2(136f, 78f), new Color(1f, 0.7f, 0.3f), false);
        var label = CreateText("Label", border.rectTransform, "Item", Vector2.zero, new Vector2(140f, 90f), 28, new Color(0.14f, 0.18f, 0.26f), TextAnchor.MiddleCenter);
        label.transform.SetAsLastSibling();
        var item = border.gameObject.AddComponent<ItemController>();
        item.Configure(border.rectTransform, border, fill, label, outline);
        return item;
    }

    private static AirPurifierController CreateAirPurifier(RectTransform parent, Sprite normalSprite, Sprite suctionSprite, Sprite failSprite)
    {
        var rootImage = CreatePanel("AirPurifier", parent, new Vector2(0f, -590f), new Vector2(600f, 600f), Color.white, false);
        rootImage.sprite = normalSprite;
        rootImage.preserveAspect = true;

        if (normalSprite == null)
        {
            var body = CreatePanel("Fallback_Body", rootImage.rectTransform, new Vector2(0f, -10f), new Vector2(235f, 225f), new Color(0.80f, 0.94f, 1f), false);
            CreateText("Fallback_Face", body.rectTransform, "空気\n清浄機", Vector2.zero, new Vector2(210f, 170f), 34, new Color(0.12f, 0.20f, 0.32f), TextAnchor.MiddleCenter);
            CreatePanel("Fallback_Intake", body.rectTransform, new Vector2(0f, 78f), new Vector2(160f, 28f), new Color(0.32f, 0.72f, 1f), false);
        }

        var suctionPoint = CreateRect("SuctionPoint", rootImage.rectTransform, new Vector2(0f, 280f), new Vector2(10f, 10f));
        var controller = rootImage.gameObject.AddComponent<AirPurifierController>();
        controller.Configure(rootImage.rectTransform, suctionPoint, rootImage, normalSprite, suctionSprite, failSprite);
        return controller;
    }

    private static Font GetBuiltinFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }
}
