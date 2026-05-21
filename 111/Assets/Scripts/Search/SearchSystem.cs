// Assets/Scripts/Search/SearchSystem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 搜索系统。
/// ── 变更摘要 ──────────────────────────────────────────────────────────
/// 1. OnDetailPageOpened 新增 0.5s 延迟后再触发通知条 + 导航红点
/// 2. 通知触发同时调用 GameManager.HasNewClue + NavigationBar.UpdateClueBadge
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class SearchSystem : MonoBehaviour
{
    public static SearchSystem Instance { get; private set; }

    [Header("数据库")]
    public SearchDatabase searchDatabase;

    void Awake() => Instance = this;

    /// <summary>执行搜索，返回匹配结果列表。</summary>
    public List<SearchEntry> Search(string keyword)
    {
        var collectedClues = new HashSet<string>();
        if (ClueSystem.Instance != null)
            foreach (var clue in ClueSystem.Instance.GetUnlockedClues())
                collectedClues.Add(clue.clueId);

        return searchDatabase.Query(keyword, collectedClues);
    }

    /// <summary>
    /// 搜索详情页加载完成后调用。
    /// 立即解锁线索（CluePanelController 通过 EventBus 响应），
    /// 0.5 秒后显示通知条并更新导航红点。
    /// </summary>
    public void OnDetailPageOpened(SearchEntry entry)
    {
        // 先解锁线索（静默，不触发通知）
        if (!string.IsNullOrEmpty(entry.triggerClueId))
            ClueSystem.Instance?.UnlockClue(entry.triggerClueId);

        // 触发人物照片解锁
        if (!string.IsNullOrEmpty(entry.triggerPhotoForCharacterId))
            GameManager.Instance?.UnlockCharacterPhoto(
                entry.triggerPhotoForCharacterId,
                entry.detailImages.Count > 0 ? entry.detailImages[0] : null);

        // 0.5s 后推送通知 + 红点（要求：详情页加载完约 0.5 秒后）
        if (!string.IsNullOrEmpty(entry.triggerClueId))
            StartCoroutine(DelayedNotify("新线索已添加"));
    }

    IEnumerator DelayedNotify(string message)
    {
        yield return new WaitForSeconds(0.5f);

        // 更新 GameManager 红点状态
        if (GameManager.Instance != null) GameManager.Instance.HasNewClue = true;
        NavigationBar.Instance?.UpdateClueBadge(true);
        NewClueNotification.Instance?.ShowNotification(message);
    }
}
