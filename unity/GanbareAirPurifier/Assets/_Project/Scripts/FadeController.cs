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
}
