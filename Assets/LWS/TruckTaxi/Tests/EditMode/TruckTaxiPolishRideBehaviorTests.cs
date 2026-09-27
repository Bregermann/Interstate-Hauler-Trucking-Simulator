using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Tests.EditMode
{
    [Category("TaxiPolish")]
    public sealed class TruckTaxiPolishRideBehaviorTests
    {
        private readonly List<Object> objects = new List<Object>();
        private TruckTaxiConfiguration config;
        private PassengerProfile passenger;
        private TruckTaxiSession session;
        private readonly List<TruckTaxiRideLocation> locations = new List<TruckTaxiRideLocation>();
        private bool hadRideRequestsPreference;
        private int previousRideRequestsPreference;

        [SetUp]
        public void SetUp()
        {
            locations.Clear();
            hadRideRequestsPreference = PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            previousRideRequestsPreference = PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, 1);
            config = ScriptableObject.CreateInstance<TruckTaxiConfiguration>();
            objects.Add(config);
            passenger = ScriptableObject.CreateInstance<PassengerProfile>();
            objects.Add(passenger);
            passenger.passengerId = "taxi-polish-test-passenger";
            passenger.passengerName = "Test commuter";
            passenger.basePatience = 1000;
            passenger.requestDifficultyRange = Vector2.one;
            config.passengers = new[] { passenger };
            config.minimumTripDistance = 10;
            config.maximumTripDistance = 1000;

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Polish test stop " + i);
                objects.Add(go);
                var stop = go.AddComponent<TruckTaxiRideLocation>();
                stop.locationId = "polish.stop." + i;
                stop.locationName = "Polish stop " + i;
                go.transform.position = Vector3.right * i * 100;
                locations.Add(stop);
            }
            session = new TruckTaxiSession(config, locations, 71);
            foreach (TruckTaxiObjectiveCapability capability in System.Enum.GetValues(typeof(TruckTaxiObjectiveCapability)))
                session.Capabilities.Register(capability, 10);
            foreach (TruckTaxiStopCategory category in new[] { TruckTaxiStopCategory.Scenic, TruckTaxiStopCategory.IllicitPickup })
            {
                var go = new GameObject("Polish optional stop " + category);
                objects.Add(go);
                var stop = go.AddComponent<TruckTaxiStopObjectivePoint>();
                stop.stableId = "polish." + category;
                stop.category = category;
                stop.durationSeconds = 8;
                session.Capabilities.Stops.Add(stop);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in objects) Object.DestroyImmediate(item);
            objects.Clear();
            locations.Clear();
            if (hadRideRequestsPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey, previousRideRequestsPreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void OfferGenerationIsIncrementalBoundedAndAllowsOnlyOnePendingJob()
        {
            session.StartShift();
            Assert.IsTrue(session.RequestRideOffer(passenger));
            Assert.IsTrue(session.IsGeneratingOffer);
            Assert.IsFalse(session.RequestRideOffer(passenger));
            Assert.IsTrue(session.IsGeneratingOffer, "A repeated request must not replace or cancel the pending generator.");

            for (int frame = 0; frame < 40 && session.IsGeneratingOffer; frame++)
                session.Tick(.016f, Vector3.zero, 0, 0, true);

            Assert.IsFalse(session.IsGeneratingOffer, session.LastOfferDiagnostics);
            Assert.AreEqual(TruckTaxiState.RideOffered, session.State);
            Assert.LessOrEqual(session.LastOfferRouteQueries,
                config.offerPickupCandidateLimit + config.offerDestinationCandidateLimit);
            Assert.Less(session.LastOfferMaximumStepMs, 100, "A single offer stage should not block for a long frame.");
        }

        [Test]
        public void SustainedGoodProgressDoesNotTimeOutALongOnboardRide()
        {
            passenger.basePatience = 60;
            Board();
            Vector3 origin = session.Pickup.StopPosition;
            session.Destination.transform.position = origin + Vector3.right * 10000;
            float startPatience = session.OnboardPatience;

            for (int step = 1; step <= 120; step++)
                session.Tick(5, origin + Vector3.right * (step * 25), 5, 0, true);

            Assert.AreEqual(600, session.ElapsedRide, .01f);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
            Assert.IsFalse(session.SafeDropRequested);
            Assert.GreaterOrEqual(session.OnboardPatience, startPatience);
        }

        [Test]
        public void SustainedWrongWayTravelRequestsSafeDrop()
        {
            passenger.basePatience = 60;
            config.patienceProgressSampleSeconds = 1;
            config.wrongWayGraceSeconds = 0;
            config.unexplainedStopGraceSeconds = 1000;
            config.patienceRecoveryPerSecond = 0;
            Board();
            Vector3 origin = session.Pickup.StopPosition;

            for (int second = 1; second <= 100 && !session.SafeDropRequested; second++)
                session.Tick(1, origin - Vector3.right * (second * 12), 12, 0, true);

            Assert.IsTrue(session.SafeDropRequested);
            Assert.IsNotNull(session.CurrentDesiredDestination);
            Assert.AreSame(session.SafeDropDestination, session.CurrentDesiredDestination);
        }

        [Test]
        public void IntercityFareRequiresThreeLocalOffersBeforeAnotherIntercityOffer()
        {
            passenger.passengerId = string.Empty;
            config.intercityRideChance = 1;
            config.localOffersAfterIntercity = 3;
            session.RegionResolver = position => position.x >= 200 ? "town.02" : "town.01";
            session.StartShift();

            Assert.IsTrue(session.OfferRide(passenger));
            Assert.IsTrue(session.IsIntercityRide);
            Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true);
            session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
            session.DebugComplete();
            Assert.AreEqual(TruckTaxiState.RideComplete, session.State);
            Assert.AreEqual(3, session.LocalOffersBeforeIntercity);

            for (int remaining = 2; remaining >= 0; remaining--)
            {
                session.ContinueShift();
                Assert.IsTrue(session.OfferRide(passenger));
                Assert.IsFalse(session.IsIntercityRide, "Cooldown should force a local destination.");
                Assert.AreEqual(remaining, session.LocalOffersBeforeIntercity);
                if (remaining > 0) session.DeclineRide();
            }

            session.DeclineRide();
            Assert.IsTrue(session.OfferRide(passenger));
            Assert.IsTrue(session.IsIntercityRide, "Intercity offers become eligible after three local offers.");
        }

        [Test]
        public void LocalOnlyOfferDoesNotFallBackToAnIntercityDestinationWhenNoLocalDropoffExists()
        {
            passenger.passengerId = string.Empty;
            config.intercityRideChance = 0;
            session.RegionResolver = position => position.x >= 300 ? "town.02" : "town.01";
            foreach (TruckTaxiRideLocation location in locations)
            {
                location.pickupAllowed = location == locations[0];
                location.dropoffAllowed = location == locations[3];
            }
            session.StartShift();

            Assert.IsFalse(session.OfferRide(passenger));
            Assert.AreEqual(TruckTaxiState.Available, session.State);
            Assert.IsNull(session.Offer);
            Assert.IsFalse(session.IsGeneratingOffer);
        }

        [Test]
        public void PickupCountdownRunsWhileApproachingAndOnlyFreezesWhenExplicitlySuspended()
        {
            session.StartShift();
            Assert.IsTrue(session.OfferRide());
            Assert.IsTrue(session.AcceptRide());
            Vector3 away = session.Pickup.StopPosition + Vector3.left * 20;

            session.Tick(8, away, 2, 0, true);
            Assert.AreEqual(8, session.PickupElapsed, .01f);
            Assert.AreEqual(session.PickupDuration - 8, session.PickupRemaining, .01f);

            var owner = new object();
            session.SetPickupPatienceSuspended(owner, true);
            session.Tick(30, away, 2, 0, true);
            Assert.AreEqual(8, session.PickupElapsed, .01f);
            Assert.AreEqual(TruckTaxiState.DrivingToPickup, session.State);

            session.SetPickupPatienceSuspended(owner, false);
            session.Tick(3, away, 2, 0, true);
            Assert.AreEqual(11, session.PickupElapsed, .01f);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OptionalStopHasNoRewardOrPenaltyWhilePendingAndHonorsChoice(bool accept)
        {
            Board();
            var diversion = NewRequest(TaxiRequestType.ScenicRoute, 900);
            int created = 0;
            session.RequestCreated += _ => created++;
            Assert.IsTrue(session.GenerateRequest(diversion));
            Assert.IsNotNull(session.PendingDiversion);
            Assert.IsEmpty(session.Requests, "An offered stop is not an active ride request before acceptance.");
            Assert.AreEqual(0, created);
            Assert.AreEqual(0, session.ShiftEarnings);
            Assert.AreEqual(0, session.EstimateFare().Diversions);
            Assert.AreEqual(0, session.EstimateFare().Penalties);

            if (accept)
            {
                Assert.IsTrue(session.AcceptDiversion());
                Assert.IsNull(session.PendingDiversion);
                Assert.AreEqual(1, session.Requests.Count);
                Assert.AreEqual(TaxiRequestState.Active, session.Requests[0].State);
                Assert.AreEqual(1, created);
            }
            else
            {
                Assert.IsTrue(session.DeclineDiversion());
                Assert.IsNull(session.PendingDiversion);
                Assert.IsEmpty(session.Requests);
                Assert.AreEqual(0, created);
            }
            Assert.AreEqual(0, session.ShiftEarnings);
            Assert.AreEqual(0, session.EstimateFare().Penalties);
        }

        [Test]
        public void SafeCancellationPaysCompletedRequestAndDiversionOnlyAfterDesiredSafeDrop()
        {
            Board();
            var request = NewRequest(TaxiRequestType.Shortcut, 700);
            Assert.IsTrue(session.GenerateRequest(request));
            session.RecordEvent(TaxiEventType.Shortcut, "polish-shortcut");
            Assert.AreEqual(TaxiRequestState.Succeeded, session.Requests[0].State);

            var diversion = NewRequest(TaxiRequestType.ScenicRoute, 900);
            Assert.IsTrue(session.GenerateRequest(diversion));
            Assert.IsTrue(session.AcceptDiversion());
            var activeDiversion = session.ActiveStop;
            Assert.IsNotNull(activeDiversion);
            var stop = activeDiversion.StopPoint;
            session.Tick(stop.durationSeconds, stop.Position, 0, 0, true);
            Assert.AreEqual(TaxiRequestState.Succeeded, activeDiversion.State);
            Assert.AreEqual(0, session.ShiftEarnings, "Earned objectives are not paid at the diversion stop.");

            session.FailRide("Passenger requested a safe cancellation.");
            var desiredDrop = session.CurrentDesiredDestination;
            Assert.IsTrue(session.SafeDropRequested);
            Assert.IsNotNull(desiredDrop);
            Assert.AreSame(session.SafeDropDestination, desiredDrop);
            Assert.AreEqual(0, session.ShiftEarnings, "Cancellation pay waits for arrival at the safe drop.");

            session.Tick(.1f, desiredDrop.StopPosition, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.PassengerExiting, session.State);
            Assert.AreEqual(0, session.ShiftEarnings);
            session.Tick(config.exitingSeconds + .1f, desiredDrop.StopPosition, 0, 0, true);

            Assert.AreEqual(TruckTaxiState.RideComplete, session.State);
            Assert.IsTrue(session.LastFare.IsCancellation);
            Assert.AreEqual(700, session.LastFare.Requests);
            Assert.AreEqual(900, session.LastFare.Diversions);
            Assert.AreEqual(session.LastFare.Total, session.ShiftEarnings);
        }

        private void Board()
        {
            session.StartShift();
            Assert.IsTrue(session.OfferRide());
            Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f, session.Pickup.StopPosition, 0, 0, true);
            session.Tick(2, session.Pickup.StopPosition, 0, 0, true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination, session.State);
        }

        private PassengerRequestDefinition NewRequest(TaxiRequestType type, long reward)
        {
            var definition = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            objects.Add(definition);
            definition.requestType = type;
            definition.target = 1;
            definition.timer = 100;
            definition.bonusMoneyCents = reward;
            return definition;
        }
    }
}
