using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LWS.InterstateHauler.Tests.EditMode
{
    public sealed class LwsNavigationEditModeTests
    {
        private const string VoicePackPath = "Assets/LWS/InterstateHauler/Navigation/Data/IH_GpsVoicePack_Default.asset";
        private const string PlayerPrefsGpsVoiceKey = "ih.settings.gpsVoiceGuidance";

        private bool _hadGpsVoicePref;
        private int _gpsVoicePref;

        [SetUp]
        public void SetUp()
        {
            _hadGpsVoicePref = PlayerPrefs.HasKey(PlayerPrefsGpsVoiceKey);
            _gpsVoicePref = PlayerPrefs.GetInt(PlayerPrefsGpsVoiceKey, 1);
            PlayerPrefs.DeleteKey(PlayerPrefsGpsVoiceKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadGpsVoicePref)
            {
                PlayerPrefs.SetInt(PlayerPrefsGpsVoiceKey, _gpsVoicePref);
            }
            else
            {
                PlayerPrefs.DeleteKey(PlayerPrefsGpsVoiceKey);
            }

            PlayerPrefs.Save();
        }

        [Test]
        public void RoutePlannerFindsDirectionalRouteThroughGraph()
        {
            LwsRoadGraph graph = CreateNavigationGraph();
            var planner = new LwsRoutePlanner(LwsNavigationTuning.Default());

            LwsRouteResult result = planner.PlanRoute(
                new LwsRouteRequest
                {
                    requestId = "navigation-test-route",
                    originNodeId = "A",
                    destinationNodeId = "D",
                    destinationId = "test-destination"
                },
                graph);

            Assert.IsTrue(result.succeeded, result.message);
            CollectionAssert.AreEqual(new[] { "AB", "BC", "CD" }, result.edgeIds);
            Assert.Greater(result.waypoints.Count, 3);
            Assert.AreEqual("test-destination", result.destinationId);
            Assert.AreEqual(LwsNavigationManeuverType.StartRoute, result.steps[0].maneuver);
            Assert.AreEqual(LwsNavigationManeuverType.Arrive, result.steps[result.steps.Count - 1].maneuver);
        }

        [Test]
        public void RoutePlannerRespectsOneWayEdges()
        {
            LwsRoadGraph graph = CreateNavigationGraph();
            var planner = new LwsRoutePlanner(LwsNavigationTuning.Default());

            LwsRouteResult result = planner.PlanRoute(
                new LwsRouteRequest
                {
                    requestId = "navigation-one-way-test",
                    originNodeId = "D",
                    destinationNodeId = "A"
                },
                graph);

            Assert.IsFalse(result.succeeded);
            StringAssert.Contains("No route found", result.message);
        }

        [Test]
        public void ManeuverClassificationCoversAngleBandsAndRampSemantics()
        {
            var planner = new LwsRoutePlanner(LwsNavigationTuning.Default());

            Assert.AreEqual(
                LwsNavigationManeuverType.ContinueStraight,
                planner.ClassifyManeuver(Vector3.forward, Quaternion.Euler(0f, 5f, 0f) * Vector3.forward, LwsRoadClass.Interstate));
            Assert.AreEqual(
                LwsNavigationManeuverType.SlightRight,
                planner.ClassifyManeuver(Vector3.forward, Quaternion.Euler(0f, 25f, 0f) * Vector3.forward, LwsRoadClass.Interstate));
            Assert.AreEqual(
                LwsNavigationManeuverType.TurnRight,
                planner.ClassifyManeuver(Vector3.forward, Quaternion.Euler(0f, 70f, 0f) * Vector3.forward, LwsRoadClass.Interstate));
            Assert.AreEqual(
                LwsNavigationManeuverType.SharpRight,
                planner.ClassifyManeuver(Vector3.forward, Quaternion.Euler(0f, 130f, 0f) * Vector3.forward, LwsRoadClass.Interstate));
            Assert.AreEqual(
                LwsNavigationManeuverType.TakeRampRight,
                planner.ClassifyManeuver(Vector3.forward, Quaternion.Euler(0f, 20f, 0f) * Vector3.forward, LwsRoadClass.Ramp));
        }

        [Test]
        public void DefaultVoicePackExposesEveryManeuverSlot()
        {
            LwsGpsVoicePack pack = AssetDatabase.LoadAssetAtPath<LwsGpsVoicePack>(VoicePackPath);

            Assert.IsNotNull(pack, $"{VoicePackPath} did not load as a GPS voice pack.");
            Assert.IsTrue(pack.ValidateSlots(out string message), message);
            Assert.AreEqual(LwsNavigationManeuverCatalog.All.Count, pack.ManeuverSlots.Count);
            Assert.AreEqual(7, pack.DistanceSlots.Count);
        }

        [Test]
        public void GpsVoiceGuidanceDefaultsOnAndCanSuppressPrompts()
        {
            var registry = new LwsServiceRegistry();
            var settings = new LwsPlayerSettingsService();
            var voice = new LwsGpsVoiceGuidanceService();
            registry.Register<ILwsPlayerSettingsService>(settings);
            registry.Register<ILwsGpsVoiceGuidanceService>(voice, typeof(ILwsPlayerSettingsService));
            registry.InitializeAll();

            Assert.IsTrue(settings.GpsVoiceGuidanceEnabled);

            LwsGpsVoicePack pack = ScriptableObject.CreateInstance<LwsGpsVoicePack>();
            pack.EnsureAllSlots();
            AudioClip clip = AudioClip.Create("route-start-test", 64, 1, 44100, false);
            for (int i = 0; i < pack.ManeuverSlots.Count; i++)
            {
                if (pack.ManeuverSlots[i].maneuver == LwsNavigationManeuverType.StartRoute)
                {
                    pack.ManeuverSlots[i].clip = clip;
                    break;
                }
            }

            var sourceObject = new GameObject("gps-voice-test");
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            voice.Configure(pack, source);

            settings.SetGpsVoiceGuidanceEnabled(false);

            Assert.IsFalse(voice.Announce(LwsNavigationManeuverType.StartRoute, 0, true));
            Assert.AreEqual("Start route", voice.LastInstruction);
            Assert.AreEqual(string.Empty, voice.LastClipName);

            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(pack);
            registry.ShutdownAll();
        }

        [Test]
        public void NavigationServiceMarksOffRouteBeyondTolerance()
        {
            var registry = new LwsServiceRegistry();
            var roadGraphService = new LwsRoadGraphService();
            var navigation = new LwsNavigationService();
            LwsRoadGraph graph = CreateNavigationGraph();

            registry.Register<ILwsRoadGraphService>(roadGraphService);
            registry.Register<ILwsNavigationService>(navigation, typeof(ILwsRoadGraphService));
            registry.InitializeAll();
            roadGraphService.SetActiveGraph(graph);

            LwsRouteResult route = navigation.RequestRoute(
                new LwsRouteRequest
                {
                    requestId = "off-route-test",
                    originNodeId = "A",
                    destinationNodeId = "D"
                },
                graph);

            Assert.IsTrue(route.succeeded, route.message);

            navigation.UpdateVehiclePose(new Vector3(1000f, 0f, 1000f), Vector3.forward, 0.25f);

            Assert.IsTrue(navigation.RuntimeState.offRoute);
            Assert.AreEqual(LwsNavigationRouteStatus.OffRoute, navigation.RuntimeState.status);
            registry.ShutdownAll();
        }

        [Test]
        public void RoutePlannerHasNoUtsSourceDependency()
        {
            string source = File.ReadAllText("Assets/LWS/InterstateHauler/Navigation/LwsRoutePlanner.cs");

            StringAssert.DoesNotContain("UTS", source.ToUpperInvariant());
            StringAssert.DoesNotContain("CARAI", source.ToUpperInvariant());
            StringAssert.DoesNotContain("WALKPATH", source.ToUpperInvariant());
        }

        private static LwsRoadGraph CreateNavigationGraph()
        {
            var graph = new LwsRoadGraph { graphId = "navigation-test-graph" };
            graph.nodes.Add(new LwsRoadNode { nodeId = "A", position = new Vector3(0f, 0f, 0f) });
            graph.nodes.Add(new LwsRoadNode { nodeId = "B", position = new Vector3(0f, 0f, 100f) });
            graph.nodes.Add(new LwsRoadNode { nodeId = "C", position = new Vector3(80f, 0f, 180f) });
            graph.nodes.Add(new LwsRoadNode { nodeId = "D", position = new Vector3(160f, 0f, 180f) });

            AddEdge(graph, "TEST_ROAD_A", "TEST_SEG_A", "AB", "A", "B", LwsRoadClass.Interstate);
            AddEdge(graph, "TEST_ROAD_B", "TEST_SEG_B", "BC", "B", "C", LwsRoadClass.Interstate);
            AddEdge(graph, "TEST_ROAD_C", "TEST_SEG_C", "CD", "C", "D", LwsRoadClass.Ramp);
            Assert.IsTrue(graph.Validate().IsValid, graph.Validate().Summary);
            return graph;
        }

        private static void AddEdge(
            LwsRoadGraph graph,
            string roadId,
            string segmentId,
            string edgeId,
            string from,
            string to,
            LwsRoadClass roadClass)
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
                roadClass = roadClass,
                direction = LwsRoadDirection.Northbound,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = true,
                distanceMeters = distance,
                travelCost = distance,
                speedLimitMph = roadClass == LwsRoadClass.Ramp ? 35f : 65f,
                laneCount = roadClass == LwsRoadClass.Ramp ? 1 : 2,
                laneWidthMeters = 3.7f,
                laneCenterOffsetsMeters = roadClass == LwsRoadClass.Ramp
                    ? new List<float> { 0f }
                    : new List<float> { -1.85f, 1.85f }
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
