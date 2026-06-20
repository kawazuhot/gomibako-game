using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class AirPurifierController : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RectTransform suctionPoint;
    [SerializeField] private Image purifierImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite suctionSprite;
    [SerializeField] private Sprite failSprite;

    private Sequence activeSequence;
    private Vector3 baseScale = Vector3.one;
    private bool isFailAnimating;

    public RectTransform RectTransform => rectTransform;
    public Vector2 SuctionPoint
    {
        get
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            return suctionPoint != null
                ? rectTransform.anchoredPosition + suctionPoint.anchoredPosition
                : rectTransform.anchoredPosition + new Vector2(0f, 90f);
        }
    }

    public void Configure(RectTransform root, RectTransform point, Image displayImage, Sprite normal, Sprite suction, Sprite fail)
    {
        rectTransform = root;
        suctionPoint = point;
        purifierImage = displayImage;
        normalSprite = normal;
        suctionSprite = suction;
        failSprite = fail;
        baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        SetNormal();
    }

    public void SetNormal()
    {
        activeSequence?.Kill();
        isFailAnimating = false;
        SetSprite(normalSprite);
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }
    }

    public void SetSuction()
    {
        SetSprite(suctionSprite != null ? suctionSprite : normalSprite);
    }

    public void SetFail()
    {
        SetSprite(failSprite != null ? failSprite : normalSprite);
    }

    public void StartSuctionHold()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (isFailAnimating)
        {
            return;
        }

        SetSuction();
        activeSequence?.Kill();
        rectTransform.localScale = baseScale;
        activeSequence = DOTween.Sequence()
            .Append(rectTransform.DOScale(new Vector3(baseScale.x * 1.04f, baseScale.y * 1.12f, baseScale.z), 0.18f))
            .Append(rectTransform.DOScale(baseScale * 1.03f, 0.18f))
            .Append(rectTransform.DOScale(baseScale, 0.18f))
            .SetLoops(-1, LoopType.Restart);
    }

    public void StopSuctionHold()
    {
        SetNormal();
    }

    public void PlaySuctionAnimation()
    {
        if (rectTransform == null)
        {
            return;
        }

        if (isFailAnimating)
        {
            return;
        }

        SetSuction();
        activeSequence?.Kill();
        rectTransform.localScale = baseScale;
        activeSequence = DOTween.Sequence()
            .Append(rectTransform.DOScale(new Vector3(baseScale.x * 1.06f, baseScale.y * 1.18f, baseScale.z), 0.12f))
            .Append(rectTransform.DOScale(baseScale * 1.04f, 0.12f))
            .Append(rectTransform.DOScale(baseScale, 0.12f));
    }

    public void PlayFailAnimation()
    {
        if (rectTransform == null)
        {
            return;
        }

        SetFail();
        isFailAnimating = true;
        activeSequence?.Kill();
        rectTransform.localScale = baseScale;
        activeSequence = DOTween.Sequence()
            .Append(rectTransform.DOShakeAnchorPos(0.36f, new Vector2(34f, 0f), 18, 80f))
            .Join(rectTransform.DOScale(new Vector3(baseScale.x * 1.10f, baseScale.y * 0.92f, baseScale.z), 0.12f).SetLoops(2, LoopType.Yoyo))
            .AppendCallback(() =>
            {
                isFailAnimating = false;
                rectTransform.localScale = baseScale;
            });
    }

    private void SetSprite(Sprite sprite)
    {
        if (purifierImage == null || sprite == null)
        {
            return;
        }

        purifierImage.sprite = sprite;
        purifierImage.preserveAspect = true;
        purifierImage.color = Color.white;
    }
}
