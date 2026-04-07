using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform handle;

    public Vector2 inputVector;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out pos
        );
        if (pos.magnitude > background.sizeDelta.x / 2)
        
        {
            pos = pos.normalized * (background.sizeDelta.x / 2);
        }

        pos.x = pos.x / (background.sizeDelta.x / 2);
        pos.y = pos.y / (background.sizeDelta.y / 2);

        inputVector = new Vector2(pos.x, pos.y);
        inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

        float radius = (background.sizeDelta.x - handle.sizeDelta.x) / 2;

        handle.anchoredPosition = new Vector2(
            inputVector.x * radius,
            inputVector.y * radius
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
