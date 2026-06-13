using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 通用循环列表组件（对象池复用，高性能）
/// 适用于背包、排行榜等大量数据的列表
/// 支持：ScrollToIndex 跳转、垂直/水平布局
/// </summary>
public class LoopList : MonoBehaviour
{
    [Header("必填：物品模板（Content 的子物体）")]
    public RectTransform itemTemplate;

    [Header("必填：ScrollView 的 Content")]
    public RectTransform content;

    [Header("每个物品的高度（垂直）或宽度（水平）")]
    public float itemSize = 100f;

    [Header("物品之间的间距")]
    public float itemSpacing = 5f;

    [Header("垂直列表 / 水平列表")]
    public Direction direction = Direction.Vertical;

    [Header("超出边界多少距离后开始回收（像素）")]
    public float recycleThreshold = 50f;

    public enum Direction { Vertical, Horizontal }

    // 数据总数
    private int dataCount;
    // 当前数据提供者
    private Action<int, RectTransform> onUpdateItem;

    // 活跃物品 <-> 对应的数据索引
    private readonly Dictionary<RectTransform, int> activeItemIndexMap = new();
    private readonly List<RectTransform> activeItems = new();
    private readonly Queue<RectTransform> pool = new();

    private ScrollRect scrollRect;
    private float lastAnchoredPos;
    private float viewportSize; // 视口大小（高或宽）

