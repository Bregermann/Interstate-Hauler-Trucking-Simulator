using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsDashboardMirrorsCabEditModeTests
    {
        [Test]
        public void DashboardDefinitionConvertsSpeedToMph()
        {
            LwsTruckDashboardDefinition definition = ScriptableObject.CreateInstance<LwsTruckDashboardDefinition>();

            float mph = definition.MetersPerSecondToDisplaySpeed(26.8224f);

            Assert.AreEqual(60f, mph, 0.05f);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void GaugeMappingValidatesAndMapsAngles()
        {
            var mapping = new LwsDashboardGaugeMapping
            {
                minimumValue = 0f,
                maximumValue = 100f,
                startAngle = 10f,
                endAngle = 110f,
                smoothing = 0.25f
            };

            Assert.IsTrue(mapping.Validate(out string message), message);
            Assert.AreEqual(60f, mapping.MapToAngle(50f), 0.001f);
        }

        [Test]
        public void SignalAndHazardBlinkLogicUsesSharedTiming()
        {
            var snapshot = new LwsTruckDashboardSnapshot
            {
                hazardsOn = true,
                turnSignal = LwsTurnSignalState.Off
            };

            Assert.IsTrue(snapshot.ShowLeftSignal(0.25f));
            Assert.IsTrue(snapshot.ShowRightSignal(0.25f));
            Assert.IsFalse(snapshot.ShowLeftSignal(0.75f));
            Assert.IsFalse(snapshot.ShowRightSignal(0.75f));
        }

        [Test]
        public void MirrorPresetValidationRejectsInvalidResolution()
        {
            var preset = new LwsMirrorQualityPreset
            {
                quality = LwsMirrorQuality.Low,
                enabled = true,
                resolutionPixels = 64,
                updateIntervalFrames = 1,
                nearClip = 0.05f,
                farClip = 100f
            };

            Assert.IsFalse(preset.Validate(out string message));
            StringAssert.Contains("resolution", message);
        }

        [Test]
        public void CabAnchorRegistryCreatesRequiredStableAnchors()
        {
            GameObject go = new GameObject("cab-anchor-registry-test");
            new GameObject("Cab").transform.SetParent(go.transform, false);
            LwsCabAccessoryAnchorRegistry registry = go.AddComponent<LwsCabAccessoryAnchorRegistry>();

            registry.EnsureInitialized();

            Assert.GreaterOrEqual(registry.CountByType(LwsCabAccessoryAnchorType.DashboardAccessory), 4);
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.HangingAccessory));
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.PassengerSeat));
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.Sleeper));
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.PersonalMemento));
            Assert.IsTrue(registry.TryGetAnchor("IH_CabAnchor_Dashboard01", out _));
            Assert.IsTrue(registry.TryGetAnchor(LwsCabAccessoryAnchorRegistry.GpsMountAnchorId, out _));
            Assert.IsTrue(registry.TryGetAnchor(LwsCabAccessoryAnchorRegistry.DashDecorationAnchorId, out _));
            Assert.IsTrue(registry.Validate(out string message), message);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CabInteriorRecoveryCreatesDashDecorationAndSleeperWithoutTextPlaceholder()
        {
            GameObject go = new GameObject("cab-recovery-test");
            new GameObject("Cab").transform.SetParent(go.transform, false);
            LwsCabAccessoryAnchorRegistry registry = go.AddComponent<LwsCabAccessoryAnchorRegistry>();
            LwsTruckCabInteriorRecovery recovery = go.AddComponent<LwsTruckCabInteriorRecovery>();

            recovery.ApplyRecovery();

            Assert.IsTrue(recovery.GpsMountReady);
            Assert.IsTrue(recovery.DashDecorationReady);
            Assert.IsTrue(recovery.SleeperInteriorReady);
            Assert.IsTrue(registry.TryGetAnchor(LwsCabAccessoryAnchorRegistry.DashDecorationAnchorId, out LwsCabAccessoryAnchor decorationAnchor));
            Assert.IsTrue(decorationAnchor.Occupied);
            Assert.AreEqual(LwsCabAccessoryAnchorRegistry.DashDecorationPlaceholderName, decorationAnchor.AttachedAccessory.name);
            Assert.IsNull(decorationAnchor.AttachedAccessory.GetComponent<TextMesh>());
            Assert.IsTrue(registry.TryGetAnchor("IH_CabAnchor_Sleeper", out LwsCabAccessoryAnchor sleeperAnchor));
            Assert.IsTrue(sleeperAnchor.Occupied);
            Assert.AreEqual(LwsTruckCabInteriorRecovery.SleeperPlaceholderRootName, sleeperAnchor.AttachedAccessory.name);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CabGpsFallbackCanvasCanBeHiddenForCompassPresentation()
        {
            GameObject go = new GameObject("cab-gps-fallback-test");
            new GameObject("Cab").transform.SetParent(go.transform, false);
            go.AddComponent<LwsCabAccessoryAnchorRegistry>().EnsureInitialized();
            LwsCabGpsController gps = go.AddComponent<LwsCabGpsController>();

            gps.BindPhysicalScreen();
            Assert.IsTrue(gps.PhysicalGpsBound);
            Assert.IsTrue(gps.FallbackPhysicalScreenVisible);
            Assert.AreEqual(RenderMode.WorldSpace, gps.PhysicalCanvas.renderMode);
            RectTransform canvasRect = gps.PhysicalCanvas.GetComponent<RectTransform>();
            AssertPositiveRect(canvasRect, 640f, 400f);
            AssertPositiveRect(gps.SemanticMapGraphic.GetComponent<RectTransform>(), 612f, 304f);

            gps.SetFallbackPhysicalScreenVisible(false);
            Assert.IsFalse(gps.FallbackPhysicalScreenVisible);
            Assert.IsTrue(gps.PhysicalGpsBound);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CabCompassFitRepairsCriticalRuntimeRectsToPositiveScreenSize()
        {
            GameObject root = new GameObject("IH Cab GPS Compass Navigator Pro", typeof(RectTransform), typeof(Canvas));
            GameObject miniMapRoot = new GameObject("MiniMap Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            GameObject miniMap = new GameObject("MiniMap", typeof(RectTransform), typeof(Image));
            GameObject miniMapMask = new GameObject("MiniMapMask", typeof(RectTransform), typeof(Image), typeof(Mask));

            try
            {
                miniMapRoot.transform.SetParent(root.transform, false);
                miniMap.transform.SetParent(miniMapRoot.transform, false);
                miniMapMask.transform.SetParent(miniMap.transform, false);
                root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

                MethodInfo fitMethod = typeof(LwsCompassNavigatorProAdapter).GetMethod(
                    "FitCabCompassToPhysicalScreen",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(fitMethod);

                fitMethod.Invoke(null, new object[] { root, new Vector2(640f, 400f) });

                AssertPositiveRect(root.GetComponent<RectTransform>(), 640f, 400f);
                AssertPositiveRect(miniMapRoot.GetComponent<RectTransform>(), 640f, 400f);
                AssertPositiveRect(miniMap.GetComponent<RectTransform>(), 640f, 400f);
                AssertPositiveRect(miniMapMask.GetComponent<RectTransform>(), 640f, 400f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AccessoryWithRigidbodyCannotAttach()
        {
            GameObject anchorGo = new GameObject("anchor");
            LwsCabAccessoryAnchor anchor = anchorGo.AddComponent<LwsCabAccessoryAnchor>();
            anchor.Configure("IH_CabAnchor_Dashboard99", LwsCabAccessoryAnchorType.DashboardAccessory, "test");
            GameObject accessory = new GameObject("unsafe-accessory");
            accessory.AddComponent<Rigidbody>();

            LwsCabAccessoryAttachmentResult result = anchor.Attach(accessory);

            Assert.IsFalse(result.Succeeded);
            Object.DestroyImmediate(accessory);
            Object.DestroyImmediate(anchorGo);
        }

        [Test]
        public void DashboardServiceRejectsDuplicateActiveControllers()
        {
            var service = new LwsTruckDashboardService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            GameObject firstGo = new GameObject("first-dashboard");
            GameObject secondGo = new GameObject("second-dashboard");
            LwsTruckDashboardController first = firstGo.AddComponent<LwsTruckDashboardController>();
            LwsTruckDashboardController second = secondGo.AddComponent<LwsTruckDashboardController>();

            LwsServiceResult firstResult = service.RegisterActiveController(first);
            LwsServiceResult secondResult = service.RegisterActiveController(second);

            Assert.IsTrue(firstResult.Succeeded, firstResult.Message);
            Assert.IsFalse(secondResult.Succeeded);
            Object.DestroyImmediate(firstGo);
            Object.DestroyImmediate(secondGo);
        }

        private static void AssertPositiveRect(RectTransform rect, float expectedWidth, float expectedHeight)
        {
            Assert.IsNotNull(rect);
            Assert.AreEqual(expectedWidth, rect.sizeDelta.x, 0.01f);
            Assert.AreEqual(expectedHeight, rect.sizeDelta.y, 0.01f);
            Assert.Greater(rect.rect.width, 1f);
            Assert.Greater(rect.rect.height, 1f);
            Assert.LessOrEqual(rect.anchorMin.x, rect.anchorMax.x);
            Assert.LessOrEqual(rect.anchorMin.y, rect.anchorMax.y);
            Assert.Greater(Mathf.Abs(rect.localScale.x), 0.0001f);
            Assert.Greater(Mathf.Abs(rect.localScale.y), 0.0001f);
        }
    }
}
