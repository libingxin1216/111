using UnityEngine;

public class RelationshipDataHolder : MonoBehaviour
{
    public static RelationshipDataHolder Instance;

    [Header("连线数据")]
    public RelationshipData relationshipData;

    [Header("关卡阶段配置")]
    public StageConfig stageConfig;

    void Awake()
    {
        Instance = this;
    }
}