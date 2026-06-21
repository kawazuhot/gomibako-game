using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FloatingScoreText : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    public void Play(int score, Vector2 anchoredPosition)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (text == null)
        {
            text = GetComponent<Text>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        var startOffset = new Vector2(Random.Range(-18f, 18f), Random.Range(4f, 18f));
        var moveOffset = new Vector2(Random.Range(-18f, 18f), Random.Range(54f, 76f));

        rectTransform.anchoredPosition = anchoredPosition + startOffset;
        rectTransform.localScale = Vector3.one * 0.6f;

        text.text = $"+{score}";
        text.raycastTarget = false;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOScale(1.22f, 0.14f).SetEase(Ease.OutBack));
        sequence.Append(rectTransform.DOScale(1.0f, 0.10f).SetEase(Ease.OutQuad));
        sequence.Join(rectTransform.DOAnchorPos(rectTransform.anchoredPosition + moveOffset, 0.58f).SetEase(Ease.OutCubic));
        sequence.Append(canvasGroup.DOFade(0f, 0.26f).SetEase(Ease.InQuad));
        sequence.OnComplete(() => Destroy(gameObject));
    }
}
