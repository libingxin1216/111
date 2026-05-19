// Assets/Scripts/MainScene/MainSceneController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSceneController : MonoBehaviour
{
    [Header("可点击的桌面道具（Button组件）")]
    public Button computerButton;       // 电脑
    public Button whiteboardButton;     // 白板/人物关系图入口
    public Button fileFolderButton;     // 文件夹/线索入口
    public Button doorButton;           // 门（触发同事到访）
    public Button notebookButton;       // 桌面上的笔记本（可选）

    [Header("门的动画")]
    public Animator doorAnimator;       // 门的Animator
                                        // Animator里准备一个"DoorKnock"触发器动画

    [Header("笔记本叠加层")]
    public GameObject notebookOverlayPanel; // 笔记本Panel的GameObject

    private bool isNotebookOpen = false;

    void Start()
    {
        // 绑定点击事件
        computerButton.onClick.AddListener(OnClickComputer);
        whiteboardButton.onClick.AddListener(OnClickWhiteboard);
        fileFolderButton.onClick.AddListener(OnClickFileFolder);
        doorButton.onClick.AddListener(OnClickDoor);
        notebookButton.onClick.AddListener(OnClickNotebook);

        // 初始化：根据游戏进度决定门是否可交互
        // 门默认不可点击，由进度触发
        if (doorButton != null)
            doorButton.interactable = false;
    }

    void OnClickComputer()
    {
        SceneTransitionManager.Instance.GoToScene("ComputerScene");
    }

    void OnClickWhiteboard()
    {
        SceneTransitionManager.Instance.GoToScene("RelationScene");
    }

    void OnClickFileFolder()
    {
        GameManager.Instance.ClearClueBadge();
        SceneTransitionManager.Instance.GoToScene("ClueScene");
    }

    void OnClickDoor()
    {
        // 触发门的敲门/开门动画，之后由动画事件或Coroutine触发同事对话
        if (doorAnimator != null)
            doorAnimator.SetTrigger("DoorKnock");
        // TODO: 触发同事对话框（第二期实现）
    }

    void OnClickNotebook()
    {
        isNotebookOpen = !isNotebookOpen;
        if (notebookOverlayPanel != null)
            notebookOverlayPanel.SetActive(isNotebookOpen);
    }

    /// <summary>
    /// 由GameManager在进度节点调用，激活门的交互
    /// </summary>
    public void ActivateDoorKnock()
    {
        if (doorButton != null)
            doorButton.interactable = true;

        // 播放门缝透光闪烁动画
        if (doorAnimator != null)
            doorAnimator.SetTrigger("DoorGlow");
    }
    public void GoToScene(string sceneName)
    {
        // 加载新场景前清空所有事件监听
        // 各脚本的 OnDestroy 会在场景卸载时自动执行 Off，
        // 但为了防止残留，在这里统一清理一次
        EventBus.Clear();
        SceneManager.LoadScene(sceneName);
    }
}