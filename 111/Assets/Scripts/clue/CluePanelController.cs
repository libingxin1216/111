using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 线索板主控制器（ClueScene）
/// ── 变更摘要 ──────────────────────────────────────────────────────────
/// 1. 新增 cardBuildParent（RectTransform）：程序化卡片会实例化到这里
/// 2. detailImage / detailText 仍保留，作为 fallback
/// 3. 文字选中 → markToNoteBtn 显示 / 隐藏由 TextSelectionWatcher 驱动
/// 4. 点击人脸 → 触发 OnPhotoUnlocked 事件
/// ─────────────────────────────────────────────────────────────────────
/// </summary>
public class CluePanelController : MonoBehaviour
{
    public static CluePanelController Instance;

    // ── 缩略图列表 ────────────────────────────────────────────────────
    [Header("缩略图容器")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject clueThumbPrefab;

    // ── 详情弹窗 ──────────────────────────────────────────────────────
    [Header("详情弹窗（原有字段，保持 Prefab 引用不变）")]
    [SerializeField] private GameObject  detailOverlay;
    [SerializeField] private Image       detailImage;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Button      closeDetailBtn;
    [SerializeField] private Button      backdropBtn;
    [SerializeField] private Button      markToNoteBtn;

    [Header("★ 新增：程序化卡片挂载点（在 detailOverlay 内新建空 GameObject）")]
    [Tooltip("程序化生成的全屏卡片 UI 会放到这里。推荐在 detailOverlay 内创建一个 'CardRoot' 空 GameObject 并拖入。")]
    [SerializeField] private RectTransform cardBuildParent;

    [Header("★ 返回主界面按钮（留空时自动在左上角创建）")]
    [Tooltip("指向场景中已有的返回按钮；若未赋值，运行时在 Canvas 左上角自动生成。")]
    [SerializeField] private Button returnToMainBtn;

    // ── 滚动支持 ──────────────────────────────────────────────────────
    [Header("可选：滚动容器（把 cardBuildParent 放进 ScrollRect 的 content 里）")]
    [SerializeField] private ScrollRect detailScrollRect;

    // ── 私有状态 ──────────────────────────────────────────────────────
    private ClueData  _currentClue;
    private string    _currentSelection = "";   // 当前选中的文字
    private bool      _listenersRegistered;
    private GameObject _builtCard;              // 当前构建的卡片 GO

    // ════════════════════════════════════════════════════════════════════
    //  Unity 生命周期
    // ════════════════════════════════════════════════════════════════════

    void Awake()
    {
        Instance = this;
        detailOverlay.SetActive(false);

        // markToNoteBtn 不涉及字体，可在 Awake 安全配置
        if (markToNoteBtn != null)
        {
            markToNoteBtn.gameObject.SetActive(false);
            markToNoteBtn.onClick.AddListener(MarkSelectedTextToNote);
        }

        // ⚠️ 按钮创建（EnsureDetailCloseButtons / EnsureReturnButton）已移至 Start()
        // 原因：创建 TMP 文字时需要从 ClueCardConfig 读取中文字体，
        //       而 ClueCardConfig.Awake() 可能在本 Awake() 之后才执行，
        //       导致字体为 null → 中文方块。
        // Unity 保证：所有脚本的 Awake() 全部结束后才进入 Start()，
        //       因此在 Start() 中调用可 100% 拿到正确字体。
    }

    // ════════════════════════════════════════════════════════════════════
    //  返回主界面按钮
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 若 returnToMainBtn 已在 Inspector 连线则直接绑定事件；
    /// 否则在场景 Canvas 左上角自动创建一个"← 主界面"按钮。
    /// </summary>
    void EnsureReturnButton()
    {
        if (returnToMainBtn != null)
        {
            // Inspector 已有按钮，直接绑定
            returnToMainBtn.onClick.AddListener(OnReturnToMain);
            return;
        }

        // ── 找到合适的 Canvas 父节点 ────────────────────────────────
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogWarning("[CluePanelController] 找不到 Canvas，无法创建返回按钮"); return; }

        // ── 创建按钮 GO ──────────────────────────────────────────────
        var go = new GameObject("_AutoReturnBtn");
        go.transform.SetParent(canvas.transform, false);

        var rt        = go.AddComponent<RectTransform>();
        rt.anchorMin  = new Vector2(0, 1);
        rt.anchorMax  = new Vector2(0, 1);
        rt.pivot      = new Vector2(0, 1);
        rt.sizeDelta  = new Vector2(140, 46);
        rt.anchoredPosition = new Vector2(16, -16);

        var img            = go.AddComponent<Image>();
        img.color          = new Color(0.08f, 0.08f, 0.08f, 0.80f);
        img.raycastTarget  = true;

        // ── "← 主界面" 标签 ─────────────────────────────────────────
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRt   = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;
        var tmp           = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text          = "←  主界面";
        tmp.fontSize      = 17;
        tmp.fontStyle     = TMPro.FontStyles.Bold;
        tmp.alignment     = TMPro.TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        // 必须指定含中文字形的字体，否则显示方块
        var chFont = GetChineseFont();
        if (chFont != null) tmp.font = chFont;

        // ── 按钮组件 ─────────────────────────────────────────────────
        var btn = go.AddComponent<Button>();
        // Hover 颜色反馈
        var colors          = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        colors.pressedColor     = new Color(0.4f,  0.4f,  0.4f,  1f);
        btn.colors          = colors;
        btn.onClick.AddListener(OnReturnToMain);

        returnToMainBtn = btn;
    }

