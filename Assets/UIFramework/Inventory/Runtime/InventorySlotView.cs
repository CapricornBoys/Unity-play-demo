using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameUI.Inventory
{
    /// <summary>
    /// 对象池中的单个格子视图。
    /// 短按触发详情，普通拖动交给 ScrollRect，长按后拖动用于交换物品。
    /// </summary>
    public sealed class InventorySlotView :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private const float LongPressDuration = 0.35f;
        private const float MoveTolerance = 14f;

        private Image background;
        private Image icon;
        private Text nameText;
        private Text countText;
        private InventoryPanel owner;
        private InventoryItem item;
        private PointerEventData pointerEvent;
        private Vector2 pointerDownPosition;
        private float pointerDownTime;
        private int index;
        private bool pointerDown;
        private bool scrollDragging;
        private bool itemDragging;
        private bool moved;

        public RectTransform RectTransform { get; private set; }

        public void Initialize()
        {
            RectTransform = GetComponent<RectTransform>();
            background = GetComponent<Image>();
            icon = transform.Find("Icon").GetComponent<Image>();
            nameText = transform.Find("Name").GetComponent<Text>();
            countText = transform.Find("Count").GetComponent<Text>();
        }

        public void Bind(
            InventoryPanel panel,
            int slotIndex,
            InventoryItem slotItem,
            bool selected)
        {
            owner = panel;
            index = slotIndex;
            item = slotItem;

            bool hasItem = item != null;
            icon.gameObject.SetActive(hasItem);
            nameText.text = hasItem ? item.Name : string.Empty;
            countText.text = hasItem ? $"x{item.Count}" : string.Empty;
            icon.color = hasItem ? item.Color : Color.clear;
            background.color = selected
                ? new Color(0.95f, 0.72f, 0.22f, 1f)
                : new Color(0.16f, 0.2f, 0.28f, 1f);
        }

        public void ResetForPool()
        {
            owner = null;
            item = null;
            ResetGesture();
        }

        private void Update()
        {
            if (!pointerDown || moved || scrollDragging || itemDragging || item == null)
            {
                return;
            }

            if (Time.unscaledTime - pointerDownTime < LongPressDuration)
            {
                return;
            }

            // 长按且没有明显位移时才进入物品拖拽，避免与列表滑动争抢手势。
            itemDragging = true;
            owner?.BeginItemDrag(index, item, pointerEvent);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            pointerDown = true;
            pointerEvent = eventData;
            pointerDownPosition = eventData.position;
            pointerDownTime = Time.unscaledTime;
            moved = false;
            scrollDragging = false;
            itemDragging = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pointerDown)
            {
                return;
            }

            UpdateMovedState(eventData.position);
            bool suppressClick = moved || scrollDragging || itemDragging;

            if (itemDragging)
            {
                owner?.EndItemDrag(index, eventData);
                itemDragging = false;
            }

            // 只有完整的短按手势才触发详情，滑动和长按拖拽均不会触发点击。
            if (!suppressClick)
            {
                owner?.SelectSlot(index);
            }

            pointerDown = false;
            pointerEvent = null;

            // ScrollRect 的 EndDrag 可能晚于 PointerUp，滚动状态需保留到 EndDrag。
            if (!scrollDragging)
            {
                ResetGesture();
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            owner?.InventoryScrollRect.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            UpdateMovedState(eventData.position);

            if (itemDragging)
            {
                owner?.UpdateItemDrag(eventData);
                return;
            }

            scrollDragging = true;
            owner?.InventoryScrollRect.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateMovedState(eventData.position);

            if (itemDragging)
            {
                owner?.UpdateItemDrag(eventData);
            }
            else if (scrollDragging)
            {
                owner?.InventoryScrollRect.OnDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (itemDragging)
            {
                owner?.EndItemDrag(index, eventData);
            }
            else if (scrollDragging)
            {
                owner?.InventoryScrollRect.OnEndDrag(eventData);
            }

            scrollDragging = false;
            itemDragging = false;

            // 若 PointerUp 已先到达，此处完成最终清理；否则保留 moved 供点击判定。
            if (!pointerDown)
            {
                ResetGesture();
            }
        }

        private void UpdateMovedState(Vector2 currentPosition)
        {
            if (!moved
                && Vector2.Distance(pointerDownPosition, currentPosition) > MoveTolerance)
            {
                moved = true;
            }
        }

        private void ResetGesture()
        {
            pointerDown = false;
            pointerEvent = null;
            moved = false;
            scrollDragging = false;
            itemDragging = false;
        }
    }
}
