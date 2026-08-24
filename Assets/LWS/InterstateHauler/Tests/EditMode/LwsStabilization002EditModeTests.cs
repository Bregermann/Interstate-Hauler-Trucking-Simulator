using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsStabilization002EditModeTests
    {
        [Test]
        public void CrossSectionProfileCreatesRoadsideGroundShouldersDitchesAndGuardrails()
        {
            GameObject root = new GameObject("Stabilization002 Roadside Test");
            try
            {
                LwsInterstateCrossSectionProfile profile = LwsInterstateCrossSectionProfile.CreateValidationDefault();
                Assert.IsTrue(profile.Validate(out string message), message);

                LwsRoadsideRuntimeRoot runtime = LwsInterstateRoadsideBuilder.BuildStraightPairedInterstate(
                    root.transform,
                    "IH_TEST_STABILIZATION_002",
                    0f,
                    260f,
                    profile);

                Assert.IsNotNull(runtime);
                Assert.IsTrue(runtime.Built);
                Assert.GreaterOrEqual(runtime.GroundMeshCount, 1);
                Assert.GreaterOrEqual(runtime.MedianMeshCount, 1);
                Assert.GreaterOrEqual(runtime.ShoulderStripCount, 4);
                Assert.GreaterOrEqual(runtime.DitchStripCount, 4);
                Assert.Greater(runtime.GuardrailSegmentCount, 0);
                Assert.Greater(root.GetComponentsInChildren<BoxCollider>().Length, 0);
                Assert.Greater(root.GetComponentsInChildren<MeshCollider>().Length, 0);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AutomaticKeyboardDirectionPolicyBrakesBeforeChangingDirection()
        {
            LwsAutomaticKeyboardDirectionDecision forwardBrake =
                LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                    false,
                    true,
                    LwsTransmissionMode.Automatic,
                    LwsAutomaticTransmissionSelector.Drive,
                    8f,
                    0.35f);

            Assert.IsTrue(forwardBrake.handled);
            Assert.IsFalse(forwardBrake.requestSelectorChange);
            Assert.AreEqual(0f, forwardBrake.throttle);
            Assert.AreEqual(1f, forwardBrake.brake);

            LwsAutomaticKeyboardDirectionDecision selectReverse =
                LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                    false,
                    true,
                    LwsTransmissionMode.Automatic,
                    LwsAutomaticTransmissionSelector.Drive,
                    0.05f,
                    0.35f);

            Assert.IsTrue(selectReverse.requestSelectorChange);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, selectReverse.requestedSelector);
            Assert.AreEqual(1f, selectReverse.throttle);
            Assert.AreEqual(0f, selectReverse.brake);

            LwsAutomaticKeyboardDirectionDecision reverseBrake =
                LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                    true,
                    false,
                    LwsTransmissionMode.Automatic,
                    LwsAutomaticTransmissionSelector.Reverse,
                    -4f,
                    0.35f);

            Assert.IsFalse(reverseBrake.requestSelectorChange);
            Assert.AreEqual(0f, reverseBrake.throttle);
            Assert.AreEqual(1f, reverseBrake.brake);
        }

        [Test]
        public void AutomaticKeyboardDirectionPolicyDoesNotHandleManualMode()
        {
            LwsAutomaticKeyboardDirectionDecision decision =
                LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                    false,
                    true,
                    LwsTransmissionMode.Truck18Speed,
                    LwsAutomaticTransmissionSelector.Drive,
                    0f,
                    0.35f);

            Assert.IsFalse(decision.handled);
        }

        [Test]
        public void StabilizationRuntimeSourceContainsProjectOwnedUtsAndFontFixes()
        {
            string trafficApi = File.ReadAllText("Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsTrafficApi.cs");
            string trafficTypes = File.ReadAllText("Assets/LWS/InterstateHauler/Traffic/LwsTrafficLaneTypes.cs");
            string gameClock = File.ReadAllText("Assets/LWS/InterstateHauler/World/Time/LwsGameClock.cs");
            string cabGps = File.ReadAllText("Assets/LWS/InterstateHauler/Navigation/LwsCabGpsController.cs");
            string devUi = File.ReadAllText("Assets/LWS/InterstateHauler/UI/Development/LwsDevelopmentUiRoot.cs");

            StringAssert.Contains("owner.SetActive(false)", trafficApi);
            StringAssert.Contains("PopulatePathPoints", trafficApi);
            StringAssert.Contains("offRoadCleanupDistanceMeters", trafficTypes);
            StringAssert.Contains("safetyFloorMeters", trafficTypes);
            StringAssert.DoesNotContain("Arial.ttf", gameClock);
            StringAssert.DoesNotContain("Arial.ttf", cabGps);
            StringAssert.DoesNotContain("Arial.ttf", devUi);
        }

        [Test]
        public void GpsVoicePackAssetUsesDedicatedScriptableObjectFile()
        {
            LwsGpsVoicePack pack = AssetDatabase.LoadAssetAtPath<LwsGpsVoicePack>(
                "Assets/LWS/InterstateHauler/Navigation/Data/IH_GpsVoicePack_Default.asset");

            Assert.IsNotNull(pack);
            Assert.IsTrue(pack.ValidateSlots(out string message), message);
        }

        [Test]
        public void StabilizationDocumentationExists()
        {
            Assert.IsTrue(File.Exists("Documentation/InterstateHauler/Stabilization_002_Highway_Baseline.md"));
            Assert.IsTrue(File.Exists("Documentation/InterstateHauler/Roads/Highway_Cross_Section_Profile.md"));
            Assert.IsTrue(File.Exists("Documentation/InterstateHauler/Traffic/UTS_Traffic_Light_API_Audit.md"));
        }
    }
}
