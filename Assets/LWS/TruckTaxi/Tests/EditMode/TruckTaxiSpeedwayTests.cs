using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiPolish")]
    public sealed class TruckTaxiSpeedwayTests
    {
        [Test]
        public void SpeedwayHasSixDistinctPersistentRideStopsAndValidRegionBounds()
        {
            Assert.That(TruckTaxiSpeedwayLayout.Stops, Has.Length.EqualTo(6));
            Assert.That(TruckTaxiSpeedwayLayout.Stops.Select(s => s.Id).Distinct().Count(), Is.EqualTo(6));
            foreach (var stop in TruckTaxiSpeedwayLayout.Stops)
                Assert.That(TruckTaxiSpeedwayLayout.RegionBounds.Contains(stop.Position), Is.True, stop.Id);
            Assert.That(TruckTaxiSpeedwayLayout.IsNearForLapOffer(TruckTaxiSpeedwayLayout.Center), Is.True);
            Assert.That(TruckTaxiSpeedwayLayout.IsNearForLapOffer(Vector3.zero), Is.False);
        }

        [Test]
        public void SpeedwayGraftIsIdempotentAndAllStopsRouteFromRegionalHighway()
        {
            var graph = new LwsRoadGraph { graphId = "speedway-test" };
            var highway = new LwsRoadNode { nodeId = "taxi.regional.2800", position = new Vector3(2800,.15f,0) };
            graph.nodes.Add(highway);
            TruckTaxiSpeedwayLayout.AddToGraph(graph);
            int nodes = graph.nodes.Count, edges = graph.edges.Count;
            TruckTaxiSpeedwayLayout.AddToGraph(graph);
            Assert.That(graph.nodes.Count, Is.EqualTo(nodes));
            Assert.That(graph.edges.Count, Is.EqualTo(edges));
            Assert.That(graph.Validate().IsValid, Is.True, graph.Validate().Summary);
            var routes = new TruckTaxiRouteDistanceService(graph);
            foreach (var stop in TruckTaxiSpeedwayLayout.Stops)
            {
                var leg = routes.Measure(highway.position, stop.Position);
                Assert.That(leg.Navigable, Is.True, stop.Name + ": " + leg.Source);
                Assert.That(leg.Meters, Is.GreaterThan(100), stop.Name);
            }
            var returnLeg = routes.Measure(TruckTaxiSpeedwayLayout.Stops[3].Position, highway.position);
            Assert.That(returnLeg.Navigable, Is.True, "Infield must route back to highway");
        }

        [Test]
        public void BankingRaisesOuterEdgeAndLowersInnerEdge()
        {
            var track = TruckTaxiSpeedwayLayout.TrackCenterline();
            Vector3 top = track[0];
            float outer = TruckTaxiSpeedwayLayout.BankOffset(top + Vector3.forward * 14, track);
            float inner = TruckTaxiSpeedwayLayout.BankOffset(top - Vector3.forward * 14, track);
            Assert.That(outer, Is.GreaterThan(7));
            Assert.That(inner, Is.LessThan(-7));
            Assert.That(TruckTaxiSpeedwayLayout.BankOffset(top, track), Is.EqualTo(0).Within(.1f));
        }

        [Test] public void TunnelHasTwoTruckLanesTrailerHeadroomAndGentleRampsBelowBanking()
        {
            Assert.That(TruckTaxiSpeedwayLayout.TunnelWidth, Is.GreaterThanOrEqualTo(12));
            Assert.That(TruckTaxiSpeedwayLayout.TunnelClearHeight, Is.GreaterThanOrEqualTo(6));
            var points=TruckTaxiSpeedwayLayout.TunnelCenterline();
            for(int i=1;i<points.Length;i++)
                Assert.That(Mathf.Abs(points[i].y-points[i-1].y) /
                    Vector3.ProjectOnPlane(points[i]-points[i-1],Vector3.up).magnitude, Is.LessThan(.08f));
            float innerTrack=10-14*Mathf.Tan(TruckTaxiSpeedwayLayout.BankingDegrees*Mathf.Deg2Rad);
            float roofTop=TruckTaxiSpeedwayLayout.TunnelRoadHeight+TruckTaxiSpeedwayLayout.TunnelClearHeight+.5f;
            Assert.That(innerTrack-roofTop, Is.GreaterThan(1));
        }

        [Test] public void TunnelGraphRemovesOldAtGradeShortcutAndRoutesBothDirections()
        {
            var graph=new LwsRoadGraph { graphId="tunnel-test" };
            graph.nodes.Add(new LwsRoadNode { nodeId="taxi.regional.2800",position=new Vector3(2800,0,0) });
            TruckTaxiSpeedwayLayout.AddToGraph(graph);
            Assert.IsFalse(graph.edges.Exists(e=>e.edgeId=="taxi.speedway.pit>taxi.speedway.infield"));
            foreach(string pair in new[] { "pit>taxi.speedway.tunnel.north", "tunnel.north>taxi.speedway.pit",
                "tunnel.north>taxi.speedway.tunnel.south", "tunnel.south>taxi.speedway.tunnel.north",
                "tunnel.south>taxi.speedway.infield", "infield>taxi.speedway.tunnel.south" })
                Assert.IsTrue(graph.edges.Exists(e=>e.edgeId=="taxi.speedway."+pair),pair);
        }

        [Test]
        public void LapRequiresOrderedCheckpointsAndReturnToStart()
        {
            var tracker = new TruckTaxiSpeedwayLapTracker();
            var points = TruckTaxiSpeedwayLayout.LapCheckpoints();
            tracker.Begin();
            Assert.That(tracker.Tick(points[1]), Is.False);
            Assert.That(tracker.NextCheckpointIndex, Is.Zero);
            for (int i = 0; i < points.Length; i++)
            {
                Assert.That(tracker.Tick(points[i]), Is.False, "checkpoint " + i);
                Assert.That(tracker.NextCheckpointIndex, Is.EqualTo((i + 1) % points.Length));
            }
            Assert.That(tracker.Tick(points[0]), Is.True);
            Assert.That(tracker.IsComplete, Is.True);
            Assert.That(tracker.Tick(points[0]), Is.False);
            tracker.Begin(); tracker.Cancel();
            Assert.That(tracker.Tick(points[0]), Is.False);
        }

        [Test]
        public void WallToolTargetsOnlyPrototypePerimeterGeometry()
        {
            Assert.That(TruckTaxiSpeedwayAuthoring.IsPrototypePerimeter("North barrier"), Is.True);
            Assert.That(TruckTaxiSpeedwayAuthoring.IsPrototypePerimeter("Townperimeterwall"), Is.True);
            Assert.That(TruckTaxiSpeedwayAuthoring.IsPrototypePerimeter("Movable construction barrier"), Is.False);
            Assert.That(TruckTaxiSpeedwayAuthoring.IsPrototypePerimeter("Oval safety rail"), Is.False);
        }

        [Test]
        public void LapRequestUsesExistingRacetrackCapabilityContract()
        {
            var request = ScriptableObject.CreateInstance<PassengerRequestDefinition>();
            try
            {
                request.requestType = TaxiRequestType.TakeALap;
                request.targetId = TruckTaxiSpeedwayLayout.LapStopId;
                request.target = 1;
                Assert.That(request.IsStop, Is.True);
                Assert.That(request.RequiredCapabilities & TruckTaxiObjectiveCapability.Racetrack,
                    Is.EqualTo(TruckTaxiObjectiveCapability.Racetrack));
            }
            finally { Object.DestroyImmediate(request); }
        }
    }
}
