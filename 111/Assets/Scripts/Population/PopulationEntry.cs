// Assets/Scripts/Population/PopulationEntry.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PopulationEntry
{
    [Header("基本信息")]
    public string name;
    public string gender;
    public string birthday;
    public string idNumber;
    public string phone;
    public string address;

    [Header("身份标签（可多个）")]
    public List<string> identities = new List<string>();

    [Header("照片")]
    public Sprite photo;

    [Header("备注标记（如失信被执行人）")]
    public List<string> remarks = new List<string>();

    [Header("是否重名")]
    public bool hasDuplicate = false;

    [Header("触发线索（查到此人时触发，留空=不触发）")]
    public string triggerClueId;
}