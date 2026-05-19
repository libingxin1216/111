// Assets/Scripts/Transport/TransportUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TransportUI : MonoBehaviour
{
    [Header("查询条件")]
    public TMP_InputField departureInput;
    public TMP_InputField arrivalInput;
    public TMP_InputField dateInput;
    public Button[] typeButtons;
    public Button searchButton;

    [Header("加载")]
    public GameObject loadingIndicator;

    [Header("结果区域")]
    public GameObject noResultPanel;
    public TextMeshProUGUI noResultText;

    [Header("班次结果（直接平铺）")]
    public GameObject trainResultArea;          // 默认隐藏
    public TextMeshProUGUI trainResultText;     // 直接显示班次信息

    [Header("乘客查询")]
    public GameObject passengerQueryArea;       // 默认隐藏
    public TMP_InputField passengerInput;
    public Button passengerSearchButton;

    [Header("乘客结果（直接平铺）")]
    public GameObject passengerResultArea;      // 默认隐藏
    public TextMeshProUGUI passengerResultText;
    public GameObject passengerNoResultPanel;   // 默认隐藏
    public TextMeshProUGUI passengerNoResultText;

    private string selectedType = "全部";
    private TrainEntry selectedTrain = null;

    void Start()
    {
        searchButton.interactable = false;
        loadingIndicator.SetActive(false);
        noResultPanel.SetActive(false);
        trainResultArea.SetActive(false);
        passengerQueryArea.SetActive(false);
        passengerResultArea.SetActive(false);
        passengerNoResultPanel.SetActive(false);

        departureInput.onValueChanged.AddListener(_ => UpdateSearchButton());
        arrivalInput.onValueChanged.AddListener(_ => UpdateSearchButton());
        dateInput.onValueChanged.AddListener(_ => UpdateSearchButton());

        dateInput.placeholder.GetComponent<TextMeshProUGUI>().text =
            "请输入日期（如2014-08-14）";

        string[] types = { "全部", "高铁", "普铁", "飞机", "汽车" };
        for (int i = 0; i < typeButtons.Length; i++)
        {
            int index = i;
            typeButtons[i].onClick.AddListener(() => OnSelectType(types[index], index));
        }

        searchButton.onClick.AddListener(OnClickSearch);
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
        // 重置所有区域
        trainResultArea.SetActive(false);
        noResultPanel.SetActive(false);
        passengerQueryArea.SetActive(false);
        passengerResultArea.SetActive(false);
        passengerNoResultPanel.SetActive(false);
        selectedTrain = null;

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
            noResultText.text = "未查询到符合条件的班次信息，请检查各项内容是否正确。";
        }
        else
        {
            // 直接把所有班次信息拼成文字显示
            string content = "";
            foreach (var train in results)
            {
                content +=
                    $"班次：{train.trainNumber}\n" +
                    $"类型：{train.transportType}\n" +
                    $"出发站：{train.departureStation}\n" +
                    $"到达站：{train.arrivalStation}\n" +
                    $"出发时间：{train.departureTime}\n" +
                    $"到达时间：{train.arrivalTime}\n";

                // 多条结果之间加分隔
                if (results.Count > 1) content += "\n――――――――――\n\n";

                TransportSystem.Instance.TriggerClue(train.triggerClueId);
            }

            trainResultText.text = content;
            trainResultArea.SetActive(true);

            // 只有一条结果时直接选中，显示乘客查询
            if (results.Count == 1)
            {
                selectedTrain = results[0];
                passengerQueryArea.SetActive(true);
            }
        }
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
            passengerNoResultText.text = "该姓名未在本次班次中查询到实名记录。";
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
}