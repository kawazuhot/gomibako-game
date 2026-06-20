using UnityEngine;
using UnityEngine.EventSystems;

public class SuctionHoldArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameManager gameManager;

    public void Configure(GameManager manager)
    {
        gameManager = manager;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        gameManager?.BeginSuctionHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        gameManager?.EndSuctionHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameManager?.EndSuctionHold();
    }
}
