using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace DeadAir.Tests.EditMode
{
    public sealed class DeadAirEditModeTests
    {
        [Test]
        public void DefaultGpsStateIsEnabledAndRouteVisible()
        {
            DeadAirGpsState state = DeadAirGpsState.Default();
            Assert.IsTrue(state.enabled);
            Assert.IsTrue(state.routeVisible);
            Assert.AreEqual(DeadAirGpsPresentationMode.Normal, state.presentationMode);
        }

        [Test]
        public void BeatLayoutContainsRequiredEndTrigger()
        {
            bool found = false;
            foreach (DeadAirBeatDefinition beat in DeadAirBeatLayoutUtility.CreateDefaultBeatDefinitions())
            {
                if (beat.beatId == "ENDING_TRIGGER")
                {
                    found = true;
                    Assert.AreEqual(DeadAirTriggerCategory.Ending, beat.category);
                    Assert.IsTrue(beat.critical);
                }
            }

            Assert.IsTrue(found);
        }

        [Test]
        public void ChoiceCommitDoesNotOverwriteSiblingOutcome()
        {
            var go = new GameObject("Story Director");
            try
            {
                DeadAirStoryDirector director = go.AddComponent<DeadAirStoryDirector>();
                Assert.IsTrue(director.CommitChoice("CHOICE_01", DeadAirChoiceOutcome.TrustDispatch));
                Assert.IsFalse(director.CommitChoice("CHOICE_01", DeadAirChoiceOutcome.TrustGPS));
                Assert.IsTrue(director.TryGetChoice("CHOICE_01", out DeadAirChoiceOutcome outcome));
                Assert.AreEqual(DeadAirChoiceOutcome.TrustDispatch, outcome);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AutomaticDirectionPolicyMapsStoppedReverseToThrottleAndReverseSelector()
        {
            LwsAutomaticKeyboardDirectionDecision decision = LwsKeyboardGamepadTruckInputSource.ResolveAutomaticKeyboardDirection(
                false,
                true,
                LwsTransmissionMode.Automatic,
                LwsAutomaticTransmissionSelector.Drive,
                0f,
                0.35f);

            Assert.IsTrue(decision.handled);
            Assert.IsTrue(decision.requestSelectorChange);
            Assert.AreEqual(LwsAutomaticTransmissionSelector.Reverse, decision.requestedSelector);
            Assert.AreEqual(1f, decision.throttle);
            Assert.AreEqual(0f, decision.brake);
        }
    }
}
