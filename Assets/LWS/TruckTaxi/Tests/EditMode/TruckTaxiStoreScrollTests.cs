using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiStoreScrollTests
    {
        [TestCase(1920, 1080, 1f)] [TestCase(2560, 1440, 1f)] [TestCase(3440, 1440, 1f)]
        [TestCase(1920, 1080, 1.25f)] [TestCase(2560, 1440, 1.25f)] [TestCase(3440, 1440, 1.25f)]
        public void DynamicCatalogIsMaskedClearOfFooterAndFocusScrollsLastItem(int width, int height, float uiScale)
        {
            var root = new GameObject("Store panel", typeof(RectTransform));
            var events = new GameObject("Store test events", typeof(EventSystem));
            var eventSystem = events.GetComponent<EventSystem>();
            try
            {
                // EditMode does not call EventSystem.OnEnable to register this fixture globally.
                var panel = root.GetComponent<RectTransform>(); panel.sizeDelta = new Vector2(width * .58f, height * .72f) / uiScale;
                var scroll = TruckTaxiStoreScrollView.Create(panel, out var content);
                Assert.IsFalse(scroll.horizontal); Assert.IsTrue(scroll.vertical);
                Assert.NotNull(scroll.viewport.GetComponent<RectMask2D>());
                Assert.Greater(scroll.viewport.anchorMin.y, .13f); Assert.Less(scroll.viewport.anchorMax.y, .86f);
                RectTransform last = null;
                for (int i = 0; i < 43; i++)
                {
                    var cell = new GameObject("Future item " + i, typeof(RectTransform), typeof(Image), typeof(Button));
                    last = cell.GetComponent<RectTransform>(); last.SetParent(content, false);
                }
                var helper = scroll.GetComponent<TruckTaxiStoreScrollView>(); helper.RefreshLayoutAndFocus(eventSystem);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Assert.Greater(content.rect.height, scroll.viewport.rect.height);
                float cellWidth = content.GetComponent<GridLayoutGroup>().cellSize.x;
                Assert.AreEqual((scroll.viewport.rect.width - 26) / 2, cellWidth, .1f);
                eventSystem.SetSelectedGameObject(last.gameObject);
                Assert.AreSame(last.gameObject, eventSystem.currentSelectedGameObject);
                helper.RefreshLayoutAndFocus(eventSystem);
                Assert.Greater(content.anchoredPosition.y, 0);
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, last);
                Assert.GreaterOrEqual(bounds.min.y, scroll.viewport.rect.yMin - .1f);
                Assert.LessOrEqual(bounds.max.y, scroll.viewport.rect.yMax + .1f);
                eventSystem.SetSelectedGameObject(content.GetChild(0).gameObject); helper.RefreshLayoutAndFocus(eventSystem);
                Assert.AreEqual(0, content.anchoredPosition.y, .1f);
                panel.sizeDelta = panel.sizeDelta * .8f; helper.RefreshLayoutAndFocus(eventSystem);
                Assert.Less(content.GetComponent<GridLayoutGroup>().cellSize.x, cellWidth);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(events); }
        }
    }
}
