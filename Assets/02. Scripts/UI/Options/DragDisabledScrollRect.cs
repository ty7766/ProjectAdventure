public class DragDisabledScrollRect : UnityEngine.UI.ScrollRect
{
    public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Do nothing to disable dragging by default

        if(PlatformCapability.UseTouchUI)
        {
            //Allow dragging if the target platform is using touch UI (e.g., mobile devices)
            base.OnBeginDrag(eventData);
        }
    }
    public override void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Do nothing to disable dragging by default

        if (PlatformCapability.UseTouchUI)
        {
            //Allow dragging if the target platform is using touch UI (e.g., mobile devices)
            base.OnDrag(eventData);
        }
    }
    public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // Do nothing to disable dragging by default

        if (PlatformCapability.UseTouchUI)
        {
            //Allow dragging if the target platform is using touch UI (e.g., mobile devices)
            base.OnEndDrag(eventData);
        }
    }
}