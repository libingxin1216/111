// Assets/Scripts/MainScene/MainSceneController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主场景（办公室）控制器。
/// ── 变更摘要 ───────────────────────────────────────────────────────────
/// 1. 新增 fileFolderBadge：桌面文件夹图标的红点 GameObject，
///    打开线索板后自动隐藏，有新线索时随 GameManager.HasNewClue 显示。
/// 2. ActivateDoorKnock() 现在把控制权移交给 ColleagueDeliveryController，
///    需在 Inspector 里填写 colleagueDelivery 引用和台词配置。
/// 3. OnClickDoor() 保留空实现，实际逻辑由 ColleagueDeliveryController 接管。
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class MainSceneController : MonoBehaviour
{
    [Header("可点击的桌面道具（Button 组件）")]
    public Button computerButton;
    public Button whiteboardButton;
    public Button fileFolderButton;
    public Button doorButton;
    public Button notebookButton;

    [Header("门的动画")]
    public Animator doorAnimator;

    [Header("笔记本叠加层")]
    public GameObject notebookOverlayPanel;

    [Header("★ 新增：桌面文件夹红点")]
    [Tooltip("文件夹图标上的红点 GameObject，有新线索时显示")]
    public GameObject fileFolderBadge;

    [Header("★ 新增：同事送达控制器")]
    [Tooltip("挂有 ColleagueDeliveryController 的 GameObject 引用")]
    public ColleagueDeliveryController colleagueDelivery;

    [Header("★ 新增：同事台词配置")]
    [Tooltip("调用 ActivateDoorKnock 时使用的台词。支持多句，点击继续。")]
    [TextArea(2, 6)]
    public string[] deliveryDialogueLines = new[]
    {
        // ── 原有台词（保持不变）──
        "你好，这是上午刚到的文件，请签收。",
        "里面有一些案件相关资料，请仔细查阅。",

        // ── 新增：游戏玩法说明 ──
        "桌上的文件夹里存放着与案件相关的线索，点击可以放大查看，\n重要信息可以摘录到笔记本留存。",
        "桌上的电脑可以联网搜索人员信息，人口数据库也能查询户籍资料，\n搜索结果有时会触发新的线索。",
        "通过分析线索和搜索获取的信息，将涉案人员的姓名和身份\n填入右侧的人物关系图谱，对齐每一位当事人的真实身份。",
        "加油，案子就交给你了！"
    };

    [Header("★ 新增：待送达线索 ID（由 GameManager/剧本逻辑填写）")]
    [Tooltip("下一次门口送达时解锁的线索 ID")]
    public string nextDeliveryClueId = "";

    [Header("★ 新增：门交互模式")]
    [Tooltip("true = 场景启动时立即激活门口敲门流程（需同时填写 nextDeliveryClueId）；\n" +
             "false = 门保持不可交互，等待外部代码调用 ActivateDoorKnock()。")]
    public bool activateDoorOnStart = false;

    private bool _isNotebookOpen;
    // 注意：_deliveryTriggered 已被 DeliveryAlreadyDone() 取代，
    // 后者基于 GameManager 持久化数据判断，不受场景重载影响。

    // ════════════════════════════════════════════════════════════════════
    void Start()
    {
        computerButton?.onClick.AddListener(OnClickComputer);
        whiteboardButton?.onClick.AddListener(OnClickWhiteboard);
        fileFolderButton?.onClick.AddListener(OnClickFileFolder);
        doorButton?.onClick.AddListener(OnClickDoor);
        notebookButton?.onClick.AddListener(OnClickNotebook);

        // 同步文件夹红点
        RefreshFolderBadge();

        // ── 门的初始状态 ──────────────────────────────────────────────
        // 场景加载时自动播放敲门音效，完成后门变为可点击（一次点击即开始对话）
        if (activateDoorOnStart && colleagueDelivery != null && !DeliveryAlreadyDone())
        {
            ActivateDoorKnock();
        }
        else
        {
            if (doorButton != null) doorButton.interactable = true;
        }
    }

    /// <summary>
    /// 判断本次同事送达是否已经完成过（基于 GameManager 持久化数据）。
    /// 无论场景重载多少次，只要初始阶段已完成，就认为已送达。
    /// </summary>
    bool DeliveryAlreadyDone()
    {
        if (GameManager.Instance == null) return false;
        // 若配置了具体线索 ID，以该线索是否已解锁为准
        if (!string.IsNullOrEmpty(nextDeliveryClueId))
            return GameManager.Instance.SavedUnlockedClueIds.Contains(nextDeliveryClueId);
        // 否则以初始阶段（stage 0）是否已清除为准
        return GameManager.Instance.HasClearedStage(0);
    }

    // ════════════════════════════════════════════════════════════════════
    //  按钮处理
    // ════════════════════════════════════════════════════════════════════
    void OnClickComputer()
        => SceneTransitionManager.Instance.GoToScene("ComputerScene");

    void OnClickWhiteboard()
        => SceneTransitionManager.Instance.GoToScene("RelationScene");

    void OnClickFileFolder()
    {
        GameManager.Instance?.ClearClueBadge();
        // 文件夹红点随之消失
        if (fileFolderBadge != null) fileFolderBadge.SetActive(false);
        SceneTransitionManager.Instance.GoToScene("ClueScene");
    }

    void OnClickDoor()
    {
        // 当 ActivateDoorKnock 已被调用时，门按钮的监听器会被
        // ColleagueDeliveryController 替换，此处不会再执行。
        // 以下是"门直接可点击但未配置 ColleagueDelivery"时的 fallback：
        if (colleagueDelivery == null)
        {
            if (doorAnimator != null) doorAnimator.SetTrigger("DoorKnock");
            return;
        }

        // 有 ColleagueDelivery 且尚未完成送达：触发流程
        // DeliveryAlreadyDone() 基于 GameManager 持久化判断，不受场景重载影响
        if (!DeliveryAlreadyDone())
            ActivateDoorKnock();
    }

    void OnClickNotebook()
    {
        _isNotebookOpen = !_isNotebookOpen;
        if (notebookOverlayPanel != null)
            notebookOverlayPanel.SetActive(_isNotebookOpen);
    }

    // ════════════════════════════════════════════════════════════════════
    //  外部接口（由 GameManager 或剧本逻辑调用）
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 激活"同事敲门"流程。
    /// 若已配置 ColleagueDeliveryController 则走完整动画序列；
    /// 否则退回门动画 + 直接解锁线索的简化逻辑。
    /// </summary>
    public void ActivateDoorKnock(string clueId = null, string[] lines = null)
    {
        // 优先使用参数，其次使用 Inspector 里配置的默认值
        string finalClueId = !string.IsNullOrEmpty(clueId) ? clueId : nextDeliveryClueId;
        string[] finalLines = (lines != null && lines.Length > 0) ? lines : deliveryDialogueLines;

        if (colleagueDelivery != null)
        {
            // ── 完整流程（含音效、动画、台词） ──────────────────────
            // onComplete 在同事动画全部结束后才触发：
            //   1. RefreshFolderBadge()         ← 刷新桌面文件夹红点
            //   2. GiveInitialClues()           ← 此时才发放初始线索 + 显示通知
            colleagueDelivery.TriggerDelivery(
                finalClueId,
                finalLines,
                onComplete: () =>
                {
                    RefreshFolderBadge();
                    StageProgressionManager.Instance?.GiveInitialClues();
                });
        }
        else
        {
            // ── Fallback：无同事动画，直接解锁后发放初始线索 ─────────
            if (doorButton != null) doorButton.interactable = true;
            if (doorAnimator != null) doorAnimator.SetTrigger("DoorGlow");

            if (!string.IsNullOrEmpty(finalClueId))
            {
                ClueSystem.Instance?.UnlockClue(finalClueId);
                GameManager.Instance?.AddClue(finalClueId);
                RefreshFolderBadge();
            }
            // Fallback 下也在"门触发"这一步发放初始线索
            StageProgressionManager.Instance?.GiveInitialClues();
        }
    }

    /// <summary>根据 GameManager.HasNewClue 同步文件夹红点状态。</summary>
    public void RefreshFolderBadge()
    {
        if (fileFolderBadge == null) return;
        bool hasNew = GameManager.Instance != null && GameManager.Instance.HasNewClue;
        fileFolderBadge.SetActive(hasNew);
    }

    // GoToScene 保留（已有代码引用）
    public void GoToScene(string sceneName)
    {
        EventBus.Clear();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
