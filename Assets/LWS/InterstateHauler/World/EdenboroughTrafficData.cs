using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public sealed class EdenboroughOsmTag
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class EdenboroughWayBinding
    {
        public string sourceRoadId;
        public long osmWayId;
        public string displayName;
        public EdenboroughOsmTag[] tags = Array.Empty<EdenboroughOsmTag>();
        public string[] easyRoadsObjectNames = Array.Empty<string>();
        public string[] laneIds = Array.Empty<string>();

        public string Tag(string key) => tags.FirstOrDefault(t => t.key == key)?.value ?? string.Empty;
    }

    [Serializable]
    public sealed class EdenboroughTurnBinding
    {
        public string edgeId;
        public string fromLaneId;
        public string toLaneId;
        public long viaOsmNode;
        public Vector3[] centerline = Array.Empty<Vector3>();
    }

    // Authored data for existing LWS navigation and LwsUtsTrafficApi consumers. No actor lifecycle.
    public sealed class EdenboroughTrafficData : ScriptableObject
    {
        public string regionId;
        public string graphId;
        public string sourceSha256;
        public bool leftHandTraffic = true;
        public List<EdenboroughWayBinding> ways = new List<EdenboroughWayBinding>();
        public List<LwsTrafficLaneDefinition> lanes = new List<LwsTrafficLaneDefinition>();
        public List<EdenboroughTurnBinding> turns = new List<EdenboroughTurnBinding>();
        public List<string> excludedSourceRoads = new List<string>();

        public bool TryComposeRoute(IReadOnlyList<string> laneIds, bool closeLoop,
            out LwsTrafficLaneDefinition route, out string message)
        {
            route = null;
            message = string.Empty;
            if (laneIds == null || laneIds.Count == 0)
            {
                message = "An ordered LWS-selected lane route is required.";
                return false;
            }
            var byId = lanes.ToDictionary(l => l.laneId, StringComparer.Ordinal);
            var routeLanes = new List<LwsTrafficLaneDefinition>();
            foreach (string id in laneIds)
            {
                if (string.IsNullOrEmpty(id) || !byId.TryGetValue(id, out LwsTrafficLaneDefinition lane) ||
                    !lane.Validate(out message))
                {
                    message = "Missing or invalid authored lane: " + id + ". " + message;
                    return false;
                }
                routeLanes.Add(lane);
            }
            var points = new List<Vector3>();
            for (int i = 0; i < routeLanes.Count; i++)
            {
                Append(points, routeLanes[i].centerline);
                if (i == routeLanes.Count - 1 && !closeLoop) break;
                string next = routeLanes[(i + 1) % routeLanes.Count].laneId;
                EdenboroughTurnBinding turn = turns.FirstOrDefault(t =>
                    t.fromLaneId == routeLanes[i].laneId && t.toLaneId == next);
                if (turn == null)
                {
                    message = "No legal authored transition: " + routeLanes[i].laneId + " -> " + next;
                    return false;
                }
                Vector3[] connector = turn.centerline;
                Vector3[] nextLane = routeLanes[(i + 1) % routeLanes.Count].centerline;
                if (connector == null || connector.Length < 2 ||
                    Vector3.Distance(points[points.Count - 1], connector[0]) > .1f ||
                    Vector3.Distance(connector[connector.Length - 1], nextLane[0]) > .1f)
                {
                    message = "Disconnected authored transition: " + turn.edgeId;
                    return false;
                }
                Append(points, turn.centerline);
            }
            float length = 0f;
            for (int i = 1; i < points.Count; i++) length += Vector3.Distance(points[i - 1], points[i]);
            route = new LwsTrafficLaneDefinition
            {
                laneId = graphId + ":composed-route", roadId = graphId, segmentId = graphId + ":route",
                edgeId = routeLanes[0].edgeId, laneIndex = 0,
                roadClass = routeLanes[0].roadClass, direction = routeLanes[0].direction,
                speedLimitMph = routeLanes.Min(l => l.speedLimitMph),
                laneWidthMeters = routeLanes.Min(l => l.laneWidthMeters),
                lengthMeters = length, spawnEnabled = true, centerline = points.ToArray()
            };
            return route.Validate(out message);
        }

        private static void Append(List<Vector3> target, IEnumerable<Vector3> points)
        {
            foreach (Vector3 point in points)
                if (target.Count == 0 || (target[target.Count - 1] - point).sqrMagnitude > .000001f)
                    target.Add(point);
        }
    }
}
