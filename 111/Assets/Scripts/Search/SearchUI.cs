// Assets/Scripts/Search/SearchUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SearchUI : MonoBehaviour
{
    [Header("搜索栏")]
    public TMP_InputField searchInputField;
    public Button searchButton;

    [Header("加载提示")]
    public GameObject loadingIndicator;  // "搜索中..."文字，默认隐藏

    [Header("结果区域")]
    public GameObject resultArea;           // 整个结果区域的父物体
    public Transform resultContainer;       // Vertical Layout Group，结果条目放这里
    public GameObject noResultPanel;        // 无结果提示
    public TextMeshProUGUI noResultText;    // 无结果提示文字

    [Header("预制体")]
    public GameObject searchResultEntryPrefab; // 单条完整结果的预制体

    void Start()
    {
        searchButton.onClick.AddListener(OnClickSearch);
        searchInputField.onSubmit.AddListener(_ => OnClickSearch());
        searchInputField.onValueChanged.AddListener(val =>
        {
            searchButton.interactable = !string.IsNullOrWhiteSpace(val);
        });

        // 初始状态
        searchButton.interactable = false;
        loadingIndicator.SetActive(false);
        resultArea.SetActive(false);
        noResultPanel.SetActive(false);
    }

    void OnClickSearch()
    {
        string keyword = searchInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return;
        StartCoroutine(SearchCoroutine(keyword));
    }

    IEnumerator SearchCoroutine(string keyword)
    {
        // 隐藏旧结果，显示加载
        resultArea.SetActive(false);
        noResultPanel.SetActive(false);
        loadingIndicator.SetActive(true);
        searchButton.interactable = false;

        yield return new WaitForSeconds(1f);

        loadingIndicator.SetActive(false);
        searchButton.interactable = true;

        // 执行搜索
        var results = SearchSystem.Instance.Search(keyword);

        // 清空旧结果
        foreach (Transform child in resultContainer)
            Destroy(child.gameObject);

        if (results.Count == 0)
        {
            noResultPanel.SetActive(true);
            noResultText.text =
                $"未找到与\"{keyword}\"相关的结果。\n建议：检查输入是否有误，或尝试其他关键词。";
        }
        else
        {
            resultArea.SetActive(true);
            foreach (var entry in results)
            {
                // 实例化每条结果
                var item = Instantiate(searchResultEntryPrefab, resultContainer);
                var entryUI = item.GetComponent<SearchResultEntry>();
                entryUI.Setup(entry);

                // 触发副作用（线索、照片等）
                SearchSystem.Instance.OnDetailPageOpened(entry);
            }
        }
    }
}