using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPedestrianTestSignal : MonoBehaviour
    {
        public bool PeopleMoveState { get; set; }
    }

    public sealed class TruckTaxiPedestrianTestPath : MonoBehaviour
    {
        public Vector3[,] points;
    }

    [Category("TaxiIntegrated")]
    public sealed class TruckTaxiPedestrianWalkGraphTests
    {
        [Test]
        public void AuthoredFullRoadCrossingConnectsOppositeSidewalks()
        {
            var ground = new GameObject("ground");
            var pathObject = new GameObject("crosswalk path");
            var crossingObject = new GameObject("intersection");
            try
            {
                ground.transform.position = new Vector3(0, -1, 0);
                var floor = ground.AddComponent<BoxCollider>();
                floor.size = new Vector3(200, 2, 200);
                var path = pathObject.AddComponent<TruckTaxiPedestrianTestPath>();
                path.points = new Vector3[1, 6];
                path.points[0, 0] = new Vector3(-10, 0, 0);
                path.points[0, 1] = new Vector3(-8, 0, 0);
                path.points[0, 2] = new Vector3(-3, 0, 0);
                path.points[0, 3] = new Vector3(3, 0, 0);
                path.points[0, 4] = new Vector3(8, 0, 0);
                path.points[0, 5] = new Vector3(10, 0, 0);
                var gate = crossingObject.AddComponent<TruckTaxiPedestrianTestSignal>();
                var crossing = crossingObject.AddComponent<TruckTaxiIntersection>();
                crossing.Configure("test", gate, new Component[] { gate }, path);
                var lane = new LwsTrafficLaneDefinition
                {
                    laneWidthMeters = 4,
                    centerline = new[] { new Vector3(0, 0, -20), new Vector3(0, 0, 20) }
                };
                Physics.SyncTransforms();
                var graph = new TruckTaxiPedestrianWalkGraph(new[] { lane }, null, null,
                    new[] { crossing, crossing }, null, null, null, 0, Vector3.zero);
                Assert.AreEqual(1, graph.CrossingJourneyCount, "Aliases of one vendor crosswalk must not duplicate its route.");
                Assert.Greater(graph.WalkableSpawnCount, 100);
                int start = -1;
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < graph.Count; i++)
                {
                    if (graph[i].x >= -8) continue;
                    float distance = (graph[i] - new Vector3(-12, 0, 0)).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; start = i; }
                }
                Assert.GreaterOrEqual(start, 0);
                var route = new List<int>();
                Assert.IsTrue(graph.PickCrossingJourney(start, route));
                int entry = -1, exit = -1;
                for (int i = 1; i < route.Count; i++)
                    if (graph.TryCrossingEntry(route[i - 1], route[i], out var observed))
                    { Assert.AreSame(crossing, observed); entry = route[i - 1]; }
                Assert.GreaterOrEqual(entry, 0);
                foreach (int node in route)
                    if (graph.IsOppositeCurb(node, entry, crossing)) { exit = node; break; }
                Assert.GreaterOrEqual(exit, 0);
                Assert.Greater(graph[exit].x, 0);
                Assert.Greater(graph[route[route.Count - 1]].x, 0);
                // A curb can extend slightly beyond the nearest valid sidewalk sample.
                var points = (List<Vector3>)typeof(TruckTaxiPedestrianWalkGraph).GetField("points",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(graph);
                points.Clear();
                points.Add(new Vector3(24, 0, 0));
                points.Add(new Vector3(25, 0, 0));
                var edges = new List<List<int>> { new List<int>(), new List<int>() };
                var link = typeof(TruckTaxiPedestrianWalkGraph).GetMethod("LinkEndpoint",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.AreEqual(0, link.Invoke(graph, new object[] { edges, 1, Vector3.right, 1, 5f }));
            }
            finally
            {
                Object.DestroyImmediate(crossingObject);
                Object.DestroyImmediate(pathObject);
                Object.DestroyImmediate(ground);
            }
        }
    }
}
