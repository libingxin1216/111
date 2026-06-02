using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 挂载到含 TMP_InputField 的 GameObject 上，双保险监听文字选区：
///   • 优先使用 TMP 原生 onTextSelection / onEndTextSelection 事件（实时、准确）
///   • 同时保留逐帧轮询作为兜底（兼容旧版 TMP 或某些平台事件不触发的情况）
///
/// 防自动全选机制（_suppressFrames）：
///   TMP_InputField 在 detailOverlay.SetActive(true) 后被激活时，
///   会将 selectionFocusPosition 设为文字末尾（全选状态）。
///   前 2 帧忽略所有选区检测，抑制期结束后把光标归零，
///   确保玩家看到干净的无选中状态，第一次拖拽即可触发按钮。
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class TextSelectionWatcher : MonoBehaviour
{
    // ── 由外部调用 Init 注入 ──────────────────────────────────────────────
    private TMP_InputField         _inputField;
    private string                 _clueId;
    private Action<string, string> _onTextSelected;   // (selectedText, clueId)

    // 存储 onDeselect 监听器引用，以便 OnDestroy 时精确移除
    private UnityEngine.Events.UnityAction<string> _onDeselectHandler;

    // ── 轮询用的上一帧位置缓存 ────────────────────────────────────────────
    private int    _lastAnchor;
    private int    _lastFocus;
    private string _lastReported = "";

    // ── 自动全选抑制窗口 ──────────────────────────────────────────────────
    // >0 时忽略所有选区事件；归零时清除视觉选区并恢复检测
    private int _suppressFrames;

    // ════════════════════════════════════════════════════════════════════
    //  初始化
    // ════════════════════════════════════════════════════════════════════

    public void Init(TMP_InputField field, string clueId,
                     Action<string, string> onTextSelected)
    {
        _inputField      = field != null ? field : GetComponent<TMP_InputField>();
        _clueId          = clueId;
        _onTextSelected  = onTextSelected;
        _lastAnchor      = 0;
        _lastFocus       = 0;
        _lastReported    = "";

        // 前 2 帧抑制：屏蔽 TMP 激活时的自动全选事件
        // 2 帧 ≈ 33ms（60fps），对玩家不可感知
        _suppressFrames  = 2;

        if (_inputField == null) return;

        // ── 订阅 TMP 原生事件 ─────────────────────────────────────────
        _inputField.onTextSelection.AddListener(OnTMPTextSelected);
        _inputField.onEndTextSelection.AddListener(OnTMPEndTextSelected);

        // 失去焦点时重置状态，确保下次聚焦能正确检测新选区
        _onDeselectHandler = _ =>
        {
            _lastAnchor   = 0;
            _lastFocus    = 0;
            _lastReported = "";
            _onTextSelected?.Invoke("", _clueId);
        };
        _inputField.onDeselect.AddListener(_onDeselectHandler);
    }

    // ════════════════════════════════════════════════════════════════════
    //  TMP 原生事件处理
    // ════════════════════════════════════════════════════════════════════

    void OnTMPTextSelected(string fullText, int startIndex, int endIndex)
    {
        if (_suppressFrames > 0) return;   // 抑制窗口内忽略

        if (startIndex == endIndex) return;

        int start  = Mathf.Min(startIndex, endIndex);
        int length = Mathf.Abs(endIndex - startIndex);
        if (start < 0 || start + length > fullText.Length) return;

        string selected = fullText.Substring(start, length).Trim();
        if (string.IsNullOrWhiteSpace(selected)) return;

        if (selected == _lastReported) return;
        _lastReported = selected;
        _onTextSelected?.Invoke(selected, _clueId);
    }

    void OnTMPEndTextSelected(string fullText, int startIndex, int endIndex)
    {
        if (_suppressFrames > 0) return;   // 抑制窗口内忽略

        if (startIndex != endIndex) return;

        if (_lastReported == "") return;
        _lastReported = "";
        _onTextSelected?.Invoke("", _clueId);
    }

    // ════════════════════════════════════════════════════════════════════
    //  逐帧轮询兜底
    // ════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (_inputField == null || _onTextSelected == null) return;

        // ── 抑制窗口处理 ─────────────────────────────────────────────
        if (_suppressFrames > 0)
        {
            _suppressFrames--;
            if (_suppressFrames == 0)
            {
                // 抑制结束：清空视觉选区和轮询缓存
                // stringPosition = 0 将光标归零并去除全选高亮，不触发 onTextSelection 事件
                _inputField.stringPosition = 0;
                _lastAnchor = 0;
                _lastFocus  = 0;
            }
            return;   // 抑制期间不做检测
        }

        // ── 正常轮询 ──────────────────────────────────────────────────
        int anchor = _inputField.selectionAnchorPosition;
        int focus  = _inputField.selectionFocusPosition;

        if (anchor == _lastAnchor && focus == _lastFocus) return;

        _lastAnchor = anchor;
        _lastFocus  = focus;

        if (anchor == focus)
        {
            if (_lastReported != "")
            {
                _lastReported = "";
                _onTextSelected.Invoke("", _clueId);
            }
            return;
        }

        int start  = Mathf.Min(anchor, focus);
        int length = Mathf.Abs(focus - anchor);
        string text = _inputField.text;
        if (start < 0 || start + length > text.Length) return;

        string selected = text.Substring(start, length).Trim();
        if (string.IsNullOrWhiteSpace(selected)) return;

        if (selected == _lastReported) return;
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
        if (_onDeselectHandler != null)
            _inputField.onDeselect.RemoveListener(_onDeselectHandler);
    }
}