    void OnReturnToMain()
    {
        GameManager.Instance?.ClearClueBadge();
        SceneTransitionManager.Instance.GoToScene("MainScene");
    }

    /// <summary>
    /// 获取支持中文的 TMP 字体。
    /// 优先使用 ClueCardRenderer 已注入的字体（由 ClueCardConfig 在 Awake 设置），
    /// 其次读 ClueCardConfig.chineseFont，最后退回 TMP 默认字体。
    /// </summary>
    static TMPro.TMP_FontAsset GetChineseFont()
        => ClueCardRenderer.GetFont()
        ?? ClueCardConfig.Instance?.chineseFont
        ?? TMPro.TMP_Settings.defaultFontAsset;

    /// <summary>
    /// 若 closeDetailBtn / markToNoteBtn 处于 ScrollRect 或 Mask 的内部，
    /// 运行时将其重新挂到 detailOverlay 层并设置正确的锚点位置，
    /// 确保它们始终固定显示、不随内容滚动。
    /// </summary>
    void FixDetailButtonPositions()
    {
        if (detailOverlay == null) return;
        var overlayT = detailOverlay.transform;

        // ── 关闭按钮 → 右上角，始终可见 ─────────────────────────────
        if (closeDetailBtn != null && closeDetailBtn.transform.parent != overlayT)
        {
            closeDetailBtn.transform.SetParent(overlayT, false);
            SetAnchoredRect(closeDetailBtn.GetComponent<RectTransform>(),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(120, 44), new Vector2(-16, -16));
            closeDetailBtn.transform.SetAsLastSibling();   // 渲染层最上
        }

        // ── 标记到笔记按钮 → 底部居中固定工具栏 ─────────────────────
        if (markToNoteBtn != null && markToNoteBtn.transform.parent != overlayT)
        {
            markToNoteBtn.transform.SetParent(overlayT, false);
            SetAnchoredRect(markToNoteBtn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(200, 48), new Vector2(0, 24));
            markToNoteBtn.transform.SetAsLastSibling();
        }
    }

    /// <summary>一行设置 RectTransform 的锚点、轴心、尺寸、偏移</summary>
    static void SetAnchoredRect(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 size, Vector2 pos)
    {
        if (rt == null) return;
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
    }

