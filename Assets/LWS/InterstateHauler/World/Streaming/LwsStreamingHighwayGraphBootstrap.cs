using UnityEngine;

namespace LWS.InterstateHauler
{
    [DisallowMultipleComponent]
    public sealed class LwsStreamingHighwayGraphBootstrap : MonoBehaviour
    {
        private const string GraphId = "IH_STREAMING_VALIDATION_GLOBAL_GRAPH";
        private const float LaneWidthMeters = 3.7f;
        private const float RightShoulderWidthMeters = 3.0f;
        private const float LeftShoulderWidthMeters = 1.2f;
        private const float MedianWidthMeters = 14.0f;
        private const float RoadSurfaceY = 0.55f;
        private const float SampleSpacingMeters = 50f;

        [SerializeField] private bool registerOnStart = true;
        [SerializeField] private LwsRoadGraphProvider roadGraphProvider;
        [SerializeField] private bool enableEndlessHighwayValidation = true;
        [SerializeField] private bool addGameClockRuntime = true;
        [SerializeField] private bool addGameClockHud = true;
        [SerializeField] private bool addNavigationDebugPanels = true;
        [SerializeField] private bool addWeatherRuntime = true;
        [SerializeField] private bool addRoadConditionRuntime = true;
        [SerializeField] private bool addTrafficDebugPanel = true;
        [SerializeField] private LwsUtsTrafficProfile validationTrafficProfile;
        [SerializeField] private GameObject[] validationTrafficPrefabs;

        public LwsRoadGraph LastGraph { get; private set; }
        public bool EndlessHighwayValidationEnabled => enableEndlessHighwayValidation;

        private static float CarriagewayWidthMeters => LaneWidthMeters * 2f + RightShoulderWidthMeters + LeftShoulderWidthMeters;

        private void Start()
        {
            if (registerOnStart)
            {
                RegisterGlobalGraphAndServices();
            }
        }

        public void RegisterGlobalGraphAndServices()
        {
            EnsureRoadGraphProvider();
            LwsEndlessStreamingHighwayController endless = null;
            if (enableEndlessHighwayValidation)
            {
                endless = EnsureComponent<LwsEndlessStreamingHighwayController>();
                endless.Configure(roadGraphProvider);
                endless.RefreshNow();
                LastGraph = endless.LastGraph;
            }
            else
            {
                LastGraph = CreateDefaultGraph();
                roadGraphProvider.SetGraph(LastGraph, true);
            }

            if (LastGraph == null)
            {
                LastGraph = CreateDefaultGraph();
                roadGraphProvider.SetGraph(LastGraph, true);
            }

            if (addGameClockRuntime)
            {
                EnsureComponent<LwsGameClockCoordinator>();
            }

            if (addGameClockHud)
            {
                EnsureComponent<LwsGameClockHud>();
            }

            if (addNavigationDebugPanels)
            {
                EnsureComponent<LwsNavigationDebugPanel>();
                EnsureComponent<LwsGpsSettingsPanel>();
            }

            if (addWeatherRuntime)
            {
                EnsureComponent<LwsWeatherMakerAdapter>();
                EnsureComponent<LwsWeatherDebugPanel>();
            }

            if (addRoadConditionRuntime)
            {
                EnsureComponent<LwsRoadConditionRuntimeController>();
                EnsureComponent<LwsWeatheradeAdapter>();
                EnsureComponent<LwsNwhRoadConditionAdapter>();
                EnsureComponent<LwsRoadConditionDebugPanel>();
            }

            if (addTrafficDebugPanel)
            {
                LwsUtsHighwayTrafficController traffic = EnsureComponent<LwsUtsHighwayTrafficController>();
                traffic.ConfigureValidationProfile(validationTrafficProfile, validationTrafficPrefabs);
                endless?.BindTrafficController(traffic);
                traffic.InitializeFromGraph(roadGraphProvider, LastGraph);
                EnsureComponent<LwsUtsTrafficDebugPanel>();
            }
        }

        private void EnsureRoadGraphProvider()
        {
            if (roadGraphProvider == null)
            {
                roadGraphProvider = GetComponent<LwsRoadGraphProvider>();
            }

            if (roadGraphProvider == null)
            {
                roadGraphProvider = gameObject.AddComponent<LwsRoadGraphProvider>();
            }
        }

        private T EnsureComponent<T>() where T : Component
        {
            T component = roadGraphProvider.GetComponent<T>();
            if (component == null)
            {
                component = roadGraphProvider.gameObject.AddComponent<T>();
            }

            return component;
        }

