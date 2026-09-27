using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRegional")]
    public sealed class TruckTaxiHudRegionalRegressionTests
    {
        [Test]
        public void DrivingStatusBarsDoNotDuplicateTopDriverNeeds()
        {
            var status = (string[])typeof(TruckTaxiStatusBars).GetField("Names", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var needs = (string[])typeof(TruckTaxiEnvironmentNeedsPanel).GetField("NeedNames", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            CollectionAssert.AreEquivalent(new[] { "FUEL", "DAMAGE", "PATIENCE" }, status);
            CollectionAssert.AreEquivalent(new[] { "BLADDER", "HUNGER", "THIRST" }, needs);
            Assert.IsFalse(status.Intersect(needs).Any());
        }

        [Test]
        public void DebugVisibilityTracksPanelAndHidesStatusBarsImmediately()
        {
            var root = new GameObject("HUD regression test", typeof(RectTransform));
            try
            {
                var hud = root.AddComponent<TruckTaxiHud>();
                var debug = root.AddComponent<TruckTaxiDebugPanel>();
                var bars = root.AddComponent<TruckTaxiStatusBars>();
                var panel = new GameObject("Debug panel", typeof(RectTransform)).GetComponent<RectTransform>();
                panel.SetParent(root.transform, false);
                Set(debug, "panel", panel);
                Set(hud, "debug", debug);
                var vehicle = new GameObject("Vehicle bars", typeof(RectTransform)).GetComponent<RectTransform>();
                var effects = new GameObject("Effect bars", typeof(RectTransform)).GetComponent<RectTransform>();
                vehicle.SetParent(root.transform, false);
                effects.SetParent(root.transform, false);
                Set(bars, "panel", vehicle);
                Set(bars, "effectPanel", effects);

                panel.gameObject.SetActive(false);
                Assert.IsFalse(hud.DebugOverlayOpen);
                panel.gameObject.SetActive(true);
                Assert.IsTrue(hud.DebugOverlayOpen);
                Assert.IsFalse(bars.RefreshVisibility());
                Assert.IsFalse(vehicle.gameObject.activeSelf);
                Assert.IsFalse(effects.gameObject.activeSelf);
                Assert.DoesNotThrow(hud.OnDebugVisibilityChanged);

                panel.gameObject.SetActive(false);
                var fullMap = root.AddComponent<TruckTaxiFullMapPresenter>();
                var mapRoot = new GameObject("Full map", typeof(RectTransform)).GetComponent<RectTransform>();
                mapRoot.SetParent(root.transform, false);
                Set(fullMap, "root", mapRoot);
                Set(hud, "<FullMap>k__BackingField", fullMap);
                Assert.IsTrue(hud.NormalHudSuppressed);
                Assert.DoesNotThrow(hud.OnFullMapVisibilityChanged);
                mapRoot.gameObject.SetActive(false);
                Assert.IsFalse(hud.NormalHudSuppressed);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, name);
            field.SetValue(target, value);
        }
    }
}
