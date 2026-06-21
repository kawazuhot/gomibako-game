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
    [SerializeField] private float streetInitialScale = 1.2f;
    [SerializeField] private float streetStageProgressScale = 1.0f;
    [SerializeField] private float stageUpDuration = 0.6f;

    private Tween activeTween;

    public float HomeInitialScale => homeInitialScale;
    public float HomeStageUpScale => homeStageUpScale;
    public float StreetInitialScale => streetInitialScale;
    public float StreetStageProgressScale => streetStageProgressScale;

    public void Configure(Image home, Image street, Sprite homeSprite, Sprite streetSprite)
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
            streetBackground.sprite = streetSprite;
            streetBackground.preserveAspect = true;
            streetBackground.color = streetSprite != null ? Color.white : new Color(0.70f, 0.88f, 1f);
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
            streetBackground.rectTransform.localScale = Vector3.one * streetInitialScale;
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
            streetBackground.rectTransform.localScale = Vector3.one * streetInitialScale;
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

    public void PlayStreetStageZoomOut(Action onComplete = null)
    {
        if (streetBackground == null)
        {
            onComplete?.Invoke();
            return;
        }

        streetBackground.gameObject.SetActive(true);
        activeTween?.Kill();
        activeTween = streetBackground.rectTransform
            .DOScale(Vector3.one * streetStageProgressScale, stageUpDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => onComplete?.Invoke());
    }
}
