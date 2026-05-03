public class DragDisabledScrollRect : UnityEngine.UI.ScrollRect
{
    public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Do nothing to disable dragging
    }
    public override void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Do nothing to disable dragging
    }
    public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Do nothing to disable dragging
    }
}