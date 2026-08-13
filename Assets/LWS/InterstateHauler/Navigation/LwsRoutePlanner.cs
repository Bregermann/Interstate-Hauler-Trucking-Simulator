using System;
using System.Collections.Generic;
using UnityEngine;

namespace LWS.InterstateHauler
{
    public sealed class LwsRoutePlanner
    {
        private readonly LwsNavigationTuning _tuning;

        public LwsRoutePlanner(LwsNavigationTuning tuning)
        {
            _tuning = tuning ?? LwsNavigationTuning.Default();
        }

        public LwsRouteResult PlanRoute(LwsRouteRequest request, LwsRoadGraph graph)
        {
            if (request == null)
            {
                return Failure(string.Empty, "Route request is null.");
            }

            if (graph == null || graph.edges == null || graph.nodes == null)
            {
                return Failure(request.requestId, "Road graph is missing.");
            }

            LwsRoadGraphValidationResult validation = graph.Validate();
            if (!validation.IsValid)
            {
                return Failure(request.requestId, validation.Summary);
            }

            if (!TryResolveEndpointNode(request.originNodeId, request.useOriginWorldPosition, request.originWorldPosition, graph, true, out string originNodeId, out string originMessage))
            {
                return Failure(request.requestId, originMessage);
            }

            if (!TryResolveEndpointNode(request.destinationNodeId, request.useDestinationWorldPosition, request.destinationWorldPosition, graph, false, out string destinationNodeId, out string destinationMessage))
            {
                return Failure(request.requestId, destinationMessage);
            }

            if (!TryFindPath(originNodeId, destinationNodeId, graph, out List<PathSegment> path, out float distance, out string message))
            {
                return Failure(request.requestId, message);
            }

            var result = new LwsRouteResult
            {
                routeId = string.IsNullOrWhiteSpace(request.requestId) ? Guid.NewGuid().ToString("N") : request.requestId,
                succeeded = true,
                message = "Route solved by LWS graph planner.",
                distanceMeters = distance,
                originNodeId = originNodeId,
                destinationNodeId = destinationNodeId,
                destinationId = request.destinationId
            };

            BuildResultGeometry(graph, path, result);
            BuildSteps(graph, path, result);
            return result;
        }

        public LwsNavigationManeuverType ClassifyManeuver(Vector3 previousForward, Vector3 nextForward, LwsRoadClass nextRoadClass)
        {
            previousForward.y = 0f;
            nextForward.y = 0f;
            if (previousForward.sqrMagnitude <= 0.0001f || nextForward.sqrMagnitude <= 0.0001f)
            {
                return LwsNavigationManeuverType.ContinueStraight;
            }

            previousForward.Normalize();
            nextForward.Normalize();
            float signedAngle = Vector3.SignedAngle(previousForward, nextForward, Vector3.up);
            float abs = Mathf.Abs(signedAngle);
            bool right = signedAngle > 0f;

            if (nextRoadClass == LwsRoadClass.Ramp)
            {
                if (abs >= _tuning.turnAngleDegrees)
                {
                    return LwsNavigationManeuverType.MakeUTurn;
                }

                return right ? LwsNavigationManeuverType.TakeRampRight : LwsNavigationManeuverType.TakeRampLeft;
            }

            if (abs <= _tuning.straightAngleDegrees)
            {
                return LwsNavigationManeuverType.ContinueStraight;
            }

            if (abs <= _tuning.slightAngleDegrees)
            {
                return right ? LwsNavigationManeuverType.SlightRight : LwsNavigationManeuverType.SlightLeft;
            }

            if (abs >= _tuning.turnAngleDegrees)
            {
                return right ? LwsNavigationManeuverType.SharpRight : LwsNavigationManeuverType.SharpLeft;
            }

            return right ? LwsNavigationManeuverType.TurnRight : LwsNavigationManeuverType.TurnLeft;
        }

        private static LwsRouteResult Failure(string routeId, string message)
        {
            return new LwsRouteResult
            {
                routeId = routeId,
                succeeded = false,
                message = string.IsNullOrWhiteSpace(message) ? "Route planning failed." : message
            };
        }

