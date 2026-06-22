using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class AspectFillImage : MonoBehaviour
{
    [SerializeField] private Image image;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private RectTransform canvasRect;
    private Sprite lastSprite;
    private Vector2 lastTargetSize;

    private void Awake()
    {
        EnsureReferences();
        Apply();
    }

    private void OnEnable()
    {
        EnsureReferences();
        Apply();
    }

    private void LateUpdate()
    {
        EnsureReferences();
        if (image == null || rectTransform == null)
        {
            return;
        }

        var targetRectSize = GetTargetRectSize();
        if (image.sprite == lastSprite && targetRectSize == lastTargetSize)
        {
            return;
        }

        Apply();
    }

    public void Apply()
    {
        EnsureReferences();
        if (image == null || rectTransform == null || image.sprite == null)
        {
            return;
        }

        var targetRectSize = GetTargetRectSize();
        if (targetRectSize.x <= 0f || targetRectSize.y <= 0f)
        {
            return;
        }

        var spriteRect = image.sprite.rect;
        if (spriteRect.width <= 0f || spriteRect.height <= 0f)
        {
            return;
        }

        var parentAspect = targetRectSize.x / targetRectSize.y;
        var spriteAspect = spriteRect.width / spriteRect.height;
        var targetSize = targetRectSize;

        if (spriteAspect > parentAspect)
        {
            targetSize.x = targetRectSize.y * spriteAspect;
        }
        else
        {
            targetSize.y = targetRectSize.x / spriteAspect;
        }

        image.preserveAspect = true;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = targetSize;
        lastSprite = image.sprite;
        lastTargetSize = targetRectSize;
    }

    private void EnsureReferences()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (parentRect == null && rectTransform != null && rectTransform.parent != null)
        {
            parentRect = rectTransform.parent as RectTransform;
        }

        if (canvasRect == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRect = canvas.transform as RectTransform;
            }
        }
    }

    private Vector2 GetTargetRectSize()
    {
        if (canvasRect != null)
        {
            var canvasSize = canvasRect.rect.size;
            if (canvasSize.x > 0f && canvasSize.y > 0f)
            {
                return canvasSize;
            }
        }

        if (parentRect != null)
        {
            var parentSize = parentRect.rect.size;
            if (parentSize.x > 0f && parentSize.y > 0f)
            {
                return parentSize;
            }
        }

        return new Vector2(Screen.width, Screen.height);
    }
}
