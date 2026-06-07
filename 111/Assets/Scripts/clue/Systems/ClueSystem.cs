using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 线索解锁系统。
/// ── 变更摘要 ──────────────────────────────────────────────────────────
/// 1. 新增 UnlockClueChain：专供"前置线索联动"调用。
///    联动解锁时只设置 GameManager.HasNewClue = true + 导航红点，
///    不弹通知条（静默解锁，玩家下次打开线索界面才发现）。
/// 2. CheckChainUnlock 改为调用 UnlockClueChain，避免联动触发通知。
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class ClueSystem : MonoBehaviour
{
    public static ClueSystem Instance;

    [SerializeField] private ClueData[] allClues;

    private HashSet<string> unlockedClueIds = new HashSet<string>();

    void Awake()
    {
        Instance = this;

        // 将 Inspector 手动配置的条目与内存中所有已加载的 ClueData 合并，
        // 确保新增资产即使未拖入 Inspector 也能被发现。
        var inMemory = Resources.FindObjectsOfTypeAll<ClueData>();
        var merged = new System.Collections.Generic.HashSet<ClueData>();
        if (allClues != null)
            foreach (var c in allClues)
                if (c != null) merged.Add(c);
        if (inMemory != null)
            foreach (var c in inMemory)
                if (c != null && !string.IsNullOrEmpty(c.clueId)) merged.Add(c);

        allClues = new ClueData[merged.Count];
        merged.CopyTo(allClues);

        if (allClues.Length == 0)
            Debug.LogWarning("[ClueSystem] allClues 为空且内存中找不到任何 ClueData！");
        else
            Debug.Log($"[ClueSystem] allClues 初始化完成，共 {allClues.Length} 条。");
    }

    // ════════════════════════════════════════════════════════════════════
    //  主动解锁（含通知，供同事送达/搜索/数据库路径调用）
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 主动解锁一条线索（校验前置条件）。
    /// 解锁成功后触发 EventBus "OnClueUnlocked" 事件。
    /// 若有子线索满足联动条件，自动静默解锁子线索。
    /// </summary>
    public void UnlockClue(string clueId)
    {
        if (unlockedClueIds.Contains(clueId)) return;

        var clue = System.Array.Find(allClues, c => c.clueId == clueId);
        if (clue == null) return;

        // 检查前置条件
        if (!string.IsNullOrEmpty(clue.prerequisiteClueId)
            && !unlockedClueIds.Contains(clue.prerequisiteClueId)) return;

        unlockedClueIds.Add(clueId);
        GameManager.Instance?.AddClueToSave(clueId);
        EventBus.Emit("OnClueUnlocked", clueId);

        // 触发子线索联动（静默）
        CheckChainUnlock(clueId);
    }

    // ════════════════════════════════════════════════════════════════════
    //  静默联动解锁（不触发通知条，只更新红点）
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 前置线索联动：静默解锁，不弹通知，仅设置导航红点。
    /// 玩家下次打开线索板时会发现新线索（带红点指示器）。
    /// </summary>
    public void UnlockClueChain(string clueId)
    {
        if (unlockedClueIds.Contains(clueId)) return;

        var clue = System.Array.Find(allClues, c => c.clueId == clueId);
        if (clue == null) return;

        // 联动解锁不检查前置（前置已在 CheckChainUnlock 里验证）
        unlockedClueIds.Add(clueId);
        GameManager.Instance?.AddClueToSave(clueId);

        // 仅更新红点，不发通知条
        if (GameManager.Instance != null) GameManager.Instance.HasNewClue = true;
        NavigationBar.Instance?.UpdateClueBadge(true);

        // EventBus 通知 CluePanelController（若当前在线索场景则追加缩略图）
        EventBus.Emit("OnClueUnlocked", clueId);

        // 递归处理更深层联动
        CheckChainUnlock(clueId);
    }

    // ════════════════════════════════════════════════════════════════════
    //  恢复（场景切换后同步）
    // ════════════════════════════════════════════════════════════════════

    /// <summary>跨场景恢复：直接恢复不再触发事件。</summary>
    public void RestoreClue(string clueId)
    {
        if (unlockedClueIds.Contains(clueId)) return;
        var clue = System.Array.Find(allClues, c => c.clueId == clueId);
        if (clue == null) return;
        unlockedClueIds.Add(clueId);
        // 不触发 EventBus，由 GameManager.SyncClueSystemIfExists → RefreshAllClues 统一刷新
    }

    // ════════════════════════════════════════════════════════════════════
    //  联动检查（改调 UnlockClueChain）
    // ════════════════════════════════════════════════════════════════════

    void CheckChainUnlock(string justUnlockedId)
    {
        foreach (var clue in allClues)
            if (clue.prerequisiteClueId == justUnlockedId)
                UnlockClueChain(clue.clueId);
    }

    // ════════════════════════════════════════════════════════════════════
    //  查询
    // ════════════════════════════════════════════════════════════════════

    public bool HasClue(string clueId) => unlockedClueIds.Contains(clueId);

    public List<ClueData> GetUnlockedClues()
    {
        var result = new List<ClueData>();
        foreach (var clue in allClues)
            if (unlockedClueIds.Contains(clue.clueId))
                result.Add(clue);
        return result;
    }
}
