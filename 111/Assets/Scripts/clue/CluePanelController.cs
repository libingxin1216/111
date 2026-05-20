using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CluePanelController : MonoBehaviour
{
    public static CluePanelController Instance;

    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject clueThumbPrefab;
    [SerializeField] private GameObject detailOverlay;
    [SerializeField] private Image detailImage;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Button closeDetailBtn;
    [SerializeField] private Button backdropBtn;
    [SerializeField] private Button markToNoteBtn;

    private ClueData currentDetailClue;
    private bool listenersRegistered = false;

    void Awake()
    {
        Instance = this;
        detailOverlay.SetActive(false);
        closeDetailBtn.onClick.AddListener(CloseDetail);
        backdropBtn.onClick.AddListener(CloseDetail);
        markToNoteBtn.onClick.AddListener(MarkCurrentClueToNote);
    }

    void Start()
    {
        // Start时主动向GameManager请求一次同步
        // 此时ClueSystem已经Awake完毕，数据可以正常读取
        GameManager.Instance?.SyncClueSystemIfExists();
    }

    void OnEnable()
    {
        if (!listenersRegistered)
        {
            EventBus.On("OnClueUnlocked", OnClueUnlocked);
            listenersRegistered = true;
        }
    }

    void OnDisable()
    {
        EventBus.Off("OnClueUnlocked", OnClueUnlocked);
        listenersRegistered = false;
    }

    void OnDestroy()
    {
        EventBus.Off("OnClueUnlocked", OnClueUnlocked);
    }

    // public，供GameManager跨场景调用
    public void RefreshAllClues()
    {
        if (ClueSystem.Instance == null) return;

        // 清空现有卡片
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        // 重新生成所有已解锁线索
        var clues = ClueSystem.Instance.GetUnlockedClues();
        foreach (var clue in clues)
            SpawnThumb(clue);
    }

    void OnClueUnlocked(object obj)
    {
        string clueId = (string)obj;
        var clues = ClueSystem.Instance.GetUnlockedClues();
        var clue = clues.Find(c => c.clueId == clueId);
        if (clue != null) SpawnThumb(clue);
    }

    void SpawnThumb(ClueData clue)
    {
        // 防止重复生成
        foreach (Transform child in contentRoot)
        {
            var existing = child.GetComponent<ClueThumbItem>();
            if (existing != null && existing.ClueId == clue.clueId)
                return;
        }

        var go = Instantiate(clueThumbPrefab, contentRoot);
        go.GetComponent<ClueThumbItem>().Setup(clue, OpenDetail);
    }

    void OpenDetail(ClueData clue)
    {
        currentDetailClue = clue;

        bool isTextType = clue.clueType == ClueType.Text;
        detailText.gameObject.SetActive(isTextType);
        detailImage.gameObject.SetActive(!isTextType);

        if (isTextType)
            detailText.text = clue.textContent;
        else
            detailImage.sprite = clue.fullImage;

        if (!string.IsNullOrEmpty(clue.characterIdToUnlockPhoto))
            EventBus.Emit("OnPhotoUnlocked", clue.characterIdToUnlockPhoto);

        detailOverlay.SetActive(true);
    }

    void CloseDetail() => detailOverlay.SetActive(false);

    void MarkCurrentClueToNote()
    {
        if (currentDetailClue == null) return;
        string content = currentDetailClue.clueType == ClueType.Text
            ? currentDetailClue.textContent
            : $"[线索图片：{currentDetailClue.clueId}]";

        var notebook = FindObjectOfType<NotebookOverlay>(true);
        if (notebook != null)
            notebook.AppendToNotebook(content, currentDetailClue.clueId);
    }

    public void OpenCluePanel() => gameObject.SetActive(true);
}