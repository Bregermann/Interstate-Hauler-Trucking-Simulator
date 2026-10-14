using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using LWS.TruckTaxi.Editor;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    [Category("TaxiEdenboroughPopulation")]
    public sealed class TruckTaxiEdenboroughPopulationAuthoringTests
    {
        private static T Invoke<T>(string name, params object[] args) => (T)typeof(TruckTaxiEdenboroughAuthoring)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        private static LwsTrafficLaneDefinition[] Lanes() => Invoke<LwsTrafficLaneDefinition[]>("CreateTrafficLoops");
        private static T Field<T>(object plan, string name) => (T)plan.GetType().GetField(name).GetValue(plan);

        [Test] public void WholeCircuitsAreValidFiniteUniqueAndContinuousIncludingLogicalWrap()
        {
            var lanes = Lanes(); Assert.AreEqual(24, lanes.Length);
            var ids = new HashSet<string>();
            foreach (var lane in lanes)
            {
                Assert.IsTrue(lane.Validate(out string reason), reason); Assert.IsTrue(ids.Add(lane.laneId));
                Assert.GreaterOrEqual(lane.laneWidthMeters, TruckTaxiEdenboroughLayout.LocalLaneWidth);
                Assert.LessOrEqual(lane.laneWidthMeters, TruckTaxiEdenboroughLayout.HighwayLaneWidth);
                var points = lane.centerline; float length = 0;
                for (int i = 0; i < points.Length; i++)
                {
                    Assert.IsTrue(float.IsFinite(points[i].x) && float.IsFinite(points[i].y) && float.IsFinite(points[i].z));
                    Assert.IsTrue(TruckTaxiEdenboroughLayout.WorldBounds.Contains(points[i]));
                    float distance = Vector3.Distance(points[i], points[(i + 1) % points.Length]);
                    Assert.That(distance, Is.GreaterThan(.005f).And.LessThanOrEqualTo(8.05f), lane.laneId);
                    length += distance;
                }
                Assert.That(lane.lengthMeters, Is.EqualTo(length).Within(.01f));
                Assert.LessOrEqual(Vector3.Distance(points[points.Length - 2], points[1]), 24.05f, "Logical endpoint guards must not wrap across an entire road edge.");
                Vector3 incoming = points[points.Length - 2] - points[points.Length - 3];
                Vector3 wrap = points[1] - points[points.Length - 2];
                Assert.Greater(Vector3.Dot(incoming.normalized, wrap.normalized), .99f, "Loop seam must be on a straight road.");
            }
        }
        [Test] public void OppositeDirectionsStayOnTheRightOfTheirActualTravelHeading()
        {
            foreach (var lane in Lanes())
            {
                var points = lane.centerline;
                var roads = CircuitRoads(lane);
                for (int i = 2; i < points.Length - 2; i += 32)
                {
                    Vector3 p = points[i], tangent = points[i + 1] - points[i - 1]; tangent.y = 0; tangent.Normalize();
                    if (TruckTaxiEdenboroughLayout.Nodes.Values.Any(n => XzSquared(n - p) < 400)) continue;
                    // Nearby intersecting streets are not the road that authored this circuit.
                    var road = ClosestRoad(roads, p, tangent, out Vector3 nearest, out float best);
                    Assert.Less(best, 16, "Circuit leaves its real road corridor: " + lane.laneId);
                    Assert.Greater(Vector3.Dot(p - nearest, Vector3.Cross(Vector3.up, tangent)), .2f,
                        lane.laneId + " point=" + i + " source=" + road.Id);
                }
            }
        }
        [Test] public void SouthernReverseKeepsRegionalRoadOffsetBesideLoanheadLocalStreet()
        {
            var lane = Lanes().Single(l => l.laneId == "taxi.edenborough.traffic.southern.reverse");
            var target = new Vector3(-870.0794f, 0, -3700.963f);
            int i = Enumerable.Range(2, lane.centerline.Length - 4)
                .OrderBy(index => XzSquared(lane.centerline[index] - target)).First();
            var p = lane.centerline[i];
            Assert.Less(XzSquared(p - target), .0001f, "Reproduce the reported sample near Loanhead.");
            var tangent = lane.centerline[i + 1] - lane.centerline[i - 1]; tangent.y = 0; tangent.Normalize();
            var actual = ClosestRoad(CircuitRoads(lane), p, tangent, out Vector3 nearest, out float actualDistance);
            var unrelated = ClosestRoad(TruckTaxiEdenboroughLayout.Roads, p, tangent, out Vector3 wrongNearest, out float wrongDistance);
            Assert.AreEqual("taxi.edenborough.road.bonnyrigg-loanhead", actual.Id);
            Assert.AreEqual("taxi.edenborough.road.loanhead.x.0.0", unrelated.Id);
            Assert.Less(wrongDistance, actualDistance, "Whole-network proximity selects the wrong street here.");
            var right = Vector3.Cross(Vector3.up, tangent);
            Assert.Less(Vector3.Dot(p - wrongNearest, right), -.9f);
            Assert.That(Vector3.Dot(p - nearest, right), Is.EqualTo(actual.LaneWidth * .5f).Within(.002f));
        }

        private static TruckTaxiEdenboroughLayout.Road[] CircuitRoads(LwsTrafficLaneDefinition lane)
        {
            var circuits = Invoke<KeyValuePair<string, string[]>[]>("PopulationCircuits");
            var nodes = circuits.Single(c => lane.roadId == "taxi.edenborough.circuit." + c.Key).Value;
            return Enumerable.Range(0, nodes.Length).Select(i => TruckTaxiEdenboroughLayout.Roads.Single(r =>
                r.From == nodes[i] && r.To == nodes[(i + 1) % nodes.Length] ||
                r.To == nodes[i] && r.From == nodes[(i + 1) % nodes.Length])).ToArray();
        }

        private static TruckTaxiEdenboroughLayout.Road ClosestRoad(IEnumerable<TruckTaxiEdenboroughLayout.Road> roads,
            Vector3 p, Vector3 tangent, out Vector3 nearest, out float best)
        {
            best = float.PositiveInfinity; nearest = default; TruckTaxiEdenboroughLayout.Road closest = default;
            foreach (var road in roads)
                for (int s = 1; s < road.Points.Length; s++)
                {
                    Vector3 a = road.Points[s - 1], ab = road.Points[s] - a; ab.y = 0;
                    if (ab.sqrMagnitude < .001f || Mathf.Abs(Vector3.Dot(ab.normalized, tangent)) < .95f) continue;
                    Vector3 delta = p - a; delta.y = 0;
                    float t = Mathf.Clamp01(Vector3.Dot(delta, ab) / ab.sqrMagnitude);
                    Vector3 candidate = a + ab * t; float distance = XzSquared(p - candidate);
                    if (distance < best) { best = distance; nearest = candidate; closest = road; }
                }
            return closest;
        }
        [Test] public void AllDistrictsAndEveryBypassSectionHaveConnectedCircuitCoverage()
        {
            var lanes = Lanes();
            foreach (var district in TruckTaxiEdenboroughLayout.Districts)
            {
                string key = district.Id.Substring("taxi.edenborough.".Length);
                Assert.AreEqual(2, lanes.Count(l => l.laneId.StartsWith("taxi.edenborough.traffic." + key + ".", StringComparison.Ordinal)));
            }
            var bypass = lanes.Where(l => l.roadId == "taxi.edenborough.circuit.bypass").ToArray();
            Assert.AreEqual(2, bypass.Length);
            foreach (var lane in bypass)
            {
                Assert.Greater(lane.lengthMeters, 10000);
                foreach (var road in TruckTaxiEdenboroughLayout.Roads.Where(r => r.Kind == "Highway"))
                {
                    var midpoint = road.Points[road.Points.Length / 2];
                    Assert.Less(lane.centerline.Min(p => XzSquared(p - midpoint)), 144, road.Id);
                }
            }
        }
        [Test] public void NineWalkGatherAreasHaveGroundedClosedLoopsOutsideRoadsAndBypassShoulders()
        {
            var plans = Invoke<Array>("PopulationWalkPlans", (object)Lanes());
            Assert.AreEqual(9, plans.Length); var ids = new HashSet<string>();
            foreach (var plan in plans)
            {
                Assert.IsTrue(ids.Add(Field<string>(plan, "Id")));
                var center = Field<Vector3>(plan, "Center"); var size = Field<Vector2>(plan, "Size");
                Assert.IsTrue(Invoke<bool>("PopulationWalkRectangleClear", center, size));
                var points = Field<Vector3[]>(plan, "Points"); Assert.Greater(points.Length, 4);
                for (int i = 0; i < points.Length; i++)
                {
                    var p = points[i]; Assert.IsTrue(TruckTaxiEdenboroughLayout.WorldBounds.Contains(p));
                    Assert.That(p.y, Is.EqualTo(TruckTaxiEdenboroughLayout.Height(p.x, p.z) + .1f).Within(.0001f));
                    Assert.LessOrEqual(Mathf.Abs(p.x - center.x), size.x * .5f);
                    Assert.LessOrEqual(Mathf.Abs(p.z - center.z), size.y * .5f);
                    Assert.LessOrEqual(Vector3.Distance(p, points[(i + 1) % points.Length]), 4.1f);
                }
            }
        }
        private static float XzSquared(Vector3 p) => p.x * p.x + p.z * p.z;
    }
}
