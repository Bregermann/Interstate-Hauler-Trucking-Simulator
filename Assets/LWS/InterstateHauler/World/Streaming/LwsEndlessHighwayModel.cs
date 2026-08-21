using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public readonly struct LwsEndlessHighwaySlotSnapshot
    {
        public LwsEndlessHighwaySlotSnapshot(
            int physicalSlotIndex,
            int logicalSegmentIndex,
            string logicalSegmentId,
            double globalSegmentStartZ,
            Vector3 localPosition)
        {
            PhysicalSlotIndex = physicalSlotIndex;
            LogicalSegmentIndex = logicalSegmentIndex;
            LogicalSegmentId = logicalSegmentId ?? string.Empty;
            GlobalSegmentStartZ = globalSegmentStartZ;
            LocalPosition = localPosition;
        }

        public int PhysicalSlotIndex { get; }
        public int LogicalSegmentIndex { get; }
        public string LogicalSegmentId { get; }
        public double GlobalSegmentStartZ { get; }
        public Vector3 LocalPosition { get; }
    }

    public static class LwsEndlessHighwayModel
    {
        public const string GraphId = "IH_ENDLESS_STREAMING_VALIDATION_GRAPH";
        public const string SegmentIdPrefix = "IH_ENDLESS_TEST_SEG";
        public const double SegmentLengthMeters = 3218.688d;
        public const float MetersPerMile = 1609.344f;
        public const float LaneWidthMeters = 3.7f;
        public const float RightShoulderWidthMeters = 3.0f;
        public const float LeftShoulderWidthMeters = 1.2f;
        public const float MedianWidthMeters = 14.0f;
        public const float RoadSurfaceY = 0.55f;
        public const float SpeedLimitMph = 65f;
        public const int DefaultBehindSegments = 2;
        public const int DefaultAheadSegments = 4;
        public const int DefaultPhysicalChunkPoolSize = DefaultBehindSegments + 1 + DefaultAheadSegments;
        public const float DefaultRoadAheadTargetMeters = 6400f;
        public const float DefaultRoadAheadEmergencyThresholdMeters = 1600f;
        private const float SampleSpacingMeters = 100f;

        public static float CarriagewayWidthMeters => LaneWidthMeters * 2f + RightShoulderWidthMeters + LeftShoulderWidthMeters;
        public static float CarriagewayOffsetMeters => MedianWidthMeters * 0.5f + CarriagewayWidthMeters * 0.5f;

        public static int GetSegmentIndex(double globalZ)
        {
            return Math.Max(0, (int)Math.Floor(globalZ / SegmentLengthMeters));
        }

        public static double GetSegmentStartZ(int logicalSegmentIndex)
        {
            return Math.Max(0, logicalSegmentIndex) * SegmentLengthMeters;
        }

        public static double GetSegmentEndZ(int logicalSegmentIndex)
        {
            return GetSegmentStartZ(logicalSegmentIndex) + SegmentLengthMeters;
        }

        public static string FormatSegmentId(int logicalSegmentIndex)
        {
            return $"{SegmentIdPrefix}_{Math.Max(0, logicalSegmentIndex):000000}";
        }

        public static float CalculateMetersOfRoadAhead(double playerGlobalZ, int highestSegmentGenerated)
        {
            double endZ = GetSegmentEndZ(highestSegmentGenerated);
            return (float)Math.Max(0d, endZ - playerGlobalZ);
        }

        public static bool IsRoadAheadUnsafe(float metersOfRoadAvailableAhead, float emergencyThresholdMeters)
        {
            return metersOfRoadAvailableAhead < Mathf.Max(1f, emergencyThresholdMeters);
        }

        public static Vector3 CalculateLocalSegmentPosition(int logicalSegmentIndex, LwsWorldPositionD originOffset)
        {
            return new LwsWorldPositionD(0d, 0d, GetSegmentStartZ(logicalSegmentIndex)).ToLocalVector3(originOffset);
        }

        public static LwsRoadGraph BuildRoadGraph(int firstLogicalSegmentIndex, int segmentCount)
        {
            int first = Math.Max(0, firstLogicalSegmentIndex);
            int count = Math.Max(1, segmentCount);
            var graph = new LwsRoadGraph { graphId = $"{GraphId}_{first:000000}_{first + count - 1:000000}" };

            for (int i = 0; i < count; i++)
            {
                int segmentIndex = first + i;
                AddStraightInterstateEdge(graph, segmentIndex, LwsRoadDirection.Northbound);
                AddStraightInterstateEdge(graph, segmentIndex, LwsRoadDirection.Southbound);
            }

            return graph;
        }

        public static IReadOnlyList<Vector3> BuildSegmentCenterline(int logicalSegmentIndex, float xOffset, bool reverse)
        {
            double start = GetSegmentStartZ(logicalSegmentIndex);
            int steps = Mathf.Max(2, Mathf.CeilToInt((float)SegmentLengthMeters / SampleSpacingMeters) + 1);
            var samples = new Vector3[steps];
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                double z = start + SegmentLengthMeters * t;
                int index = reverse ? steps - 1 - i : i;
                samples[index] = new Vector3(xOffset, RoadSurfaceY, (float)z);
            }

            return samples;
        }

        private static void AddStraightInterstateEdge(LwsRoadGraph graph, int logicalSegmentIndex, LwsRoadDirection direction)
        {
            string segmentId = $"{FormatSegmentId(logicalSegmentIndex)}_{(direction == LwsRoadDirection.Northbound ? "NB" : "SB")}";
            string roadId = segmentId;
            string edgeId = $"{segmentId}_EDGE";
            bool southbound = direction == LwsRoadDirection.Southbound;
            IReadOnlyList<Vector3> samples = BuildSegmentCenterline(logicalSegmentIndex, southbound ? -CarriagewayOffsetMeters : CarriagewayOffsetMeters, southbound);
            string startNodeId = $"{segmentId}_START";
            string endNodeId = $"{segmentId}_END";
            graph.nodes.Add(new LwsRoadNode { nodeId = startNodeId, position = samples[0], regionId = "endless_validation", sceneChunkId = FormatSegmentId(logicalSegmentIndex) });
            graph.nodes.Add(new LwsRoadNode { nodeId = endNodeId, position = samples[samples.Count - 1], regionId = "endless_validation", sceneChunkId = FormatSegmentId(logicalSegmentIndex) });

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
                distanceMeters = (float)SegmentLengthMeters,
                travelCost = (float)SegmentLengthMeters,
                speedLimitMph = SpeedLimitMph,
                laneCount = 2,
                laneWidthMeters = LaneWidthMeters,
                leftShoulderWidthMeters = LeftShoulderWidthMeters,
                rightShoulderWidthMeters = RightShoulderWidthMeters,
                medianWidthMeters = MedianWidthMeters,
                stateOrRegionId = "endless_validation",
                sceneChunkId = FormatSegmentId(logicalSegmentIndex)
            };

            edge.laneCenterOffsetsMeters.Add(-LaneWidthMeters * 0.5f);
            edge.laneCenterOffsetsMeters.Add(LaneWidthMeters * 0.5f);
            PopulateSamples(edge, samples);
            graph.edges.Add(edge);
        }

        private static void PopulateSamples(LwsRoadEdge edge, IReadOnlyList<Vector3> positions)
        {
            float distance = 0f;
            for (int i = 0; i < positions.Count; i++)
            {
                if (i > 0)
                {
                    distance += Vector3.Distance(positions[i - 1], positions[i]);
                }

                Vector3 forward = ResolveForward(positions, i);
                edge.samples.Add(new LwsRoadSample
                {
                    roadId = edge.roadId,
                    segmentId = edge.segmentId,
                    distanceFromStartMeters = distance,
                    position = positions[i],
                    forward = forward,
                    up = Vector3.up,
                    direction = edge.direction,
                    roadWidthMeters = CarriagewayWidthMeters,
                    laneWidthMeters = LaneWidthMeters,
                    laneCount = 2,
                    speedLimitMph = SpeedLimitMph
                });
            }
        }

        private static Vector3 ResolveForward(IReadOnlyList<Vector3> samples, int index)
        {
            if (samples == null || samples.Count < 2)
            {
                return Vector3.forward;
            }

            if (index < samples.Count - 1)
            {
                Vector3 next = samples[index + 1] - samples[index];
                if (next.sqrMagnitude > 0.0001f)
                {
                    return next.normalized;
                }
            }

            Vector3 previous = samples[index] - samples[Mathf.Max(0, index - 1)];
            return previous.sqrMagnitude > 0.0001f ? previous.normalized : Vector3.forward;
        }
    }
}
