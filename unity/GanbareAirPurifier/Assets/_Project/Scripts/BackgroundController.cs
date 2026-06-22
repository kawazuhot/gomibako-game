using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundController : MonoBehaviour
{
    [SerializeField] private Image homeBackground;
    [SerializeField] private Image streetBackground;
    [SerializeField] private Image cityBackground;
    [SerializeField] private Image spaceBackground;
    [SerializeField] private float homeInitialScale = 1.0f;
    [SerializeField] private float homeStageUpScale = 1.0f;
    [SerializeField] private float streetInitialScale = 1.0f;
    [SerializeField] private float streetStageProgressScale = 1.0f;
    [SerializeField] private float cityInitialScale = 1.0f;
    [SerializeField] private float cityStageProgressScale = 1.0f;
    [SerializeField] private float spaceInitialScale = 1.0f;
    [SerializeField] private float spaceStageProgressScale = 1.0f;
    [SerializeField] private float stageUpDuration = 0.6f;

    private Tween activeTween;

    public float HomeInitialScale => homeInitialScale;
    public float HomeStageUpScale => homeStageUpScale;
    public float StreetInitialScale => streetInitialScale;
    public float StreetStageProgressScale => streetStageProgressScale;
    public float CityInitialScale => cityInitialScale;
    public float CityStageProgressScale => cityStageProgressScale;
    public float SpaceInitialScale => spaceInitialScale;
    public float SpaceStageProgressScale => spaceStageProgressScale;

    public void Configure(Image home, Image street, Image city, Image space, Sprite homeSprite, Sprite streetSprite, Sprite citySprite, Sprite spaceSprite)
    {
        homeBackground = home;
        streetBackground = street;
        cityBackground = city;
        spaceBackground = space;

        if (homeBackground != null)
        {
            homeBackground.sprite = homeSprite;
            homeBackground.preserveAspect = true;
            homeBackground.color = homeSprite != null ? Color.white : new Color(1f, 0.92f, 0.74f);
            homeBackground.GetComponent<AspectFillImage>()?.Apply();
        }

        if (streetBackground != null)
        {
            streetBackground.sprite = streetSprite;
            streetBackground.preserveAspect = true;
            streetBackground.color = streetSprite != null ? Color.white : new Color(0.70f, 0.88f, 1f);
            streetBackground.GetComponent<AspectFillImage>()?.Apply();
        }

        if (cityBackground != null)
        {
            cityBackground.sprite = citySprite;
            cityBackground.preserveAspect = true;
            cityBackground.color = citySprite != null ? Color.white : new Color(0.55f, 0.70f, 0.92f);
            cityBackground.GetComponent<AspectFillImage>()?.Apply();
        }

        if (spaceBackground != null)
        {
            spaceBackground.sprite = spaceSprite;
            spaceBackground.preserveAspect = true;
            spaceBackground.color = spaceSprite != null ? Color.white : new Color(0.10f, 0.08f, 0.22f);
            spaceBackground.GetComponent<AspectFillImage>()?.Apply();
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

        if (cityBackground != null)
        {
            cityBackground.gameObject.SetActive(false);
            cityBackground.rectTransform.localScale = Vector3.one * cityInitialScale;
        }

        if (spaceBackground != null)
        {
            spaceBackground.gameObject.SetActive(false);
            spaceBackground.rectTransform.localScale = Vector3.one * spaceInitialScale;
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

        if (cityBackground != null)
        {
            cityBackground.gameObject.SetActive(false);
            cityBackground.rectTransform.localScale = Vector3.one * cityInitialScale;
        }

        if (spaceBackground != null)
        {
            spaceBackground.gameObject.SetActive(false);
            spaceBackground.rectTransform.localScale = Vector3.one * spaceInitialScale;
        }
    }

    public void SetCityBackground()
    {
        activeTween?.Kill();

        if (homeBackground != null)
        {
            homeBackground.gameObject.SetActive(false);
        }

        if (streetBackground != null)
        {
            streetBackground.gameObject.SetActive(false);
        }

        if (cityBackground != null)
        {
            cityBackground.gameObject.SetActive(true);
            cityBackground.rectTransform.localScale = Vector3.one * cityInitialScale;
        }

        if (spaceBackground != null)
        {
            spaceBackground.gameObject.SetActive(false);
            spaceBackground.rectTransform.localScale = Vector3.one * spaceInitialScale;
        }
    }

    public void SetSpaceBackground()
    {
        activeTween?.Kill();

        if (homeBackground != null)
        {
            homeBackground.gameObject.SetActive(false);
        }

        if (streetBackground != null)
        {
            streetBackground.gameObject.SetActive(false);
        }

        if (cityBackground != null)
        {
            cityBackground.gameObject.SetActive(false);
        }

        if (spaceBackground != null)
        {
            spaceBackground.gameObject.SetActive(true);
            spaceBackground.rectTransform.localScale = Vector3.one * spaceInitialScale;
        }
    }

    public void PlayHomeStageZoomOut(Action onComplete = null)
    {
        activeTween?.Kill();
        onComplete?.Invoke();
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
        activeTween?.Kill();
        onComplete?.Invoke();
    }

    public void PlayCityStageZoomOut(Action onComplete = null)
    {
        activeTween?.Kill();
        onComplete?.Invoke();
    }

    public void PlaySpaceStageZoomOut(Action onComplete = null)
    {
        activeTween?.Kill();
        onComplete?.Invoke();
    }
}
