using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiDispatch"), Category("TaxiLateSeptember")]
    public sealed class TruckTaxiDispatchCoverageTests
    {
        private readonly List<UnityEngine.Object> created=new List<UnityEngine.Object>();
        private TruckTaxiConfiguration config;
        private TruckTaxiRideLocation[] stops;
        private TruckTaxiRouteDistanceService routes;
        private Vector3 position;
        private int preference;
        private bool hadPreference;

        [SetUp] public void SetUp()
        {
            hadPreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            preference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            config=Create<TruckTaxiConfiguration>();
            config.minimumTripDistance=10; config.maximumTripDistance=800; config.intercityMaximumTripDistance=4000;
            config.pickupHardMaximumSeconds=400; config.intercityRideChance=.02f;
            config.boardingSeconds=.01f;
            var graph=new LwsRoadGraph { graphId="dispatch-coverage" };
            stops=new TruckTaxiRideLocation[4];
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("dispatch-stop-"+i); created.Add(go);
                go.transform.position=new Vector3(i*400,0,0);
                stops[i]=go.AddComponent<TruckTaxiRideLocation>();
                stops[i].locationId="stop-"+i; stops[i].district="Downtown";
                graph.nodes.Add(new LwsRoadNode { nodeId="node-"+i,position=go.transform.position });
            }
            for(int a=0;a<4;a++) for(int b=0;b<4;b++) if(a!=b)
            {
                var start=stops[a].StopPosition; var end=stops[b].StopPosition;
                float distance=Vector3.Distance(start,end);
                var edge=new LwsRoadEdge { edgeId=$"{a}-{b}",fromNodeId="node-"+a,toNodeId="node-"+b,
                    distanceMeters=distance,travelCost=distance };
                edge.samples.Add(new LwsRoadSample { position=start,forward=(end-start).normalized,distanceFromStartMeters=0 });
                edge.samples.Add(new LwsRoadSample { position=end,forward=(end-start).normalized,distanceFromStartMeters=distance });
                graph.edges.Add(edge);
            }
            Assert.IsTrue(graph.Validate().IsValid);
            routes=new TruckTaxiRouteDistanceService(graph); position=Vector3.zero;
        }
        [TearDown] public void TearDown()
        {
            foreach(var item in created) UnityEngine.Object.DestroyImmediate(item);
            created.Clear();
            if(hadPreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,preference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
        }
        private T Create<T>() where T:ScriptableObject
        { var item=ScriptableObject.CreateInstance<T>(); created.Add(item); return item; }
        private PassengerProfile Passenger(string id)
        { var p=Create<PassengerProfile>(); p.passengerId=id; p.basePatience=10000; return p; }
        private TruckTaxiSession Session(int seed=20260930,bool regional=true)
        {
            var session=new TruckTaxiSession(config,stops,seed,routes,()=>position,
                regional ? (Func<Vector3,string>)(p=>p.x>=800 ? "east" : "west") : null);
            session.StartShift(); return session;
        }
        private void Board(TruckTaxiSession session)
        {
            Assert.IsTrue(session.AcceptRide()); position=session.Pickup.StopPosition;
            session.Tick(.1f,position,0,0,true); session.Tick(.1f,position,0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
        }
        private void Complete(TruckTaxiSession session)
        {
            Board(session); position=session.Destination.StopPosition;
            session.DebugComplete();
            if(session.State==TruckTaxiState.AppreciationOffer) session.ChooseAppreciation(false);
            Assert.AreEqual(TruckTaxiState.RideComplete,session.State);
        }

        [TestCase(12,"Downtown",false)]
        [TestCase(23,"Downtown",false)]
        [TestCase(12,"Neutral",false)]
        [TestCase(12,"Speedway",true)]
        public void ThousandActualOffersCoverEligibleRosterWithBoundedStreaks(float hour,string district,bool racing)
        {
            config.passengerDatabase=UnityEditor.AssetDatabase.LoadAssetAtPath<TruckTaxiPassengerDatabase>(
                "Assets/LWS/TruckTaxi/Passengers/TruckTaxiPassengerDatabase.asset");
            Assert.IsNotNull(config.passengerDatabase);
            foreach(var stop in stops) stop.district=district;
            if(district=="Speedway")
            {
                // Public venue pickups need a destination outside the same complex in each town.
                stops[0].locationId="taxi.speedway.main-gate";
                stops[2].locationId="taxi.speedway.vip-entrance";
                foreach(int index in new[]{1,3})
                { stops[index].district="Downtown"; stops[index].pickupAllowed=false; }
            }
            var session=Session(); session.RaceEventActive=racing;
            session.SetWorldConditions(0,hour,TruckTaxiDemandWeather.Neutral);
            int eligible=session.CapturePassengerEligibility().Count(p=>p.PickupMetadataEligible);
            var counts=new Dictionary<string,int>(); string previous=null,category=null;
            int run=0,maxRun=0,raceCount=0;
            for(int i=0;i<1000;i++)
            {
                TruckTaxiDispatchTestClock.ExpireManualDeclineCooldown(session,hour);
                Assert.IsTrue(session.OfferRide(),session.LastOfferDiagnostics);
                Assert.IsTrue(TruckTaxiOfferEligibility.Pair(session.Pickup,session.Destination));
                if(district=="Speedway")
                {
                    Assert.AreEqual("Speedway",session.Pickup.district);
                    Assert.AreNotEqual("Speedway",session.Destination.district);
                }
                string id=session.Passenger.passengerId, nextCategory=session.Passenger.DispatchArchetype;
                Assert.AreNotEqual(previous,id,"Manual decline has viable alternative passengers.");
                counts.TryGetValue(id,out int old); counts[id]=old+1;
                if(old==2) Assert.GreaterOrEqual(counts.Count,Math.Ceiling(eligible*.9));
                run=category==nextCategory ? run+1 : 1; maxRun=Math.Max(maxRun,run);
                if(nextCategory=="racing") raceCount++;
                previous=id; category=nextCategory; session.DeclineRide();
            }
            Assert.AreEqual(eligible,counts.Count);
            Assert.IsTrue(counts.ContainsKey("space-hero"));
            Assert.LessOrEqual(maxRun,2);
            Assert.LessOrEqual(raceCount,racing ? 300 : 150);
            Assert.AreEqual(1000,session.OffersGenerated); Assert.AreEqual(1000,session.OffersDeclined);
            Assert.AreEqual(0,session.CapturePassengerSelectionState().passengers.Sum(p=>p.timesAccepted));
            Debug.Log($"Dispatch {hour}/{district}/{racing}: unique={counts.Count}/{eligible}, max offers={counts.Values.Max()}, archetype run={maxRun}, racing={raceCount/10f:F1}%");
        }

        [Test] public void ManualDeclineStartsImmediatelyAndExpiryKeepsDemandDelay()
        {
            config.passengers=new[]{Passenger("first"),Passenger("second")};
            var session=Session(); Assert.IsTrue(session.OfferRide());
            string old=session.Passenger.passengerId, pickup=session.Pickup.locationId,destination=session.Destination.locationId;
            session.DeclineRide(); Assert.IsTrue(session.IsGeneratingOffer); Assert.Zero(session.DispatchRemaining);
            Assert.IsTrue(session.OfferRide()); Assert.AreNotEqual(old,session.Passenger.passengerId);
            Assert.AreNotEqual(pickup,session.Pickup.locationId); Assert.AreNotEqual(destination,session.Destination.locationId);
            session.DebugSetOfferDuration(.1f); session.Tick(.2f,position,0,0,true);
            Assert.IsFalse(session.IsGeneratingOffer); Assert.Greater(session.DispatchRemaining,0);
            Assert.AreEqual(1,session.OffersDeclined); Assert.AreEqual(1,session.OffersExpired);
            Assert.Zero(session.IntercityPolicy.CompletedLocalStreak);
        }

        [Test] public void ExposureCountsOnlyOfferedAcceptedAndSuccessfullyCompletedRides()
        {
            var p=Passenger("stable"); config.passengers=new[]{p};
            var session=Session(); Assert.IsTrue(session.OfferRide()); session.DeclineRide();
            Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide()); session.FailRide("test"); session.ContinueShift();
            Assert.IsTrue(session.OfferRide()); Complete(session);
            var entry=session.CapturePassengerSelectionState().passengers.Single();
            Assert.AreEqual(3,entry.timesOffered); Assert.AreEqual(2,entry.timesAccepted); Assert.AreEqual(1,entry.timesCompleted);
            Assert.AreEqual(3,entry.lastOfferedRideIndex);
        }

        [Test] public void TenThousandCompletedRidesUseActualThirtyThreePercentRuleWithoutSatisfactionGate()
        {
            var p=Passenger("adult-female"); p.gender=TruckTaxiPassengerGender.Female;
            p.explicitlyAdult=true; p.minimumAdultAge=21; p.specialAppreciationEligible=true; p.baseSatisfaction=1;
            config.passengers=new[]{p}; var session=Session(330033,false); int offered=0;
            for(int i=0;i<10000;i++)
            {
                Assert.IsTrue(session.OfferRide(),session.LastOfferDiagnostics); Board(session);
                position=session.Destination.StopPosition; session.DebugComplete();
                if(session.State==TruckTaxiState.AppreciationOffer)
                {
                    offered++; float satisfaction=session.Satisfaction;
                    Assert.IsTrue(session.ChooseAppreciation(false)); Assert.AreEqual(satisfaction,session.Satisfaction);
                }
                Assert.AreEqual(TruckTaxiState.RideComplete,session.State); session.ContinueShift();
            }
            Assert.That(offered,Is.InRange(3100,3500)); Assert.AreEqual(10000,session.CompletedRides);
            Debug.Log($"Actual completed-ride appreciation offers: {offered}/10000");
        }

        [Test] public void ThousandGraphGatedOffersStayNearQuarterIntercityWithoutFixedFourthOffer()
        {
            config.intercityRideChance=.25f;
            config.passengers=new[]{Passenger("intercity")}; var session=Session(130013); int across=0,maxLocal=0;
            for(int i=0;i<1000;i++)
            {
                bool guarantee=session.LocalOfferStreak>=7;
                Assert.IsTrue(session.OfferRide(),session.LastOfferDiagnostics);
                if(guarantee) Assert.IsTrue(session.IsIntercityRide);
                if(session.IsIntercityRide) across++;
                maxLocal=Math.Max(maxLocal,session.LocalOfferStreak); Complete(session);
                session.ContinueShift();
            }
            Assert.That(across,Is.InRange(230,280)); Assert.That(maxLocal,Is.InRange(4,7));
            Debug.Log($"Actual routed offers: intercity={across}/1000, local streak={maxLocal}, {session.LastOfferDiagnostics}");
        }

        [Test] public void MetadataNeverInfersGenderFromNameModelOrVoiceAndMaleIsIneligible()
        {
            var p=Passenger("unresolved"); p.passengerName="Female sounding name";
            p.adultFemalePresentation=true; p.flirtatiousPresentation=true;
            p.explicitlyAdult=true; p.minimumAdultAge=30; p.specialAppreciationEligible=true;
            Assert.AreEqual(TruckTaxiPassengerGender.Unassigned,p.AuthoredGender); Assert.IsFalse(p.CanOfferAppreciation);
            p.casting=Create<TruckTaxiCastingProfile>(); p.casting.sexPresentation="Adult";
            Assert.AreEqual(TruckTaxiPassengerGender.Unassigned,p.AuthoredGender);
            p.casting.sexPresentation="Woman"; Assert.IsTrue(p.CanOfferAppreciation);
            p.gender=TruckTaxiPassengerGender.Male; Assert.IsFalse(p.CanOfferAppreciation);
            p.gender=TruckTaxiPassengerGender.Female; p.minimumAdultAge=20; Assert.IsFalse(p.CanOfferAppreciation);
        }

        [Test] public void DynamicEligibilitySnapshotsAreCopiesAndMalformedRestoreIsAtomic()
        {
            var policy=new TruckTaxiPassengerDispatchPolicy(); var random=new System.Random(12);
            var initial=new[]{new TruckTaxiPassengerCandidate("old","business",1)};
            policy.RecordOffered(policy.Select(initial,random,false),"business",5);
            var changed=new[]{initial[0],new TruckTaxiPassengerCandidate("new","commuter",1)};
            Assert.AreEqual("new",policy.Select(changed,random,false));
            var snapshot=policy.CaptureState(); Assert.IsTrue(new TruckTaxiPassengerDispatchPolicy().RestoreState(snapshot));
            snapshot.passengers[0].timesOffered=99; Assert.AreEqual(1,policy.Exposure("old").timesOffered);
            snapshot.passengers.Add(snapshot.passengers[0]); Assert.IsFalse(policy.RestoreState(snapshot));
            Assert.AreEqual(1,policy.Exposure("old").timesOffered);
        }

        [Test] public void SharedLapRequestDoesNotTurnCaptainIntoRacingPassenger()
        {
            var captain=UnityEditor.AssetDatabase.LoadAssetAtPath<PassengerProfile>("Assets/LWS/TruckTaxi/Passengers/Profiles/space-hero.asset");
            Assert.IsNotNull(captain); Assert.AreNotEqual("racing",captain.DispatchArchetype);
            Assert.AreEqual(TruckTaxiPassengerGender.Male,captain.AuthoredGender);
        }

        [TestCase(false,3)]
        [TestCase(true,6)]
        public void RacingCapWinsOverDeclineAvoidanceWhenOnlyOneNonRacingAlternativeExists(bool raceEvent,int cap)
        {
            var choices=new List<TruckTaxiPassengerCandidate> { new TruckTaxiPassengerCandidate("local","commuter",1) };
            for(int i=0;i<30;i++) choices.Add(new TruckTaxiPassengerCandidate("racing-"+i,"racing",100));
            var policy=new TruckTaxiPassengerDispatchPolicy(); var random=new System.Random(8001); string previous=null;
            var recent=new Queue<bool>();
            for(int i=0;i<1000;i++)
            {
                string id=policy.Select(choices,random,raceEvent,previous);
                bool racing=id!="local"; policy.RecordOffered(id,racing ? "racing" : "commuter",i);
                recent.Enqueue(racing); if(recent.Count>20) recent.Dequeue();
                Assert.LessOrEqual(recent.Count(value=>value),cap);
                previous=id;
            }
        }

        [Test] public void IntercitySnapshotPreservesGuaranteeAndDeclinesDoNotConsumeCooldown()
        {
            var policy=new TruckTaxiIntercityPolicy(1,2,3); var random=new System.Random(1);
            policy.CompleteRide(true);
            for(int i=0;i<20;i++) Assert.IsFalse(policy.WantsIntercity(random,out _));
            Assert.AreEqual(2,policy.LocalCooldownRemaining);
            policy.CompleteRide(false); policy.CompleteRide(false); policy.CompleteRide(false);
            var snapshot=policy.CaptureState(); var restored=new TruckTaxiIntercityPolicy(0,2,3);
            Assert.IsTrue(restored.RestoreState(snapshot)); Assert.IsTrue(restored.WantsIntercity(random,out bool guaranteed)); Assert.IsTrue(guaranteed);
            snapshot.completedLocalStreak=0; Assert.AreEqual(3,restored.CompletedLocalStreak);
        }

        [Test] public void MissingStableIdsAndZeroWeightsAreExplicitlyRejected()
        {
            var missing=Passenger(""); var disabled=Passenger("zero"); disabled.spawnWeight=0;
            config.passengers=new[]{missing,disabled}; var session=Session();
            Assert.IsFalse(session.OfferRide()); Assert.Zero(session.OffersGenerated);
            var audit=session.CapturePassengerEligibility(); Assert.IsTrue(audit.All(row=>!row.PickupMetadataEligible));
            Assert.That(audit[0].Reason,Does.Contain("stable ID")); Assert.That(audit[1].Reason,Does.Contain("weight"));
        }

        [Test] public void CustomObjectiveRequiresAuthoredMetadataAndRegisteredActivePair()
        {
            Assert.AreEqual(20,(int)TaxiRequestType.TakeALap); Assert.AreEqual(21,(int)TaxiRequestType.CustomItemAction);
            var definition=Create<PassengerRequestDefinition>(); definition.requestType=TaxiRequestType.CustomItemAction;
            var capabilities=new TruckTaxiObjectiveCapabilities();
            capabilities.Register(TruckTaxiObjectiveCapability.CustomItemActions,1);
            Assert.IsFalse(capabilities.Supports(definition));
            definition.objectiveId="authored.objective"; definition.requiredCustomEventId="designer.event"; definition.requiredItemStableId="designer.item";
            Assert.IsFalse(capabilities.Supports(definition),"A capability bit alone does not register an event.");
            Assert.IsTrue(capabilities.RegisterCustomItemAction("designer.event","other.item"));
            Assert.IsFalse(capabilities.Supports(definition));
            Assert.IsTrue(capabilities.RegisterCustomItemAction("designer.event","designer.item")); Assert.IsTrue(capabilities.Supports(definition));
            definition.enabledForSelection=false; Assert.IsFalse(capabilities.Supports(definition)); definition.enabledForSelection=true;
            capabilities.RegisterCustomItemAction("designer.event","designer.item",false); Assert.IsFalse(capabilities.Supports(definition));
            Assert.IsFalse(capabilities.RegisterCustomItemAction(" ","designer.item"));
        }
    }
}
