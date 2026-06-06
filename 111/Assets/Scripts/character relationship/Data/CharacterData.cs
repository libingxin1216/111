using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("������Ϣ")]
    public string characterId;
    public CharacterType characterType;

    [Header("��ȷ��")]
    public string correctName;
    public string correctRole;
    public Sprite photo;

    [Header("ѡ�����ѡ��")]
    public string[] nameOptions;
    public string[] roleOptions;

    [Header("�����ǩ�����˷��ӡ����ӣ�")]
    public string[] badgeLabels;

    [Header("Ԥ����Ϣ����Ϸ��ʼʱֱ����ʾ��������Ԥ�")]
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