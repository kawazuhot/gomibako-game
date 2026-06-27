using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private Button settingsButton;
    [SerializeField] private RectTransform settingsPanelRoot;
    [SerializeField] private Text selectedDirectionText;
    [SerializeField] private Button rightToLeftButton;
    [SerializeField] private Button leftToRightButton;

    private bool isTransitioning;
    private Tween logoBreathTween;
    private Tween startTextTween;
    private ItemFlowDirection selectedFlowDirection = ItemFlowDirection.RightToLeft;

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
        Debug.Log($"[Lifecycle] TitleAwake scene={SceneManager.GetActiveScene().name} t={Time.realtimeSinceStartup:0.00}");
        EnsureEventSystem();
        ApplyDefaultFont();
        EnsureSettingsUi();
        LoadSettings();

        if (fadePanel != null)
        {
            StretchFadePanelToCanvas();
            fadePanel.color = new Color(0f, 0f, 0f, 0f);
            fadePanel.raycastTarget = false;
        }
    }

    private void Start()
    {
        Debug.Log($"[Lifecycle] TitleLoaded scene={SceneManager.GetActiveScene().name} t={Time.realtimeSinceStartup:0.00}");
        PlayIdleAnimations();
    }

    private void Update()
    {
        if (isTransitioning)
        {
            return;
        }

        if (settingsPanelRoot != null && settingsPanelRoot.gameObject.activeSelf)
        {
            if (WasCloseSettingsPressed())
            {
                CloseSettingsPanel();
            }

            return;
        }

        if (WasKeyboardStartPressed())
        {
            StartGame();
            return;
        }

        if (TryGetPointerStartPosition(out var pointerPosition))
        {
            if (IsPointerInsideSettingsUi(pointerPosition))
            {
                return;
            }

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
        Debug.Log($"[Lifecycle] GameplayStartRequested from=TitleTap target={gameplaySceneName} flowDirection={FlowDirectionSettings.GetDisplayName(FlowDirectionSettings.Load())} t={Time.realtimeSinceStartup:0.00}");
        logoBreathTween?.Kill();
        startTextTween?.Kill();
        CloseSettingsPanel();

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
        fadePanel.transform.SetAsLastSibling();
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

    private void EnsureSettingsUi()
    {
        if (settingsPanelRoot != null && settingsButton != null)
        {
            return;
        }

        var canvas = titleImage != null ? titleImage.canvas : FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var root = canvas.GetComponent<RectTransform>();
        var font = UiFontUtility.GetDefaultFont();

        settingsButton = CreateButton("SettingsButton", root, "設定", new Vector2(365f, -835f), new Vector2(240f, 82f), new Color(0.12f, 0.52f, 0.96f, 0.92f), font);
        settingsButton.onClick.AddListener(OpenSettingsPanel);

        settingsPanelRoot = CreateRect("SettingsPanelRoot", root, Vector2.zero, Vector2.zero);
        StretchToParent(settingsPanelRoot);

        var dim = CreatePanel("SettingsDim", settingsPanelRoot, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.56f), true);
        StretchToParent(dim.rectTransform);

        var panel = CreatePanel("SettingsPanel", settingsPanelRoot, Vector2.zero, new Vector2(780f, 690f), new Color(0.04f, 0.13f, 0.23f, 0.96f), true);
        panel.raycastTarget = true;
        var panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(1f, 1f, 1f, 0.28f);
        panelOutline.effectDistance = new Vector2(4f, -4f);

        CreateText("SettingsTitleText", panel.rectTransform, "設定", new Vector2(0f, 245f), new Vector2(620f, 84f), 52, Color.white, TextAnchor.MiddleCenter, font);
        CreateText("FlowDirectionLabel", panel.rectTransform, "流れる方向", new Vector2(0f, 155f), new Vector2(620f, 66f), 34, Color.white, TextAnchor.MiddleCenter, font);
        selectedDirectionText = CreateText("SelectedFlowDirectionText", panel.rectTransform, string.Empty, new Vector2(0f, 95f), new Vector2(620f, 58f), 32, new Color(0.82f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter, font);

        rightToLeftButton = CreateButton("FlowRightToLeftButton", panel.rectTransform, "右 → 左", new Vector2(0f, 5f), new Vector2(560f, 86f), new Color(0.14f, 0.50f, 0.92f, 0.95f), font);
        leftToRightButton = CreateButton("FlowLeftToRightButton", panel.rectTransform, "左 → 右", new Vector2(0f, -105f), new Vector2(560f, 86f), new Color(0.14f, 0.50f, 0.92f, 0.95f), font);
        var closeButton = CreateButton("SettingsCloseButton", panel.rectTransform, "閉じる", new Vector2(0f, -245f), new Vector2(360f, 78f), new Color(0.20f, 0.74f, 0.52f, 0.95f), font);

        rightToLeftButton.onClick.AddListener(() => SetFlowDirection(ItemFlowDirection.RightToLeft));
        leftToRightButton.onClick.AddListener(() => SetFlowDirection(ItemFlowDirection.LeftToRight));
        closeButton.onClick.AddListener(CloseSettingsPanel);

        settingsPanelRoot.SetAsLastSibling();
        settingsPanelRoot.gameObject.SetActive(false);
    }

    private void LoadSettings()
    {
        selectedFlowDirection = FlowDirectionSettings.Load();
        RefreshSettingsUi();
    }

    private void OpenSettingsPanel()
    {
        if (isTransitioning || settingsPanelRoot == null)
        {
            return;
        }

        LoadSettings();
        settingsPanelRoot.gameObject.SetActive(true);
        settingsPanelRoot.SetAsLastSibling();
    }

    private void CloseSettingsPanel()
    {
        if (settingsPanelRoot != null)
        {
            settingsPanelRoot.gameObject.SetActive(false);
        }
    }

    private void SetFlowDirection(ItemFlowDirection direction)
    {
        selectedFlowDirection = direction;
        FlowDirectionSettings.Save(direction);
        RefreshSettingsUi();
        Debug.Log($"[Settings] FlowDirection={FlowDirectionSettings.GetDisplayName(direction)} value={(int)direction}");
    }

    private void RefreshSettingsUi()
    {
        if (selectedDirectionText != null)
        {
            selectedDirectionText.text = $"現在：{FlowDirectionSettings.GetDisplayName(selectedFlowDirection)}";
        }

        SetDirectionButtonSelected(rightToLeftButton, selectedFlowDirection == ItemFlowDirection.RightToLeft);
        SetDirectionButtonSelected(leftToRightButton, selectedFlowDirection == ItemFlowDirection.LeftToRight);
    }

    private static void SetDirectionButtonSelected(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null)
        {
            return;
        }

        button.targetGraphic.color = selected
            ? new Color(0.10f, 0.68f, 1f, 1f)
            : new Color(0.14f, 0.50f, 0.92f, 0.95f);
    }

    private bool IsPointerInsideSettingsUi(Vector2 screenPosition)
    {
        var canvas = titleImage != null ? titleImage.canvas : FindAnyObjectByType<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return IsScreenPointInside(settingsButton != null ? settingsButton.transform as RectTransform : null, screenPosition, camera) ||
               IsScreenPointInside(settingsPanelRoot, screenPosition, camera);
    }

    private static bool IsScreenPointInside(RectTransform rectTransform, Vector2 screenPosition, Camera camera)
    {
        return rectTransform != null &&
               rectTransform.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, camera);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        var rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        return rectTransform;
    }

    private static Image CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool raycastTarget)
    {
        var rectTransform = CreateRect(name, parent, position, size);
        var image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment, Font font)
    {
        var rectTransform = CreateRect(name, parent, position, size);
        var label = rectTransform.gameObject.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        var outline = rectTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
        outline.effectDistance = new Vector2(3f, -3f);
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size, Color color, Font font)
    {
        var image = CreatePanel(name, parent, position, size, color, true);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.65f);
        outline.effectDistance = new Vector2(4f, -4f);

        var label = CreateText(name + "_Text", image.rectTransform, text, Vector2.zero, size, Mathf.RoundToInt(size.y * 0.42f), Color.white, TextAnchor.MiddleCenter, font);
        label.transform.SetAsLastSibling();
        return button;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
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

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
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

        var texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        foreach (var text in texts)
        {
            text.font = font;
        }
    }

    private static bool WasKeyboardStartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            return true;
        }

        return false;
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static bool WasCloseSettingsPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private static bool TryGetPointerStartPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            screenPosition = Input.GetTouch(0).position;
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
#endif
    }
}
