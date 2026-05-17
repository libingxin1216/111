using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("基础信息")]
    public string characterId;
    public CharacterType characterType;

    [Header("正确答案")]
    public string correctName;
    public string correctRole;
    public Sprite photo;

    [Header("选择面板选项")]
    public string[] nameOptions;
    public string[] roleOptions;

    [Header("额外标签（如人贩子、在逃）")]
    public string[] badgeLabels;

    [Header("预填信息（游戏开始时直接显示，留空则不预填）")]
    public string prefilledName;

    [HideInInspector] public string currentName = "";
    [HideInInspector] public string currentRole = "";
    [HideInInspector] public bool photoUnlocked = false;
    [HideInInspector] public bool isLocked = false;

    public void ResetRuntimeData()
    {
        currentName = string.IsNullOrEmpty(prefilledName) ? "" : prefilledName;
        currentRole = "";
        photoUnlocked = false;
        isLocked = false;
    }
}