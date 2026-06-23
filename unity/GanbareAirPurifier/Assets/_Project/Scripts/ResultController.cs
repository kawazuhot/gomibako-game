using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
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
    [SerializeField, Range(0f, 1f)] private float fadeAlpha = 0.76f;

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
    private Image backgroundPanel;
    private Text titleText;
    private Text normalScoreText;
    private RectTransform bonusContainer;
    private Text totalScoreText;
    private Text rankText;
    private Font uiFont;
    private readonly List<Text> bonusRows = new List<Text>();
    private Coroutine resultRoutine;
    private bool isTransitioning;

    public void Configure(RectTransform resultRoot, Font font)
    {
        root = resultRoot;
        uiFont = font != null ? font : UiFontUtility.GetDefaultFont();
        BuildUi();
    }

    public void ShowResult(int normalScore, int reachedLevel, PurifierStage reachedStage, int maxCombo, int mistakeCount, int bombHitCount)
    {
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

    private void BuildUi()
    {
        if (root == null || fadePanel != null)
        {
            return;
        }

        fadePanel = CreatePanel("ResultFadePanel", root, Vector2.zero, new Vector2(1080f, 1920f), new Color(0f, 0f, 0f, 0f), true);
        StretchToParent(fadePanel.rectTransform);

        backgroundPanel = CreatePanel("ResultBackground", root, Vector2.zero, new Vector2(880f, 1160f), new Color(0.04f, 0.08f, 0.14f, 0.86f), false);
        var backgroundOutline = backgroundPanel.gameObject.AddComponent<Outline>();
        backgroundOutline.effectColor = new Color(1f, 1f, 1f, 0.9f);
        backgroundOutline.effectDistance = new Vector2(8f, -8f);
        backgroundPanel.gameObject.SetActive(false);

        titleText = CreateText("ResultTitleText", backgroundPanel.rectTransform, "清浄リザルト", new Vector2(0f, 470f), new Vector2(780f, 110f), 58, new Color(1f, 0.95f, 0.24f), TextAnchor.MiddleCenter);
        normalScoreText = CreateText("ResultNormalScoreText", backgroundPanel.rectTransform, string.Empty, new Vector2(0f, 330f), new Vector2(760f, 82f), 42, Color.white, TextAnchor.MiddleCenter);
        bonusContainer = CreateRect("ResultBonusContainer", backgroundPanel.rectTransform, new Vector2(0f, 80f), new Vector2(760f, 330f));
        totalScoreText = CreateText("ResultTotalScoreText", backgroundPanel.rectTransform, string.Empty, new Vector2(0f, -220f), new Vector2(760f, 92f), 48, new Color(0.54f, 0.94f, 1f), TextAnchor.MiddleCenter);
        rankText = CreateText("ResultRankText", backgroundPanel.rectTransform, string.Empty, new Vector2(0f, -350f), new Vector2(820f, 132f), 68, new Color(1f, 0.42f, 0.18f), TextAnchor.MiddleCenter);
        restartButton = CreateButton("ResultRestartButton", backgroundPanel.rectTransform, "リスタート", new Vector2(0f, -485f), new Vector2(520f, 86f), new Color(0.18f, 0.62f, 1f));
        backToTitleButton = CreateButton("ResultBackToTitleButton", backgroundPanel.rectTransform, "タイトルに戻る", new Vector2(0f, -590f), new Vector2(520f, 86f), new Color(0.20f, 0.78f, 0.48f));
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        backToTitleButton.onClick.AddListener(OnBackToTitleButtonClicked);
        SetResultButtonsVisible(false);
    }

    private IEnumerator PlayResult(int normalScore, int reachedLevel, PurifierStage reachedStage, int maxCombo, int mistakeCount, int bombHitCount)
    {
        BuildUi();
        ResetViews();

        fadePanel.gameObject.SetActive(true);
        fadePanel.DOKill();
        fadePanel.color = new Color(0f, 0f, 0f, 0f);
        yield return TweenToYield(fadePanel.DOFade(fadeAlpha, fadeDuration).SetEase(Ease.OutQuad).SetUpdate(true));

        backgroundPanel.gameObject.SetActive(true);
        yield return PlayTextIn(titleText, 0.16f);

        normalScoreText.text = $"清浄スコア  {normalScore:N0}";
        yield return PlayTextIn(normalScoreText, 0.4f);

        var bonuses = BuildBonuses(reachedLevel, reachedStage, maxCombo, mistakeCount, bombHitCount);
        var bonusTotal = 0;
        foreach (var bonus in bonuses)
        {
            if (!bonus.Achieved)
            {
                continue;
            }

            bonusTotal += bonus.Score;
            var row = CreateBonusRow(bonusRows.Count, $"{bonus.DisplayName}  +{bonus.Score:N0}");
            bonusRows.Add(row);
            yield return PlayTextIn(row, 0.25f);
        }

        var finalScore = normalScore + bonusTotal;
        totalScoreText.text = $"TOTAL  {finalScore:N0}";
        yield return PlayTextIn(totalScoreText, 0.4f);

        rankText.text = $"清浄RANK {GetRank(finalScore)}";
        yield return PlayTextIn(rankText, 0.2f, 1.18f);

        ShowResultButtons();
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

    private Text CreateBonusRow(int index, string text)
    {
        var y = 130f - index * 76f;
        return CreateText("ResultBonusText", bonusContainer, text, new Vector2(0f, y), new Vector2(720f, 64f), 34, new Color(1f, 0.94f, 0.56f), TextAnchor.MiddleCenter);
    }

    private void ResetViews()
    {
        foreach (var row in bonusRows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }
        bonusRows.Clear();

        backgroundPanel.gameObject.SetActive(false);
        isTransitioning = false;
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
        var panel = CreatePanel(name, parent, anchoredPosition, size, color, true);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.92f);
        outline.effectDistance = new Vector2(5f, -5f);
        var shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(7f, -7f);

        var button = panel.gameObject.AddComponent<Button>();
        button.targetGraphic = panel;
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        button.colors = colors;

        CreateText($"{name}_Text", panel.rectTransform, text, Vector2.zero, size, 34, Color.white, TextAnchor.MiddleCenter);
        return button;
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
        TransitionToScene(SceneManager.GetActiveScene().name);
    }

    private void OnBackToTitleButtonClicked()
    {
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
