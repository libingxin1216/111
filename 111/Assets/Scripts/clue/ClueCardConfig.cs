using UnityEngine;
using TMPro;

/// <summary>
/// 挂载到 ClueScene 的任意持久 GameObject（推荐挂到 CluePanelController 同一节点）。
/// 在 Inspector 里拖入中文 SDF 字体后，Awake 时自动注入 ClueCardRenderer。
///
/// ── Inspector 配置 ──────────────────────────────────────────────────────
/// • chineseFont      ← 拖入 Assets/Font/SIMHEI.asset 或 STXINWEI SDF.asset
/// • useCardRenderer  ← 勾选后 ClueThumbItem 启用程序化卡片渲染；
///                       取消勾选则退回原有 Sprite 显示逻辑（降级兼容）
/// ────────────────────────────────────────────────────────────────────────
/// </summary>
public class ClueCardConfig : MonoBehaviour
{
    public static ClueCardConfig Instance { get; private set; }

    [Header("字体")]
    [Tooltip("支持中文的 TMP SDF 字体资产，推荐 SIMHEI.asset")]
    public TMP_FontAsset chineseFont;

    [Header("功能开关")]
    [Tooltip("true = 使用程序化卡片渲染器；false = 沿用原有 Sprite 缩略图")]
    public bool useCardRenderer = true;

    void Awake()
    {
        Instance = this;
        if (chineseFont != null)
            ClueCardRenderer.SetFont(chineseFont);
    }
}
