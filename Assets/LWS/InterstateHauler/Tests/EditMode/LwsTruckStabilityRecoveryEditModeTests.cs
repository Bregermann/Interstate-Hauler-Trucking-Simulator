using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsTruckStabilityRecoveryEditModeTests
    {
        [Test]
        public void StabilityControllerDefaultsTargetHeavyHighwayTruckBehavior()
        {
            GameObject go = new GameObject("stability-test");
            try
            {
                LwsTruckStabilityController stability = go.AddComponent<LwsTruckStabilityController>();

                Assert.AreEqual(new Vector3(0f, -0.45f, 0f), stability.CenterOfMassOffset);
                Assert.AreEqual(14000f, stability.FrontAntiRollStrength, 0.01f);
                Assert.AreEqual(18000f, stability.RearAntiRollStrength, 0.01f);
                Assert.AreEqual(0.9f, stability.ForceApplicationPointDistance, 0.001f);
                Assert.AreEqual(35f, stability.EvaluateEffectiveMaxSteeringAngle(0f), 0.001f);
                Assert.AreEqual(8f, stability.EvaluateEffectiveMaxSteeringAngle(70f * 0.44704f), 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void UprightResetRotationPreservesHeadingAndRemovesPitchRoll()
        {
            Quaternion tippedRotation = Quaternion.Euler(58f, 123f, -74f);
            Quaternion upright = LwsTruckUprightRecoveryController.CreateUprightYawRotation(tippedRotation);
            Vector3 expectedForward = Vector3.ProjectOnPlane(tippedRotation * Vector3.forward, Vector3.up).normalized;
            Vector3 actualForward = Vector3.ProjectOnPlane(upright * Vector3.forward, Vector3.up).normalized;

            Assert.AreEqual(1f, Vector3.Dot(upright * Vector3.up, Vector3.up), 0.0001f);
            Assert.Less(Vector3.Angle(expectedForward, actualForward), 0.01f);
        }

        [Test]
        public void FlippedDetectionUsesConfigurableUpDotThreshold()
        {
            GameObject go = new GameObject("flipped-test");
            try
            {
                Assert.IsFalse(LwsTruckUprightRecoveryController.IsSubstantiallyOverturnedTransform(go.transform, 0.35f));

                go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

                Assert.IsTrue(LwsTruckUprightRecoveryController.IsSubstantiallyOverturnedTransform(go.transform, 0.35f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ResetTruckCommandCombinesThroughExistingInputFrame()
        {
            LwsVehicleCommandFrame keyboard = new LwsVehicleCommandFrame
            {
                resetTruckUpright = LwsMomentaryIntent.Pressed
            };
            LwsVehicleCommandFrame wheel = new LwsVehicleCommandFrame
            {
                resetTruckUpright = LwsMomentaryIntent.Held
            };

            LwsVehicleCommandFrame combined = LwsVehicleCommandFrameUtility.Combine(keyboard, wheel);

            Assert.AreEqual(LwsMomentaryIntent.Pressed, combined.resetTruckUpright);
        }

        [Test]
        public void PlayerTruckRuntimePathInstallsStabilityAndRecoveryComponents()
        {
            string spawner = File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs");
            string playerTruck = File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruck.cs");

            StringAssert.Contains("AddComponent<LwsTruckStabilityController>", spawner);
            StringAssert.Contains("ApplyStabilityTuning", spawner);
            StringAssert.Contains("AddComponent<LwsTruckUprightRecoveryController>", spawner);
            StringAssert.Contains("EnsureRuntimeSupportComponents", playerTruck);
            StringAssert.Contains("StabilityController", playerTruck);
            StringAssert.Contains("UprightRecoveryController", playerTruck);
        }

        [Test]
        public void ResetTruckInputAndHudUseExistingControlSurfaces()
        {
            string keyboard = File.ReadAllText("Assets/LWS/InterstateHauler/Input/LwsKeyboardGamepadTruckInputSource.cs");
            string wheelProfile = File.ReadAllText("Assets/LWS/InterstateHauler/Input/Wheels/LwsWheelCalibrationProfile.cs");
            string wheelSource = File.ReadAllText("Assets/LWS/InterstateHauler/Input/Wheels/LwsWheelInputSource.cs");
            string ui = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("keyboard.uKey", keyboard);
            StringAssert.Contains("resetTruckChord", keyboard);
            StringAssert.Contains("resetTruckUpright", keyboard);
            StringAssert.Contains("ResetTruckUpright", wheelProfile);
            StringAssert.Contains("resetTruckUprightBinding", wheelSource);
            StringAssert.Contains("Reset Truck Button", ui);
            StringAssert.Contains("RESET TRUCK", ui);
            StringAssert.Contains("RequestResetTruckUpright", ui);
            StringAssert.Contains("ResolveUprightRecoveryController", ui);
        }

        [Test]
        public void RecoveryComponentDoesNotReplaceNwhOrFreezeRotation()
        {
            string recovery = File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsTruckUprightRecoveryController.cs");
            string stability = File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsTruckStabilityController.cs");

            StringAssert.Contains("VehicleController", recovery);
            StringAssert.Contains("TrailerHitchModuleWrapper", recovery);
            StringAssert.Contains("Physics.SyncTransforms", recovery);
            StringAssert.Contains("body.linearVelocity = Vector3.zero", recovery);
            StringAssert.Contains("body.angularVelocity = Vector3.zero", recovery);
            StringAssert.Contains("antiRollBarForce", stability);
            StringAssert.Contains("speedSensitiveSteeringCurve", stability);
            Assert.IsFalse(recovery.Contains("RigidbodyConstraints.FreezeRotation"));
            Assert.IsFalse(stability.Contains("RigidbodyConstraints.FreezeRotation"));
        }
    }
}
