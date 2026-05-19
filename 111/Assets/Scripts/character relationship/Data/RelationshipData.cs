using UnityEngine;

[System.Serializable]
public class RelationshipLine
{
    public string fromId;
    public string toId;
    public Color lineColor = Color.gray;
}

[CreateAssetMenu(fileName = "RelationshipData", menuName = "Game/RelationshipData")]
public class RelationshipData : ScriptableObject
{
    public RelationshipLine[] lines;
}