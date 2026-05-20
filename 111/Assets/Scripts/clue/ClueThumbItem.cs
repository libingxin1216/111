using UnityEngine;
using UnityEngine.UI;
using System;

public class ClueThumbItem : MonoBehaviour
{
    [SerializeField] private Image thumbImage;
    [SerializeField] private GameObject newDotIndicator;

    private ClueData data;

    // 供CluePanelController做重复检测用
    public string ClueId => data != null ? data.clueId : "";

    public void Setup(ClueData clueData, Action<ClueData> onClickCallback)
    {
        data = clueData;

        if (clueData.thumbnail != null)
            thumbImage.sprite = clueData.thumbnail;

        newDotIndicator.SetActive(true);

        GetComponent<Button>().onClick.AddListener(() =>
        {
            newDotIndicator.SetActive(false);
            onClickCallback?.Invoke(data);
        });
    }
}