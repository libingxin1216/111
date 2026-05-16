using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform contentRoot;
    private bool isDraggingCard = false;

    public void SetCardDragging(bool val) => isDraggingCard = val;

    public void OnPointerDown(PointerEventData e) { }

    public void OnDrag(PointerEventData e)
    {
        if (isDraggingCard) return;
        // 只响应右键或中键平移画布，左键留给卡片
        if (e.button == PointerEventData.InputButton.Right ||
            e.button == PointerEventData.InputButton.Middle)
        {
            contentRoot.anchoredPosition += e.delta;
        }
    }
}