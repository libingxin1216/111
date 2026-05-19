// Assets/Scripts/Search/SearchEntry.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SearchEntry
{
    [Header("触发词")]
    public List<string> triggerWords = new List<string>();
    // 可填多个同义词，如"钟德发"、"老赖钟德发"都触发同一条

    [Header("匹配方式")]
    public MatchType matchType = MatchType.Exact;
    // Exact=精确匹配，Contains=包含匹配

    [Header("是否为干扰项/彩蛋")]
    public bool isDistractor = false;
    // 《》内的干扰项标记，逻辑一样，只是策划区分用

    [Header("搜索结果列表标题（蓝色可点击）")]
    public string resultTitle = "";

    [Header("详情页正文（支持换行）")]
    [TextArea(5, 20)]
    public string detailContent = "";

    [Header("详情页图片（可选）")]
    public List<Sprite> detailImages = new List<Sprite>();

    [Header("前置解锁条件（线索ID，留空=始终可见）")]
    public List<string> requiredClueIds = new List<string>();

    [Header("触发新线索（留空=不触发）")]
    public string triggerClueId = "";

    [Header("触发后给予照片（人物ID，留空=不触发）")]
    public string triggerPhotoForCharacterId = "";
}

public enum MatchType
{
    Exact,    // 精确匹配
    Contains  // 包含匹配
}