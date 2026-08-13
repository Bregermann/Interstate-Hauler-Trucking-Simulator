using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public readonly struct LwsRoadLookupResult
    {
        public LwsRoadLookupResult(
            bool found,
            string roadId,
            string segmentId,
            string edgeId,
            LwsRoadClass roadClass,
            LwsRoadDirection direction,
            LwsRoadSurfaceType surfaceType,
            float speedLimitMph,
            int laneCount,
            float laneWidthMeters,
            float distanceAlongSegmentMeters,
            float lateralDistanceMeters,
            Vector3 nearestPosition,
            Vector3 forward)
        {
            Found = found;
            RoadId = roadId ?? string.Empty;
            SegmentId = segmentId ?? string.Empty;
            EdgeId = edgeId ?? string.Empty;
            RoadClass = roadClass;
            Direction = direction;
            SurfaceType = surfaceType;
            SpeedLimitMph = speedLimitMph;
            LaneCount = laneCount;
            LaneWidthMeters = laneWidthMeters;
            DistanceAlongSegmentMeters = distanceAlongSegmentMeters;
            LateralDistanceMeters = lateralDistanceMeters;
            NearestPosition = nearestPosition;
            Forward = forward;
        }

        public bool Found { get; }
        public string RoadId { get; }
        public string SegmentId { get; }
        public string EdgeId { get; }
        public LwsRoadClass RoadClass { get; }
        public LwsRoadDirection Direction { get; }
        public LwsRoadSurfaceType SurfaceType { get; }
        public float SpeedLimitMph { get; }
        public int LaneCount { get; }
        public float LaneWidthMeters { get; }
        public float DistanceAlongSegmentMeters { get; }
        public float LateralDistanceMeters { get; }
        public Vector3 NearestPosition { get; }
        public Vector3 Forward { get; }

        public static LwsRoadLookupResult None => new LwsRoadLookupResult(
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            LwsRoadClass.Unknown,
            LwsRoadDirection.Unknown,
            LwsRoadSurfaceType.Unknown,
            0f,
            0,
            0f,
            0f,
            float.PositiveInfinity,
            Vector3.zero,
            Vector3.forward);
    }

    public interface ILwsRoadGraphService : ILwsService
    {
        LwsRoadGraph ActiveGraph { get; }
        LwsRoadGraphValidationResult SetActiveGraph(LwsRoadGraph graph);
        bool TryFindNearestRoad(Vector3 worldPosition, float maxDistanceMeters, out LwsRoadLookupResult result);
    }

    public sealed class LwsRoadGraphService : ILwsRoadGraphService
    {
        public string ServiceId => "lws.road.graph";
        public LwsRoadGraph ActiveGraph { get; private set; }

        public LwsServiceResult Initialize(LwsServiceContext context)
        {
            ActiveGraph = null;
            return LwsServiceResult.Success("LWS road graph service initialized.");
        }

        public LwsServiceResult Shutdown(LwsServiceContext context)
        {
            ActiveGraph = null;
            return LwsServiceResult.Success("LWS road graph service shut down.");
        }

        public LwsRoadGraphValidationResult SetActiveGraph(LwsRoadGraph graph)
        {
            if (graph == null)
            {
                ActiveGraph = null;
                return new LwsRoadGraphValidationResult(false, new[] { "Road graph is null." });
            }

            LwsRoadGraphValidationResult validation = graph.Validate();
            if (validation.IsValid)
            {
                ActiveGraph = graph;
            }

            return validation;
        }

        public bool TryFindNearestRoad(Vector3 worldPosition, float maxDistanceMeters, out LwsRoadLookupResult result)
        {
            return LwsRoadGraphQuery.TryFindNearestRoad(ActiveGraph, worldPosition, maxDistanceMeters, out result);
        }
    }

    public static class LwsRoadGraphQuery
    {
        public static bool TryFindNearestRoad(
            LwsRoadGraph graph,
            Vector3 worldPosition,
            float maxDistanceMeters,
            out LwsRoadLookupResult result)
        {
            result = LwsRoadLookupResult.None;
            if (graph == null || graph.edges == null || graph.edges.Count == 0)
            {
                return false;
            }

            float bestSqr = maxDistanceMeters > 0f
                ? maxDistanceMeters * maxDistanceMeters
                : float.PositiveInfinity;
            LwsRoadLookupResult best = LwsRoadLookupResult.None;

            foreach (LwsRoadEdge edge in graph.edges)
            {
                if (edge == null || edge.samples == null || edge.samples.Count == 0)
                {
                    continue;
                }

                for (int i = 0; i < edge.samples.Count; i++)
                {
                    LwsRoadSample sample = edge.samples[i];
                    if (sample == null)
                    {
                        continue;
                    }

                    Vector3 nearest = sample.position;
                    float distanceAlong = sample.distanceFromStartMeters;
                    Vector3 forward = sample.forward.sqrMagnitude > 0.0001f
                        ? sample.forward.normalized
                        : Vector3.forward;

                    if (i < edge.samples.Count - 1 && edge.samples[i + 1] != null)
                    {
                        LwsRoadSample next = edge.samples[i + 1];
                        Vector3 segment = next.position - sample.position;
                        float segmentSqr = segment.sqrMagnitude;
                        if (segmentSqr > 0.0001f)
                        {
                            float t = Mathf.Clamp01(Vector3.Dot(worldPosition - sample.position, segment) / segmentSqr);
                            nearest = Vector3.Lerp(sample.position, next.position, t);
                            forward = segment.normalized;
                            distanceAlong = Mathf.Lerp(sample.distanceFromStartMeters, next.distanceFromStartMeters, t);
                        }
                    }

                    float sqr = (worldPosition - nearest).sqrMagnitude;
                    if (sqr >= bestSqr)
                    {
                        continue;
                    }

                    bestSqr = sqr;
                    best = new LwsRoadLookupResult(
                        true,
                        string.IsNullOrWhiteSpace(edge.roadId) ? sample.roadId : edge.roadId,
                        string.IsNullOrWhiteSpace(edge.segmentId) ? sample.segmentId : edge.segmentId,
                        edge.edgeId,
                        edge.roadClass,
                        edge.direction,
                        edge.surfaceType,
                        edge.speedLimitMph,
                        edge.laneCount,
                        edge.laneWidthMeters,
                        distanceAlong,
                        Mathf.Sqrt(sqr),
                        nearest,
                        forward);
                }
            }

            result = best;
            return best.Found;
        }
    }

}
