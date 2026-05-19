// Assets/Scripts/Search/SearchDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SearchDatabase", menuName = "GameData/SearchDatabase")]
public class SearchDatabase : ScriptableObject
{
    public List<SearchEntry> entries = new List<SearchEntry>();

    /// <summary>
    /// 根据玩家输入的关键词查找所有匹配的结果
    /// </summary>
    public List<SearchEntry> Query(string input, HashSet<string> collectedClues)
    {
        var results = new List<SearchEntry>();
        if (string.IsNullOrWhiteSpace(input)) return results;

        string trimmed = input.Trim();

        foreach (var entry in entries)
        {
            // 检查前置条件
            if (!IsUnlocked(entry, collectedClues)) continue;

            // 检查是否匹配
            if (IsMatch(entry, trimmed))
                results.Add(entry);
        }

        return results;
    }

    bool IsMatch(SearchEntry entry, string input)
    {
        foreach (var word in entry.triggerWords)
        {
            if (entry.matchType == MatchType.Exact)
            {
                if (input == word) return true;
            }
            else // Contains
            {
                if (input.Contains(word)) return true;
            }
        }
        return false;
    }

    bool IsUnlocked(SearchEntry entry, HashSet<string> collectedClues)
    {
        if (entry.requiredClueIds == null || entry.requiredClueIds.Count == 0)
            return true;

        foreach (var clueId in entry.requiredClueIds)
        {
            if (!collectedClues.Contains(clueId)) return false;
        }
        return true;
    }
}