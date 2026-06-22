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
    [SerializeField] private Text startText;
    [SerializeField] private Image fadePanel;
    [SerializeField] private string gameplaySceneName = "Main";
    [SerializeField] private float fadeDuration = 0.55f;

    private bool isTransitioning;
    private Tween imageFloatTween;
    private Tween imageScaleTween;
    private Tween startTextTween;

    public void Configure(Image title, Text start, Image fade, string gameplayScene, float duration)
    {
        titleImage = title;
        startText = start;
        fadePanel = fade;
        gameplaySceneName = gameplayScene;
        fadeDuration = duration;
    }

    private void Awake()
    {
        Time.timeScale = 1f;

        if (fadePanel != null)
        {
            fadePanel.color = new Color(0f, 0f, 0f, 0f);
            fadePanel.raycastTarget = false;
        }
    }

    private void Start()
    {
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
        imageFloatTween?.Kill();
        imageScaleTween?.Kill();
        startTextTween?.Kill();
        fadePanel?.DOKill();
    }

    private void StartGame()
    {
        isTransitioning = true;
        imageFloatTween?.Kill();
        imageScaleTween?.Kill();
        startTextTween?.Kill();

        if (startText != null)
        {
            startText.text = "START!";
        }

        if (fadePanel == null)
        {
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        fadePanel.raycastTarget = true;
        fadePanel.DOFade(1f, fadeDuration)
            .SetUpdate(true)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => SceneManager.LoadScene(gameplaySceneName));
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
