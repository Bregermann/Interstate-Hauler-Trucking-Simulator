using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiRegional")]
    public sealed class TruckTaxiRegionalTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private TruckTaxiConfiguration config;
        private PassengerProfile passenger;
        private TruckTaxiRideLocation start, near, across;
        private LwsRoadGraph graph;
        private Vector3 player;

        [SetUp] public void SetUp()
        {
            config=ScriptableObject.CreateInstance<TruckTaxiConfiguration>(); created.Add(config);
            passenger=ScriptableObject.CreateInstance<PassengerProfile>(); created.Add(passenger);
            passenger.passengerId="regional.passenger";
            config.passengers=new[]{passenger};
            config.minimumTripDistance=10; config.maximumTripDistance=1000;
            graph=new LwsRoadGraph { graphId="taxi-regional-test" };
            graph.nodes.Add(new LwsRoadNode { nodeId="A", position=Vector3.zero });
            graph.nodes.Add(new LwsRoadNode { nodeId="B", position=new Vector3(0,0,600) });
            graph.nodes.Add(new LwsRoadNode { nodeId="C", position=new Vector3(30,0,600) });
            graph.nodes.Add(new LwsRoadNode { nodeId="D", position=new Vector3(30,0,0) });
            AddEdge("A","B"); AddEdge("B","C"); AddEdge("C","D");
            Assert.IsTrue(graph.Validate().IsValid,graph.Validate().Summary);
            start=Stop("start",graph.nodes[0].position);
            near=Stop("near",graph.nodes[1].position);
            across=Stop("across",graph.nodes[3].position);
            player=Vector3.zero;
        }

        [TearDown] public void TearDown()
        {
            foreach(var item in created) UnityEngine.Object.DestroyImmediate(item);
            created.Clear();
        }

        [Test] public void DetourBeyondHardEtaDoesNotBecomeAnOfferAndRetriesLater()
        {
            start.pickupAllowed=near.pickupAllowed=false;
            config.pickupHardMaximumSeconds=60;
            var session=Session();
            Assert.Less(Vector3.Distance(player,across.StopPosition),50);
            Assert.Greater(new TruckTaxiRouteDistanceService(graph).Measure(player,across.StopPosition).Meters,1000);
            Assert.IsFalse(session.OfferRide());
            session.Tick(config.rideFrequency+.1f,player,0,0,true);
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsNull(session.Offer);
            Assert.Less(session.StateAge,.01f);
            near.pickupAllowed=true;
            session.Tick(config.rideFrequency+.1f,player,0,0,true);
            Assert.AreEqual(TruckTaxiState.RideOffered,session.State);
            Assert.AreSame(near,session.Pickup);
        }

        [Test] public void IntercityChanceSelectsAcrossRegionAndUsesExistingFareMultiplier()
        {
            start.pickupAllowed=across.pickupAllowed=false;
            near.dropoffAllowed=false;
            config.intercityRideChance=1;
            config.intercityFareMultiplier=1.25f;
            var session=Session();
            Assert.IsTrue(session.OfferRide());
            Assert.AreSame(near,session.Pickup);
            Assert.AreSame(across,session.Destination);
            Assert.IsTrue(session.IsIntercityRide);
            Assert.That(session.Offer.ToPickup.Meters/config.pickupReasonableSpeedMetersPerSecond,
                Is.InRange(config.pickupTargetMinimumSeconds,config.pickupTargetMaximumSeconds));
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(1.25f).Within(.001f));
            Assert.IsTrue(session.AcceptRide());
            Assert.AreEqual((long)Math.Round(config.baseFareCents*session.DemandFareMultiplier),session.EstimateFare().Base);
        }

        [Test] public void ZeroIntercityChanceKeepsLocalDestinationWhenAvailable()
        {
            start.pickupAllowed=across.pickupAllowed=false;
            near.dropoffAllowed=false;
            config.intercityRideChance=0;
            var session=Session();
            Assert.IsTrue(session.OfferRide());
            Assert.AreSame(start,session.Destination);
            Assert.IsFalse(session.IsIntercityRide);
            Assert.That(session.DemandFareMultiplier,Is.EqualTo(1).Within(.001f));
        }

        [Test] public void RepeatPassengerAtDistantRealLocationIsIneligible()
        {
            start.pickupAllowed=across.pickupAllowed=false;
            near.dropoffAllowed=false;
            config.intercityRideChance=1;
            var session=Session();
            Assert.IsTrue(session.OfferRide(passenger));
            Assert.IsTrue(session.AcceptRide());
            session.Tick(.1f,near.StopPosition,0,0,true);
            session.Tick(2,near.StopPosition,0,0,true);
            session.DebugComplete();
            Assert.AreEqual(TruckTaxiState.RideComplete,session.State);
            session.ContinueShift();
            across.pickupAllowed=true;
            config.pickupHardMaximumSeconds=60;
            Assert.IsFalse(session.OfferRide(passenger));
            Assert.AreEqual(TruckTaxiState.Available,session.State);
            Assert.IsTrue(session.TryGetPassengerContinuity(passenger.passengerId,out var locationId,out _,out _));
            Assert.AreEqual(across.locationId,locationId);
        }

        private TruckTaxiSession Session()
        {
            var session=new TruckTaxiSession(config,new[]{start,near,across},17,
                new TruckTaxiRouteDistanceService(graph),()=>player);
            session.RegionResolver=position=>position.x>=20 ? "town.02" : "town.01";
            session.StartShift();
            return session;
        }

        private TruckTaxiRideLocation Stop(string id,Vector3 position)
        {
            var go=new GameObject(id); created.Add(go);
            go.transform.position=position;
            var stop=go.AddComponent<TruckTaxiRideLocation>(); stop.locationId=id;
            return stop;
        }

        private void AddEdge(string from,string to)
        {
            Vector3 a=graph.nodes.Find(node=>node.nodeId==from).position;
            Vector3 b=graph.nodes.Find(node=>node.nodeId==to).position;
            float length=Vector3.Distance(a,b);
            var edge=new LwsRoadEdge { edgeId=from+to, fromNodeId=from, toNodeId=to,
                distanceMeters=length, travelCost=length };
            edge.samples.Add(new LwsRoadSample { position=a, forward=(b-a).normalized, distanceFromStartMeters=0 });
            edge.samples.Add(new LwsRoadSample { position=b, forward=(b-a).normalized, distanceFromStartMeters=length });
            graph.edges.Add(edge);
        }
    }
}
