using System.Collections.Generic;
using UnityEngine;

public class LockSystem : MonoBehaviour
{
    public static LockSystem Instance;

    [SerializeField] private StageConfig stageConfig;

    private Dictionary<string, CharacterData> dataMap = new();
    private int currentStageIndex = 0;

    void Awake()
    {
        Instance = this;
        if (stageConfig == null && RelationshipDataHolder.Instance != null)
            stageConfig = RelationshipDataHolder.Instance.stageConfig;
    }

    public void RegisterCharacter(CharacterData data)
    {
        dataMap[data.characterId] = data;
    }

    // 触发方式1：玩家点击提交按钮
    public void OnSubmitClicked()
    {
        TryLockAlignedCharacters();
    }

    // 触发方式2：结局判定阶段由外部系统调用
    public void TriggerFinalJudgment()
    {
        TryLockAlignedCharacters();
        EventBus.Emit("OnFinalJudgmentDone", CountLocked());
    }

    // 核心判定：遍历所有人物，锁定已对齐的
    private void TryLockAlignedCharacters()
    {
        bool anyNewLocked = false;
        foreach (var kv in dataMap)
        {
            var data = kv.Value;
            if (data.isLocked) continue;
            if (!IsAligned(data)) continue; // 填写错误：不锁定，不提示

            data.isLocked = true;
            EventBus.Emit("OnCharacterLocked", data.characterId);
            anyNewLocked = true;
        }
        if (anyNewLocked) CheckStageProgress();
    }

    // 单个人物对齐判定规则
    private bool IsAligned(CharacterData data)
    {
        if (data.characterType == CharacterType.Main)
            return data.currentName == data.correctName
                && data.currentRole == data.correctRole
                && data.photoUnlocked;
        else
            return data.currentName == data.correctName;
    }

    // 阶段门槛检查
    private void CheckStageProgress()
    {
        int lockedCount = CountLocked();
        EventBus.Emit("OnAlignmentProgress", lockedCount);

        if (stageConfig == null) return;
        if (currentStageIndex >= stageConfig.stageThresholds.Length) return;

        int threshold = stageConfig.stageThresholds[currentStageIndex];
        if (lockedCount >= threshold)
        {
            currentStageIndex++;
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