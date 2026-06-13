using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包物品数据
/// </summary>
[System.Serializable]
public class InventoryItemData
{
    public string itemName;
    public int count;
    public Sprite icon;

    public InventoryItemData(string name, int count, Sprite icon = null)
    {
        this.itemName = name;
        this.count = count;
        this.icon = icon;
    }
}
