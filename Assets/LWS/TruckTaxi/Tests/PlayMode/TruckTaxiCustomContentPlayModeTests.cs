using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiCustomContentPlayModeTests
    {
        private readonly List<Object> owned = new List<Object>();
        private TruckTaxiCustomContentRunner runner;
        private TruckTaxiDriverNeedsCoordinator needs;
        private bool hadPreference;
        private int oldPreference;
        private T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        private T Asset<T>() where T : ScriptableObject => Keep(ScriptableObject.CreateInstance<T>());
        private static void Set(object target, string name, object value) => target.GetType().GetProperty(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);

        [SetUp]
        public void Setup()
        {
            hadPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            oldPreference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            runner = Keep(new GameObject("Neutral content fixture")).AddComponent<TruckTaxiCustomContentRunner>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            runner?.Cancel();
            if (needs != null) Set(needs, "State", null);
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.Destroy(owned[i]);
            owned.Clear();
            if (hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, oldPreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeItemAcceptanceConsumesOnceCompletesEventOnlyGoalAndCleansUp()
        {
            var owner = Keep(new GameObject("Inactive isolated bootstrap")); owner.SetActive(false);
            var host = owner.AddComponent<TruckTaxiBootstrap>(); host.enabled = false;
            var truck = Keep(new GameObject("Fixture player")); truck.SetActive(false);
            Set(host, "Player", truck.AddComponent<LwsPlayerTruck>());
            var passenger = Asset<PassengerProfile>();
            passenger.passengerId = "fixture.commuter";
            passenger.basePatience = passenger.requestFrequency = 10000;
            passenger.requestDifficultyRange = Vector2.one;
            passenger.possibleRequests = Array.Empty<PassengerRequestDefinition>();
            var config = Asset<TruckTaxiConfiguration>();
            config.passengers = new[] { passenger }; config.minimumTripDistance = 10; config.maximumTripDistance = 1000;
            var stops = new List<TruckTaxiRideLocation>();
            for (int i = 0; i < 3; i++)
            {
                var point = Keep(new GameObject("Fixture stop")).AddComponent<TruckTaxiRideLocation>();
                point.locationId = "fixture." + i; point.transform.position = Vector3.right * i * 100; stops.Add(point);
            }
            var session = new TruckTaxiSession(config, stops, 31); Set(host, "Session", session);
            needs = owner.AddComponent<TruckTaxiDriverNeedsCoordinator>();
            var inventory = new TruckTaxiDriverNeedsState(Asset<TruckTaxiEnvironmentSettings>(), 31);
            Set(needs, "State", inventory); Set(host, "DriverNeeds", needs);
            var action = Asset<TruckTaxiCustomItemInteractionDefinition>();
            action.stableId = "fixture.water"; action.requiredItemStableId = "water-bottle";
            action.customEventId = "fixture.designer.event"; action.rewardCashCents = 125;
            action.presentationProfile = Asset<TruckTaxiCustomPresentationProfile>();
            action.presentationProfile.stableId = "fixture.empty-visual";
            action.presentationProfile.duration = .05f; action.presentationProfile.lockInput = false;
            passenger.customItemInteractions = new[] { action };
            inventory.AddItem(TruckTaxiNeedsItem.WaterBottle, 2);
            runner.Initialize(host);
            session.StartShift(); Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true);
            session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
            runner.RefreshObjectiveCapabilities();
            var goal = Asset<PassengerRequestDefinition>();
            goal.requestType = TaxiRequestType.CustomItemAction; goal.objectiveId = "fixture.objective";
            goal.requiredCustomEventId = action.customEventId; goal.requiredItemStableId = "";
            Assert.IsTrue(session.GenerateRequest(goal));
            Assert.IsTrue(runner.TryExecuteItem(action, out var reason), reason);
            Assert.AreEqual(1, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.AreEqual(125, session.WalletBalanceCents);
            Assert.AreEqual(TaxiRequestState.Succeeded, session.Requests[0].State);
            Assert.IsFalse(runner.TryExecuteItem(action, out _));
            float deadline = Time.realtimeSinceStartup + 3;
            while (runner.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(runner.IsPlaying);
            Assert.IsFalse(runner.TryExecuteItem(action, out _), "Once-per-ride guard remains after presentation cleanup.");
            Assert.AreEqual(1, inventory.Count(TruckTaxiNeedsItem.WaterBottle));
            Assert.Zero(runner.FadeAlpha);
            // The inactive coordinator was never clock-initialized; prevent its unrelated teardown from sampling it.
            Set(needs, "State", null);
        }

        [UnityTest]
        public IEnumerator RapidProfilesLatestWinsAndDisableStopsAllOwnedAudio()
        {
            var a = Asset<TruckTaxiCustomInteractionProfile>(); a.stableId = "fixture.first";
            var b = Asset<TruckTaxiCustomInteractionProfile>(); b.stableId = "fixture.latest";
            var phases = new List<string>(); runner.PhaseChanged += (id, phase) => phases.Add(id + ":" + phase);
            runner.Begin(a, null, null); runner.Begin(b, null, null);
            Assert.Contains("fixture.first:Cancelled", phases);
            Assert.Contains("fixture.latest:Started", phases);
            runner.enabled = false;
            Assert.Contains("fixture.latest:Cancelled", phases);
            Assert.IsFalse(runner.IsPlaying); Assert.Zero(runner.FadeAlpha);
            foreach (var source in runner.GetComponentsInChildren<AudioSource>()) Assert.IsFalse(source.isPlaying);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartedListenerCanCancelWithoutLeavingPlayback()
        {
            var value = Asset<TruckTaxiCustomInteractionProfile>(); value.stableId = "fixture.cancel-at-start";
            runner.interactionStarted.AddListener(_ => runner.Cancel());
            runner.Begin(value, null, null);
            Assert.IsFalse(runner.IsPlaying);
            Assert.Zero(runner.FadeAlpha);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancellationListenerCanStartLatestProfileWithoutRecursiveCancellation()
        {
            var first = Asset<TruckTaxiCustomInteractionProfile>(); first.stableId = "fixture.first";
            var requested = Asset<TruckTaxiCustomInteractionProfile>(); requested.stableId = "fixture.requested";
            var latest = Asset<TruckTaxiCustomInteractionProfile>(); latest.stableId = "fixture.latest";
            var started = new List<string>(); runner.interactionStarted.AddListener(started.Add);
            runner.Begin(first, null, null);
            runner.interactionCancelled.AddListener(id => { if (id == first.stableId) runner.Begin(latest, null, null); });
            runner.Begin(requested, null, null);
            CollectionAssert.AreEqual(new[] { first.stableId, latest.stableId }, started);
            Assert.IsTrue(runner.IsPlaying);
            runner.Cancel(); Assert.IsFalse(runner.IsPlaying);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThrowingCancellationListenerCannotPreventCleanup()
        {
            var value = Asset<TruckTaxiCustomInteractionProfile>(); value.stableId = "fixture.throw";
            runner.Begin(value, null, null);
            runner.interactionCancelled.AddListener(_ => throw new InvalidOperationException("fixture cancellation observer"));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: fixture cancellation observer"));
            runner.Cancel();
            Assert.IsFalse(runner.IsPlaying); Assert.Zero(runner.FadeAlpha);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledRunnerDoesNotBeginOrPreview()
        {
            var value = Asset<TruckTaxiCustomInteractionProfile>(); value.stableId = "fixture.disabled";
            runner.enabled = false;
            runner.Begin(value, null, null); runner.Preview(null, null);
            Assert.IsFalse(runner.IsPlaying);
            Assert.IsFalse(runner.TryExecuteItem(null, out var reason));
            StringAssert.Contains("disabled", reason);
            Assert.IsFalse(runner.OwnsInputHold);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvalidDurationPreviewFallsBackAndFinishes()
        {
            var visual = Asset<TruckTaxiCustomPresentationProfile>(); visual.stableId = "fixture.invalid";
            visual.duration = float.NaN;
            LogAssert.Expect(LogType.Warning, "Custom presentation skipped: Duration must be positive.");
            runner.Preview(visual, null);
            float deadline = Time.realtimeSinceStartup + 5;
            while (runner.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(runner.IsPlaying); Assert.Zero(runner.FadeAlpha);
        }
    }
}
