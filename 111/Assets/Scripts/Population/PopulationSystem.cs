// Assets/Scripts/Population/PopulationSystem.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 人口/数据库查询系统。
/// ── 变更摘要 ──────────────────────────────────────────────────────────
/// TriggerClue 在解锁线索后等待 0.5s 再弹出通知条（符合"数据库结果
/// 出现约 0.5 秒后弹出绿色通知条"的需求）。
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class PopulationSystem : MonoBehaviour
{
    public static PopulationSystem Instance { get; private set; }

    public PopulationDatabase database;

    void Awake() => Instance = this;

    public QueryResult Query(string name, string phone, string idNumber)
        => database.Query(name, phone, idNumber);

    /// <summary>
    /// 数据库查询后触发线索：立即解锁，0.5s 后推送通知 + 导航红点。
    /// </summary>
    public void TriggerClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        GameManager.Instance?.AddClue(clueId);
        StartCoroutine(DelayedNotify("新线索已添加"));
    }

    IEnumerator DelayedNotify(string message)
    {
        yield return new WaitForSeconds(0.5f);
        NewClueNotification.Instance?.ShowNotification(message);
        // ShowNotification 内部已同步更新红点（见更新后的 NewClueNotification.cs）
    }
}
