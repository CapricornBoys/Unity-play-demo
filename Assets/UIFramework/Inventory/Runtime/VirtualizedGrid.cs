using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Inventory
{
    /// <summary>
    /// 纵向循环网格列表。
    /// 只保留可见区域附近的格子，离开区域的格子会进入对象池等待复用。
    /// </summary>
    public sealed class VirtualizedGrid : MonoBehaviour
    {
        private readonly Dictionary<int, InventorySlotView> activeViews =
            new Dictionary<int, InventorySlotView>();
        private readonly Stack<InventorySlotView> pool =
            new Stack<InventorySlotView>();
        private readonly List<int> recycleIndices = new List<int>();

        private ScrollRect scrollRect;
        private RectTransform viewport;
        private RectTransform content;
        private RectTransform template;
        private Action<int, InventorySlotView> bindItem;
        private float cellWidth;
        private float cellHeight;
        private float spacingX;
        private float spacingY;
        private int bufferRows;
        private int itemCount;
        private int columns;
        private int firstVisibleIndex = -1;
        private int lastVisibleIndex = -1;
        private Vector2 lastViewportSize;

        public int ActiveViewCount => activeViews.Count;
        public int PooledViewCount => pool.Count;
        public ScrollRect ScrollRect => scrollRect;

        public void Initialize(
            ScrollRect targetScrollRect,
            RectTransform itemTemplate,
            float width,
            float height,
            float horizontalSpacing,
            float verticalSpacing,
            int extraRows,
            Action<int, InventorySlotView> binder)
        {
            scrollRect = targetScrollRect;
            viewport = scrollRect.viewport;
            content = scrollRect.content;
            template = itemTemplate;
            cellWidth = width;
            cellHeight = height;
            spacingX = horizontalSpacing;
            spacingY = verticalSpacing;
            bufferRows = Mathf.Max(0, extraRows);
            bindItem = binder;

            template.gameObject.SetActive(false);
            scrollRect.onValueChanged.AddListener(OnScroll);
        }

        public void SetItemCount(int count, bool resetPosition = true)
        {
            itemCount = Mathf.Max(0, count);
            if (resetPosition)
            {
                content.anchoredPosition = Vector2.zero;
            }

            RecalculateLayout();
            RefreshVisible(true);
        }

        public void RefreshAll()
        {
            foreach (KeyValuePair<int, InventorySlotView> pair in activeViews)
            {
                bindItem?.Invoke(pair.Key, pair.Value);
            }
        }

        public bool TryGetIndexAtScreenPoint(
            Vector2 screenPoint,
            Camera eventCamera,
            out int index)
        {
            index = -1;
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPoint, eventCamera))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    content,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            // ScreenPointToLocalPoint 的原点位于 Content Pivot，需要换算到左上角坐标。
            float x = localPoint.x + content.rect.width * content.pivot.x;
            float y = -localPoint.y;
            int column = Mathf.FloorToInt(x / (cellWidth + spacingX));
            int row = Mathf.FloorToInt(y / (cellHeight + spacingY));

            if (column < 0 || column >= columns || row < 0)
            {
                return false;
            }

            index = row * columns + column;
            return index >= 0 && index < itemCount;
        }

        private void LateUpdate()
        {
            if (viewport == null)
            {
                return;
            }

            if (lastViewportSize != viewport.rect.size)
            {
                RecalculateLayout();
                RefreshVisible(true);
            }
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScroll);
            }
        }

        private void OnScroll(Vector2 _)
        {
            RefreshVisible(false);
        }

        private void RecalculateLayout()
        {
            lastViewportSize = viewport.rect.size;
            float strideX = cellWidth + spacingX;
            columns = Mathf.Max(
                1,
                Mathf.FloorToInt((viewport.rect.width + spacingX) / strideX));

            int rows = Mathf.CeilToInt(itemCount / (float)columns);
            float contentHeight = rows > 0
                ? rows * cellHeight + Mathf.Max(0, rows - 1) * spacingY
                : 0f;

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, contentHeight);
        }

        private void RefreshVisible(bool force)
        {
            if (force && activeViews.Count > 0)
            {
                RecycleAll();
                firstVisibleIndex = -1;
                lastVisibleIndex = -1;
            }

            if (itemCount == 0 || viewport.rect.height <= 0f)
            {
                RecycleAll();
                return;
            }

            float strideY = cellHeight + spacingY;
            float scrollY = Mathf.Max(0f, content.anchoredPosition.y);
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / strideY) - bufferRows);
            int lastRow = Mathf.CeilToInt(
                (scrollY + viewport.rect.height) / strideY) + bufferRows;

            int newFirstIndex = Mathf.Clamp(firstRow * columns, 0, itemCount - 1);
            int newLastIndex = Mathf.Clamp(
                (lastRow + 1) * columns - 1,
                0,
                itemCount - 1);

            if (!force
                && newFirstIndex == firstVisibleIndex
                && newLastIndex == lastVisibleIndex)
            {
                return;
            }

            firstVisibleIndex = newFirstIndex;
            lastVisibleIndex = newLastIndex;

            recycleIndices.Clear();
            foreach (KeyValuePair<int, InventorySlotView> pair in activeViews)
            {
                if (pair.Key < firstVisibleIndex || pair.Key > lastVisibleIndex)
                {
                    recycleIndices.Add(pair.Key);
                }
            }

            for (int i = 0; i < recycleIndices.Count; i++)
            {
                Recycle(recycleIndices[i]);
            }

            for (int index = firstVisibleIndex; index <= lastVisibleIndex; index++)
            {
                if (activeViews.ContainsKey(index))
                {
                    continue;
                }

                InventorySlotView view = GetView();
                activeViews.Add(index, view);
                PositionView(view.RectTransform, index);
                view.gameObject.SetActive(true);
                bindItem?.Invoke(index, view);
            }
        }

        private InventorySlotView GetView()
        {
            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            RectTransform clone = Instantiate(template, content);
            clone.name = "PooledSlot";
            InventorySlotView view = clone.gameObject.AddComponent<InventorySlotView>();
            view.Initialize();
            return view;
        }

        private void PositionView(RectTransform rect, int index)
        {
            int row = index / columns;
            int column = index % columns;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(cellWidth, cellHeight);
            rect.anchoredPosition = new Vector2(
                column * (cellWidth + spacingX),
                -row * (cellHeight + spacingY));
        }

        private void Recycle(int index)
        {
            InventorySlotView view = activeViews[index];
            activeViews.Remove(index);
            view.ResetForPool();
            view.gameObject.SetActive(false);
            pool.Push(view);
        }

        private void RecycleAll()
        {
            recycleIndices.Clear();
            recycleIndices.AddRange(activeViews.Keys);
            for (int i = 0; i < recycleIndices.Count; i++)
            {
                Recycle(recycleIndices[i]);
            }
        }
    }
}
