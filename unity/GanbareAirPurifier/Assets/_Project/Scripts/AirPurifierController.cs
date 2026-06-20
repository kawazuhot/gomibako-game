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

    private Sequence failSequence;
    private Tween idleBreathingTween;
    private Tween suctionShakeTween;
    private Vector3 baseScale = Vector3.one;
    private Vector2 baseAnchoredPosition;
    private bool isFailAnimating;
    private bool isSuctionHolding;

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
        baseAnchoredPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        SetNormal();
    }

    public void SetNormal()
    {
        StopAllMotion();
        isFailAnimating = false;
        isSuctionHolding = false;
        SetSprite(normalSprite);
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
            rectTransform.anchoredPosition = baseAnchoredPosition;
        }
        StartIdleBreathing();
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

        isSuctionHolding = true;
        SetSuction();
        StopIdleBreathing();
        StopSuctionShake();
        rectTransform.localScale = baseScale;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        StartSuctionShake();
    }

    public void StopSuctionHold()
    {
        isSuctionHolding = false;
        if (isFailAnimating)
        {
            return;
        }

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
        if (isSuctionHolding && suctionShakeTween == null)
        {
            StartSuctionShake();
        }
    }

    public void PlayFailAnimation()
    {
        if (rectTransform == null)
        {
            return;
        }

        SetFail();
        isFailAnimating = true;
        StopIdleBreathing();
        StopSuctionShake();
        failSequence?.Kill();
        rectTransform.localScale = baseScale;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        failSequence = DOTween.Sequence()
            .Append(rectTransform.DOShakeAnchorPos(0.36f, new Vector2(34f, 0f), 18, 80f))
            .Join(rectTransform.DOScale(new Vector3(baseScale.x * 1.10f, baseScale.y * 0.92f, baseScale.z), 0.12f).SetLoops(2, LoopType.Yoyo))
            .AppendCallback(() =>
            {
                isFailAnimating = false;
                rectTransform.localScale = baseScale;
                rectTransform.anchoredPosition = baseAnchoredPosition;
                if (isSuctionHolding)
                {
                    StartSuctionHold();
                }
                else
                {
                    SetNormal();
                }
            });
    }

    public void StartIdleBreathing()
    {
        if (rectTransform == null || isFailAnimating || isSuctionHolding)
        {
            return;
        }

        StopIdleBreathing();
        rectTransform.localScale = baseScale;
        idleBreathingTween = rectTransform.DOScale(baseScale * 1.04f, 0.95f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopIdleBreathing()
    {
        idleBreathingTween?.Kill();
        idleBreathingTween = null;
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }
    }

    public void StartSuctionShake()
    {
        if (rectTransform == null || isFailAnimating)
        {
            return;
        }

        StopSuctionShake();
        rectTransform.anchoredPosition = baseAnchoredPosition;
        suctionShakeTween = rectTransform.DOAnchorPosX(baseAnchoredPosition.x + 6f, 0.055f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopSuctionShake()
    {
        suctionShakeTween?.Kill();
        suctionShakeTween = null;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = baseAnchoredPosition;
        }
    }

    private void StopAllMotion()
    {
        failSequence?.Kill();
        failSequence = null;
        StopIdleBreathing();
        StopSuctionShake();
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
