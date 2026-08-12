using NUnit.Framework;
using UnityEngine;

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

            Assert.GreaterOrEqual(registry.CountByType(LwsCabAccessoryAnchorType.DashboardAccessory), 2);
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.HangingAccessory));
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.PassengerSeat));
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.Sleeper));
            Assert.AreEqual(1, registry.CountByType(LwsCabAccessoryAnchorType.PersonalMemento));
            Assert.IsTrue(registry.TryGetAnchor("IH_CabAnchor_Dashboard01", out _));
            Assert.IsTrue(registry.Validate(out string message), message);
            Object.DestroyImmediate(go);
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
    }
}
