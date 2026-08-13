using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public enum LwsRoadClass
    {
        Unknown,
        Interstate,
        UsHighway,
        StateHighway,
        Ramp,
        LocalRoad,
        DepotAccess
    }

    public enum LwsRoadDirection
    {
        Unknown,
        Northbound,
        Southbound,
        Eastbound,
        Westbound,
        Bidirectional
    }

    public enum LwsRoadSurfaceType
    {
        Unknown,
        AsphaltInterstate,
        WetAsphalt,
        Snow,
        Ice,
        Dirt,
        Gravel,
        DamagedPavement
    }

    [Serializable]
    public sealed class LwsRoadRestriction
    {
        public bool truckAllowed = true;
        public float maxVehicleHeightMeters;
        public float maxVehicleWeightKg;
        public string restrictionId;
    }

    [Serializable]
    public sealed class LwsRoadNode
    {
        public string nodeId;
        public Vector3 position;
        public string regionId;
        public string sceneChunkId;
    }

    [Serializable]
    public sealed class LwsRoadSample
    {
        public string roadId;
        public string segmentId;
        public float distanceFromStartMeters;
        public Vector3 position;
        public Vector3 forward;
        public Vector3 up = Vector3.up;
        public LwsRoadDirection direction;
        public float roadWidthMeters;
        public float laneWidthMeters;
        public int laneCount;
        public float speedLimitMph;
    }

    [Serializable]
    public sealed class LwsRoadEdge
    {
        public string roadId;
        public string segmentId;
        public string edgeId;
        public string fromNodeId;
        public string toNodeId;
        public LwsRoadClass roadClass;
        public LwsRoadDirection direction;
        public LwsRoadSurfaceType surfaceType;
        public bool oneWay;
        public float distanceMeters;
        public float travelCost;
        public float speedLimitMph;
        public int laneCount;
        public float laneWidthMeters;
        public float leftShoulderWidthMeters;
        public float rightShoulderWidthMeters;
        public float medianWidthMeters;
        public List<float> laneCenterOffsetsMeters = new List<float>();
        public string stateOrRegionId;
        public string sceneChunkId;
        public List<LwsRoadRestriction> restrictions = new List<LwsRoadRestriction>();
        public List<LwsRoadSample> samples = new List<LwsRoadSample>();
    }

    [Serializable]
    public sealed class LwsRoadGraph
    {
        public int schemaVersion = 1;
        public string graphId;
        public List<LwsRoadNode> nodes = new List<LwsRoadNode>();
        public List<LwsRoadEdge> edges = new List<LwsRoadEdge>();

        public LwsRoadGraphValidationResult Validate()
        {
            var errors = new List<string>();
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var roadIds = new HashSet<string>(StringComparer.Ordinal);
            var segmentIds = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(graphId))
            {
                errors.Add("Road graph has an empty graph ID.");
            }

            foreach (LwsRoadNode node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                {
                    errors.Add("Road graph contains a node with an empty ID.");
                    continue;
                }

                if (!nodeIds.Add(node.nodeId))
                {
                    errors.Add($"Duplicate road node ID: {node.nodeId}");
                }
            }

            foreach (LwsRoadEdge edge in edges)
            {
                if (edge == null || string.IsNullOrWhiteSpace(edge.edgeId))
                {
                    errors.Add("Road graph contains an edge with an empty ID.");
                    continue;
                }

                if (!edgeIds.Add(edge.edgeId))
                {
                    errors.Add($"Duplicate road edge ID: {edge.edgeId}");
                }

                bool requiresHighwayMetadata = edge.roadClass != LwsRoadClass.Unknown;
                if (requiresHighwayMetadata)
                {
                    if (string.IsNullOrWhiteSpace(edge.roadId))
                    {
                        errors.Add($"Road edge {edge.edgeId} has an empty road ID.");
                    }
                    else if (!roadIds.Add(edge.roadId))
                    {
                        errors.Add($"Duplicate road ID: {edge.roadId}");
                    }

                    if (string.IsNullOrWhiteSpace(edge.segmentId))
                    {
                        errors.Add($"Road edge {edge.edgeId} has an empty segment ID.");
                    }
                    else if (!segmentIds.Add(edge.segmentId))
                    {
                        errors.Add($"Duplicate road segment ID: {edge.segmentId}");
                    }

                    if (edge.speedLimitMph <= 0f)
                    {
                        errors.Add($"Road edge {edge.edgeId} has an invalid speed limit.");
                    }

                    if (edge.laneCount <= 0)
                    {
                        errors.Add($"Road edge {edge.edgeId} has an invalid lane count.");
                    }

                    if (edge.laneWidthMeters <= 0f)
                    {
                        errors.Add($"Road edge {edge.edgeId} has an invalid lane width.");
                    }

                    if (edge.surfaceType == LwsRoadSurfaceType.Unknown)
                    {
                        errors.Add($"Road edge {edge.edgeId} has an unknown surface type.");
                    }
                }

                if (!nodeIds.Contains(edge.fromNodeId))
                {
                    errors.Add($"Road edge {edge.edgeId} references missing from-node {edge.fromNodeId}.");
                }

                if (!nodeIds.Contains(edge.toNodeId))
                {
                    errors.Add($"Road edge {edge.edgeId} references missing to-node {edge.toNodeId}.");
                }

                if (edge.distanceMeters <= 0f)
                {
                    errors.Add($"Road edge {edge.edgeId} has a non-positive distance.");
                }

                if (edge.laneCenterOffsetsMeters != null &&
                    edge.laneCenterOffsetsMeters.Count > 0 &&
                    edge.laneCount > 0 &&
                    edge.laneCenterOffsetsMeters.Count != edge.laneCount)
                {
                    errors.Add($"Road edge {edge.edgeId} lane offset count does not match lane count.");
                }

                float previousDistance = -1f;
                foreach (LwsRoadSample sample in edge.samples)
                {
                    if (sample == null)
                    {
                        errors.Add($"Road edge {edge.edgeId} contains a null sample.");
                        continue;
                    }

                    if (sample.distanceFromStartMeters < previousDistance)
                    {
                        errors.Add($"Road edge {edge.edgeId} samples are not ordered by distance.");
                        break;
                    }

                    previousDistance = sample.distanceFromStartMeters;

                    if (sample.forward.sqrMagnitude <= 0.0001f)
                    {
                        errors.Add($"Road edge {edge.edgeId} contains a sample with no forward direction.");
                    }
                }
            }

            return new LwsRoadGraphValidationResult(errors.Count == 0, errors);
        }

        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static LwsRoadGraph FromJson(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new LwsRoadGraph() : JsonUtility.FromJson<LwsRoadGraph>(json);
        }

        public static LwsRoadGraph CreateEmpty(string graphId)
        {
            return new LwsRoadGraph { graphId = graphId ?? string.Empty };
        }
    }

    public readonly struct LwsRoadGraphValidationResult
    {
        public LwsRoadGraphValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? Array.Empty<string>();
        }

        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        public string Summary => IsValid ? "Road graph is valid." : string.Join("; ", Errors);
    }

    [Serializable]
    public sealed class LwsRouteRequest
    {
        public string requestId;
        public string originNodeId;
        public string destinationNodeId;
        public bool truckRouteRequired = true;
    }

    [Serializable]
    public sealed class LwsRouteResult
    {
        public string routeId;
        public bool succeeded;
        public string message;
        public float distanceMeters;
        public List<string> edgeIds = new List<string>();
        public List<Vector3> waypoints = new List<Vector3>();
    }
}
