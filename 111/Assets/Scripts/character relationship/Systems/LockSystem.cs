using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LockSystem : MonoBehaviour
{
    public static LockSystem Instance;

    [SerializeField] private StageConfig stageConfig;

    private Dictionary<string, CharacterData> dataMap = new();
    private int currentStageIndex = 0;

    void Awake()
    {
        Instance = this;
        TryInitStageConfig();
        RestoreStageIndexFromGameManager();
    }

    void Start()
    {
        // 二次初始化兜底：Awake 时 RelationshipDataHolder.Instance 可能尚未设置
        TryInitStageConfig();
    }

    /// <summary>
    /// 尝试从 RelationshipDataHolder 加载 stageConfig。
    /// 在 Awake 和 Start 各调用一次，确保不因 Awake 顺序问题导致 stageConfig 为空。
    /// </summary>
    void TryInitStageConfig()
    {
        if (stageConfig != null) return;
        if (RelationshipDataHolder.Instance != null)
            stageConfig = RelationshipDataHolder.Instance.stageConfig;
    }

    /// <summary>
    /// 从 GameManager.ClearedStagesList 中找出最大的常规阶段编号（1~10），
    /// 将 currentStageIndex 恢复到对应值，与 StageProgressionManager 保持同步。
    /// </summary>
    void RestoreStageIndexFromGameManager()
    {
        if (GameManager.Instance == null) return;
        int maxCleared = 0;
        foreach (var stage in GameManager.Instance.ClearedStagesList)
            if (stage >= 1 && stage <= 10 && stage > maxCleared)
                maxCleared = stage;
        currentStageIndex = maxCleared;
    }

    public void RegisterCharacter(CharacterData data)
    {
        dataMap[data.characterId] = data;
    }

    // ������ʽ1����ҵ���ύ��ť
    public void OnSubmitClicked()
    {
        TryLockAlignedCharacters();
    }

    // ������ʽ2������ж��׶����ⲿϵͳ����
    public void TriggerFinalJudgment()
    {
        TryLockAlignedCharacters();
        EventBus.Emit("OnFinalJudgmentDone", CountLocked());
    }

    // �����ж�������������������Ѷ����
    private void TryLockAlignedCharacters()
    {
        if (dataMap.Count == 0)
        {
            Debug.LogWarning("[LockSystem] dataMap 为空，没有注册任何角色。" +
                             "请确认 CardManager 已正确调用 RegisterCharacter。");
            return;
        }

        bool anyNewLocked = false;
        foreach (var kv in dataMap)
        {
            var data = kv.Value;
            if (data.isLocked) continue;

            bool aligned = IsAligned(data);
            Debug.Log($"[LockSystem] 检查 {data.characterId}：" +
                      $"photo={data.photoUnlocked} " +
                      $"name='{data.currentName}'(正确:'{data.correctName}') " +
                      $"role='{data.currentRole}'(正确:'{data.correctRole}') " +
                      $"type={data.characterType} → {(aligned ? "✓ 对齐" : "✗ 未对齐")}");

            if (!aligned) continue;

            data.isLocked = true;
            EventBus.Emit("OnCharacterLocked", data.characterId);
            anyNewLocked = true;
            Debug.Log($"[LockSystem] ✓ {data.characterId} 锁定成功！");
        }

        if (anyNewLocked)
            CheckStageProgress();
        else
            Debug.Log("[LockSystem] 本次提交没有新增锁定的角色（填写内容与正确答案不符）。");

        CheckAllSolved();
    }

    // 检查是否所有人物信息均已判定成功，若是则跳转到最终结局场景
    private void CheckAllSolved()
    {
        if (dataMap.Count == 0) return;
        if (CountLocked() < dataMap.Count) return;

        Debug.Log("[LockSystem] 所有人物信息均已判定成功，跳转到最终结局场景。");
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.GoToScene("FinalScene");
        else
            SceneManager.LoadScene("FinalScene");
    }

    // 判断人物信息是否对齐
    private bool IsAligned(CharacterData data)
    {
        // correctName 为空说明 ScriptableObject 尚未配置答案，跳过
        if (string.IsNullOrEmpty(data.correctName)) return false;

        if (data.characterType == CharacterType.Main)
        {
            // 主要人物：照片已解锁 + 姓名正确 + 角色身份正确，三项全部满足才对齐
            return data.photoUnlocked
                && data.currentName == data.correctName
                && data.currentRole == data.correctRole;
        }
        else
        {
            // 次要人物：只需姓名正确
            return data.currentName == data.correctName;
        }
    }

    // 阶段进度检查
    private void CheckStageProgress()
    {
        int lockedCount = CountLocked();
        EventBus.Emit("OnAlignmentProgress", lockedCount);

        // 最后一次尝试加载 stageConfig
        TryInitStageConfig();

        if (stageConfig == null)
        {
            Debug.LogWarning("[LockSystem] StageConfig 为空，无法触发阶段进度。" +
                             "请确认 RelationshipDataHolder 已配置 StageConfig 资产，" +
                             "且 StageConfig.stageThresholds 已填写（如 [3,4,5]）。");
            return;
        }

        if (currentStageIndex >= stageConfig.stageThresholds.Length)
        {
            Debug.Log($"[LockSystem] 所有阶段已完成（已锁定 {lockedCount} 人）");
            return;
        }

        int threshold = stageConfig.stageThresholds[currentStageIndex];
        Debug.Log($"[LockSystem] 当前已锁定 {lockedCount} 人，" +
                  $"阶段 {currentStageIndex + 1} 解锁条件：{threshold} 人");

        if (lockedCount >= threshold)
        {
            currentStageIndex++;
            Debug.Log($"[LockSystem] 阶段 {currentStageIndex} 达成！发送 OnStageCleared 事件");
            EventBus.Emit("OnStageCleared", currentStageIndex);
        }
    }

    private int CountLocked()
    {
        int count = 0;
        foreach (var kv in dataMap)
            if (kv.Value.isLocked) count++;
        return count;
    }

    public void ResetAll()
    {
        currentStageIndex = 0;
        foreach (var kv in dataMap)
            kv.Value.ResetRuntimeData();
    }
}