using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏进度")]
    // 这里才是线索数据的唯一真相来源，跨场景不丢失
    public List<string> SavedUnlockedClueIds = new List<string>();
    public HashSet<string> UnlockedSearchTerms = new HashSet<string>();
    public Dictionary<string, CharacterProgress> CharacterProgressMap
        = new Dictionary<string, CharacterProgress>();
    public List<NotebookTabData> NotebookTabs = new List<NotebookTabData>();
    public bool HasNewClue = false;

    [Header("当前状态")]
    public string CurrentScene = "MainScene";

    [Header("人物照片解锁")]
    public Dictionary<string, Sprite> UnlockedPhotos = new Dictionary<string, Sprite>();

    [Header("剧情阶段进度（由 StageProgressionManager 维护）")]
    [Tooltip("已触发过的阶段编号列表（防止重复解锁）。普通阶段 1/2/3，刘陆特殊阶段 99。")]
    public List<int> ClearedStagesList = new List<int>();

    /// <summary>判断阶段是否已触发过</summary>
    public bool HasClearedStage(int stage) => ClearedStagesList.Contains(stage);

    /// <summary>标记阶段已触发</summary>
    public void MarkStageCleared(int stage)
    {
        if (!ClearedStagesList.Contains(stage))
            ClearedStagesList.Add(stage);
    }

    [Header("全局中文字体（拖入 SIMHEI.asset，所有场景共用）")]
    [Tooltip("拖入 Assets/Font/SIMHEI.asset（或其他含中文字形的 TMP SDF 字体）。\n" +
             "GameManager 是 DontDestroyOnLoad，所有场景均可从此处获取字体。")]
    public TMP_FontAsset chineseFont;

    /// <summary>
    /// 获取中文字体：优先用 GameManager 字段，其次从 ClueCardRenderer 取。
    /// 任何脚本均可调用此静态方法，无需关心当前场景。
    /// </summary>
    public static TMP_FontAsset GetChineseFont()
    {
        // 1. GameManager（DontDestroyOnLoad，全场景可用）
        if (Instance != null && Instance.chineseFont != null)
            return Instance.chineseFont;
        // 2. ClueCardRenderer 注入的字体（ClueScene 有效）
        var rendererFont = ClueCardRenderer.GetFont();
        if (rendererFont != null) return rendererFont;
        // 3. 内存中已加载的 SIMHEI（场景引用过的情况）
        foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (f != null && (f.name.IndexOf("SIMHEI", System.StringComparison.OrdinalIgnoreCase) >= 0
                           || f.name.IndexOf("黑", System.StringComparison.OrdinalIgnoreCase) >= 0))
                return f;
        // 4. 最终回退 TMP 默认字体
        return TMP_Settings.defaultFontAsset;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化默认笔记本标签
        if (NotebookTabs.Count == 0)
            NotebookTabs.Add(new NotebookTabData("默认笔记"));

        // 监听场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // 若 StageProgressionManager 存在，由它负责初始线索发放，
        // 此处只做兜底（保证老 CLU_001/CLU_002 等已有 ScriptableObject 仍可工作）
        if (StageProgressionManager.Instance == null)
        {
            AddClueToSave("CLU_001");
            AddClueToSave("CLU_002");
        }
        // 初始化默认笔记本标签
        if (NotebookTabs.Count == 0)
            NotebookTabs.Add(new NotebookTabData("默认笔记"));

        // 通知当前场景的ClueSystem同步数据
        SyncClueSystemIfExists();
    }

    // 场景加载完成后自动触发
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 每次新场景加载完，把存好的线索同步给新场景的ClueSystem
        SyncClueSystemIfExists();
    }

    // 把GameManager里存的线索ID全部同步给ClueSystem
    public void SyncClueSystemIfExists()
    {
        // 优先使用静态 Instance（Awake 里赋值），避免因 GameObject 处于非激活
        // 状态时 FindObjectOfType 返回 null 的问题。
        var clueSystem = ClueSystem.Instance
                      ?? FindObjectOfType<ClueSystem>(true);   // fallback
        if (clueSystem == null) return;

        foreach (var clueId in SavedUnlockedClueIds)
            clueSystem.RestoreClue(clueId);

        // 同步完毕后通知 CluePanelController 刷新显示
        var cluePanel = CluePanelController.Instance
                     ?? FindObjectOfType<CluePanelController>(true);  // fallback
        if (cluePanel != null)
            cluePanel.RefreshAllClues();
    }

    // 解锁一条线索，同时存入持久化列表
    public void AddClueToSave(string clueId)
    {
        if (!SavedUnlockedClueIds.Contains(clueId))
            SavedUnlockedClueIds.Add(clueId);
    }

    public void UnlockCharacterPhoto(string characterId, Sprite photo)
    {
        if (photo == null) return;
        if (!UnlockedPhotos.ContainsKey(characterId))
            UnlockedPhotos[characterId] = photo;
    }

    public void AddClue(string clueId)
    {
        AddClueToSave(clueId);
        HasNewClue = true;
        NavigationBar.Instance?.UpdateClueBadge(true);
    }

    public void ClearClueBadge()
    {
        HasNewClue = false;
        NavigationBar.Instance?.UpdateClueBadge(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  人物进度：存取（供 CharacterCard 使用）
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取指定人物的进度记录；若不存在则自动创建空记录。
    /// </summary>
    public CharacterProgress GetOrCreateCharacterProgress(string characterId)
    {
        if (!CharacterProgressMap.ContainsKey(characterId))
            CharacterProgressMap[characterId] = new CharacterProgress
                { CharacterId = characterId };
        return CharacterProgressMap[characterId];
    }

    /// <summary>
    /// 将人物当前状态快照写入 CharacterProgressMap（场景切换后可恢复）。
    /// </summary>
    public void SaveCharacterProgress(string characterId,
        string currentName, string currentRole, bool isLocked)
    {
        var p           = GetOrCreateCharacterProgress(characterId);
        p.FilledName    = currentName  ?? "";
        p.FilledIdentity = currentRole ?? "";
        p.IsLocked      = isLocked;
    }
}

[System.Serializable]
public class CharacterProgress
{
    public string CharacterId;
    public string FilledName     = "";
    public string FilledIdentity = "";
    public string FilledPhotoId  = "";  // 保留旧字段，向后兼容
    public bool   IsLocked       = false;
}