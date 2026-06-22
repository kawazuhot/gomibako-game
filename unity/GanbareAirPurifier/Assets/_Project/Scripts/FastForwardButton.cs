using UnityEngine;
using UnityEngine.EventSystems;

public class FastForwardButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameManager gameManager;

    public void Configure(GameManager manager)
    {
        gameManager = manager;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        gameManager?.SuppressPointerSuckInput();
        gameManager?.SetFastForwardButtonHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        gameManager?.SuppressPointerSuckInput();
        gameManager?.SetFastForwardButtonHeld(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameManager?.SuppressPointerSuckInput();
        gameManager?.SetFastForwardButtonHeld(false);
    }
}
