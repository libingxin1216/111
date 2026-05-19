using System.Collections.Generic;
using UnityEngine;

public class LineManager : MonoBehaviour
{
    [SerializeField] private UILineDrawer lineDrawer;
    [SerializeField] private RelationshipData relationshipData;

    private Dictionary<string, RectTransform> cardRects = new();

    void Awake()
    {
        // 不要调用 EventBus.Clear()，只注册自己的监听
        EventBus.On("OnCardMoved", OnCardMoved);
        EventBus.On("OnCardsReady", OnCardsReady);
    }

    void OnDestroy()
    {
        EventBus.Off("OnCardMoved", OnCardMoved);
        EventBus.Off("OnCardsReady", OnCardsReady);
    }

    private void OnCardMoved(object obj) => lineDrawer.Refresh();
    private void OnCardsReady(object obj) => BuildLines();

    public void RegisterCard(string id, RectTransform rect)
    {
        cardRects[id] = rect;
    }

    void BuildLines()
    {
        if (relationshipData == null) return;

        lineDrawer.lines.Clear();
        foreach (var rel in relationshipData.lines)
        {
            if (cardRects.TryGetValue(rel.fromId, out var from) &&
                cardRects.TryGetValue(rel.toId, out var to))
            {
                lineDrawer.lines.Add(new UILineDrawer.LineEntry
                {
                    from = from,
                    to = to,
                    color = rel.lineColor,
                    width = 4f
                });
            }
        }
        lineDrawer.Refresh();
    }
}