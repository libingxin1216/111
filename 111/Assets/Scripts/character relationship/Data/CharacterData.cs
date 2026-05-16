using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("基础信息")]
    public string characterId;
    public CharacterType characterType;

    [Header("正确答案")]
    public string correctName;
    public string correctRole; // 仅主要人物需要
    public Sprite photo;       // 仅主要人物需要

    [Header("选择面板选项")]
    public string[] nameOptions;
    public string[] roleOptions;

    [Header("额外标签（如人贩子、在逃）")]
    public string[] badgeLabels;

    [Header("运行时状态（不需要填写）")]
    [HideInInspector] public string currentName = "";
    [HideInInspector] public string currentRole = "";
    [HideInInspector] public bool photoUnlocked = false;
    [HideInInspector] public bool isLocked = false;

    public void ResetRuntimeData()
    {
        currentName = "";
        currentRole = "";
        photoUnlocked = false;
        isLocked = false;
    }
}