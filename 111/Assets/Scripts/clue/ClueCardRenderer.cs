using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 运行时动态构建各类型线索卡片 UI。
/// 支持两种模式：
///   • Thumbnail（缩略图）——用于线索板横向列表
///   • FullCard（全屏详情）——用于放大查看弹窗
///
/// 使用前须调用 ClueCardConfig.Instance.Apply() 注入字体，
/// 或直接在 ClueCardConfig 脚本 Awake 里自动注入。
/// </summary>
public static class ClueCardRenderer
{
    // ────────────────────────────────────────────────────────────────────
    //  字体（由 ClueCardConfig 注入）
    // ────────────────────────────────────────────────────────────────────
    static TMP_FontAsset _font;
    public static void SetFont(TMP_FontAsset font) => _font = font;
    /// <summary>返回当前注入的中文字体（由 ClueCardConfig.Awake 注入）</summary>
    public static TMP_FontAsset GetFont() => _font;

    // ────────────────────────────────────────────────────────────────────
    //  颜色常量
    // ────────────────────────────────────────────────────────────────────
    static readonly Color32 C_PaperWhite  = new Color32(253, 250, 243, 255);
    static readonly Color32 C_PaperCream  = new Color32(255, 254, 248, 255);
    static readonly Color32 C_LineBlue    = new Color32(150, 180, 230,  40);
    static readonly Color32 C_HeaderBlue  = new Color32( 26,  60, 107, 255);
    static readonly Color32 C_FooterGray  = new Color32(235, 232, 225, 255);
    static readonly Color32 C_TextDark    = new Color32( 38,  38,  38, 255);
    static readonly Color32 C_TextMid     = new Color32( 90,  90,  90, 255);
    static readonly Color32 C_TextLight   = new Color32(200, 200, 200, 255);
    static readonly Color32 C_PhotoBg     = new Color32(100, 120, 160, 255);
    static readonly Color32 C_IDCardBg    = new Color32(214, 234, 248, 255);
    static readonly Color32 C_IDRedBar    = new Color32(192,  56,  44, 255);
    static readonly Color32 C_IDGold      = new Color32(255, 215,  80, 180);
    static readonly Color32 C_ReceiptBg   = new Color32(255, 254, 248, 255);
    static readonly Color32 C_ReceiptLine = new Color32(150, 140, 120, 100);
    static readonly Color32 C_DeviceBg   = new Color32( 24,  24,  24, 255);
    static readonly Color32 C_ScreenBg   = new Color32( 18,  52,  80, 255);
    static readonly Color32 C_ScreenApp  = new Color32( 26,  74, 112, 255);
    static readonly Color32 C_BubbleLeft  = new Color32(255, 255, 255,  45);
    static readonly Color32 C_BubbleRight = new Color32( 52, 144, 220, 130);

    // ════════════════════════════════════════════════════════════════════
    //  公共 API
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 在 parent 下构建缩略图，返回根 GameObject 及其尺寸。
    /// </summary>
    public static (GameObject go, Vector2 size) BuildThumbnail(ClueData clue, Transform parent)
    {
        switch (clue.clueType)
        {
            case ClueType.Text:       return BuildTranscriptThumb(clue, parent);
            case ClueType.Photo:      return BuildPhotoThumb(clue, parent);
            case ClueType.IDCard:     return BuildIDCardThumb(clue, parent);
            case ClueType.Receipt:    return BuildReceiptThumb(clue, parent);
            case ClueType.Screenshot: return BuildScreenshotThumb(clue, parent);
            default:                  return BuildTranscriptThumb(clue, parent);
        }
    }

