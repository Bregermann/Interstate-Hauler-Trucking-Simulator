using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public static class LwsFiftyMileHighwayModel
    {
        public const string WorldId = "IH_50_MILE_FLOATING_ORIGIN_WORLD";
        public const string GraphId = "IH_50_MILE_GLOBAL_ROAD_GRAPH";
        public const string EastboundRoadId = "IH_TEST_50MI_EB";
        public const string WestboundRoadId = "IH_TEST_50MI_WB";
        public const string EastboundSegmentId = "IH_TEST_50MI_EB_MAIN";
        public const string WestboundSegmentId = "IH_TEST_50MI_WB_MAIN";
        public const string EastboundEdgeId = "IH_TEST_50MI_EB_MAIN_EDGE";
        public const string WestboundEdgeId = "IH_TEST_50MI_WB_MAIN_EDGE";
        public const string ValidationScenePath = "Assets/LWS/InterstateHauler/World/Origin/Validation/IH_50MileFloatingOriginValidation.unity";
        public const string ChunkSceneDirectory = "Assets/LWS/InterstateHauler/World/Origin/Validation/Chunks";

        public const double MetersPerMile = 1609.344d;
        public const double TotalMiles = 50d;
        public const double TotalLengthMeters = 80467.2d;
        public const int ChunkCount = 25;
        public const double ChunkLengthMeters = 3218.688d;
        public const int WholeMileMarkerCount = 51;

        public const float LaneWidthMeters = 3.7f;
        public const float RightShoulderWidthMeters = 3.0f;
        public const float LeftShoulderWidthMeters = 1.2f;
        public const float MedianWidthMeters = 14.0f;
        public const float RoadSurfaceY = 0.55f;
        public const float SpeedLimitMph = 65f;
        public const float RoadGraphSampleSpacingMeters = 500f;

        public static float CarriagewayWidthMeters =>
            LaneWidthMeters * 2f + RightShoulderWidthMeters + LeftShoulderWidthMeters;

        public static float CarriagewayOffsetMeters =>
            MedianWidthMeters * 0.5f + CarriagewayWidthMeters * 0.5f;

        public static Vector3 EastboundStartPosition =>
            EastboundLanePosition(0d, 0);

        public static Vector3 EastboundDestinationPosition =>
            EastboundLanePosition(TotalLengthMeters, 0);

        public static Vector3 WestboundStartPosition =>
            WestboundLanePosition(TotalLengthMeters, 0);

        public static string GetChunkId(int chunkIndex)
        {
            return $"IH_50MI_CHUNK_{Mathf.Clamp(chunkIndex, 0, ChunkCount - 1):000}";
        }

        public static string GetChunkDisplayName(int chunkIndex)
        {
            return $"50-Mile Validation Chunk {Mathf.Clamp(chunkIndex, 0, ChunkCount - 1):000}";
        }

        public static string GetChunkSceneName(int chunkIndex)
        {
            return $"IH_50Mile_Chunk_{Mathf.Clamp(chunkIndex, 0, ChunkCount - 1):000}";
        }

        public static string GetChunkScenePath(int chunkIndex)
        {
            return $"{ChunkSceneDirectory}/{GetChunkSceneName(chunkIndex)}.unity";
        }

        public static double GetChunkStartMeters(int chunkIndex)
        {
            return Mathf.Clamp(chunkIndex, 0, ChunkCount - 1) * ChunkLengthMeters;
        }

        public static double GetChunkEndMeters(int chunkIndex)
        {
            return Math.Min(TotalLengthMeters, GetChunkStartMeters(chunkIndex) + ChunkLengthMeters);
        }

        public static double MileToMeters(double mile)
        {
            return Math.Max(0d, Math.Min(TotalMiles, mile)) * MetersPerMile;
        }

        public static double MetersToMile(double meters)
        {
            return Math.Max(0d, Math.Min(TotalLengthMeters, meters)) / MetersPerMile;
        }

        public static int GetChunkIndexForMeters(double globalDistanceMeters)
        {
            double clamped = Math.Max(0d, Math.Min(TotalLengthMeters, globalDistanceMeters));
            if (clamped >= TotalLengthMeters)
            {
                return ChunkCount - 1;
            }

            return Mathf.Clamp((int)Math.Floor(clamped / ChunkLengthMeters), 0, ChunkCount - 1);
        }

        public static Vector3 GlobalCenterlinePosition(double globalDistanceMeters)
        {
            return new Vector3(0f, RoadSurfaceY, (float)Math.Max(0d, Math.Min(TotalLengthMeters, globalDistanceMeters)));
        }

        public static Vector3 EastboundCarriagewayPosition(double globalDistanceMeters)
        {
            return GlobalCenterlinePosition(globalDistanceMeters) + Vector3.right * CarriagewayOffsetMeters;
        }

        public static Vector3 WestboundCarriagewayPosition(double globalDistanceMeters)
        {
            return GlobalCenterlinePosition(globalDistanceMeters) - Vector3.right * CarriagewayOffsetMeters;
        }

        public static Vector3 EastboundLanePosition(double globalDistanceMeters, int laneIndex)
        {
            return EastboundCarriagewayPosition(globalDistanceMeters) + Vector3.right * ResolveLaneOffset(laneIndex, 2);
        }

        public static Vector3 WestboundLanePosition(double globalDistanceMeters, int laneIndex)
        {
            return WestboundCarriagewayPosition(globalDistanceMeters) - Vector3.right * ResolveLaneOffset(laneIndex, 2);
        }

        public static IEnumerable<int> EnumerateWholeMileMarkersForChunk(int chunkIndex)
        {
            for (int mile = 0; mile <= (int)TotalMiles; mile++)
            {
                int owner = GetChunkIndexForMeters(MileToMeters(mile));
                if (owner == chunkIndex)
                {
                    yield return mile;
                }
            }
        }

        public static IReadOnlyList<LwsWorldChunkDefinition> CreateChunkDefinitions()
        {
            var chunks = new List<LwsWorldChunkDefinition>(ChunkCount);
            for (int i = 0; i < ChunkCount; i++)
            {
                double start = GetChunkStartMeters(i);
                double end = GetChunkEndMeters(i);
                var neighbors = new List<string>(2);
                if (i > 0)
                {
                    neighbors.Add(GetChunkId(i - 1));
                }

                if (i < ChunkCount - 1)
                {
                    neighbors.Add(GetChunkId(i + 1));
                }

                chunks.Add(new LwsWorldChunkDefinition
                {
                    chunkId = GetChunkId(i),
                    displayName = GetChunkDisplayName(i),
                    sceneName = GetChunkSceneName(i),
                    scenePath = GetChunkScenePath(i),
                    boundsCenter = new Vector3(0f, 0f, (float)((start + end) * 0.5d)),
                    boundsSize = new Vector3(320f, 160f, (float)(end - start)),
                    neighborChunkIds = neighbors,
                    roadIds = new List<string>
                    {
                        $"{EastboundRoadId}_{i:000}",
                        $"{WestboundRoadId}_{i:000}"
                    },
                    trafficContent = "50-mile validation traffic lane presentation.",
                    weatheradeContent = "50-mile validation asphalt wetness/snow presentation.",
                    preloadPriority = i,
                    containsRoadGeometry = true,
                    containsTrafficPresentation = true,
                    containsWeatheradePresentation = true,
                    containsGlobalServices = false,
                    containsPlayerContent = false
                });
            }

            return chunks;
        }

        public static LwsWorldStreamingManifest CreateManifest(LwsWorldStreamingPolicy policy)
        {
            LwsWorldStreamingManifest manifest = ScriptableObject.CreateInstance<LwsWorldStreamingManifest>();
            manifest.worldId = WorldId;
            manifest.policy = policy;
            manifest.chunks = new List<LwsWorldChunkDefinition>(CreateChunkDefinitions());
            return manifest;
        }

        public static LwsRoadGraph CreateRoadGraph()
        {
            var graph = new LwsRoadGraph { graphId = GraphId };
            AddRoadEdge(
                graph,
                EastboundRoadId,
                EastboundSegmentId,
                EastboundEdgeId,
                LwsRoadDirection.Eastbound,
                EastboundCarriagewayPosition,
                0d,
                TotalLengthMeters,
                Vector3.forward);
            AddRoadEdge(
                graph,
                WestboundRoadId,
                WestboundSegmentId,
                WestboundEdgeId,
                LwsRoadDirection.Westbound,
                WestboundCarriagewayPosition,
                TotalLengthMeters,
                0d,
                Vector3.back);
            return graph;
        }

        public static string GetWeatherPresetForMile(double mile)
        {
            if (mile < 5d)
            {
                return LwsWeatherPresetCatalog.ClearId;
            }

            if (mile < 10d)
            {
                return LwsWeatherPresetCatalog.PartlyCloudyId;
            }

            if (mile < 15d)
            {
                return LwsWeatherPresetCatalog.OvercastId;
            }

            if (mile < 20d)
            {
                return LwsWeatherPresetCatalog.LightRainId;
            }

            if (mile < 25d)
            {
                return LwsWeatherPresetCatalog.HeavyRainId;
            }

            if (mile < 30d)
            {
                return LwsWeatherPresetCatalog.ThunderstormId;
            }

            if (mile < 35d)
            {
                return LwsWeatherPresetCatalog.FogId;
            }

            if (mile < 40d)
            {
                return LwsWeatherPresetCatalog.LightSnowId;
            }

            if (mile < 45d)
            {
                return LwsWeatherPresetCatalog.HeavySnowId;
            }

            return LwsWeatherPresetCatalog.ClearId;
        }

        private static void AddRoadEdge(
            LwsRoadGraph graph,
            string roadId,
            string segmentId,
            string edgeId,
            LwsRoadDirection direction,
            Func<double, Vector3> positionAtDistance,
            double startDistance,
            double endDistance,
            Vector3 forward)
        {
            string startNodeId = $"{segmentId}_START";
            string endNodeId = $"{segmentId}_END";
            Vector3 start = positionAtDistance(startDistance);
            Vector3 end = positionAtDistance(endDistance);
            graph.nodes.Add(new LwsRoadNode { nodeId = startNodeId, position = start, regionId = "validation.50mile", sceneChunkId = "global.50mile" });
            graph.nodes.Add(new LwsRoadNode { nodeId = endNodeId, position = end, regionId = "validation.50mile", sceneChunkId = "global.50mile" });

            var edge = new LwsRoadEdge
            {
                roadId = roadId,
                segmentId = segmentId,
                edgeId = edgeId,
                fromNodeId = startNodeId,
                toNodeId = endNodeId,
                roadClass = LwsRoadClass.Interstate,
                direction = direction,
                surfaceType = LwsRoadSurfaceType.AsphaltInterstate,
                oneWay = true,
                distanceMeters = (float)TotalLengthMeters,
                travelCost = (float)TotalLengthMeters,
                speedLimitMph = SpeedLimitMph,
                laneCount = 2,
                laneWidthMeters = LaneWidthMeters,
                leftShoulderWidthMeters = LeftShoulderWidthMeters,
                rightShoulderWidthMeters = RightShoulderWidthMeters,
                medianWidthMeters = MedianWidthMeters,
                stateOrRegionId = "validation.50mile",
                sceneChunkId = "global.50mile"
            };
            edge.laneCenterOffsetsMeters.Add(ResolveLaneOffset(0, 2));
            edge.laneCenterOffsetsMeters.Add(ResolveLaneOffset(1, 2));

            PopulateRoadSamples(edge, positionAtDistance, startDistance, endDistance, forward);
            graph.edges.Add(edge);
        }

        private static void PopulateRoadSamples(
            LwsRoadEdge edge,
            Func<double, Vector3> positionAtDistance,
            double startDistance,
            double endDistance,
            Vector3 forward)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt((float)(Math.Abs(endDistance - startDistance) / RoadGraphSampleSpacingMeters)));
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                double globalDistance = startDistance + (endDistance - startDistance) * t;
                edge.samples.Add(new LwsRoadSample
                {
                    roadId = edge.roadId,
                    segmentId = edge.segmentId,
                    distanceFromStartMeters = (float)(TotalLengthMeters * t),
                    position = positionAtDistance(globalDistance),
                    forward = forward,
                    up = Vector3.up,
                    direction = edge.direction,
                    roadWidthMeters = CarriagewayWidthMeters,
                    laneWidthMeters = LaneWidthMeters,
                    laneCount = edge.laneCount,
                    speedLimitMph = edge.speedLimitMph
                });
            }
        }

        private static float ResolveLaneOffset(int laneIndex, int laneCount)
        {
            int clamped = Mathf.Clamp(laneIndex, 0, Mathf.Max(0, laneCount - 1));
            return ((laneCount - 1) * -0.5f + clamped) * LaneWidthMeters;
        }
    }
}
