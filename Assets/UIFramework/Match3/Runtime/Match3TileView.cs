using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameUI.Match3
{
    /// <summary>
    /// 单个三消棋子的视图组件。
    /// 该组件由 Match3Panel 在运行时动态挂载，不保存在 Prefab 中。
    /// </summary>
    public sealed class Match3TileView : MonoBehaviour, IPointerClickHandler
    {
        private Image background;
        private Image gemImage;
        private Match3Panel owner;

        public RectTransform RectTransform { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }
        public int Type { get; private set; }

        public void Initialize(Match3Panel panel)
        {
            owner = panel;
            RectTransform = GetComponent<RectTransform>();
            background = GetComponent<Image>();
            gemImage = transform.Find("Gem").GetComponent<Image>();
        }

        public void Bind(int row, int column, int type, Color color, bool selected)
        {
            Row = row;
            Column = column;
            Type = type;
            gemImage.color = color;
            background.color = selected
                ? new Color(1f, 0.82f, 0.24f, 1f)
                : new Color(0.12f, 0.15f, 0.22f, 1f);
        }

        public void SetBoardPosition(Vector2 position)
        {
            RectTransform.anchoredPosition = position;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                owner?.SelectTile(Row, Column);
            }
        }
    }
}
