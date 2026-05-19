// Assets/Scripts/Transport/TransportSystem.cs
using UnityEngine;
using System.Collections.Generic;

public class TransportSystem : MonoBehaviour
{
    public static TransportSystem Instance { get; private set; }

    public TransportDatabase database;

    void Awake() => Instance = this;

    /// <summary>
    /// 查询班次
    /// </summary>
    public List<TrainEntry> QueryTrain(string departure, string arrival,
                                       string date, string type)
    {
        var results = new List<TrainEntry>();
        foreach (var entry in database.entries)
        {
            bool matchDep = entry.departureStation == departure.Trim();
            bool matchArr = entry.arrivalStation == arrival.Trim();
            bool matchDate = entry.date == date.Trim();
            bool matchType = type == "全部" || entry.transportType == type;

            if (matchDep && matchArr && matchDate && matchType)
                results.Add(entry);
        }
        return results;
    }

    /// <summary>
    /// 查询乘客
    /// </summary>
    public PassengerEntry QueryPassenger(TrainEntry train, string passengerName)
    {
        foreach (var p in train.passengers)
        {
            if (p.name == passengerName.Trim())
                return p;
        }
        return null;
    }

    /// <summary>
    /// 脱敏处理身份证号
    /// </summary>
    public string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return idNumber;
        return idNumber.Substring(0, 6) + "********" + idNumber.Substring(14);
    }

    /// <summary>
    /// 触发线索
    /// </summary>
    public void TriggerClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        GameManager.Instance?.AddClue(clueId);
        NewClueNotification.Instance?.ShowNotification("新线索已添加");
    }
}