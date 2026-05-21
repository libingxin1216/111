using UnityEngine;

/// <summary>
/// 照片线索中的人脸可点击热区。
/// normalizedRect 的坐标系：左下角为(0,0)，右上角为(1,1)，与 Unity Sprite 坐标一致。
/// 点击后会把 characterPhoto 填入人物关系图对应人物卡的照片槽，
/// 并触发 EventBus "OnPhotoUnlocked" 事件（参数为 characterId）。
/// </summary>
[System.Serializable]
public class FaceHotspot
{
    [Tooltip("在图片中的归一化矩形（0-1），左下原点。x/y 为左下角坐标，width/height 为尺寸。")]
    public Rect normalizedRect = new Rect(0.35f, 0.3f, 0.3f, 0.4f);

    [Tooltip("对应的人物 ID，与 CharacterData.characterId 一致")]
    public string characterId;

    [Tooltip("解锁后填入人物卡的照片 Sprite")]
    public Sprite characterPhoto;
}
