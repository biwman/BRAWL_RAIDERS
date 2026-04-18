using UnityEngine.EventSystems;

public class ShipInventoryHudSlotDragHandler : UnityEngine.MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ShipInventoryHudUI owner;
    public int slotIndex;

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.BeginSlotDrag(slotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.UpdateSlotDrag(slotIndex, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndSlotDrag(slotIndex, eventData);
    }
}
