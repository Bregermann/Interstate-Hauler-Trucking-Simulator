using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsStabilization002PlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeRoadsideBuilderCreatesPhysicalBarriersAndGround()
        {
            GameObject root = new GameObject("Stabilization002 Play Roadside");
            try
            {
                LwsRoadsideRuntimeRoot runtime = LwsInterstateRoadsideBuilder.BuildStraightPairedInterstate(
                    root.transform,
                    "IH_TEST_PLAY_STABILIZATION_002",
                    -40f,
                    180f);

                yield return null;

                Assert.IsNotNull(runtime);
                Assert.IsTrue(runtime.Built);
                Assert.Greater(root.GetComponentsInChildren<BoxCollider>().Length, 0);
                Assert.Greater(root.GetComponentsInChildren<MeshCollider>().Length, 0);
                Assert.Greater(runtime.GuardrailSegmentCount, 0);
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator AutomaticKeyboardDecisionCanSelectReverseAtRestWithoutManualBypass()
        {
            LwsAutomaticKeyboardDirectionDecision automaticDecision =
                LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                    false,
                    true,
                    LwsTransmissionMode.Automatic,
                    LwsAutomaticTransmissionSelector.Drive,
                    0f,
                    0.35f);

            LwsAutomaticKeyboardDirectionDecision manualDecision =
                LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                    false,
                    true,
                    LwsTransmissionMode.Truck18Speed,
                    LwsAutomaticTransmissionSelector.Drive,
                    0f,
                    0.35f);

            yield return null;

            Assert.IsTrue(automaticDecision.handled);
            Assert.IsTrue(automaticDecision.requestSelectorChange);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, automaticDecision.requestedSelector);
            Assert.IsFalse(manualDecision.handled);
        }
    }
}
