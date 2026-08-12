using System;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsG29InputEditModeTests
    {
        [Test]
        public void SteeringCalibrationNormalizesDeadzoneAndExtremes()
        {
            LwsWheelAxisCalibration calibration = LwsWheelAxisCalibration.SteeringDefault();
            calibration.deadzone = 0.1f;

            Assert.AreEqual(0f, LwsWheelCalibrationUtility.NormalizeSteering(0.05f, calibration), 0.0001f);
            Assert.AreEqual(1f, LwsWheelCalibrationUtility.NormalizeSteering(1f, calibration), 0.0001f);
            Assert.AreEqual(-1f, LwsWheelCalibrationUtility.NormalizeSteering(-1f, calibration), 0.0001f);
        }

        [Test]
        public void InvertedPedalCalibrationStillMapsReleasedZeroPressedOne()
        {
            var calibration = new LwsWheelPedalCalibration
            {
                releasedValue = 1f,
                pressedValue = 0f,
                deadzone = 0f,
                saturation = 0f,
                inverted = false
            };

            Assert.AreEqual(0f, LwsWheelCalibrationUtility.NormalizePedal(1f, calibration), 0.0001f);
            Assert.AreEqual(1f, LwsWheelCalibrationUtility.NormalizePedal(0f, calibration), 0.0001f);
        }

        [Test]
        public void ShifterStatesMapToValidationNwhGears()
        {
            LwsTruckGearIntent gate3 = LwsWheelCalibrationUtility.ToValidationGearIntent(
                new LwsHPatternShifterState
                {
                    activeGate = LwsTruckShifterGate.Gate3,
                    range = LwsTruckRange.Low,
                    splitter = LwsTruckSplitter.Low
                },
                true);
            LwsTruckGearIntent neutral = LwsWheelCalibrationUtility.ToValidationGearIntent(LwsHPatternShifterState.Neutral(), true);
            LwsTruckGearIntent reverse = LwsWheelCalibrationUtility.ToValidationGearIntent(
                new LwsHPatternShifterState
                {
                    activeGate = LwsTruckShifterGate.Reverse,
                    reverse = true
                },
                true);

            Assert.AreEqual(LwsTruckShifterGate.Gate3, gate3.physicalGate);
            Assert.AreEqual(3, gate3.requestedLogicalGear);
            Assert.IsTrue(neutral.neutralRequested);
            Assert.IsTrue(reverse.reverseRequested);
        }

        [Test]
        public void CalibrationProfileSerializesBindings()
        {
            LwsWheelCalibrationProfile profile = LwsWheelCalibrationProfile.CreateDefaultLogitechG29();
            profile.SetBinding(LwsWheelControlBinding.Create(
                LwsWheelLogicalControl.RangeToggle,
                LwsWheelControlKind.Button,
                "/TestWheel/button7",
                "Button 7"));
            profile.SetBinding(LwsWheelControlBinding.Create(
                LwsWheelLogicalControl.SplitterToggle,
                LwsWheelControlKind.Button,
                "/TestWheel/button8",
                "Button 8"));

            string json = profile.ToJson();
            LwsWheelCalibrationProfile copy = LwsWheelCalibrationProfile.FromJson(json);

            Assert.AreEqual("/TestWheel/button7", copy.rangeBinding.controlPath);
            Assert.AreEqual("/TestWheel/button8", copy.splitterBinding.controlPath);
        }

        [Test]
        public void VehicleInputServiceRejectsDuplicateWheelOwners()
        {
            var service = new LwsVehicleInputService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));

            var first = new DummyInputSource("first");
            var second = new DummyInputSource("second");

            LwsServiceResult firstResult = service.SetInputSource(first, LwsVehicleInputOwner.Wheel);
            LwsServiceResult secondResult = service.SetInputSource(second, LwsVehicleInputOwner.Wheel);

            Assert.IsTrue(firstResult.Succeeded, firstResult.Message);
            Assert.IsFalse(secondResult.Succeeded);
        }

        [Test]
        public void DisconnectNeutralizesWheelInput()
        {
            var go = new GameObject("wheel-source");
            LwsWheelInputSource source = go.AddComponent<LwsWheelInputSource>();
            source.SetSimulatedFrameForTests(new LwsWheelInputFrame
            {
                connectionState = LwsWheelConnectionState.Connected,
                analog = new LwsWheelAnalogState { steering = 1f, throttle = 1f, brake = 0.5f, clutch = 0.25f },
                shifter = new LwsHPatternShifterState { activeGate = LwsTruckShifterGate.Gate4 }
            });

            source.NeutralizeForDisconnect("Wheel disconnected for test.");

            LwsVehicleContinuousInput continuous = source.ReadContinuousInput();
            LwsTruckGearIntent gear = source.ReadGearIntent();
            Assert.AreEqual(0f, continuous.steering);
            Assert.AreEqual(0f, continuous.throttle);
            Assert.IsTrue(gear.neutralRequested);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void ForceFeedbackUsesDirectInputBackendWhenAvailableAndFailsSafelyWithoutDevice()
        {
            var service = new LwsForceFeedbackService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            service.ApplySettings(new LwsForceFeedbackSettings
            {
                enabled = true,
                masterStrength = 2f,
                alignmentStrength = 2f,
                dampingStrength = 2f,
                roadStrength = 2f,
                impactStrength = 2f,
                directInputBackendRequired = true,
                preferredDeviceSearchTerm = "g29"
            });

            service.SetForces(10f, 10f, 10f, 10f);

            Assert.AreEqual(
                service.DirectInputBackendAvailable ? "unity-directinput.dmanager" : "lws.ffb.unavailable",
                service.Status.backendId);

            if (!service.Status.deviceConnected)
            {
                Assert.IsFalse(service.Status.enabled);
            }

            Assert.LessOrEqual(Math.Abs(LwsWheelCalibrationUtility.ClampForce(10f)), 1f);
        }

        [Test]
        public void PluginVerifiedCompatibilityStatusIsDistinctFromPhysicalVerification()
        {
            Assert.AreNotEqual(
                LwsWheelCompatibilityStatus.PluginVerified,
                LwsWheelCompatibilityStatus.SupportedPhysicallyVerified);
        }

        private sealed class DummyInputSource : ILwsVehicleInputSource
        {
            public DummyInputSource(string id)
            {
                SourceId = id;
            }

            public string SourceId { get; }
            public LwsVehicleContinuousInput ReadContinuousInput() => default;
            public LwsVehicleCommandFrame ReadCommandFrame() => default;
            public LwsTruckGearIntent ReadGearIntent() => default;
        }
    }
}
