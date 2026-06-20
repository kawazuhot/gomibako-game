using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SuctionZoneVisualController : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image[] visualImages;

    private Sequence pulseSequence;
    private Sequence flashSequence;
    private Color idleColor = new Color(1f, 0.16f, 0.16f, 0.45f);
    private Color activeColor = new Color(1f, 0.28f, 0.18f, 0.80f);
    private Color suckingColor = new Color(1f, 0.12f, 0.12f, 0.65f);
    private Color failColor = new Color(1f, 0.02f, 0.02f, 0.82f);

    public void Configure(RectTransform targetRoot, Image[] images)
    {
        root = targetRoot;
        visualImages = images;
        SetIdle();
    }

    public void SetIdle()
    {
        StopSequences();
        ApplyColor(idleColor);
        if (root != null)
        {
            root.localScale = Vector3.one;
            root.localRotation = Quaternion.identity;
        }
    }

    public void SetTargetInRange()
    {
        StopSequences();
        ApplyColor(activeColor);
        if (root != null)
        {
            root.localRotation = Quaternion.identity;
            root.DOScale(1.08f, 0.12f).SetEase(Ease.OutBack);
        }
    }

    public void SetSucking()
    {
        StopSequences();
        ApplyColor(suckingColor);
        if (root == null)
        {
            return;
        }

        root.localScale = Vector3.one;
        pulseSequence = DOTween.Sequence()
            .Append(root.DOScale(1.08f, 0.22f).SetEase(Ease.OutQuad))
            .Join(root.DORotate(new Vector3(0f, 0f, 4f), 0.22f))
            .Append(root.DOScale(0.98f, 0.22f).SetEase(Ease.InOutQuad))
            .Join(root.DORotate(new Vector3(0f, 0f, -4f), 0.22f))
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void SetFailFlash()
    {
        StopSequences();
        if (root == null)
        {
            ApplyColor(failColor);
            return;
        }

        root.localRotation = Quaternion.identity;
        flashSequence = DOTween.Sequence()
            .AppendCallback(() => ApplyColor(failColor))
            .Append(root.DOShakeAnchorPos(0.18f, new Vector2(10f, 0f), 12, 90f))
            .AppendCallback(SetIdle);
    }

    private void StopSequences()
    {
        pulseSequence?.Kill();
        flashSequence?.Kill();
        if (root != null)
        {
            root.DOKill();
        }
    }

    private void ApplyColor(Color color)
    {
        if (visualImages == null)
        {
            return;
        }

        foreach (var image in visualImages)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }
}
