using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 挂载到包含 TMP_InputField 的 GameObject 上。
/// 每帧检测选区变化，有选中文字时回调 OnTextSelected(selectedText, clueId)，
/// 清除选区时回调 OnTextSelected("", clueId)。
/// CluePanelController 用它来驱动"标记到笔记"按钮的显隐。
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class TextSelectionWatcher : MonoBehaviour
{
    // ── 由外部调用 Init 注入 ──────────────────────────────────────────────
    private TMP_InputField _inputField;
    private string         _clueId;
    private Action<string, string> _onTextSelected;   // (selectedText, clueId)

    // ── 用于判断选区是否变化（避免每帧都触发回调）──────────────────────
    private int _lastAnchor = 0;
    private int _lastFocus  = 0;

    public void Init(TMP_InputField field, string clueId,
                     Action<string, string> onTextSelected)
    {
        _inputField      = field != null ? field : GetComponent<TMP_InputField>();
        _clueId          = clueId;
        _onTextSelected  = onTextSelected;
        _lastAnchor      = 0;
        _lastFocus       = 0;
    }

    void Update()
    {
        if (_inputField == null || _onTextSelected == null) return;

        int anchor = _inputField.selectionAnchorPosition;
        int focus  = _inputField.selectionFocusPosition;

        // 没有变化则跳过
        if (anchor == _lastAnchor && focus == _lastFocus) return;

        _lastAnchor = anchor;
        _lastFocus  = focus;

        if (anchor == focus)
        {
            // 选区已清除
            _onTextSelected.Invoke("", _clueId);
            return;
        }

        // 安全地截取选中字符串
        int start  = Mathf.Min(anchor, focus);
        int length = Mathf.Abs(focus - anchor);
        string text = _inputField.text;

        if (start < 0 || start + length > text.Length) return;

        string selected = text.Substring(start, length);
        if (!string.IsNullOrWhiteSpace(selected))
            _onTextSelected.Invoke(selected.Trim(), _clueId);
    }
}
