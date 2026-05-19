// Assets/Scripts/Search/SearchResultEntry.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SearchResultEntry : MonoBehaviour
{
    [Header("UI组件")]
    public Text titleText;
    public Text bodyText;
    public Transform imageContainer;
    public GameObject imagePrefab;

    public void Setup(SearchEntry entry)
    {
        titleText.text = entry.resultTitle;
        bodyText.text = entry.detailContent;

        foreach (Transform child in imageContainer)
            Destroy(child.gameObject);

        bool hasImages = entry.detailImages != null && entry.detailImages.Count > 0;
        imageContainer.gameObject.SetActive(hasImages);

        foreach (var sprite in entry.detailImages)
        {
            if (sprite == null) continue;
            var imgObj = Instantiate(imagePrefab, imageContainer);
            imgObj.GetComponent<Image>().sprite = sprite;
            imgObj.GetComponent<Image>().preserveAspect = true;
        }
    }
}