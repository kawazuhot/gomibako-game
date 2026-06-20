using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TargetMarkerController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RectTransform marker;
    [SerializeField] private RectTransform coordinateRoot;
    [SerializeField] private Vector2 minPosition = new Vector2(-460f, -500f);
    [SerializeField] private Vector2 maxPosition = new Vector2(460f, 560f);

    public void Configure(GameManager manager, RectTransform targetMarker, RectTransform root)
    {
        gameManager = manager;
        marker = targetMarker;
        coordinateRoot = root;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveMarker(eventData);
        gameManager?.BeginSuctionHold();
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveMarker(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        gameManager?.EndSuctionHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.dragging)
        {
            return;
        }

        gameManager?.EndSuctionHold();
    }

    private void MoveMarker(PointerEventData eventData)
    {
        if (marker == null || coordinateRoot == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(coordinateRoot, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            return;
        }

        localPoint.x = Mathf.Clamp(localPoint.x, minPosition.x, maxPosition.x);
        localPoint.y = Mathf.Clamp(localPoint.y, minPosition.y, maxPosition.y);
        marker.anchoredPosition = localPoint;
    }
}
