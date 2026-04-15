using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public float deadZone = 0.15f;
    public RectTransform background;
    public RectTransform handle;

    public Vector2 inputVector;
    public bool IsPressed { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsPressed)
            return;

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

        if (inputVector.magnitude < deadZone)
        {
            inputVector = Vector2.zero;
        }

        float radius = (background.sizeDelta.x - handle.sizeDelta.x) / 2;

        handle.anchoredPosition = new Vector2(
            inputVector.x * radius,
            inputVector.y * radius
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    void OnDisable()
    {
        ResetJoystick();
    }

    void ResetJoystick()
    {
        IsPressed = false;
        inputVector = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
