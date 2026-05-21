/// <summary>
/// 票据线索中的单行商品/项目数据。
/// 在 ClueData Inspector 里逐条填写即可。
/// </summary>
[System.Serializable]
public class ReceiptItemEntry
{
    public string itemName  = "商品名称";
    public int    quantity  = 1;
    public float  unitPrice = 0f;

    /// <summary>单行小计 = quantity × unitPrice</summary>
    public float Subtotal => quantity * unitPrice;
}
