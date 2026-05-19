// Assets/Scripts/Transport/TransportUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TransportUI : MonoBehaviour
{
    [Header("两个Panel")]
    public GameObject trainQueryPanel;      // 班次查询Panel
    public GameObject passengerQueryPanel;  // 乘客查询Panel

    [Header("班次查询Panel内")]
    public TMP_InputField departureInput;
    public TMP_InputField arrivalInput;
    public TMP_InputField dateInput;
    public Button[] typeButtons;
    public Button searchButton;
    public GameObject loadingIndicator;
    public GameObject noResultPanel;

    [Header("乘客查询Panel内")]
    public Button returnButton;
    public Transform trainListArea;          // 班次列表容器
    public GameObject trainButtonPrefab;     // 班次按钮预制体
    public GameObject passengerSearchBar;    // 默认隐藏
    public TMP_InputField passengerInput;
    public Button passengerSearchButton;
    public GameObject passengerResultArea;   // 默认隐藏
    public Text passengerResultText;
    public GameObject passengerNoResultPanel;// 默认隐藏

    private string selectedType = "全部";
    private TrainEntry selectedTrain = null;
    private TrainButtonItem selectedTrainButton = null;

    void Start()
    {
        // 初始状态
        trainQueryPanel.SetActive(true);
        passengerQueryPanel.SetActive(false);
        searchButton.interactable = false;
        loadingIndicator.SetActive(false);
        noResultPanel.SetActive(false);
        passengerSearchBar.SetActive(false);
        passengerResultArea.SetActive(false);
        passengerNoResultPanel.SetActive(false);

        // 输入框监听
        departureInput.onValueChanged.AddListener(_ => UpdateSearchButton());
        arrivalInput.onValueChanged.AddListener(_ => UpdateSearchButton());
        dateInput.onValueChanged.AddListener(_ => UpdateSearchButton());

        dateInput.placeholder.GetComponent<TextMeshProUGUI>().text =
            "请输入日期（如2014-08-14）";

        // 交通工具类型按钮
        string[] types = { "全部", "高铁", "普铁", "飞机", "汽车" };
        for (int i = 0; i < typeButtons.Length; i++)
        {
            int index = i;
            typeButtons[i].onClick.AddListener(() => OnSelectType(types[index], index));
        }

        searchButton.onClick.AddListener(OnClickSearch);
        returnButton.onClick.AddListener(OnClickReturn);
        passengerSearchButton.onClick.AddListener(OnClickPassengerSearch);
    }

    void UpdateSearchButton()
    {
        searchButton.interactable =
            !string.IsNullOrWhiteSpace(departureInput.text) &&
            !string.IsNullOrWhiteSpace(arrivalInput.text) &&
            !string.IsNullOrWhiteSpace(dateInput.text);
    }

    void OnSelectType(string type, int index)
    {
        selectedType = type;
        for (int i = 0; i < typeButtons.Length; i++)
        {
            typeButtons[i].GetComponent<Image>().color = (i == index)
                ? new Color(0f, 0.4f, 0.9f, 1f)
                : new Color(0.8f, 0.8f, 0.8f, 1f);
        }
        UpdateSearchButton();
    }

    void OnClickSearch()
    {
        StartCoroutine(SearchCoroutine());
    }

    IEnumerator SearchCoroutine()
    {
        noResultPanel.SetActive(false);
        loadingIndicator.SetActive(true);
        searchButton.interactable = false;

        yield return new WaitForSeconds(1f);

        loadingIndicator.SetActive(false);
        searchButton.interactable = true;

        var results = TransportSystem.Instance.QueryTrain(
            departureInput.text,
            arrivalInput.text,
            dateInput.text,
            selectedType
        );

        if (results.Count == 0)
        {
            noResultPanel.SetActive(true);
        }
        else
        {
            // 清空旧班次列表
            foreach (Transform child in trainListArea)
                Destroy(child.gameObject);

            // 重置乘客查询区域
            selectedTrain = null;
            selectedTrainButton = null;
            passengerSearchBar.SetActive(false);
            passengerResultArea.SetActive(false);
            passengerNoResultPanel.SetActive(false);

            // 生成班次按钮
            foreach (var train in results)
            {
                var item = Instantiate(trainButtonPrefab, trainListArea);
                var itemUI = item.GetComponent<TrainButtonItem>();
                itemUI.Setup(train, OnSelectTrain);
                TransportSystem.Instance.TriggerClue(train.triggerClueId);
            }

            // 切换到乘客查询Panel
            trainQueryPanel.SetActive(false);
            passengerQueryPanel.SetActive(true);
        }
    }

    void OnSelectTrain(TrainEntry train, TrainButtonItem buttonItem)
    {
        // 取消上一个高亮
        selectedTrainButton?.SetHighlight(false);

        // 高亮当前选中
        selectedTrain = train;
        selectedTrainButton = buttonItem;
        selectedTrainButton.SetHighlight(true);

        // 显示乘客搜索框，清空旧结果
        passengerSearchBar.SetActive(true);
        passengerInput.text = "";
        passengerResultArea.SetActive(false);
        passengerNoResultPanel.SetActive(false);
    }

    void OnClickPassengerSearch()
    {
        if (selectedTrain == null) return;

        string name = passengerInput.text.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var passenger = TransportSystem.Instance.QueryPassenger(selectedTrain, name);

        if (passenger == null)
        {
            passengerResultArea.SetActive(false);
            passengerNoResultPanel.SetActive(true);
        }
        else
        {
            passengerNoResultPanel.SetActive(false);
            passengerResultArea.SetActive(true);

            string idDisplay = passenger.showFullId
                ? passenger.idNumber
                : TransportSystem.Instance.MaskIdNumber(passenger.idNumber);

            passengerResultText.text =
                $"姓名：{passenger.name}\n" +
                $"座位号：{passenger.seatNumber}\n" +
                $"联系电话：{passenger.phone}\n" +
                $"身份证号：{idDisplay}";

            TransportSystem.Instance.TriggerClue(passenger.triggerClueId);
        }
    }

    void OnClickReturn()
    {
        // 回到班次查询Panel，保留之前输入的内容
        passengerQueryPanel.SetActive(false);
        trainQueryPanel.SetActive(true);
    }
}