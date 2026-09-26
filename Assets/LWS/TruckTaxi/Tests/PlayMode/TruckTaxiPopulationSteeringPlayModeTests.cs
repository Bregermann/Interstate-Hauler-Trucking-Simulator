using System.Collections;
using System.Collections.Generic;
using LWS.InterstateHauler;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LWS.TruckTaxi.Tests
{
    public sealed class TruckTaxiPopulationSteeringPlayModeTests
    {
        [UnityTest]
        public IEnumerator WalkGraphUsesFullRoadWidthAndStaticGround()
        {
            var origin = new Vector3(10000, 0, 10000);
            var ground = new GameObject("pedestrian test ground");
            var movingSurface = new GameObject("pedestrian test moving surface");
            var building = new GameObject("pedestrian test building");
            try
            {
                ground.transform.position = origin + new Vector3(0, -.5f, 0);
                ground.AddComponent<BoxCollider>().size = new Vector3(120, 1, 60);
                movingSurface.transform.position = origin + new Vector3(20, -.5f, 45);
                movingSurface.AddComponent<BoxCollider>().size = new Vector3(12, 1, 12);
                var movingBody = movingSurface.AddComponent<Rigidbody>();
                movingBody.isKinematic = true; movingBody.useGravity = false;
                building.transform.position = origin + new Vector3(20, 0, 0);
                building.AddComponent<TruckTaxiWorldAnchor>().Initialize(
                    "00000000-0000-0000-0000-000000000001", TruckTaxiWorldAnchorType.Building, "test");
                var buildingCollider = building.AddComponent<BoxCollider>();
                buildingCollider.center = Vector3.up * 3; buildingCollider.size = new Vector3(10, 6, 10);
                Physics.SyncTransforms();
                var road = LwsRoadGraph.CreateEmpty("pedestrian-width-test");
                var edge = new LwsRoadEdge();
                edge.samples.Add(new LwsRoadSample { position = origin + new Vector3(0, 0, -30), roadWidthMeters = 20 });
                edge.samples.Add(new LwsRoadSample { position = origin + new Vector3(0, 0, 30), roadWidthMeters = 20 });
                road.edges.Add(edge);
                var graph = new TruckTaxiPedestrianWalkGraph(System.Array.Empty<LwsTrafficLaneDefinition>(),
                    System.Array.Empty<TruckTaxiPedestrianArea>(), System.Array.Empty<Component>(),
                    System.Array.Empty<TruckTaxiIntersection>(), road,
                    new[] { building.GetComponent<TruckTaxiWorldAnchor>() },
                    System.Array.Empty<TruckTaxiSurface>(), 100, origin);
                Assert.Greater(graph.WalkableSpawnCount, 0);
                for (int i = 0; i < graph.WalkableSpawnCount; i++)
                {
                    var point = graph[graph.NextSpawnNode()];
                    float x = point.x - origin.x, z = point.z - origin.z;
                    Assert.GreaterOrEqual(Mathf.Abs(x), 11.5f, "Full 20 m road and border are excluded.");
                    Assert.LessOrEqual(Mathf.Abs(z), 30.1f, "A moving collider cannot supply ground beyond the static pad.");
                    Assert.IsFalse(x >= 14 && x <= 26 && Mathf.Abs(z) <= 6,
                        "World Builder building footprints are excluded.");
                }
                yield return null;
            }
            finally
            {
                Object.Destroy(ground); Object.Destroy(movingSurface); Object.Destroy(building);
            }
        }

        [UnityTest]
        public IEnumerator DenseCityReusesNativePopulationAndVisualSteeringAuthorities()
        {
            yield return SceneManager.LoadSceneAsync("TruckTaxi_DemoCity");
            float deadline = Time.realtimeSinceStartup + 60;
            while ((TruckTaxiBootstrap.Instance == null || !TruckTaxiBootstrap.Instance.Ready) && Time.realtimeSinceStartup < deadline) yield return null;
            var host = TruckTaxiBootstrap.Instance;
            Assert.IsNotNull(host); Assert.IsTrue(host.Ready);
            Assert.AreEqual(12, host.pedestrians.BaselineActiveCount, "Measured native UTS baseline, not the old maximumPeople value.");
            Assert.AreEqual(12, host.traffic.BaselineActiveCount);
            Assert.AreEqual(144, host.pedestrians.TargetCount);
            Assert.AreEqual(60, host.traffic.TargetCount);
            int paths = host.pedestrians.peoplePaths.Length;
            host.pedestrians.Initialize(); host.traffic.Initialize();
            Assert.AreEqual(paths, host.pedestrians.peoplePaths.Length);
            var visual = host.Player.GetComponent<TruckTaxiSteeringWheelVisual>();
            Assert.IsNotNull(visual, "Main integration must attach the reusable presenter to the runtime player.");
            Assert.IsTrue(visual.Applied);
            Assert.IsFalse(host.Player.DashboardController.SteeringWheelAnimationEnabled);
            Assert.IsNull(host.Player.NwhAdapter.VehicleController.steering.steeringWheel);
            host.SetPaused(false);
            deadline = Time.realtimeSinceStartup + 30;
            while ((host.pedestrians.ActiveCount < 144 || host.traffic.ActiveCount < 60) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(144, host.pedestrians.ActiveCount);
            Assert.GreaterOrEqual(host.pedestrians.GraphWalkableSpawnCount, 144, "Grounded off-road graph can host the live population.");
            Assert.Greater(host.pedestrians.GraphRoadSegmentCount, 0, "Road exclusions include the authored road graph.");
            Assert.AreEqual(144, host.pedestrians.GraphWalkerCount, "Every dense pedestrian has a dynamic UTS waypoint route.");
            var occupied = new HashSet<Vector2Int>();
            foreach (var ped in host.pedestrians.People)
                occupied.Add(new Vector2Int(Mathf.FloorToInt(ped.transform.position.x / 20),
                    Mathf.FloorToInt(ped.transform.position.z / 20)));
            Assert.Greater(occupied.Count, 40, "Pedestrians should span city walkable space, not authored loop clusters.");
            Assert.AreEqual(60, host.traffic.ActiveCount);
            Assert.LessOrEqual(host.pedestrians.RagdollCount, host.pedestrians.densityProfile.maximumActiveRagdolls);
            Time.timeScale = 1;
        }
    }
}
