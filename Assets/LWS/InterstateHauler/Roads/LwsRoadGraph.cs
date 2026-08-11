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
        public float distanceFromStartMeters;
        public Vector3 position;
        public Vector3 forward;
        public float speedLimitMph;
    }

    [Serializable]
    public sealed class LwsRoadEdge
    {
        public string edgeId;
        public string fromNodeId;
        public string toNodeId;
        public LwsRoadClass roadClass;
        public bool oneWay;
        public float distanceMeters;
        public float travelCost;
        public float speedLimitMph;
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

                if (!nodeIds.Contains(edge.fromNodeId))
                {
                    errors.Add($"Road edge {edge.edgeId} references missing from-node {edge.fromNodeId}.");
                }

                if (!nodeIds.Contains(edge.toNodeId))
                {
                    errors.Add($"Road edge {edge.edgeId} references missing to-node {edge.toNodeId}.");
                }

                if (edge.distanceMeters < 0f)
                {
                    errors.Add($"Road edge {edge.edgeId} has a negative distance.");
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
