using UnityEngine;
using System.Collections.Generic;

public class ClueSystem : MonoBehaviour
{
    public static ClueSystem Instance;

    [SerializeField] private ClueData[] allClues;

    private HashSet<string> unlockedClueIds = new HashSet<string>();

    void Awake()
    {
        Instance = this;
    }

    // 正常解锁流程，有前置条件检查
    public void UnlockClue(string clueId)
    {
        if (unlockedClueIds.Contains(clueId)) return;

        var clue = System.Array.Find(allClues, c => c.clueId == clueId);
        if (clue == null) return;

        if (!string.IsNullOrEmpty(clue.prerequisiteClueId)
            && !unlockedClueIds.Contains(clue.prerequisiteClueId)) return;

        unlockedClueIds.Add(clueId);

        // 同时存入GameManager持久化
        GameManager.Instance?.AddClueToSave(clueId);

        EventBus.Emit("OnClueUnlocked", clueId);

        CheckChainUnlock(clueId);
    }

    // 恢复流程，跨场景用，跳过前置条件检查，不重复发事件
    public void RestoreClue(string clueId)
    {
        if (unlockedClueIds.Contains(clueId)) return;

        var clue = System.Array.Find(allClues, c => c.clueId == clueId);
        if (clue == null) return;

        unlockedClueIds.Add(clueId);
        // 注意：这里不发EventBus事件，由GameManager统一刷新面板
    }

    void CheckChainUnlock(string justUnlockedId)
    {
        foreach (var clue in allClues)
            if (clue.prerequisiteClueId == justUnlockedId)
                UnlockClue(clue.clueId);
    }

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