        private bool TryResolveEndpointNode(
            string explicitNodeId,
            bool useWorldPosition,
            Vector3 worldPosition,
            LwsRoadGraph graph,
            bool origin,
            out string nodeId,
            out string message)
        {
            nodeId = string.Empty;
            message = string.Empty;

            if (!string.IsNullOrWhiteSpace(explicitNodeId) && FindNode(graph, explicitNodeId) != null)
            {
                nodeId = explicitNodeId;
                return true;
            }

            if (!useWorldPosition)
            {
                message = origin ? "Route origin node is missing." : "Route destination node is missing.";
                return false;
            }

            float bestSqr = float.PositiveInfinity;
            LwsRoadNode best = null;
            foreach (LwsRoadNode node in graph.nodes)
            {
                if (node == null)
                {
                    continue;
                }

                float sqr = (node.position - worldPosition).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = node;
                }
            }

            if (best == null)
            {
                message = origin ? "Route origin could not be resolved to a graph node." : "Route destination could not be resolved to a graph node.";
                return false;
            }

            nodeId = best.nodeId;
            return true;
        }

        private bool TryFindPath(string originNodeId, string destinationNodeId, LwsRoadGraph graph, out List<PathSegment> path, out float distance, out string message)
        {
            path = new List<PathSegment>();
            distance = 0f;
            message = string.Empty;

            var frontier = new List<string> { originNodeId };
            var costs = new Dictionary<string, float>(StringComparer.Ordinal) { [originNodeId] = 0f };
            var previous = new Dictionary<string, PathSegment>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);

            while (frontier.Count > 0)
            {
                int bestIndex = 0;
                float bestCost = costs[frontier[0]];
                for (int i = 1; i < frontier.Count; i++)
                {
                    float candidateCost = costs[frontier[i]];
                    if (candidateCost < bestCost)
                    {
                        bestCost = candidateCost;
                        bestIndex = i;
                    }
                }

                string currentNode = frontier[bestIndex];
                frontier.RemoveAt(bestIndex);
                if (!visited.Add(currentNode))
                {
                    continue;
                }

                if (string.Equals(currentNode, destinationNodeId, StringComparison.Ordinal))
                {
                    break;
                }

                foreach (PathSegment segment in EnumerateOutgoingSegments(currentNode, graph))
                {
                    if (string.IsNullOrWhiteSpace(segment.toNodeId))
                    {
                        continue;
                    }

                    float candidate = bestCost + segment.cost;
                    if (costs.TryGetValue(segment.toNodeId, out float existing) && candidate >= existing)
                    {
                        continue;
                    }

                    costs[segment.toNodeId] = candidate;
                    previous[segment.toNodeId] = segment;
                    if (!visited.Contains(segment.toNodeId) && !frontier.Contains(segment.toNodeId))
                    {
                        frontier.Add(segment.toNodeId);
                    }
                }
            }

            if (!costs.TryGetValue(destinationNodeId, out distance))
            {
                message = $"No route found from {originNodeId} to {destinationNodeId}.";
                return false;
            }

            string node = destinationNodeId;
            while (!string.Equals(node, originNodeId, StringComparison.Ordinal))
            {
                if (!previous.TryGetValue(node, out PathSegment segment))
                {
                    message = $"Route reconstruction failed at node {node}.";
                    return false;
                }

                path.Add(segment);
                node = segment.fromNodeId;
            }

