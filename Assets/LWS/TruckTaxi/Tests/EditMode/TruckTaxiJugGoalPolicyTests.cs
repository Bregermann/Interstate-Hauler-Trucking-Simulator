using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiJugGoal")]
    public sealed class TruckTaxiJugGoalPolicyTests
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private TruckTaxiEnvironmentSettings settings;
        private TruckTaxiDriverNeedsState needs;
        private TruckTaxiConfiguration config;
        private PassengerProfile passenger;
        private PassengerRequestDefinition definition;
        private TruckTaxiRideLocation[] stops;
        private Vector3 position;
        private bool hadPreference;
        private int preference;

        [SetUp] public void SetUp()
        {
            hadPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            preference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            settings = Asset<TruckTaxiEnvironmentSettings>();
            settings.bladderCycleGameHours = new Vector2(4, 8); settings.jugDurationSeconds = 8;
            needs = new TruckTaxiDriverNeedsState(settings, 41); needs.SetPressure(.5f);
            config = Asset<TruckTaxiConfiguration>(); config.minimumTripDistance = 10; config.maximumTripDistance = 20000;
            config.intercityRideChance = 0; config.boardingSeconds = .01f;
            passenger = Asset<PassengerProfile>(); passenger.passengerId = "jug.goal.fixture";
            passenger.basePatience = passenger.requestFrequency = 10000;
            passenger.requestDifficultyRange = new Vector2(3, 3);
            definition = Asset<PassengerRequestDefinition>(); definition.requestType = TaxiRequestType.FillJug;
            definition.objectiveId = "fixture.ordinary-jug"; definition.target = 99;
            passenger.possibleRequests = Array.Empty<PassengerRequestDefinition>(); config.passengers = new[] { passenger };
            stops = new TruckTaxiRideLocation[2];
            for (int i = 0; i < stops.Length; i++)
            {
                var go = new GameObject("Jug feasibility stop " + i); owned.Add(go);
                stops[i] = go.AddComponent<TruckTaxiRideLocation>(); stops[i].locationId = "jug.stop." + i;
                go.transform.position = Vector3.right * i * 12000;
            }
        }
        [TearDown] public void TearDown()
        {
            foreach (var item in owned) UnityEngine.Object.DestroyImmediate(item); owned.Clear();
            if (hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, preference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }
        private T Asset<T>() where T : ScriptableObject
        { var result = ScriptableObject.CreateInstance<T>(); owned.Add(result); return result; }
        private static TruckTaxiJugGoalContext Context() => new TruckTaxiJugGoalContext
        {
            RuntimeAvailable = true, SafeCompletionAvailable = true, UsableContainers = 2,
            Pressure = .5f, CurrentCycleGameSeconds = 14400, MinimumCycleGameSeconds = 14400, MaximumCycleGameSeconds = 28800,
            CrisisGraceGameSeconds = 1200, JugDurationSeconds = 8, GameSecondsPerRealSecond = 60, BladderRateMultiplier = 1
        };
        private TruckTaxiSession Board(int seed = 17)
        {
            position = Vector3.zero;
            var session = new TruckTaxiSession(config, stops, seed, playerPosition: () => position);
            session.StartShift(); Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
            position = session.Pickup.StopPosition;
            session.Tick(.1f, position, 0, 0, true); session.Tick(.1f, position, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
            session.Capabilities.Register(TruckTaxiObjectiveCapabilities.Required(TaxiRequestType.FillJug), 1);
            session.JugGoalContextProvider = () => TruckTaxiJugGoalPolicy.Capture(needs, settings, 60, true, true);
            return session;
        }

        [TestCase("runtime")]
        [TestCase("unsafe")]
        [TestCase("active")]
        [TestCase("container")]
        [TestCase("pressure-nan")]
        [TestCase("pressure-range")]
        [TestCase("cycle-zero")]
        [TestCase("cycle-infinity")]
        [TestCase("duration-nan")]
        [TestCase("clock-infinity")]
        [TestCase("clock-negative")]
        [TestCase("rate-zero")]
        [TestCase("crisis-exhausted")]
        [TestCase("short-budget")]
        [TestCase("deadline")]
        [TestCase("ride-nan")]
        public void MissingUnsafeOrInvalidEvidenceRejectsAssignment(string defect)
        {
            var context = Context(); float ride = 600, deadline = float.MaxValue;
            switch (defect)
            {
                case "runtime": context.RuntimeAvailable = false; break;
                case "unsafe": context.SafeCompletionAvailable = false; break;
                case "active": context.JugActive = true; break;
                case "container": context.UsableContainers = 0; break;
                case "pressure-nan": context.Pressure = float.NaN; break;
                case "pressure-range": context.Pressure = 2; break;
                case "cycle-zero": context.CurrentCycleGameSeconds = 0; break;
                case "cycle-infinity": context.MaximumCycleGameSeconds = float.PositiveInfinity; break;
                case "duration-nan": context.JugDurationSeconds = float.NaN; break;
                case "clock-infinity": context.GameSecondsPerRealSecond = float.PositiveInfinity; break;
                case "clock-negative": context.GameSecondsPerRealSecond = -1; break;
                case "rate-zero": context.BladderRateMultiplier = 0; break;
                case "crisis-exhausted": context.CrisisGameSeconds = context.CrisisGraceGameSeconds; break;
                case "short-budget": ride = 27; break;
                case "deadline": deadline = 27; break;
                case "ride-nan": ride = float.NaN; break;
            }
            Assert.Zero(TruckTaxiJugGoalPolicy.Capacity(context, ride, deadline, out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test] public void OrdinaryOneAndRareTwoRespectTimeInventoryAndPostReliefCycle()
        {
            var context = Context();
            Assert.AreEqual(2, TruckTaxiJugGoalPolicy.Capacity(context, 600, float.MaxValue, out _));
            Assert.AreEqual(1, TruckTaxiJugGoalPolicy.Capacity(context, 299, float.MaxValue, out _));
            Assert.AreEqual(1, TruckTaxiJugGoalPolicy.Capacity(context, 600, 150, out _), "Second refill uses the maximum NEW cycle, not the old sample.");
            context.UsableContainers = 1;
            Assert.AreEqual(1, TruckTaxiJugGoalPolicy.Capacity(context, 600, float.MaxValue, out _));
        }

        [Test] public void EmptyBladderMustHaveNaturalRefillTimeWithoutTemporaryBoostAssumptions()
        {
            var context = Context(); context.Pressure = 0;
            Assert.Zero(TruckTaxiJugGoalPolicy.Capacity(context, 60, float.MaxValue, out _));
            Assert.AreEqual(1, TruckTaxiJugGoalPolicy.Capacity(context, 100, float.MaxValue, out _));
            context.BladderRateMultiplier = 2;
            Assert.Zero(TruckTaxiJugGoalPolicy.Capacity(context, 70, float.MaxValue, out _));
        }

        [Test] public void FrozenClockAllowsAnAlreadyReadySingleQteButNotARefill()
        {
            var context = Context(); context.GameSecondsPerRealSecond = 0;
            Assert.AreEqual(1, TruckTaxiJugGoalPolicy.Capacity(context, 600, float.MaxValue, out _));
            context.Pressure = 0;
            Assert.Zero(TruckTaxiJugGoalPolicy.Capacity(context, 600, float.MaxValue, out _));
        }

        [Test] public void ImminentAccidentRejectsAnOtherwiseLongRide()
        {
            var context = Context(); context.Pressure = .99f;
            Assert.Zero(TruckTaxiJugGoalPolicy.Capacity(context, 600, float.MaxValue, out _));
        }

        [Test] public void TwoIsRareAndNeverSelectedWithoutExplicitAllowance()
        {
            int twos = 0;
            for (int i = 0; i < 1000; i++)
            {
                float roll = (i + .5f) / 1000;
                twos += TruckTaxiJugGoalPolicy.SelectCount(2, true, roll) == 2 ? 1 : 0;
                Assert.AreEqual(1, TruckTaxiJugGoalPolicy.SelectCount(2, false, roll));
                Assert.AreEqual(1, TruckTaxiJugGoalPolicy.SelectCount(1, true, roll));
            }
            Assert.AreEqual(100, twos); Assert.Zero(TruckTaxiJugGoalPolicy.SelectCount(0, true, 0));
            Assert.AreEqual(1, TruckTaxiJugGoalPolicy.SelectCount(2, true, float.NaN));
        }

        [Test] public void CaptureCountsMatchingOutputSpaceAndDoesNotMutateNeeds()
        {
            Assert.IsTrue(needs.AddItem(TruckTaxiNeedsItem.FilledJug, 99));
            var before = needs.CapturePersistentState();
            var context = TruckTaxiJugGoalPolicy.Capture(needs, settings, 60, true, true);
            Assert.Zero(context.UsableContainers); Assert.IsFalse(needs.CanStartJug);
            Assert.IsTrue(needs.AddItem(TruckTaxiNeedsItem.EmptyBottle, 2));
            context = TruckTaxiJugGoalPolicy.Capture(needs, settings, 60, true, true);
            Assert.AreEqual(2, context.UsableContainers);
            Assert.AreEqual(before.Pressure, needs.Pressure); Assert.AreEqual(1, needs.Count(TruckTaxiNeedsItem.EmptyPissJug));
            Assert.IsFalse(needs.JugActive); Assert.Zero(needs.SuccessfulJugs);
        }

        [Test] public void SessionRejectsMissingProviderUnsafeContextAndInsufficientDeadline()
        {
            var session = Board(); session.JugGoalContextProvider = null;
            Assert.IsFalse(session.CanAssignJugGoal(definition, out _)); Assert.IsFalse(session.GenerateRequest(definition));
            var context = Context(); context.SafeCompletionAvailable = false;
            session.JugGoalContextProvider = () => context;
            Assert.IsFalse(session.GenerateRequest(definition));
            context.SafeCompletionAvailable = true; definition.hasDeadline = true; definition.timer = 27;
            Assert.IsFalse(session.GenerateRequest(definition)); Assert.IsEmpty(session.Requests);
        }

        [Test] public void SessionRechecksFeasibilityBeforeConstructingProgress()
        {
            var session = Board(); int samples = 0;
            session.JugGoalContextProvider = () => ++samples == 1 ? Context() : default;
            Assert.IsFalse(session.GenerateRequest(definition)); Assert.IsEmpty(session.Requests);
        }

        [Test] public void CommonGoalCapsAtOneDespiteHighAuthoredTargetAndDifficulty()
        {
            var session = Board(); Assert.IsTrue(session.GenerateRequest(definition));
            Assert.AreEqual(1, session.Requests[0].Target);
        }

        [Test] public void AuthoredChaoticGoalNeverExceedsTwoAndAnExplicitMaximumOneWins()
        {
            passenger.chaosAffinity = 1; definition.countBand = TaxiRequestCountBand.Chaotic;
            needs.AddItem(TruckTaxiNeedsItem.EmptyPissJug);
            bool sawTwo = false;
            for (int seed = 1; seed <= 100; seed++)
            {
                var session = Board(seed); Assert.IsTrue(session.GenerateRequest(definition));
                Assert.That(session.Requests[0].Target, Is.InRange(1, 2));
                sawTwo |= session.Requests[0].Target == 2;
            }
            Assert.IsTrue(sawTwo);
            definition.maximumCount = 1;
            for (int seed = 1; seed <= 20; seed++)
            { var session = Board(seed); Assert.IsTrue(session.GenerateRequest(definition)); Assert.AreEqual(1, session.Requests[0].Target); }
        }

        [Test] public void OnlySuccessfulExistingQteAdvancesGoalAndCannotPayTwice()
        {
            var session = Board();
            session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.JugSucceeded);
            Assert.IsTrue(session.GenerateRequest(definition)); var goal = session.Requests[0];
            needs.Event += kind => { session.RecordNeedsEvent(kind); session.RefreshJugGoalProgress(); };
            session.RefreshJugGoalProgress(); Assert.Zero(goal.Progress, "Old successes cannot retroactively satisfy a new goal.");
            Assert.IsTrue(needs.StartJug()); needs.CancelJug(); Assert.Zero(goal.Progress);
            Assert.IsTrue(needs.StartJug()); needs.TickJug(1, false, false, false, 0); Assert.Zero(goal.Progress);
            needs.SetPressure(.5f); Assert.IsTrue(needs.StartJug());
            for (int i = 0; i < 300 && needs.JugActive; i++)
                needs.TickJug(.05f, true, needs.CueIsOpen(false), false, 0);
            Assert.IsFalse(needs.JugActive); Assert.AreEqual(1, needs.SuccessfulJugs);
            Assert.AreEqual(TaxiRequestState.Succeeded, goal.State); Assert.AreEqual(1, goal.Progress);
            Assert.AreEqual(1, needs.Count(TruckTaxiNeedsItem.FilledJug));
            float satisfaction = session.Satisfaction; long reward = session.EstimateFare().Requests;
            session.RefreshJugGoalProgress(); Assert.AreEqual(reward, session.EstimateFare().Requests);
            Assert.AreEqual(satisfaction, session.Satisfaction);
            Assert.IsFalse(session.GenerateRequest(definition), "A second jug goal must not bypass the ride's count cap.");
        }

        [Test] public void CustomItemTargetLimitRemainsIndependent()
        {
            var session = Board();
            session.JugGoalContextProvider = () => throw new InvalidOperationException("Unrelated custom objective queried jug state.");
            var custom = Asset<PassengerRequestDefinition>(); custom.requestType = TaxiRequestType.CustomItemAction;
            custom.objectiveId = "fixture.custom.goal"; custom.requiredCustomEventId = "fixture.event";
            custom.requiredItemStableId = "water-bottle"; custom.target = 10;
            var action = Asset<TruckTaxiCustomItemInteractionDefinition>(); action.stableId = "fixture.action";
            action.customEventId = custom.requiredCustomEventId; action.requiredItemStableId = custom.requiredItemStableId;
            passenger.customItemInteractions = new[] { action };
            session.Capabilities.RegisterCustomItemAction(custom.requiredCustomEventId, custom.requiredItemStableId);
            session.CustomObjectiveTargetLimit = _ => 1;
            Assert.IsTrue(session.GenerateRequest(custom)); Assert.AreEqual(1, session.Requests[0].Target);
        }
    }
}
