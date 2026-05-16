using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasDragHandler canvasDragHandler;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasDragHandler = GetComponentInParent<CanvasDragHandler>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        canvasDragHandler?.SetCardDragging(true);
        // 拖动时置顶
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        rectTransform.anchoredPosition += e.delta / canvas.scaleFactor;
        // 通知连线系统重绘
        EventBus.Emit("OnCardMoved", null);
    }

    public void OnEndDrag(PointerEventData e)
    {
        canvasDragHandler?.SetCardDragging(false);
    }
}