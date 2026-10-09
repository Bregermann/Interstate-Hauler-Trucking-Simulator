using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPersistence")]
    public sealed class TruckTaxiSessionPersistenceTests
    {
        private readonly List<UnityEngine.Object> created=new List<UnityEngine.Object>();
        private TruckTaxiConfiguration config;
        private PassengerProfile passenger;
        private TruckTaxiRideLocation[] stops;
        private bool hadPreference;
        private int preference;

        [SetUp] public void SetUp()
        {
            hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            preference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            config=Create<TruckTaxiConfiguration>(); config.minimumTripDistance=10; config.maximumTripDistance=1000;
            config.intercityRideChance=0; config.boardingSeconds=.01f;
            passenger=Create<PassengerProfile>(); passenger.passengerId="persistence.passenger";
            passenger.basePatience=10000; config.passengers=new[]{passenger};
            stops=new TruckTaxiRideLocation[2];
            for(int i=0;i<stops.Length;i++)
            {
                var go=new GameObject("Persistence stop "+i); created.Add(go);
                stops[i]=go.AddComponent<TruckTaxiRideLocation>(); stops[i].locationId="persistence.stop."+i;
                go.transform.position=Vector3.right*i*100;
            }
        }
        [TearDown] public void TearDown()
        {
            foreach(var item in created) UnityEngine.Object.DestroyImmediate(item); created.Clear();
            if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,preference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }
        private T Create<T>() where T:ScriptableObject
        { var item=ScriptableObject.CreateInstance<T>(); created.Add(item); return item; }
        private TruckTaxiSession Session()
        { var session=new TruckTaxiSession(config,stops,731); session.StartShift(); return session; }
        private void Board(TruckTaxiSession session)
        {
            Assert.IsTrue(session.OfferRide(passenger)); Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
        }
        private TruckTaxiSession FundedSession()
        {
            var session=Session(); Assert.IsTrue(session.TryEnterRace(0)); session.CommitRaceEntry();
            session.RecordRaceResult(1,80.5f,123456,75);
            Assert.IsTrue(session.TrySpend(1200)); session.ChargeService(124000);
            session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.JugSucceeded);
            session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.JugSpilled);
            session.RecordNeedsEvent(TruckTaxiDriverNeedEvent.LitterThrown);
            session.SetWorldConditions(2345.5,13,TruckTaxiDemandWeather.Neutral);
            return session;
        }

        [Test] public void WalletRoundTripPreservesAllComponentsAndDebtWithoutGrantingMoney()
        {
            var source=FundedSession(); var snapshot=source.CapturePersistentState();
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(snapshot,out string reason),reason);
            var target=Session(); target.RecordRaceResult(3,0,500);
            Assert.IsTrue(target.TryRestorePersistentState(snapshot,out reason),reason);
            Assert.AreEqual(123456,target.ShiftEarnings); Assert.AreEqual(1200,target.PurchaseChargesCents);
            Assert.AreEqual(124000,target.ServiceChargesCents); Assert.AreEqual(-1744,target.WalletBalanceCents);
            Assert.AreEqual(1744,target.ServiceDebtCents); Assert.AreEqual(123456,target.RacePrizeEarningsCents);
            Assert.AreEqual(1,target.RacesEntered); Assert.AreEqual(1,target.RaceWins); Assert.AreEqual(1,target.RacePodiums);
            Assert.AreEqual(75,target.RaceChaosEarned); Assert.AreEqual(80.5f,target.ThunderBowlBestLapSeconds);
            Assert.AreEqual(1,target.JugsSucceeded); Assert.AreEqual(1,target.JugsSpilled); Assert.AreEqual(1,target.ContainersThrown);
            Assert.AreEqual(2345.5,target.GameMinutes);
            Assert.IsTrue(target.TryRestorePersistentState(snapshot,out reason),reason);
            Assert.AreEqual(-1744,target.WalletBalanceCents,"Restoring twice must not add saved earnings again.");
        }

        [Test] public void CompletedStatsExposureGuaranteeAndWorkAreaRoundTrip()
        {
            var source=Session(); source.SetWorldConditions(50,12,TruckTaxiDemandWeather.Neutral);
            for(int i=0;i<3;i++) { Board(source); source.DebugComplete(); source.ContinueShift(); }
            Assert.IsTrue(source.SetWorkArea(new TruckTaxiRideWorkArea { mode=TruckTaxiWorkAreaMode.Custom,
                center=new Vector3(10,20,30),radius=400,label="AUTHORED WORK AREA" }));
            var snapshot=source.CapturePersistentState(); var target=Session();
            Assert.AreEqual("custom",snapshot.workArea.modeId);
            Assert.IsTrue(target.TryRestorePersistentState(snapshot,out string reason),reason);
            Assert.AreEqual(source.CompletedRides,target.CompletedRides); Assert.AreEqual(source.TotalStarsEarned,target.TotalStarsEarned);
            Assert.AreEqual(source.ShiftScore,target.ShiftScore); Assert.AreEqual(source.ShiftEarnings,target.ShiftEarnings);
            var exposure=target.CapturePassengerSelectionState().passengers.Single();
            Assert.AreEqual(3,exposure.timesOffered); Assert.AreEqual(3,exposure.timesAccepted); Assert.AreEqual(3,exposure.timesCompleted);
            Assert.AreEqual(3,target.IntercityPolicy.CompletedLocalStreak); Assert.IsTrue(target.IntercityPolicy.GuaranteePending);
            Assert.AreEqual(new Vector3(10,20,30),target.WorkArea.center); Assert.AreEqual(400,target.WorkArea.radius);
            Assert.AreEqual("AUTHORED WORK AREA",target.WorkArea.label);
            snapshot.passengerSelection.passengers[0].timesOffered=999; snapshot.workArea.centerX=999;
            Assert.AreEqual(3,target.OffersGenerated); Assert.AreEqual(10,target.WorkArea.center.x);
        }

        [Test] public void ActiveRideIsNotSavedOrPaidAndRestoredSessionIsAvailable()
        {
            var source=FundedSession(); Board(source);
            var snapshot=source.CapturePersistentState(); Assert.AreEqual(0,snapshot.statistics.completedRides);
            var target=Session(); Board(target); var owner=new object(); target.AcquireOfferSuppression(owner);
            int notifications=0; target.Changed+=()=>notifications++;
            Assert.IsTrue(target.TryRestorePersistentState(snapshot,out string reason),reason);
            Assert.AreEqual(TruckTaxiState.Available,target.State); Assert.IsFalse(target.HasPassenger);
            Assert.IsNull(target.Passenger); Assert.IsNull(target.Pickup); Assert.IsNull(target.Destination);
            Assert.IsNull(target.Offer); Assert.IsNull(target.CurrentRideId); Assert.IsNull(target.LastFare);
            Assert.IsEmpty(target.Requests); Assert.IsEmpty(target.RideHistory); Assert.IsFalse(target.OffersSuppressed);
            Assert.IsFalse(target.IsGeneratingOffer); Assert.AreEqual(0,target.FailedRides); Assert.AreEqual(0,target.CompletedRides);
            Assert.AreEqual(1,target.CapturePassengerSelectionState().passengers.Single().timesAccepted);
            Assert.AreEqual(source.WalletBalanceCents,target.WalletBalanceCents); Assert.AreEqual(1,notifications);
            target.DebugComplete(); Assert.AreEqual(source.WalletBalanceCents,target.WalletBalanceCents);
        }

        [Test] public void RequestToggleRestoresWithoutWritingPreferencesOrStartingAnOffer()
        {
            var source=Session(); var snapshot=source.CapturePersistentState(); snapshot.rideRequestsEnabled=false;
            var target=Session(); Assert.IsTrue(target.RequestRideOffer());
            int before=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey);
            Assert.IsTrue(target.TryRestorePersistentState(snapshot,out string reason),reason);
            Assert.IsFalse(target.RideRequestsEnabled); Assert.IsFalse(target.IsGeneratingOffer);
            Assert.AreEqual(TruckTaxiState.Available,target.State);
            Assert.AreEqual(before,PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey));
            target.Tick(1,Vector3.zero,0,0,true); Assert.IsFalse(target.IsGeneratingOffer); Assert.IsNull(target.Offer);
        }

        [Test] public void PendingRaceChargeCannotBecomeARefundAfterRestore()
        {
            var source=Session(); source.RecordRaceResult(3,0,1000);
            Assert.IsTrue(source.TryEnterRace(125)); var snapshot=source.CapturePersistentState();
            var target=Session(); Assert.IsTrue(target.TryRestorePersistentState(snapshot,out string reason),reason);
            Assert.AreEqual(875,target.WalletBalanceCents); Assert.AreEqual(125,target.PurchaseChargesCents);
            Assert.IsFalse(target.RefundRaceEntry()); Assert.AreEqual(875,target.WalletBalanceCents);
        }

        [Test] public void CaptureIsDetachedAndDoesNotStopOfferGeneration()
        {
            var session=Session(); Assert.IsTrue(session.OfferRide()); session.DeclineRide();
            Assert.IsTrue(session.IsGeneratingOffer); var snapshot=session.CapturePersistentState();
            snapshot.passengerSelection.passengers[0].timesOffered=123;
            snapshot.statistics.offersDeclined=123; snapshot.workArea.radius=999;
            Assert.IsTrue(session.IsGeneratingOffer); Assert.AreEqual(1,session.OffersGenerated);
            Assert.AreEqual(1,session.OffersDeclined); Assert.AreEqual(500,session.WorkArea.radius);
        }

        [TestCase("null")]
        [TestCase("version")]
        [TestCase("negative-cash")]
        [TestCase("balance-overflow")]
        [TestCase("debt-overflow")]
        [TestCase("missing-stats")]
        [TestCase("negative-count")]
        [TestCase("count-overflow")]
        [TestCase("lap-nan")]
        [TestCase("clock-infinity")]
        [TestCase("area-nan")]
        [TestCase("area-overflow")]
        [TestCase("area-radius")]
        [TestCase("area-mode")]
        [TestCase("duplicate-id")]
        [TestCase("exposure-totals")]
        [TestCase("exposure-future-time")]
        [TestCase("intercity-cooldown")]
        public void InvalidSnapshotLeavesWalletRideExposureAndGeneratorUntouched(string defect)
        {
            var source=Session(); Assert.IsTrue(source.OfferRide()); source.DeclineRide();
            var snapshot=source.CapturePersistentState();
            switch(defect)
            {
                case "null": snapshot=null; break;
                case "version": snapshot.version=999; break;
                case "negative-cash": snapshot.purchaseChargesCents=-1; break;
                case "balance-overflow": snapshot.purchaseChargesCents=long.MaxValue; snapshot.serviceChargesCents=2; break;
                case "debt-overflow": snapshot.purchaseChargesCents=long.MaxValue; snapshot.serviceChargesCents=1; break;
                case "missing-stats": snapshot.statistics=null; break;
                case "negative-count": snapshot.statistics.jugsSpilled=-1; break;
                case "count-overflow": snapshot.statistics.racesEntered=int.MaxValue; break;
                case "lap-nan": snapshot.statistics.thunderBowlBestLapSeconds=float.NaN; break;
                case "clock-infinity": snapshot.gameMinutes=double.PositiveInfinity; break;
                case "area-nan": snapshot.workArea.centerY=float.NaN; break;
                case "area-overflow": snapshot.workArea.centerX=float.MaxValue; break;
                case "area-radius": snapshot.workArea.radius=49; break;
                case "area-mode": snapshot.workArea.modeId="3"; break;
                case "duplicate-id": snapshot.passengerSelection.passengers.Add(snapshot.passengerSelection.passengers[0].Copy()); break;
                case "exposure-totals": snapshot.passengerSelection.offerIndex++; break;
                case "exposure-future-time": snapshot.passengerSelection.passengers[0].lastOfferedGameMinutes=100; break;
                case "intercity-cooldown": snapshot.intercity.localCooldownRemaining=2; break;
            }
            var target=FundedSession(); Assert.IsTrue(target.OfferRide()); target.DeclineRide();
            int changes=0; target.Changed+=()=>changes++;
            long cash=target.WalletBalanceCents; int generated=target.OffersGenerated;
            Assert.IsFalse(TruckTaxiSession.ValidatePersistentState(snapshot,out string reason)); Assert.IsNotEmpty(reason);
            Assert.IsFalse(target.TryRestorePersistentState(snapshot,out reason)); Assert.IsNotEmpty(reason);
            Assert.AreEqual(cash,target.WalletBalanceCents); Assert.AreEqual(generated,target.OffersGenerated);
            Assert.IsTrue(target.IsGeneratingOffer); Assert.AreEqual(TruckTaxiState.Available,target.State);
            Assert.AreEqual(1,target.OffersDeclined); Assert.Zero(changes);
        }

        [Test] public void CareerChaosGoalsAndRatingsSurviveClearedHistoryAndRepeatedRestore()
        {
            var source=Session(); Board(source);
            var goal=Create<PassengerRequestDefinition>(); goal.requestType=TaxiRequestType.MaximumChaos;
            goal.objectiveId="test.goal.chaos"; goal.target=100;
            Assert.IsTrue(source.GenerateRequest(goal));
            source.DebugResolveRequest(true); source.DebugChaos(); source.DebugComplete();
            var snapshot=JsonUtility.FromJson<TruckTaxiPersistentState>(JsonUtility.ToJson(source.CapturePersistentState()));
            Assert.Greater(snapshot.career.chaosScore,0);
            Assert.AreEqual(1,snapshot.career.GoalsCompleted);
            Assert.AreEqual(1,snapshot.career.goals.Single().offered);
            Assert.AreEqual(1,snapshot.career.goals.Single().attempted);
            var target=Session();
            for(int i=0;i<2;i++)
            {
                Assert.IsTrue(target.TryRestorePersistentState(snapshot,out var reason),reason);
                Assert.IsEmpty(target.RideHistory);
                Assert.AreEqual(source.LifetimeChaosScore,target.LifetimeChaosScore);
                Assert.AreEqual(1,target.LifetimeObjectivesCompleted);
                Assert.AreEqual(source.TotalStarsEarned,target.TotalStarsEarned);
            }
            snapshot.career.goals[0].completed=0;
            Assert.AreEqual(1,target.LifetimeObjectivesCompleted,"Restore must detach nested lists.");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void LegacyJsonWithoutOptionalCareerIdentityRestoresIntoBoundRun(int version)
        {
            string json="{\"version\":"+version+",\"statistics\":{},"+
                "\"workArea\":{\"modeId\":\"nearby\",\"radius\":500},"+
                "\"passengerSelection\":{\"version\":1,\"offerIndex\":0,\"passengers\":[],\"recentArchetypes\":[]},"+
                "\"intercity\":{\"version\":1},"+
                "\"career\":{\"version\":1,\"goals\":[],\"events\":[],\"discoveredToiletIds\":[]}}";
            var snapshot=JsonUtility.FromJson<TruckTaxiPersistentState>(json);
            string before=JsonUtility.ToJson(snapshot);
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(snapshot,out var reason),reason);
            var target=Session();
            Assert.IsTrue(target.TryRestoreCareerRun("legacy.target",snapshot,out reason),reason);
            Assert.IsFalse(target.HasCareerIdentity);
            Assert.IsTrue(target.TryInitializeCareer("Driver","truck.test",out reason),reason);
            Assert.AreEqual("legacy.target",target.CareerRunProfileId);
            Assert.IsFalse(target.CaptureCareerProfile().firstGuideEligible);
            Assert.AreEqual(before,JsonUtility.ToJson(snapshot),"Validation/restore must not mutate source data.");
        }

        [TestCase("identity")]
        [TestCase("guide")]
        [TestCase("ownership")]
        [TestCase("future-version")]
        public void PartialCareerDataIsNotTreatedAsAbsent(string defect)
        {
            var snapshot=Session().CapturePersistentState();
            snapshot.ownedProfile=new TruckTaxiCareerProfileData();
            switch(defect)
            {
                case "identity": snapshot.ownedProfile.runProfileId="foreign.profile"; break;
                case "guide": snapshot.ownedProfile.firstGuideClaimed=true; break;
                case "ownership": snapshot.ownedProfile.ownedHeadwearIds.Add("hat.test"); break;
                case "future-version": snapshot.ownedProfile.version=99; break;
            }
            snapshot=JsonUtility.FromJson<TruckTaxiPersistentState>(JsonUtility.ToJson(snapshot));
            var target=FundedSession(); long balance=target.WalletBalanceCents;
            Assert.IsFalse(target.TryRestoreCareerRun("target.profile",snapshot,out var reason));
            Assert.IsNotEmpty(reason);
            Assert.AreEqual(balance,target.WalletBalanceCents);
            Assert.IsFalse(target.HasCareerIdentity);
        }

        [Test] public void LastSessionIsSeparateFromLifetimeAfterResume()
        {
            var first=FundedSession(); var snapshot=first.CapturePersistentState();
            var resumed=Session();
            Assert.IsTrue(resumed.TryRestorePersistentState(snapshot,out var reason),reason);
            Assert.AreEqual(snapshot.lastSession.earningsCents,resumed.CaptureLastSessionStatistics().earningsCents);
            resumed.RecordRaceResult(3,0,500);
            var next=resumed.CapturePersistentState();
            Assert.AreEqual(snapshot.shiftEarningsCents+500,next.shiftEarningsCents);
            Assert.AreEqual(500,next.lastSession.earningsCents);
            Assert.AreEqual(0,next.lastSession.jugEvents);
            Assert.AreEqual(1,next.statistics.jugsSucceeded);
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(next,out reason),reason);
        }

        [Test] public void LegacyMigrationPreservesKnownTotalsWithoutInventingHistoricalDetails()
        {
            var legacy=FundedSession().CapturePersistentState();
            legacy.version=1; legacy.career=null; legacy.lastSession=null;
            var target=Session();
            Assert.IsTrue(target.TryRestorePersistentState(legacy,out var reason),reason);
            var upgraded=target.CapturePersistentState();
            Assert.AreEqual(2,upgraded.version);
            Assert.AreEqual(legacy.shiftEarningsCents,upgraded.shiftEarningsCents);
            Assert.AreEqual(legacy.statistics.jugsSucceeded,upgraded.statistics.jugsSucceeded);
            Assert.IsTrue(upgraded.career.historicalDetailsIncomplete);
            Assert.IsNull(upgraded.lastSession);
        }

        [Test] public void StableEventAndToiletCountsRoundTripWithoutDuplicateDiscovery()
        {
            var source=Session();
            Assert.IsTrue(source.RecordCareerEvent("taxi.event.test"));
            Assert.IsTrue(source.RecordCareerEvent("taxi.event.test"));
            Assert.IsTrue(source.RecordCareerToiletDiscovery("taxi.toilet.test"));
            Assert.IsFalse(source.RecordCareerToiletDiscovery("taxi.toilet.test"));
            source.RecordCareerRaceCompleted(); source.RecordCareerStolenFareCompleted();
            var target=Session();
            Assert.IsTrue(target.TryRestorePersistentState(source.CapturePersistentState(),out var reason),reason);
            var c=target.CaptureCareerStatistics();
            Assert.AreEqual(2,c.events.Single().count);
            Assert.AreEqual(1,c.racesCompleted); Assert.AreEqual(1,c.stolenFaresCompleted);
            Assert.IsFalse(target.RecordCareerToiletDiscovery("taxi.toilet.test"));
        }

        [TestCase("duplicate-goal")]
        [TestCase("nonfinite-playtime")]
        [TestCase("last-session-exceeds-lifetime")]
        [TestCase("missing-career")]
        public void InvalidCareerDataCannotMutateLiveState(string defect)
        {
            var source=FundedSession().CapturePersistentState();
            switch(defect)
            {
                case "duplicate-goal":
                    source.career.goals.Add(new TruckTaxiCareerGoalStatistics { id="test.goal" });
                    source.career.goals.Add(new TruckTaxiCareerGoalStatistics { id="test.goal" }); break;
                case "nonfinite-playtime": source.career.playtimeSeconds=double.NaN; break;
                case "last-session-exceeds-lifetime": source.lastSession.rides=99; break;
                case "missing-career": source.career=null; break;
            }
            var target=Session(); string before=JsonUtility.ToJson(target.CapturePersistentState());
            Assert.IsFalse(target.TryRestorePersistentState(source,out _));
            Assert.AreEqual(before,JsonUtility.ToJson(target.CapturePersistentState()));
        }

        [Test] public void SavedMenuStatsExtractWithoutStaticCacheOrRestoringASession()
        {
            var source=FundedSession().CapturePersistentState();
            var saved=new LwsSaveSnapshot { profileId="test.profile",participants=new List<LwsSaveParticipantState> {
                new LwsSaveParticipantState { participantId=TruckTaxiVibinSaveParticipant.Id,payloadVersion=1,
                    payloadJson=JsonUtility.ToJson(new TruckTaxiVibinSavePayload { session=source,regionId="test" }) }
            }};
            TruckTaxiMainMenu.ClearSessionStats();
            Assert.IsTrue(TruckTaxiMainMenu.TryExtractSavedStatistics(saved,"test.profile",out var extracted,out var reason),reason);
            Assert.AreEqual(source.shiftEarningsCents,TruckTaxiMainMenu.LifetimeSummary(extracted).earningsCents);
            Assert.AreEqual(source.lastSession.jugEvents,extracted.lastSession.jugEvents);
            Assert.IsNull(TruckTaxiMainMenu.LastSessionStats);
            Assert.IsFalse(TruckTaxiMainMenu.TryExtractSavedStatistics(saved,"different.profile",out _,out _));
            saved.participants.Add(saved.participants[0]);
            Assert.IsFalse(TruckTaxiMainMenu.TryExtractSavedStatistics(saved,"test.profile",out _,out _));
        }

        [Test] public void FiveManualDeclinesBlockAllOffersForTenGameMinutesAcrossSaveRestore()
        {
            var source=Session(); source.SetWorldConditions(100,12,TruckTaxiDemandWeather.Neutral);
            for(int i=0;i<5;i++) { Assert.IsTrue(source.OfferRide(passenger)); source.DeclineRide(); }
            Assert.AreEqual(5,source.ConsecutiveManualDeclines);
            Assert.AreEqual(10,source.ManualDeclineCooldownRemainingGameMinutes,0.0001);
            Assert.IsFalse(source.OfferRide(passenger));
            Assert.IsFalse(source.RequestRideOffer());
            var target=Session();
            Assert.IsTrue(target.TryRestorePersistentState(source.CapturePersistentState(),out var reason),reason);
            target.SetWorldConditions(109.99,12,TruckTaxiDemandWeather.Neutral);
            Assert.IsFalse(target.OfferRide(passenger));
            target.SetWorldConditions(110,12,TruckTaxiDemandWeather.Neutral);
            Assert.AreEqual(0,target.ConsecutiveManualDeclines);
            Assert.IsTrue(target.OfferRide(passenger));
        }

        [Test] public void AcceptanceResetsDeclinesButExpiryAndRecoveryDoNotCountAsManualDeclines()
        {
            var session=Session();
            Assert.IsTrue(session.OfferRide(passenger)); session.DeclineRide();
            Assert.IsTrue(session.OfferRide(passenger)); session.CancelTemporaryActionsForRecovery();
            Assert.AreEqual(1,session.ConsecutiveManualDeclines);
            Assert.IsTrue(session.OfferRide(passenger));
            session.Tick(session.OfferDuration+1,Vector3.zero,0,0,true);
            Assert.AreEqual(1,session.OffersExpired); Assert.AreEqual(1,session.ConsecutiveManualDeclines);
            Assert.IsTrue(session.OfferRide(passenger)); Assert.IsTrue(session.AcceptRide());
            Assert.AreEqual(0,session.ConsecutiveManualDeclines);
            Assert.IsFalse(session.ManualDeclineCooldownActive);
        }

        [Test] public void ExternalGameClockNotWallSecondsControlsDeclineCooldown()
        {
            var session=Session(); session.SetWorldConditions(10,12,TruckTaxiDemandWeather.Neutral);
            for(int i=0;i<5;i++) { Assert.IsTrue(session.OfferRide(passenger)); session.DeclineRide(); }
            session.Tick(600,Vector3.zero,0,0,true);
            Assert.AreEqual(10,session.ManualDeclineCooldownRemainingGameMinutes,0.0001);
            Assert.IsFalse(session.IsGeneratingOffer);
        }

        [Test] public void CancelPickupIsGuardedRecordsOnceAndPreservesOtherOwners()
        {
            var session=Session(); Assert.IsTrue(session.OfferRide(passenger)); Assert.IsTrue(session.AcceptRide());
            var owner=new object(); session.AcquireOfferSuppression(owner);
            int reentrantSuccesses=0;
            session.Changed+=()=> { if(session.CancelPickup()) reentrantSuccesses++; };
            Assert.IsTrue(session.CancelPickup());
            Assert.IsFalse(session.CancelPickup());
            Assert.Zero(reentrantSuccesses);
            Assert.AreEqual(1,session.FailedRides); Assert.AreEqual(1,session.PickupCancellations);
            Assert.AreEqual(1,session.RideHistory.Count);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsTrue(session.OffersSuppressed);
            Assert.IsNull(session.CurrentRideId);
            Assert.IsTrue(TruckTaxiSession.ValidatePersistentState(session.CapturePersistentState(),out var reason),reason);
        }

        [Test] public void CancelPickupRejectsBoardingAndOnboardFare()
        {
            var session=Session(); Assert.IsTrue(session.OfferRide(passenger)); Assert.IsTrue(session.AcceptRide());
            session.DebugBoard(); Assert.IsFalse(session.CancelPickup());
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            Assert.IsTrue(session.HasPassenger); Assert.IsFalse(session.CancelPickup());
            Assert.AreEqual(0,session.PickupCancellations);
        }

        [Test] public void NewConfigurationUsesQuarterIntercityChanceAndEmptyReactionAuthoring()
        {
            var fresh=Create<TruckTaxiConfiguration>();
            Assert.AreEqual(.25f,fresh.intercityRideChance);
            Assert.IsEmpty(fresh.companionReactions);
        }

        [Test] public void SnapshotSchemaContainsOnlyPlainDataAndNoEnumOrLiveRideFields()
        {
            var visited=new HashSet<Type>(); AssertPlain(typeof(TruckTaxiPersistentState),visited);
            AssertPlain(typeof(UnityEngine.Color),new HashSet<Type>());
            Assert.Throws<AssertionException>(()=>AssertPlain(typeof(UnityEngine.Object),new HashSet<Type>()));
            Assert.Throws<AssertionException>(()=>AssertPlain(typeof(UnityEngine.CameraClearFlags),new HashSet<Type>()));
            Assert.IsFalse(typeof(TruckTaxiPersistentState).GetFields().Any(field=>field.Name=="state" || field.Name=="ride" || field.Name=="passenger"));
        }
        private static void AssertPlain(Type type,HashSet<Type> visited)
        {
            if(type.IsPrimitive || type==typeof(string)) return;
            Assert.IsFalse(type.IsEnum,type.FullName); Assert.IsFalse(typeof(UnityEngine.Object).IsAssignableFrom(type),type.FullName);
            if(type.IsGenericType && type.GetGenericTypeDefinition()==typeof(List<>)) { AssertPlain(type.GetGenericArguments()[0],visited); return; }
            if(!visited.Add(type)) return;
            var fields=type.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public);
            // Unity serializes Color's RGBA values despite its missing CLR Serializable attribute.
            if(type==typeof(UnityEngine.Color))
            {
                Assert.IsTrue(type.IsValueType);
                CollectionAssert.AreEquivalent(new[]{"r","g","b","a"},fields.Select(field=>field.Name));
                Assert.IsTrue(fields.All(field=>field.FieldType==typeof(float)));
            }
            else Assert.IsTrue(type.IsSerializable,type.FullName);
            foreach(var field in fields) AssertPlain(field.FieldType,visited);
        }
    }
}
