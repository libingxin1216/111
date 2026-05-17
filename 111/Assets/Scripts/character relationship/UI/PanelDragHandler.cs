using UnityEngine;
using UnityEngine.EventSystems;

public class PanelDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler
{
    public void OnBeginDrag(PointerEventData e)
    {
        SelectionPanel.Instance.OnBeginDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        SelectionPanel.Instance.OnDrag(e);
    }
}