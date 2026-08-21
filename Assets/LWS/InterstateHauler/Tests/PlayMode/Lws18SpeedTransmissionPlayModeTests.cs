using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class Lws18SpeedTransmissionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ControllerDefaultsToAutomaticAndStaysNeutralWithMissingInput()
        {
            var go = new GameObject("transmission-controller");
            Lws18SpeedTransmissionController controller = go.AddComponent<Lws18SpeedTransmissionController>();
            controller.SetDefinition(Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset());

            yield return null;
            yield return null;

            LwsTransmissionState state = controller.CaptureState();
            Assert.AreEqual(LwsTransmissionMode.Automatic, state.mode);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Drive, state.automaticSelector);
            Assert.AreEqual(Lws18SpeedGearId.Neutral, state.logicalGear);
            Assert.AreEqual(LwsShiftRejectionReason.None, state.lastRejectionReason);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator AutomaticSelectorCanChooseDriveNeutralAndReverseAtRest()
        {
            var go = new GameObject("transmission-controller-auto-selector");
            Lws18SpeedTransmissionController controller = go.AddComponent<Lws18SpeedTransmissionController>();
            controller.SetDefinition(Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset());

            yield return null;

            Assert.IsTrue(controller.TrySetAutomaticSelector(LwsAutomaticTransmissionSelector.Neutral, out string neutralMessage), neutralMessage);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Neutral, controller.AutomaticSelector);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Neutral, controller.DisplayState.automaticSelector);

            Assert.IsTrue(controller.TrySetAutomaticSelector(LwsAutomaticTransmissionSelector.Reverse, out string reverseMessage), reverseMessage);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, controller.AutomaticSelector);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, controller.DisplayState.automaticSelector);
            Assert.AreEqual("R", controller.DisplayState.automaticTargetLabel);

            Assert.IsTrue(controller.TrySetAutomaticSelector(LwsAutomaticTransmissionSelector.Drive, out string driveMessage), driveMessage);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Drive, controller.AutomaticSelector);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Drive, controller.DisplayState.automaticSelector);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator AutomaticSelectorUnavailableInManualMode()
        {
            var go = new GameObject("transmission-controller-auto-selector-manual");
            Lws18SpeedTransmissionController controller = go.AddComponent<Lws18SpeedTransmissionController>();
            controller.SetDefinition(Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset());

            yield return null;

            Assert.IsTrue(controller.TrySetDevelopmentAutomaticTestMode(false, out _));
            Assert.IsFalse(controller.TrySetAutomaticSelector(LwsAutomaticTransmissionSelector.Reverse, out string message));
            StringAssert.Contains("AUTOMATIC SELECTOR UNAVAILABLE IN MANUAL MODE", message);
            Assert.AreEqual(LwsTransmissionMode.Truck18Speed, controller.CaptureState().mode);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator ApplyingGearIntentWithoutNwhFailsSafely()
        {
            var go = new GameObject("transmission-controller-no-nwh");
            Lws18SpeedTransmissionController controller = go.AddComponent<Lws18SpeedTransmissionController>();
            controller.SetDefinition(Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset());
            controller.SetMode(LwsTransmissionMode.Truck18Speed);

            yield return null;

            controller.ApplyGearIntent(new LwsTruckGearIntent
            {
                physicalGate = LwsTruckShifterGate.Gate3,
                range = LwsTruckRange.Low,
                splitter = LwsTruckSplitter.Low
            });

            LwsTransmissionState state = controller.CaptureState();
            Assert.AreEqual(LwsShiftRejectionReason.GearUnavailable, state.lastRejectionReason);
            Assert.AreEqual(LwsTransmissionShiftState.Rejected, state.shiftState);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator SaveRestoreRequiresShifterSynchronization()
        {
            var go = new GameObject("transmission-controller-save");
            Lws18SpeedTransmissionController controller = go.AddComponent<Lws18SpeedTransmissionController>();
            controller.SetDefinition(Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset());

            yield return null;

            controller.RestoreState(new LwsTransmissionState
            {
                mode = LwsTransmissionMode.Truck18Speed,
                logicalGear = Lws18SpeedGearId.Gear8High,
                logicalRatioIndex = 18,
                displayLabel = "8H",
                physicalGate = LwsTruckShifterGate.Gate6,
                requestedRange = LwsTruckRange.High,
                engagedRange = LwsTruckRange.High,
                requestedSplitter = LwsTruckSplitter.High,
                engagedSplitter = LwsTruckSplitter.High,
                nwhGear = 18
            });

            LwsTransmissionState restored = controller.CaptureState();
            Assert.IsTrue(restored.requiresShifterSynchronization);
            Assert.AreEqual(Lws18SpeedGearId.Gear8High, restored.logicalGear);
            Object.Destroy(go);
        }
    }
}
