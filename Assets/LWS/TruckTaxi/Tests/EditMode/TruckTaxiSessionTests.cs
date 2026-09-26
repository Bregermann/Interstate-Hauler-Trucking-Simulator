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
        [Test] public void EjectionPaysPenaltyOnceAndAllowsNextRide()
        {
            Board(); int signals=0; session.PassengerEjected+=()=>signals++;
            Assert.IsTrue(session.EjectPassenger()); long earnings=session.ShiftEarnings;
            Assert.AreEqual(TruckTaxiState.PassengerEjected,session.State);
            Assert.IsFalse(session.HasPassenger); Assert.IsFalse(session.EjectPassenger());
            Assert.AreEqual(1,signals); Assert.AreEqual(earnings,session.ShiftEarnings);
            Assert.AreEqual(0,session.CompletedRides); Assert.AreEqual(passenger.ejectionFarePenaltyCents,session.LastFare.Penalties);
            session.Tick(3.1f,Vector3.zero,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State); Assert.IsTrue(session.OfferRide());
        }
        [Test] public void NeverTipsPreservesFareButRemovesTip()
        {
            passenger.uniqueMechanics=new[]{TruckTaxiMechanic.NeverTips}; Board(); Arrive();
            Assert.AreEqual(0,session.LastFare.Tip); Assert.Greater(session.LastFare.Total,0);
        }
        [Test] public void PickupVisualUsesExactlyTheGameplayRadiusAndSpeed()
        {
            session.StartShift(); session.OfferRide(); session.AcceptRide();
            Vector3 center=session.Pickup.StopPosition; float radius=session.Pickup.detectionRadius;
            Assert.AreEqual(TruckTaxiPickupVisualState.Approaching,TruckTaxiPickupZoneVisualizer.Evaluate(session,center+Vector3.right*(radius+.1f),0));
            Assert.AreEqual(TruckTaxiPickupVisualState.Inside,TruckTaxiPickupZoneVisualizer.Evaluate(session,center+Vector3.right*(radius-.1f),0));
            Assert.AreEqual(TruckTaxiPickupVisualState.TooFast,TruckTaxiPickupZoneVisualizer.Evaluate(session,center,config.stoppedSpeed+.1f));
            session.Tick(.1f,center,0,0,true);
            Assert.AreEqual(TruckTaxiPickupVisualState.Boarding,TruckTaxiPickupZoneVisualizer.Evaluate(session,center,0));
            session.Tick(.1f,center+Vector3.right*(radius+1),0,0,true);
            Assert.AreEqual(TruckTaxiState.DrivingToPickup,session.State);
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
            Assert.IsNull(session.Offer);
        }
        [Test] public void OfferSnapshotIsCompleteBeforeNotificationAndAcceptedWithoutReroll()
        {
            session.StartShift();
            bool complete=false;
            session.Changed+=()=> { if(session.State==TruckTaxiState.RideOffered) complete=session.Offer!=null && session.Offer.Trip.Meters>0; };
            Assert.IsTrue(session.OfferRide()); Assert.IsTrue(complete);
            var offer=session.Offer;
            Assert.Greater(offer.EstimatedFareCents,config.baseFareCents);
            Assert.IsFalse(offer.Trip.Navigable); Assert.AreEqual("Straight-Line Fallback (no graph)",offer.Trip.Source);
            Assert.IsTrue(session.AcceptRide());
            Assert.AreSame(offer.Passenger,session.Passenger); Assert.AreSame(offer.Pickup,session.Pickup); Assert.AreSame(offer.Destination,session.Destination);
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
        [Test] public void PedestrianBonesCannotDuplicateEventScoreObjectiveOrReaction()
        {
            var request=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); objects.Add(request);
            request.requestType=TaxiRequestType.HitPedestrian; request.target=2;
            passenger.possibleRequests=new[]{request}; passenger.passengerId="passenger.test"; passenger.chaosAffinity=1;
            Board(); int events=0,contextEvents=0;
            session.DrivingEvent+=type=> { if(type==TaxiEventType.PedestrianHit) events++; };
            session.PedestrianImpact+=hit=>contextEvents++;
            float satisfaction=session.Satisfaction;
            for(int i=0;i<12;i++) session.RecordPedestrianHit("same-person",5,Vector3.forward*200,Vector3.one);
            Assert.AreEqual(1,events); Assert.AreEqual(1,contextEvents); Assert.AreEqual(1,session.PedestriansHit);
            Assert.AreEqual(1,session.Requests[0].Progress); Assert.AreEqual(config.pedestrianHitScore,session.ChaosScore);
            Assert.Greater(session.Satisfaction,satisfaction); Assert.AreEqual(1,session.TrackedCollisions);
            Assert.AreEqual("Hit 2 pedestrians",session.Requests[0].Description);
            Assert.AreEqual("passenger.test",session.LastPedestrianImpact.Value.PassengerId);
            Assert.AreEqual(session.CurrentRideId,session.LastPedestrianImpact.Value.RideId);
            Assert.AreEqual(Vector3.one,session.LastPedestrianImpact.Value.Position);
            session.RecordPedestrianHit("next-person",7,Vector3.forward*280,Vector3.zero);
            Assert.AreEqual(TaxiRequestState.Succeeded,session.Requests[0].State);
        }
        [Test] public void LowSpeedDoesNotConsumePedestrianHitGuard()
        {
            Board(); session.RecordPedestrianHit("person",1,Vector3.zero,Vector3.zero);
            Assert.Zero(session.PedestriansHit); Assert.Zero(session.ChaosScore);
            session.RecordPedestrianHit("person",2.5f,Vector3.forward*100,Vector3.zero);
            Assert.AreEqual(1,session.PedestriansHit); Assert.Greater(session.ChaosScore,0);
        }
        [TestCase(TaxiRequestType.HitPedestrian)]
        [TestCase(TaxiRequestType.RamTraffic)]
        [TestCase(TaxiRequestType.PropertyDamage)]
        public void ImpactRequirementsCannotBeAssignedAlongsideNoCollisions(TaxiRequestType type)
        {
            var safe=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); objects.Add(safe); safe.requestType=TaxiRequestType.NoCollisions;
            var impact=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); objects.Add(impact); impact.requestType=type;
            Assert.IsFalse(safe.IsCompatibleWith(impact)); Assert.IsFalse(impact.IsCompatibleWith(safe));
            passenger.possibleRequests=new[]{safe}; Board(); passenger.possibleRequests=new[]{impact};
            Assert.IsFalse(session.GenerateRequest()); Assert.AreEqual(1,session.Requests.Count);
            session.DebugResolveRequest(false); Assert.IsTrue(session.GenerateRequest());
        }
        [Test] public void AuthoredBehaviorRulesAreSymmetricAndExtensible()
        {
            var a=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); objects.Add(a);
            var b=ScriptableObject.CreateInstance<PassengerRequestDefinition>(); objects.Add(b);
            a.requiredBehavior=TaxiRequestBehavior.Offroad; b.forbiddenBehavior=TaxiRequestBehavior.Offroad;
            Assert.IsFalse(a.IsCompatibleWith(b)); Assert.IsFalse(b.IsCompatibleWith(a));
            b.forbiddenBehavior=TaxiRequestBehavior.None; Assert.IsTrue(a.IsCompatibleWith(b));
        }
        [Test] public void PedestrianImpulseIsDirectionalSpeedDependentFiniteAndCapped()
        {
            var tuning=new TruckTaxiPedestrianImpactSettings();
            var slow=tuning.CalculateImpulse(Vector3.forward*3);
            var fast=tuning.CalculateImpulse(Vector3.forward*20);
            Assert.Greater(fast.magnitude,slow.magnitude); Assert.Greater(slow.z,0);
            Assert.LessOrEqual(tuning.CalculateImpulse(Vector3.left*10000).magnitude,900.01f);
            Assert.AreEqual(Vector3.zero,tuning.CalculateImpulse(new Vector3(float.NaN,0,0)));
        }
        [Test] public void PlayerContentHasNaturalPedestrianWording()
        {
            foreach(var path in System.IO.Directory.EnumerateFiles("Assets/LWS/TruckTaxi/ScriptableObjects","*.asset",System.IO.SearchOption.AllDirectories))
                StringAssert.DoesNotContain("fictional pedestrian",System.IO.File.ReadAllText(path).ToLowerInvariant(),path);
            var request=UnityEditor.AssetDatabase.LoadAssetAtPath<PassengerRequestDefinition>("Assets/LWS/TruckTaxi/ScriptableObjects/Requests/HitPedestrian.asset");
            Assert.AreEqual("Hit a pedestrian",new TaxiRequestProgress(request,1).Description);
        }
        [Test] public void CityGraphValidAndRoutesAllStops()
        {
            var graph=Editor.TruckTaxiDemoBuilder.CreateGraph();
            Assert.IsTrue(graph.Validate().IsValid,graph.Validate().Summary);
            Assert.AreEqual(45,graph.nodes.Count); Assert.AreEqual(160,graph.edges.Count);
            var distances=new TruckTaxiRouteDistanceService(graph);
            Vector3 a=graph.nodes[0].position,b=graph.nodes[24].position;
            var leg=distances.Measure(a,b);
            Assert.IsTrue(leg.Navigable);
            Assert.Greater(leg.Meters,Vector3.Distance(a,b)+100,"Offer distance must follow the city streets, not the diagonal.");
            float sum=0;
            for(int i=1;i<leg.Points.Count;i++) sum+=Vector3.Distance(leg.Points[i-1],leg.Points[i]);
            Assert.That(leg.Meters,Is.EqualTo(sum).Within(.01));
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
