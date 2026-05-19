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
        var collectedClues = GameManager.Instance?.CollectedClues
                             ?? new HashSet<string>();
        return searchDatabase.Query(keyword, collectedClues);
    }

    /// <summary>
    /// 玩家打开某条详情页后调用
    /// 处理触发线索、触发照片等副作用
    /// </summary>
    public void OnDetailPageOpened(SearchEntry entry)
    {
        // 触发新线索
        if (!string.IsNullOrEmpty(entry.triggerClueId))
        {
            GameManager.Instance?.AddClue(entry.triggerClueId);
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