    /// <summary>
    /// 保底机制：若 backdropBtn / closeDetailBtn 在 Inspector 中未赋值，
    /// 则在 detailOverlay 内自动创建，确保玩家无论如何都能关闭详情。
    /// </summary>
    void EnsureDetailCloseButtons()
    {
        if (detailOverlay == null) return;

        // ── 背景遮罩按钮（点击卡片外侧空白区域关闭）────────────────────
        if (backdropBtn == null)
        {
            var go = new GameObject("_AutoBackdrop");
            go.transform.SetParent(detailOverlay.transform, false);
            go.transform.SetAsFirstSibling();      // 最底层，不会遮挡卡片内容

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color        = Color.clear;        // 透明但可以接收点击
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            backdropBtn = btn;
        }

        // ── 右上角 ✕ 关闭按钮（始终可见，无论卡片大小）────────────────
        if (closeDetailBtn == null)
        {
            var go = new GameObject("_AutoCloseBtn");
            go.transform.SetParent(detailOverlay.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(1, 1);
            rt.sizeDelta        = new Vector2(52, 52);
            rt.anchoredPosition = new Vector2(-16, -16);

            var img = go.AddComponent<Image>();
            img.color        = new Color(0f, 0f, 0f, 0.72f);
            img.raycastTarget = true;

            // ✕ 文字
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text          = "✕";
            tmp.fontSize      = 24;
            tmp.alignment     = TMPro.TextAlignmentOptions.Center;
            tmp.color         = Color.white;
            tmp.raycastTarget = false;
            var chFont2 = GetChineseFont();
            if (chFont2 != null) tmp.font = chFont2;

            var btn = go.AddComponent<Button>();
            closeDetailBtn = btn;
        }
    }

    /// <summary>ESC 键快捷关闭详情弹窗</summary>
    void Update()
    {
        if (detailOverlay != null && detailOverlay.activeSelf
            && Input.GetKeyDown(KeyCode.Escape))
            CloseDetail();
    }

    void Start()
    {
        // ── 此时所有 Awake() 已执行完毕，ClueCardConfig.Instance 和
        //    ClueCardRenderer._font 已就绪，可以安全使用中文字体 ────────
        EnsureContentRootLayout();

        // 自动创建/修复按钮（必须在 Start 而非 Awake，原因见 Awake 注释）
        EnsureDetailCloseButtons();
        FixDetailButtonPositions();
        EnsureReturnButton();

        // 绑定关闭事件（按钮可能刚在上面创建，所以也必须在 Start）
        closeDetailBtn?.onClick.AddListener(CloseDetail);
        backdropBtn?.onClick.AddListener(CloseDetail);

        GameManager.Instance?.SyncClueSystemIfExists();
    }

    /// <summary>
    /// 确保 contentRoot 拥有横向布局组件。
    /// 若 Inspector 里已配置好 HorizontalLayoutGroup 则不会重复添加，
    /// 仅在缺失时才动态补全，保证缩略图从左到右依次排开。
    /// </summary>
    void EnsureContentRootLayout()
    {
        if (contentRoot == null) return;

        // ── HorizontalLayoutGroup ─────────────────────────────────────
        var hlg = contentRoot.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
        {
            hlg = contentRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment        = TextAnchor.UpperLeft;
            hlg.spacing               = 20f;
            hlg.padding               = new RectOffset(16, 16, 40, 16);
            hlg.childControlWidth     = false;   // 子对象自控宽度
            hlg.childControlHeight    = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
        }

        // ── ContentSizeFitter（让容器随内容横向扩展，支持滚动）──────────
        var csf = contentRoot.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;
        }
    }

    void OnEnable()
    {
        if (!_listenersRegistered)
        {
            EventBus.On("OnClueUnlocked", OnClueUnlocked);
            _listenersRegistered = true;
        }
    }

    void OnDisable()
    {
        EventBus.Off("OnClueUnlocked", OnClueUnlocked);
        _listenersRegistered = false;
    }

    void OnDestroy()
    {
        EventBus.Off("OnClueUnlocked", OnClueUnlocked);
    }

    // ════════════════════════════════════════════════════════════════════
    //  缩略图管理
    // ════════════════════════════════════════════════════════════════════

