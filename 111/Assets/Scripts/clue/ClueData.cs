using UnityEngine;

[CreateAssetMenu(menuName = "Game/ClueData")]
public class ClueData : ScriptableObject
{
    public string clueId;
    public ClueType clueType;      // 枚举：Text/Photo/IDCard/Receipt/Screenshot
    public Sprite thumbnail;       // 缩略图
    public Sprite fullImage;       // 放大后显示的完整图
    public string textContent;     // 若是文字笔录，存正文文字
    public string prerequisiteClueId;   // 前置线索ID，空=无前置
    public string characterIdToUnlockPhoto; // 若含人脸，填对应characterId
}