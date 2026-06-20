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
    [SerializeField] private Image levelBadgeImage;
    [SerializeField] private Text levelBadgeText;
    [SerializeField] private Outline outline;

    private Tween moveTween;
    private Action<ItemController> onMissed;
    private Vector2 normalSize = new Vector2(150f, 92f);
    private Vector2 normalFillSize = new Vector2(136f, 78f);

    public ItemData Data { get; private set; }
    public bool IsResolving { get; private set; }
    public bool IsAvailable => !IsResolving && gameObject.activeInHierarchy;
    public RectTransform RectTransform => rectTransform;

    public void Configure(RectTransform rect, Image border, Image fill, Text label, Image badgeImage, Text badgeText, Outline outlineEffect)
    {
        rectTransform = rect;
        borderImage = border;
        fillImage = fill;
        labelText = label;
        levelBadgeImage = badgeImage;
        levelBadgeText = badgeText;
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
        var hasSprite = data.Sprite != null;

        if (fillImage != null)
        {
            fillImage.sprite = data.Sprite;
            fillImage.preserveAspect = hasSprite;
            fillImage.color = hasSprite ? Color.white : data.Color;
            fillImage.rectTransform.sizeDelta = normalFillSize * visualScale;
        }

        if (borderImage != null)
        {
            borderImage.color = hasSprite ? new Color(1f, 1f, 1f, 0f) : Color.white;
        }

        if (outline != null)
        {
            outline.enabled = !hasSprite;
        }

        if (labelText != null)
        {
            labelText.text = $"{data.DisplayName}\nLv{data.RequiredLevel}";
            labelText.enabled = !hasSprite;
        }

        ConfigureLevelBadge(data.RequiredLevel, visualScale);
        SetHighlighted(false);
        moveTween?.Kill();
        moveTween = rectTransform.DOAnchorPosX(endX, moveDuration).SetEase(Ease.Linear).OnComplete(() => onMissed?.Invoke(this));
    }

    private void ConfigureLevelBadge(int requiredLevel, float visualScale)
    {
        var badgeColor = GetLevelBadgeColor(requiredLevel);
        if (levelBadgeImage != null)
        {
            levelBadgeImage.enabled = true;
            levelBadgeImage.color = badgeColor;
            var badgeRect = levelBadgeImage.rectTransform;
            badgeRect.sizeDelta = GetBadgeSize(requiredLevel);
            badgeRect.anchoredPosition = new Vector2(normalSize.x * visualScale * 0.38f, normalSize.y * visualScale * 0.36f);
            badgeRect.localRotation = Quaternion.Euler(0f, 0f, -6f);
            badgeRect.SetAsLastSibling();
        }

        if (levelBadgeText != null)
        {
            levelBadgeText.enabled = true;
            levelBadgeText.text = $"★ Lv{requiredLevel}";
            levelBadgeText.color = Color.white;
            levelBadgeText.fontSize = 28;
            levelBadgeText.fontStyle = FontStyle.Bold;
            levelBadgeText.rectTransform.SetAsLastSibling();
        }
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
            if (Data != null && Data.Sprite != null)
            {
                outline.enabled = false;
            }
            else
            {
                outline.effectColor = highlighted ? new Color(1f, 0.88f, 0.12f, 1f) : Color.white;
                outline.effectDistance = highlighted ? new Vector2(9f, -9f) : new Vector2(5f, -5f);
            }
        }

        if (borderImage != null)
        {
            if (Data != null && Data.Sprite != null)
            {
                borderImage.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            borderImage.color = highlighted ? new Color(1f, 0.96f, 0.42f) : Color.white;
        }
    }

    private static Color GetLevelBadgeColor(int requiredLevel)
    {
        if (requiredLevel <= 1)
        {
            return new Color(0.16f, 0.72f, 0.30f, 0.96f);
        }

        if (requiredLevel == 2)
        {
            return new Color(0.10f, 0.48f, 1.00f, 0.96f);
        }

        return new Color(1.00f, 0.36f, 0.08f, 0.96f);
    }

    private static Vector2 GetBadgeSize(int requiredLevel)
    {
        if (requiredLevel <= 1)
        {
            return new Vector2(104f, 50f);
        }

        if (requiredLevel == 2)
        {
            return new Vector2(112f, 54f);
        }

        return new Vector2(120f, 58f);
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
