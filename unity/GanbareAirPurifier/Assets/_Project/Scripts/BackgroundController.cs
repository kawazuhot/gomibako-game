using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundController : MonoBehaviour
{
    [SerializeField] private Image homeBackground;
    [SerializeField] private Image streetBackground;
    [SerializeField] private float homeInitialScale = 1.2f;
    [SerializeField] private float homeStageUpScale = 1.0f;
    [SerializeField] private float stageUpDuration = 0.6f;

    private Tween activeTween;

    public float HomeInitialScale => homeInitialScale;
    public float HomeStageUpScale => homeStageUpScale;

    public void Configure(Image home, Image street, Sprite homeSprite)
    {
        homeBackground = home;
        streetBackground = street;

        if (homeBackground != null)
        {
            homeBackground.sprite = homeSprite;
            homeBackground.preserveAspect = true;
            homeBackground.color = homeSprite != null ? Color.white : new Color(1f, 0.92f, 0.74f);
        }

        if (streetBackground != null)
        {
            streetBackground.preserveAspect = true;
            streetBackground.color = new Color(0.70f, 0.88f, 1f);
        }

        SetHomeBackground();
    }

    public void SetHomeBackground()
    {
        activeTween?.Kill();

        if (homeBackground != null)
        {
            homeBackground.gameObject.SetActive(true);
            homeBackground.rectTransform.localScale = Vector3.one * homeInitialScale;
        }

        if (streetBackground != null)
        {
            streetBackground.gameObject.SetActive(false);
            streetBackground.rectTransform.localScale = Vector3.one;
        }
    }

    public void SetStreetBackground()
    {
        activeTween?.Kill();

        if (homeBackground != null)
        {
            homeBackground.gameObject.SetActive(false);
        }

        if (streetBackground != null)
        {
            streetBackground.gameObject.SetActive(true);
            streetBackground.rectTransform.localScale = Vector3.one;
        }
    }

    public void PlayHomeStageZoomOut(Action onComplete = null)
    {
        if (homeBackground == null)
        {
            onComplete?.Invoke();
            return;
        }

        homeBackground.gameObject.SetActive(true);
        activeTween?.Kill();
        activeTween = homeBackground.rectTransform
            .DOScale(Vector3.one * homeStageUpScale, stageUpDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void PlayStageUpBackgroundTransition(Action onComplete = null)
    {
        PlayHomeStageZoomOut(() =>
        {
            SetStreetBackground();
            onComplete?.Invoke();
        });
    }
}
