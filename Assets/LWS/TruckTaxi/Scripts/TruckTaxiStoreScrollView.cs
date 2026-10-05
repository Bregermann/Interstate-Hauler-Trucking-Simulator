using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LWS.TruckTaxi
{
    // Extends uGUI ScrollRect with responsive two-column cells and focused-item visibility.
    [DisallowMultipleComponent]
    public sealed class TruckTaxiStoreScrollView : MonoBehaviour
    {
        private ScrollRect scroll;
        private GridLayoutGroup grid;
        private GameObject lastSelection;
        private float lastWidth = -1;
        public static ScrollRect Create(RectTransform parent, out RectTransform content)
        {
            var viewport = TruckTaxiHud.Rect(parent, "Store item viewport", new Vector2(.05f, .17f), new Vector2(.95f, .85f));
            viewport.gameObject.AddComponent<Image>().color = new Color(.05f, .07f, .08f, .7f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            content = TruckTaxiHud.Rect(viewport, "Store catalog content", new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.spacing = new Vector2(10, 10); grid.padding = new RectOffset(8, 8, 8, 8);
            grid.cellSize = new Vector2(400, 72);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content; scroll.viewport = viewport;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
            var helper = viewport.gameObject.AddComponent<TruckTaxiStoreScrollView>();
            helper.scroll = scroll; helper.grid = grid;
            return scroll;
        }
        private void OnEnable() { lastSelection = null; lastWidth = -1; }
        private void LateUpdate()
        { RefreshLayoutAndFocus(); }
        public void RefreshLayoutAndFocus() => RefreshLayoutAndFocus(EventSystem.current);
        public void RefreshLayoutAndFocus(EventSystem eventSystem)
        {
            if (scroll == null) return;
            float width = scroll.viewport.rect.width;
            bool resized = !Mathf.Approximately(lastWidth, width);
            if (resized)
            {
                grid.cellSize = new Vector2(Mathf.Max(1, (width - grid.padding.horizontal - grid.spacing.x) / 2), 72);
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content); lastWidth = width;
            }
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (!resized && selected == lastSelection) return;
            lastSelection = selected;
            if (selected == null || !selected.transform.IsChildOf(scroll.content)) return;
            // Heat may put the Selectable below the cell root.
            Transform cell = selected.transform;
            while (cell.parent != scroll.content && cell.parent != null) cell = cell.parent;
            EnsureVisible(scroll, cell as RectTransform);
        }
        public static void EnsureVisible(ScrollRect scroll, RectTransform cell)
        {
            if (scroll == null || cell == null || !cell.IsChildOf(scroll.content)) return;
            Canvas.ForceUpdateCanvases();
            Bounds item = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, cell);
            Rect view = scroll.viewport.rect;
            var padding = scroll.content.GetComponent<GridLayoutGroup>()?.padding;
            float top = view.yMax - (padding?.top ?? 0), bottom = view.yMin + (padding?.bottom ?? 0);
            float adjustment = item.max.y > top ? top - item.max.y : item.min.y < bottom ? bottom - item.min.y : 0;
            if (Mathf.Approximately(adjustment, 0)) return;
            scroll.StopMovement();
            Vector2 position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + adjustment, 0, Mathf.Max(0, scroll.content.rect.height - view.height));
            scroll.content.anchoredPosition = position;
        }
    }
}
