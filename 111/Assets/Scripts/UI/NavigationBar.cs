// Assets/Scripts/UI/NavigationBar.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NavigationBar : MonoBehaviour
{
    public static NavigationBar Instance { get; private set; }

    [Header("导航按钮")]
    public Button btnRelation;   // 人物关系图
    public Button btnClue;       // 线索
    public Button btnComputer;   // 电脑
    public Button btnHome;       // 主界面(HOME)
    public Button btnNotebook;   // 笔记本（叠加层，不切场景）
    public Button btnSettings;   // 设置

    [Header("红点提示")]
    public GameObject clueBadge; // 线索按钮上的红点GameObject

    [Header("激活状态图片")]
    // 每个按钮对应的激活底座图片，按顺序对应上面6个按钮
    public Image[] buttonHighlights;

    [Header("笔记本叠加层")]
    public GameObject notebookOverlayPanel; // 笔记本Panel的GameObject

    private bool isNotebookOpen = false;

    // 场景名称常量
    const string SCENE_MAIN = "MainScene";
    const string SCENE_COMPUTER = "ComputerScene";
    const string SCENE_RELATION = "RelationScene";
    const string SCENE_CLUE = "ClueScene";

    void Awake()
    {
        // 注意：导航栏每个场景都有一个实例，不做DontDestroyOnLoad
        // 用静态Instance供外部调用当前场景的导航栏
        Instance = this;
    }

    void Start()
    {
        btnRelation.onClick.AddListener(() => NavigateTo(SCENE_RELATION));
        btnClue.onClick.AddListener(OnClickClue);
        btnComputer.onClick.AddListener(OnClickComputer);
        btnHome.onClick.AddListener(() => NavigateTo(SCENE_MAIN));
        btnNotebook.onClick.AddListener(OnClickNotebook);
        btnSettings.onClick.AddListener(OnClickSettings);

        // 根据当前场景高亮对应按钮
        UpdateHighlight();

        // 同步红点状态
        UpdateClueBadge(GameManager.Instance?.HasNewClue ?? false);

        // 电脑场景隐藏导航栏（文档要求）
        if (SceneManager.GetActiveScene().name == SCENE_COMPUTER)
            gameObject.SetActive(false);
    }

    void NavigateTo(string sceneName)
    {
        if (SceneManager.GetActiveScene().name == sceneName) return;
        SceneTransitionManager.Instance.GoToScene(sceneName);
    }

    void OnClickClue()
    {
        GameManager.Instance?.ClearClueBadge();
        NavigateTo(SCENE_CLUE);
    }

    void OnClickComputer()
    {
        // 进入电脑场景后导航栏会自动隐藏（见Start逻辑）
        NavigateTo(SCENE_COMPUTER);
    }

    void OnClickNotebook()
    {
        isNotebookOpen = !isNotebookOpen;
        if (notebookOverlayPanel != null)
            notebookOverlayPanel.SetActive(isNotebookOpen);
    }

    void OnClickSettings()
    {
        // TODO: 弹出设置菜单（后续实现）
        Debug.Log("设置菜单（待实现）");
    }

    /// <summary>
    /// 更新线索红点显示
    /// </summary>
    public void UpdateClueBadge(bool show)
    {
        if (clueBadge != null)
            clueBadge.SetActive(show);
    }

    /// <summary>
    /// 根据当前场景高亮对应导航按钮
    /// </summary>
    void UpdateHighlight()
    {
        if (buttonHighlights == null || buttonHighlights.Length < 4) return;

        // 先全部关掉
        foreach (var img in buttonHighlights)
            if (img != null) img.enabled = false;

        string current = SceneManager.GetActiveScene().name;
        int activeIndex = current switch
        {
            SCENE_RELATION => 0,
            SCENE_CLUE => 1,
            SCENE_COMPUTER => 2,
            SCENE_MAIN => 3,
            _ => -1
        };

        if (activeIndex >= 0 && activeIndex < buttonHighlights.Length)
            buttonHighlights[activeIndex].enabled = true;
    }
}