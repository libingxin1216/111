using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 剧情进度管理器：根据人物对齐数量和特定人物解锁分批发放线索。
///
/// ── 挂载方式 ────────────────────────────────────────────────────────────
/// 1. 在 MainScene 中找到挂有 GameManager 的 GameObject
/// 2. Add Component → StageProgressionManager
/// 3. 按下方 Inspector 字段说明填入对应 Clue ID
///
/// ── 进度触发逻辑 ─────────────────────────────────────────────────────────
/// • 游戏开始   → 发放 initialClueIds（3条初始线索）
/// • 对齐 3 人  → OnStageCleared(1) → 发放 stage1ClueIds（2条）
/// • 对齐 4 人  → OnStageCleared(2) → 发放 stage2ClueIds（2条）
/// • 对齐 5 人  → OnStageCleared(3) → 发放 stage3ClueIds（2条）
/// • 刘陆被对齐 → OnCharacterLocked("刘陆 characterId") → 发放 liuluClueIds（1条）
///
/// ── StageConfig 配置提醒 ────────────────────────────────────────────────
/// 在 DemoStageConfig.asset（或你的 StageConfig 资产）中：
///   stageThresholds = [3, 4, 5]
/// 这样 LockSystem 会在对齐 3/4/5 人时分别发出 OnStageCleared(1/2/3)。
/// ────────────────────────────────────────────────────────────────────────
/// </summary>
public class StageProgressionManager : MonoBehaviour
{
    public static StageProgressionManager Instance { get; private set; }

    // ── 初始线索 ──────────────────────────────────────────────────────────
    [Header("游戏开始时发放的初始线索（3条）")]
    [Tooltip("对应 ClueData.clueId，需在 ClueSystem.allClues 中存在。")]
    public string[] initialClueIds =
    {
        "CLU_OVERVIEW",        // 钟德发交代的案件大体
        "CLU_FENG_ZHONG_CONF", // 南溪市凤栖路8·14儿童拐卖案钟德发口供
        "CLU_FENG_XIE_REPORT"  // 南溪市凤栖路8·14儿童拐卖案谢明远报案记录
    };

    // ── 阶段线索 ──────────────────────────────────────────────────────────
    [Header("阶段一：对齐3人后解锁（钟德发·高亚珍·谢雨彤一家）")]
    public string[] stage1ClueIds =
    {
        "CLU_DONG_ZHONG_CONF",  // 南溪市东桥菜市场儿童拐卖案钟德发口供
        "CLU_DONG_ZHANG_REPORT" // 南溪市东桥菜市场儿童拐卖案张翠兰报案记录
    };

    [Header("阶段二：对齐4人后解锁（李雨欣一家）")]
    public string[] stage2ClueIds =
    {
        "CLU_TANG_ZHONG_CONF",  // 南溪市塘尾村儿童拐卖案钟德发口供
        "CLU_TANG_CHEN_REPORT"  // 南溪市塘尾村儿童拐卖案陈建军、刘桂英报案记录
    };

    [Header("阶段三：对齐5人后解锁")]
    public string[] stage3ClueIds =
    {
        "CLU_DONG_GAO_CONF", // 南溪市东桥菜市场儿童拐卖案高亚珍口供
        "CLU_FENG_GAO_CONF"  // 南溪市凤栖路8·14儿童拐卖案高亚珍口供
    };

    // ── 特殊：刘陆/老六被对齐 ──────────────────────────────────────────
    [Header("特殊触发：刘陆/老六被对齐后解锁")]
    [Tooltip("填入 CharacterData14老六.asset 中 characterId 字段的值")]
    public string liuluCharacterId = "老六";

    public string[] liuluClueIds =
    {
        "CLU_LIULU_CONF" // 刘陆的口供
    };

    // 特殊阶段 ID（用于 ClearedStages 防重）
    private const int STAGE_LIULU = 99;

