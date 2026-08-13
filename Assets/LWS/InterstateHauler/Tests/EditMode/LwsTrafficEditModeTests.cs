using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsTrafficEditModeTests
    {
        private const string InterstateTrafficProfilePath = "Assets/LWS/InterstateHauler/Traffic/Data/IH_TrafficProfile_InterstateValidation.asset";

        private static readonly string[] InterstateTrafficPrefabPaths =
        {
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_3.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_5.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Jeep.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Taxi.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_2.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/City_bus.prefab"
        };

        [Test]
        public void InterstateValidationTrafficProfileResolvesSelectedUtsPrefabs()
        {
            LwsUtsTrafficProfile profile = AssetDatabase.LoadAssetAtPath<LwsUtsTrafficProfile>(InterstateTrafficProfilePath);

            Assert.IsNotNull(profile, $"{InterstateTrafficProfilePath} did not load as a LwsUtsTrafficProfile.");
            Assert.IsTrue(profile.TryGetTrafficPrefabsCopy(out GameObject[] prefabs, out string message), message);
            Assert.AreEqual(InterstateTrafficPrefabPaths.Length, prefabs.Length);

            Type carMoveType = ResolveTypeByName("CarMove");
            Type carWheelsType = ResolveTypeByName("CarWheels");
            Assert.IsNotNull(carMoveType, "UTS CarMove type was not available.");
            Assert.IsNotNull(carWheelsType, "UTS CarWheels type was not available.");

            for (int i = 0; i < prefabs.Length; i++)
            {
                Assert.IsNotNull(prefabs[i], $"UTS validation traffic prefab slot {i} is null.");
                Assert.AreEqual(InterstateTrafficPrefabPaths[i], AssetDatabase.GetAssetPath(prefabs[i]));
                Assert.IsNotNull(prefabs[i].GetComponentInChildren(carMoveType, true), $"{prefabs[i].name} is missing UTS CarMove.");
                Assert.IsNotNull(prefabs[i].GetComponentInChildren(carWheelsType, true), $"{prefabs[i].name} is missing UTS CarWheels.");
            }
        }

        [Test]
        public void TrafficLaneBuilderCreatesHighwayAndRampLanes()
        {
            LwsRoadGraph graph = CreateTrafficGraph();
            var policy = new LwsTrafficSpawnPolicy { includeRampTraffic = true, includeTurnaroundTraffic = false };

            IReadOnlyList<LwsTrafficLaneDefinition> lanes = LwsTrafficLaneBuilder.BuildTrafficLanes(graph, policy);

            Assert.AreEqual(5, lanes.Count);
            Assert.AreEqual("NB_MAIN_TRAFFIC_L1", lanes[0].laneId);
            Assert.AreEqual(LwsRoadDirection.Northbound, lanes[0].direction);
            Assert.AreEqual(3, lanes[0].centerline.Length);
            Assert.AreNotEqual(Vector3.zero, lanes[0].centerline[0]);
        }

        [Test]
        public void TrafficLaneBuilderExcludesTurnaroundByDefault()
        {
            LwsRoadGraph graph = CreateTrafficGraph();
            var policy = new LwsTrafficSpawnPolicy();

            IReadOnlyList<LwsTrafficLaneDefinition> lanes = LwsTrafficLaneBuilder.BuildTrafficLanes(graph, policy);

            foreach (LwsTrafficLaneDefinition lane in lanes)
            {
                Assert.AreNotEqual("TURN_TRAFFIC_L1", lane.laneId);
            }
        }

        [Test]
        public void TrafficSpawnPolicyRejectsInvalidDistances()
        {
            var policy = new LwsTrafficSpawnPolicy
            {
                minimumPlayerSpawnDistanceMeters = 500f,
                maximumPlayerSpawnDistanceMeters = 100f
            };

            Assert.IsFalse(policy.Validate(out string message));
            StringAssert.Contains("distance", message);
        }

        [Test]
        public void TrafficServiceRejectsDuplicateActiveIds()
        {
            var service = new LwsTrafficService();
            service.Initialize(new LwsServiceContext(new LwsServiceRegistry()));
            var first = new GameObject("traffic-a").AddComponent<LwsTrafficIdentity>();
            var second = new GameObject("traffic-b").AddComponent<LwsTrafficIdentity>();
            first.Configure("ih.traffic.test.001", "lane-a", "Car_1", LwsTrafficVehicleKind.PassengerCar);
            second.Configure("ih.traffic.test.001", "lane-a", "Car_1", LwsTrafficVehicleKind.PassengerCar);

            LwsServiceResult firstResult = service.RegisterTrafficVehicle(first);
            LwsServiceResult duplicateResult = service.RegisterTrafficVehicle(second);

            Assert.IsTrue(firstResult.Succeeded, firstResult.Message);
            Assert.IsFalse(duplicateResult.Succeeded);
            UnityEngine.Object.DestroyImmediate(first.gameObject);
            UnityEngine.Object.DestroyImmediate(second.gameObject);
        }

        [Test]
        public void MilesPerHourConversionUsesUnityMetersPerSecond()
        {
            Assert.AreEqual(29.0576f, LwsTrafficLaneBuilder.MilesPerHourToMetersPerSecond(65f), 0.0005f);
        }

        private static LwsRoadGraph CreateTrafficGraph()
        {
            var graph = new LwsRoadGraph { graphId = "traffic-test" };
            AddEdge(graph, "NB", "NB_MAIN", "NB_EDGE", LwsRoadClass.Interstate, LwsRoadDirection.Northbound, 2, 65f, 12f);
            AddEdge(graph, "SB", "SB_MAIN", "SB_EDGE", LwsRoadClass.Interstate, LwsRoadDirection.Southbound, 2, 65f, -12f);
            AddEdge(graph, "RAMP", "RAMP", "RAMP_EDGE", LwsRoadClass.Ramp, LwsRoadDirection.Northbound, 1, 35f, 2f);
            AddEdge(graph, "TURN", "TURN", "TURN_EDGE", LwsRoadClass.Ramp, LwsRoadDirection.Bidirectional, 1, 25f, 0f);
            return graph;
        }

        private static void AddEdge(
            LwsRoadGraph graph,
            string roadId,
            string segmentId,
            string edgeId,
            LwsRoadClass roadClass,
            LwsRoadDirection direction,
            int lanes,
            float speedMph,
            float x)
        {
            string start = $"{edgeId}_A";
            string end = $"{edgeId}_B";
            graph.nodes.Add(new LwsRoadNode { nodeId = start, position = new Vector3(x, 0f, 0f) });
            graph.nodes.Add(new LwsRoadNode { nodeId = end, position = new Vector3(x, 0f, 100f) });

            var edge = new LwsRoadEdge
            {
                roadId = roadId,
                segmentId = segmentId,
                edgeId = edgeId,
                fromNodeId = start,
                toNodeId = end,
                roadClass = roadClass,
                direction = direction,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                distanceMeters = 100f,
                speedLimitMph = speedMph,
                laneCount = lanes,
                laneWidthMeters = 3.7f
            };

            for (int lane = 0; lane < lanes; lane++)
            {
                edge.laneCenterOffsetsMeters.Add(((lanes - 1) * -0.5f + lane) * edge.laneWidthMeters);
            }

            edge.samples.Add(CreateSample(edge, new Vector3(x, 0f, 0f), 0f));
            edge.samples.Add(CreateSample(edge, new Vector3(x, 0f, 50f), 50f));
            edge.samples.Add(CreateSample(edge, new Vector3(x, 0f, 100f), 100f));
            graph.edges.Add(edge);
        }

        private static LwsRoadSample CreateSample(LwsRoadEdge edge, Vector3 position, float distance)
        {
            return new LwsRoadSample
            {
                roadId = edge.roadId,
                segmentId = edge.segmentId,
                distanceFromStartMeters = distance,
                position = position,
                forward = Vector3.forward,
                up = Vector3.up,
                direction = edge.direction,
                laneCount = edge.laneCount,
                laneWidthMeters = edge.laneWidthMeters,
                speedLimitMph = edge.speedLimitMph
            };
        }

        private static Type ResolveTypeByName(string typeName)
        {
            Type type = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Assembly-CSharp");
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
