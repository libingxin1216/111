using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SelectionPanel : MonoBehaviour
{
    public static SelectionPanel Instance;

    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform optionContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Button closeOverlay;

    [Header("面板设置")]
    [SerializeField] private float buttonHeight = 40f;
    [SerializeField] private float buttonSpacing = 4f;
    [SerializeField] private float panelMaxHeight = 200f;
    [SerializeField] private float panelWidth = 180f;

    private Action<string> currentCallback;
    private List<GameObject> spawnedButtons = new();

    private float totalContentHeight = 0f;
    private float currentScrollY = 0f;
    private float dragStartMouseY = 0f;
    private float dragStartScrollY = 0f;

    private Canvas cachedCanvas;

    void Awake()
    {
        Instance = this;
        cachedCanvas = GetComponentInParent<Canvas>();
        panelRoot.gameObject.SetActive(false);
        closeOverlay?.onClick.AddListener(Hide);
    }

    public void Show(string[] options, Action<string> callback,
                     RectTransform anchor)
    {
        currentCallback = callback;
        currentScrollY = 0f;

        // 清空旧按钮
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();

        // 生成按钮，全部用绝对定位
        for (int i = 0; i < options.Length; i++)
        {
            var btnGo = Instantiate(optionButtonPrefab, optionContainer);
            var rect = btnGo.GetComponent<RectTransform>();

            // 锚点和 Pivot 全部设为左上角，便于绝对定位
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(panelWidth, buttonHeight);
            // 初始位置：从顶部往下排列
            rect.anchoredPosition = new Vector2(0,
                -i * (buttonHeight + buttonSpacing));

            var tmp = btnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = options[i];

            string captured = options[i];
            btnGo.GetComponent<Button>().onClick.AddListener(() => {
                currentCallback?.Invoke(captured);
                Hide();
            });
            spawnedButtons.Add(btnGo);
        }

        // 计算总高度
        totalContentHeight = options.Length * buttonHeight
                           + Mathf.Max(0, options.Length - 1) * buttonSpacing;

        // 设置 panelRoot 宽高（可视窗口）
        float visibleH = Mathf.Min(totalContentHeight, panelMaxHeight);
        panelRoot.sizeDelta = new Vector2(panelWidth, visibleH);

        // 设置 optionContainer 和 panelRoot 一样大
        // （Rect Mask 2D 会按 panelRoot 裁切，optionContainer 只是按钮的父容器）
        optionContainer.anchorMin = new Vector2(0, 1);
        optionContainer.anchorMax = new Vector2(0, 1);
        optionContainer.pivot = new Vector2(0, 1);
        optionContainer.sizeDelta = new Vector2(panelWidth, totalContentHeight);
        optionContainer.anchoredPosition = Vector2.zero;

        // 定位 panelRoot 到点击的按钮下方
        PositionPanel(anchor);

        panelRoot.gameObject.SetActive(true);
        closeOverlay.gameObject.SetActive(true);
    }

    void PositionPanel(RectTransform anchor)
    {
        // 把 anchor 的屏幕坐标转换到 Canvas 本地坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            cachedCanvas.worldCamera, anchor.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cachedCanvas.GetComponent<RectTransform>(),
            screenPos,
            cachedCanvas.worldCamera,
            out Vector2 localPos);

        // 面板出现在锚点正下方
        panelRoot.anchoredPosition = new Vector2(localPos.x, localPos.y - anchor.sizeDelta.y);
    }

    // ── 由 PanelDragHandler 调用 ──────────────────────────
    public void OnBeginDrag(PointerEventData e)
    {
        dragStartMouseY = e.position.y;
        dragStartScrollY = currentScrollY;
    }

    public void OnDrag(PointerEventData e)
    {
        float visibleH = panelRoot.sizeDelta.y;
        float maxScroll = Mathf.Max(0, totalContentHeight - visibleH);
        float delta = e.position.y - dragStartMouseY;

        // 向上拖（delta < 0）→ 内容向上滚（scrollY 增大）
        currentScrollY = Mathf.Clamp(dragStartScrollY - delta, 0, maxScroll);

        // 移动 optionContainer 实现滚动
        optionContainer.anchoredPosition = new Vector2(0, currentScrollY);
    }

    public void Hide()
    {
        panelRoot.gameObject.SetActive(false);
        closeOverlay.gameObject.SetActive(false);
    }
}