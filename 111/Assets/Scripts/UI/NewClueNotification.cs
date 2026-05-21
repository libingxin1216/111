// Assets/Scripts/UI/NewClueNotification.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 右下角绿色"新线索已添加"通知条。
/// ── 变更摘要 ──────────────────────────────────────────────────────────
/// 1. 改用 CanvasGroup + RectTransform 做"从右下滑入 + 淡入/淡出"动画
/// 2. ShowNotification 调用时同步设置 NavigationBar 红点 + GameManager.HasNewClue
/// 3. 点击通知条跳转到 ClueScene 并清除红点
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class NewClueNotification : MonoBehaviour
{
    public static NewClueNotification Instance { get; private set; }

    [Header("UI 组件")]
    [Tooltip("通知条根 GameObject（初始 SetActive false）")]
    public GameObject notificationPanel;

    [Tooltip("通知文字（TMP 或旧版 Text 均可；建议改用 TMP）")]
    public TMP_Text notificationTMP;
    public Text     notificationText;   // 旧版 Text fallback

    [Tooltip("通知条上的整体 Button（点击跳转线索板）")]
    public Button notificationButton;

    [Header("动画参数")]
    [Tooltip("滑入起始偏移量（相对于最终停靠位置，正值 = 向右偏移）")]
    public float slideOffsetX = 320f;
    [Tooltip("滑入动画时长（秒）")]
    public float slideInDuration  = 0.35f;
    [Tooltip("滑出动画时长（秒）")]
    public float slideOutDuration = 0.28f;
    [Tooltip("停留时长（秒）")]
    public float stayDuration     = 3f;

    // ── 内部 ──────────────────────────────────────────────────────────
    private RectTransform _rt;
    private CanvasGroup   _cg;
    private Vector2       _shownPos;       // 最终停靠位置（从 Inspector 布局决定）
    private Coroutine     _activeCoroutine;
    private bool          _initialized;

    // ════════════════════════════════════════════════════════════════════
    void Awake()
    {
        Instance = this;
        if (notificationPanel != null) notificationPanel.SetActive(false);
    }

    void Start()
    {
        notificationButton?.onClick.AddListener(OnClickNotification);

        // 延迟一帧缓存 RectTransform / CanvasGroup（Panel 可能是 Awake 时新 spawn 的）
        StartCoroutine(LateInit());
    }

    void OnDestroy()
    {
        // 组件销毁时（场景卸载）立即停止动画协程，防止访问已销毁的 CanvasGroup
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
    }

    IEnumerator LateInit()
    {
        yield return null;
        if (notificationPanel == null) yield break;
        _rt = notificationPanel.GetComponent<RectTransform>();
        _cg = notificationPanel.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = notificationPanel.AddComponent<CanvasGroup>();
        _shownPos    = _rt.anchoredPosition;   // 记录 Inspector 里设置好的停靠位置
        _initialized = true;
    }

    // ════════════════════════════════════════════════════════════════════
    //  公共接口
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 显示通知条，并同步更新导航栏红点。
    /// </summary>
    public void ShowNotification(string message = "新线索已添加")
    {
        // 同步更新文字
        SetMessage(message);

        // 同步更新导航红点 + GameManager 状态
        if (GameManager.Instance != null) GameManager.Instance.HasNewClue = true;
        NavigationBar.Instance?.UpdateClueBadge(true);

        // 重启动画协程（防止重叠）
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(ShowAndHide());
    }

    // ════════════════════════════════════════════════════════════════════
    //  动画协程
    // ════════════════════════════════════════════════════════════════════
    // 统一的"面板仍然有效"检查（处理场景卸载导致的销毁）
    bool PanelAlive() => notificationPanel != null && _rt != null && _cg != null;

    IEnumerator ShowAndHide()
    {
        if (notificationPanel == null) yield break;

        // 确保 LateInit 完成（最多等 1 秒）
        float waited = 0f;
        while (!_initialized && waited < 1f)
        {
            yield return null;
            if (notificationPanel == null) yield break;   // 等待期间面板被销毁
            waited += Time.deltaTime;
        }
        if (notificationPanel == null) yield break;

        notificationPanel.SetActive(true);

        // ── 滑入 ──────────────────────────────────────────────────────
        if (PanelAlive())
        {
            _rt.anchoredPosition = _shownPos + new Vector2(slideOffsetX, 0);
            _cg.alpha = 0f;

            float t = 0f;
            while (t < 1f)
            {
                if (!PanelAlive()) yield break;           // 场景切换，提前退出
                t = Mathf.Clamp01(t + Time.deltaTime / slideInDuration);
                float eased = t * t * (3f - 2f * t);
                _rt.anchoredPosition = Vector2.Lerp(
                    _shownPos + new Vector2(slideOffsetX, 0), _shownPos, eased);
                _cg.alpha = Mathf.Lerp(0f, 1f, eased);
                yield return null;
            }
            if (!PanelAlive()) yield break;
            _rt.anchoredPosition = _shownPos;
            _cg.alpha = 1f;
        }

        // ── 停留 ──────────────────────────────────────────────────────
        yield return new WaitForSeconds(stayDuration);
        if (!PanelAlive()) yield break;

        // ── 淡出 ──────────────────────────────────────────────────────
        if (PanelAlive())
        {
            float t = 0f;
            while (t < 1f)
            {
                if (!PanelAlive()) yield break;           // 场景切换，提前退出
                t = Mathf.Clamp01(t + Time.deltaTime / slideOutDuration);
                _cg.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            if (!PanelAlive()) yield break;
            _cg.alpha = 0f;
        }

        if (notificationPanel != null) notificationPanel.SetActive(false);
        _activeCoroutine = null;
    }

    // ════════════════════════════════════════════════════════════════════
    //  点击通知条
    // ════════════════════════════════════════════════════════════════════
    void OnClickNotification()
    {
        if (_activeCoroutine != null) { StopCoroutine(_activeCoroutine); _activeCoroutine = null; }
        if (notificationPanel != null) notificationPanel.SetActive(false);

        GameManager.Instance?.ClearClueBadge();
        SceneTransitionManager.Instance.GoToScene("ClueScene");
    }

    // ════════════════════════════════════════════════════════════════════
    //  辅助
    // ════════════════════════════════════════════════════════════════════
    void SetMessage(string msg)
    {
        if (notificationTMP  != null) notificationTMP.text  = msg;
        if (notificationText != null) notificationText.text = msg;
    }
}
