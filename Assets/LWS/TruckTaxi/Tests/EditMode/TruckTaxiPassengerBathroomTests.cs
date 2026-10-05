using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPassengerBathroomTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private static TruckTaxiPassengerBathroomPolicy Policy() => new TruckTaxiPassengerBathroomPolicy {
            rideChance = 1, initialBladder = .7f, gainPerGameHour = .2f, minimumRideMinutes = 45
        };
        private static TruckTaxiPassengerBathroomState Ready()
        {
            var state = new TruckTaxiPassengerBathroomState(Policy(), 0, false, 0);
            state.ObserveClock(3600, true);
            return state;
        }

        [TearDown] public void TearDown()
        {
            foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            objects.Clear();
        }
        [Test] public void NeedUsesGameMinutesAndNeverOffersDuringAShortRide()
        {
            var state = new TruckTaxiPassengerBathroomState(Policy(), 0, false, 0);
            state.ObserveClock(600, true);
            Assert.That(state.RideMinutes, Is.EqualTo(10));
            Assert.That(state.CanOffer, Is.False);
            state.ObserveClock(2700, true);
            Assert.That(state.CanOffer, Is.True);
        }
        [Test] public void PausedAndReliefClockJumpsDoNotLeakIntoNeed()
        {
            var state = Ready();
            double bladder = state.Bladder, minutes = state.RideMinutes;
            state.ObserveClock(7200, false);
            Assert.That(state.Bladder, Is.EqualTo(bladder));
            state.Offer(); state.Accept(); state.BeginPresentation();
            state.ObserveClock(7380, true);
            Assert.That(state.RideMinutes, Is.EqualTo(minutes));
            Assert.That(state.CompleteAfterReboard(), Is.True);
            state.ObserveClock(7440, true);
            Assert.That(state.RideMinutes, Is.EqualTo(minutes + 1));
        }
        [Test] public void IntercityAuthoringRaisesNeedFasterAndChanceCanDisableIt()
        {
            var policy = Policy();
            var local = new TruckTaxiPassengerBathroomState(policy, 0, false, 0);
            var intercity = new TruckTaxiPassengerBathroomState(policy, 0, true, 0);
            local.ObserveClock(1800, true); intercity.ObserveClock(1800, true);
            Assert.That(intercity.Bladder, Is.GreaterThan(local.Bladder));
            policy.rideChance = 0;
            var disabled = new TruckTaxiPassengerBathroomState(policy, 0, true, 0);
            disabled.ObserveClock(86400, true);
            Assert.That(disabled.CanOffer, Is.False);
        }
        [Test] public void DeclineIsOneShotNonfatalAndDoesNotResetBladder()
        {
            var state = Ready();
            double bladder = state.Bladder;
            Assert.That(state.Offer(), Is.True);
            Assert.That(state.Decline(), Is.True);
            Assert.That(state.Decline(), Is.False);
            Assert.That(state.Bladder, Is.EqualTo(bladder));
            state.ObserveClock(86400, true);
            Assert.That(state.CanOffer, Is.False);
            Assert.That(TruckTaxiPassengerBathroomState.DeclinePatienceLoss(.1f), Is.Zero);
            Assert.That(.16f - TruckTaxiPassengerBathroomState.DeclinePatienceLoss(.16f), Is.EqualTo(.15f).Within(.0001));
        }
        [Test] public void ReliefRequiresAcceptedPresentationAndReboardAcknowledgement()
        {
            var state = Ready();
            Assert.That(state.CompleteAfterReboard(), Is.False);
            Assert.That(state.BeginPresentation(), Is.False);
            state.Offer(); state.Accept();
            Assert.That(state.CompleteAfterReboard(), Is.False);
            state.BeginPresentation();
            Assert.That(state.Bladder, Is.GreaterThan(0));
            Assert.That(state.CompleteAfterReboard(), Is.True);
            Assert.That(state.Bladder, Is.Zero);
            Assert.That(state.CompleteAfterReboard(), Is.False);
        }
        [Test] public void WithdrawPreservesNeedAndDoesNotConsumeRequestQuota()
        {
            var state = Ready(); state.Offer(); state.Accept();
            double bladder = state.Bladder;
            state.Withdraw();
            Assert.That(state.CanOffer, Is.False);
            Assert.That(state.ResolvedRequests, Is.Zero);
            Assert.That(state.Bladder, Is.EqualTo(bladder));
            state.ObserveClock(3900, true);
            Assert.That(state.CanOffer, Is.True);
        }
        [Test] public void MalformedPolicyAndClockRemainFiniteAndBounded()
        {
            var policy = Policy(); policy.reliefMinutes = float.NaN; policy.minimumRideMinutes = -1;
            policy.gainPerGameHour = float.PositiveInfinity;
            var state = new TruckTaxiPassengerBathroomState(policy, 10, true, 0);
            state.ObserveClock(double.NaN, true); state.ObserveClock(-10, true);
            Assert.That(state.RideMinutes, Is.Zero);
            state.ObserveClock(1000000, true);
            Assert.That(state.Bladder, Is.InRange(0d, 1d));
            Assert.That(state.ReliefMinutes, Is.InRange(1, 10));
            Assert.That(policy.minimumRideMinutes, Is.EqualTo(-1), "Normalization must not mutate authored data.");
        }
        [Test] public void RestroomMustBeRealEnabledAndCapabilityMatching()
        {
            var shop = Service("shop", new Vector3(100, 0, 0), TruckTaxiServiceCapability.Store);
            Assert.That(TruckTaxiPassengerBathroomNeed.IsRestroom(shop), Is.False);
            shop.capabilities |= TruckTaxiServiceCapability.Restroom;
            Assert.That(TruckTaxiPassengerBathroomNeed.IsRestroom(shop), Is.True);
            shop.enabled = false;
            Assert.That(TruckTaxiPassengerBathroomNeed.IsRestroom(shop), Is.False);
        }
        [Test] public void NearestMeansReachableRoadDistanceNotDecorativeProximity()
        {
            var unreachable = Service("closed-road", new Vector3(40, 0, 0));
            var detour = Service("long-road", new Vector3(80, 0, 0));
            var nearest = Service("nearest-road", new Vector3(120, 0, 0));
            var result = TruckTaxiPassengerBathroomNeed.FindNearestRestroom(new[] { unreachable, detour, nearest }, Vector3.zero,
                (from, to) => new TruckTaxiRouteLeg(to.x == 80 ? 900 : 200, "test", new[] { from, to }, to.x != 40));
            Assert.That(result, Is.SameAs(nearest));
            Assert.That(TruckTaxiPassengerBathroomNeed.FindNearestRestroom(new[] { nearest }, Vector3.zero, null), Is.Null);
        }
        [Test] public void ParkingUsesActualBayAnchorRadiusHeightAndStoppedSpeed()
        {
            var service = Service("restroom", new Vector3(999, 0, 999));
            var location = NewObject("authored bay").AddComponent<TruckTaxiRideLocation>();
            location.transform.position = new Vector3(10, 0, 10);
            service.location = location; service.radius = 3; service.verticalTolerance = 2;
            Assert.That(service.CanUse(service.Position, 0, .44704f), Is.True);
            Assert.That(service.CanUse(service.transform.position, 0, .44704f), Is.False);
            Assert.That(service.CanUse(service.Position + Vector3.up * 3, 0, .44704f), Is.False);
            Assert.That(service.CanUse(service.Position, 1, .44704f), Is.False);
        }
        [Test] public void PassengerBathroomLeaseRestoresFareAndCannotEraseHigherOrReplacementOwners()
        {
            var intents = new TruckTaxiNavigationIntents<string>();
            intents.Set(TruckTaxiNavigationLayer.RideFinal, "ride", "fare", "fare");
            var lease = intents.Set(TruckTaxiNavigationLayer.PassengerDiversion, TruckTaxiPassengerBathroomNeed.NavigationOwner, "restroom", "restroom");
            var service = intents.Set(TruckTaxiNavigationLayer.PlayerService, "player-service", "food", "food");
            Assert.That(intents.Active.TargetId, Is.EqualTo("food"));
            intents.Release(service.Layer, service.OwnerId, service.TargetId, service.Token);
            Assert.That(intents.Active.TargetId, Is.EqualTo("restroom"));
            intents.Release(lease.Layer, lease.OwnerId, lease.TargetId, lease.Token);
            Assert.That(intents.Active.TargetId, Is.EqualTo("fare"));
            intents.Set(lease.Layer, "passenger", "scenic", "scenic");
            Assert.That(intents.Release(lease.Layer, lease.OwnerId, lease.TargetId, lease.Token), Is.False);
            Assert.That(intents.Active.TargetId, Is.EqualTo("scenic"));
        }
        private GameObject NewObject(string name)
        {
            var go = new GameObject(name); objects.Add(go); return go;
        }
        private TruckTaxiServicePoint Service(string id, Vector3 position, TruckTaxiServiceCapability capability = TruckTaxiServiceCapability.Restroom)
        {
            var point = NewObject(id).AddComponent<TruckTaxiServicePoint>();
            point.stableId = id; point.capabilities = capability; point.radius = 3; point.transform.position = position;
            return point;
        }
    }
}
