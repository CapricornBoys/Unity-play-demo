using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 背包演示脚本
/// 挂载到场景中的 ScrollView（带 LoopList 组件）上
/// </summary>
public class InventoryDemo : MonoBehaviour
{
    [Header("LoopList 组件（自动获取）")]
    public LoopList loopList;

    [Header("物品模板（Content 下的模板）")]
    public RectTransform itemTemplate;

    [Header("测试物品数量")]
    public int itemCount = 1000;

    [Header("跳转输入框（可选，拖入 InputField）")]
    public InputField scrollToInputField;

    [Header("跳转按钮（可选，拖入 Button）")]
    public Button scrollToButton;

    // 模拟背包数据
    private List<InventoryItemData> inventoryData = new();

    // 物品名称池（模拟数据用）
    private readonly string[] sampleNames = new string[]
    {
        "生命药水", "魔法药水", "铁剑", "木盾",
        "火球卷轴", "治疗术卷轴", "金币袋", "宝石",
        "皮革靴", "铁盔", "魔法杖", "炸弹",
        "钥匙", "地图", "火把", "弓箭",
        "护身符", "戒指", "披风", "法书"
    };

    void Start()
    {
        // 自动获取 LoopList
        if (loopList == null)
            loopList = GetComponent<LoopList>();

        if (loopList == null)
        {
            Debug.LogError("找不到 LoopList 组件，请挂载到带有 LoopList 的 GameObject 上！");
            return;
        }

        // 生成模拟数据
        GenerateTestData();

        // 设置循环列表
        loopList.itemTemplate = itemTemplate;
        loopList.content = loopList.GetComponent<ScrollRect>().content;
        loopList.itemSpacing = 5f; // 物品间距

        loopList.SetData(inventoryData.Count, OnUpdateItem);

        // 绑定跳转按钮事件
        if (scrollToButton != null)
            scrollToButton.onClick.AddListener(OnScrollToButtonClicked);

        // 绑定输入框回车事件
        if (scrollToInputField != null)
            scrollToInputField.onEndEdit.AddListener(OnScrollToInputSubmit);
    }

    /// <summary>
    /// 生成测试背包数据
    /// </summary>
    void GenerateTestData()
    {
        inventoryData.Clear();
        for (int i = 0; i < itemCount; i++)
        {
            string name = sampleNames[i % sampleNames.Length];
            int count = UnityEngine.Random.Range(1, 99);
            inventoryData.Add(new InventoryItemData($"{name} #{i + 1}", count));
        }
    }

    /// <summary>
    /// 更新每个物品的显示（LoopList 回调）
    /// </summary>
    void OnUpdateItem(int index, UnityEngine.RectTransform item)
    {
        if (index < 0 || index >= inventoryData.Count) return;

        // 获取或添加 UI 组件
        InventoryItemUI ui = item.GetComponent<InventoryItemUI>();
        if (ui == null)
            ui = item.gameObject.AddComponent<InventoryItemUI>();

        // 自动查找子物体上的 UI
        AutoBindUI(item, ui);

        ui.Refresh(inventoryData[index]);
    }

    /// <summary>
    /// 自动绑定模板里的 UI 组件
    /// </summary>
    void AutoBindUI(UnityEngine.RectTransform item, InventoryItemUI ui)
    {
        // 用名称查找（更可靠）
        if (ui.iconImage == null)
        {
            var icon = item.Find("Icon");
            if (icon != null) ui.iconImage = icon.GetComponent<Image>();
        }

        if (ui.nameText == null)
        {
            var nameT = item.Find("NameText");
            if (nameT != null) ui.nameText = nameT.GetComponent<Text>();
        }

        if (ui.countText == null)
        {
            var countT = item.Find("CountText");
            if (countT != null) ui.countText = countT.GetComponent<Text>();
        }
    }

    // ===== 跳转功能 =====

    /// <summary>
    /// 跳转到指定索引（外部调用示例）
    /// </summary>
    public void ScrollToItem(int index)
    {
        if (loopList == null) return;
        // align=0 靠上, 0.5 居中, 1 靠下
        loopList.ScrollToIndex(index, 0.5f);
    }

    /// <summary>
    /// 跳转到第 900 个物品（索引 899）
    /// </summary>
    public void ScrollToItem900()
    {
        ScrollToItem(899);
    }

    /// <summary>
    /// 跳转到最后一个物品
    /// </summary>
    public void ScrollToLast()
    {
        ScrollToItem(inventoryData.Count - 1);
    }

    /// <summary>
    /// 跳转到第一个物品（滚回顶部）
    /// </summary>
    public void ScrollToFirst()
    {
        ScrollToItem(0);
    }

    // 按钮点击回调
    void OnScrollToButtonClicked()
    {
        if (scrollToInputField == null) return;

        if (int.TryParse(scrollToInputField.text, out int index))
        {
            // 用户输入的是"第 N 个"，转成 0-based 索引
            int idx = Mathf.Max(0, index - 1);
            ScrollToItem(idx);
        }
    }

    // 输入框回车回调
    void OnScrollToInputSubmit(string value)
    {
        if (int.TryParse(value, out int index))
        {
            int idx = Mathf.Max(0, index - 1);
            ScrollToItem(idx);
        }
    }
}
