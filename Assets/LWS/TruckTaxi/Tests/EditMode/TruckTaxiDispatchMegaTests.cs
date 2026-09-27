using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiDispatch"), Category("TaxiMegaPass")]
    public sealed class TruckTaxiDispatchMegaTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private TruckTaxiConfiguration config;
        private LwsRoadGraph graph;
        private TruckTaxiRideLocation start, local, across, disconnected;
        private Vector3 player;
        private bool hadRidePreference;
        private int previousRidePreference;

        [SetUp] public void SetUp()
        {
            hadRidePreference=PlayerPrefs.HasKey(TruckTaxiSession.RideRequestsPreferenceKey);
            previousRidePreference=PlayerPrefs.GetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,1);
            config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); created.Add(config);
            config.minimumTripDistance=10;
            config.maximumTripDistance=1000;
            config.intercityMaximumTripDistance=2000;
            config.intercityRideChance=0;
            config.localOffersAfterIntercity=3;
            config.intercityDryStreakThreshold=5;
            config.normalDemandDelay=new Vector2(11,11);
            graph=new LwsRoadGraph { graphId="dispatch-mega-test" };
            AddNode("A",Vector3.zero);
            AddNode("B",new Vector3(0,0,600));
            AddNode("C",new Vector3(30,0,600));
            AddNode("D",new Vector3(30,0,0));
            AddNode("E",new Vector3(100,0,0));
            AddEdge("A","B"); AddEdge("B","C"); AddEdge("C","D");
            Assert.IsTrue(graph.Validate().IsValid,graph.Validate().Summary);
            start=Stop("start",Vector3.zero);
            local=Stop("local",new Vector3(0,0,600));
            across=Stop("across",new Vector3(30,0,0));
            disconnected=Stop("disconnected",new Vector3(100,0,0));
            start.dropoffAllowed=false;
            local.pickupAllowed=across.pickupAllowed=disconnected.pickupAllowed=false;
            disconnected.dropoffAllowed=false;
            player=Vector3.zero;
        }

        [TearDown] public void TearDown()
        {
            foreach(var item in created) UnityEngine.Object.DestroyImmediate(item);
            created.Clear();
            if(hadRidePreference) PlayerPrefs.SetInt(TruckTaxiSession.RideRequestsPreferenceKey,previousRidePreference);
            else PlayerPrefs.DeleteKey(TruckTaxiSession.RideRequestsPreferenceKey);
            PlayerPrefs.Save();
        }

        [Test] public void WorkAreaFiltersPickupOnlyAndAllowsDeliveryOutsideCircle()
        {
            var session=Session();
            var area=Area(start.StopPosition,50);
            Assert.IsTrue(session.SetWorkArea(area));
            Assert.IsTrue(session.OfferRide(NewPassenger("inside")));
            Assert.AreSame(start,session.Pickup);
            Assert.AreSame(local,session.Destination);
            Assert.IsFalse(area.Contains(session.Destination.StopPosition));
            session.DeclineRide();

            start.pickupAllowed=false;
            local.pickupAllowed=true;
            Assert.IsFalse(session.OfferRide(NewPassenger("outside")));
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsNull(session.Offer);
        }

        [Test] public void PickupInsideCircleStillMustPassExistingRoutedHardRange()
        {
            start.pickupAllowed=false;
            across.pickupAllowed=true;
            config.pickupHardMaximumSeconds=20;
            var session=Session();
            Assert.IsTrue(session.SetWorkArea(Area(across.StopPosition,50)));
            Assert.Less(Vector3.Distance(player,across.StopPosition),50);
            Assert.Greater(new TruckTaxiRouteDistanceService(graph).Measure(player,across.StopPosition).Meters,
                config.pickupHardMaximumSeconds*config.pickupReasonableSpeedMetersPerSecond);
            Assert.IsFalse(session.OfferRide(NewPassenger("detour")));
            Assert.Greater(session.LastOfferRouteQueries,0);
            Assert.IsNull(session.Offer);
        }

        [Test] public void ChangingAreaDeclinesAnOutOfAreaPendingPickup()
        {
            var session=Session();
            Assert.IsTrue(session.OfferRide(NewPassenger("pending")));
            Assert.AreSame(start,session.Pickup);
            Assert.IsTrue(session.SetWorkArea(Area(local.StopPosition,50)));
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsNull(session.Offer);
            Assert.Zero(session.CompletedRides);
            Assert.Zero(session.IntercityPolicy.CompletedLocalStreak);
        }

        [Test] public void WorkAreaSnapshotsAreCopiesAndRepeatedRestoreKeepsTheSameArea()
        {
            var session=Session();
            var original=Area(start.StopPosition,75);
            original.label="HOME";
            Assert.IsTrue(session.SetWorkArea(original));
            var snapshot=session.CaptureWorkArea();
            original.center=local.StopPosition;
            Assert.AreEqual(Vector3.zero,snapshot.center);
            snapshot.center=local.StopPosition;
            Assert.AreEqual(Vector3.zero,session.WorkArea.center);
            snapshot.center=Vector3.zero;
            Assert.IsTrue(session.RestoreWorkArea(snapshot));
            Assert.IsTrue(session.RestoreWorkArea(snapshot));
            Assert.AreEqual(snapshot.center,session.CaptureWorkArea().center);
            Assert.AreEqual(snapshot.radius,session.WorkArea.radius);
            Assert.AreEqual("HOME",session.WorkArea.label);
            Assert.IsFalse(session.RestoreWorkArea(Area(Vector3.zero,20)));
            Assert.AreEqual(75,session.WorkArea.radius);
        }

        [Test] public void ChangingWorkAreaDoesNotTeleportARepeatPassenger()
        {
            var passenger=NewPassenger("repeat");
            var session=Session();
            Assert.IsTrue(session.OfferRide(passenger));
            CompleteOfferedRide(session);
            Assert.AreEqual(local.locationId,session.RideHistory[0].DestinationId);
            Assert.IsTrue(session.TryGetPassengerContinuity(passenger.passengerId,out var last,out _,out _));
            Assert.AreEqual(local.locationId,last);
            session.ContinueShift();
            local.pickupAllowed=true;
            start.dropoffAllowed=true;
            Assert.IsTrue(session.SetWorkArea(Area(start.StopPosition,50)));
            Assert.IsFalse(session.OfferRide(passenger));
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsTrue(session.TryGetPassengerContinuity(passenger.passengerId,out last,out _,out _));
            Assert.AreEqual(local.locationId,last);
            Assert.IsTrue(session.SetWorkArea(Area(local.StopPosition,50)));
            Assert.IsTrue(session.OfferRide(passenger));
            Assert.AreSame(local,session.Pickup);
            Assert.IsTrue(session.IsRepeatPassenger);
        }

        [Test] public void IntercityPolicyHasTenPercentRandomChanceAfterThreeCompletedLocalRides()
        {
            var policy=new TruckTaxiIntercityPolicy(.1f,3,5);
            policy.CompleteRide(true);
            var random=new System.Random(433);
            for(int i=0;i<3;i++)
            {
                Assert.IsFalse(policy.WantsIntercity(random,out var guaranteed));
                Assert.IsFalse(guaranteed);
                policy.CompleteRide(false);
            }
            int randomSelections=0;
            for(int i=0;i<1000;i++)
            {
                if(policy.WantsIntercity(random,out var guaranteed)) randomSelections++;
                Assert.IsFalse(guaranteed);
            }
            Assert.That(randomSelections,Is.InRange(70,130),"Seeded 1,000 eligible draws should remain near 10%.");
        }

        [Test] public void ThousandCompletedRidePolicySimulationReportsDistributionAndBoundsLocalStreak()
        {
            var policy=new TruckTaxiIntercityPolicy(.1f,3,5);
            var random=new System.Random(9091);
            int localRides=0,randomIntercity=0,guaranteedIntercity=0,maxLocalStreak=0,declines=0,invalidPaths=0;
            for(int ride=0;ride<1000;ride++)
            {
                if(ride%11==0)
                {
                    int before=policy.CompletedLocalStreak;
                    policy.WantsIntercity(random,out _);
                    declines++;
                    Assert.AreEqual(before,policy.CompletedLocalStreak,"Declines do not complete a ride.");
                }
                if(ride%17==0)
                {
                    int before=policy.CompletedLocalStreak;
                    policy.WantsIntercity(random,out _);
                    invalidPaths++;
                    Assert.AreEqual(before,policy.CompletedLocalStreak,"An unroutable attempt cannot consume the guarantee.");
                }
                bool intercity=policy.WantsIntercity(random,out bool guaranteed);
                policy.CompleteRide(intercity);
                if(intercity)
                {
                    if(guaranteed) guaranteedIntercity++;
                    else randomIntercity++;
                }
                else localRides++;
                maxLocalStreak=Math.Max(maxLocalStreak,policy.CompletedLocalStreak);
                Assert.LessOrEqual(policy.CompletedLocalStreak,5,
                    "Every completed attempt in this simulation has a valid eligible intercity route.");
            }
            Debug.Log($"Taxi dispatch 1,000 rides: local={localRides}, random intercity={randomIntercity}, " +
                $"guaranteed intercity={guaranteedIntercity}, max local streak={maxLocalStreak}, " +
                $"declines={declines}, invalid paths={invalidPaths}.");
            Assert.AreEqual(1000,localRides+randomIntercity+guaranteedIntercity);
            Assert.Greater(randomIntercity,0);
            Assert.Greater(guaranteedIntercity,0);
            Assert.AreEqual(5,maxLocalStreak);
            Assert.Greater(declines,0);
            Assert.Greater(invalidPaths,0);
        }

        [Test] public void CompletedLocalStreakGuaranteesNextValidIntercityDespiteDeclinesAndInvalidPath()
        {
            var session=Session();
            for(int i=0;i<5;i++)
            {
                Assert.IsTrue(session.OfferRide(NewPassenger("local."+i)));
                Assert.IsFalse(session.IsIntercityRide);
                CompleteOfferedRide(session);
                Assert.AreEqual(i+1,session.IntercityPolicy.CompletedLocalStreak);
                session.ContinueShift();
            }
            Assert.IsTrue(session.IntercityPolicy.GuaranteePending);
            across.dropoffAllowed=false;
            disconnected.dropoffAllowed=true;
            Assert.IsFalse(new TruckTaxiRouteDistanceService(graph).BetweenStops(start,disconnected).Navigable);
            Assert.IsFalse(session.OfferRide(NewPassenger("invalid")));
            Assert.AreEqual("GUARANTEE",session.LastIntercitySelection);
            Assert.IsTrue(session.IntercityPolicy.GuaranteePending);
            Assert.AreEqual(5,session.IntercityPolicy.CompletedLocalStreak);
            Assert.That(session.LastDispatchDelay,Is.EqualTo(11).Within(.001f),
                "A failed offer resumes the normal demand delay.");

            disconnected.dropoffAllowed=false;
            across.dropoffAllowed=true;
            Assert.IsTrue(session.OfferRide(NewPassenger("declined")));
            Assert.IsTrue(session.IsIntercityRide);
            Assert.AreEqual("GUARANTEE",session.LastIntercitySelection);
            session.DeclineRide();
            Assert.IsTrue(session.IntercityPolicy.GuaranteePending);
            Assert.AreEqual(5,session.IntercityPolicy.CompletedLocalStreak);

            Assert.IsTrue(session.OfferRide(NewPassenger("completed")));
            Assert.AreSame(across,session.Destination);
            Assert.IsTrue(session.IsIntercityRide);
            CompleteOfferedRide(session);
            Assert.AreEqual(0,session.IntercityPolicy.CompletedLocalStreak);
            Assert.AreEqual(3,session.LocalOffersBeforeIntercity);
            Assert.IsFalse(session.IntercityPolicy.GuaranteePending);
        }

        [Test] public void FareBonusComponentsAreAdditiveCapturedAndWeatherIsNotDoubled()
        {
            config.intercityRideChance=1;
            config.intercityFareMultiplier=1.2f;
            var session=Session();
            session.SetWorldConditions(0,12,TruckTaxiDemandWeather.Storm);
            session.FareModifiersProvider=(pickup,destination)=>new TruckTaxiFareModifiers(1.3f,1.08f,1.1f,"EVENT","OTHER");
            Assert.IsTrue(session.OfferRide(NewPassenger("fare")));
            Assert.IsTrue(session.IsIntercityRide);
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(1.75f).Within(.001f));
            Assert.That(session.AcceptedFareModifiers.EventMultiplier,Is.EqualTo(1.3f).Within(.001f));
            Assert.That(session.AcceptedFareModifiers.WeatherMultiplier,Is.EqualTo(1.15f).Within(.001f));
            Assert.That(session.AcceptedFareModifiers.OtherMultiplier,Is.EqualTo(1.1f).Within(.001f));
            Assert.That(session.IntercityFareBonusMultiplier,Is.EqualTo(1.2f).Within(.001f));
            Assert.IsTrue(session.AcceptRide());
            session.FareModifiersProvider=(pickup,destination)=>new TruckTaxiFareModifiers(4,4,4);
            session.SetWorldConditions(10,23,TruckTaxiDemandWeather.Blizzard);
            config.intercityFareMultiplier=4;
            var fare=session.EstimateFare();
            Assert.AreEqual(500,fare.Base);
            Assert.AreEqual(150,fare.EventBonus);
            Assert.AreEqual(75,fare.WeatherBonus,"Storm and provider weather are one captured component.");
            Assert.AreEqual(50,fare.OtherBonus);
            Assert.AreEqual(100,fare.IntercityBonus);
            Assert.AreEqual(875,fare.Total);
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(1.75f).Within(.001f));
        }

        [Test] public void RequestsOffOrSuppressedDoNotAutoDispatch()
        {
            var session=Session();
            session.SetRideRequestsEnabled(false);
            session.Tick(100,player,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsNull(session.Offer);
            Assert.IsFalse(session.IsGeneratingOffer);
            Assert.IsFalse(session.RequestRideOffer());

            session.SetRideRequestsEnabled(true);
            var owner=new object();
            session.AcquireOfferSuppression(owner);
            session.Tick(100,player,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsNull(session.Offer);
            Assert.IsFalse(session.IsGeneratingOffer);
            Assert.IsFalse(session.RequestRideOffer());
            session.ReleaseOfferSuppression(owner);
            Assert.Greater(session.DispatchRemaining,0);
            Assert.IsTrue(session.OfferRide(NewPassenger("resumed")));
        }

        private TruckTaxiSession Session()
        {
            var session=new TruckTaxiSession(config,new[]{start,local,across,disconnected},17,
                new TruckTaxiRouteDistanceService(graph),()=>player);
            session.RegionResolver=position=>position.x>=20 ? "town.02" : "town.01";
            session.StartShift();
            return session;
        }

        private PassengerProfile NewPassenger(string id)
        {
            var passenger=ScriptableObject.CreateInstance<PassengerProfile>(); created.Add(passenger);
            passenger.passengerId=id;
            passenger.basePatience=1000;
            return passenger;
        }

        private static TruckTaxiRideWorkArea Area(Vector3 center,float radius) => new TruckTaxiRideWorkArea
        { mode=TruckTaxiWorkAreaMode.Custom,center=center,radius=radius,label="TEST AREA" };

        private static void CompleteOfferedRide(TruckTaxiSession session)
        {
            Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(2,session.Pickup.StopPosition,0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
            session.DebugComplete();
            Assert.AreEqual(TruckTaxiState.RideComplete,session.State);
        }

        private TruckTaxiRideLocation Stop(string id,Vector3 position)
        {
            var go=new GameObject(id); created.Add(go);
            go.transform.position=position;
            var stop=go.AddComponent<TruckTaxiRideLocation>(); stop.locationId=id;
            return stop;
        }

        private void AddNode(string id,Vector3 position)
        { graph.nodes.Add(new LwsRoadNode { nodeId=id,position=position }); }

        private void AddEdge(string from,string to)
        {
            Vector3 a=graph.nodes.Find(node=>node.nodeId==from).position;
            Vector3 b=graph.nodes.Find(node=>node.nodeId==to).position;
            float length=Vector3.Distance(a,b);
            var edge=new LwsRoadEdge { edgeId=from+to,fromNodeId=from,toNodeId=to,
                distanceMeters=length,travelCost=length };
            edge.samples.Add(new LwsRoadSample { position=a,forward=(b-a).normalized,distanceFromStartMeters=0 });
            edge.samples.Add(new LwsRoadSample { position=b,forward=(b-a).normalized,distanceFromStartMeters=length });
            graph.edges.Add(edge);
        }
    }
}
