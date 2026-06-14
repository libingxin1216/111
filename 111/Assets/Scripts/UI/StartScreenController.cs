// Assets/Scripts/UI/StartScreenController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开始界面控制器 —— 挂在开始界面面板（StartScreenPanel）根节点上。
///
/// ── 功能 ──────────────────────────────────────────────────────────────
/// 1. 点击"开始游戏"：隐藏开始界面面板，进入游戏。
/// 2. 点击"制作人员"：显示制作人员信息面板。
/// 3. 按下 Esc 键：退出游戏（编辑器中则停止运行）。
///
/// ── Inspector 必填 ────────────────────────────────────────────────────
///   startScreenPanel   开始界面面板根节点（本物体）
///   startButton        "开始游戏"按钮
///   creditsButton      "制作人员"按钮
///   creditsInfoPanel   制作人员信息面板，默认隐藏
///   closeCreditsButton 关闭制作人员信息面板的按钮
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class StartScreenController : MonoBehaviour
{
    [Header("开始界面")]
    public GameObject startScreenPanel;
    public Button startButton;
    public Button creditsButton;
    public Button exitButton;
    public Button settingsButton;

    [Header("设置面板")]
    [Tooltip("场景中的设置面板控制器（SettingsPanel 上的 SettingsPanelController）")]
    public SettingsPanelController settingsPanelController;

    [Header("制作人员信息面板")]
    public GameObject creditsInfoPanel;
    public Button closeCreditsButton;

    [Header("开始游戏过渡动画")]
    [Tooltip("门的图片（点击开始游戏后显示并播放动画）")]
    public Image doorImage;
    [Tooltip("门关闭状态的贴图")]
    public Sprite doorClosedSprite;
    [Tooltip("门打开/高亮状态的贴图")]
    public Sprite doorOpenSprite;
    [Tooltip("用于播放敲门音效的 AudioSource")]
    public AudioSource audioSource;
    [Tooltip("敲门音效")]
    public AudioClip knockClip;
    [Tooltip("门动画总时长（秒）")]
    public float doorAnimDuration = 1f;

    /// 玩家点击过"开始游戏"后置为 true，整个游玩过程中保持。
    /// 从其他场景返回主界面场景时，开始界面面板不再显示。
    private static bool hasGameStarted = false;

    /// <summary>
    /// 重置为初始标题状态，使开始界面面板在下次加载主场景时重新显示。
    /// 由"返回标题"按钮调用。
    /// </summary>
    public static void ResetToTitle()
    {
        hasGameStarted = false;
    }

    /// <summary>
    /// 标记游戏已开始，使开始界面面板不再显示。
    /// 由"重新侦破"按钮调用，确保返回主场景时不会出现开始界面面板。
    /// </summary>
    public static void MarkGameStarted()
    {
        hasGameStarted = true;
    }

    void Awake()
    {
        if (hasGameStarted)
        {
            if (startScreenPanel != null) startScreenPanel.SetActive(false);
            return;
        }

        if (creditsInfoPanel != null) creditsInfoPanel.SetActive(false);
    }

    void Start()
    {
        startButton?.onClick.AddListener(StartGame);
        creditsButton?.onClick.AddListener(OpenCredits);
        closeCreditsButton?.onClick.AddListener(CloseCredits);
        exitButton?.onClick.AddListener(QuitGame);
        settingsButton?.onClick.AddListener(OpenSettings);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            QuitGame();
    }

    void StartGame()
    {
        hasGameStarted = true;
        StartCoroutine(PlayDoorAnimationAndStart());
    }

    IEnumerator PlayDoorAnimationAndStart()
    {
        if (doorImage != null)
        {
            if (doorClosedSprite != null) doorImage.sprite = doorClosedSprite;

            Transform doorTransform = doorImage.transform;
            doorTransform.localScale = Vector3.one * 0.3f;
            doorImage.gameObject.SetActive(true);

            float openSwitchTime = doorAnimDuration * 0.5f;
            float elapsed = 0f;
            while (elapsed < doorAnimDuration)
            {
                elapsed += Time.deltaTime;
                float ratio = Mathf.Clamp01(elapsed / doorAnimDuration);
                doorTransform.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one, ratio);

                if (doorOpenSprite != null && elapsed >= openSwitchTime && doorImage.sprite != doorOpenSprite)
                    doorImage.sprite = doorOpenSprite;

                yield return null;
            }

            doorImage.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(doorAnimDuration);
        }

        if (startScreenPanel != null) startScreenPanel.SetActive(false);

        if (audioSource != null && knockClip != null)
            audioSource.PlayOneShot(knockClip);
    }

    void OpenCredits()
    {
        if (creditsInfoPanel != null) creditsInfoPanel.SetActive(true);
    }

    void CloseCredits()
    {
        if (creditsInfoPanel != null) creditsInfoPanel.SetActive(false);
    }

    void OpenSettings()
    {
        if (settingsPanelController != null) settingsPanelController.OpenSettings();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
