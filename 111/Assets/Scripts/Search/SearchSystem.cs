// Assets/Scripts/Search/SearchSystem.cs
using UnityEngine;
using System.Collections.Generic;

public class SearchSystem : MonoBehaviour
{
    public static SearchSystem Instance { get; private set; }

    [Header("数据库")]
    public SearchDatabase searchDatabase;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 执行搜索，返回匹配结果列表
    /// </summary>
    public List<SearchEntry> Search(string keyword)
    {
        // 从ClueSystem获取已解锁线索ID集合
        var collectedClues = new HashSet<string>();
        if (ClueSystem.Instance != null)
        {
            var unlocked = ClueSystem.Instance.GetUnlockedClues();
            foreach (var clue in unlocked)
                collectedClues.Add(clue.clueId);
        }

        return searchDatabase.Query(keyword, collectedClues);
    }

    /// <summary>
    /// 玩家打开某条详情页后调用
    /// 处理触发线索、触发照片等副作用
    /// </summary>
    public void OnDetailPageOpened(SearchEntry entry)
    {
        // 触发新线索，通过ClueSystem解锁，内部会同步到GameManager
        if (!string.IsNullOrEmpty(entry.triggerClueId))
        {
            ClueSystem.Instance?.UnlockClue(entry.triggerClueId);
            NewClueNotification.Instance?.ShowNotification("新线索已添加");
        }

        // 触发人物照片解锁
        if (!string.IsNullOrEmpty(entry.triggerPhotoForCharacterId))
        {
            GameManager.Instance?.UnlockCharacterPhoto(
                entry.triggerPhotoForCharacterId,
                entry.detailImages.Count > 0 ? entry.detailImages[0] : null
            );
        }
    }
}