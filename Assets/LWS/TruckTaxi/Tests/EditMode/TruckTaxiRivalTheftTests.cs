using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRivalTheft")]
    public sealed class TruckTaxiRivalTheftTests
    {
        private readonly List<Object> owned = new List<Object>();
        private readonly List<TruckTaxiRideLocation> stops = new List<TruckTaxiRideLocation>();
        private TruckTaxiConfiguration config;
        private TruckTaxiSession session;
        private PassengerProfile primary, rival;
        private bool hadPreference;
        private int preference;
        private T Keep<T>(T item) where T : Object { owned.Add(item); return item; }

        [SetUp]
        public void Setup()
        {
            hadPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            preference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            config = Keep(ScriptableObject.CreateInstance<TruckTaxiConfiguration>());
            primary = Keep(ScriptableObject.CreateInstance<PassengerProfile>()); primary.passengerId = "fare.primary";
            rival = Keep(ScriptableObject.CreateInstance<PassengerProfile>()); rival.passengerId = "fare.rival";
            config.passengers = new[] { primary }; config.minimumTripDistance = 10; config.maximumTripDistance = 1000;
            for (int i = 0; i < 4; i++)
            {
                var stop = Keep(new GameObject("stop " + i)).AddComponent<TruckTaxiRideLocation>();
                stop.locationId = "stop." + i; stop.transform.position = Vector3.right * i * 100;
                stops.Add(stop);
            }
            session = new TruckTaxiSession(config, stops, 17); session.StartShift();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var item in owned) if (item != null) Object.DestroyImmediate(item);
            owned.Clear(); stops.Clear();
            if (hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, preference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }

        private TruckTaxiRivalFare CreateFare()
        {
            Assert.IsTrue(session.TryCreateRivalFare("taxi.rival.0", rival, Vector3.zero, out var fare));
            return fare;
        }

        private void BoardPrimary()
        {
            Assert.IsTrue(session.OfferRide(primary)); Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true);
            session.Tick(config.boardingSeconds + .1f, session.Pickup.StopPosition, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
        }

        [Test]
        public void PrimaryArrivalWaitsForExtraDeliveryThenResumes()
        {
            BoardPrimary();
            var destination = session.Destination;
            var fare = CreateFare();
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            session.Tick(config.exitingSeconds + .1f, destination.StopPosition, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
            Assert.AreEqual(0, session.CompletedRides);
            Assert.IsTrue(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            session.Tick(.1f, destination.StopPosition, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.PassengerExiting, session.State);
            Assert.AreSame(destination, session.Destination);
            Assert.AreEqual(fare.PayoutCents, session.WalletBalanceCents);
        }

        [Test]
        public void SafeDropAndContinueShiftKeepExtraFareDeliverable()
        {
            BoardPrimary();
            var fare = CreateFare();
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsTrue(session.RequestSafeDrop("test emergency"));
            Vector3 stop = session.CurrentDesiredDestination.StopPosition;
            session.Tick(.1f, stop, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.PassengerExiting, session.State);
            session.Tick(config.exitingSeconds + .1f, stop, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.RideComplete, session.State);
            session.ContinueShift();
            Assert.AreEqual(TruckTaxiState.Available, session.State);
            Assert.AreSame(fare, session.StolenPassengerFare);
            Assert.IsTrue(session.OffersSuppressed);
            long balance = session.WalletBalanceCents;
            Assert.IsTrue(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            Assert.AreEqual(balance + fare.PayoutCents, session.WalletBalanceCents);
            Assert.IsFalse(session.OffersSuppressed);
        }

        [Test]
        public void TransferCallbacksCannotDeliverBeforeVisualCommit()
        {
            var fare = CreateFare();
            System.Action attempt = () => Assert.IsFalse(session.TryDeliverStolenPassenger(
                fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            session.RivalPassengerChanged += attempt;
            session.Changed += attempt;
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.AreSame(fare, session.StolenPassengerFare);
            Assert.AreEqual(0, session.WalletBalanceCents);
            Assert.IsTrue(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            Assert.AreEqual(fare.PayoutCents, session.WalletBalanceCents);
        }

        [Test]
        public void ReentrantCancelDuringTransferReportsFailureWithoutPayment()
        {
            var fare = CreateFare();
            session.RivalPassengerChanged += () =>
            {
                if (session.HasStolenPassenger) session.CancelStolenPassenger(fare.Id);
            };
            Assert.IsFalse(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsFalse(session.HasStolenPassenger);
            Assert.AreEqual(0, session.WalletBalanceCents);
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
        }

        [Test]
        public void EndShiftInvalidatesExtraFareWithoutDispatchCallbacksOrPayment()
        {
            var fare = CreateFare();
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            int demandCalls = 0;
            session.RegionalDemandMultiplier = _ => { demandCalls++; return 1; };
            session.RivalPassengerChanged += () => Assert.IsFalse(session.TryCreateRivalFare(
                fare.RivalId, rival, Vector3.zero, out _));
            session.EndShift();
            Assert.AreEqual(0, demandCalls);
            Assert.AreEqual(TruckTaxiState.Inactive, session.State);
            Assert.IsFalse(session.HasStolenPassenger);
            Assert.IsFalse(session.OffersSuppressed);
            Assert.AreEqual(0, session.WalletBalanceCents);
        }

        [Test]
        public void InvalidLoadPreservesExtraFareAndAcceptedLoadClearsItWithoutDispatch()
        {
            var clean = session.CapturePersistentState();
            var fare = CreateFare();
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsFalse(session.TryRestorePersistentState(null, out _));
            Assert.AreSame(fare, session.StolenPassengerFare);
            int demandCalls = 0;
            session.RegionalDemandMultiplier = _ => { demandCalls++; return 1; };
            Assert.IsTrue(session.TryRestorePersistentState(clean, out var reason), reason);
            Assert.AreEqual(0, demandCalls);
            Assert.IsFalse(session.HasStolenPassenger);
            Assert.IsFalse(session.OffersSuppressed);
            Assert.IsFalse(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            Assert.AreEqual(0, session.WalletBalanceCents);
        }

        [Test]
        public void TheftAndIndependentPaymentLeavePrimaryFareAndPatienceUntouched()
        {
            Assert.IsTrue(session.OfferRide(primary)); Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true);
            session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
            Assert.IsTrue(session.HasPassenger);
            session.AdjustOnboardPatience(-.2f);
            var destination = session.Destination; var id = session.CurrentRideId; var offer = session.Offer;
            float patience = session.OnboardPatience; long adjustment = session.MechanicFareAdjustment;
            var fare = CreateFare();
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsTrue(session.HasStolenPassenger, "Parent save guard must reject this state.");
            Assert.AreEqual("PASSENGER STOLEN\nNEW STOP ADDED", session.Reaction);
            Assert.IsFalse(session.ReleaseRivalFare(fare.RivalId, fare.Id));
            Assert.IsTrue(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            Assert.AreEqual(fare.PayoutCents, session.WalletBalanceCents);
            Assert.AreEqual(0, session.CompletedRides);
            Assert.AreEqual(0, session.RideHistory.Count);
            Assert.AreSame(primary, session.Passenger); Assert.AreSame(destination, session.Destination);
            Assert.AreEqual(id, session.CurrentRideId); Assert.AreSame(offer, session.Offer);
            Assert.AreEqual(patience, session.OnboardPatience); Assert.AreEqual(adjustment, session.MechanicFareAdjustment);
            Assert.IsNull(session.LastFare);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
        }

        [Test]
        public void ExactIdsCapacityAndRepeatedTransfersAreGuarded()
        {
            var fare = CreateFare();
            Assert.IsFalse(session.TryStealRivalPassenger(fare.RivalId, fare.Id, false));
            Assert.IsFalse(session.TryStealRivalPassenger("TAXI.RIVAL.0", fare.Id, true));
            Assert.IsFalse(session.TryStealRivalPassenger(fare.RivalId, fare.Id + ".stale", true));
            Assert.IsTrue(session.OwnsRivalFare(fare.RivalId, fare.Id));
            Assert.IsFalse(session.OffersSuppressed);
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsFalse(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsTrue(session.OffersSuppressed);
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId.ToUpperInvariant(), fare.DestinationPosition, 0));
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id + ".stale", fare.DestinationId, fare.DestinationPosition, 0));
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, Vector3.one * 9999, 0));
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, float.NaN));
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, config.stoppedSpeed + 1));
            Assert.AreEqual(0, session.WalletBalanceCents);
        }

        [Test]
        public void ReentrantCompletionPaysOnceAndReleasesOnlyItsSuppression()
        {
            var fare = CreateFare(); var otherOwner = new object();
            session.AcquireOfferSuppression(otherOwner);
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            int callbacks = 0;
            session.RivalPassengerChanged += () =>
            {
                callbacks++;
                Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            };
            Assert.IsTrue(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            Assert.AreEqual(1, callbacks); Assert.AreEqual(fare.PayoutCents, session.WalletBalanceCents);
            Assert.IsTrue(session.OffersSuppressed);
            session.ReleaseOfferSuppression(otherOwner); Assert.IsFalse(session.OffersSuppressed);
            Assert.IsFalse(session.HasStolenPassenger);
        }

        [Test]
        public void ResetAndCancelCannotPayOrResurrectStaleFares()
        {
            var fare = CreateFare();
            Assert.IsTrue(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            Assert.IsFalse(session.CancelStolenPassenger(fare.Id + ".stale"));
            session.ResetRivalPassengerTheft();
            Assert.IsFalse(session.HasStolenPassenger); Assert.IsFalse(session.OffersSuppressed);
            Assert.IsFalse(session.TryDeliverStolenPassenger(fare.Id, fare.DestinationId, fare.DestinationPosition, 0));
            Assert.IsFalse(session.TryStealRivalPassenger(fare.RivalId, fare.Id, true));
            var next = CreateFare(); Assert.AreNotEqual(fare.Id, next.Id);
            Assert.IsTrue(session.TryStealRivalPassenger(next.RivalId, next.Id, true));
            Assert.IsTrue(session.CancelStolenPassenger(next.Id));
            Assert.AreEqual(0, session.WalletBalanceCents);
        }

        [Test]
        public void DuplicatePassengerAssetsAndAmbiguousDestinationIdsAreRejected()
        {
            Assert.IsTrue(session.OfferRide(primary));
            var clone = Keep(ScriptableObject.CreateInstance<PassengerProfile>()); clone.passengerId = primary.passengerId;
            Assert.IsFalse(TruckTaxiRivalPopulation.IsIdentityEligible(clone, primary, null));
            Assert.IsFalse(session.TryCreateRivalFare("taxi.rival.0", clone, Vector3.zero, out _));
            foreach (var stop in stops) stop.locationId = "duplicate";
            Assert.IsFalse(session.TryCreateRivalFare("taxi.rival.0", rival, Vector3.zero, out _));
        }

        [TestCase(6, 6, 3, true)]
        [TestCase(20, 1, 10, false)]
        [TestCase(3, 3, 3, false)]
        [TestCase(20, 20, 0, false)]
        [TestCase(20, 20, -10, false)]
        public void StrongPlayerContactRequired(float relative, float normal, float approach, bool expected) =>
            Assert.AreEqual(expected, TruckTaxiRivalTheft.IsValidImpact(relative, normal, approach));

        [Test]
        public void NonfiniteImpactCannotTransfer() =>
            Assert.IsFalse(TruckTaxiRivalTheft.IsValidImpact(float.NaN, 10, 10));

        [Test]
        public void ExtraStopRestoresExactServiceDiversionAndFareLeases()
        {
            var stack = new TruckTaxiNavigationIntents<object>();
            var primaryLease = stack.Set(TruckTaxiNavigationLayer.RideFinal, "ride", "destination", new object());
            var diversion = stack.Set(TruckTaxiNavigationLayer.PassengerDiversion, "passenger", "bathroom", new object());
            var service = stack.Set(TruckTaxiNavigationLayer.PlayerService, "player-service", "gas", new object());
            var stolen = stack.Set(TruckTaxiNavigationLayer.RivalPassenger, "rival-theft.id", "stolen.destination", new object());
            var emergency = stack.Set(TruckTaxiNavigationLayer.Emergency, "recovery", "depot", new object());
            Assert.IsTrue(stack.Release(stolen.Layer, stolen.OwnerId, stolen.TargetId, stolen.Token));
            Assert.AreSame(emergency, stack.Active);
            Assert.IsTrue(stack.Release(emergency.Layer, emergency.OwnerId, emergency.TargetId, emergency.Token));
            Assert.AreSame(service, stack.Active);
            Assert.AreSame(primaryLease, stack.Get(TruckTaxiNavigationLayer.RideFinal));
            Assert.AreSame(diversion, stack.Get(TruckTaxiNavigationLayer.PassengerDiversion));
            var replacement = stack.Set(stolen.Layer, stolen.OwnerId, stolen.TargetId, new object());
            Assert.IsFalse(stack.Release(stolen.Layer, stolen.OwnerId, stolen.TargetId, stolen.Token));
            Assert.AreSame(replacement, stack.Active);
        }
    }
}
