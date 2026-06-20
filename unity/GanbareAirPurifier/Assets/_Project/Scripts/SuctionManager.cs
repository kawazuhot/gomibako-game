using System;
using DG.Tweening;
using UnityEngine;

public class SuctionManager : MonoBehaviour
{
    [SerializeField] private float popHeight = 90f;
    [SerializeField] private float popDuration = 0.16f;
    [SerializeField] private float successSuckDuration = 0.45f;
    [SerializeField] private float successShrinkDuration = 0.14f;
    [SerializeField] private float failApproachDuration = 0.28f;
    [SerializeField] private float failFlyDuration = 0.45f;

    private GameManager gameManager;
    private AirPurifierController airPurifier;
    private bool isBusy;

    public bool IsBusy => isBusy;

    public void Configure(GameManager manager, AirPurifierController purifier)
    {
        gameManager = manager;
        airPurifier = purifier;
    }

    public void TrySuck(ItemController target)
    {
        if (isBusy || target == null || airPurifier == null)
        {
            return;
        }

        target.MarkResolving();
        isBusy = true;

        var success = target.Data.RequiredLevel <= gameManager.CurrentSuctionLevel;
        if (success)
        {
            PlaySuccess(target, () => Complete(target, true));
        }
        else
        {
            PlayFailure(target, () => Complete(target, false));
        }
    }

    private void PlaySuccess(ItemController item, Action onComplete)
    {
        airPurifier.PlaySuctionAnimation();
        var rect = item.RectTransform;
        var start = rect.anchoredPosition;
        rect.localScale = Vector3.one;

        var sequence = DOTween.Sequence();
        sequence.Append(rect.DOAnchorPos(start + new Vector2(0f, popHeight), popDuration).SetEase(Ease.OutQuad));
        sequence.Append(rect.DOAnchorPos(airPurifier.SuctionPoint, successSuckDuration).SetEase(Ease.InBack));
        sequence.Join(rect.DORotate(new Vector3(0f, 0f, 360f), successSuckDuration, RotateMode.FastBeyond360));
        sequence.Append(rect.DOScale(0.05f, successShrinkDuration).SetEase(Ease.InQuad));
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    private void PlayFailure(ItemController item, Action onComplete)
    {
        var rect = item.RectTransform;
        var start = rect.anchoredPosition;
        rect.localScale = Vector3.one;

        var front = airPurifier.SuctionPoint + new Vector2(0f, 115f);
        var flyDirection = start.x >= 0f ? 1f : -1f;
        var flyTarget = front + new Vector2(420f * flyDirection, 120f);

        var sequence = DOTween.Sequence();
        sequence.Append(rect.DOAnchorPos(start + new Vector2(0f, popHeight), popDuration).SetEase(Ease.OutQuad));
        sequence.Append(rect.DOAnchorPos(front, failApproachDuration).SetEase(Ease.OutCubic));
        sequence.AppendCallback(() => airPurifier.PlayFailAnimation());
        sequence.Append(rect.DOAnchorPos(flyTarget, failFlyDuration).SetEase(Ease.OutBack));
        sequence.Join(rect.DORotate(new Vector3(0f, 0f, 540f * flyDirection), failFlyDuration, RotateMode.FastBeyond360));
        sequence.Join(rect.DOScale(0.85f, failFlyDuration));
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    private void Complete(ItemController item, bool success)
    {
        isBusy = false;
        gameManager.ResolveSuction(item, success);
    }
}
