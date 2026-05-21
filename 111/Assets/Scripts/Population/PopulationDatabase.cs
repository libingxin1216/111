// Assets/Scripts/Population/PopulationDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PopulationDatabase",
                 menuName = "GameData/PopulationDatabase")]
public class PopulationDatabase : ScriptableObject
{
    public List<PopulationEntry> entries = new List<PopulationEntry>();

    /// <summary>
    /// 根据姓名查询
    /// </summary>
    public List<PopulationEntry> QueryByName(string name)
    {
        return entries.FindAll(e => e.name == name.Trim());
    }

    /// <summary>
    /// 根据电话查询
    /// </summary>
    public PopulationEntry QueryByPhone(string phone)
    {
        return entries.Find(e => e.phone == phone.Trim());
    }

    /// <summary>
    /// 根据身份证查询
    /// </summary>
    public PopulationEntry QueryByIdNumber(string idNumber)
    {
        return entries.Find(e => e.idNumber == idNumber.Trim());
    }

    /// <summary>
    /// 综合查询入口
    /// </summary>
    public QueryResult Query(string name, string phone, string idNumber)
    {
        // 电话或身份证优先，精确锁定唯一一人
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var result = QueryByPhone(phone);
            if (result != null) return new QueryResult(QueryResultType.Found,
                                                       new List<PopulationEntry> { result });
            return new QueryResult(QueryResultType.NotFound, null);
        }

        if (!string.IsNullOrWhiteSpace(idNumber))
        {
            var result = QueryByIdNumber(idNumber);
            if (result != null) return new QueryResult(QueryResultType.Found,
                                                       new List<PopulationEntry> { result });
            return new QueryResult(QueryResultType.NotFound, null);
        }

        // 只填了姓名
        if (!string.IsNullOrWhiteSpace(name))
        {
            var results = QueryByName(name);
            if (results.Count == 0)
                return new QueryResult(QueryResultType.NotFound, null);
            if (results.Count == 1 && !results[0].hasDuplicate)
                return new QueryResult(QueryResultType.Found, results);
            // 重名
            return new QueryResult(QueryResultType.Duplicate, null);
        }

        return new QueryResult(QueryResultType.NotFound, null);
    }
}

public enum QueryResultType
{
    Found,      // 找到唯一结果
    Duplicate,  // 重名
    NotFound    // 无结果
}

public class QueryResult
{
    public QueryResultType type;
    public List<PopulationEntry> entries;

    public QueryResult(QueryResultType type, List<PopulationEntry> entries)
    {
        this.type = type;
        this.entries = entries;
    }
}