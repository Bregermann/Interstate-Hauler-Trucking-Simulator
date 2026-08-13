using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LWS.InterstateHauler.Tests.PlayMode
{
    public sealed class LwsNavigationPlayModeTests
    {
        private const string PlayerPrefsGpsVoiceKey = "ih.settings.gpsVoiceGuidance";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsGpsVoiceKey);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsGpsVoiceKey);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTest]
        public IEnumerator NavigationServicesInitializeAndStartRoute()
        {
            LwsRoadGraph graph = CreateNavigationGraph();
            var registry = new LwsServiceRegistry();
            var settings = new LwsPlayerSettingsService();
            var roadGraphService = new LwsRoadGraphService();
            var voice = new LwsGpsVoiceGuidanceService();
            var navigation = new LwsNavigationService();

            registry.Register<ILwsPlayerSettingsService>(settings);
            registry.Register<ILwsRoadGraphService>(roadGraphService);
            registry.Register<ILwsGpsVoiceGuidanceService>(voice, typeof(ILwsPlayerSettingsService));
            registry.Register<ILwsNavigationService>(navigation, typeof(ILwsRoadGraphService), typeof(ILwsGpsVoiceGuidanceService));
            registry.InitializeAll();
            roadGraphService.SetActiveGraph(graph);

            LwsRouteResult route = navigation.RequestRoute(
                new LwsRouteRequest
                {
                    requestId = "playmode-navigation-route",
                    originNodeId = "A",
                    destinationNodeId = "C",
                    destinationId = "playmode-destination"
                },
                graph);

            yield return null;

            Assert.IsTrue(route.succeeded, route.message);
            Assert.IsTrue(navigation.RuntimeState.routeActive);
            Assert.AreEqual("playmode-destination", navigation.RuntimeState.destinationId);
            Assert.AreEqual(LwsNavigationRouteStatus.Active, navigation.RuntimeState.status);
            registry.ShutdownAll();
        }

        [UnityTest]
        public IEnumerator PhysicalGpsPresenterBindsWorldSpaceScreen()
        {
            var truck = new GameObject("IH test truck");
            var cab = new GameObject("Cab");
            cab.transform.SetParent(truck.transform, false);
            LwsCabGpsController gps = truck.AddComponent<LwsCabGpsController>();

            gps.BindPhysicalScreen();

            yield return null;

            Assert.IsTrue(gps.PhysicalGpsBound);
            Assert.IsNotNull(cab.transform.Find("IH Physical Cab GPS Screen"));
            Object.Destroy(truck);
        }

        [UnityTest]
        public IEnumerator VoiceSettingCanDisableVoiceWithoutClearingVisualRoute()
        {
            LwsRoadGraph graph = CreateNavigationGraph();
            var registry = new LwsServiceRegistry();
            var settings = new LwsPlayerSettingsService();
            var roadGraphService = new LwsRoadGraphService();
            var voice = new LwsGpsVoiceGuidanceService();
            var navigation = new LwsNavigationService();

            registry.Register<ILwsPlayerSettingsService>(settings);
            registry.Register<ILwsRoadGraphService>(roadGraphService);
            registry.Register<ILwsGpsVoiceGuidanceService>(voice, typeof(ILwsPlayerSettingsService));
            registry.Register<ILwsNavigationService>(navigation, typeof(ILwsRoadGraphService), typeof(ILwsGpsVoiceGuidanceService));
            registry.InitializeAll();
            roadGraphService.SetActiveGraph(graph);

            LwsRouteResult route = navigation.RequestRoute(
                new LwsRouteRequest
                {
                    requestId = "playmode-voice-toggle-route",
                    originNodeId = "A",
                    destinationNodeId = "C"
                },
                graph);

            settings.SetGpsVoiceGuidanceEnabled(false);
            bool announced = voice.Announce(LwsNavigationManeuverType.TurnRight, 1, true);

            yield return null;

            Assert.IsTrue(route.succeeded, route.message);
            Assert.IsTrue(navigation.RuntimeState.routeActive);
            Assert.IsFalse(settings.GpsVoiceGuidanceEnabled);
            Assert.IsFalse(announced);
            Assert.AreSame(route, navigation.CurrentRoute);
            registry.ShutdownAll();
        }

        private static LwsRoadGraph CreateNavigationGraph()
        {
            var graph = new LwsRoadGraph { graphId = "playmode-navigation-test-graph" };
            graph.nodes.Add(new LwsRoadNode { nodeId = "A", position = new Vector3(0f, 0f, 0f) });
            graph.nodes.Add(new LwsRoadNode { nodeId = "B", position = new Vector3(0f, 0f, 80f) });
            graph.nodes.Add(new LwsRoadNode { nodeId = "C", position = new Vector3(60f, 0f, 140f) });
            AddEdge(graph, "PLAY_ROAD_A", "PLAY_SEG_A", "AB", "A", "B");
            AddEdge(graph, "PLAY_ROAD_B", "PLAY_SEG_B", "BC", "B", "C");
            return graph;
        }

        private static void AddEdge(LwsRoadGraph graph, string roadId, string segmentId, string edgeId, string from, string to)
        {
            Vector3 fromPosition = FindNode(graph, from).position;
            Vector3 toPosition = FindNode(graph, to).position;
            Vector3 forward = (toPosition - fromPosition).normalized;
            float distance = Vector3.Distance(fromPosition, toPosition);
            var edge = new LwsRoadEdge
            {
                roadId = roadId,
                segmentId = segmentId,
                edgeId = edgeId,
                fromNodeId = from,
                toNodeId = to,
                roadClass = LwsRoadClass.Interstate,
                direction = LwsRoadDirection.Northbound,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = true,
                distanceMeters = distance,
                travelCost = distance,
                speedLimitMph = 55f,
                laneCount = 2,
                laneWidthMeters = 3.7f,
                laneCenterOffsetsMeters = new List<float> { -1.85f, 1.85f }
            };

            edge.samples.Add(CreateSample(edge, fromPosition, 0f, forward));
            edge.samples.Add(CreateSample(edge, Vector3.Lerp(fromPosition, toPosition, 0.5f), distance * 0.5f, forward));
            edge.samples.Add(CreateSample(edge, toPosition, distance, forward));
            graph.edges.Add(edge);
        }

        private static LwsRoadSample CreateSample(LwsRoadEdge edge, Vector3 position, float distance, Vector3 forward)
        {
            return new LwsRoadSample
            {
                roadId = edge.roadId,
                segmentId = edge.segmentId,
                distanceFromStartMeters = distance,
                position = position,
                forward = forward,
                up = Vector3.up,
                direction = edge.direction,
                roadWidthMeters = edge.laneCount * edge.laneWidthMeters,
                laneWidthMeters = edge.laneWidthMeters,
                laneCount = edge.laneCount,
                speedLimitMph = edge.speedLimitMph
            };
        }

        private static LwsRoadNode FindNode(LwsRoadGraph graph, string nodeId)
        {
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i].nodeId == nodeId)
                {
                    return graph.nodes[i];
                }
            }

            return null;
        }
    }
}
