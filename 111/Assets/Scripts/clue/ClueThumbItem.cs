using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 线索缩略图单项。
/// • 若 ClueCardConfig.useCardRenderer = true：调用 ClueCardRenderer 程序化构建卡片 UI，
///   并在卡片顶部附加图钉图形、右上角附加新线索红点。
/// • 否则退回原有 Sprite 显示逻辑（向后兼容）。
/// </summary>
public class ClueThumbItem : MonoBehaviour
{
    [SerializeField] private Image      thumbImage;
    [SerializeField] private GameObject newDotIndicator;

    private ClueData _data;
    public string ClueId => _data != null ? _data.clueId : "";

    // ── 图钉颜色（在程序化模式下动态创建） ────────────────────────────
    static readonly Color32 PinRed = new Color32(222, 54, 42, 255);

    public void Setup(ClueData clueData, Action<ClueData> onClickCallback)
    {
        _data = clueData;

        bool useRenderer = ClueCardConfig.Instance != null
                        && ClueCardConfig.Instance.useCardRenderer;

        if (useRenderer)
            SetupWithRenderer(clueData, onClickCallback);
        else
            SetupLegacy(clueData, onClickCallback);
    }

    // ════════════════════════════════════════════════════════════════════
    //  程序化渲染模式
    // ════════════════════════════════════════════════════════════════════
    void SetupWithRenderer(ClueData clue, Action<ClueData> onClickCallback)
    {
        // 隐藏原有 Sprite 图
        if (thumbImage != null) thumbImage.gameObject.SetActive(false);

        // ── 构建卡片 ──────────────────────────────────────────────────
        var (cardGo, cardSize) = ClueCardRenderer.BuildThumbnail(clue, transform);
        var cardRt = cardGo.GetComponent<RectTransform>();
        cardRt.anchoredPosition = new Vector2(0, -14); // 给图钉留出顶部空间

        // ── 设置自身尺寸，并通过 LayoutElement 告知父布局组 ────────────
        float itemW = cardSize.x + 10f;
        float itemH = cardSize.y + 20f;

        var selfRt = GetComponent<RectTransform>();
        if (selfRt != null) selfRt.sizeDelta = new Vector2(itemW, itemH);

        // LayoutElement 让 HorizontalLayoutGroup（无论 childControlWidth 是 true/false）
        // 都能正确获取到每个卡片的宽高，从而依次横向排开
        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        le.minWidth       = itemW;
        le.preferredWidth = itemW;
        le.minHeight      = itemH;
        le.preferredHeight = itemH;

        // ── 图钉 ──────────────────────────────────────────────────────
        AddPin(cardSize.x);

        // ── 新线索红点 ─────────────────────────────────────────────────
        var dotGo = newDotIndicator;
        if (dotGo == null)
        {
            dotGo = new GameObject("NewDot");
            dotGo.transform.SetParent(transform, false);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = new Color32(231, 76, 60, 255);
            var dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(10, 10);
            dotRt.anchoredPosition = new Vector2(cardSize.x / 2 + 1, cardSize.y / 2 - 2);
            dotGo.AddComponent<DotPulse>();
        }
        dotGo.SetActive(true);

        // ── 点击 ──────────────────────────────────────────────────────
        var btn = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            dotGo.SetActive(false);
            onClickCallback?.Invoke(_data);
        });

        // ── Hover 效果（轻微上移） ────────────────────────────────────
        var hover = gameObject.GetComponent<ThumbHoverEffect>()
                 ?? gameObject.AddComponent<ThumbHoverEffect>();
        hover.target = cardGo;
    }

    void AddPin(float cardWidth)
    {
        var pin = new GameObject("Pin");
        pin.transform.SetParent(transform, false);
        var pinImg = pin.AddComponent<Image>();
        pinImg.color = PinRed;
        var pinRt = pin.GetComponent<RectTransform>();
        pinRt.sizeDelta = new Vector2(12, 12);
        pinRt.anchoredPosition = new Vector2(0, -6);   // 卡片顶部正上方

        // 图钉针
        var needle = new GameObject("Needle");
        needle.transform.SetParent(pin.transform, false);
        var needleImg = needle.AddComponent<Image>();
        needleImg.color = new Color32(130, 100, 60, 255);
        var needleRt = needle.GetComponent<RectTransform>();
        needleRt.sizeDelta = new Vector2(2, 9);
        needleRt.anchoredPosition = new Vector2(0, -10.5f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Fallback：原有 Sprite 逻辑（向后兼容）
    // ════════════════════════════════════════════════════════════════════
    void SetupLegacy(ClueData clue, Action<ClueData> onClickCallback)
    {
        if (thumbImage != null && clue.thumbnail != null)
            thumbImage.sprite = clue.thumbnail;

        if (newDotIndicator != null) newDotIndicator.SetActive(true);

        // 同样向父布局组声明自身尺寸（使用 Prefab 里已有的 sizeDelta）
        var selfRt = GetComponent<RectTransform>();
        if (selfRt != null)
        {
            float w = selfRt.sizeDelta.x > 1f ? selfRt.sizeDelta.x : 130f;
            float h = selfRt.sizeDelta.y > 1f ? selfRt.sizeDelta.y : 180f;
            var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            le.minWidth        = w;
            le.preferredWidth  = w;
            le.minHeight       = h;
            le.preferredHeight = h;
        }

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (newDotIndicator != null) newDotIndicator.SetActive(false);
                onClickCallback?.Invoke(_data);
            });
        }
    }
}

// ────────────────────────────────────────────────────────────────────────
//  辅助组件：红点脉冲动画
// ────────────────────────────────────────────────────────────────────────
public class DotPulse : MonoBehaviour
{
    float _t;
    void Update()
    {
        _t += Time.deltaTime * 2.5f;
        float s = 1f + Mathf.Sin(_t) * 0.22f;
        transform.localScale = Vector3.one * s;
    }
}

// ────────────────────────────────────────────────────────────────────────
//  辅助组件：鼠标悬停时卡片上移 6px
// ────────────────────────────────────────────────────────────────────────
public class ThumbHoverEffect : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    public GameObject target;
    private Vector2 _basePos;
    private bool    _initialized;
    private bool    _isHovered;
    private float   _t;
    const float DURATION = 0.12f;
    const float LIFT     = 6f;

    void Start()
    {
        if (target != null)
        {
            _basePos     = target.GetComponent<RectTransform>().anchoredPosition;
            _initialized = true;
        }
    }

    void Update()
    {
        if (!_initialized) return;
        var rt = target.GetComponent<RectTransform>();
        _t = Mathf.Clamp01(_t + ((_isHovered ? 1 : -1) * Time.deltaTime / DURATION));
        float eased = _t * _t * (3f - 2f * _t);     // smoothstep
        rt.anchoredPosition = _basePos + new Vector2(0, eased * LIFT);
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData _) => _isHovered = true;
    public void OnPointerExit (UnityEngine.EventSystems.PointerEventData _) => _isHovered = false;
}