    // ════════════════════════════════════════════════════════════════════
    //  生命周期
    // ════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 若已有实例，销毁重复的 GO 或组件
            if (gameObject != Instance.gameObject)
                Destroy(gameObject);
            else
                Destroy(this);
            return;
        }
        Instance = this;
        // 确保跨场景持久——无论是独立 GO 还是与 GameManager 共用同一 GO 都适用
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        EventBus.On("OnStageCleared",    OnStageCleared);
        EventBus.On("OnCharacterLocked", OnCharacterLocked);
    }

    void OnDisable()
    {
        EventBus.Off("OnStageCleared",    OnStageCleared);
        EventBus.Off("OnCharacterLocked", OnCharacterLocked);
    }

    void Start()
    {
        // ⚠️ 不在此处发放初始线索！
        // 初始线索须等玩家点击门、同事完成送达动画后才发放，
        // 避免游戏一启动就闪烁导航栏红点和通知条。
        // 发放时机由 MainSceneController.ActivateDoorKnock() 的 onComplete 回调控制。
    }

    // ════════════════════════════════════════════════════════════════════
    //  初始线索
    // ════════════════════════════════════════════════════════════════════

    // stage 0 = 初始线索已发放标记（防止场景重载时重复发放）
    private const int STAGE_INITIAL = 0;

    /// <summary>
    /// 发放初始线索（由门口同事送达动画完成后的回调触发）。
    /// 幂等：已发放过（stage 0 已标记）则跳过，不会重复发放。
    /// 公开方法，供 MainSceneController 在 onComplete 中调用。
    /// </summary>
    public void GiveInitialClues()
    {
        if (initialClueIds == null || initialClueIds.Length == 0) return;
        if (GameManager.Instance == null) return;

        // 用专用阶段标记判重，避免与 nextDeliveryClueId 重叠导致误判
        if (GameManager.Instance.HasClearedStage(STAGE_INITIAL)) return;
        GameManager.Instance.MarkStageCleared(STAGE_INITIAL);

        UnlockClues(initialClueIds, showNotification: true);
        Debug.Log($"[StageProgression] 初始线索已发放（{initialClueIds.Length} 条）");
    }

    // ════════════════════════════════════════════════════════════════════
    //  事件处理
    // ════════════════════════════════════════════════════════════════════

    void OnStageCleared(object obj)
    {
        int stage = (int)obj;
        if (GameManager.Instance == null) return;

        // 防重
        if (GameManager.Instance.HasClearedStage(stage)) return;
        GameManager.Instance.MarkStageCleared(stage);

        string[] clues;
        string stageDesc;
        switch (stage)
        {
            case 1:
                clues     = stage1ClueIds;
                stageDesc = "对齐3人（钟德发·高亚珍·谢雨彤）";
                break;
            case 2:
                clues     = stage2ClueIds;
                stageDesc = "对齐4人（+李雨欣一家）";
                break;
            case 3:
                clues     = stage3ClueIds;
                stageDesc = "对齐5人";
                break;
            default:
                return;
        }

        if (clues == null || clues.Length == 0) return;
        UnlockClues(clues, showNotification: true);
        Debug.Log($"[StageProgression] 阶段 {stage} 触发（{stageDesc}）→ 解锁 {clues.Length} 条新线索");
    }

    void OnCharacterLocked(object obj)
    {
        string charId = (string)obj;
        if (charId != liuluCharacterId) return;
        if (GameManager.Instance == null) return;

        // 防重
        if (GameManager.Instance.HasClearedStage(STAGE_LIULU)) return;
        GameManager.Instance.MarkStageCleared(STAGE_LIULU);

        if (liuluClueIds == null || liuluClueIds.Length == 0) return;
        UnlockClues(liuluClueIds, showNotification: true);
        Debug.Log($"[StageProgression] 刘陆/老六被对齐 → 解锁「刘陆的口供」");
    }

    // ════════════════════════════════════════════════════════════════════
    //  线索解锁工具
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 批量解锁线索并保存到 GameManager。
    /// • 若当前在 ClueScene，ClueSystem 会立刻响应 EventBus 事件。
    /// • 若在其他场景，线索 ID 先存入 GameManager.SavedUnlockedClueIds，
    ///   待进入 ClueScene 时由 SyncClueSystemIfExists 自动恢复。
    /// </summary>
    void UnlockClues(string[] clueIds, bool showNotification)
    {
        if (clueIds == null || GameManager.Instance == null) return;

        int newCount = 0;
        foreach (var id in clueIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            if (GameManager.Instance.SavedUnlockedClueIds.Contains(id)) continue;

            // 保存到持久列表（跨场景）
            GameManager.Instance.AddClue(id);   // AddClue = AddClueToSave + Badge

            // 若 ClueSystem 当前在场景中，立即解锁（绕过前置条件检查，用 RestoreClue 风格）
            ClueSystem.Instance?.UnlockClue(id);

            newCount++;
        }

        if (newCount > 0)
        {
            // ── 1. 无论如何都刷新导航栏徽章（直接调用，不依赖 NewClueNotification）
            NavigationBar.Instance?.UpdateClueBadge(true);

            if (showNotification)
            {
                string msg = newCount == 1
                    ? "新线索已添加"
                    : $"新线索已添加（+{newCount}）";

                if (NewClueNotification.Instance != null)
                {
                    // ── 2a. 当前场景有通知组件：直接调用
                    NewClueNotification.Instance.ShowNotification(msg);
                }
                else
                {
                    // ── 2b. 当前场景无通知组件（如 RelationScene）：
                    //        通过 EventBus 广播，任何场景的 NewClueNotification 均可响应；
                    //        同时在当前 Canvas 上创建一个临时绿色提示条作为兜底
                    EventBus.Emit("OnRequestNotification", msg);
                    StartCoroutine(ShowTempNotification(msg));
                }
            }
        }
    }

    // ── 在任意场景 Canvas 上创建临时绿色提示条（3 秒后自动销毁）────────────────
    System.Collections.IEnumerator ShowTempNotification(string message)
    {
        var canvas = FindObjectOfType<UnityEngine.Canvas>();
        if (canvas == null) yield break;

        // 创建提示条
        var go = new UnityEngine.GameObject("_TempClueNotification");
        go.transform.SetParent(canvas.transform, false);

        var rt = go.AddComponent<UnityEngine.RectTransform>();
        rt.anchorMin = new UnityEngine.Vector2(1f, 0f);
        rt.anchorMax = new UnityEngine.Vector2(1f, 0f);
        rt.pivot     = new UnityEngine.Vector2(1f, 0f);
        rt.sizeDelta = new UnityEngine.Vector2(280, 50);
        rt.anchoredPosition = new UnityEngine.Vector2(-20f, 80f);

        var bg = go.AddComponent<UnityEngine.UI.Image>();
        bg.color = new UnityEngine.Color(0.08f, 0.55f, 0.22f, 0.92f);
        bg.raycastTarget = false;

        var textGO = new UnityEngine.GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<UnityEngine.RectTransform>();
        trt.anchorMin = UnityEngine.Vector2.zero;
        trt.anchorMax = UnityEngine.Vector2.one;
        trt.sizeDelta = UnityEngine.Vector2.zero;
        var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 15;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = UnityEngine.Color.white;
        tmp.raycastTarget = false;
        var font = GameManager.GetChineseFont();
        if (font != null) tmp.font = font;

        // 滑入：从右侧出现
        float elapsed = 0f;
        var hiddenPos = rt.anchoredPosition + new UnityEngine.Vector2(320f, 0f);
        while (elapsed < 0.35f)
        {
            elapsed += UnityEngine.Time.deltaTime;
            float t = UnityEngine.Mathf.Clamp01(elapsed / 0.35f);
            float e = t * t * (3f - 2f * t);
            rt.anchoredPosition = UnityEngine.Vector2.Lerp(hiddenPos, new UnityEngine.Vector2(-20f, 80f), e);
            yield return null;
        }
        rt.anchoredPosition = new UnityEngine.Vector2(-20f, 80f);

        yield return new UnityEngine.WaitForSeconds(2.5f);

        // 淡出
        var cg = go.AddComponent<UnityEngine.CanvasGroup>();
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            if (go == null) yield break;
            elapsed += UnityEngine.Time.deltaTime;
            cg.alpha = 1f - UnityEngine.Mathf.Clamp01(elapsed / 0.3f);
            yield return null;
        }

        if (go != null) UnityEngine.Object.Destroy(go);
    }
}
