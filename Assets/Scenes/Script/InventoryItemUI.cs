using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包物品 UI 组件，挂在每个物品模板上
/// </summary>
public class InventoryItemUI : MonoBehaviour
{
    [Header("UI 引用（在模板里手动拖拽赋值）")]
    public Image iconImage;
    public Text nameText;
    public Text countText;

    /// <summary>
    /// 根据数据刷新显示
    /// </summary>
    public void Refresh(InventoryItemData data)
    {
        if (data == null) return;

        if (iconImage != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.itemName;

        if (countText != null)
            countText.text = "x" + data.count;
    }
}
