using System.Collections.Generic;
using UnityEngine;

public class LineManager : MonoBehaviour
{
    [SerializeField] private UILineDrawer lineDrawer;
    [SerializeField] private RelationshipData relationshipData;

    private Dictionary<string, RectTransform> cardRects = new();

    // 改为 Awake，确保在 CardManager.Start 发送事件前就已监听
    void Awake()
    {
        EventBus.On("OnCardMoved", _ => lineDrawer.Refresh());
        EventBus.On("OnCardsReady", _ => BuildLines());
    }

    void OnDestroy()
    {
        EventBus.Off("OnCardMoved", _ => lineDrawer.Refresh());
        EventBus.Off("OnCardsReady", _ => BuildLines());
    }

    public void RegisterCard(string id, RectTransform rect)
    {
        cardRects[id] = rect;
        Debug.Log("注册卡片：" + id);
    }

    void BuildLines()
    {
        Debug.Log("BuildLines 调用，已注册卡片数：" + cardRects.Count);

        if (relationshipData == null)
        {
            Debug.LogError("RelationshipData 为空，请检查 Inspector 引用");
            return;
        }

        lineDrawer.lines.Clear();

        foreach (var rel in relationshipData.lines)
        {
            bool hasFrom = cardRects.TryGetValue(rel.fromId, out var from);
            bool hasTo = cardRects.TryGetValue(rel.toId, out var to);

            if (hasFrom && hasTo)
            {
                lineDrawer.lines.Add(new UILineDrawer.LineEntry
                {
                    from = from,
                    to = to,
                    color = rel.lineColor,
                    width = 4f
                });
                Debug.Log($"连线成功：{rel.fromId} → {rel.toId}");
            }
            else
            {
                Debug.LogWarning($"连线失败：{rel.fromId}（{(hasFrom ? "找到" : "未找到")}）→ {rel.toId}（{(hasTo ? "找到" : "未找到")}）");
            }
        }

        lineDrawer.Refresh();
        Debug.Log("最终连线数：" + lineDrawer.lines.Count);
    }
}