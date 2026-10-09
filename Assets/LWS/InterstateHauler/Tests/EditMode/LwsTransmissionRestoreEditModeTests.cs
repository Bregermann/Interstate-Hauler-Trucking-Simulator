using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsTransmissionRestoreEditModeTests
    {
        private GameObject root;
        private Lws18SpeedTransmissionController controller;
        private Lws18SpeedTransmissionDefinition definition;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private void Set(string name, object value) => typeof(Lws18SpeedTransmissionController).GetField(name, Private).SetValue(controller, value);
        private T Get<T>(string name) => (T)typeof(Lws18SpeedTransmissionController).GetField(name, Private).GetValue(controller);

        [SetUp] public void Setup()
        {
            root = new GameObject("Transmission restore fixture");
            controller = root.AddComponent<Lws18SpeedTransmissionController>();
            definition = Lws18SpeedTransmissionDefinition.CreateTransientG29DevelopmentPreset();
            controller.SetDefinition(definition);
        }

        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(root); Object.DestroyImmediate(definition); }

        private static LwsTransmissionState Saved(LwsTransmissionMode mode, LwsAutomaticTransmissionSelector selector, int gear) =>
            new LwsTransmissionState {
                mode = mode, automaticSelector = selector, nwhGear = gear,
                logicalGear = gear < 0 ? Lws18SpeedGearId.Reverse1 : gear == 0 ? Lws18SpeedGearId.Neutral : Lws18SpeedGearId.LowLow,
                logicalRatioIndex = gear > 0 ? 1 : 0, neutral = gear == 0, reverse = gear < 0,
                physicalGate = gear < 0 ? LwsTruckShifterGate.Reverse : gear == 0 ? LwsTruckShifterGate.Neutral : LwsTruckShifterGate.Gate2,
                requestedRange = LwsTruckRange.Low, engagedRange = LwsTruckRange.Low,
                requestedSplitter = LwsTruckSplitter.Low, engagedSplitter = LwsTruckSplitter.Low,
                clutchInput = 1f, requiresShifterSynchronization = true
            };

        [TestCase(LwsAutomaticTransmissionSelector.Drive, 1)]
        [TestCase(LwsAutomaticTransmissionSelector.Reverse, -1)]
        [TestCase(LwsAutomaticTransmissionSelector.Neutral, 0)]
        public void AutomaticRestoreReplacesManualModePreservingSelectorAndGear(LwsAutomaticTransmissionSelector selector, int gear)
        {
            controller.SetMode(LwsTransmissionMode.Truck18Speed);
            Set("_nextAutomaticShiftTime", float.PositiveInfinity);
            Set("_automaticTargetNwhGear", 18);
            Set("_automaticTargetLabel", "STALE");
            Set("_lastClutchInput", 1f);
            controller.RestoreState(Saved(LwsTransmissionMode.Automatic, selector, gear));
            var state = Get<LwsTransmissionState>("_state");
            Assert.IsTrue(controller.AutomaticModeActive);
            Assert.AreEqual(LwsTransmissionMode.Automatic, controller.DisplayState.mode);
            Assert.AreEqual(selector, controller.AutomaticSelector);
            Assert.AreEqual(gear, state.nwhGear);
            Assert.AreEqual(gear, controller.AutomaticTargetNwhGear);
            Assert.AreNotEqual("STALE", controller.AutomaticTargetLabel);
            Assert.IsFalse(state.requiresShifterSynchronization);
            Assert.AreEqual(0f, state.clutchInput);
            Assert.AreEqual(0f, Get<float>("_lastClutchInput"));
            Assert.AreEqual(0f, Get<float>("_nextAutomaticShiftTime"));
        }

        [Test] public void ManualRestoreRequiresMatchingShifterAndDoesNotForceNeutral()
        {
            controller.SetMode(LwsTransmissionMode.Automatic);
            controller.RestoreState(Saved(LwsTransmissionMode.Truck18Speed, LwsAutomaticTransmissionSelector.Reverse, 1));
            Assert.IsFalse(controller.AutomaticModeActive);
            Assert.AreEqual(LwsTransmissionMode.Truck18Speed, controller.DisplayState.mode);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, controller.AutomaticSelector);
            Assert.AreEqual(1, Get<LwsTransmissionState>("_state").nwhGear);
            Assert.IsTrue(Get<LwsTransmissionState>("_state").requiresShifterSynchronization);
            controller.ApplyGearIntent(new LwsTruckGearIntent { neutralRequested = true, physicalGate = LwsTruckShifterGate.Neutral });
            Assert.AreEqual(LwsTransmissionShiftState.ShifterMismatch, Get<LwsTransmissionState>("_state").shiftState);
            Assert.AreEqual(1, Get<LwsTransmissionState>("_state").nwhGear);
            controller.ApplyGearIntent(new LwsTruckGearIntent {
                physicalGate = LwsTruckShifterGate.Gate2, range = LwsTruckRange.Low, splitter = LwsTruckSplitter.Low
            });
            Assert.IsFalse(Get<LwsTransmissionState>("_state").requiresShifterSynchronization);
        }

        [Test] public void AutomaticRestoreUsesFreshThrottleInsteadOfOldCooldownOrShifterGate()
        {
            controller.SetMode(LwsTransmissionMode.Truck18Speed);
            Set("_nextAutomaticShiftTime", float.PositiveInfinity);
            controller.RestoreState(Saved(LwsTransmissionMode.Automatic, LwsAutomaticTransmissionSelector.Drive, 0));
            typeof(Lws18SpeedTransmissionController).GetMethod("ProcessAutomaticTransmission", Private).Invoke(controller,
                new object[] { definition, new LwsVehicleContinuousInput { throttle = 1f } });
            Assert.AreEqual(controller.AutomaticStartingForwardGear, controller.AutomaticTargetNwhGear);
            Assert.IsFalse(Get<LwsTransmissionState>("_state").requiresShifterSynchronization);
            Assert.AreNotEqual(LwsTransmissionShiftState.Preselected, Get<LwsTransmissionState>("_state").shiftState);
        }

        [Test] public void SaveParticipantRestoreUsesTheSameModeAndSynchronizationPolicy()
        {
            controller.SetMode(LwsTransmissionMode.Truck18Speed);
            var payload = new Lws18SpeedTransmissionSavePayload {
                schemaVersion = 1, mode = LwsTransmissionMode.Automatic,
                automaticSelector = LwsAutomaticTransmissionSelector.Reverse,
                logicalGear = Lws18SpeedGearId.Reverse1, nwhGear = -1
            };
            var result = controller.RestoreState(new LwsSaveParticipantState {
                participantId = controller.ParticipantId, payloadVersion = controller.PayloadVersion,
                payloadJson = JsonUtility.ToJson(payload)
            });
            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.IsTrue(controller.AutomaticModeActive);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, controller.AutomaticSelector);
            Assert.IsFalse(Get<LwsTransmissionState>("_state").requiresShifterSynchronization);
            Assert.AreEqual(-1, Get<LwsTransmissionState>("_state").nwhGear);
        }

        [Test] public void InvalidModeIsRejectedBeforeMutation()
        {
            controller.RestoreState(Saved(LwsTransmissionMode.Automatic, LwsAutomaticTransmissionSelector.Reverse, -1));
            string before = JsonUtility.ToJson(Get<LwsTransmissionState>("_state"));
            Assert.Throws<ArgumentOutOfRangeException>(() => controller.RestoreState(Saved((LwsTransmissionMode)999, LwsAutomaticTransmissionSelector.Drive, 1)));
            Assert.AreEqual(before, JsonUtility.ToJson(Get<LwsTransmissionState>("_state")));
            Assert.IsTrue(controller.AutomaticModeActive);
        }

        [TestCase(LwsTransmissionMode.Automatic)]
        [TestCase(LwsTransmissionMode.Truck18Speed)]
        public void ClearStateKeepsConfiguredMode(LwsTransmissionMode mode)
        {
            controller.SetMode(mode);
            Assert.IsTrue(controller.ClearState().Succeeded);
            Assert.AreEqual(mode, Get<LwsTransmissionMode>("mode"));
            Assert.AreEqual(mode, Get<LwsTransmissionState>("_state").mode);
            Assert.IsTrue(Get<LwsTransmissionState>("_state").neutral);
            Assert.IsFalse(Get<LwsTransmissionState>("_state").requiresShifterSynchronization);
        }
    }
}
