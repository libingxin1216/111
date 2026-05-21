// Assets/Scripts/Population/PopulationSystem.cs
using UnityEngine;

public class PopulationSystem : MonoBehaviour
{
    public static PopulationSystem Instance { get; private set; }

    public PopulationDatabase database;

    void Awake() => Instance = this;

    public QueryResult Query(string name, string phone, string idNumber)
    {
        return database.Query(name, phone, idNumber);
    }

    public void TriggerClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId)) return;
        GameManager.Instance?.AddClue(clueId);
        NewClueNotification.Instance?.ShowNotification("新线索已添加");
    }
}