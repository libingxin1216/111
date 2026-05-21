// Assets/Scripts/UI/NavigationBar.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 导航栏控制器（每个场景各有一个实例）。
/// ── 变更摘要 ──────────────────────────────────────────────────────────
/// 1. UpdateClueBadge(true) 时启动红点脉冲缩放动画
/// 2. UpdateClueBadge(false) 时停止动画并恢复原始大小
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class NavigationBar : MonoBehaviour
{
    public static NavigationBar Instance { get; private set; }

    [Header("导航按钮")]
    public Button btnRelation;
    public Button btnClue;
    public Button btnComputer;
    public Button btnHome;
    public Button btnNotebook;
    public Button btnSettings;

    [Header("红点提示")]
    public GameObject clueBadge;

    [Header("激活状态图片")]
    public Image[] buttonHighlights;

    [Header("笔记本叠加层")]
    public GameObject notebookOverlayPanel;

    // ── 红点动画参数 ───────────────────────────────────────────────────
    [Header("红点脉冲动画")]
    [Tooltip("脉冲峰值缩放倍数（建议 1.25-1.40）")]
    public float badgePulseScale = 1.30f;
    [Tooltip("脉冲周期（秒）")]
    public float badgePulsePeriod = 1.2f;

    private bool    _isNotebookOpen;
    private Coroutine _badgePulseCoroutine;

    const string SCENE_MAIN     = "MainScene";
    const string SCENE_COMPUTER = "ComputerScene";
    const string SCENE_RELATION = "RelationScene";
    const string SCENE_CLUE     = "ClueScene";

    // ════════════════════════════════════════════════════════════════════
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        btnRelation?.onClick.AddListener(() => NavigateTo(SCENE_RELATION));
        btnClue?.onClick.AddListener(OnClickClue);
        btnComputer?.onClick.AddListener(OnClickComputer);
        btnHome?.onClick.AddListener(() => NavigateTo(SCENE_MAIN));
        btnNotebook?.onClick.AddListener(OnClickNotebook);
        btnSettings?.onClick.AddListener(OnClickSettings);

        UpdateHighlight();

        // 同步红点（场景加载后立即反映状态）
        bool hasNew = GameManager.Instance?.HasNewClue ?? false;
        UpdateClueBadge(hasNew);

        // 电脑场景隐藏导航栏
        if (SceneManager.GetActiveScene().name == SCENE_COMPUTER)
            gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  导航
    // ════════════════════════════════════════════════════════════════════
    void NavigateTo(string sceneName)
    {
        if (SceneManager.GetActiveScene().name == sceneName) return;
        SceneTransitionManager.Instance.GoToScene(sceneName);
    }

    void OnClickClue()
    {
        GameManager.Instance?.ClearClueBadge();
        // MainSceneController 上的文件夹红点也要清掉
        FindObjectOfType<MainSceneController>()?.RefreshFolderBadge();
        NavigateTo(SCENE_CLUE);
    }

    void OnClickComputer() => NavigateTo(SCENE_COMPUTER);

    void OnClickNotebook()
    {
        _isNotebookOpen = !_isNotebookOpen;
        if (notebookOverlayPanel != null)
            notebookOverlayPanel.SetActive(_isNotebookOpen);
    }

    void OnClickSettings()
    {
        Debug.Log("设置菜单（待实现）");
    }

    // ════════════════════════════════════════════════════════════════════
    //  红点管理
    // ════════════════════════════════════════════════════════════════════

    /// <summary>显示或隐藏线索红点，并控制脉冲动画。</summary>
    public void UpdateClueBadge(bool show)
    {
        if (clueBadge == null) return;
        clueBadge.SetActive(show);

        if (show)
        {
            if (_badgePulseCoroutine == null)
                _badgePulseCoroutine = StartCoroutine(PulseBadge());
        }
        else
        {
            if (_badgePulseCoroutine != null)
            {
                StopCoroutine(_badgePulseCoroutine);
                _badgePulseCoroutine = null;
            }
            clueBadge.transform.localScale = Vector3.one;
        }
    }

    IEnumerator PulseBadge()
    {
        float half = badgePulsePeriod * 0.5f;
        while (clueBadge != null && clueBadge.activeSelf)
        {
            // 放大
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime / half);
                float s = Mathf.Lerp(1f, badgePulseScale, t * t * (3f - 2f * t));
                if (clueBadge != null) clueBadge.transform.localScale = Vector3.one * s;
                yield return null;
            }
            // 缩小
            t = 0f;
            while (t < 1f)
            {
                t = Mathf.Clamp01(t + Time.deltaTime / half);
                float s = Mathf.Lerp(badgePulseScale, 1f, t * t * (3f - 2f * t));
                if (clueBadge != null) clueBadge.transform.localScale = Vector3.one * s;
                yield return null;
            }
        }
        if (clueBadge != null) clueBadge.transform.localScale = Vector3.one;
        _badgePulseCoroutine = null;
    }

    // ════════════════════════════════════════════════════════════════════
    //  高亮
    // ════════════════════════════════════════════════════════════════════
    void UpdateHighlight()
    {
        if (buttonHighlights == null || buttonHighlights.Length < 4) return;
        foreach (var img in buttonHighlights)
            if (img != null) img.enabled = false;

        string current = SceneManager.GetActiveScene().name;
        int activeIndex = current switch
        {
            SCENE_RELATION => 0,
            SCENE_CLUE     => 1,
            SCENE_COMPUTER => 2,
            SCENE_MAIN     => 3,
            _              => -1
        };
        if (activeIndex >= 0 && activeIndex < buttonHighlights.Length)
            buttonHighlights[activeIndex].enabled = true;
    }
}
