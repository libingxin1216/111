using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TransportUI : MonoBehaviour
{
    [Header("两个Panel")]
    public GameObject trainQueryPanel;
    public GameObject passengerQueryPanel;

    [Header("班次查询Panel内")]
    public TMP_InputField departureInput;
    public TMP_InputField arrivalInput;
    public TMP_InputField dateInput;
    public Button[] typeButtons;
    public Button searchButton;
    public GameObject loadingIndicator;
    public GameObject noResultPanel;
    public Button noResultReturnButton;      // ← 新增：NoResultPanel上的返回键

    [Header("乘客查询Panel内")]
    public Button returnButton;
    public Transform trainListArea;
    public GameObject trainButtonPrefab;
    public GameObject passengerSearchBar;
    public TMP_InputField passengerInput;
    public Button passengerSearchButton;
    public GameObject passengerResultArea;
    public Text passengerResultText;
    public GameObject passengerNoResultPanel;
    public Button passengerNoResultReturnButton; // ← 新增：PassengerNoResultPanel上的返回键

    private string selectedType = "";           // ← 改为空字符串，强制玩家选择
    private int selectedTypeIndex = -1;         // ← 记录当前选中的按钮index，-1=未选
    private TrainEntry selectedTrain = null;
    private TrainButtonItem selectedTrainButton = null;

    void Start()
    {
        trainQueryPanel.SetActive(true);
        passengerQueryPanel.SetActive(false);
        searchButton.interactable = false;
        loadingIndicator.SetActive(false);
        noResultPanel.SetActive(false);
        passengerSearchBar.SetActive(false);
        passengerResultArea.SetActive(false);
        passengerNoResultPanel.SetActive(false);

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

        // 初始化所有类型按钮为灰色（未选中状态）
        foreach (var btn in typeButtons)
            btn.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1f);

        searchButton.onClick.AddListener(OnClickSearch);
        returnButton.onClick.AddListener(OnClickReturn);

        // NoResultPanel返回键
        noResultReturnButton.onClick.AddListener(() =>
        {
            noResultPanel.SetActive(false);
        });

        // PassengerNoResultPanel返回键
        passengerNoResultReturnButton.onClick.AddListener(() =>
        {
            passengerNoResultPanel.SetActive(false);
        });

        // 乘客查询：按钮 + 回车键
        passengerSearchButton.onClick.AddListener(OnClickPassengerSearch);
        passengerInput.onSubmit.AddListener(_ => OnClickPassengerSearch());
    }

    void UpdateSearchButton()
    {
        // 四项都必须填写，且必须选择了出行方式
        searchButton.interactable =
            !string.IsNullOrWhiteSpace(departureInput.text) &&
            !string.IsNullOrWhiteSpace(arrivalInput.text) &&
            !string.IsNullOrWhiteSpace(dateInput.text) &&
            selectedTypeIndex != -1;             // ← 必须选了出行方式
    }

    void OnSelectType(string type, int index)
    {
        selectedType = type;
        selectedTypeIndex = index;

        // 高亮选中，其余灰色
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
            foreach (Transform child in trainListArea)
                Destroy(child.gameObject);

            selectedTrain = null;
            selectedTrainButton = null;
            passengerSearchBar.SetActive(false);
            passengerResultArea.SetActive(false);
            passengerNoResultPanel.SetActive(false);

            foreach (var train in results)
            {
                var item = Instantiate(trainButtonPrefab, trainListArea);
                var itemUI = item.GetComponent<TrainButtonItem>();
                itemUI.Setup(train, OnSelectTrain);
                TransportSystem.Instance.TriggerClue(train.triggerClueId);
            }

            trainQueryPanel.SetActive(false);
            passengerQueryPanel.SetActive(true);
        }
    }

    void OnSelectTrain(TrainEntry train, TrainButtonItem buttonItem)
    {
        selectedTrainButton?.SetHighlight(false);
        selectedTrain = train;
        selectedTrainButton = buttonItem;
        selectedTrainButton.SetHighlight(true);

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
        passengerQueryPanel.SetActive(false);
        trainQueryPanel.SetActive(true);
    }
}