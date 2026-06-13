// Assets/Scripts/UI/SettingsPanelController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置面板控制器 —— 挂在 SettingsPanel 根节点（或同级管理节点）上。
///
/// ── 功能 ──────────────────────────────────────────────────────────────
/// 1. 音量调节：volumeSlider 拖动时实时调整 AudioListener.volume，
///    并用 PlayerPrefs 持久化，跨场景/重启后仍生效。
/// 2. 视图缩放：zoomInButton / zoomOutButton 调整 canvasScaler.scaleFactor，
///    在 [minZoom, maxZoom] 范围内放大/缩小整个 UI。
/// 3. 制作人信息：creditsButton 点击后展示/隐藏 creditsPanel，
///    内容来自 creditsText（文本可在 Inspector 的 creditsContent 中编辑）。
/// 4. 返回：closeButton 关闭设置面板，回到主界面。
///
/// ── Inspector 必填 ────────────────────────────────────────────────────
///   settingsPanel   设置面板根节点（含遮罩），默认隐藏
///   volumeSlider    音量滑条（Slider，0~1）
///   canvasScaler    场景 MainCanvas 上的 CanvasScaler 组件
///   zoomInButton / zoomOutButton  视图放大/缩小按钮
///   creditsPanel    制作人信息面板（默认隐藏）
///   creditsButton   显示/隐藏制作人信息的按钮
///   creditsText     制作人信息文本（TMP_Text）
///   closeButton     返回按钮
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [Header("设置面板")]
    [Tooltip("整个设置面板（含半透明遮罩），默认隐藏")]
    public GameObject settingsPanel;

    [Header("音量")]
    public Slider volumeSlider;

    [Header("视图缩放")]
    [Tooltip("场景 MainCanvas 上的 CanvasScaler 组件")]
    public CanvasScaler canvasScaler;
    public Button zoomInButton;
    public Button zoomOutButton;
    [Tooltip("每次点击的缩放步长")]
    public float zoomStep = 0.1f;
    public float minZoom = 0.7f;
    public float maxZoom = 1.5f;

    [Header("制作人信息")]
    [Tooltip("制作人信息面板，默认隐藏")]
    public GameObject creditsPanel;
    public Button creditsButton;
    public TMP_Text creditsText;
    [TextArea(3, 12)]
    public string creditsContent =
        "《XXX》制作团队\n\n策划：XXX\n美术：XXX\n程序：XXX\n音效：XXX\n\n感谢游玩！";

    [Header("返回")]
    [Tooltip("关闭设置面板，返回主界面")]
    public Button closeButton;

    const string VOLUME_PREF_KEY = "settings_volume";
    const string ZOOM_PREF_KEY   = "settings_zoom";

    // CanvasScaler.scaleFactor 的赋值只在其内部 Handle() 重新计算时才会同步到
    // Canvas.scaleFactor（该重算不是每帧都跑），运行时直接改 scaleFactor 往往
    // "看起来没反应"。因此额外缓存根 Canvas，并在修改时直接同步它的 scaleFactor，
    // 保证立即生效。
    private Canvas _targetCanvas;

    void Awake()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel  != null) creditsPanel.SetActive(false);
        if (canvasScaler  != null) _targetCanvas = canvasScaler.GetComponent<Canvas>();
    }

    void Start()
    {
        // ── 音量：读取存档并应用 ──────────────────────────────────────
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 1f);
        AudioListener.volume = savedVolume;
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // ── 视图缩放：读取存档并应用 ──────────────────────────────────
        if (canvasScaler != null)
        {
            float savedZoom = PlayerPrefs.GetFloat(ZOOM_PREF_KEY, canvasScaler.scaleFactor);
            ApplyZoom(Mathf.Clamp(savedZoom, minZoom, maxZoom));
        }
        zoomInButton?.onClick.AddListener(() => Zoom(zoomStep));
        zoomOutButton?.onClick.AddListener(() => Zoom(-zoomStep));

        // ── 其他按钮 ──────────────────────────────────────────────────
        closeButton?.onClick.AddListener(CloseSettings);
        creditsButton?.onClick.AddListener(ToggleCredits);

        if (creditsText != null) creditsText.text = creditsContent;
    }

    // ════════════════════════════════════════════════════════════════════
    //  面板开关（供 NavigationBar 调用）
    // ════════════════════════════════════════════════════════════════════
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel  != null) creditsPanel.SetActive(false);
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null) return;
        bool show = !settingsPanel.activeSelf;
        settingsPanel.SetActive(show);
        if (!show && creditsPanel != null) creditsPanel.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  音量
    // ════════════════════════════════════════════════════════════════════
    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, value);
    }

    // ════════════════════════════════════════════════════════════════════
    //  视图缩放
    // ════════════════════════════════════════════════════════════════════
    void Zoom(float delta)
    {
        if (canvasScaler == null) return;
        float newScale = Mathf.Clamp(canvasScaler.scaleFactor + delta, minZoom, maxZoom);
        ApplyZoom(newScale);
        PlayerPrefs.SetFloat(ZOOM_PREF_KEY, newScale);
    }

    /// <summary>同时写入 CanvasScaler 和 Canvas 的 scaleFactor，确保立即生效。</summary>
    void ApplyZoom(float scale)
    {
        if (canvasScaler   != null) canvasScaler.scaleFactor = scale;
        if (_targetCanvas  != null) _targetCanvas.scaleFactor = scale;
    }

    // ════════════════════════════════════════════════════════════════════
    //  制作人信息
    // ════════════════════════════════════════════════════════════════════
    void ToggleCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(!creditsPanel.activeSelf);
    }
}
