using System.Collections.Generic;
using UnityEngine;

public class LineManager : MonoBehaviour
{
    [SerializeField] private UILineDrawer lineDrawer;
    [SerializeField] private RelationshipData relationshipData;

    private Dictionary<string, RectTransform> cardRects = new();

    void Start()
    {
        EventBus.On("OnCardMoved", _ => lineDrawer.Refresh());
        EventBus.On("OnCardsReady", _ => BuildLines());
    }

    void OnDestroy()
    {
        EventBus.Off("OnCardMoved", _ => lineDrawer.Refresh());
    }

    public void RegisterCard(string id, RectTransform rect)
    {
        cardRects[id] = rect;
    }

    void BuildLines()
    {
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
                    width = 2f
                });
            }
        }
        lineDrawer.Refresh();
    }
}