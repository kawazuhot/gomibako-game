using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [SerializeField] private Image overlayImage;

    private Tween activeTween;

    public void Configure(Image overlay)
    {
        overlayImage = overlay;
        StretchOverlayToCanvas();
        SetAlpha(0f);
        SetBlocksInput(false);
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return FadeTo(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return FadeTo(0f, duration);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (overlayImage == null)
        {
            yield break;
        }

        StretchOverlayToCanvas();
        SetBlocksInput(true);
        activeTween?.Kill();

        var completed = false;
        activeTween = overlayImage
            .DOFade(targetAlpha, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => completed = true);

        while (!completed)
        {
            yield return null;
        }

        SetBlocksInput(targetAlpha > 0.001f);
    }

    private void SetAlpha(float alpha)
    {
        if (overlayImage == null)
        {
            return;
        }

        var color = overlayImage.color;
        color.a = alpha;
        overlayImage.color = color;
    }

    private void SetBlocksInput(bool blocksInput)
    {
        if (overlayImage != null)
        {
            overlayImage.raycastTarget = blocksInput;
        }
    }

    private void StretchOverlayToCanvas()
    {
        if (overlayImage == null)
        {
            return;
        }

        var rectTransform = overlayImage.rectTransform;
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
}
