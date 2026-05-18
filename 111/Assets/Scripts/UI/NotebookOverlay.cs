// Assets/Scripts/UI/NotebookOverlay.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NotebookOverlay : MonoBehaviour
{
    [Header("UI组件")]
    public TMP_InputField notebookInputField; // 笔记输入框
    public Button closeButton;               // 关闭按钮（可选）

    private Coroutine autoSaveCoroutine;
    private const float AUTO_SAVE_DELAY = 1f; // 停止输入1秒后保存

    void OnEnable()
    {
        // 打开时读取已保存内容
        if (GameManager.Instance != null)
            notebookInputField.text = GameManager.Instance.NotebookContent;

        notebookInputField.onValueChanged.AddListener(OnTextChanged);
        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    void OnDisable()
    {
        // 关闭时立即保存
        SaveNow();
        notebookInputField.onValueChanged.RemoveListener(OnTextChanged);
    }

    void OnTextChanged(string newText)
    {
        // 重置自动保存计时
        if (autoSaveCoroutine != null)
            StopCoroutine(autoSaveCoroutine);
        autoSaveCoroutine = StartCoroutine(AutoSaveAfterDelay());
    }

    IEnumerator AutoSaveAfterDelay()
    {
        yield return new WaitForSeconds(AUTO_SAVE_DELAY);
        SaveNow();
    }

    void SaveNow()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.NotebookContent = notebookInputField.text;
    }

    /// <summary>
    /// 从线索界面"标记到笔记"功能调用
    /// 追加文本到笔记末尾
    /// </summary>
    public void AppendToNotebook(string text, string sourceLabel)
    {
        string appendText = $"\n\n【来自{sourceLabel}】\n{text}";
        notebookInputField.text += appendText;
        // 滚动到底部
        notebookInputField.caretPosition = notebookInputField.text.Length;
    }
}