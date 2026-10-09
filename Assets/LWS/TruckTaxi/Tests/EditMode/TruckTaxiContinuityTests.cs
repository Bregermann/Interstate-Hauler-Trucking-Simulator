using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiContinuityTests
    {
        private readonly List<Object> created = new List<Object>();
        private TruckTaxiConfiguration config;
        private PassengerProfile passenger;
        private List<TruckTaxiRideLocation> stops;
        private TruckTaxiSession session;
        private bool hadPreference;
        private int previousPreference;

        [SetUp] public void SetUp()
        {
            hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            previousPreference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); created.Add(config);
            passenger=ScriptableObject.CreateInstance<PassengerProfile>(); created.Add(passenger);
            passenger.passengerId="test.person"; passenger.basePatience=1000;
            config.passengers=new[]{passenger}; config.minimumTripDistance=10; config.maximumTripDistance=1000;
            stops=new List<TruckTaxiRideLocation>();
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("Stop "+i); created.Add(go);
                var stop=go.AddComponent<TruckTaxiRideLocation>(); stop.locationId="stop."+i;
                stop.district=i==2 ? "east" : "west"; go.transform.position=Vector3.right*i*100;
                stops.Add(stop);
            }
            session=new TruckTaxiSession(config,stops,17);
            session.SetWorldConditions(0,12,TruckTaxiDemandWeather.Neutral);
            session.StartShift();
        }
        [TearDown] public void TearDown()
        {
            foreach(var item in created) Object.DestroyImmediate(item);
            created.Clear();
            if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,previousPreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }

        private void CompleteRide()
        {
            Assert.IsTrue(session.OfferRide(passenger)); Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(2,session.Pickup.StopPosition,0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
            session.DebugComplete(); Assert.AreEqual(TruckTaxiState.RideComplete,session.State);
        }

        [Test] public void RecentRepeatStartsAtLastDropoffAndHistoryIsSnapshot()
        {
            CompleteRide(); string destination=session.Destination.locationId;
            var first=session.RideHistory[0];
            Assert.AreEqual(TruckTaxiRideOutcome.Completed,first.Outcome);
            Assert.AreEqual(destination,first.DestinationId);
            Assert.IsTrue(session.TryGetPassengerContinuity(passenger.passengerId,out var last,out var minute,out var district));
            Assert.AreEqual(destination,last); Assert.AreEqual(0,minute); Assert.IsNotNull(district);
            session.ContinueShift(); session.SetWorldConditions(30,12,TruckTaxiDemandWeather.Neutral);
            Assert.IsTrue(session.OfferRide(passenger));
            Assert.AreEqual(destination,session.Pickup.locationId);
            Assert.IsTrue(session.IsRepeatPassenger);
            Assert.AreEqual(destination,first.DestinationId);
        }

        [Test] public void DestinationOnlyDropoffUsesNearestLegalBayWithoutChangingHistory()
        {
            CompleteRide(); var destination=session.Destination;
            destination.pickupAllowed=false;
            var bay=stops.Find(stop=>stop!=destination);
            bay.transform.position=destination.StopPosition+Vector3.forward*5;
            session.ContinueShift();
            Assert.IsTrue(session.OfferRide(passenger));
            Assert.AreEqual(bay,session.Pickup);
            Assert.AreEqual(destination.locationId,session.RideHistory[0].DestinationId);
        }

        [Test] public void RecentPassengerCannotJumpToDistantBayWhenDropoffDisallowsPickup()
        {
            CompleteRide(); session.Destination.pickupAllowed=false;
            session.ContinueShift();
            Assert.IsFalse(session.OfferRide(passenger));
            Assert.AreEqual(TruckTaxiState.Available,session.State);
        }

        [Test] public void LongElapsedGameTimeDoesNotTeleportRepeatPassenger()
        {
            CompleteRide(); string destination=session.Destination.locationId;
            session.ContinueShift(); session.SetWorldConditions(1000,12,TruckTaxiDemandWeather.Neutral);
            for(int i=0;i<30;i++)
            {
                TruckTaxiDispatchTestClock.ExpireManualDeclineCooldown(session,12);
                Assert.IsTrue(session.OfferRide(passenger));
                Assert.AreEqual(destination,session.Pickup.locationId);
                session.DeclineRide();
            }
        }

        [Test] public void PostAcceptRouteTimerCancelsAndDoesNotPay()
        {
            Assert.IsTrue(session.OfferRide()); Assert.Greater(session.OfferRemaining,0);
            Assert.Zero(session.PickupRemaining);
            float meters=session.Offer.ToPickup.Meters;
            Assert.IsTrue(session.AcceptRide());
            Assert.AreEqual(0,session.OfferRemaining);
            Assert.GreaterOrEqual(session.PickupDuration,config.pickupMinimumSeconds);
            Assert.That(session.PickupDuration,Is.EqualTo(Mathf.Max(config.pickupMinimumSeconds,
                config.pickupBaseGraceSeconds+meters/config.pickupReasonableSpeedMetersPerSecond*
                config.pickupPatienceMultiplier*passenger.pickupPatienceMultiplier)).Within(.01f));
            session.Tick(session.PickupDuration+1,Vector3.one*10000,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.AreEqual(TruckTaxiRideOutcome.PickupCancelled,session.RideHistory[0].Outcome);
            Assert.AreEqual(1,session.PickupCancellations);
            Assert.IsNull(session.RideHistory[0].Fare);
            Assert.Zero(session.ShiftEarnings);
        }

        [Test] public void RequiredServiceBooksDebtAndNeedsCountersAreCaptured()
        {
            CompleteRide(); long fare=session.ShiftEarnings;
            Assert.IsFalse(session.TrySpend(fare+1)); Assert.IsTrue(session.TrySpend(100));
            session.ChargeService(fare);
            Assert.AreEqual(-100,session.WalletBalanceCents);
            Assert.AreEqual(100,session.ServiceDebtCents);
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>session.ChargeService(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>session.TrySpend(-1));
            session.ContinueShift();
            Assert.IsTrue(session.OfferRide(passenger)); Assert.IsTrue(session.AcceptRide());
            session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.JugSucceeded);
            session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.JugSpilled);
            session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.JugThrownFromWindow);
            session.FailRide("Test cancellation");
            Assert.AreEqual(1,session.JugsSucceeded); Assert.AreEqual(1,session.JugsSpilled);
            Assert.AreEqual(1,session.ContainersThrown);
            Assert.AreEqual(1,session.RideHistory[1].JugsSucceeded);
            Assert.AreEqual(1,session.RideHistory[1].ContainersThrown);
        }

        [Test] public void DatabaseCaptainRegistrationIsUniqueAndNoHardcodedCaptainWeight()
        {
            var database=UnityEditor.AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(
                "Assets/LWS/TruckTaxi/Passengers/TruckTaxiPassengerDatabase.asset");
            Assert.IsNotNull(database);
            int captain=0, eligible=0;
            foreach(var profile in database.Query(new TruckTaxiPassengerQuery { availableOnly=true }))
            {
                eligible++;
                if(profile.passengerId!="space-hero") continue;
                captain++;
                Assert.AreEqual(TruckTaxiRarity.Common,profile.rarity);
                Assert.AreEqual(1,profile.spawnWeight);
                Assert.IsEmpty(profile.districts);
            }
            Assert.AreEqual(1,captain);
            Assert.Greater(eligible,50,"The full database, not the ten-profile demo fallback, is used.");
        }

        [Test] public void StableIdCoverageAndManualDeclinesTakePriorityOverRarityWeights()
        {
            var other=ScriptableObject.CreateInstance<PassengerProfile>(); created.Add(other);
            other.passengerId="test.other";
            config.passengers=new[]{passenger,other,passenger};
            config.recentOfferCount=1;
            config.recentOfferWeight=.05f;
            int repeats=0;
            string previous=null;
            for(int i=0;i<500;i++)
            {
                TruckTaxiDispatchTestClock.ExpireManualDeclineCooldown(session,12);
                Assert.IsTrue(session.OfferRide());
                if(session.Passenger.passengerId==previous) repeats++;
                previous=session.Passenger.passengerId;
                session.DeclineRide();
            }
            Assert.Zero(repeats,"Manual decline has another eligible passenger.");
            other.rarity=TruckTaxiRarity.Rare;
            config.rareWeight=.1f;
            config.recentOfferWeight=1;
            int rare=0;
            for(int i=0;i<500;i++)
            {
                TruckTaxiDispatchTestClock.ExpireManualDeclineCooldown(session,12);
                Assert.IsTrue(session.OfferRide());
                if(session.Passenger==other) rare++;
                session.DeclineRide();
            }
            Assert.That(rare,Is.InRange(249,251),"Rarity affects order, not eventual fair coverage.");
        }

        [Test] public void AppreciationRequiresExplicitlyAuthoredAgeTwentyOne()
        {
            var casting=ScriptableObject.CreateInstance<TruckTaxiCastingProfile>(); created.Add(casting);
            passenger.casting=casting; passenger.specialAppreciationEligible=true;
            passenger.explicitlyAdult=true; passenger.adultFemalePresentation=true;
            passenger.flirtatiousPresentation=true;
            passenger.gender=TruckTaxiPassengerGender.Female;
            passenger.minimumAdultAge=20;
            Assert.IsFalse(passenger.CanOfferAppreciation);
            passenger.minimumAdultAge=21;
            Assert.IsTrue(passenger.CanOfferAppreciation);
            Assert.AreEqual(.35f,config.appreciationBaseChance);
        }

        [Test] public void MorningStormChangesOfferCadenceAndFareFromNeutral()
        {
            float neutralDelay=session.DispatchRemaining;
            session.SetWorldConditions(60,8,TruckTaxiDemandWeather.Storm);
            session.Tick(1,Vector3.zero,0,0,true);
            float demandScale=1/(config.morningFrequencyMultiplier*config.stormFrequencyMultiplier);
            Assert.That(session.DispatchRemaining,Is.EqualTo(neutralDelay-demandScale).Within(.001f));
            session.Tick((session.DispatchRemaining-.05f)/demandScale,Vector3.zero,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            session.Tick(.2f,Vector3.zero,0,0,true);
            Assert.IsTrue(session.IsGeneratingOffer || session.State==TruckTaxiState.RideOffered,
                "Morning storm dispatch must start at the adjusted deadline.");
            for(int i=0;i<64 && session.State==TruckTaxiState.Available;i++)
                session.Tick(.1f,Vector3.zero,0,0,true);
            Assert.AreEqual(TruckTaxiState.RideOffered,session.State,session.LastOfferDiagnostics);
            float fare=config.morningFareMultiplier+(config.stormFareMultiplier-1);
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(fare).Within(.001f));
            Assert.Greater(session.DemandEstimatedFareCents,session.Offer.EstimatedFareCents);
            Assert.IsTrue(session.AcceptRide());
            var estimated=session.EstimateFare();
            Assert.AreEqual(config.baseFareCents,estimated.Base);
            Assert.AreEqual((long)System.Math.Round(config.baseFareCents*(config.morningFareMultiplier-1)),estimated.OtherBonus);
            Assert.AreEqual((long)System.Math.Round(config.baseFareCents*(config.stormFareMultiplier-1)),estimated.WeatherBonus);
        }

        [Test] public void ExternalObjectiveProgressRequiresOwnedActiveRideRequest()
        {
            var definition=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); created.Add(definition);
            definition.requestType=TaxiRequestType.Shortcut; definition.target=2; definition.timer=100;
            definition.minimumCount=2; definition.maximumCount=2;
            stops[1].transform.position=Vector3.right*600;
            stops[2].transform.position=Vector3.right*1200;
            config.maximumTripDistance=2000;
            passenger.possibleRequests=new[]{definition}; passenger.requestDifficultyRange=Vector2.one;
            session.Capabilities.Register(TruckTaxiObjectiveCapability.Shortcuts,10);
            Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
            var foreign=new TaxiRequestProgress(definition,1);
            Assert.IsFalse(session.RecordObjectiveProgress(foreign,1));
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(2,session.Pickup.StopPosition,0,0,true);
            Assert.AreEqual(1,session.Requests.Count);
            var request=session.Requests[0];
            Assert.AreEqual(2,request.Target,"The fixture needs two reachable shortcut opportunities.");
            Assert.IsFalse(session.RecordObjectiveProgress(request,-1));
            Assert.IsTrue(session.RecordObjectiveProgress(request,1));
            Assert.AreEqual(1,request.Progress);
            Assert.IsTrue(session.RecordObjectiveProgress(request,100));
            Assert.AreEqual(TaxiRequestState.Succeeded,request.State);
            Assert.AreEqual(request.Target,request.Progress);
            Assert.IsFalse(session.RecordObjectiveProgress(request,1));
        }
    }

    internal static class TruckTaxiDispatchTestClock
    {
        internal static void ExpireManualDeclineCooldown(TruckTaxiSession session, float hour)
        {
            double remaining=session.ManualDeclineCooldownRemainingGameMinutes;
            if(remaining<=0) return;
            Assert.IsFalse(session.OfferRide(),"Manual declines must block offers until the real cooldown expires.");
            // Keep the demand-window variable fixed while advancing the authoritative elapsed game clock.
            session.SetWorldConditions(session.GameMinutes+remaining,hour,TruckTaxiDemandWeather.Neutral);
            Assert.IsFalse(session.ManualDeclineCooldownActive);
            Assert.Zero(session.ConsecutiveManualDeclines);
        }
    }
}
