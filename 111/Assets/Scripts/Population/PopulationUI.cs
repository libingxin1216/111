// Assets/Scripts/Population/PopulationUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PopulationUI : MonoBehaviour
{
    [Header("输入区")]
    public TMP_InputField nameInput;
    public TMP_InputField phoneInput;
    public TMP_InputField idInput;
    public Button searchButton;
    public Button clearNameButton;
    public Button clearPhoneButton;
    public Button clearIdButton;

    [Header("加载")]
    public GameObject loadingIndicator;

    [Header("面板")]
    public GameObject queryArea;            // 查询输入区
    public GameObject resultArea;           // 找到结果时显示
    public GameObject duplicatePanel;       // 重名提示
    public GameObject noResultPanel;        // 无结果提示

    [Header("返回按钮")]
    public Button resultReturnButton;
    public Button duplicateReturnButton;
    public Button noResultReturnButton;

    [Header("结果内容")]
    public Image photoImage;
    public Text nameText;
    public Text genderText;
    public Text birthdayText;
    public Text idNumberText;
    public Text phoneText;
    public Text addressText;
    public Text remarkText;      // 直接显示备注文字

    [Header("底部")]
    public Text queryCountText;

    private int queryCount = 0;

    void Start()
    {
        searchButton.interactable = false;
        loadingIndicator.SetActive(false);
        queryArea.SetActive(true);
        resultArea.SetActive(false);
        duplicatePanel.SetActive(false);
        noResultPanel.SetActive(false);

        // 输入框监听
        nameInput.onValueChanged.AddListener(_ => UpdateSearchButton());
        phoneInput.onValueChanged.AddListener(_ => UpdateSearchButton());
        idInput.onValueChanged.AddListener(_ => UpdateSearchButton());

        // 输入限制
        phoneInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        phoneInput.characterLimit = 11;
        idInput.characterLimit = 18;

        // 清空按钮
        clearNameButton.onClick.AddListener(() => nameInput.text = "");
        clearPhoneButton.onClick.AddListener(() => phoneInput.text = "");
        clearIdButton.onClick.AddListener(() => idInput.text = "");

        // 搜索按钮
        searchButton.onClick.AddListener(OnClickSearch);

        // 返回按钮：隐藏当前面板，回到查询区
        resultReturnButton.onClick.AddListener(ReturnToQuery);
        duplicateReturnButton.onClick.AddListener(ReturnToQuery);
        noResultReturnButton.onClick.AddListener(ReturnToQuery);

        UpdateQueryCount();
    }

    void UpdateSearchButton()
    {
        searchButton.interactable =
            !string.IsNullOrWhiteSpace(nameInput.text) ||
            !string.IsNullOrWhiteSpace(phoneInput.text) ||
            !string.IsNullOrWhiteSpace(idInput.text);

        clearNameButton.gameObject.SetActive(
            !string.IsNullOrWhiteSpace(nameInput.text));
        clearPhoneButton.gameObject.SetActive(
            !string.IsNullOrWhiteSpace(phoneInput.text));
        clearIdButton.gameObject.SetActive(
            !string.IsNullOrWhiteSpace(idInput.text));
    }

    void OnClickSearch()
    {
        StartCoroutine(SearchCoroutine());
    }

    IEnumerator SearchCoroutine()
    {
        queryArea.SetActive(false);
        resultArea.SetActive(false);
        duplicatePanel.SetActive(false);
        noResultPanel.SetActive(false);
        loadingIndicator.SetActive(true);
        searchButton.interactable = false;

        yield return new WaitForSeconds(1f);

        loadingIndicator.SetActive(false);
        searchButton.interactable = true;

        var result = PopulationSystem.Instance.Query(
            nameInput.text,
            phoneInput.text,
            idInput.text
        );

        switch (result.type)
        {
            case QueryResultType.Found:
                ShowResult(result.entries[0]);
                queryCount++;
                UpdateQueryCount();
                PopulationSystem.Instance.TriggerClue(
                    result.entries[0].triggerClueId);
                break;

            case QueryResultType.Duplicate:
                duplicatePanel.SetActive(true);
                break;

            case QueryResultType.NotFound:
                noResultPanel.SetActive(true);
                break;
        }
    }

    void ShowResult(PopulationEntry entry)
    {
        resultArea.SetActive(true);

        // 有照片就显示，没有就隐藏
        if (entry.photo != null)
        {
            photoImage.sprite = entry.photo;
            photoImage.gameObject.SetActive(true);
        }
        else
        {
            photoImage.gameObject.SetActive(false);
        }

        // 基本信息
        nameText.text = entry.name;
        genderText.text = entry.gender;
        birthdayText.text = entry.birthday;
        idNumberText.text = entry.idNumber;
        phoneText.text = entry.phone;
        addressText.text = string.IsNullOrEmpty(entry.address)
            ? "——" : entry.address;

        // 备注直接显示文字
        if (entry.remarks != null && entry.remarks.Count > 0)
        {
            remarkText.text = string.Join("  ", entry.remarks);
            remarkText.gameObject.SetActive(true);
        }
        else
        {
            remarkText.gameObject.SetActive(false);
        }
    }

    void ReturnToQuery()
    {
        resultArea.SetActive(false);
        duplicatePanel.SetActive(false);
        noResultPanel.SetActive(false);
        queryArea.SetActive(true);
    }

    void UpdateQueryCount()
    {
        if (queryCountText != null)
            queryCountText.text = $"查询记录：第{queryCount}条";
    }
}