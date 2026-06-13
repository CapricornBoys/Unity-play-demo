using UnityEngine;

namespace GameUI.Inventory
{
    /// <summary>
    /// 背包物品数据。示例中颜色代替真实图标，正式项目可改为 Addressable Sprite 地址。
    /// </summary>
    public sealed class InventoryItem
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Count { get; }
        public Color Color { get; }

        public InventoryItem(
            int id,
            string name,
            string description,
            int count,
            Color color)
        {
            Id = id;
            Name = name;
            Description = description;
            Count = count;
            Color = color;
        }
    }
}
