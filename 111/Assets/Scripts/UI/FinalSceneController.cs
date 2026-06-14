// Assets/Scripts/UI/FinalSceneController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 最终结局场景控制器 —— 挂在 FinalScene 的 Canvas 上。
///
/// "重新侦破"按钮：跳转回主场景，继续游玩。
/// "返回标题"按钮：重置开始界面状态后跳转回主场景，使开始界面面板重新显示。
/// </summary>
public class FinalSceneController : MonoBehaviour
{
    [Tooltip("\"重新侦破\"按钮")]
    public Button reinvestigateButton;

    [Tooltip("\"返回标题\"按钮")]
    public Button returnToTitleButton;

    void Start()
    {
        reinvestigateButton?.onClick.AddListener(OnReinvestigate);
        returnToTitleButton?.onClick.AddListener(OnReturnToTitle);
    }

    void OnReinvestigate()
    {
        StartScreenController.MarkGameStarted();
        GoToMainScene();
    }

    void OnReturnToTitle()
    {
        StartScreenController.ResetToTitle();
        GoToMainScene();
    }

    void GoToMainScene()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.GoToScene("MainScene");
        else
            SceneManager.LoadScene("MainScene");
    }
}
