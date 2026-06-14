using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameUI.MergeTwo
{
    /// <summary>
    /// Runtime view for one merge-two board cell.
    /// </summary>
    public sealed class MergeTwoTileView :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerClickHandler,
        IDragHandler
    {
        private const float DragThreshold = 32f;

        private Image background;
        private Image tileImage;
        private Text valueText;
        private MergeTwoPanel owner;
        private bool dragTriggered;

        public RectTransform RectTransform { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }
        public int Level { get; private set; }

        public void Initialize(MergeTwoPanel panel)
        {
            owner = panel;
            RectTransform = GetComponent<RectTransform>();
            background = GetComponent<Image>();
            tileImage = transform.Find("Gem").GetComponent<Image>();

            GameObject labelObject = new GameObject(
                "Value",
                typeof(RectTransform),
                typeof(Text));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(tileImage.transform, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            valueText = labelObject.GetComponent<Text>();
            valueText.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.fontStyle = FontStyle.Bold;
            valueText.color = Color.white;
            valueText.raycastTarget = false;
        }

        public void Bind(
            int row,
            int column,
            int level,
            Color color,
            bool selected)
        {
            Row = row;
            Column = column;
            Level = level;

            bool occupied = level >= 0;
            tileImage.enabled = occupied;
            tileImage.color = color;
            valueText.text = occupied
                ? GetTileValue(level).ToString()
                : string.Empty;
            valueText.fontSize = level < 6 ? 30 : 23;
            background.color = selected
                ? new Color(1f, 0.82f, 0.24f, 1f)
                : new Color(0.12f, 0.15f, 0.22f, 1f);
        }

        public void SetBoardPosition(Vector2 position)
        {
            RectTransform.anchoredPosition = position;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragTriggered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                && !dragTriggered)
            {
                owner?.SelectTile(Row, Column);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragTriggered
                || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Vector2 dragDelta = eventData.position - eventData.pressPosition;
            if (dragDelta.sqrMagnitude < DragThreshold * DragThreshold)
            {
                return;
            }

            Vector2Int direction;
            if (Mathf.Abs(dragDelta.x) >= Mathf.Abs(dragDelta.y))
            {
                direction = new Vector2Int(dragDelta.x > 0f ? 1 : -1, 0);
            }
            else
            {
                direction = new Vector2Int(0, dragDelta.y > 0f ? -1 : 1);
            }

            dragTriggered = owner != null
                            && owner.TryDragMove(Row, Column, direction);
        }

        private static int GetTileValue(int level)
        {
            return 1 << Mathf.Clamp(level + 1, 1, 20);
        }
    }
}
