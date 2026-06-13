using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameUI.Inventory
{
    /// <summary>
    /// 背包界面控制器。
    /// 包含循环网格、对象池、点击详情和拖拽交换位置功能。
    /// </summary>
    public sealed class InventoryPanel : UIPanel
    {
        private const int SlotCount = 200;

        private readonly List<InventoryItem> items = new List<InventoryItem>(SlotCount);

        private VirtualizedGrid grid;
        private Text detailName;
        private Text detailCount;
        private Text detailDescription;
        private Image detailIcon;
        private RectTransform dragGhost;
        private Image dragGhostIcon;
        private Text dragGhostText;
        private int selectedIndex = -1;

        public int ActiveSlotViewCount => grid != null ? grid.ActiveViewCount : 0;
        public int PooledSlotViewCount => grid != null ? grid.PooledViewCount : 0;
        public ScrollRect InventoryScrollRect => grid.ScrollRect;

        protected override void OnInitialize()
        {
            Button closeButton = GetUI<Button>("CloseButton");
            closeButton.onClick.AddListener(Close);

            detailName = GetUI<Text>("DetailName");
            detailCount = GetUI<Text>("DetailCount");
            detailDescription = GetUI<Text>("DetailDescription");
            detailIcon = GetUI<Image>("DetailIcon");

            ScrollRect scrollRect = GetUI<ScrollRect>("InventoryScroll");
            RectTransform template = GetUI<RectTransform>("SlotTemplate");

            grid = scrollRect.gameObject.AddComponent<VirtualizedGrid>();
            grid.Initialize(
                scrollRect,
                template,
                150f,
                150f,
                16f,
                16f,
                1,
                BindSlot);

            CreateDragGhost();
            BuildDemoData();
            grid.SetItemCount(items.Count);
            ClearDetails();
        }

        protected override void OnOpen(object args)
        {
            grid.RefreshAll();
            RefreshDetails();
        }

        public void SelectSlot(int index)
        {
            selectedIndex = index;
            grid.RefreshAll();
            RefreshDetails();
        }

        public void BeginItemDrag(
            int index,
            InventoryItem item,
            PointerEventData eventData)
        {
            grid.ScrollRect.enabled = false;
            dragGhost.gameObject.SetActive(true);
            dragGhostIcon.color = item.Color;
            dragGhostText.text = item.Name;
            UpdateDragGhostPosition(eventData);
        }

        public void UpdateItemDrag(PointerEventData eventData)
        {
            UpdateDragGhostPosition(eventData);
        }

        public void EndItemDrag(int sourceIndex, PointerEventData eventData)
        {
            grid.ScrollRect.enabled = true;
            dragGhost.gameObject.SetActive(false);

            if (!grid.TryGetIndexAtScreenPoint(
                    eventData.position,
                    eventData.pressEventCamera,
                    out int targetIndex)
                || targetIndex == sourceIndex)
            {
                return;
            }

            SwapItems(sourceIndex, targetIndex);
        }

        private void BindSlot(int index, InventorySlotView view)
        {
            view.Bind(this, index, items[index], index == selectedIndex);
        }

        private void SwapItems(int sourceIndex, int targetIndex)
        {
            InventoryItem temporary = items[sourceIndex];
            items[sourceIndex] = items[targetIndex];
            items[targetIndex] = temporary;

            if (selectedIndex == sourceIndex)
            {
                selectedIndex = targetIndex;
            }
            else if (selectedIndex == targetIndex)
            {
                selectedIndex = sourceIndex;
            }

            grid.RefreshAll();
            RefreshDetails();
        }

        private void BuildDemoData()
        {
            items.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                items.Add(null);
            }

            string[] names =
            {
                "Health Potion",
                "Mana Potion",
                "Iron Sword",
                "Oak Shield",
                "Fire Scroll",
                "Blue Crystal",
                "Traveler Boots",
                "Ancient Key"
            };

            Color[] colors =
            {
                new Color(0.85f, 0.18f, 0.22f),
                new Color(0.18f, 0.42f, 0.92f),
                new Color(0.72f, 0.76f, 0.82f),
                new Color(0.56f, 0.34f, 0.16f),
                new Color(0.96f, 0.42f, 0.12f),
                new Color(0.2f, 0.82f, 0.9f),
                new Color(0.46f, 0.3f, 0.2f),
                new Color(0.95f, 0.78f, 0.18f)
            };

            for (int i = 0; i < 72; i++)
            {
                int type = i % names.Length;
                items[i] = new InventoryItem(
                    i + 1,
                    names[type],
                    $"A demo item of type {names[type]}. Long press and drag it to exchange positions.",
                    i % 5 + 1,
                    colors[type]);
            }
        }

        private void RefreshDetails()
        {
            if (selectedIndex < 0
                || selectedIndex >= items.Count
                || items[selectedIndex] == null)
            {
                ClearDetails();
                return;
            }

            InventoryItem item = items[selectedIndex];
            detailIcon.gameObject.SetActive(true);
            detailIcon.color = item.Color;
            detailName.text = item.Name;
            detailCount.text = $"Count: {item.Count}";
            detailDescription.text = item.Description;
        }

        private void ClearDetails()
        {
            detailIcon.gameObject.SetActive(false);
            detailName.text = "Select an item";
            detailCount.text = string.Empty;
            detailDescription.text =
                "Click an item to view details.\nLong press, then drag to exchange positions.";
        }

        private void CreateDragGhost()
        {
            GameObject ghost = new GameObject(
                "DragGhost",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            dragGhost = ghost.GetComponent<RectTransform>();
            dragGhost.SetParent(transform, false);
            dragGhost.sizeDelta = new Vector2(130f, 130f);

            CanvasGroup group = ghost.GetComponent<CanvasGroup>();
            group.alpha = 0.9f;
            group.blocksRaycasts = false;
            group.interactable = false;

            dragGhostIcon = ghost.GetComponent<Image>();

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(Text));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(dragGhost, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);

            dragGhostText = labelObject.GetComponent<Text>();
            dragGhostText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dragGhostText.fontSize = 18;
            dragGhostText.alignment = TextAnchor.LowerCenter;
            dragGhostText.color = Color.white;
            dragGhostText.raycastTarget = false;
            ghost.SetActive(false);
        }

        private void UpdateDragGhostPosition(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                dragGhost.anchoredPosition = localPoint;
                dragGhost.SetAsLastSibling();
            }
        }
    }
}
