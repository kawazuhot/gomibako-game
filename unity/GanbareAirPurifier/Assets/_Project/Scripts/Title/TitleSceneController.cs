using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private Image titleImage;
    [SerializeField] private Image titleLogo;
    [SerializeField] private Text startText;
    [SerializeField] private Image fadePanel;
    [SerializeField] private string gameplaySceneName = "Main";
    [SerializeField] private float fadeDuration = 0.32f;

    private bool isTransitioning;
    private Tween logoBreathTween;
    private Tween startTextTween;

    public void Configure(Image title, Text start, Image fade, string gameplayScene, float duration)
    {
        Configure(title, null, start, fade, gameplayScene, duration);
    }

    public void Configure(Image title, Image logo, Text start, Image fade, string gameplayScene, float duration)
    {
        titleImage = title;
        titleLogo = logo;
        startText = start;
        fadePanel = fade;
        gameplaySceneName = gameplayScene;
        fadeDuration = duration;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        ApplyDefaultFont();

        if (fadePanel != null)
        {
            StretchFadePanelToCanvas();
            fadePanel.color = new Color(0f, 0f, 0f, 0f);
            fadePanel.raycastTarget = false;
        }
    }

    private void Start()
    {
        PlayIdleAnimations();
    }

    private void Update()
    {
        if (isTransitioning)
        {
            return;
        }

        if (WasStartPressed())
        {
            StartGame();
        }
    }

    private void OnDestroy()
    {
        logoBreathTween?.Kill();
        startTextTween?.Kill();
        fadePanel?.DOKill();
    }

    private void StartGame()
    {
        isTransitioning = true;
        logoBreathTween?.Kill();
        startTextTween?.Kill();

        if (titleLogo != null)
        {
            titleLogo.rectTransform.localScale = Vector3.one;
        }

        if (startText != null)
        {
            startText.text = "START!";
            startText.color = Color.white;
        }

        if (fadePanel == null)
        {
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        StretchFadePanelToCanvas();
        fadePanel.raycastTarget = true;
        fadePanel.DOFade(1f, fadeDuration)
            .SetUpdate(true)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => SceneManager.LoadScene(gameplaySceneName));
    }

    private void PlayIdleAnimations()
    {
        if (titleLogo != null)
        {
            var logoRect = titleLogo.rectTransform;
            logoRect.DOKill();
            logoRect.localScale = Vector3.one;
            logoBreathTween = logoRect
                .DOScale(1.025f, 1.35f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        if (startText != null)
        {
            startText.DOKill();
            startText.color = Color.white;
            startTextTween = startText
                .DOFade(0.45f, 0.72f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void StretchFadePanelToCanvas()
    {
        if (fadePanel == null)
        {
            return;
        }

        var rectTransform = fadePanel.rectTransform;
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ApplyDefaultFont()
    {
        var font = UiFontUtility.GetDefaultFont();
        if (font == null)
        {
            return;
        }

        var texts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in texts)
        {
            text.font = font;
        }
    }

    private static bool WasStartPressed()
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
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        if (Input.touchCount <= 0)
        {
            return false;
        }

        return Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }
}