            path.Reverse();
            return path.Count > 0;
        }

        private IEnumerable<PathSegment> EnumerateOutgoingSegments(string nodeId, LwsRoadGraph graph)
        {
            LwsRoadNode node = FindNode(graph, nodeId);
            if (node == null)
            {
                yield break;
            }

            for (int i = 0; i < graph.edges.Count; i++)
            {
                LwsRoadEdge edge = graph.edges[i];
                if (edge == null)
                {
                    continue;
                }

                if (string.Equals(edge.fromNodeId, nodeId, StringComparison.Ordinal))
                {
                    yield return new PathSegment(nodeId, edge.toNodeId, edge.edgeId, false, 0, Mathf.Max(edge.distanceMeters, edge.travelCost));
                }

                if (!edge.oneWay && string.Equals(edge.toNodeId, nodeId, StringComparison.Ordinal))
                {
                    yield return new PathSegment(nodeId, edge.fromNodeId, edge.edgeId, true, Mathf.Max(0, edge.samples.Count - 1), Mathf.Max(edge.distanceMeters, edge.travelCost));
                }

                if (TryFindNearestSample(edge, node.position, _tuning.routeConnectorToleranceMeters, out int sampleIndex, out float along))
                {
                    if (!string.Equals(edge.toNodeId, nodeId, StringComparison.Ordinal) &&
                        !string.Equals(edge.fromNodeId, nodeId, StringComparison.Ordinal) &&
                        edge.distanceMeters - along > 1f)
                    {
                        yield return new PathSegment(nodeId, edge.toNodeId, edge.edgeId, false, sampleIndex, Mathf.Max(1f, edge.distanceMeters - along));
                    }

                    if (!edge.oneWay &&
                        !string.Equals(edge.fromNodeId, nodeId, StringComparison.Ordinal) &&
                        along > 1f)
                    {
                        yield return new PathSegment(nodeId, edge.fromNodeId, edge.edgeId, true, sampleIndex, Mathf.Max(1f, along));
                    }
                }
            }
        }

        private static void BuildResultGeometry(LwsRoadGraph graph, IReadOnlyList<PathSegment> path, LwsRouteResult result)
        {
            Vector3 previous = Vector3.positiveInfinity;
            for (int i = 0; i < path.Count; i++)
            {
                PathSegment segment = path[i];
                LwsRoadEdge edge = FindEdge(graph, segment.edgeId);
                if (edge == null)
                {
                    continue;
                }

                result.edgeIds.Add(edge.edgeId);
                AppendEdgeSamples(edge, segment, result.waypoints, ref previous);
            }
        }

        private void BuildSteps(LwsRoadGraph graph, IReadOnlyList<PathSegment> path, LwsRouteResult result)
        {
            float distance = 0f;
            Vector3 previousForward = Vector3.zero;

            for (int i = 0; i < path.Count; i++)
            {
                LwsRoadEdge edge = FindEdge(graph, path[i].edgeId);
                if (edge == null)
                {
                    continue;
                }

                Vector3 entryForward = GetEdgeForward(edge, path[i]);
                LwsNavigationManeuverType maneuver = i == 0
                    ? LwsNavigationManeuverType.StartRoute
                    : ClassifyManeuver(previousForward, entryForward, edge.roadClass);

                string roadName = LwsRoadDisplayNames.GetRoadDisplayName(edge.roadId, edge.segmentId);
                string nextName = i < path.Count - 1
                    ? LwsRoadDisplayNames.GetRoadDisplayName(FindEdge(graph, path[i + 1].edgeId)?.roadId, FindEdge(graph, path[i + 1].edgeId)?.segmentId)
                    : roadName;

                float segmentDistance = Mathf.Max(1f, path[i].cost);
                var step = new LwsRouteStep
                {
                    stepIndex = result.steps.Count,
                    edgeId = edge.edgeId,
                    roadId = edge.roadId,
                    segmentId = edge.segmentId,
                    roadDisplayName = roadName,
                    nextRoadDisplayName = nextName,
                    maneuver = maneuver,
                    maneuverPosition = GetEntryPosition(edge, path[i]),
                    distanceFromRouteStartMeters = distance,
                    distanceMeters = segmentDistance
                };
                step.instructionText = BuildInstruction(step);
                result.steps.Add(step);
                previousForward = GetEdgeExitForward(edge, path[i]);
                distance += segmentDistance;
            }

            if (result.steps.Count > 0)
            {
                LwsRouteStep arrive = new LwsRouteStep
                {
                    stepIndex = result.steps.Count,
                    edgeId = result.steps[result.steps.Count - 1].edgeId,
                    roadId = result.steps[result.steps.Count - 1].roadId,
                    segmentId = result.steps[result.steps.Count - 1].segmentId,
                    roadDisplayName = result.steps[result.steps.Count - 1].roadDisplayName,
                    nextRoadDisplayName = "Destination",
                    maneuver = LwsNavigationManeuverType.Arrive,
                    instructionText = "Arrive at destination",
                    maneuverPosition = result.waypoints.Count > 0 ? result.waypoints[result.waypoints.Count - 1] : Vector3.zero,
                    distanceFromRouteStartMeters = result.distanceMeters,
                    distanceMeters = 0f
                };
                result.steps.Add(arrive);
            }
        }

        private static string BuildInstruction(LwsRouteStep step)
        {
            if (step.maneuver == LwsNavigationManeuverType.StartRoute)
            {
                return $"Start on {step.roadDisplayName}";
            }

            if (step.maneuver == LwsNavigationManeuverType.Arrive)
            {
                return "Arrive at destination";
            }

            return $"{LwsNavigationManeuverCatalog.GetDisplayName(step.maneuver)} onto {step.roadDisplayName}";
        }

        private static void AppendEdgeSamples(LwsRoadEdge edge, PathSegment segment, List<Vector3> waypoints, ref Vector3 previous)
        {
            if (edge.samples == null || edge.samples.Count == 0)
            {
                return;
            }

            int index = Mathf.Clamp(segment.entrySampleIndex, 0, edge.samples.Count - 1);
            if (segment.reversed)
            {
                for (int i = index; i >= 0; i--)
                {
                    AppendWaypoint(edge.samples[i].position, waypoints, ref previous);
                }
            }
            else
            {
                for (int i = index; i < edge.samples.Count; i++)
                {
                    AppendWaypoint(edge.samples[i].position, waypoints, ref previous);
                }
            }
        }

        private static void AppendWaypoint(Vector3 waypoint, List<Vector3> waypoints, ref Vector3 previous)
        {
            if (previous.x != float.PositiveInfinity && (previous - waypoint).sqrMagnitude <= 0.25f)
            {
                return;
            }

            waypoints.Add(waypoint);
            previous = waypoint;
        }

        private static Vector3 GetEntryPosition(LwsRoadEdge edge, PathSegment segment)
        {
            if (edge.samples == null || edge.samples.Count == 0)
            {
                return Vector3.zero;
            }

            int index = Mathf.Clamp(segment.entrySampleIndex, 0, edge.samples.Count - 1);
            return edge.samples[index].position;
        }

        private static Vector3 GetEdgeForward(LwsRoadEdge edge, PathSegment segment)
        {
            if (edge.samples == null || edge.samples.Count == 0)
            {
                return Vector3.forward;
            }

            int index = Mathf.Clamp(segment.entrySampleIndex, 0, edge.samples.Count - 1);
            Vector3 forward = edge.samples[index].forward;
            return segment.reversed ? -forward : forward;
        }

        private static Vector3 GetEdgeExitForward(LwsRoadEdge edge, PathSegment segment)
        {
            if (edge.samples == null || edge.samples.Count == 0)
            {
                return Vector3.forward;
            }

            int index = segment.reversed ? 0 : edge.samples.Count - 1;
            Vector3 forward = edge.samples[index].forward;
            return segment.reversed ? -forward : forward;
        }

        private static bool TryFindNearestSample(LwsRoadEdge edge, Vector3 position, float maxDistance, out int sampleIndex, out float distanceAlong)
        {
            sampleIndex = 0;
            distanceAlong = 0f;
            if (edge.samples == null || edge.samples.Count == 0)
            {
                return false;
            }

            float bestSqr = maxDistance * maxDistance;
            bool found = false;
            for (int i = 0; i < edge.samples.Count; i++)
            {
                float sqr = (edge.samples[i].position - position).sqrMagnitude;
                if (sqr >= bestSqr)
                {
                    continue;
                }

                found = true;
                bestSqr = sqr;
                sampleIndex = i;
                distanceAlong = edge.samples[i].distanceFromStartMeters;
            }

            return found;
        }

        private static LwsRoadNode FindNode(LwsRoadGraph graph, string nodeId)
        {
            if (graph == null || graph.nodes == null || string.IsNullOrWhiteSpace(nodeId))
            {
                return null;
            }

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                LwsRoadNode node = graph.nodes[i];
                if (node != null && string.Equals(node.nodeId, nodeId, StringComparison.Ordinal))
                {
                    return node;
                }
            }

            return null;
        }

        public static LwsRoadEdge FindEdge(LwsRoadGraph graph, string edgeId)
        {
            if (graph == null || graph.edges == null || string.IsNullOrWhiteSpace(edgeId))
            {
                return null;
            }

            for (int i = 0; i < graph.edges.Count; i++)
            {
                LwsRoadEdge edge = graph.edges[i];
                if (edge != null && string.Equals(edge.edgeId, edgeId, StringComparison.Ordinal))
                {
                    return edge;
                }
            }

            return null;
        }

        private readonly struct PathSegment
        {
            public PathSegment(string fromNodeId, string toNodeId, string edgeId, bool reversed, int entrySampleIndex, float cost)
            {
                this.fromNodeId = fromNodeId;
                this.toNodeId = toNodeId;
                this.edgeId = edgeId;
                this.reversed = reversed;
                this.entrySampleIndex = entrySampleIndex;
                this.cost = cost;
            }

            public readonly string fromNodeId;
            public readonly string toNodeId;
            public readonly string edgeId;
            public readonly bool reversed;
            public readonly int entrySampleIndex;
            public readonly float cost;
        }
    }
}
