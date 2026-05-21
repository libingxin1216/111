using UnityEngine;

[CreateAssetMenu(menuName = "Game/ClueData")]
public class ClueData : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────
    // 原有字段（保持不变，向后兼容）
    // ─────────────────────────────────────────────────────────────────────
    [Header("基本信息")]
    public string clueId;
    public ClueType clueType;            // Text / Photo / IDCard / Receipt / Screenshot
    public Sprite thumbnail;             // 缩略图 Sprite（可选，有则优先显示）
    public Sprite fullImage;             // 放大后显示的完整图片
    public string textContent;           // 文字内容（Text/Screenshot 类型使用）
    public string prerequisiteClueId;   // 前置线索 ID，空 = 无前置
    public string characterIdToUnlockPhoto; // 打开此线索自动解锁对应 characterId 的照片

    // ─────────────────────────────────────────────────────────────────────
    // ★ 新增：展示标题与元数据
    // ─────────────────────────────────────────────────────────────────────
    [Header("展示信息（卡片标题 / 页眉 / 页脚）")]
    
    public string clueTitle;

   
    public string clueDate;

   
    public string clueLocation;
    public string clueSubtitle;

    // ─────────────────────────────────────────────────────────────────────
    // ★ 新增：票据（Receipt）专用字段
    // ─────────────────────────────────────────────────────────────────────
    [Header("票据数据（Receipt 类型填写）")]
    public string receiptStoreName;
    public string receiptStoreAddress;
    public ReceiptItemEntry[] receiptItems;
 
    public string receiptTotal;
    public string receiptCashier;

    // ─────────────────────────────────────────────────────────────────────
    // ★ 新增：照片（Photo）专用字段
    // ─────────────────────────────────────────────────────────────────────
    [Header("人脸热区（Photo 类型填写，用于点击人脸解锁照片）")]
    public FaceHotspot[] faceHotspots;

    // ─────────────────────────────────────────────────────────────────────
    // ★ 新增：截图（Screenshot）专用字段
    // ─────────────────────────────────────────────────────────────────────
    [Header("截图数据（Screenshot 类型填写）")]
  
    public string screenshotPlatform;

    [Tooltip("true = 手机竖屏框；false = 电脑横屏框")]
    public bool isPhoneScreenshot = true;
}
