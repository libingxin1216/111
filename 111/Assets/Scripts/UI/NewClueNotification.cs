// Assets/Scripts/UI/NewClueNotification.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NewClueNotification : MonoBehaviour
{
    public static NewClueNotification Instance { get; private set; }

    [Header("UI")]
    public GameObject notificationPanel;  // 绿色通知条Panel
    public Text notificationText;
    public Button notificationButton;     // 点击跳转线索界面

    private const float AUTO_HIDE_DELAY = 3f;
    private Coroutine hideCoroutine;

    void Awake()
    {
        Instance = this;
        notificationPanel.SetActive(false);
    }

    void Start()
    {
        notificationButton.onClick.AddListener(OnClickNotification);
    }

    /// <summary>
    /// 显示"新线索已添加"通知
    /// </summary>
    public void ShowNotification(string message = "新线索已添加")
    {
        notificationText.text = message;
        notificationPanel.SetActive(true);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(AutoHide());
    }

    void OnClickNotification()
    {
        notificationPanel.SetActive(false);
        GameManager.Instance?.ClearClueBadge();
        SceneTransitionManager.Instance.GoToScene("ClueScene");
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(AUTO_HIDE_DELAY);
        notificationPanel.SetActive(false);
    }
}