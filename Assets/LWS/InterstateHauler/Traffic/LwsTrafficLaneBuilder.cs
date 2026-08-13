using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public static class LwsTrafficLaneBuilder
    {
        public static IReadOnlyList<LwsTrafficLaneDefinition> BuildTrafficLanes(LwsRoadGraph graph, LwsTrafficSpawnPolicy policy)
        {
            var lanes = new List<LwsTrafficLaneDefinition>();
            if (graph == null || graph.edges == null)
            {
                return lanes;
            }

            for (int edgeIndex = 0; edgeIndex < graph.edges.Count; edgeIndex++)
            {
                LwsRoadEdge edge = graph.edges[edgeIndex];
                if (!CanUseEdge(edge, policy))
                {
                    continue;
                }

                int laneCount = Mathf.Max(1, edge.laneCount);
                for (int lane = 0; lane < laneCount; lane++)
                {
                    float offset = ResolveLaneOffset(edge, lane, laneCount);
                    Vector3[] centerline = BuildLaneCenterline(edge, offset);
                    var definition = new LwsTrafficLaneDefinition
                    {
                        laneId = $"{edge.segmentId}_TRAFFIC_L{lane + 1}",
                        roadId = edge.roadId,
                        segmentId = edge.segmentId,
                        edgeId = edge.edgeId,
                        laneIndex = lane,
                        roadClass = edge.roadClass,
                        direction = edge.direction,
                        speedLimitMph = edge.speedLimitMph,
                        laneWidthMeters = edge.laneWidthMeters,
                        laneCenterOffsetMeters = offset,
                        lengthMeters = edge.distanceMeters,
                        spawnEnabled = true,
                        centerline = centerline
                    };

                    lanes.Add(definition);
                }
            }

            return lanes;
        }

        public static float MilesPerHourToMetersPerSecond(float mph)
        {
            return mph * 0.44704f;
        }

        public static int FindNearestPointIndex(LwsTrafficLaneDefinition lane, Vector3 worldPosition)
        {
            if (lane == null || lane.centerline == null || lane.centerline.Length == 0)
            {
                return 0;
            }

            int bestIndex = 0;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < lane.centerline.Length; i++)
            {
                float sqr = (worldPosition - lane.centerline[i]).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public static int ChooseSpawnPointIndex(
            LwsTrafficLaneDefinition lane,
            Vector3 playerPosition,
            float minimumDistanceMeters,
            float maximumDistanceMeters,
            int fallbackOffset)
        {
            if (lane == null || lane.centerline == null || lane.centerline.Length < 3)
            {
                return 1;
            }

            int nearest = FindNearestPointIndex(lane, playerPosition);
            int minOffset = Mathf.Max(2, Mathf.RoundToInt(minimumDistanceMeters / EstimatePointSpacing(lane)));
            int maxOffset = Mathf.Max(minOffset + 1, Mathf.RoundToInt(maximumDistanceMeters / EstimatePointSpacing(lane)));
            int desired = nearest + Mathf.Clamp(fallbackOffset, minOffset, maxOffset);
            return Mathf.Clamp(desired, 1, lane.centerline.Length - 2);
        }

        private static bool CanUseEdge(LwsRoadEdge edge, LwsTrafficSpawnPolicy policy)
        {
            if (edge == null || edge.samples == null || edge.samples.Count < 3 || edge.laneCount <= 0)
            {
                return false;
            }

            if (edge.roadClass == LwsRoadClass.Interstate)
            {
                return true;
            }

            if (edge.roadClass == LwsRoadClass.Ramp)
            {
                if (edge.direction == LwsRoadDirection.Bidirectional)
                {
                    return policy != null && policy.includeTurnaroundTraffic;
                }

                return policy == null || policy.includeRampTraffic;
            }

            return false;
        }

        private static float ResolveLaneOffset(LwsRoadEdge edge, int lane, int laneCount)
        {
            if (edge.laneCenterOffsetsMeters != null && lane < edge.laneCenterOffsetsMeters.Count)
            {
                return edge.laneCenterOffsetsMeters[lane];
            }

            return ((laneCount - 1) * -0.5f + lane) * Mathf.Max(3.0f, edge.laneWidthMeters);
        }

        private static Vector3[] BuildLaneCenterline(LwsRoadEdge edge, float laneOffset)
        {
            var points = new List<Vector3>(edge.samples.Count);
            for (int i = 0; i < edge.samples.Count; i++)
            {
                LwsRoadSample sample = edge.samples[i];
                if (sample == null)
                {
                    continue;
                }

                Vector3 forward = sample.forward.sqrMagnitude > 0.0001f ? sample.forward.normalized : Vector3.forward;
                Vector3 up = sample.up.sqrMagnitude > 0.0001f ? sample.up.normalized : Vector3.up;
                Vector3 right = Vector3.Cross(up, forward).normalized;
                points.Add(sample.position + right * laneOffset + up * 0.1f);
            }

            return points.ToArray();
        }

        private static float EstimatePointSpacing(LwsTrafficLaneDefinition lane)
        {
            if (lane == null || lane.centerline == null || lane.centerline.Length < 2)
            {
                return 50f;
            }

            float total = 0f;
            int samples = 0;
            for (int i = 0; i < lane.centerline.Length - 1; i++)
            {
                total += Vector3.Distance(lane.centerline[i], lane.centerline[i + 1]);
                samples++;
            }

            return samples > 0 ? Mathf.Max(1f, total / samples) : 50f;
        }
    }
}
