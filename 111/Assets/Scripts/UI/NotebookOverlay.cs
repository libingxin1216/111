// Assets/Scripts/UI/NotebookOverlay.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NotebookOverlay : MonoBehaviour
{
    public static NotebookOverlay Instance { get; private set; }

    [Header("UI组件")]
    public Button closeButton;
    public TMP_InputField contentInputField;  // 右侧内容输入框

    [Header("标签栏")]
    public Transform tabListContainer;        // 左侧标签列表容器
    public GameObject tabItemPrefab;          // 标签预制体
    public Button addTabButton;               // "+"按钮

    private int currentTabIndex = 0;          // 当前选中的标签序号
    private Coroutine autoSaveCoroutine;
    private const float AUTO_SAVE_DELAY = 1f;
    private List<NotebookTabItem> tabItems = new List<NotebookTabItem>();
    private bool skipNextSave = false;


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 只绑定一次，不放在OnEnable里
        closeButton.onClick.AddListener(OnClickClose);
        addTabButton.onClick.AddListener(OnClickAddTab);
    }

    void OnEnable()
    {
        // OnEnable里只做数据刷新，不绑定按钮
        RefreshAllTabs();
        contentInputField.onValueChanged.AddListener(OnContentChanged);
    }

    void OnDisable()
    {
        SaveCurrentTab();
        contentInputField.onValueChanged.RemoveAllListeners();
    }

    /// <summary>
    /// 刷新左侧所有标签
    /// </summary>
    void RefreshAllTabs()
    {
        foreach (Transform child in tabListContainer)
            Destroy(child.gameObject);
        tabItems.Clear();

        // ← 新增：如果没有任何标签，先创建默认标签
        if (GameManager.Instance.NotebookTabs.Count == 0)
            GameManager.Instance.NotebookTabs.Add(new NotebookTabData("默认笔记"));

        var tabs = GameManager.Instance.NotebookTabs;
        for (int i = 0; i < tabs.Count; i++)
            CreateTabItem(i, tabs[i].tabName);

        // 确保currentTabIndex合法
        currentTabIndex = Mathf.Clamp(currentTabIndex, 0, tabs.Count - 1);

        SelectTab(currentTabIndex);
    }

    /// <summary>
    /// 创建一个标签UI
    /// </summary>
    void CreateTabItem(int index, string name)
    {
        var item = Instantiate(tabItemPrefab, tabListContainer);
        var tabItem = item.GetComponent<NotebookTabItem>();
        tabItem.Setup(
            index,
            name,
            onSelect: SelectTab,
            onRename: RenameTab,
            onDelete: DeleteTab
        );
        tabItems.Add(tabItem);
    }

    /// <summary>
    /// 切换到指定标签
    /// </summary>
    void SelectTab(int index)
    {
        var tabs = GameManager.Instance.NotebookTabs;

        // ← 新增：防止index越界
        if (tabs == null || tabs.Count == 0 || index < 0 || index >= tabs.Count)
            return;

        SaveCurrentTab();
        currentTabIndex = index;

        for (int i = 0; i < tabItems.Count; i++)
            tabItems[i].SetHighlight(i == currentTabIndex);

        contentInputField.text = tabs[index].content;
    }

    /// <summary>
    /// 保存当前标签内容到GameManager
    /// </summary>
    void SaveCurrentTab()
    {
        // ← 新增：跳过这次保存
        if (skipNextSave)
        {
            skipNextSave = false;
            return;
        }

        var tabs = GameManager.Instance?.NotebookTabs;
        if (tabs == null || currentTabIndex >= tabs.Count) return;
        tabs[currentTabIndex].content = contentInputField.text;
    }

    void OnContentChanged(string newText)
    {
        if (autoSaveCoroutine != null)
            StopCoroutine(autoSaveCoroutine);
        autoSaveCoroutine = StartCoroutine(AutoSaveAfterDelay());
    }

    IEnumerator AutoSaveAfterDelay()
    {
        yield return new WaitForSeconds(AUTO_SAVE_DELAY);
        SaveCurrentTab();
    }

    /// <summary>
    /// 点击"+"新建标签
    /// </summary>
    void OnClickAddTab()
    {
        var tabs = GameManager.Instance.NotebookTabs;
        string newName = $"新建标签{tabs.Count}";
        tabs.Add(new NotebookTabData(newName));

        // 只新增这一个标签UI，不重建全部
        int newIndex = tabs.Count - 1;
        CreateTabItem(newIndex, newName);

        // 切换到新标签
        SelectTab(newIndex);
    }

    /// <summary>
    /// 重命名标签（双击触发）
    /// </summary>
    void RenameTab(int index, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        GameManager.Instance.NotebookTabs[index].tabName = newName;
        tabItems[index].UpdateName(newName);
    }

    /// <summary>
    /// 删除标签（长按后确认删除）
    /// </summary>
    void DeleteTab(int index)
    {
        if (GameManager.Instance.NotebookTabs.Count <= 1) return;

        // ← 先保存所有其他标签的内容（除了要删除的那个）
        // 此时不调用SaveCurrentTab，避免把空内容写进去

        // 直接从GameManager里删除，不经过SaveCurrentTab
        GameManager.Instance.NotebookTabs[index].content = "";
        GameManager.Instance.NotebookTabs.RemoveAt(index);

        // 清空输入框
        contentInputField.text = "";

        currentTabIndex = Mathf.Clamp(index - 1, 0,
                          GameManager.Instance.NotebookTabs.Count - 1);

        // ← RefreshAllTabs里会调用SelectTab，SelectTab会载入正确内容
        // 但SelectTab开头调用了SaveCurrentTab，需要屏蔽掉这次保存
        skipNextSave = true;
        RefreshAllTabs();
    }

    void OnClickClose()
    {
        SaveCurrentTab();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 外部调用：追加文字到当前标签
    /// </summary>
    public void AppendText(string text, string sourceLabel)
    {
        string appendText = $"\n\n【来自{sourceLabel}】\n{text}";
        contentInputField.text += appendText;
        contentInputField.caretPosition = contentInputField.text.Length;
        SaveCurrentTab();
    }
}