        public static LwsRoadGraph CreateDefaultGraph()
        {
            Vector3[] centerline = CreateReferenceCenterline();
            float carriagewayOffset = MedianWidthMeters * 0.5f + CarriagewayWidthMeters * 0.5f;
            Vector3[] northbound = OffsetPolyline(centerline, carriagewayOffset);
            Vector3[] southbound = Reverse(OffsetPolyline(centerline, -carriagewayOffset));
            Vector3[] ramp =
            {
                new Vector3(-35f, RoadSurfaceY, -110f),
                new Vector3(-18f, RoadSurfaceY, -40f),
                new Vector3(3f, RoadSurfaceY, 30f),
                new Vector3(carriagewayOffset, RoadSurfaceY, 135f)
            };
            Vector3[] crossover =
            {
                northbound[northbound.Length - 1],
                new Vector3(0f, centerline[centerline.Length - 1].y, centerline[centerline.Length - 1].z + 55f),
                OffsetPolyline(centerline, -carriagewayOffset)[centerline.Length - 1]
            };

            var graph = new LwsRoadGraph { graphId = GraphId };
            AddEdge(graph, "IH_TEST_I000_NB", "IH_TEST_I000_NB_MAIN", "IH_TEST_I000_NB_MAIN_EDGE", LwsRoadDirection.Northbound, LwsRoadClass.Interstate, 2, 65f, northbound, CarriagewayWidthMeters);
            AddEdge(graph, "IH_TEST_I000_SB", "IH_TEST_I000_SB_MAIN", "IH_TEST_I000_SB_MAIN_EDGE", LwsRoadDirection.Southbound, LwsRoadClass.Interstate, 2, 65f, southbound, CarriagewayWidthMeters);
            AddEdge(graph, "IH_TEST_I000_RAMP", "IH_TEST_I000_NB_ENTRY_RAMP", "IH_TEST_I000_NB_ENTRY_RAMP_EDGE", LwsRoadDirection.Northbound, LwsRoadClass.Ramp, 1, 35f, ramp, 7.5f);
            AddEdge(graph, "IH_TEST_I000_TURN", "IH_TEST_I000_TURNAROUND_CROSSOVER", "IH_TEST_I000_TURNAROUND_CROSSOVER_EDGE", LwsRoadDirection.Bidirectional, LwsRoadClass.Ramp, 1, 25f, crossover, 10f);
            return graph;
        }

        private static void AddEdge(
            LwsRoadGraph graph,
            string roadId,
            string segmentId,
            string edgeId,
            LwsRoadDirection direction,
            LwsRoadClass roadClass,
            int laneCount,
            float speedLimitMph,
            Vector3[] polyline,
            float roadWidth)
        {
            string startNodeId = $"{segmentId}_START";
            string endNodeId = $"{segmentId}_END";
            graph.nodes.Add(new LwsRoadNode { nodeId = startNodeId, position = polyline[0], regionId = "validation", sceneChunkId = "global.streaming" });
            graph.nodes.Add(new LwsRoadNode { nodeId = endNodeId, position = polyline[polyline.Length - 1], regionId = "validation", sceneChunkId = "global.streaming" });

            var edge = new LwsRoadEdge
            {
                roadId = roadId,
                segmentId = segmentId,
                edgeId = edgeId,
                fromNodeId = startNodeId,
                toNodeId = endNodeId,
                roadClass = roadClass,
                direction = direction,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = direction != LwsRoadDirection.Bidirectional,
                distanceMeters = ComputePolylineLength(polyline),
                travelCost = ComputePolylineLength(polyline),
                speedLimitMph = speedLimitMph,
                laneCount = laneCount,
                laneWidthMeters = LaneWidthMeters,
                leftShoulderWidthMeters = roadClass == LwsRoadClass.Interstate ? LeftShoulderWidthMeters : 0.5f,
                rightShoulderWidthMeters = roadClass == LwsRoadClass.Interstate ? RightShoulderWidthMeters : 1.0f,
                medianWidthMeters = roadClass == LwsRoadClass.Interstate ? MedianWidthMeters : 0f,
                stateOrRegionId = "validation",
                sceneChunkId = "global.streaming"
            };

            for (int i = 0; i < laneCount; i++)
            {
                float laneOffset = ((laneCount - 1) * -0.5f + i) * LaneWidthMeters;
                edge.laneCenterOffsetsMeters.Add(laneOffset);
            }

            PopulateSamples(edge, polyline, roadWidth);
            graph.edges.Add(edge);
        }

        private static void PopulateSamples(LwsRoadEdge edge, Vector3[] polyline, float roadWidth)
        {
            float distance = 0f;
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                Vector3 start = polyline[i];
                Vector3 end = polyline[i + 1];
                Vector3 segment = end - start;
                float length = segment.magnitude;
                if (length <= 0.01f)
                {
                    continue;
                }

                int steps = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(5f, SampleSpacingMeters)));
                for (int step = 0; step < steps; step++)
                {
                    float t = step / (float)steps;
                    edge.samples.Add(CreateSample(edge, Vector3.Lerp(start, end, t), segment.normalized, distance + length * t, roadWidth));
                }

                distance += length;
            }

            Vector3 finalForward = (polyline[polyline.Length - 1] - polyline[polyline.Length - 2]).normalized;
            edge.samples.Add(CreateSample(edge, polyline[polyline.Length - 1], finalForward, distance, roadWidth));
        }

        private static LwsRoadSample CreateSample(LwsRoadEdge edge, Vector3 position, Vector3 forward, float distance, float roadWidth)
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
                roadWidthMeters = roadWidth,
                laneWidthMeters = edge.laneWidthMeters,
                laneCount = edge.laneCount,
                speedLimitMph = edge.speedLimitMph
            };
        }

        private static Vector3[] CreateReferenceCenterline()
        {
            return new[]
            {
                new Vector3(0f, RoadSurfaceY, 0f),
                new Vector3(0f, RoadSurfaceY, 520f),
                new Vector3(105f, RoadSurfaceY + 2f, 1080f),
                new Vector3(255f, RoadSurfaceY + 10f, 1740f),
                new Vector3(170f, RoadSurfaceY + 17f, 2460f),
                new Vector3(0f, RoadSurfaceY + 8f, 3350f)
            };
        }

        private static Vector3[] OffsetPolyline(Vector3[] source, float xOffset)
        {
            var result = new Vector3[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i] + Vector3.right * xOffset;
            }

            return result;
        }

        private static Vector3[] Reverse(Vector3[] source)
        {
            var result = new Vector3[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[source.Length - 1 - i];
            }

            return result;
        }

        private static float ComputePolylineLength(Vector3[] polyline)
        {
            float total = 0f;
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                total += Vector3.Distance(polyline[i], polyline[i + 1]);
            }

            return total;
        }
    }
}
