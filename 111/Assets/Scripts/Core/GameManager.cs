using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏进度")]
    // 这里才是线索数据的唯一真相来源，跨场景不丢失
    public List<string> SavedUnlockedClueIds = new List<string>();
    public HashSet<string> UnlockedSearchTerms = new HashSet<string>();
    public Dictionary<string, CharacterProgress> CharacterProgressMap
        = new Dictionary<string, CharacterProgress>();
    public string NotebookContent = "";
    public bool HasNewClue = false;

    [Header("当前状态")]
    public string CurrentScene = "MainScene";

    [Header("人物照片解锁")]
    public Dictionary<string, Sprite> UnlockedPhotos = new Dictionary<string, Sprite>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 监听场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // 开局解锁初始线索，存入GameManager
        AddClueToSave("CLU_001");
        AddClueToSave("CLU_002");

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
        var clueSystem = FindObjectOfType<ClueSystem>();
        if (clueSystem == null) return;

        foreach (var clueId in SavedUnlockedClueIds)
            clueSystem.RestoreClue(clueId);

        // 同步完毕后通知CluePanelController刷新显示
        var cluePanel = FindObjectOfType<CluePanelController>(true);
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
}

[System.Serializable]
public class CharacterProgress
{
    public string CharacterId;
    public string FilledName = "";
    public string FilledIdentity = "";
    public string FilledPhotoId = "";
    public bool IsLocked = false;
}