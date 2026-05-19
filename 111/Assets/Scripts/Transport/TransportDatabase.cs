// Assets/Scripts/Transport/TransportDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TransportDatabase", menuName = "GameData/TransportDatabase")]
public class TransportDatabase : ScriptableObject
{
    public List<TrainEntry> entries = new List<TrainEntry>();
}

[System.Serializable]
public class TrainEntry
{
    [Header("班次信息")]
    public string trainNumber;      // 班次号，如"G5580"
    public string departureStation; // 出发站
    public string arrivalStation;   // 到达站
    public string date;             // 日期，如"2014-08-14"
    public string transportType;    // 交通工具类型：高铁/普铁/飞机/汽车
    public string departureTime;    // 出发时间
    public string arrivalTime;      // 到达时间

    [Header("乘客名单")]
    public List<PassengerEntry> passengers = new List<PassengerEntry>();

    [Header("触发线索（查到班次时触发，留空=不触发）")]
    public string triggerClueId;
}

[System.Serializable]
public class PassengerEntry
{
    public string name;             // 乘客姓名
    public string seatNumber;       // 座位号
    public string phone;            // 联系电话
    public string idNumber;         // 身份证号（完整）
    public bool showFullId;         // false=脱敏显示，true=完整显示

    [Header("触发线索（查到此乘客时触发，留空=不触发）")]
    public string triggerClueId;
}