    /// <summary>
    /// 在 parent 下构建全屏详情卡片，返回根 GameObject。
    /// onTextSelected(selectedText, clueId) ── 有选区时回调（可为 null）
    /// onFaceClicked(characterId, photo)    ── 点击人脸时回调（可为 null）
    /// </summary>
    public static GameObject BuildFullCard(
        ClueData clue,
        Transform parent,
        Action<string, string> onTextSelected = null,
        Action<string, Sprite> onFaceClicked  = null)
    {
        switch (clue.clueType)
        {
            case ClueType.Text:       return BuildTranscriptFull(clue, parent, onTextSelected);
            case ClueType.Photo:      return BuildPhotoFull(clue, parent, onFaceClicked);
            case ClueType.IDCard:     return BuildIDCardFull(clue, parent, onTextSelected);
            case ClueType.Receipt:    return BuildReceiptFull(clue, parent, onTextSelected);
            case ClueType.Screenshot: return BuildScreenshotFull(clue, parent, onTextSelected);
            default:                  return BuildTranscriptFull(clue, parent, onTextSelected);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  缩略图  ── 各类型
    // ════════════════════════════════════════════════════════════════════

    // ── 文字笔录 A4 纸风格 ──────────────────────────────────────────────
    static (GameObject, Vector2) BuildTranscriptThumb(ClueData clue, Transform parent)
    {
        const float W = 120, H = 168;
        var root = MakeRect(parent, "Transcript_Thumb", W, H, C_PaperWhite);

        // 横线
        for (int i = 0; i < 7; i++)
            MakeBg(MakeRect(root, $"rl{i}", W - 20, 1, Color.clear), C_LineBlue).gameObject
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40 - i * 17);

        // 蓝色页眉
        var hdr = MakeRect(root, "Header", W, 22, C_HeaderBlue);
        Pos(hdr, 0, H / 2 - 11);
        MakeTMP(hdr, !string.IsNullOrEmpty(clue.clueTitle) ? Truncate(clue.clueTitle, 10) : "审讯笔录",
            7f, C_TextLight, TextAlignmentOptions.Center, W - 8, 18);

        // 页脚
        var ftr = MakeRect(root, "Footer", W, 14, C_FooterGray);
        Pos(ftr, 0, -(H / 2 - 7));
        if (!string.IsNullOrEmpty(clue.clueDate))
            MakeTMP(ftr, clue.clueDate, 5.5f, C_TextMid, TextAlignmentOptions.Right, W - 12, 12);

        AddBoxShadow(root, W, H);
        return (root, new Vector2(W, H));
    }

    // ── 照片纸  宝丽来风格 ─────────────────────────────────────────────
    static (GameObject, Vector2) BuildPhotoThumb(ClueData clue, Transform parent)
    {
        const float W = 140, H = 162;
        var root = MakeRect(parent, "Photo_Thumb", W, H, Color.white);
        // 白色纸框（左右 6px，上 6px，下 22px ≈ 宝丽来）
        const float imgW = 128, imgH = 118;
        var img = MakeRect(root, "Photo", imgW, imgH, C_PhotoBg);
        Pos(img, 0, (H / 2 - 6 - imgH / 2));
        if      (clue.thumbnail  != null) SetSprite(img, clue.thumbnail);
        else if (clue.fullImage  != null) SetSprite(img, clue.fullImage);

        // 日期标注
        if (!string.IsNullOrEmpty(clue.clueDate))
            MakeTMP(root, clue.clueDate, 6f, C_TextMid, TextAlignmentOptions.Center, 120, 14)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(H / 2 - 13));

        // 光泽斜条
        var sheen = MakeRect(root, "Sheen", 30, imgH, new Color(1, 1, 1, 0.07f));
        Pos(sheen, -imgW / 2 + 20, H / 2 - 6 - imgH / 2);
        sheen.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 15);

