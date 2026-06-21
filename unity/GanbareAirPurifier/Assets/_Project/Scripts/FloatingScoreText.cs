using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FloatingScoreText : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text comboText;

    public void Play(int score, int combo, float comboMultiplier, Vector2 anchoredPosition, Font font)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        var hasComboBonus = combo >= 5;
        var comboScale = GetComboDisplayScale(combo);
        scoreText = CreateText("ScoreText", rectTransform, font, $"+{score}", hasComboBonus ? new Vector2(0f, 34f) : Vector2.zero, new Vector2(340f, 70f), 48, new Color(1f, 0.92f, 0.18f));
        if (hasComboBonus)
        {
            var comboFontSize = Mathf.RoundToInt(44f * comboScale);
            comboText = CreateText("ComboText", rectTransform, font, $"{combo}Combo!! ×{comboMultiplier:0.0}", new Vector2(0f, -34f), new Vector2(430f, 78f), comboFontSize, GetComboColor(combo));
            if (combo >= 100)
            {
                comboText.DOColor(new Color(0.12f, 0.95f, 1f), 0.12f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        var startOffset = new Vector2(Random.Range(-18f, 18f), Random.Range(4f, 18f));
        var moveOffset = new Vector2(Random.Range(-18f, 18f), Random.Range(54f, 76f));

        rectTransform.anchoredPosition = anchoredPosition + startOffset;
        rectTransform.localScale = Vector3.one * 0.5f;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var peakScale = hasComboBonus ? Mathf.Min(1.38f, 1.25f + (comboScale - 1f) * 0.18f) : 1.22f;
        var settleScale = hasComboBonus ? Mathf.Min(1.12f, 1f + (comboScale - 1f) * 0.08f) : 1f;
        var sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOScale(peakScale, 0.16f).SetEase(Ease.OutBack));
        sequence.Append(rectTransform.DOScale(settleScale, 0.12f).SetEase(Ease.OutQuad));
        if (hasComboBonus)
        {
            sequence.Append(rectTransform.DORotate(new Vector3(0f, 0f, Random.Range(-4f, 4f)), 0.06f).SetEase(Ease.InOutSine));
            sequence.Append(rectTransform.DORotate(Vector3.zero, 0.08f).SetEase(Ease.InOutSine));
        }
        sequence.Join(rectTransform.DOAnchorPos(rectTransform.anchoredPosition + moveOffset, 0.58f).SetEase(Ease.OutCubic));
        sequence.Append(canvasGroup.DOFade(0f, 0.26f).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            comboText?.DOKill();
            Destroy(gameObject);
        });
    }

    private static Text CreateText(string name, RectTransform parent, Font font, string value, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
        textObject.transform.SetParent(parent, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;

        var outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.10f, 0.22f, 0.52f, 0.95f);
        outline.effectDistance = new Vector2(4f, -4f);

        var shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        shadow.effectDistance = new Vector2(5f, -5f);

        return text;
    }

    private static float GetComboDisplayScale(int combo)
    {
        if (combo >= 100)
        {
            return 1.5f;
        }

        if (combo >= 70)
        {
            return 1.35f;
        }

        if (combo >= 50)
        {
            return 1.25f;
        }

        if (combo >= 40)
        {
            return 1.2f;
        }

        if (combo >= 30)
        {
            return 1.15f;
        }

        if (combo >= 20)
        {
            return 1.1f;
        }

        return 1.0f;
    }

    private static Color GetComboColor(int combo)
    {
        if (combo >= 100)
        {
            return new Color(1f, 0.12f, 0.92f);
        }

        if (combo >= 70)
        {
            return new Color(0.72f, 0.28f, 1f);
        }

        if (combo >= 50)
        {
            return new Color(1f, 0.14f, 0.12f);
        }

        if (combo >= 40)
        {
            return new Color(1f, 0.48f, 0.08f);
        }

        if (combo >= 30)
        {
            return new Color(0.22f, 0.86f, 0.30f);
        }

        if (combo >= 20)
        {
            return new Color(1f, 0.92f, 0.12f);
        }

        return new Color(0.18f, 0.56f, 1f);
    }
}
