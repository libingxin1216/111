// Assets/Scripts/UI/NotebookTabItem.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

public class NotebookTabItem : MonoBehaviour,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    public Text tabNameText;
    public TMP_InputField renameInputField;
    public Button deleteButton;
    public Image background;

    private int tabIndex;
    private Action<int> onSelectCallback;
    private Action<int, string> onRenameCallback;
    private Action<int> onDeleteCallback;

    private bool isRenaming = false;
    private Coroutine longPressCoroutine;
    private const float LONG_PRESS_DURATION = 0.6f;

    public void Setup(int index, string name,
        Action<int> onSelect,
        Action<int, string> onRename,
        Action<int> onDelete)
    {
        tabIndex = index;
        onSelectCallback = onSelect;
        onRenameCallback = onRename;
        onDeleteCallback = onDelete;

        tabNameText.text = name;
        tabNameText.gameObject.SetActive(true);
        renameInputField.gameObject.SetActive(false);
        deleteButton.gameObject.SetActive(false);

        renameInputField.onSubmit.AddListener(ConfirmRename);
        renameInputField.onEndEdit.AddListener(ConfirmRename);

        deleteButton.onClick.AddListener(() =>
        {
            tabNameText.gameObject.SetActive(true);
            deleteButton.gameObject.SetActive(false);
            onDeleteCallback?.Invoke(tabIndex);
        });
    }

    public void UpdateName(string newName)
    {
        tabNameText.text = newName;
    }

    public void SetHighlight(bool highlight)
    {
        background.color = highlight
            ? new Color(0.9f, 0.85f, 0.7f, 1f)
            : new Color(1f, 1f, 1f, 1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        longPressCoroutine = StartCoroutine(LongPressCoroutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isRenaming) return;

        if (eventData.clickCount == 1)
        {
            // 恢复标签名显示，隐藏删除按钮
            tabNameText.gameObject.SetActive(true);
            deleteButton.gameObject.SetActive(false);

            onSelectCallback?.Invoke(tabIndex);
        }
        else if (eventData.clickCount == 2)
        {
            StartRename();
        }
    }

    IEnumerator LongPressCoroutine()
    {
        yield return new WaitForSeconds(LONG_PRESS_DURATION);

        // 隐藏标签名，显示删除按钮
        tabNameText.gameObject.SetActive(false);
        deleteButton.gameObject.SetActive(true);
    }

    void StartRename()
    {
        isRenaming = true;
        tabNameText.gameObject.SetActive(false);
        renameInputField.gameObject.SetActive(true);
        renameInputField.text = tabNameText.text;
        renameInputField.Select();
        renameInputField.ActivateInputField();
    }

    void ConfirmRename(string newName)
    {
        isRenaming = false;
        renameInputField.gameObject.SetActive(false);
        tabNameText.gameObject.SetActive(true);

        if (!string.IsNullOrWhiteSpace(newName))
            onRenameCallback?.Invoke(tabIndex, newName);
    }
}