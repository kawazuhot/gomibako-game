using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ItemController : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image borderImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text labelText;
    [SerializeField] private Outline outline;

    private Tween moveTween;
    private Action<ItemController> onMissed;
    private Vector2 normalSize = new Vector2(150f, 92f);

    public ItemData Data { get; private set; }
    public bool IsResolving { get; private set; }
    public bool IsAvailable => !IsResolving && gameObject.activeInHierarchy;
    public RectTransform RectTransform => rectTransform;

    public void Configure(RectTransform rect, Image border, Image fill, Text label, Outline outlineEffect)
    {
        rectTransform = rect;
        borderImage = border;
        fillImage = fill;
        labelText = label;
        outline = outlineEffect;
    }

    public void Initialize(ItemData data, int currentSuctionLevel, Vector2 startPosition, float endX, float moveDuration, Action<ItemController> missedCallback)
    {
        Data = data;
        IsResolving = false;
        onMissed = missedCallback;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        var visualScale = data.BaseScale;
        if (data.RequiredLevel > currentSuctionLevel)
        {
            visualScale *= 1.14f;
        }

        rectTransform.anchoredPosition = startPosition;
        rectTransform.sizeDelta = normalSize * visualScale;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        if (fillImage != null)
        {
            fillImage.sprite = data.Sprite;
            fillImage.preserveAspect = data.Sprite != null;
            fillImage.color = data.Sprite != null ? Color.white : data.Color;
        }

        if (borderImage != null)
        {
            borderImage.color = Color.white;
        }

        if (labelText != null)
        {
            labelText.text = $"{data.DisplayName}\nLv{data.RequiredLevel}";
        }

        SetHighlighted(false);
        moveTween?.Kill();
        moveTween = rectTransform.DOAnchorPosX(endX, moveDuration).SetEase(Ease.Linear).OnComplete(() => onMissed?.Invoke(this));
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        if (moveTween != null)
        {
            moveTween.timeScale = Mathf.Max(0.01f, multiplier);
        }
    }

    public void StopMovement()
    {
        moveTween?.Kill();
        moveTween = null;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outline != null)
        {
            outline.effectColor = highlighted ? new Color(1f, 0.88f, 0.12f, 1f) : Color.white;
            outline.effectDistance = highlighted ? new Vector2(9f, -9f) : new Vector2(5f, -5f);
        }

        if (borderImage != null)
        {
            borderImage.color = highlighted ? new Color(1f, 0.96f, 0.42f) : Color.white;
        }
    }

    public void MarkResolving()
    {
        IsResolving = true;
        SetHighlighted(false);
        StopMovement();
    }

    public void KillTweens()
    {
        moveTween?.Kill();
        rectTransform?.DOKill();
    }
}