    // 单个物品的占距（size + spacing）
    private float ItemStride => itemSize + itemSpacing;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("LoopList 需要挂载在带有 ScrollRect 的 GameObject 上！");
            return;
        }

        if (itemTemplate == null)
        {
            Debug.LogError("请设置 itemTemplate！");
            return;
        }

        itemTemplate.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // 监听滚动事件（更精准，替代 Update 轮询）
        if (scrollRect != null)
            scrollRect.onValueChanged.AddListener(OnScroll);
    }

    void OnDisable()
    {
        if (scrollRect != null)
            scrollRect.onValueChanged.RemoveListener(OnScroll);
    }

    /// <summary>
    /// 设置数据并更新列表
    /// </summary>
    /// <param name="count">数据总数</param>
    /// <param name="onUpdate">更新每个物品的回调 (index, itemTransform)</param>
    public void SetData(int count, Action<int, RectTransform> onUpdate)
    {
        dataCount = count;
        onUpdateItem = onUpdate;

        // 清空旧物品
        foreach (var item in activeItems)
        {
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }
        activeItems.Clear();
        activeItemIndexMap.Clear();

        // 设置 Content 大小
        float totalSize = dataCount * ItemStride - itemSpacing; // 最后一个物品无 spacing
        if (direction == Direction.Vertical)
            content.sizeDelta = new Vector2(content.sizeDelta.x, totalSize);
        else
            content.sizeDelta = new Vector2(totalSize, content.sizeDelta.y);

        // 记录视口大小
        viewportSize = direction == Direction.Vertical
            ? scrollRect.viewport.rect.height
            : scrollRect.viewport.rect.width;

        // 滚回顶部
        content.anchoredPosition = Vector2.zero;
        lastAnchoredPos = 0;

        RefreshVisibleItems();
    }

    /// <summary>
    /// 滚动到指定索引（核心功能！）
    /// </summary>
    /// <param name="index">目标物品的索引（0-based）</param>
    /// <param name="align">对齐方式：0=靠上/左, 0.5=居中, 1=靠下/右</param>
    public void ScrollToIndex(int index, float align = 0.5f)
    {
        if (dataCount == 0 || index < 0 || index >= dataCount) return;

        // 计算目标 anchoredPosition
        float targetPos = index * ItemStride;

        // 对齐偏移：让目标物品显示在视口指定位置
        float offset = align * viewportSize - itemSize / 2f;
        targetPos -= offset;

        // 限制范围，不能滚出边界
        float maxScroll = Mathf.Max(0, dataCount * ItemStride - ItemStride - viewportSize + itemSize);
        targetPos = Mathf.Clamp(targetPos, 0, maxScroll);

        if (direction == Direction.Vertical)
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetPos);
        else
            content.anchoredPosition = new Vector2(-targetPos, content.anchoredPosition.y);

        // 立即刷新
        RefreshVisibleItems();
    }

    /// <summary>
    /// 刷新全部（数据变化后调用）
    /// </summary>
    public void RefreshAll()
    {
        RefreshVisibleItems();
    }

    // 滚动事件回调（替代 Update 轮询，更高效）
    void OnScroll(Vector2 normalizedPos)
    {
        if (dataCount == 0) return;
        RefreshVisibleItems();
    }

    void RefreshVisibleItems()
    {
        if (onUpdateItem == null) return;

        // 计算可见范围
        float scrollPos = direction == Direction.Vertical
            ? -content.anchoredPosition.y
            : content.anchoredPosition.x;

        float viewStart = scrollPos - recycleThreshold;
        float viewEnd = viewStart + viewportSize + recycleThreshold * 2;

        int startIndex = Mathf.Max(0, Mathf.FloorToInt(viewStart / ItemStride));
        int endIndex = Mathf.Min(dataCount - 1, Mathf.FloorToInt((viewEnd - itemSize) / ItemStride));

        // 回收不在可见范围内的物品
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            var item = activeItems[i];
            int idx = activeItemIndexMap[item];

            if (idx < startIndex || idx > endIndex)
            {
                item.gameObject.SetActive(false);
                pool.Enqueue(item);
                activeItems.RemoveAt(i);
                activeItemIndexMap.Remove(item);
            }
        }

        // 创建/复用可见范围内的物品
        for (int i = startIndex; i <= endIndex; i++)
        {
            if (IsIndexActive(i)) continue;

            RectTransform item = GetItem();
            activeItems.Add(item);
            activeItemIndexMap[item] = i;

            // 设置位置（锚点设为左上，Pivot(0,1) 方便计算）
            SetItemPosition(item, i);

            item.gameObject.SetActive(true);
            onUpdateItem.Invoke(i, item);
        }
    }

    void SetItemPosition(RectTransform item, int index)
    {
        // 确保锚点为左上，Pivot 为 (0,1)，这样 position 计算最直接
        item.anchorMin = new Vector2(0, 1);
        item.anchorMax = new Vector2(0, 1);
        item.pivot = new Vector2(0, 1);

        float pos = index * ItemStride;

        if (direction == Direction.Vertical)
            item.anchoredPosition = new Vector2(0, -pos);
        else
            item.anchoredPosition = new Vector2(pos, 0);
    }

    bool IsIndexActive(int index)
    {
        foreach (var kv in activeItemIndexMap)
        {
            if (kv.Value == index) return true;
        }
        return false;
    }

    RectTransform GetItem()
    {
        if (pool.Count > 0)
        {
            var item = pool.Dequeue();
            item.gameObject.SetActive(true);
            return item;
        }

        var newItem = Instantiate(itemTemplate, content);
        newItem.name = "Item";
        // 确保新物品锚点正确
        newItem.anchorMin = new Vector2(0, 1);
        newItem.anchorMax = new Vector2(0, 1);
        newItem.pivot = new Vector2(0, 1);
        return newItem;
    }

    /// <summary>
    /// 获取当前可见的第一个物品索引
    /// </summary>
    public int GetFirstVisibleIndex()
    {
        float scrollPos = direction == Direction.Vertical
            ? -content.anchoredPosition.y
            : content.anchoredPosition.x;
        return Mathf.Max(0, Mathf.FloorToInt(scrollPos / ItemStride));
    }

    /// <summary>
    /// 获取当前活跃物品数量（调试用）
    /// </summary>
    public int GetActiveItemCount()
    {
        return activeItems.Count;
    }
}
