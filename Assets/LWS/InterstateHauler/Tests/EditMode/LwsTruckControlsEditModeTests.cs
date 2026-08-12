using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsTruckControlsEditModeTests
    {
        [Test]
        public void CommandFrameCombineKeepsPressedEdges()
        {
            LwsVehicleCommandFrame keyboard = new LwsVehicleCommandFrame
            {
                parkingBrakeToggle = LwsMomentaryIntent.Pressed
            };
            LwsVehicleCommandFrame wheel = new LwsVehicleCommandFrame
            {
                parkingBrakeToggle = LwsMomentaryIntent.Held,
                horn = LwsMomentaryIntent.Held
            };

            LwsVehicleCommandFrame combined = LwsVehicleCommandFrameUtility.Combine(keyboard, wheel);

            Assert.AreEqual(LwsMomentaryIntent.Pressed, combined.parkingBrakeToggle);
            Assert.AreEqual(LwsMomentaryIntent.Held, combined.horn);
        }

        [Test]
        public void ToggleCommandsOnlyReactToPressedEdges()
        {
            GameObject go = new GameObject("truck-control-toggle-test");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { parkingBrakeToggle = LwsMomentaryIntent.Pressed }, default);
            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { parkingBrakeToggle = LwsMomentaryIntent.Held }, default);

            Assert.IsTrue(controller.CurrentState.parkingBrakeOn);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SignalAndHazardStateTransitionsAreDeterministic()
        {
            GameObject go = new GameObject("truck-control-signal-test");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { leftIndicator = LwsMomentaryIntent.Pressed }, default);
            Assert.AreEqual(LwsTurnSignalState.Left, controller.CurrentState.turnSignal);
            Assert.IsFalse(controller.CurrentState.hazardsOn);

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { rightIndicator = LwsMomentaryIntent.Pressed }, default);
            Assert.AreEqual(LwsTurnSignalState.Right, controller.CurrentState.turnSignal);

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { hazardLights = LwsMomentaryIntent.Pressed }, default);
            Assert.AreEqual(LwsTurnSignalState.Off, controller.CurrentState.turnSignal);
            Assert.IsTrue(controller.CurrentState.hazardsOn);

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { leftIndicator = LwsMomentaryIntent.Pressed }, default);
            Assert.AreEqual(LwsTurnSignalState.Left, controller.CurrentState.turnSignal);
            Assert.IsFalse(controller.CurrentState.hazardsOn);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void WiperStateCyclesWithoutVisualImplementation()
        {
            GameObject go = new GameObject("truck-control-wiper-test");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { wipers = LwsMomentaryIntent.Pressed }, default);
            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { wipers = LwsMomentaryIntent.Pressed }, default);
            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { wiperDecrease = LwsMomentaryIntent.Pressed }, default);

            Assert.AreEqual(LwsWiperState.Intermittent, controller.CurrentState.wiperState);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void UnsupportedEngineBrakeAndRetarderDoNotInventState()
        {
            LwsTruckControlCapabilities capabilities = LwsTruckControlCapabilities.NwhSemiDevelopmentDefault();
            Assert.IsTrue(capabilities.Validate(out string message), message);

            GameObject go = new GameObject("truck-control-brake-feature-test");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame
            {
                engineBrake = LwsMomentaryIntent.Pressed,
                retarderIncrease = LwsMomentaryIntent.Pressed
            }, default);

            Assert.AreEqual(0, controller.CurrentState.engineBrakeLevel);
            Assert.AreEqual(0, controller.CurrentState.retarderLevel);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CruiseFallbackFeedsNormalizedInputWithoutApplyingForces()
        {
            GameObject go = new GameObject("truck-control-cruise-test");
            LwsTruckControlController controller = go.AddComponent<LwsTruckControlController>();

            controller.ApplyCommandFrame(new LwsVehicleCommandFrame { cruiseSet = LwsMomentaryIntent.Pressed }, default);

            Assert.IsTrue(controller.CurrentState.cruiseEnabled);
            Assert.Greater(controller.CurrentState.cruiseTargetSpeedMetersPerSecond, 0f);
            Assert.Greater(controller.CurrentState.cruiseThrottleOutput, 0f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TruckControlServiceRejectsDuplicateActiveControllers()
        {
            var service = new LwsTruckControlService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            GameObject firstGo = new GameObject("first-control");
            GameObject secondGo = new GameObject("second-control");
            LwsTruckControlController first = firstGo.AddComponent<LwsTruckControlController>();
            LwsTruckControlController second = secondGo.AddComponent<LwsTruckControlController>();

            LwsServiceResult firstResult = service.RegisterActiveController(first);
            LwsServiceResult secondResult = service.RegisterActiveController(second);

            Assert.IsTrue(firstResult.Succeeded, firstResult.Message);
            Assert.IsFalse(secondResult.Succeeded);
            Object.DestroyImmediate(firstGo);
            Object.DestroyImmediate(secondGo);
        }

        [Test]
        public void FlipOffNoTargetIsSafeAndCooldownPreventsSpam()
        {
            GameObject go = new GameObject("gesture-test");
            LwsPlayerGestureController gestureController = go.AddComponent<LwsPlayerGestureController>();

            bool accepted = gestureController.RequestFlipOff(out LwsDriverGestureEvent first);
            bool acceptedAgain = gestureController.RequestFlipOff(out LwsDriverGestureEvent second);

            Assert.IsTrue(accepted);
            Assert.IsFalse(first.hasValidTarget);
            Assert.IsFalse(acceptedAgain);
            Assert.AreEqual(first.gestureType, second.gestureType);
            Object.DestroyImmediate(go);
        }
    }
}