    public void RefreshAllClues()
    {
        if (ClueSystem.Instance == null)
        {
            Debug.LogWarning("[CluePanelController] ClueSystem.Instance 为 null，无法刷新线索");
            return;
        }
        if (contentRoot == null)
        {
            Debug.LogError("[CluePanelController] contentRoot 未赋值！请在 Inspector 中拖入线索缩略图容器。");
            return;
        }

        // ⚠️ 不在此处用 Destroy 清空子节点！
        // 原因：RefreshAllClues 会被 GameManager.OnSceneLoaded 和
        //       CluePanelController.Start() 先后调用两次：
        //         第一次：contentRoot 为空 → SpawnThumb 正常创建缩略图
        //         第二次：Destroy 标记已有子节点（延迟到帧末执行），
        //                 但防重检查认为"已存在"跳过创建，
        //                 帧末子节点真正删除 → 面板空白
        // 解决方案：只做"追加"，SpawnThumb 内部已有防重复检测。
        // 场景重新加载时 contentRoot 会随场景一起销毁，下次打开是干净的空节点。
        foreach (var clue in ClueSystem.Instance.GetUnlockedClues())
            SpawnThumb(clue);

        // 强制立即重算布局，确保新增缩略图立刻依次排开，不必等到帧末
        var contentRt = contentRoot.GetComponent<RectTransform>();
        if (contentRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
    }

    void OnClueUnlocked(object obj)
    {
        string clueId = (string)obj;
        var clue = ClueSystem.Instance?.GetUnlockedClues().Find(c => c.clueId == clueId);
        if (clue != null)
        {
            SpawnThumb(clue);
            // 新线索追加后立刻重算排列
            var rt = contentRoot?.GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    void SpawnThumb(ClueData clue)
    {
        if (clueThumbPrefab == null)
        {
            Debug.LogError("[CluePanelController] clueThumbPrefab 未赋值！请在 Inspector 中拖入 ClueThumb 预制体。");
            return;
        }
        // 防重
        foreach (Transform child in contentRoot)
        {
            var existing = child.GetComponent<ClueThumbItem>();
            if (existing != null && existing.ClueId == clue.clueId) return;
        }
        var go = Instantiate(clueThumbPrefab, contentRoot);
        var item = go.GetComponent<ClueThumbItem>();
        if (item == null)
        {
            Debug.LogError("[CluePanelController] clueThumbPrefab 上缺少 ClueThumbItem 组件！");
            return;
        }
        item.Setup(clue, OpenDetail);
    }

    // ════════════════════════════════════════════════════════════════════
    //  详情弹窗
    // ════════════════════════════════════════════════════════════════════

    void OpenDetail(ClueData clue)
    {
        _currentClue      = clue;
        _currentSelection = "";
        if (markToNoteBtn != null) markToNoteBtn.gameObject.SetActive(false);

        // ── 清除上一张卡片 ──────────────────────────────────────────
        if (_builtCard != null) { Destroy(_builtCard); _builtCard = null; }

        bool useRenderer = (ClueCardConfig.Instance != null && ClueCardConfig.Instance.useCardRenderer
                            && cardBuildParent != null);

        if (useRenderer)
        {
            // ── 程序化渲染 ────────────────────────────────────────────
            if (detailImage != null) detailImage.gameObject.SetActive(false);
            if (detailText  != null) detailText.gameObject.SetActive(false);

            _builtCard = ClueCardRenderer.BuildFullCard(
                clue,
                cardBuildParent,
                onTextSelected: OnTextSelectionChanged,
                onFaceClicked:  OnFaceClicked);

            // 重置滚动位置
            if (detailScrollRect != null) detailScrollRect.verticalNormalizedPosition = 1f;
        }
        else
        {
            // ── Fallback：原有逻辑 ────────────────────────────────────
            bool isText = clue.clueType == ClueType.Text;
            if (detailText  != null) detailText.gameObject.SetActive(isText);
            if (detailImage != null) detailImage.gameObject.SetActive(!isText);

            if (isText && detailText != null)
                detailText.text = clue.textContent;
            else if (!isText && detailImage != null)
                detailImage.sprite = clue.fullImage;

            // Fallback 模式下"标记到笔记"标记整条
            if (markToNoteBtn != null) markToNoteBtn.gameObject.SetActive(true);
        }

        // ── 自动解锁人物照片 ──────────────────────────────────────────
        if (!string.IsNullOrEmpty(clue.characterIdToUnlockPhoto))
            EventBus.Emit("OnPhotoUnlocked", clue.characterIdToUnlockPhoto);

        // 显示叠加层，并确保关闭按钮可见（标记按钮由文字选中逻辑驱动）
        detailOverlay.SetActive(true);
        if (closeDetailBtn != null) closeDetailBtn.gameObject.SetActive(true);
    }

    void CloseDetail()
    {
        detailOverlay.SetActive(false);
        _currentSelection = "";
        if (markToNoteBtn != null) markToNoteBtn.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  文字选中回调（由 TextSelectionWatcher 驱动）
    // ════════════════════════════════════════════════════════════════════

    void OnTextSelectionChanged(string selectedText, string clueId)
    {
        _currentSelection = selectedText;
        bool hasSelection = !string.IsNullOrWhiteSpace(selectedText);
        if (markToNoteBtn != null)
            markToNoteBtn.gameObject.SetActive(hasSelection);
    }

    // ════════════════════════════════════════════════════════════════════
    //  人脸热区点击回调
    // ════════════════════════════════════════════════════════════════════

    void OnFaceClicked(string characterId, UnityEngine.Sprite photo)
    {
        if (string.IsNullOrEmpty(characterId)) return;
        if (photo == null)
        {
            Debug.LogWarning($"[CluePanelController] 人脸热区 {characterId} 未配置照片 Sprite，请在 ClueData.faceHotspots 里填写 characterPhoto。");
            return;
        }

        // 存入 GameManager（跨场景持久）
        GameManager.Instance?.UnlockCharacterPhoto(characterId, photo);

        // 通知人物关系图刷新（若 RelationScene 已加载则立即更新）
        EventBus.Emit("OnPhotoUnlocked", characterId);

        // 给玩家反馈：右下角短暂提示
        NewClueNotification.Instance?.ShowNotification("人物照片已解锁 →");
    }

    // ════════════════════════════════════════════════════════════════════
    //  标记到笔记
    // ════════════════════════════════════════════════════════════════════

    void MarkSelectedTextToNote()
    {
        var notebook = FindObjectOfType<NotebookOverlay>(true);
        if (notebook == null || _currentClue == null) return;

        bool useRenderer = (ClueCardConfig.Instance != null && ClueCardConfig.Instance.useCardRenderer);
        string content;
        string sourceLabel;

        if (useRenderer && !string.IsNullOrWhiteSpace(_currentSelection))
        {
            // 程序化模式：标记选中文字
            content     = _currentSelection;
            sourceLabel = !string.IsNullOrEmpty(_currentClue.clueTitle)
                        ? _currentClue.clueTitle
                        : _currentClue.clueId;
        }
        else
        {
            // Fallback：标记整条内容
            content = _currentClue.clueType == ClueType.Text
                    ? _currentClue.textContent
                    : $"[图片线索：{_currentClue.clueId}]";
            sourceLabel = _currentClue.clueId;
        }

        notebook.AppendText(content, sourceLabel);

        // 短暂高亮按钮给玩家反馈
        if (markToNoteBtn != null)
            StartCoroutine(FlashButton(markToNoteBtn));
    }

    System.Collections.IEnumerator FlashButton(Button btn)
    {
        var normalColors = btn.colors;
        var flash = btn.colors;
        flash.normalColor = new Color(0.2f, 0.78f, 0.35f);
        btn.colors = flash;
        yield return new WaitForSeconds(0.4f);
        btn.colors = normalColors;
    }

    // ════════════════════════════════════════════════════════════════════
    //  外部接口
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Fallback：整条标记（用于非程序化模式的 markToNoteBtn.onClick）</summary>
    public void MarkCurrentClueToNote()
    {
        if (_currentClue == null) return;
        _currentSelection = _currentClue.clueType == ClueType.Text
            ? _currentClue.textContent
            : $"[图片线索：{_currentClue.clueId}]";
        MarkSelectedTextToNote();
    }

    public void OpenCluePanel() => gameObject.SetActive(true);
}
