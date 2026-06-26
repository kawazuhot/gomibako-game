using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
    private const int RoundedPanelTextureSize = 48;
    private const int RoundedPanelRadius = 12;

    private class ResultBonus
    {
        public string DisplayName;
        public int Score;
        public bool Achieved;

        public ResultBonus(string displayName, int score, bool achieved)
        {
            DisplayName = displayName;
            Score = score;
            Achieved = achieved;
        }
    }

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] private float resultImageFadeDuration = 1.2f;
    [SerializeField] private float resultImageHoldDuration = 1.0f;
    [SerializeField] private float resultDarkFadeDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float resultDarkOverlayAlpha = 0.45f;
    [SerializeField] private float scoreItemInterval = 0.3f;
    [SerializeField] private float rankRevealDelay = 0.6f;
    [SerializeField] private float rankRevealFadeDuration = 0.24f;
    [SerializeField] private float rankRevealPopDuration = 0.32f;
    [SerializeField] private float rankRevealSettleDuration = 0.16f;
    [SerializeField] private float rankRevealStartScale = 0.6f;
    [SerializeField] private float rankRevealPopScale = 1.35f;

    [Header("Rank Result Images")]
    [SerializeField] private Sprite resultRankD;
    [SerializeField] private Sprite resultRankCb;
    [SerializeField] private Sprite resultRankA;
    [SerializeField] private Sprite resultRankS;
    [SerializeField] private Sprite resultRankSs;

    [Header("Rank Thresholds")]
    [SerializeField] private int rankCThreshold = 50000;
    [SerializeField] private int rankBThreshold = 100000;
    [SerializeField] private int rankAThreshold = 250000;
    [SerializeField] private int rankSThreshold = 400000;
    [SerializeField] private int rankSSThreshold = 600000;

    [Header("Scene Transition")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private float sceneTransitionFadeDuration = 0.35f;

    private RectTransform root;
    private Image fadePanel;
    private Image resultRankBackground;
    private Image resultDarkOverlay;
    private Image backgroundPanel;
    private Text titleText;
    private Text normalScoreText;
    private RectTransform bonusContainer;
    private Text totalScoreText;
    private Text rankText;
    private Font uiFont;
    private readonly List<Text> bonusRows = new List<Text>();
    private static Sprite roundedPanelSprite;
    private Coroutine resultRoutine;
    private bool isTransitioning;
    private bool isResultSequencePlaying;
    private bool hasPlayedResultScoreStartSe;
    private bool hasPlayedResultRankRevealSe;

    public void Configure(RectTransform resultRoot, Font font)
    {
        root = resultRoot;
        uiFont = font != null ? font : UiFontUtility.GetDefaultFont();
        BuildUi();
    }

    public void ConfigureRankSprites(Sprite rankD, Sprite rankCb, Sprite rankA, Sprite rankS, Sprite rankSs)
    {
        resultRankD = rankD;
        resultRankCb = rankCb;
        resultRankA = rankA;
        resultRankS = rankS;
        resultRankSs = rankSs;
    }

    public void ShowResult(int normalScore, int reachedLevel, PurifierStage reachedStage, int maxCombo, int mistakeCount, int bombHitCount)
    {
        Debug.Log($"[Lifecycle] ResultShown controller=ResultController score={normalScore} reachedLevel={reachedLevel} reachedStage={reachedStage} maxCombo={maxCombo} mistakes={mistakeCount} bombs={bombHitCount} t={Time.realtimeSinceStartup:0.00}");

        if (isResultSequencePlaying)
        {
            return;
        }

        if (root == null)
        {
            root = GetComponent<RectTransform>();
        }

        if (root == null)
        {
            return;
        }

        root.gameObject.SetActive(true);
        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
        }

        resultRoutine = StartCoroutine(PlayResult(normalScore, reachedLevel, reachedStage, maxCombo, mistakeCount, bombHitCount));
    }

    private void OnDisable()
    {
        StopResultRoutine();
    }

    private void OnDestroy()
    {
        StopResultRoutine();
    }

    private void StopResultRoutine()
    {
        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }

        isResultSequencePlaying = false;
        fadePanel?.DOKill();
        resultRankBackground?.DOKill();
        resultDarkOverlay?.DOKill();
        backgroundPanel?.DOKill();
    }

    private void BuildUi()
    {
        if (root == null || fadePanel != null)
        {
            return;
        }

        resultRankBackground = CreatePanel("ResultRankBackground", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(1f, 1f, 1f, 0f), false);
        StretchToParent(resultRankBackground.rectTransform);
        resultRankBackground.preserveAspect = true;
        resultRankBackground.gameObject.AddComponent<AspectFillImage>();
        resultRankBackground.gameObject.SetActive(false);

        resultDarkOverlay = CreatePanel("ResultDarkOverlay", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0f, 0f, 0f, 0f), false);
        StretchToParent(resultDarkOverlay.rectTransform);
        resultDarkOverlay.gameObject.SetActive(false);

        backgroundPanel = CreatePanel("ResultScoreRoot", root, Vector2.zero, new Vector2(900f, 1160f), new Color(0f, 0f, 0f, 0f), false);
        backgroundPanel.gameObject.SetActive(false);

        titleText = CreateText("ResultTitleText", backgroundPanel.rectTransform, "清浄リザルト", new Vector2(0f, 470f), new Vector2(780f, 110f), 58, new Color(1f, 0.95f, 0.24f), TextAnchor.MiddleCenter);
        normalScoreText = CreateText("ResultNormalScoreText", backgroundPanel.rectTransform, string.Empty, new Vector2(0f, 335f), new Vector2(780f, 82f), 44, Color.white, TextAnchor.MiddleCenter);
        bonusContainer = CreateRect("ResultInfoContainer", backgroundPanel.rectTransform, new Vector2(0f, 95f), new Vector2(780f, 330f));
        totalScoreText = CreateText("ResultTotalScoreText", backgroundPanel.rectTransform, string.Empty, new Vector2(0f, -210f), new Vector2(780f, 92f), 50, new Color(0.54f, 0.94f, 1f), TextAnchor.MiddleCenter);
        rankText = CreateText("ResultRankText", backgroundPanel.rectTransform, string.Empty, new Vector2(0f, -340f), new Vector2(900f, 172f), 104, new Color(1f, 0.42f, 0.18f), TextAnchor.MiddleCenter);
        restartButton = CreateButton("ResultRestartButton", backgroundPanel.rectTransform, "リスタート", new Vector2(0f, -485f), new Vector2(520f, 86f), new Color(0.18f, 0.62f, 1f));
        backToTitleButton = CreateButton("ResultBackToTitleButton", backgroundPanel.rectTransform, "タイトルに戻る", new Vector2(0f, -590f), new Vector2(520f, 86f), new Color(0.20f, 0.78f, 0.48f));
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        backToTitleButton.onClick.AddListener(OnBackToTitleButtonClicked);
        SetResultButtonsVisible(false);

        fadePanel = CreatePanel("ResultFadePanel", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0f, 0f, 0f, 0f), true);
        StretchToParent(fadePanel.rectTransform);
        fadePanel.rectTransform.SetAsLastSibling();
        fadePanel.gameObject.SetActive(false);
    }

    private IEnumerator PlayResult(int normalScore, int reachedLevel, PurifierStage reachedStage, int maxCombo, int mistakeCount, int bombHitCount)
    {
        BuildUi();
        ResetViews();
        isResultSequencePlaying = true;

        var bonuses = BuildBonuses(reachedLevel, reachedStage, maxCombo, mistakeCount, bombHitCount);
        var bonusTotal = 0;
        for (var i = 0; i < bonuses.Count; i++)
        {
            if (bonuses[i].Achieved)
            {
                bonusTotal += bonuses[i].Score;
            }
        }

        var finalScore = normalScore + bonusTotal;
        var rank = GetRank(finalScore);
        SetRankBackground(rank);

        fadePanel.gameObject.SetActive(true);
        fadePanel.DOKill();
        fadePanel.color = new Color(0f, 0f, 0f, 0f);
        yield return TweenToYield(fadePanel.DOFade(1f, Mathf.Max(0.01f, fadeDuration)).SetEase(Ease.OutQuad).SetUpdate(true));

        resultRankBackground.gameObject.SetActive(true);
        resultRankBackground.color = Color.white;
        resultRankBackground.GetComponent<AspectFillImage>()?.Apply();
        yield return TweenToYield(fadePanel.DOFade(0f, Mathf.Max(0.01f, resultImageFadeDuration)).SetEase(Ease.InOutSine).SetUpdate(true));
        fadePanel.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, resultImageHoldDuration));

        resultDarkOverlay.gameObject.SetActive(true);
        resultDarkOverlay.DOKill();
        resultDarkOverlay.color = new Color(0f, 0f, 0f, 0f);
        yield return TweenToYield(resultDarkOverlay.DOFade(Mathf.Clamp01(resultDarkOverlayAlpha), Mathf.Max(0.01f, resultDarkFadeDuration)).SetEase(Ease.OutQuad).SetUpdate(true));

        backgroundPanel.gameObject.SetActive(true);
        PlayResultScoreStartSeOnce();

        normalScoreText.text = $"清浄量  {normalScore:N0}pt";
        yield return PlayTextIn(normalScoreText, scoreItemInterval);

        var infoRows = new[]
        {
            $"最大コンボ  {maxCombo:N0}",
            $"到達ステージ  {GetStageDisplayName(reachedStage)}",
            $"到達Lv  {reachedLevel}",
        };
        for (var i = 0; i < infoRows.Length; i++)
        {
            var row = CreateBonusRow(bonusRows.Count, infoRows[i]);
            bonusRows.Add(row);
            yield return PlayTextIn(row, scoreItemInterval);
        }

        totalScoreText.text = $"TOTAL  {finalScore:N0}";
        yield return PlayTextIn(totalScoreText, scoreItemInterval);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, rankRevealDelay));
        rankText.text = $"清浄RANK {rank}";
        yield return PlayRankReveal(rankText);

        ShowResultButtons();
        isResultSequencePlaying = false;
        resultRoutine = null;
    }

    private List<ResultBonus> BuildBonuses(int reachedLevel, PurifierStage reachedStage, int maxCombo, int mistakeCount, int bombHitCount)
    {
        var bonuses = new List<ResultBonus>();

        if (reachedLevel >= 12)
        {
            bonuses.Add(new ResultBonus("到達Lvボーナス", 50000, true));
        }
        else if (reachedLevel >= 10)
        {
            bonuses.Add(new ResultBonus("到達Lvボーナス", 30000, true));
        }
        else if (reachedLevel >= 7)
        {
            bonuses.Add(new ResultBonus("到達Lvボーナス", 15000, true));
        }
        else if (reachedLevel >= 4)
        {
            bonuses.Add(new ResultBonus("到達Lvボーナス", 5000, true));
        }

        if (maxCombo >= 100)
        {
            bonuses.Add(new ResultBonus("最大コンボボーナス", 30000, true));
        }
        else if (maxCombo >= 50)
        {
            bonuses.Add(new ResultBonus("最大コンボボーナス", 15000, true));
        }
        else if (maxCombo >= 20)
        {
            bonuses.Add(new ResultBonus("最大コンボボーナス", 5000, true));
        }

        switch (reachedStage)
        {
            case PurifierStage.Space:
                bonuses.Add(new ResultBonus("宇宙到達ボーナス", 30000, true));
                break;
            case PurifierStage.City:
                bonuses.Add(new ResultBonus("都市到達ボーナス", 15000, true));
                break;
            case PurifierStage.Street:
                bonuses.Add(new ResultBonus("街到達ボーナス", 5000, true));
                break;
        }

        var totalMistakes = mistakeCount + bombHitCount;
        if (totalMistakes == 0)
        {
            bonuses.Add(new ResultBonus("清浄精度ボーナス", 30000, true));
        }
        else if (totalMistakes <= 3)
        {
            bonuses.Add(new ResultBonus("清浄精度ボーナス", 10000, true));
        }

        return bonuses;
    }

    private string GetRank(int finalScore)
    {
        if (finalScore >= rankSSThreshold)
        {
            return "SS";
        }

        if (finalScore >= rankSThreshold)
        {
            return "S";
        }

        if (finalScore >= rankAThreshold)
        {
            return "A";
        }

        if (finalScore >= rankBThreshold)
        {
            return "B";
        }

        if (finalScore >= rankCThreshold)
        {
            return "C";
        }

        return "D";
    }

    private void SetRankBackground(string rank)
    {
        if (resultRankBackground == null)
        {
            return;
        }

        var sprite = GetRankSprite(rank);
        resultRankBackground.sprite = sprite;
        resultRankBackground.color = sprite != null ? Color.white : new Color(0.02f, 0.04f, 0.08f, 1f);
    }

    private Sprite GetRankSprite(string rank)
    {
        switch (rank)
        {
            case "SS":
                return resultRankSs;
            case "S":
                return resultRankS;
            case "A":
                return resultRankA;
            case "B":
            case "C":
                return resultRankCb;
            case "D":
            default:
                return resultRankD;
        }
    }

    private static string GetStageDisplayName(PurifierStage stage)
    {
        switch (stage)
        {
            case PurifierStage.Home:
                return "家ステージ";
            case PurifierStage.Street:
                return "街ステージ";
            case PurifierStage.City:
                return "都市ステージ";
            case PurifierStage.Space:
                return "宇宙ステージ";
            default:
                return "ステージ";
        }
    }

    private Text CreateBonusRow(int index, string text)
    {
        var y = 120f - index * 82f;
        return CreateText("ResultInfoText", bonusContainer, text, new Vector2(0f, y), new Vector2(760f, 70f), 38, new Color(1f, 0.94f, 0.56f), TextAnchor.MiddleCenter);
    }

    private void ResetViews()
    {
        fadePanel?.DOKill();
        resultRankBackground?.DOKill();
        resultDarkOverlay?.DOKill();
        backgroundPanel?.DOKill();
        titleText?.DOKill();
        titleText?.rectTransform.DOKill();
        normalScoreText?.DOKill();
        normalScoreText?.rectTransform.DOKill();
        totalScoreText?.DOKill();
        totalScoreText?.rectTransform.DOKill();
        rankText?.DOKill();
        rankText?.rectTransform.DOKill();

        foreach (var row in bonusRows)
        {
            if (row != null)
            {
                row.DOKill();
                row.rectTransform.DOKill();
                Destroy(row.gameObject);
            }
        }
        bonusRows.Clear();

        if (fadePanel != null)
        {
            fadePanel.color = new Color(0f, 0f, 0f, 0f);
            fadePanel.gameObject.SetActive(false);
        }

        if (resultRankBackground != null)
        {
            resultRankBackground.color = new Color(1f, 1f, 1f, 0f);
            resultRankBackground.gameObject.SetActive(false);
        }

        if (resultDarkOverlay != null)
        {
            resultDarkOverlay.color = new Color(0f, 0f, 0f, 0f);
            resultDarkOverlay.gameObject.SetActive(false);
        }

        backgroundPanel.gameObject.SetActive(false);
        isTransitioning = false;
        isResultSequencePlaying = false;
        hasPlayedResultScoreStartSe = false;
        hasPlayedResultRankRevealSe = false;
        titleText.gameObject.SetActive(false);
        normalScoreText.gameObject.SetActive(false);
        totalScoreText.gameObject.SetActive(false);
        rankText.gameObject.SetActive(false);
        SetResultButtonsVisible(false);
        normalScoreText.text = string.Empty;
        totalScoreText.text = string.Empty;
        rankText.text = string.Empty;
    }

    private IEnumerator PlayTextIn(Text text, float holdSeconds, float punchScale = 1.08f)
    {
        if (text == null)
        {
            yield break;
        }

        text.gameObject.SetActive(true);
        text.DOKill();
        text.rectTransform.DOKill();
        text.color = WithAlpha(text.color, 0f);
        text.rectTransform.localScale = Vector3.one * 0.82f;
        text.rectTransform.anchoredPosition += new Vector2(0f, -18f);

        var startPosition = text.rectTransform.anchoredPosition;
        var targetPosition = startPosition + new Vector2(0f, 18f);
        text.DOFade(1f, 0.16f).SetUpdate(true);
        text.rectTransform.DOAnchorPos(targetPosition, 0.18f).SetEase(Ease.OutCubic).SetUpdate(true);
        yield return TweenToYield(text.rectTransform.DOScale(punchScale, 0.14f).SetEase(Ease.OutBack).SetUpdate(true));
        yield return TweenToYield(text.rectTransform.DOScale(1f, 0.10f).SetEase(Ease.OutQuad).SetUpdate(true));
        yield return new WaitForSecondsRealtime(holdSeconds);
    }

    private IEnumerator PlayRankReveal(Text text)
    {
        if (text == null)
        {
            yield break;
        }

        PlayResultRankRevealSeOnce();
        text.gameObject.SetActive(true);
        text.DOKill();
        text.rectTransform.DOKill();
        text.color = WithAlpha(text.color, 0f);
        text.rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, rankRevealStartScale);

        var sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(text.DOFade(1f, Mathf.Max(0.01f, rankRevealFadeDuration)).SetEase(Ease.OutQuad));
        sequence.Join(text.rectTransform.DOScale(Mathf.Max(0.01f, rankRevealPopScale), Mathf.Max(0.01f, rankRevealPopDuration)).SetEase(Ease.OutBack));
        sequence.Append(text.rectTransform.DOScale(1f, Mathf.Max(0.01f, rankRevealSettleDuration)).SetEase(Ease.OutQuad));
        yield return TweenToYield(sequence);
    }

    private void PlayResultScoreStartSeOnce()
    {
        if (hasPlayedResultScoreStartSe)
        {
            return;
        }

        hasPlayedResultScoreStartSe = true;
        AudioManager.Instance?.PlayResultScoreStartSfx();
    }

    private void PlayResultRankRevealSeOnce()
    {
        if (hasPlayedResultRankRevealSe)
        {
            return;
        }

        hasPlayedResultRankRevealSe = true;
        AudioManager.Instance?.PlayResultRankRevealSfx();
    }

    private static IEnumerator TweenToYield(Tween tween)
    {
        var completed = false;
        tween.OnComplete(() => completed = true);
        while (!completed)
        {
            yield return null;
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private RectTransform CreateRect(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
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

    private Image CreatePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, bool raycastTarget)
    {
        var rect = CreateRect(name, parent, anchoredPosition, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private Text CreateText(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAnchor alignment)
    {
        var rect = CreateRect(name, parent, anchoredPosition, size);
        var label = rect.gameObject.AddComponent<Text>();
        label.text = text;
        label.font = uiFont != null ? uiFont : UiFontUtility.GetDefaultFont();
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
        outline.effectDistance = new Vector2(5f, -5f);
        var shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.50f);
        shadow.effectDistance = new Vector2(6f, -6f);
        return label;
    }

    private Button CreateButton(string name, RectTransform parent, string text, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var panel = CreateRoundedPanel(name, parent, anchoredPosition, size, Color.white, true);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.92f);
        outline.effectDistance = new Vector2(5f, -5f);
        var shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(7f, -7f);
        var inner = CreateRoundedPanel("Fill", panel.rectTransform, Vector2.zero, size - new Vector2(12f, 12f), color, false);

        var button = panel.gameObject.AddComponent<Button>();
        button.targetGraphic = inner;
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        button.colors = colors;

        CreateText($"{name}_Text", panel.rectTransform, text, Vector2.zero, size, 34, Color.white, TextAnchor.MiddleCenter);
        return button;
    }

    private Image CreateRoundedPanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, bool raycastTarget)
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
        texture.name = "GeneratedResultRoundedPanelTexture";
        texture.wrapMode = TextureWrapMode.Clamp;

        var radius = RoundedPanelRadius;
        var maxIndex = RoundedPanelTextureSize - 1;
        for (var y = 0; y < RoundedPanelTextureSize; y++)
        {
            for (var x = 0; x < RoundedPanelTextureSize; x++)
            {
                var dx = x < radius ? radius - x : x > maxIndex - radius ? x - (maxIndex - radius) : 0;
                var dy = y < radius ? radius - y : y > maxIndex - radius ? y - (maxIndex - radius) : 0;
                var inside = dx * dx + dy * dy <= radius * radius;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
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
        roundedPanelSprite.name = "GeneratedResultRoundedPanel";
        return roundedPanelSprite;
    }

    private void ShowResultButtons()
    {
        SetResultButtonsVisible(true);
        AnimateButtonIn(restartButton, 0f);
        AnimateButtonIn(backToTitleButton, 0.06f);
    }

    private void AnimateButtonIn(Button button, float delay)
    {
        if (button == null)
        {
            return;
        }

        var rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.DOKill();
        rect.localScale = Vector3.one * 0.84f;
        rect.DOScale(1f, 0.18f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void SetResultButtonsVisible(bool visible)
    {
        SetButtonVisible(restartButton, visible);
        SetButtonVisible(backToTitleButton, visible);
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(visible);
        button.interactable = visible && !isTransitioning;
    }

    private void OnRestartButtonClicked()
    {
        Debug.Log($"[Lifecycle] RestartCalled from=ResultButton target={SceneManager.GetActiveScene().name} t={Time.realtimeSinceStartup:0.00}");
        TransitionToScene(SceneManager.GetActiveScene().name);
    }

    private void OnBackToTitleButtonClicked()
    {
        Debug.Log($"[Lifecycle] ReturnToTitleCalled from=ResultButton target={titleSceneName} t={Time.realtimeSinceStartup:0.00}");
        TransitionToScene(titleSceneName);
    }

    private void TransitionToScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        StartCoroutine(TransitionToSceneRoutine(sceneName));
    }

    private IEnumerator TransitionToSceneRoutine(string sceneName)
    {
        isTransitioning = true;
        if (restartButton != null)
        {
            restartButton.interactable = false;
        }

        if (backToTitleButton != null)
        {
            backToTitleButton.interactable = false;
        }

        Time.timeScale = 1f;
        AudioManager.Instance?.StopGameplayBgm();
        Debug.Log($"[Lifecycle] ResultTransitionLoading scene={sceneName} t={Time.realtimeSinceStartup:0.00}");
        fadePanel.gameObject.SetActive(true);
        fadePanel.DOKill();
        yield return TweenToYield(fadePanel.DOFade(1f, Mathf.Max(0f, sceneTransitionFadeDuration)).SetEase(Ease.OutQuad).SetUpdate(true));
        SceneManager.LoadScene(sceneName);
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
