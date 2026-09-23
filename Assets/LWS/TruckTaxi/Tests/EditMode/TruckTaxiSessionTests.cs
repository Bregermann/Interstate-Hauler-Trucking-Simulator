using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public class TruckTaxiSessionTests
    {
        private readonly List<Object> objects = new List<Object>();
        private TruckTaxiConfiguration config;
        private PassengerProfile passenger;
        private TruckTaxiSession session;
        [SetUp] public void SetUp()
        {
            config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); objects.Add(config);
            passenger=ScriptableObject.CreateInstance<PassengerProfile>(); objects.Add(passenger);
            passenger.passengerName="Test commuter"; passenger.basePatience=1000; passenger.requestDifficultyRange=Vector2.one;
            config.passengers=new[]{passenger}; config.minimumTripDistance=10; config.maximumTripDistance=1000;
            var locations=new List<TruckTaxiRideLocation>();
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Stop "+i); objects.Add(go);
                var stop=go.AddComponent<TruckTaxiRideLocation>(); stop.locationId="stop."+i; go.transform.position=Vector3.right*i*100; locations.Add(stop);
            }
            session=new TruckTaxiSession(config,locations,17);
        }
        [TearDown] public void TearDown() { foreach(var o in objects) Object.DestroyImmediate(o); objects.Clear(); }
        private void Board()
        {
            session.StartShift(); Assert.IsTrue(session.OfferRide()); Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(2,session.Pickup.StopPosition,0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
        }
        private void Arrive()
        {
            session.Tick(.1f,session.Destination.StopPosition,0,0,true);
            session.Tick(2,session.Destination.StopPosition,0,0,true);
        }
        [Test] public void ThreeConsecutiveRidesPayOnceEach()
        {
            for(int i=0;i<3;i++)
            {
                Board(); Arrive();
                Assert.AreEqual(i+1,session.CompletedRides);
                long money=session.ShiftEarnings; session.DebugComplete(); Assert.AreEqual(money,session.ShiftEarnings);
                session.ContinueShift();
            }
        }
        [Test] public void CannotFinishBeforePickupOrAcceptSecondRide()
        {
            session.StartShift(); session.OfferRide(); session.DebugComplete();
            Assert.AreEqual(TruckTaxiState.RideOffered,session.State);
            session.AcceptRide(); Assert.IsFalse(session.AcceptRide()); Assert.IsFalse(session.OfferRide());
            Assert.AreEqual(0,session.CompletedRides);
        }
        [Test] public void PickupAndDropoffRequireAStop()
        {
            session.StartShift(); session.OfferRide(); session.AcceptRide();
            session.Tick(3,session.Pickup.StopPosition,10,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToPickup,session.State);
            session.Tick(.1f,session.Pickup.StopPosition,0,0,true);
            session.Tick(2,session.Pickup.StopPosition,0,0,true);
            session.Tick(3,session.Destination.StopPosition,10,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToDestination,session.State);
        }
        [Test] public void StopsDistinctAndOffersExpire()
        {
            session.StartShift(); session.OfferRide();
            Assert.AreNotEqual(session.Pickup.locationId,session.Destination.locationId);
            session.Tick(config.offerDuration+1,Vector3.zero,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
        }
        [Test] public void SameCollisionHasPersonalitySpecificSatisfaction()
        {
            Board(); passenger.chaosAffinity=-1; session.RecordEvent(TaxiEventType.TrafficRam,"car",5);
            float normal=session.Satisfaction;
            passenger.chaosAffinity=1; session.RecordEvent(TaxiEventType.TrafficRam,"car2",5);
            Assert.Greater(session.Satisfaction,normal);
        }
        [Test] public void NoScoringAfterRideOrBeforeBoarding()
        {
            session.StartShift(); session.OfferRide(); session.RecordEvent(TaxiEventType.TrafficRam,"car",10);
            Assert.Zero(session.ChaosScore);
            session.AcceptRide(); session.DebugBoard(); session.Tick(2,session.Pickup.StopPosition,0,0,true);
            Arrive(); int score=session.ChaosScore; session.RecordEvent(TaxiEventType.TrafficRam,"car2",10); Assert.AreEqual(score,session.ChaosScore);
        }
        [TestCase(TaxiRequestType.FastDelivery)]
        [TestCase(TaxiRequestType.Shortcut)]
        [TestCase(TaxiRequestType.RamTraffic)]
        [TestCase(TaxiRequestType.HitPedestrian)]
        [TestCase(TaxiRequestType.PropertyDamage)]
        [TestCase(TaxiRequestType.Offroad)]
        [TestCase(TaxiRequestType.SmoothRide)]
        [TestCase(TaxiRequestType.NoCollisions)]
        [TestCase(TaxiRequestType.MaximumChaos)]
        [TestCase(TaxiRequestType.ScenicRoute)]
        [TestCase(TaxiRequestType.NearMiss)]
        public void AllRequestTypesCanComplete(TaxiRequestType type)
        {
            var request=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); objects.Add(request);
            request.requestType=type; request.target=1; request.timer=100; passenger.possibleRequests=new[]{request};
            Board();
            if(type==TaxiRequestType.Offroad) session.Tick(1,session.Pickup.StopPosition,3,0,false);
            else if(type==TaxiRequestType.MaximumChaos) session.RecordEvent(TaxiEventType.Shortcut,"cut");
            else if(type==TaxiRequestType.Shortcut) session.RecordEvent(TaxiEventType.Shortcut,"cut");
            else if(type==TaxiRequestType.RamTraffic) session.RecordEvent(TaxiEventType.TrafficRam,"car",5);
            else if(type==TaxiRequestType.HitPedestrian) session.RecordEvent(TaxiEventType.PedestrianHit,"npc",5);
            else if(type==TaxiRequestType.PropertyDamage) session.RecordEvent(TaxiEventType.PropDamage,"prop",5);
            else if(type==TaxiRequestType.ScenicRoute) session.RecordEvent(TaxiEventType.ScenicPoint,"park");
            else if(type==TaxiRequestType.NearMiss) session.RecordEvent(TaxiEventType.NearMiss,"car");
            Arrive();
            Assert.AreEqual(TaxiRequestState.Succeeded,session.Requests[0].State);
            Assert.Greater(session.LastFare.Requests,0);
        }
        [Test] public void ShortcutDialogueUsesAuthoredOverrideOnlyDuringRide()
        {
            session.RecordEvent(TaxiEventType.Shortcut,"cut",0,300,"Too early");
            Assert.IsNull(session.Reaction);
            Board();
            session.RecordEvent(TaxiEventType.Shortcut,"cut",0,300,"A creative turn!");
            Assert.AreEqual("A creative turn!",session.Reaction);
            session.RecordEvent(TaxiEventType.ScenicPoint,"park",0,0,"Lovely view.");
            Assert.AreEqual("Lovely view.",session.Reaction);
            session.RecordEvent(TaxiEventType.Shortcut,"other",0,300);
            Assert.AreEqual(passenger.shortcutReaction,session.Reaction);
        }
        [Test] public void RepeatedTargetCannotFarmOneRequest()
        {
            var r=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); objects.Add(r); r.requestType=TaxiRequestType.RamTraffic; r.target=2;
            passenger.possibleRequests=new[]{r}; Board();
            session.RecordEvent(TaxiEventType.TrafficRam,"same",5); session.RecordEvent(TaxiEventType.TrafficRam,"same",5);
            Assert.AreEqual(1,session.Requests[0].Progress);
        }
        [Test] public void DrivingSandboxExcludesCareerPersistenceAndJobs()
        {
            var registry=LwsApplicationBootstrap.CreateDefaultRegistry(false);
            Assert.IsFalse(registry.TryGet(out ILwsSaveService save));
            Assert.IsFalse(registry.TryGet(out ILwsActiveJobService jobs));
            Assert.IsTrue(registry.TryGet(out ILwsNavigationService navigation));
        }
        [Test] public void CityGraphValidAndRoutesAllStops()
        {
            var graph=Editor.TruckTaxiDemoBuilder.CreateGraph();
            Assert.IsTrue(graph.Validate().IsValid,graph.Validate().Summary);
            Assert.AreEqual(45,graph.nodes.Count); Assert.AreEqual(160,graph.edges.Count);
            var planner=new LwsRoutePlanner(LwsNavigationTuning.Default());
            for(int from=0;from<20;from++) for(int to=0;to<20;to++)
            {
                if(from==to) continue;
                var result=planner.PlanRoute(new LwsRouteRequest{originNodeId="taxi.stop."+from.ToString("00"),
                    destinationNodeId="taxi.stop."+to.ToString("00"),truckRouteRequired=true},graph);
                Assert.IsTrue(result.succeeded,result.message);
            }
        }
        [Test] public void AuthoredDemoPassengersAndRequestsArePopulated()
        {
            var content=UnityEditor.AssetDatabase.LoadAssetAtPath<TruckTaxiConfiguration>(
                "Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_DemoConfiguration.asset");
            Assert.AreEqual(10,content.passengers.Length);
            Assert.GreaterOrEqual(content.requests.Length,11);
            var types=new HashSet<TaxiRequestType>();
            foreach(var profile in content.passengers)
            {
                Assert.IsNotEmpty(profile.passengerName,profile.name);
                Assert.IsNotEmpty(profile.personality,profile.name);
                Assert.Greater(profile.possibleRequests.Length,0,profile.name);
            }
            foreach(var request in content.requests)
            { Assert.IsNotEmpty(request.description,request.name); types.Add(request.requestType); }
            Assert.AreEqual(11,types.Count);
        }
    }
}