        AddBoxShadow(root, W, H);
        return (root, new Vector2(W, H));
    }

    // ── 身份证卡片 ─────────────────────────────────────────────────────
    static (GameObject, Vector2) BuildIDCardThumb(ClueData clue, Transform parent)
    {
        // 85.6:54 = 1.586 → 168×106
        const float W = 168, H = 106;
        var root = MakeRect(parent, "IDCard_Thumb", W, H, C_IDCardBg);

        // 红色竖栏（22px）
        var red = MakeRect(root, "RedBar", 22, H, C_IDRedBar);
        Pos(red, -(W / 2 - 11), 0);
        MakeTMP(red, "居民\n身份证", 5.5f, Color.white, TextAlignmentOptions.Center, 18, 54);

        // 内容区域
        float cx = -(W / 2 - 22) + 4;
        MakeTMP(root, !string.IsNullOrEmpty(clue.clueTitle) ? $"姓名  {clue.clueTitle}" : "姓名",
            6f, C_TextDark, TextAlignmentOptions.Left, 90, 14)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(cx + 48, 28);
        if (!string.IsNullOrEmpty(clue.clueDate))
            MakeTMP(root, $"出生  {clue.clueDate}", 5f, C_TextMid, TextAlignmentOptions.Left, 90, 12)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(cx + 48, 12);
        if (!string.IsNullOrEmpty(clue.clueSubtitle))
            MakeTMP(root, clue.clueSubtitle, 4.5f, C_TextMid, TextAlignmentOptions.Left, 100, 10)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(cx + 48, -2);

        // 照片格
        var photoSlot = MakeRect(root, "PhotoSlot", 30, 42, new Color(0.7f, 0.8f, 0.87f, 1f));
        Pos(photoSlot, W / 2 - 22, 8);
        if (clue.fullImage != null) SetSprite(photoSlot, clue.fullImage);

        // 编号条
        var numBar = MakeRect(root, "NumBar", W - 22, 14, new Color(0.78f, 0.86f, 0.92f, 0.5f));
        Pos(numBar, 11, -(H / 2 - 7));

        AddBoxShadow(root, W, H);
        return (root, new Vector2(W, H));
    }

    // ── 热敏小票 ──────────────────────────────────────────────────────
    static (GameObject, Vector2) BuildReceiptThumb(ClueData clue, Transform parent)
    {
        const float W = 88, H = 188;
        var root = MakeRect(parent, "Receipt_Thumb", W, H, C_ReceiptBg);

        // 店名
        var hdr = MakeRect(root, "Store", W - 8, 20, new Color(0.96f, 0.95f, 0.90f, 1f));
        Pos(hdr, 0, H / 2 - 16);
        MakeTMP(hdr, !string.IsNullOrEmpty(clue.receiptStoreName) ? Truncate(clue.receiptStoreName, 8) : "收据",
            7f, C_TextDark, TextAlignmentOptions.Center, W - 10, 18);

        // 分隔线
        MakeBg(MakeRect(root, "div0", W - 10, 1, Color.clear), C_ReceiptLine)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 32);

        // 条目列表（最多 5 行）
        if (clue.receiptItems != null)
        {
            int maxRows = Mathf.Min(clue.receiptItems.Length, 5);
            for (int i = 0; i < maxRows; i++)
            {
                float y = H / 2 - 42 - i * 14;
                var item = clue.receiptItems[i];
                MakeTMP(root, Truncate(item.itemName, 7), 5f, C_TextDark, TextAlignmentOptions.Left, 54, 12)
                    .GetComponent<RectTransform>().anchoredPosition = new Vector2(-18, y);
                MakeTMP(root, $"¥{item.Subtotal:F0}", 5f, C_TextDark, TextAlignmentOptions.Right, 30, 12)
                    .GetComponent<RectTransform>().anchoredPosition = new Vector2(28, y);
            }
        }

        // 合计
        MakeBg(MakeRect(root, "divT", W - 10, 1, Color.clear), C_ReceiptLine)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(H / 2 - 26));
        MakeTMP(root, string.IsNullOrEmpty(clue.receiptTotal) ? "合计" : $"合计 ¥{clue.receiptTotal}",
            6f, C_TextDark, TextAlignmentOptions.Right, W - 10, 14)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(H / 2 - 16));

        // 锯齿边（顶/底各一条浅色条）
        MakeBg(MakeRect(root, "edgeT", W, 4, Color.clear), new Color32(230, 228, 215, 255))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 2);
        MakeBg(MakeRect(root, "edgeB", W, 4, Color.clear), new Color32(230, 228, 215, 255))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(H / 2 - 2));

        AddBoxShadow(root, W, H);
        return (root, new Vector2(W, H));
    }

    // ── 截图 / 手机屏幕框 ─────────────────────────────────────────────
    static (GameObject, Vector2) BuildScreenshotThumb(ClueData clue, Transform parent)
    {
        const float W = 104, H = 186;
        var root = MakeRect(parent, "Screenshot_Thumb", W, H, C_DeviceBg);

        // 挖孔刘海
        var notch = MakeRect(root, "Notch", 26, 6, new Color(0.06f, 0.06f, 0.06f, 1f));
        Pos(notch, 0, H / 2 - 5);

        // 屏幕
        var screen = MakeRect(root, "Screen", 94, 166, C_ScreenBg);
        Pos(screen, 0, -4);

        // 状态栏
        var stat = MakeRect(screen, "StatusBar", 94, 12, new Color(0, 0, 0, 0.3f));
        Pos(stat, 0, 77);
        MakeTMP(stat, "12:00", 4.5f, new Color32(210, 210, 210, 255), TextAlignmentOptions.Left, 36, 10)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(-40, 0);

        // App 标题栏
        var appHdr = MakeRect(screen, "AppHdr", 94, 20, C_ScreenApp);
        Pos(appHdr, 0, 63);
        string appTitle = !string.IsNullOrEmpty(clue.screenshotPlatform) ? clue.screenshotPlatform : "消息";
        MakeTMP(appHdr, $"< {appTitle}", 6f, Color.white, TextAlignmentOptions.Center, 80, 16);

        // 聊天气泡（装饰用）
        MakeBg(MakeRect(screen, "b1", 55, 14, Color.clear), C_BubbleLeft)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(-16, 36);
        MakeBg(MakeRect(screen, "b2", 44, 14, Color.clear), C_BubbleRight)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 16);
        MakeBg(MakeRect(screen, "b3", 50, 14, Color.clear), C_BubbleLeft)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(-18, -4);
        MakeBg(MakeRect(screen, "b4", 38, 14, Color.clear), C_BubbleRight)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(22, -24);

        // 底部 Home 条
        var home = MakeRect(root, "HomeBar", 32, 4, new Color(0.5f, 0.5f, 0.5f, 0.8f));
        Pos(home, 0, -(H / 2 - 5));

        AddBoxShadow(root, W, H);
        return (root, new Vector2(W, H));
    }

    // ════════════════════════════════════════════════════════════════════
    //  全屏详情  ── 各类型
    // ════════════════════════════════════════════════════════════════════

    // ── 文字笔录  A4 纸 ───────────────────────────────────────────────
    static GameObject BuildTranscriptFull(ClueData clue, Transform parent,
        Action<string, string> onTextSelected)
    {
        const float W = 520, H = 740;
        var root = MakeRect(parent, "Transcript_Full", W, H, C_PaperWhite);

        // ── 页眉 ──
        var hdr = MakeRect(root, "Header", W, 64, C_HeaderBlue);
        Pos(hdr, 0, H / 2 - 32);
        MakeTMP(hdr, !string.IsNullOrEmpty(clue.clueSubtitle) ? clue.clueSubtitle : "案件档案",
            9f, new Color32(180, 200, 230, 255), TextAlignmentOptions.Center, W - 20, 20)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 14);
        MakeTMP(hdr, !string.IsNullOrEmpty(clue.clueTitle) ? clue.clueTitle : "审讯笔录",
            16f, Color.white, TextAlignmentOptions.Center, W - 20, 28)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

        // ── 元信息行 ──
        var meta = MakeRect(root, "Meta", W, 26, C_FooterGray);
        Pos(meta, 0, H / 2 - 64 - 13);
        string metaStr = Join("  ", clue.clueDate, clue.clueLocation);
        MakeTMP(meta, metaStr, 8f, C_TextMid, TextAlignmentOptions.Center, W - 30, 20);

        // ── 横线（A4 纸感） ──
        for (int i = 0; i < 22; i++)
        {
            var l = MakeRect(root, $"rl{i}", W - 50, 1, C_LineBlue);
            Pos(l, 5, H / 2 - 118 - i * 26);
        }

        // ── 操作提示（告知玩家可以拖选文字）──
        if (onTextSelected != null)
        {
            var hintRt = MakeRect(root, "SelectHint", W - 40, 20, Color.clear);
            Pos(hintRt, 0, H / 2 - 64 - 26 - 14);   // meta 行下方
            MakeTMP(hintRt, "[ 拖拽鼠标选中文字，可标记到笔记 ]",
                7f, new Color32(120, 130, 160, 180), TextAlignmentOptions.Center, W - 40, 18);
        }

        // ── 正文（可选中 TMP_InputField） ──
        var ifGo = BuildReadonlyInputField(root,
            !string.IsNullOrEmpty(clue.textContent) ? clue.textContent : "(暂无内容)",
            11f, C_TextDark, W - 60, H - 170);
        Pos(ifGo.GetComponent<RectTransform>(), 5, -(64 + 26) / 2 + 10);

        if (onTextSelected != null)
        {
            var watcher = ifGo.AddComponent<TextSelectionWatcher>();
            watcher.Init(ifGo.GetComponent<TMP_InputField>(), clue.clueId, onTextSelected);
        }

        // ── 页脚 ──
        var ftr = MakeRect(root, "Footer", W, 42, C_FooterGray);
        Pos(ftr, 0, -(H / 2 - 21));
        MakeTMP(ftr, $"档案编号：{clue.clueId}    本文件属于保密资料，请勿外传",
            7.5f, new Color32(140, 130, 110, 255), TextAlignmentOptions.Center, W - 30, 32);

        AddBoxShadow(root, W, H);
        return root;
    }

    // ── 照片纸（宝丽来 + 人脸热区）─────────────────────────────────────
    static GameObject BuildPhotoFull(ClueData clue, Transform parent,
        Action<string, Sprite> onFaceClicked)
    {
        const float W = 500, H = 560;
        const float imgW = 476, imgH = 446;
        var root = MakeRect(parent, "Photo_Full", W, H, Color.white);

        // ── 照片区 ──
        var img = MakeRect(root, "PhotoArea", imgW, imgH, C_PhotoBg);
        Pos(img, 0, H / 2 - 12 - imgH / 2);
        if      (clue.fullImage  != null) SetSprite(img, clue.fullImage);
        else if (clue.thumbnail  != null) SetSprite(img, clue.thumbnail);

        // ── 人脸热区按钮 ──
        if (clue.faceHotspots != null && onFaceClicked != null)
        {
            foreach (var hs in clue.faceHotspots)
            {
                // normalizedRect 宽/高为零时（未精确配置），回退为整张照片均可点击
                bool fullArea = hs.normalizedRect.width <= 0f || hs.normalizedRect.height <= 0f;

                float bx, by, bw, bh;
                if (fullArea)
                {
                    bx = 0f; by = 0f; bw = imgW; bh = imgH;
                }
                else
                {
                    bx = (hs.normalizedRect.x + hs.normalizedRect.width  * 0.5f - 0.5f) * imgW;
                    by = (hs.normalizedRect.y + hs.normalizedRect.height * 0.5f - 0.5f) * imgH;
                    bw = hs.normalizedRect.width  * imgW;
                    bh = hs.normalizedRect.height * imgH;
                }

                var hsRect = MakeRect(img, $"Face_{hs.characterId}", bw, bh, Color.clear);
                Pos(hsRect, bx, by);
                // 整图可点击时不显示金色边框（避免遮挡）；精确热区才显示定位框
                if (!fullArea)
                    MakeBg(MakeRect(hsRect, "border", bw - 2, bh - 2, new Color(1f, 0.85f, 0f, 0.22f)));
                // 点击按钮（characterPhoto 未配置时兜底使用整张照片）
                var btn = hsRect.GetComponent<Button>() ?? hsRect.AddComponent<Button>();
                btn.targetGraphic = hsRect.GetComponent<Image>() ?? hsRect.AddComponent<Image>();
                ((Image)btn.targetGraphic).color = Color.clear;
                ((Image)btn.targetGraphic).raycastTarget = true;
                var capturedHs       = hs;
                var capturedFallback = clue.fullImage;
                btn.onClick.AddListener(() =>
                {
                    var photo = capturedHs.characterPhoto != null
                              ? capturedHs.characterPhoto
                              : capturedFallback;
                    onFaceClicked?.Invoke(capturedHs.characterId, photo);
                });
            }
        }

        // ── 白色底边（宝丽来加厚下边框） ──
        var caption = MakeRect(root, "Caption", W, 94, Color.white);
        Pos(caption, 0, -(H / 2 - 47));
        if (!string.IsNullOrEmpty(clue.clueTitle))
            MakeTMP(caption, clue.clueTitle, 12f, C_TextDark, TextAlignmentOptions.Center, W - 40, 28)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 18);
        string capMeta = Join("  ", clue.clueDate, clue.clueLocation);
        if (!string.IsNullOrEmpty(capMeta))
            MakeTMP(caption, capMeta, 8f, C_TextMid, TextAlignmentOptions.Center, W - 40, 20)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -6);

        AddBoxShadow(root, W, H);
        return root;
    }

    // ── 身份证（1:1 还原） ───────────────────────────────────────────
    static GameObject BuildIDCardFull(ClueData clue, Transform parent,
        Action<string, string> onTextSelected)
    {
        // 86:54 缩放到 520×328
        const float W = 520, H = 328;
        var root = MakeRect(parent, "IDCard_Full", W, H, C_IDCardBg);

        // 安全底纹（极淡蓝圆点阵）
        for (int i = 0; i < 6; i++)
            for (int j = 0; j < 10; j++)
            {
                var dot = MakeRect(root, $"dot{i}{j}", 3, 3, new Color(0.4f, 0.6f, 0.8f, 0.06f));
                Pos(dot, -W / 2 + 40 + j * 48, H / 2 - 30 - i * 52);
            }

        // 红色竖栏（宽 67px）
        var redBar = MakeRect(root, "RedBar", 67, H, C_IDRedBar);
        Pos(redBar, -(W / 2 - 33.5f), 0);
        MakeTMP(redBar, "中华人民共和国", 8f, Color.white, TextAlignmentOptions.Center, 52, 80)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 58);
        MakeTMP(redBar, "居民身份证", 9f, Color.white, TextAlignmentOptions.Center, 52, 56)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -14);
        // 国徽（用金色星形模拟）
        MakeTMP(redBar, "✦", 26f, C_IDGold, TextAlignmentOptions.Center, 52, 38)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);

        // 内容区域起始 x（红栏右边 + 10）
        float fx = -W / 2 + 67 + 12;

        // 各字段（Label + Value）
        string[,] fields =
        {
            {"姓    名", !string.IsNullOrEmpty(clue.clueTitle)    ? clue.clueTitle    : ""},
            {"性    别", "男"},
            {"民    族", "汉"},
            {"出生日期", !string.IsNullOrEmpty(clue.clueDate)     ? clue.clueDate     : ""},
            {"住        址", !string.IsNullOrEmpty(clue.clueLocation) ? clue.clueLocation : ""},
        };
        float startY = 118;
        for (int i = 0; i < fields.GetLength(0); i++)
        {
            float y = startY - i * 42;
            MakeTMP(root, fields[i, 0], 10f, C_TextDark, TextAlignmentOptions.Left, 78, 22)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(fx, y);
            MakeTMP(root, fields[i, 1], 10f, C_TextDark, TextAlignmentOptions.Left, 220, 22)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(fx + 82, y);
        }

        // 照片槽（右上区域）
        var ps = MakeRect(root, "PhotoSlot", 96, 130, new Color(0.62f, 0.73f, 0.83f, 1f));
        Pos(ps, W / 2 - 67, 60);
        if (clue.fullImage != null)
        {
            var psImg = ps.GetComponent<Image>() ?? ps.AddComponent<Image>();
            psImg.sprite = clue.fullImage;
            psImg.type = Image.Type.Simple;
            psImg.preserveAspect = false;
        }

        // 编号行（底部横跨）
        var numBar = MakeRect(root, "IDNumBar", W - 67, 38, new Color(0.74f, 0.85f, 0.92f, 0.5f));
        Pos(numBar, 33.5f, -(H / 2 - 19));
        MakeTMP(numBar, "公民身份号码", 8.5f, C_TextDark, TextAlignmentOptions.Left, 80, 26)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(fx, 0);
        MakeTMP(numBar, !string.IsNullOrEmpty(clue.clueSubtitle) ? clue.clueSubtitle : "000000000000000000",
            11f, C_TextDark, TextAlignmentOptions.Right, 260, 26)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(W / 2 - 70, 0);

        // 隐形 InputField 提供文字选取能力
        if (onTextSelected != null)
        {
            string text = $"姓名：{clue.clueTitle}\n出生：{clue.clueDate}\n" +
                          $"住址：{clue.clueLocation}\n身份证号：{clue.clueSubtitle}";
            var ifGo = BuildReadonlyInputField(root, text, 1f, Color.clear, 1, 1);
            ifGo.GetComponent<RectTransform>().sizeDelta = Vector2.one;
            var watcher = ifGo.AddComponent<TextSelectionWatcher>();
            watcher.Init(ifGo.GetComponent<TMP_InputField>(), clue.clueId, onTextSelected);
        }

        AddBoxShadow(root, W, H);
        return root;
    }

    // ── 票据（热敏小票） ──────────────────────────────────────────────
    static GameObject BuildReceiptFull(ClueData clue, Transform parent,
        Action<string, string> onTextSelected)
    {
        int itemCount = clue.receiptItems != null ? clue.receiptItems.Length : 0;
        float H = Mathf.Max(440, 130 + itemCount * 30 + 130);
        const float W = 286;
        var root = MakeRect(parent, "Receipt_Full", W, H, C_ReceiptBg);

        // 锯齿顶边
        MakeBg(MakeRect(root, "edgeT", W, 6, Color.clear), new Color32(228, 225, 208, 255))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 3);

        // 店名
        MakeTMP(root, !string.IsNullOrEmpty(clue.receiptStoreName) ? clue.receiptStoreName : "收据",
            14f, C_TextDark, TextAlignmentOptions.Center, W - 20, 32)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 30);
        MakeTMP(root, !string.IsNullOrEmpty(clue.receiptStoreAddress) ? clue.receiptStoreAddress : "",
            8f, C_TextMid, TextAlignmentOptions.Center, W - 20, 18)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 52);
        MakeTMP(root, !string.IsNullOrEmpty(clue.clueDate) ? clue.clueDate : "",
            8f, C_TextMid, TextAlignmentOptions.Center, W - 20, 16)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 68);

        // 分隔线
        MakeBg(MakeRect(root, "div0", W - 20, 1, Color.clear), C_ReceiptLine)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 84);

        // 表头
        float rowY = H / 2 - 100;
        MakeTMP(root, "商品", 8.5f, C_TextMid, TextAlignmentOptions.Left, 120, 18)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, rowY);
        MakeTMP(root, "数量", 8.5f, C_TextMid, TextAlignmentOptions.Center, 40, 18)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(20, rowY);
        MakeTMP(root, "金额", 8.5f, C_TextMid, TextAlignmentOptions.Right, 60, 18)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(95, rowY);
        MakeBg(MakeRect(root, "div1", W - 20, 1, Color.clear), new Color32(180, 170, 150, 80))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, H / 2 - 112);

        // 条目
        if (clue.receiptItems != null)
        {
            for (int i = 0; i < clue.receiptItems.Length; i++)
            {
                var it = clue.receiptItems[i];
                float y = H / 2 - 128 - i * 30;
                MakeTMP(root, it.itemName, 9.5f, C_TextDark, TextAlignmentOptions.Left, 120, 22)
                    .GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, y);
                MakeTMP(root, $"×{it.quantity}", 9.5f, C_TextMid, TextAlignmentOptions.Center, 40, 22)
                    .GetComponent<RectTransform>().anchoredPosition = new Vector2(20, y);
                MakeTMP(root, $"¥{it.Subtotal:F2}", 9.5f, C_TextDark, TextAlignmentOptions.Right, 60, 22)
                    .GetComponent<RectTransform>().anchoredPosition = new Vector2(95, y);
                // 浅色隔行
                if (i % 2 == 0)
                    MakeBg(MakeRect(root, $"row{i}", W, 28, Color.clear), new Color(0, 0, 0, 0.02f))
                        .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, y);
            }
        }

        float botStart = H / 2 - 128 - itemCount * 30;

        // 合计行
        MakeBg(MakeRect(root, "divT", W - 20, 1, Color.clear), C_ReceiptLine)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, botStart - 8);
        MakeTMP(root, "合    计", 11f, C_TextDark, TextAlignmentOptions.Left, 100, 26)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(-70, botStart - 28);
        MakeTMP(root, $"¥{(string.IsNullOrEmpty(clue.receiptTotal) ? "0.00" : clue.receiptTotal)}",
            12f, C_TextDark, TextAlignmentOptions.Right, 90, 26)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(90, botStart - 28);

        // 收银员 / 感谢语
        if (!string.IsNullOrEmpty(clue.receiptCashier))
            MakeTMP(root, $"收银员：{clue.receiptCashier}", 8f, C_TextMid, TextAlignmentOptions.Center, W - 20, 18)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, botStart - 58);
        MakeBg(MakeRect(root, "divB", W - 20, 1, Color.clear), C_ReceiptLine)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, botStart - 72);
        MakeTMP(root, "*  感谢惠顾，欢迎再来  *", 8f, C_TextMid, TextAlignmentOptions.Center, W - 20, 18)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, botStart - 86);

        // 锯齿底边
        MakeBg(MakeRect(root, "edgeB", W, 6, Color.clear), new Color32(228, 225, 208, 255))
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(H / 2 - 3));

        // 可选中文本
        if (onTextSelected != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(clue.receiptStoreName); sb.AppendLine(clue.clueDate);
            if (clue.receiptItems != null)
                foreach (var it in clue.receiptItems)
                    sb.AppendLine($"{it.itemName}  ×{it.quantity}  ¥{it.Subtotal:F2}");
            sb.AppendLine($"合计：¥{clue.receiptTotal}");
            var ifGo = BuildReadonlyInputField(root, sb.ToString(), 1f, Color.clear, 1, 1);
            ifGo.GetComponent<RectTransform>().sizeDelta = Vector2.one;
            var watcher = ifGo.AddComponent<TextSelectionWatcher>();
            watcher.Init(ifGo.GetComponent<TMP_InputField>(), clue.clueId, onTextSelected);
        }

        AddBoxShadow(root, W, H);
        return root;
    }

    // ── 截图 / 手机框 ─────────────────────────────────────────────────
    static GameObject BuildScreenshotFull(ClueData clue, Transform parent,
        Action<string, string> onTextSelected)
    {
        const float W = 340, H = 600;
        const float SW = 314, SH = 552;     // 屏幕尺寸
        var root = MakeRect(parent, "Screenshot_Full", W, H, C_DeviceBg);

        // 刘海
        var notch = MakeRect(root, "Notch", 68, 18, new Color(0.06f, 0.06f, 0.06f, 1f));
        Pos(notch, 0, H / 2 - 10);

        // 屏幕
        var screen = MakeRect(root, "Screen", SW, SH, C_ScreenBg);
        Pos(screen, 0, -8);

        // 状态栏
        var stat = MakeRect(screen, "Stat", SW, 22, new Color(0f, 0f, 0f, 0.4f));
        Pos(stat, 0, SH / 2 - 11);
        MakeTMP(stat, "12:00", 8.5f, new Color32(220, 220, 220, 255), TextAlignmentOptions.Left, 60, 18)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(-SW / 2 + 12, 0);
        MakeTMP(stat, "▐▐  WiFi  100%", 7f, new Color32(210, 210, 210, 255), TextAlignmentOptions.Right, 100, 18)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(SW / 2 - 8, 0);

        // App 标题栏
        var appHdr = MakeRect(screen, "AppHdr", SW, 38, C_ScreenApp);
        Pos(appHdr, 0, SH / 2 - 22 - 19);
        string title = !string.IsNullOrEmpty(clue.screenshotPlatform) ? clue.screenshotPlatform : "聊天";
        MakeTMP(appHdr, $"<  {title}", 12f, Color.white, TextAlignmentOptions.Center, SW - 30, 30);

        // 内容区（可选中 InputField）
        float contentH = SH - 22 - 38 - 10;
        if (!string.IsNullOrEmpty(clue.textContent))
        {
            var ifGo = BuildReadonlyInputField(screen,
                clue.textContent, 10f, new Color32(220, 225, 235, 255), SW - 16, contentH);
            Pos(ifGo.GetComponent<RectTransform>(), 0, -(22 + 38) / 2 + 10);

            if (onTextSelected != null)
            {
                var watcher = ifGo.AddComponent<TextSelectionWatcher>();
                watcher.Init(ifGo.GetComponent<TMP_InputField>(), clue.clueId, onTextSelected);
            }
        }

        // Home 条
        var home = MakeRect(root, "Home", 80, 5, new Color(0.5f, 0.5f, 0.5f, 0.8f));
        Pos(home, 0, -(H / 2 - 8));

        AddBoxShadow(root, W, H);
        return root;
    }

    // ════════════════════════════════════════════════════════════════════
    //  只读 TMP_InputField（用于文字选中）
    // ════════════════════════════════════════════════════════════════════
    static GameObject BuildReadonlyInputField(GameObject parent,
        string text, float fontSize, Color32 textColor, float w, float h)
    {
        return BuildReadonlyInputField(parent.transform, text, fontSize, textColor, w, h);
    }

    static GameObject BuildReadonlyInputField(Transform parent,
        string text, float fontSize, Color32 textColor, float w, float h)
    {
        // ── Root ──────────────────────────────────────────────────────
        var rootGo = new GameObject("ClueInputField");
        rootGo.transform.SetParent(parent, false);
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(w, h);

        var rootImg = rootGo.AddComponent<Image>();
        rootImg.color = Color.clear;
        // InputField 的根 Image 不需要拦截点击（TMP_InputField 自身以及 textComponent 会处理）
        rootImg.raycastTarget = false;

        var inputField = rootGo.AddComponent<TMP_InputField>();

        // ── Viewport / Text Area ──────────────────────────────────────
        var vpGo = new GameObject("Text Area");
        vpGo.transform.SetParent(rootGo.transform, false);
        var vpRt = vpGo.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero;
        vpRt.offsetMin = new Vector2(6, 4);
        vpRt.offsetMax = new Vector2(-6, -4);
        vpGo.AddComponent<Image>().color = Color.clear;
        vpGo.AddComponent<RectMask2D>();

        // ── Text ──────────────────────────────────────────────────────
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(vpGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.lineSpacing = 2;
        if (_font != null) tmp.font = _font;

        // ── Placeholder ───────────────────────────────────────────────
        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(vpGo.transform, false);
        var phRt = phGo.AddComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.sizeDelta = Vector2.zero;
        var phTmp = phGo.AddComponent<TextMeshProUGUI>();
        phTmp.text = "";
        phTmp.color = Color.clear;
        if (_font != null) phTmp.font = _font;

        // ── Wire up ───────────────────────────────────────────────────
        inputField.textViewport   = vpRt;
        inputField.textComponent  = tmp;
        inputField.placeholder    = phTmp;
        inputField.text           = text;
        inputField.readOnly       = true;
        inputField.richText       = false;
        inputField.lineType       = TMP_InputField.LineType.MultiLineNewline;
        // multiLine 是只读属性，由 lineType = MultiLineNewline 自动设置

        // 选中高亮色：蓝色半透明，让玩家清楚看到已选区域
        inputField.selectionColor = new Color32(52, 144, 220, 120);

        // caretWidth 必须 ≥ 1，否则部分 TMP 版本在 readonly 模式下
        // 不会正确进入选择状态，导致 selectionAnchorPosition 始终 == selectionFocusPosition
        inputField.caretWidth     = 1;
        inputField.caretColor     = new Color(0, 0, 0, 0);   // 光标透明，不干扰阅读

        // 确保文字组件可以接收射线（鼠标点击/拖拽）
        tmp.raycastTarget = true;

        return rootGo;
    }

    // ════════════════════════════════════════════════════════════════════
    //  低级 UI 工具方法
    // ════════════════════════════════════════════════════════════════════

    /// <summary>创建子 GameObject，附加 RectTransform（不附带 Image）。</summary>
    static GameObject MakeRect(Transform parent, string name, float w, float h, Color32 col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        if (col.a > 0)
        {
            var img = go.AddComponent<Image>();
            img.color = col;
            // 装饰性背景不拦截点击，让事件穿透到背后的 backdropBtn
            img.raycastTarget = false;
        }
        return go;
    }

    static GameObject MakeRect(GameObject parent, string name, float w, float h, Color32 col)
        => MakeRect(parent.transform, name, w, h, col);

    /// <summary>设置 GameObject（必须已有 RectTransform）的 anchoredPosition。</summary>
    static void Pos(GameObject go, float x, float y)
        => go.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);

    static void Pos(RectTransform rt, float x, float y)
        => rt.anchoredPosition = new Vector2(x, y);

    /// <summary>给已有 GameObject 添加背景 Image（若已有则直接返回）。</summary>
    static Image MakeBg(GameObject go, Color32 col)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;   // 装饰性，不拦截点击
        return img;
    }
    static Image MakeBg(GameObject go)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.raycastTarget = false;   // 装饰性，不拦截点击
        return img;
    }

    /// <summary>给 GameObject 添加 TextMeshProUGUI，并创建带尺寸的子节点存放文字。</summary>
    static GameObject MakeTMP(GameObject parent, string text, float fontSize, Color32 col,
        TextAlignmentOptions align, float w, float h)
    {
        var go = new GameObject("TMP_" + text.Substring(0, Mathf.Min(text.Length, 8)));
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = col;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        // 标签文字不需要接受点击，让事件穿透到 backdropBtn
        // 注意：BuildReadonlyInputField 里的 textComponent 不走这个方法，仍保持 raycastTarget=true
        tmp.raycastTarget = false;
        if (_font != null) tmp.font = _font;
        return go;
    }

    static void SetSprite(GameObject go, Sprite sprite)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
    }

    /// <summary>
    /// 用偏移稍大的半透明深色 Image 模拟投影阴影。
    /// 必须在父 parent 里插到最低 sibling，或直接用 sibling index 控制。
    /// </summary>
    static void AddBoxShadow(GameObject card, float w, float h)
    {
        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(card.transform.parent, false);
        shadow.transform.SetSiblingIndex(card.transform.GetSiblingIndex());
        var rt = shadow.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w + 6, h + 6);
        rt.anchoredPosition = card.GetComponent<RectTransform>().anchoredPosition + new Vector2(4, -4);
        var img = shadow.AddComponent<Image>();
        img.color = new Color32(0, 0, 0, 55);
        img.raycastTarget = false;   // 纯视觉阴影，不拦截点击
    }

    static string Truncate(string s, int maxLen)
        => s != null && s.Length > maxLen ? s.Substring(0, maxLen) + "…" : (s ?? "");

    static string Join(string sep, params string[] parts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var p in parts)
            if (!string.IsNullOrEmpty(p)) { if (sb.Length > 0) sb.Append(sep); sb.Append(p); }
        return sb.ToString();
    }
}
