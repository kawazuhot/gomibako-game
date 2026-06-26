using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        WaitingToStart,
        Countdown,
        Playing,
        GameOver
    }

    private const float InitialTime = 90f;
    private const float PenaltySeconds = 5f;
    private const float NormalTimeScale = 1f;
    private const float FastForwardTimeScale = 2.5f;
    private const int RoundedPanelTextureSize = 48;
    private const int RoundedPanelRadius = 12;

    [Header("Runtime References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform itemLayer;
    [SerializeField] private RectTransform scorePopupLayer;
    [SerializeField] private RectTransform suctionZone;
    [SerializeField] private SuctionZoneVisualController suctionZoneVisual;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image bombExplosionImage;
    [SerializeField] private BackgroundController backgroundController;
    [SerializeField] private FadeController fadeController;
    [SerializeField] private Text timeText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text levelNumberText;
    [SerializeField] private Image levelPanelFill;
    [SerializeField] private Text stageText;
    [SerializeField] private Text resultText;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private Image startOverlayDimPanel;
    [SerializeField] private Text startPromptText;
    [SerializeField] private Text countdownText;
    [SerializeField] private TargetMarkerController targetMarkerController;
    [SerializeField] private Button fastForwardButton;
    [SerializeField] private Button gameplayRestartButton;
    [SerializeField] private AirPurifierController airPurifier;
    [SerializeField] private Sprite airPurifierNormalSprite;
    [SerializeField] private Sprite airPurifierSuctionSprite;
    [SerializeField] private Sprite airPurifierFailSprite;
    [SerializeField] private Sprite homeStageBackgroundSprite;
    [SerializeField] private Sprite streetStageBackgroundSprite;
    [SerializeField] private Sprite cityStageBackgroundSprite;
    [SerializeField] private Sprite spaceStageBackgroundSprite;
    [SerializeField] private Sprite bottomVisibilityOverlaySprite;
    [SerializeField] private Sprite bombExplosionSprite;
    [SerializeField] private TextAsset itemMasterCsv;
    [SerializeField] private ItemSpriteDatabase itemSpriteDatabase;
    [SerializeField] private SfxDatabase sfxDatabase;
    [SerializeField] private AudioClip gameplayBgmClip;
    [SerializeField] private ItemController itemTemplate;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private SuctionManager suctionManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private ResultController resultController;

    [Header("Suction Zone")]
    [SerializeField] private float suctionZoneRadius = 150f;

    [Header("Bottom Visibility Overlay")]
    [SerializeField, Range(0f, 1f)] private float overlayAlpha = 0.64f;
    [SerializeField] private float overlayHeight = 820f;
    [SerializeField] private float overlayBottomPadding = 0f;

    private readonly List<ItemController> activeItems = new List<ItemController>();
    private readonly StageManager stageManager = new StageManager();
    private readonly GaugeManager gaugeManager = new GaugeManager();
    private readonly ScoreManager scoreManager = new ScoreManager();
    private readonly ComboManager comboManager = new ComboManager();
    private readonly TimerManager timerManager = new TimerManager();
    private static Sprite roundedPanelSprite;

    private ItemController highlightedItem;
    private bool buttonFastForward;
    private bool keyboardFastForward;
    private bool isSuctionHeld;
    private bool isStageTransitioning;
    private bool isFastForwardEnabled;
    private bool isBombStunned;
    private bool lastHadTargetInRange;
    private bool resultStarted;
    private bool isGameplayRestarting;
    private GameState currentState = GameState.WaitingToStart;
    private int lastDisplayedSuctionLevel = -1;
    private int lastDisplayedTime = -1;
    private int lastDisplayedScore = -1;
    private int lastDisplayedCombo = -1;
    private int maxCombo;
    private int mistakeCount;
    private int bombHitCount;
    private int reachedLevel = 1;
    private PurifierStage reachedStage = PurifierStage.Home;
    private float suppressPointerSuckUntilRealtime;

    public int CurrentSuctionLevel => gaugeManager.SuctionLevel;
    public PurifierStage CurrentStage => stageManager.CurrentStage;
    public bool IsFastForwardActive => isFastForwardEnabled;
    public bool IsWaitingToStart => currentState == GameState.WaitingToStart;
    public bool IsCountdown => currentState == GameState.Countdown;
    public bool IsPlaying => currentState == GameState.Playing;
    public bool CanSpawnItems => currentState == GameState.Countdown || currentState == GameState.Playing;
    public bool CanTickTimer => currentState == GameState.Playing;
    public bool CanSuction => currentState == GameState.Playing;
    public bool CanFastForward => currentState == GameState.Playing;
    public bool IsTargetControlLocked => isStageTransitioning || IsTimeUp || !IsPlaying;
    public bool IsSuctionLocked => isStageTransitioning || isBombStunned || IsTimeUp || !CanSuction;
    public bool IsTimeUp => timerManager.IsFinished;
    private float GameplaySpeedMultiplier => GetStageMoveSpeedMultiplier(stageManager.CurrentStage) * (isFastForwardEnabled ? FastForwardTimeScale : NormalTimeScale);

    public void ConfigureAirPurifierSprites(Sprite normal, Sprite suction, Sprite fail)
    {
        airPurifierNormalSprite = normal;
        airPurifierSuctionSprite = suction;
        airPurifierFailSprite = fail;
    }

    public void ConfigureBackgroundSprites(Sprite homeBackground, Sprite streetBackground, Sprite cityBackground, Sprite spaceBackground)
    {
        homeStageBackgroundSprite = homeBackground;
        streetStageBackgroundSprite = streetBackground;
        cityStageBackgroundSprite = cityBackground;
        spaceStageBackgroundSprite = spaceBackground;
    }

    public void ConfigureBottomVisibilityOverlay(Sprite overlaySprite)
    {
        bottomVisibilityOverlaySprite = overlaySprite;
    }

    public void ConfigureDataAssets(TextAsset itemMaster, ItemSpriteDatabase spriteDatabase, SfxDatabase sfxDatabaseAsset = null)
    {
        itemMasterCsv = itemMaster;
        itemSpriteDatabase = spriteDatabase;
        sfxDatabase = sfxDatabaseAsset;
        ItemDatabase.SetSpriteDatabase(itemSpriteDatabase);
    }

    public void ConfigureBgmAsset(AudioClip gameplayBgm)
    {
        gameplayBgmClip = gameplayBgm;
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
        ItemDatabase.SetSpriteDatabase(itemSpriteDatabase);
        EnsureEventSystem();
        EnsureRuntimeView();
        EnsureFullscreenRuntimeOverlays();
        ApplyDefaultFontToCanvas();
        EnsureComponents();
    }

    private void Start()
    {
        stageManager.Initialize(ItemDatabase.LoadDefault(itemMasterCsv, itemSpriteDatabase));
        scoreManager.Reset();
        comboManager.Reset();
        gaugeManager.Reset();
        timerManager.Reset(InitialTime);
        stageManager.ApplyLevel(gaugeManager.SuctionLevel);
        ApplyStageVisuals(false);
        maxCombo = 0;
        mistakeCount = 0;
        bombHitCount = 0;
        reachedLevel = gaugeManager.SuctionLevel;
        reachedStage = stageManager.CurrentStage;
        resultStarted = false;
        currentState = GameState.WaitingToStart;
        ShowStartPrompt();
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

        if (IsWaitingToStart)
        {
            HandleStartPromptInput();
            UpdateUi();
            return;
        }

        if (CanTickTimer)
        {
            timerManager.Tick(Time.unscaledDeltaTime);
            if (IsTimeUp && currentState == GameState.Playing)
            {
                BeginGameOver();
                UpdateUi();
                return;
            }
        }

        if (IsTimeUp && isSuctionHeld)
        {
            EndSuctionHold();
        }

        if (CanSpawnItems)
        {
            itemSpawner.Tick(Time.deltaTime);
        }

        UpdateCandidateHighlight();
        if (IsPlaying)
        {
            HandleHeldSuction();
            HandleInput();
        }
        UpdateUi();

        if (IsTimeUp && currentState == GameState.Playing)
        {
            BeginGameOver();
        }
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

    public static float GetStageMoveSpeedMultiplier(PurifierStage stage)
    {
        switch (stage)
        {
            case PurifierStage.Street:
                return 1.2f * 1.3f;
            case PurifierStage.City:
                return 1.4f * 1.3f;
            case PurifierStage.Space:
                return 1.6f * 1.3f;
            case PurifierStage.Home:
            default:
                return 1.0f * 1.3f;
        }
    }

    public static float GetStageSpawnIntervalMultiplier(PurifierStage stage)
    {
        switch (stage)
        {
            case PurifierStage.Street:
                return 1.2f;
            case PurifierStage.City:
                return 1.4f;
            case PurifierStage.Space:
                return 2.2f;
            case PurifierStage.Home:
            default:
                return 1.0f;
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
        if (IsSuctionLocked)
        {
            return;
        }

        var candidate = GetBestCandidate();
        if (candidate == null)
        {
            resultText.text = "空振り";
            return;
        }

        resultText.text = candidate.Data.IsBomb ? "危険!" : candidate.Data.RequiredLevel <= gaugeManager.SuctionLevel ? "吸引!" : "重すぎる!";
        suctionManager.TrySuck(candidate);
    }

    public void BeginSuctionHold()
    {
        if (IsSuctionLocked)
        {
            return;
        }

        isSuctionHeld = true;
        suctionZoneVisual?.SetSucking();
        airPurifier.StartSuctionHold();
    }

    public void EndSuctionHold()
    {
        isSuctionHeld = false;

        if (highlightedItem != null)
        {
            suctionZoneVisual?.SetTargetInRange();
        }
        else
        {
            suctionZoneVisual?.SetIdle();
        }

        airPurifier.StopSuctionHold();
    }

    public void ResolveSuction(ItemController item, bool success)
    {
        activeItems.Remove(item);

        if (!success)
        {
            suctionZoneVisual?.SetFailFlash();
            comboManager.Reset();
            mistakeCount += 1;
            if (currentState != GameState.GameOver)
            {
                timerManager.ApplyPenalty(PenaltySeconds);
                resultText.text = $"MISS -{PenaltySeconds:0}s";
            }
        }

        if (item != null)
        {
            item.KillTweens();
            Destroy(item.gameObject);
        }

        RestoreAirPurifierState();
        UpdateUi();
    }

    public void ApplySuccessReward(ItemController item)
    {
        if (currentState == GameState.GameOver || item == null || item.Data == null || item.Data.IsBomb || !item.TryMarkRewardApplied())
        {
            return;
        }

        var combo = comboManager.AddCombo();
        maxCombo = Mathf.Max(maxCombo, combo);
        var gainedScore = scoreManager.AddSuccessScore(item.Data.Score, combo);
        audioManager?.PlaySuccessSfx(item.Data.SuccessSfxKey);
        ShowScorePopup(gainedScore, combo, ScoreManager.GetComboMultiplier(combo), item.RectTransform.anchoredPosition);
        var previousLevel = gaugeManager.SuctionLevel;
        var previousStage = stageManager.CurrentStage;
        var leveledUp = gaugeManager.AddGauge(item.Data.GaugeGain);
        reachedLevel = Mathf.Max(reachedLevel, gaugeManager.SuctionLevel);
        reachedStage = StageManager.GetStageForLevel(reachedLevel);
        if (leveledUp)
        {
            var nextStage = StageManager.GetStageForLevel(gaugeManager.SuctionLevel);
            var stageChanged = previousStage != nextStage;
            if (stageChanged)
            {
                resultText.text = "ステージアップ!";
                StartCoroutine(PlayStageTransition(previousStage, nextStage, previousLevel, gaugeManager.SuctionLevel));
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

        UpdateUi();
    }

    private void ShowScorePopup(int score, int combo, float comboMultiplier, Vector2 anchoredPosition)
    {
        if (scorePopupLayer == null)
        {
            return;
        }

        var popupRect = CreateRect("ScorePopup", scorePopupLayer, anchoredPosition, new Vector2(320f, 128f));
        var canvasGroup = popupRect.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var popup = popupRect.gameObject.AddComponent<FloatingScoreText>();
        popup.Play(score, combo, comboMultiplier, anchoredPosition, GetBuiltinFont());
    }

    public void ResolveBomb(ItemController item)
    {
        activeItems.Remove(item);
        comboManager.Reset();
        bombHitCount += 1;
        resultText.text = "BOMB!";

        if (item != null)
        {
            item.KillTweens();
            Destroy(item.gameObject);
        }

        StartCoroutine(PlayBombPenalty());
        UpdateUi();
    }

    public void PlayWrongSfx()
    {
        audioManager?.PlayWrongSfx();
    }

    public void PlayBombSfx()
    {
        audioManager?.PlayBombSfx();
    }

    public void BeginBombLock()
    {
        if (isBombStunned)
        {
            return;
        }

        isBombStunned = true;
        buttonFastForward = false;
        keyboardFastForward = false;
        SetFastForward(false);
        EndSuctionHold();
        resultText.text = "危険!";
    }

    public void PlayBombExplosionFeedback(Vector2 explosionPosition)
    {
        resultText.text = "BOOM!";
        airPurifier.PlayFailAnimation();
        PlayBombExplosionEffect(explosionPosition);
        var root = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (root == null)
        {
            return;
        }

        root.DOKill();
        root.anchoredPosition = Vector2.zero;
        root.DOShakeAnchorPos(0.42f, new Vector2(34f, 28f), 24, 90f)
            .OnComplete(() => root.anchoredPosition = Vector2.zero);
    }

    private void PlayBombExplosionEffect(Vector2 explosionPosition)
    {
        if (bombExplosionImage == null)
        {
            return;
        }

        var rect = bombExplosionImage.rectTransform;
        bombExplosionImage.DOKill();
        rect.DOKill();

        rect.anchoredPosition = explosionPosition;
        rect.localScale = Vector3.one * 0.6f;
        bombExplosionImage.color = bombExplosionSprite != null
            ? Color.white
            : new Color(1f, 0.42f, 0.08f, 0.92f);
        bombExplosionImage.gameObject.SetActive(true);

        var sequence = DOTween.Sequence();
        sequence.Append(rect.DOScale(1.2f, 0.12f).SetEase(Ease.OutBack));
        sequence.Append(rect.DOScale(1.4f, 0.18f).SetEase(Ease.OutQuad));
        sequence.Join(bombExplosionImage.DOFade(0f, 0.18f).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            bombExplosionImage.gameObject.SetActive(false);
            rect.localScale = Vector3.one;
        });
    }

    public void SetFastForward(bool enabled)
    {
        if ((isStageTransitioning || isBombStunned || !CanFastForward) && enabled)
        {
            return;
        }

        isFastForwardEnabled = enabled;
        ApplyFastForwardToActiveItems();
    }

    public void SetFastForwardButtonHeld(bool held)
    {
        buttonFastForward = held && CanFastForward;
        RefreshFastForwardState();
    }

    public void SuppressPointerSuckInput(float seconds = 0.2f)
    {
        suppressPointerSuckUntilRealtime = Mathf.Max(suppressPointerSuckUntilRealtime, Time.realtimeSinceStartup + seconds);
    }

    private void ShowStartPrompt()
    {
        if (startOverlayDimPanel != null)
        {
            startOverlayDimPanel.gameObject.SetActive(true);
            startOverlayDimPanel.color = new Color(0f, 0f, 0f, 0.45f);
        }

        if (startPromptText != null)
        {
            startPromptText.gameObject.SetActive(true);
            startPromptText.text = "TAPで清浄スタート！";
            startPromptText.rectTransform.DOKill();
            startPromptText.rectTransform.localScale = Vector3.one;
            startPromptText.rectTransform.DOScale(1.06f, 0.65f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void HandleStartPromptInput()
    {
        if (WasStartPressed())
        {
            StartCoroutine(PlayStartCountdown());
        }
    }

    private IEnumerator PlayStartCountdown()
    {
        if (currentState != GameState.WaitingToStart)
        {
            yield break;
        }

        currentState = GameState.Countdown;
        buttonFastForward = false;
        keyboardFastForward = false;
        SetFastForward(false);
        EndSuctionHold();
        itemSpawner?.BeginCountdownSpawn();

        if (startPromptText != null)
        {
            startPromptText.rectTransform.DOKill();
            startPromptText.gameObject.SetActive(false);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            yield return PlayCountdownText("3", 1.0f);
            yield return PlayCountdownText("2", 1.0f);
            yield return PlayCountdownText("1", 1.0f);

            currentState = GameState.Playing;
            resultText.text = "清浄スタート！";
            audioManager?.PlayGameplayBgm();
            UpdateUi();
            if (startOverlayDimPanel != null)
            {
                startOverlayDimPanel.DOKill();
                startOverlayDimPanel.DOFade(0f, 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(() => startOverlayDimPanel.gameObject.SetActive(false));
            }

            yield return PlayCountdownText("清浄スタート！", 0.7f);
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(2.5f);
            currentState = GameState.Playing;
            resultText.text = "清浄スタート！";
            audioManager?.PlayGameplayBgm();
            UpdateUi();
        }

        if (startOverlayDimPanel != null && currentState != GameState.Playing)
        {
            startOverlayDimPanel.gameObject.SetActive(false);
        }

        UpdateUi();
    }

    private IEnumerator PlayCountdownText(string text, float duration)
    {
        countdownText.text = text;
        countdownText.DOKill();
        countdownText.rectTransform.DOKill();
        countdownText.rectTransform.localScale = Vector3.one * 0.6f;

        var color = countdownText.color;
        color.a = 1f;
        countdownText.color = color;

        countdownText.rectTransform.DOScale(1.22f, 0.16f).SetEase(Ease.OutBack)
            .OnComplete(() => countdownText.rectTransform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
        countdownText.DOFade(0f, Mathf.Min(0.22f, duration * 0.45f))
            .SetDelay(Mathf.Max(0f, duration - 0.22f))
            .SetEase(Ease.InQuad);

        yield return new WaitForSecondsRealtime(duration);
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

        if (audioManager == null)
        {
            audioManager = GetComponent<AudioManager>();
        }

        if (resultController == null)
        {
            resultController = GetComponent<ResultController>();
        }

        if (audioManager == null)
        {
            audioManager = gameObject.AddComponent<AudioManager>();
        }

        if (resultController == null)
        {
            resultController = gameObject.AddComponent<ResultController>();
        }

        itemSpawner.Configure(this, itemLayer, itemTemplate);
        suctionManager.Configure(this, airPurifier);
        audioManager.Configure(sfxDatabase, gameplayBgmClip);
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
        backgroundController = CreateBackgroundController(root, homeStageBackgroundSprite, streetStageBackgroundSprite, cityStageBackgroundSprite, spaceStageBackgroundSprite);
        CreateBottomVisibilityOverlay(root, bottomVisibilityOverlaySprite, overlayAlpha, overlayHeight, overlayBottomPadding);
        CreateTopVisibilityOverlay(root, bottomVisibilityOverlaySprite, overlayAlpha, overlayHeight, overlayBottomPadding);
        CreatePanel("Play_Lane", root, new Vector2(0f, 0f), new Vector2(1080f, 520f), new Color(1f, 0.84f, 0.42f, 0f), false);

        itemLayer = CreateRect("ItemLayer", root, Vector2.zero, new Vector2(1080f, 1920f));
        itemTemplate = CreateItemTemplate(itemLayer);
        itemTemplate.gameObject.SetActive(false);

        airPurifier = CreateAirPurifier(root, airPurifierNormalSprite, airPurifierSuctionSprite, airPurifierFailSprite);

        suctionZone = CreateRect("SuctionZoneRoot", root, new Vector2(0f, 30f), new Vector2(suctionZoneRadius * 2f, suctionZoneRadius * 2f));
        suctionZoneVisual = CreateSuctionZoneVisual(suctionZone, suctionZoneRadius);

        targetMarkerController = CreateTargetMarkerInputArea("TargetMarker_InputArea", root, new Vector2(0f, 40f), new Vector2(1080f, 1120f), this, suctionZone);
        bombExplosionSprite = ItemDatabase.LoadSpriteOrNull("Effect_BombExplosion");
        bombExplosionImage = CreateBombExplosionImage(root, bombExplosionSprite);
        scorePopupLayer = CreateRect("ScorePopupLayer", root, Vector2.zero, new Vector2(1080f, 1920f));

        var hudPanelColor = new Color(0.42f, 0.78f, 1f, 0.38f);
        scoreText = CreateReadableText("CENTER_HUD_Text", root, GetCenterHudText(90, 0), new Vector2(5f, 700f), new Vector2(390f, 300f), 42, Color.white, TextAnchor.MiddleCenter, hudPanelColor);
        scoreText.lineSpacing = 0.58f;
        timeText = scoreText;
        comboText = CreateReadableText("COMBO_Text", root, GetComboHudText(0), new Vector2(360f, 780f), new Vector2(242f, 139f), 33, Color.white, TextAnchor.MiddleCenter, hudPanelColor);
        var levelPanel = CreatePanel("SuctionLevel_Panel", root, new Vector2(-335f, 700f), new Vector2(190f, 390f), Color.white, false);
        ApplyRoundedCorners(levelPanel);
        var levelPanelOutline = levelPanel.gameObject.AddComponent<Outline>();
        levelPanelOutline.effectColor = new Color(1f, 1f, 1f, 0.95f);
        levelPanelOutline.effectDistance = new Vector2(8f, -8f);
        levelPanelFill = CreatePanel("SuctionLevel_PanelFill", levelPanel.rectTransform, Vector2.zero, new Vector2(172f, 372f), LevelColorUtility.GetLevelColor(gaugeManager.SuctionLevel), false);
        ApplyRoundedCorners(levelPanelFill);
        levelText = CreateText("SuctionLevel_Label", levelPanel.rectTransform, "吸引Lv", new Vector2(0f, 135f), new Vector2(160f, 60f), 28, new Color(0.12f, 0.18f, 0.28f), TextAnchor.MiddleCenter);
        levelNumberText = CreateText("SuctionLevel_Number", levelPanel.rectTransform, "1", new Vector2(0f, -30f), new Vector2(180f, 260f), 150, new Color(0.10f, 0.16f, 0.28f), TextAnchor.MiddleCenter);
        stageText = CreateReadableText("Stage_Text", root, "家ステージ", new Vector2(375f, 835f), new Vector2(300f, 56f), 28, Color.white, TextAnchor.MiddleCenter);
        resultText = CreateReadableText("Result_Text", root, "クリックで吸引", new Vector2(375f, 770f), new Vector2(300f, 56f), 26, new Color(0.92f, 1f, 1f), TextAnchor.MiddleCenter);
        SetReadableTextVisible(stageText, false);
        SetReadableTextVisible(resultText, false);

        var gaugeBack = CreatePanel("Gauge_Back", root, new Vector2(-465f, 700f), new Vector2(70f, 390f), Color.white, false);
        ApplyRoundedCorners(gaugeBack);
        var gaugeBackFill = CreatePanel("Gauge_BackFill", gaugeBack.rectTransform, Vector2.zero, new Vector2(52f, 372f), new Color(0.10f, 0.20f, 0.30f, 0.35f), false);
        ApplyRoundedCorners(gaugeBackFill);
        gaugeFill = CreatePanel("Gauge_Fill", gaugeBack.rectTransform, new Vector2(0f, -176f), new Vector2(42f, 0f), new Color(0.15f, 0.76f, 1f), false);
        ApplyRoundedCorners(gaugeFill);
        gaugeFill.rectTransform.pivot = new Vector2(0.5f, 0f);

        fastForwardButton = CreateButton("FastForward_Button", root, "x2\n早送り", new Vector2(-260f, -650f), new Vector2(360f, 160f), new Color(0.26f, 0.62f, 1f));
        var fastButton = fastForwardButton.gameObject.AddComponent<FastForwardButton>();
        fastButton.Configure(this);
        gameplayRestartButton = CreateButton("GameplayRestartButton", root, "再", new Vector2(366f, 635f), new Vector2(88f, 88f), new Color(1f, 0.58f, 0.18f));
        gameplayRestartButton.onClick.AddListener(RestartGameplayScene);
        BringReadableTextToFront(scoreText);
        BringReadableTextToFront(comboText);

        CreateStartOverlay(root);
        fadeController = CreateFadeController(root);
        resultController = CreateResultController(root);
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

        var hasTarget = highlightedItem != null;
        if (hasTarget == lastHadTargetInRange || suctionZoneVisual == null || isSuctionHeld)
        {
            lastHadTargetInRange = hasTarget;
            return;
        }

        if (hasTarget)
        {
            suctionZoneVisual.SetTargetInRange();
        }
        else
        {
            suctionZoneVisual.SetIdle();
        }

        lastHadTargetInRange = hasTarget;
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
        if (isBombStunned)
        {
            if (buttonFastForward || keyboardFastForward || isFastForwardEnabled)
            {
                buttonFastForward = false;
                keyboardFastForward = false;
                SetFastForward(false);
            }
            return;
        }

        if (WasSuckPressed())
        {
            TrySuck();
        }

        var fastHeld = IsFastForwardHeld();
        if (fastHeld != keyboardFastForward)
        {
            keyboardFastForward = fastHeld;
            RefreshFastForwardState();
        }
    }

    private void RefreshFastForwardState()
    {
        SetFastForward(buttonFastForward || keyboardFastForward);
    }

    private void HandleHeldSuction()
    {
        if (IsSuctionLocked || !isSuctionHeld)
        {
            return;
        }

        var candidate = GetBestCandidate();
        if (candidate == null)
        {
            return;
        }

        resultText.text = candidate.Data.IsBomb ? "危険!" : candidate.Data.RequiredLevel <= gaugeManager.SuctionLevel ? "吸引!" : "重すぎる!";
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
            suctionZoneVisual?.SetSucking();
            airPurifier.StartSuctionHold();
        }
        else
        {
            if (highlightedItem != null)
            {
                suctionZoneVisual?.SetTargetInRange();
            }
            else
            {
                suctionZoneVisual?.SetIdle();
            }

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

    private IEnumerator PlayStageTransition(PurifierStage previousStage, PurifierStage nextStage, int previousLevel, int nextLevel)
    {
        isStageTransitioning = true;
        keyboardFastForward = false;
        SetFastForward(false);
        EndSuctionHold();
        resultText.text = "ステージアップ!";

        if (fadeController != null)
        {
            yield return fadeController.FadeOut(0.3f);
            yield return new WaitForSecondsRealtime(0.12f);
        }

        ClearActiveItems();
        stageManager.ApplyLevel(gaugeManager.SuctionLevel);
        ApplyFastForwardToActiveItems();
        if (nextStage == PurifierStage.Street)
        {
            backgroundController.SetStreetBackground();
        }
        else if (nextStage == PurifierStage.City)
        {
            backgroundController.SetCityBackground();
        }
        else if (nextStage == PurifierStage.Space)
        {
            backgroundController.SetSpaceBackground();
        }
        resultText.text = $"{stageManager.CurrentStageName}!";
        UpdateUi();

        if (fadeController != null)
        {
            yield return fadeController.FadeIn(0.4f);
        }

        isStageTransitioning = false;
        resultText.text = $"{stageManager.CurrentStageName}!";
    }

    private void BeginGameOver()
    {
        if (resultStarted)
        {
            return;
        }

        resultStarted = true;
        currentState = GameState.GameOver;
        if (gameplayRestartButton != null)
        {
            gameplayRestartButton.gameObject.SetActive(false);
        }
        buttonFastForward = false;
        keyboardFastForward = false;
        SetFastForward(false);
        EndSuctionHold();
        resultText.text = "TIME UP";
        audioManager?.FadeOutGameplayBgm();
        suctionZoneVisual?.SetIdle();
        highlightedItem = null;
        lastHadTargetInRange = false;

        if (resultController != null)
        {
            resultController.ShowResult(scoreManager.Score, reachedLevel, reachedStage, maxCombo, mistakeCount, bombHitCount);
        }
    }

    private void RestartGameplayScene()
    {
        if (isGameplayRestarting)
        {
            return;
        }

        isGameplayRestarting = true;
        if (gameplayRestartButton != null)
        {
            gameplayRestartButton.interactable = false;
        }

        Time.timeScale = 1f;
        audioManager?.StopGameplayBgm();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator PlayBombPenalty()
    {
        isBombStunned = true;
        keyboardFastForward = false;
        SetFastForward(false);
        EndSuctionHold();
        suctionZoneVisual?.SetFailFlash();
        resultText.text = "吸引停止 1秒";

        yield return new WaitForSecondsRealtime(1f);

        isBombStunned = false;
        resultText.text = "復帰!";
        RestoreAirPurifierState();
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
        lastHadTargetInRange = false;
        suctionZoneVisual?.SetIdle();
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

    private bool WasStartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Return) ||
               Input.GetKeyDown(KeyCode.KeypadEnter) ||
               Input.GetKeyDown(KeyCode.Space) ||
               Input.GetMouseButtonDown(0) ||
               Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
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
        return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }

    private void UpdateUi()
    {
        var displayedTime = Mathf.CeilToInt(timerManager.TimeLeft);
        if (displayedTime != lastDisplayedTime || scoreManager.Score != lastDisplayedScore)
        {
            SetTextIfChanged(scoreText, GetCenterHudText(displayedTime, scoreManager.Score));
            lastDisplayedTime = displayedTime;
            lastDisplayedScore = scoreManager.Score;
        }

        if (comboManager.Combo != lastDisplayedCombo)
        {
            comboText.color = Color.white;
            SetTextIfChanged(comboText, GetComboHudText(comboManager.Combo));
            lastDisplayedCombo = comboManager.Combo;
        }

        SetTextIfChanged(levelText, "吸引Lv");
        if (levelNumberText != null && lastDisplayedSuctionLevel != gaugeManager.SuctionLevel)
        {
            var isMaxLevel = gaugeManager.SuctionLevel >= GaugeManager.MaxSuctionLevel;
            levelNumberText.text = isMaxLevel ? "MAX" : gaugeManager.SuctionLevel.ToString();
            levelNumberText.fontSize = isMaxLevel ? 82 : 150;
            UpdateLevelPanelColor();
            if (lastDisplayedSuctionLevel >= 0)
            {
                PlayLevelNumberBounce();
            }
            lastDisplayedSuctionLevel = gaugeManager.SuctionLevel;
        }
        gaugeFill.rectTransform.sizeDelta = new Vector2(42f, 352f * gaugeManager.GaugeRate);

        if (IsTimeUp)
        {
            SetTextIfChanged(resultText, "TIME UP");
        }
    }

    private static void SetTextIfChanged(Text target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }

    private static string GetCenterHudText(int time, int score)
    {
        return $"<color=#FFF03B><size=132>{time:00}</size></color>\n<color=#FFFFFF><size=34>清浄量</size></color>\n<size=54>{GetScoreHudText(score)}</size>";
    }

    private static string GetComboHudText(int combo)
    {
        return $"<color=#FFFFFF>COMBO</color>\n<color=#{GetColorHex(GetComboHudColor(combo))}><size=68>{combo}</size></color>";
    }

    private static string GetScoreHudText(int score)
    {
        var scoreText = $"{score.ToString("#,0", CultureInfo.InvariantCulture)}pt";
        if (score >= 500000)
        {
            return GetRainbowRichText(scoreText);
        }

        return $"<color=#{GetScoreHudColorHex(score)}>{scoreText}</color>";
    }

    private static string GetScoreHudColorHex(int score)
    {
        if (score >= 200000)
        {
            return "FF3030";
        }

        if (score >= 100000)
        {
            return "35E85A";
        }

        if (score >= 50000)
        {
            return "FFF03B";
        }

        if (score >= 10000)
        {
            return "4CC7FF";
        }

        return "FFFFFF";
    }

    private static string GetRainbowRichText(string value)
    {
        var colors = new[] { "FF3030", "FF9A20", "FFF03B", "35E85A", "35D7FF", "4B75FF", "D24BFF" };
        var builder = new System.Text.StringBuilder(value.Length * 24);
        for (var i = 0; i < value.Length; i++)
        {
            builder.Append("<color=#");
            builder.Append(colors[i % colors.Length]);
            builder.Append(">");
            builder.Append(value[i]);
            builder.Append("</color>");
        }

        return builder.ToString();
    }

    private static string GetColorHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    private static Color GetComboHudColor(int combo)
    {
        if (combo >= 100)
        {
            return new Color(1f, 0.12f, 0.92f);
        }

        if (combo >= 70)
        {
            return new Color(0.72f, 0.28f, 1f);
        }

        if (combo >= 50)
        {
            return new Color(1f, 0.14f, 0.12f);
        }

        if (combo >= 40)
        {
            return new Color(1f, 0.48f, 0.08f);
        }

        if (combo >= 30)
        {
            return new Color(0.22f, 0.86f, 0.30f);
        }

        if (combo >= 20)
        {
            return new Color(1f, 0.92f, 0.12f);
        }

        return new Color(0.18f, 0.56f, 1f);
    }

    private void UpdateLevelPanelColor()
    {
        if (levelPanelFill == null)
        {
            return;
        }

        levelPanelFill.color = LevelColorUtility.GetLevelColor(gaugeManager.SuctionLevel);
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

        if (stageManager.CurrentStage == PurifierStage.City)
        {
            backgroundController.SetCityBackground();
            return;
        }

        if (stageManager.CurrentStage == PurifierStage.Space)
        {
            backgroundController.SetSpaceBackground();
            return;
        }

        backgroundController.SetStreetBackground();
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

    private static Image CreateRoundedPanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, bool raycastTarget)
    {
        var image = CreatePanel(name, parent, anchoredPosition, size, color, raycastTarget);
        ApplyRoundedCorners(image);
        return image;
    }

    private static void ApplyRoundedCorners(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = GetRoundedPanelSprite();
        image.type = Image.Type.Sliced;
    }

    private static Sprite GetRoundedPanelSprite()
    {
        if (roundedPanelSprite != null)
        {
            return roundedPanelSprite;
        }

        var texture = new Texture2D(RoundedPanelTextureSize, RoundedPanelTextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var radius = RoundedPanelRadius;
        var maxIndex = RoundedPanelTextureSize - 1;
        for (var y = 0; y < RoundedPanelTextureSize; y++)
        {
            for (var x = 0; x < RoundedPanelTextureSize; x++)
            {
                var nearestX = Mathf.Clamp(x, radius, maxIndex - radius);
                var nearestY = Mathf.Clamp(y, radius, maxIndex - radius);
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        roundedPanelSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, RoundedPanelTextureSize, RoundedPanelTextureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        roundedPanelSprite.name = "GeneratedRoundedPanel";
        return roundedPanelSprite;
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
        label.supportRichText = true;
        label.raycastTarget = false;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3f, -3f);
        return label;
    }

    private static Text CreateReadableText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAnchor alignment)
    {
        return CreateReadableText(name, parent, text, anchoredPosition, size, fontSize, color, alignment, new Color(0.04f, 0.08f, 0.16f, 0.62f));
    }

    private static Text CreateReadableText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAnchor alignment, Color panelColor)
    {
        var panel = CreateReadablePanel(name + "_Panel", parent, anchoredPosition, size, panelColor);

        var label = CreateText(name, panel.rectTransform, text, Vector2.zero, size, fontSize, color, alignment);
        var outline = label.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(4f, -4f);
        }

        var shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(5f, -5f);
        return label;
    }

    private static Image CreateReadablePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateReadablePanel(name, parent, anchoredPosition, size, new Color(0.04f, 0.08f, 0.16f, 0.62f));
    }

    private static Image CreateReadablePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color panelColor)
    {
        var panel = CreateRoundedPanel(name, parent, anchoredPosition, size, panelColor, false);
        var panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(1f, 1f, 1f, 0.88f);
        panelOutline.effectDistance = new Vector2(5f, -5f);
        return panel;
    }

    private static void SetReadableTextVisible(Text label, bool visible)
    {
        if (label == null)
        {
            return;
        }

        if (label.transform.parent != null)
        {
            label.transform.parent.gameObject.SetActive(visible);
            return;
        }

        label.gameObject.SetActive(visible);
    }

    private static void BringReadableTextToFront(Text label)
    {
        if (label == null)
        {
            return;
        }

        if (label.transform.parent != null && label.transform.parent.GetComponent<Image>() != null)
        {
            label.transform.parent.SetAsLastSibling();
            return;
        }

        label.transform.SetAsLastSibling();
    }

    private static Button CreateButton(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var image = CreateRoundedPanel(name, parent, anchoredPosition, size, Color.white, true);
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(6f, -6f);
        var inner = CreateRoundedPanel("Fill", image.rectTransform, Vector2.zero, size - new Vector2(14f, 14f), color, false);
        var label = CreateText("Label", image.rectTransform, text, Vector2.zero, size, 38, Color.white, TextAnchor.MiddleCenter);
        var labelOutline = label.GetComponent<Outline>();
        if (labelOutline != null)
        {
            labelOutline.effectColor = new Color(0f, 0.12f, 0.30f, 0.95f);
            labelOutline.effectDistance = new Vector2(4f, -4f);
        }
        var labelShadow = label.gameObject.AddComponent<Shadow>();
        labelShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        labelShadow.effectDistance = new Vector2(4f, -4f);
        label.transform.SetAsLastSibling();
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = inner;
        return button;
    }

    private static SuctionZoneVisualController CreateSuctionZoneVisual(RectTransform parent, float radius)
    {
        var images = new List<Image>();

        images.AddRange(CreateRingLikeBox("Crosshair_RingOuter", parent, Vector2.zero, new Vector2(radius * 1.55f, radius * 1.55f), 16f));
        images.AddRange(CreateRingLikeBox("Crosshair_RingInner", parent, Vector2.zero, new Vector2(radius * 0.58f, radius * 0.58f), 12f));

        images.Add(CreateCrosshairLine("Crosshair_Top", parent, new Vector2(0f, radius * 0.62f), new Vector2(18f, 70f)));
        images.Add(CreateCrosshairLine("Crosshair_Bottom", parent, new Vector2(0f, -radius * 0.62f), new Vector2(18f, 70f)));
        images.Add(CreateCrosshairLine("Crosshair_Left", parent, new Vector2(-radius * 0.62f, 0f), new Vector2(70f, 18f)));
        images.Add(CreateCrosshairLine("Crosshair_Right", parent, new Vector2(radius * 0.62f, 0f), new Vector2(70f, 18f)));
        images.Add(CreateCrosshairLine("Crosshair_CenterDot", parent, Vector2.zero, new Vector2(28f, 28f)));

        CreateText("Crosshair_Label", parent, "吸引", new Vector2(0f, -radius * 0.92f), new Vector2(160f, 46f), 24, new Color(1f, 0.22f, 0.18f, 0.82f), TextAnchor.MiddleCenter);

        var controller = parent.gameObject.AddComponent<SuctionZoneVisualController>();
        controller.Configure(parent, images.ToArray());
        return controller;
    }

    private static List<Image> CreateRingLikeBox(string name, RectTransform parent, Vector2 center, Vector2 size, float thickness)
    {
        return new List<Image>
        {
            CreateCrosshairLine(name + "_Top", parent, center + new Vector2(0f, size.y * 0.5f), new Vector2(size.x, thickness)),
            CreateCrosshairLine(name + "_Bottom", parent, center + new Vector2(0f, -size.y * 0.5f), new Vector2(size.x, thickness)),
            CreateCrosshairLine(name + "_Left", parent, center + new Vector2(-size.x * 0.5f, 0f), new Vector2(thickness, size.y)),
            CreateCrosshairLine(name + "_Right", parent, center + new Vector2(size.x * 0.5f, 0f), new Vector2(thickness, size.y))
        };
    }

    private static Image CreateCrosshairLine(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
    {
        return CreatePanel(name, parent, anchoredPosition, size, new Color(1f, 0.16f, 0.16f, 0.72f), false);
    }

    private static BackgroundController CreateBackgroundController(RectTransform parent, Sprite homeSprite, Sprite streetSprite, Sprite citySprite, Sprite spaceSprite)
    {
        var root = CreateRect("BackgroundRoot", parent, Vector2.zero, new Vector2(1080f, 1920f));
        root.SetAsFirstSibling();

        var home = CreatePanel("HomeBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(1f, 0.92f, 0.74f), false);
        var street = CreatePanel("StreetBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0.70f, 0.88f, 1f), false);
        var city = CreatePanel("CityBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0.55f, 0.70f, 0.92f), false);
        var space = CreatePanel("SpaceBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0.10f, 0.08f, 0.22f), false);
        home.gameObject.AddComponent<AspectFillImage>();
        street.gameObject.AddComponent<AspectFillImage>();
        city.gameObject.AddComponent<AspectFillImage>();
        space.gameObject.AddComponent<AspectFillImage>();

        var controller = root.gameObject.AddComponent<BackgroundController>();
        controller.Configure(home, street, city, space, homeSprite, streetSprite, citySprite, spaceSprite);
        return controller;
    }

    private static FadeController CreateFadeController(RectTransform parent)
    {
        var overlay = CreatePanel("FadeOverlay", parent, Vector2.zero, new Vector2(1080f, 1920f), new Color(0f, 0f, 0f, 0f), true);
        StretchToParent(overlay.rectTransform);
        overlay.rectTransform.SetAsLastSibling();
        var controller = overlay.gameObject.AddComponent<FadeController>();
        controller.Configure(overlay);
        return controller;
    }

    private static ResultController CreateResultController(RectTransform parent)
    {
        var root = CreateRect("ResultRoot", parent, Vector2.zero, new Vector2(1080f, 1920f));
        StretchToParent(root);
        root.SetAsLastSibling();
        var controller = root.gameObject.AddComponent<ResultController>();
        controller.Configure(root, GetBuiltinFont());
        root.gameObject.SetActive(false);
        return controller;
    }

    private static Image CreateBottomVisibilityOverlay(RectTransform parent, Sprite sprite, float alpha, float height, float bottomPadding)
    {
        var image = CreatePanel("BottomVisibilityOverlay", parent, Vector2.zero, new Vector2(1080f, height), Color.white, false);
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomPadding);
        rect.sizeDelta = new Vector2(0f, height);
        rect.offsetMin = new Vector2(0f, bottomPadding);
        rect.offsetMax = new Vector2(0f, bottomPadding + height);

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.color = GetVisibilityOverlayColor(alpha);
        return image;
    }

    private static Image CreateTopVisibilityOverlay(RectTransform parent, Sprite sprite, float alpha, float height, float topPadding)
    {
        var image = CreatePanel("TopVisibilityOverlay", parent, Vector2.zero, new Vector2(1080f, height), Color.white, false);
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -topPadding - height * 0.5f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.localRotation = Quaternion.Euler(0f, 0f, 180f);

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.color = GetVisibilityOverlayColor(alpha);
        return image;
    }

    private static Color GetVisibilityOverlayColor(float alpha)
    {
        return new Color(0.38f, 0.76f, 1f, Mathf.Clamp01(alpha * 0.55f));
    }

    private void CreateStartOverlay(RectTransform parent)
    {
        startOverlayDimPanel = CreatePanel("StartOverlayDimPanel", parent, Vector2.zero, new Vector2(1080f, 1920f), new Color(0f, 0f, 0f, 0.45f), true);
        StretchToParent(startOverlayDimPanel.rectTransform);

        startPromptText = CreateText("StartPromptText", parent, "TAPで清浄スタート！", Vector2.zero, new Vector2(930f, 180f), 76, new Color(0.18f, 0.62f, 1f), TextAnchor.MiddleCenter);
        var promptOutline = startPromptText.GetComponent<Outline>();
        if (promptOutline != null)
        {
            promptOutline.effectColor = new Color(0.04f, 0.10f, 0.22f, 0.98f);
            promptOutline.effectDistance = new Vector2(7f, -7f);
        }

        countdownText = CreateText("CountdownText", parent, "3", Vector2.zero, new Vector2(900f, 220f), 118, new Color(1f, 0.96f, 0.24f), TextAnchor.MiddleCenter);
        var countdownOutline = countdownText.GetComponent<Outline>();
        if (countdownOutline != null)
        {
            countdownOutline.effectColor = new Color(0.04f, 0.10f, 0.22f, 0.98f);
            countdownOutline.effectDistance = new Vector2(8f, -8f);
        }

        countdownText.gameObject.SetActive(false);
    }

    private static Image CreateBombExplosionImage(RectTransform parent, Sprite sprite)
    {
        var image = CreatePanel("BombExplosion_Effect", parent, Vector2.zero, new Vector2(420f, 420f), new Color(1f, 0.42f, 0.08f, 0f), false);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
    }

    private static SuctionHoldArea CreateSuctionHoldArea(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, GameManager manager)
    {
        var image = CreatePanel(name, parent, anchoredPosition, size, new Color(1f, 1f, 1f, 0.01f), true);
        var holdArea = image.gameObject.AddComponent<SuctionHoldArea>();
        holdArea.Configure(manager);
        return holdArea;
    }

    private static TargetMarkerController CreateTargetMarkerInputArea(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, GameManager manager, RectTransform marker)
    {
        var image = CreatePanel(name, parent, anchoredPosition, size, new Color(1f, 1f, 1f, 0f), true);
        var controller = image.gameObject.AddComponent<TargetMarkerController>();
        controller.Configure(manager, marker, parent);
        return controller;
    }

    private static ItemController CreateItemTemplate(RectTransform parent)
    {
        var border = CreatePanel("Item_Template", parent, Vector2.zero, new Vector2(150f, 92f), Color.white, false);
        var outline = border.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(5f, -5f);
        var fill = CreatePanel("Fill", border.rectTransform, Vector2.zero, new Vector2(136f, 78f), new Color(1f, 0.7f, 0.3f), false);
        var label = CreateText("Label", border.rectTransform, "Item", Vector2.zero, new Vector2(140f, 90f), 28, new Color(0.14f, 0.18f, 0.26f), TextAnchor.MiddleCenter);
        var badge = CreatePanel("LevelBadge", border.rectTransform, new Vector2(58f, 34f), new Vector2(104f, 50f), new Color(0.16f, 0.72f, 0.30f, 0.96f), false);
        var badgeOutline = badge.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = Color.white;
        badgeOutline.effectDistance = new Vector2(7f, -7f);
        var badgeShadow = badge.gameObject.AddComponent<Shadow>();
        badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        badgeShadow.effectDistance = new Vector2(5f, -5f);
        var badgeText = CreateText("LevelBadge_Text", badge.rectTransform, "★ Lv1", Vector2.zero, new Vector2(112f, 52f), 28, Color.white, TextAnchor.MiddleCenter);
        var badgeTextOutline = badgeText.gameObject.GetComponent<Outline>();
        if (badgeTextOutline != null)
        {
            badgeTextOutline.effectColor = new Color(0.04f, 0.04f, 0.06f, 0.92f);
            badgeTextOutline.effectDistance = new Vector2(3f, -3f);
        }
        label.transform.SetAsLastSibling();
        badge.transform.SetAsLastSibling();
        var item = border.gameObject.AddComponent<ItemController>();
        item.Configure(border.rectTransform, border, fill, label, badge, badgeText, outline);
        return item;
    }

    private static AirPurifierController CreateAirPurifier(RectTransform parent, Sprite normalSprite, Sprite suctionSprite, Sprite failSprite)
    {
        var rootImage = CreatePanel("AirPurifier", parent, new Vector2(245f, -610f), new Vector2(594f, 594f), Color.white, false);
        rootImage.sprite = normalSprite;
        rootImage.preserveAspect = true;

        if (normalSprite == null)
        {
            var body = CreatePanel("Fallback_Body", rootImage.rectTransform, new Vector2(0f, -10f), new Vector2(235f, 225f), new Color(0.80f, 0.94f, 1f), false);
            CreateText("Fallback_Face", body.rectTransform, "空気\n清浄機", Vector2.zero, new Vector2(210f, 170f), 34, new Color(0.12f, 0.20f, 0.32f), TextAnchor.MiddleCenter);
            CreatePanel("Fallback_Intake", body.rectTransform, new Vector2(0f, 78f), new Vector2(160f, 28f), new Color(0.32f, 0.72f, 1f), false);
        }

        var suctionPoint = CreateRect("SuctionPoint", rootImage.rectTransform, new Vector2(0f, 277.2f), new Vector2(10f, 10f));
        var controller = rootImage.gameObject.AddComponent<AirPurifierController>();
        controller.Configure(rootImage.rectTransform, suctionPoint, rootImage, normalSprite, suctionSprite, failSprite);
        return controller;
    }

    private static Font GetBuiltinFont()
    {
        return UiFontUtility.GetDefaultFont();
    }

    private void EnsureFullscreenRuntimeOverlays()
    {
        StretchNamedRect("FadeOverlay");
        SetNamedImageAlpha("Play_Lane", 0f);
        SetNamedImageAlpha("TargetMarker_InputArea", 0f);
    }

    private void ApplyDefaultFontToCanvas()
    {
        if (canvas == null)
        {
            return;
        }

        var font = UiFontUtility.GetDefaultFont();
        if (font == null)
        {
            return;
        }

        var texts = canvas.GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            text.font = font;
        }
    }

    private void StretchNamedRect(string objectName)
    {
        if (canvas == null)
        {
            return;
        }

        var children = canvas.GetComponentsInChildren<RectTransform>(true);
        foreach (var child in children)
        {
            if (child.name == objectName)
            {
                StretchToParent(child);
            }
        }
    }

    private void SetNamedImageAlpha(string objectName, float alpha)
    {
        if (canvas == null)
        {
            return;
        }

        var images = canvas.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image.name != objectName)
            {
                continue;
            }

            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
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
}
