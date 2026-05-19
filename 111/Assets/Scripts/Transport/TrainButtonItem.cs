// Assets/Scripts/Transport/TrainButtonItem.cs
using UnityEngine;
using UnityEngine.UI;
using System;

public class TrainButtonItem : MonoBehaviour
{
    public Text trainInfoText;
    public Button button;
    public Image background;

    private TrainEntry entry;
    private Action<TrainEntry, TrainButtonItem> onSelectCallback;

    public void Setup(TrainEntry entry, Action<TrainEntry, TrainButtonItem> callback)
    {
        this.entry = entry;
        this.onSelectCallback = callback;

        trainInfoText.text =
            $"{entry.trainNumber}  |  " +
            $"{entry.departureStation} → {entry.arrivalStation}  |  " +
            $"{entry.departureTime} - {entry.arrivalTime}  |  " +
            $"{entry.transportType}";

        button.onClick.AddListener(() => onSelectCallback?.Invoke(this.entry, this));
    }

    public void SetHighlight(bool highlight)
    {
        background.color = highlight
            ? new Color(0f, 0.4f, 0.9f, 0.3f)   // 蓝色半透明=选中
            : new Color(0.9f, 0.9f, 0.9f, 1f);   // 灰色=未选中
    }
}