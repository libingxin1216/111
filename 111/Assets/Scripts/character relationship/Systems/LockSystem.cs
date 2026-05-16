using System.Collections.Generic;
using UnityEngine;

public class LockSystem : MonoBehaviour
{
    public static LockSystem Instance;
    private Dictionary<string, CharacterData> dataMap = new();

    void Awake() => Instance = this;

    public void RegisterCharacter(CharacterData data)
    {
        dataMap[data.characterId] = data;
    }

    public void TryCheckAlignment(string characterId)
    {
        if (!dataMap.TryGetValue(characterId, out var data)) return;
        if (data.isLocked) return;

        bool aligned;
        if (data.characterType == CharacterType.Main)
        {
            aligned = data.currentName == data.correctName
                   && data.currentRole == data.correctRole
                   && data.photoUnlocked;
        }
        else
        {
            aligned = data.currentName == data.correctName;
        }

        if (aligned)
        {
            data.isLocked = true;
            EventBus.Emit("OnCharacterLocked", characterId);
            CheckStageClear();
        }
    }

    void CheckStageClear()
    {
        int lockedCount = 0;
        foreach (var kv in dataMap)
            if (kv.Value.isLocked) lockedCount++;

        EventBus.Emit("OnAlignmentProgress", lockedCount);
    }
}