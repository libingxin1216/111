using UnityEngine;

[CreateAssetMenu(fileName = "StageConfig", menuName = "Game/StageConfig")]
public class StageConfig : ScriptableObject
{
    [Header("各阶段门槛（从小到大填写）")]
    [Tooltip("示例：{2,3,4} 表示锁定2人触发阶段一，3人触发阶段二，4人触发阶段三")]
    public int[] stageThresholds;
}