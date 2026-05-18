// Assets/Scripts/Core/GameManager.cs
using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏进度")]
    // 已收集的线索ID集合
    public HashSet<string> CollectedClues = new HashSet<string>();
    // 已解锁的搜索词
    public HashSet<string> UnlockedSearchTerms = new HashSet<string>();
    // 人物信息填写进度 key=人物ID, value=填写状态
    public Dictionary<string, CharacterProgress> CharacterProgressMap
        = new Dictionary<string, CharacterProgress>();
    // 笔记本内容
    public string NotebookContent = "";
    // 新线索红点提示
    public bool HasNewClue = false;

    [Header("当前状态")]
    public string CurrentScene = "MainScene";

    void Awake()
    {
        // 单例模式，跨场景保活
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 添加线索，同时标记红点
    /// </summary>
    public void AddClue(string clueId)
    {
        if (!CollectedClues.Contains(clueId))
        {
            CollectedClues.Add(clueId);
            HasNewClue = true;
            // 通知导航栏更新红点
            NavigationBar.Instance?.UpdateClueBadge(true);
        }
    }

    /// <summary>
    /// 玩家打开线索界面后清除红点
    /// </summary>
    public void ClearClueBadge()
    {
        HasNewClue = false;
        NavigationBar.Instance?.UpdateClueBadge(false);
    }
}

/// <summary>
/// 每个人物的填写进度
/// </summary>
[System.Serializable]
public class CharacterProgress
{
    public string CharacterId;
    public string FilledName = "";
    public string FilledIdentity = "";
    public string FilledPhotoId = "";
    public bool IsLocked = false; // 信息固定后为true
}