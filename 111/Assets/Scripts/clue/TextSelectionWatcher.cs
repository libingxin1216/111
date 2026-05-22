using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 挂载到含 TMP_InputField 的 GameObject 上，双保险监听文字选区：
///   • 优先使用 TMP 原生 onTextSelection / onEndTextSelection 事件（实时、准确）
///   • 同时保留逐帧轮询作为兜底（兼容旧版 TMP 或某些平台事件不触发的情况）
///
/// 有选中文字时回调 onTextSelected(selectedText, clueId)，
/// 选区清除时回调 onTextSelected("", clueId)。
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class TextSelectionWatcher : MonoBehaviour
{
    // ── 由外部调用 Init 注入 ──────────────────────────────────────────────
    private TMP_InputField         _inputField;
    private string                 _clueId;
    private Action<string, string> _onTextSelected;   // (selectedText, clueId)

    // ── 轮询用的上一帧位置缓存 ────────────────────────────────────────────
    private int    _lastAnchor;
    private int    _lastFocus;
    // 事件上报过的最后一条选区内容（去重，避免重复触发回调）
    private string _lastReported = "";

    // ════════════════════════════════════════════════════════════════════
    //  初始化
    // ════════════════════════════════════════════════════════════════════

    public void Init(TMP_InputField field, string clueId,
                     Action<string, string> onTextSelected)
    {
        _inputField     = field != null ? field : GetComponent<TMP_InputField>();
        _clueId         = clueId;
        _onTextSelected = onTextSelected;
        _lastAnchor     = 0;
        _lastFocus      = 0;
        _lastReported   = "";

        if (_inputField == null) return;

        // ── 订阅 TMP 原生事件（最可靠的方式）────────────────────────────
        _inputField.onTextSelection.AddListener(OnTMPTextSelected);
        _inputField.onEndTextSelection.AddListener(OnTMPEndTextSelected);
    }

    // ════════════════════════════════════════════════════════════════════
    //  TMP 原生事件处理
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// onTextSelection(fullText, startIndex, endIndex)
    /// 参数：fullText = InputField 全文；startIndex/endIndex = 字符索引范围
    /// </summary>
    void OnTMPTextSelected(string fullText, int startIndex, int endIndex)
    {
        if (startIndex == endIndex) return;

        int start  = Mathf.Min(startIndex, endIndex);
        int length = Mathf.Abs(endIndex - startIndex);
        if (start < 0 || start + length > fullText.Length) return;

        string selected = fullText.Substring(start, length).Trim();
        if (string.IsNullOrWhiteSpace(selected)) return;

        if (selected == _lastReported) return;   // 去重
        _lastReported = selected;
        _onTextSelected?.Invoke(selected, _clueId);
    }

    /// <summary>
    /// onEndTextSelection(fullText, startIndex, endIndex)
    /// startIndex == endIndex 说明选区已取消
    /// </summary>
    void OnTMPEndTextSelected(string fullText, int startIndex, int endIndex)
    {
        if (startIndex != endIndex) return;   // 仍有选区，不清除

        if (_lastReported == "") return;
        _lastReported = "";
        _onTextSelected?.Invoke("", _clueId);
    }

    // ════════════════════════════════════════════════════════════════════
    //  逐帧轮询兜底（兼容 onTextSelection 不触发的情况）
    // ════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (_inputField == null || _onTextSelected == null) return;

        int anchor = _inputField.selectionAnchorPosition;
        int focus  = _inputField.selectionFocusPosition;

        // 位置未变 → 跳过
        if (anchor == _lastAnchor && focus == _lastFocus) return;
        _lastAnchor = anchor;
        _lastFocus  = focus;

        if (anchor == focus)
        {
            // 选区已清除
            if (_lastReported != "")
            {
                _lastReported = "";
                _onTextSelected.Invoke("", _clueId);
            }
            return;
        }

        // 截取选区字符串
        int start  = Mathf.Min(anchor, focus);
        int length = Mathf.Abs(focus - anchor);
        string text = _inputField.text;
        if (start < 0 || start + length > text.Length) return;

        string selected = text.Substring(start, length).Trim();
        if (string.IsNullOrWhiteSpace(selected)) return;

        if (selected == _lastReported) return;   // 去重（与事件通道共享 _lastReported）
        _lastReported = selected;
        _onTextSelected.Invoke(selected, _clueId);
    }

    // ════════════════════════════════════════════════════════════════════
    //  清理
    // ════════════════════════════════════════════════════════════════════

    void OnDestroy()
    {
        if (_inputField == null) return;
        _inputField.onTextSelection.RemoveListener(OnTMPTextSelected);
        _inputField.onEndTextSelection.RemoveListener(OnTMPEndTextSelected);
    }
}
