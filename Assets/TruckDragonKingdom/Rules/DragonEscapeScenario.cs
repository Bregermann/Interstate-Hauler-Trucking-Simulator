using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    public enum DragonExtractionKind { Fortress, Mountain, Boat }

    [Serializable]
    public sealed class DragonExtractionDefinition
    {
        public string id, nodeId;
        public DragonExtractionKind kind;
        public Vector3 position;
        // World-space parking/shelter volume. Actual support, orientation, dwell,
        // boat attachment and departure are verified by the physical host.
        public Bounds parkingBounds;
        public float secureSeconds = 2;
        public float maximumRelativeSpeed = .7f;
        public float minimumBoatDepartureDistance = 12;
    }

    [Serializable]
    public sealed class DragonEscapeStartDefinition
    {
        public string id, regionId, nodeId;
        // Optional distinct participant starts within the selected region.
        // Empty uses nodeId as the shared starting road node.
        public string[] participantNodeIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DragonEscapeClosurePlan
    {
        public string id;
        public string[] edgeIds = Array.Empty<string>();
    }

    public sealed class DragonExtractionSnapshot
    {
        public string Id { get; }
        public string NodeId { get; }
        public DragonExtractionKind Kind { get; }
        public Vector3 Position { get; }
        public Bounds ParkingBounds { get; }
        public float SecureSeconds { get; }
        public float MaximumRelativeSpeed { get; }
        public float MinimumBoatDepartureDistance { get; }
        internal DragonExtractionSnapshot(DragonExtractionDefinition source)
        {
            Id = source.id; NodeId = source.nodeId; Kind = source.kind;
            Position = source.position; ParkingBounds = source.parkingBounds;
            SecureSeconds = source.secureSeconds; MaximumRelativeSpeed = source.maximumRelativeSpeed;
            MinimumBoatDepartureDistance = source.minimumBoatDepartureDistance;
        }
    }

    public sealed class DragonEscapeRouteSnapshot
    {
        public string OriginNodeId { get; }
        public string DestinationNodeId { get; }
        public string DestinationId { get; }
        public float DistanceMeters { get; }
        public IReadOnlyList<string> EdgeIds { get; }
        public IReadOnlyList<Vector3> Waypoints { get; }
        // Never expose the planner's mutable result to UI or AI readers.
        readonly string routeJson;
        internal DragonEscapeRouteSnapshot(LwsRouteResult route)
        {
            OriginNodeId = route.originNodeId; DestinationNodeId = route.destinationNodeId;
            DestinationId = route.destinationId; DistanceMeters = route.distanceMeters;
            EdgeIds = Array.AsReadOnly(route.edgeIds.ToArray());
            Waypoints = Array.AsReadOnly(route.waypoints.ToArray());
            routeJson = JsonUtility.ToJson(route);
        }
        // Compass/LWS presenters accept a mutable vendor result. Return a fresh
        // detached copy so a presenter cannot mutate the authoritative snapshot.
        public LwsRouteResult CreatePresentationRoute() => JsonUtility.FromJson<LwsRouteResult>(routeJson);
    }

    public sealed class DragonEscapeScenarioSnapshot
    {
        public int Seed { get; }
        public string StartId { get; }
        public string StartRegionId { get; }
        public string StartNodeId { get; }
        public string ClosurePlanId { get; }
        public string ConfigurationId { get; }
        public DragonExtractionSnapshot Extraction { get; }
        public IReadOnlyList<string> ParticipantStartNodeIds { get; }
        public IReadOnlyList<string> PlannedClosures { get; }
        public IReadOnlyList<DragonEscapeRouteSnapshot> InitialRoutes { get; }
        // Guaranteed routes after ALL planned closures, not only the first event.
        public IReadOnlyList<DragonEscapeRouteSnapshot> Routes { get; }
        internal string GraphJson { get; }
        internal DragonEscapeScenarioSnapshot(int seed, DragonEscapeStartDefinition start,
            DragonExtractionDefinition extraction, DragonEscapeClosurePlan plan, string graphJson,
            string[] starts, IReadOnlyList<DragonEscapeRouteSnapshot> initial,
            IReadOnlyList<DragonEscapeRouteSnapshot> final)
        {
            Seed = seed; StartId = start.id; StartRegionId = start.regionId; StartNodeId = start.nodeId;
            ClosurePlanId = plan.id; Extraction = new DragonExtractionSnapshot(extraction);
            ParticipantStartNodeIds = Array.AsReadOnly((string[])starts.Clone());
            PlannedClosures = Array.AsReadOnly(plan.edgeIds.OrderBy(id => id, StringComparer.Ordinal).ToArray());
            InitialRoutes = initial; Routes = final; GraphJson = graphJson;
            // Length-prefixed components cannot collide when authored IDs contain separators.
            ConfigurationId = Identity(start.regionId) + Identity(start.nodeId) + Identity(extraction.id) +
                Identity(string.Concat(starts.Select(Identity))) +
                Identity(string.Concat(PlannedClosures.Select(Identity))) +
                string.Concat(final.Select(route => Identity(string.Concat(route.EdgeIds.Select(Identity)))));
        }
        static string Identity(string value) => value.Length + ":" + value;
    }

    // Escape scenario semantics over the original LWS road planner. There is no
    // second pathfinder, world mutation, movement, timer-based extraction or award.
    public static class DragonEscapeScenario
    {
        public static bool TryCreate(LwsRoadGraph graph, int seed,
            IReadOnlyList<DragonEscapeStartDefinition> starts,
            IReadOnlyList<DragonExtractionDefinition> extractions,
            IReadOnlyList<DragonEscapeClosurePlan> closurePlans,
            DragonEscapeScenarioSnapshot previous, out DragonEscapeScenarioSnapshot scenario, out string reason)
        {
            scenario = null;
            if (!ValidateGraph(graph, out reason)) return false;
            if (!ValidateDefinitions(graph, starts, extractions, closurePlans, out reason)) return false;
            string graphJson = graph.ToJson();
            var candidates = new List<DragonEscapeScenarioSnapshot>();
            foreach (var start in starts.OrderBy(value => value.id, StringComparer.Ordinal))
            foreach (var extraction in extractions.OrderBy(value => value.id, StringComparer.Ordinal))
            foreach (var plan in closurePlans.OrderBy(value => value.id, StringComparer.Ordinal))
            {
                string[] origins = start.participantNodeIds.Length == 0 ? new[] { start.nodeId } : start.participantNodeIds;
                if (!TryRoutes(graphJson, origins, extraction.id, extraction.nodeId, Array.Empty<string>(), out var initial, out _)) continue;
                if (!TryRoutes(graphJson, origins, extraction.id, extraction.nodeId, plan.edgeIds, out var final, out _)) continue;
                candidates.Add(new DragonEscapeScenarioSnapshot(seed, start, extraction, plan, graphJson, origins, initial, final));
            }
            if (candidates.Count == 0)
            {
                reason = "No scenario preserves a physical road route from every participant start after all planned closures.";
                return false;
            }
            if (previous != null)
            {
                var differentDestination = candidates.Where(value => value.Extraction.Id != previous.Extraction.Id).ToList();
                if (differentDestination.Count > 0) candidates = differentDestination;
                else
                {
                    var differentConfiguration = candidates.Where(value => value.ConfigurationId != previous.ConfigurationId).ToList();
                    if (differentConfiguration.Count > 0) candidates = differentConfiguration;
                }
            }
            scenario = candidates[new System.Random(seed).Next(candidates.Count)];
            reason = string.Empty;
            return true;
        }

        // The caller commits a live closure only when this succeeds for all
        // surviving participants. Failure leaves both the scenario and GPS intact.
        public static bool TryReplan(DragonEscapeScenarioSnapshot scenario,
            IReadOnlyList<string> currentParticipantNodeIds, IReadOnlyList<string> blockedEdgeIds,
            out IReadOnlyList<DragonEscapeRouteSnapshot> routes, out string reason)
        {
            routes = null;
            if (scenario == null) { reason = "A valid escape scenario is required."; return false; }
            return TryRoutes(scenario.GraphJson, currentParticipantNodeIds, scenario.Extraction.Id,
                scenario.Extraction.NodeId, blockedEdgeIds, out routes, out reason);
        }

        static bool TryRoutes(string graphJson, IReadOnlyList<string> origins, string destinationId,
            string destinationNode, IReadOnlyList<string> closures,
            out IReadOnlyList<DragonEscapeRouteSnapshot> routes, out string reason)
        {
            routes = null;
            if (origins == null || origins.Count == 0 || closures == null)
            { reason = "Participant road nodes and an explicit closure list are required."; return false; }
            var graph = LwsRoadGraph.FromJson(graphJson);
            var nodes = new HashSet<string>(graph.nodes.Select(value => value.nodeId), StringComparer.Ordinal);
            var edges = new HashSet<string>(graph.edges.Select(value => value.edgeId), StringComparer.Ordinal);
            var blocked = new HashSet<string>(StringComparer.Ordinal);
            foreach (string closure in closures)
                if (string.IsNullOrWhiteSpace(closure) || !edges.Contains(closure) || !blocked.Add(closure))
                { reason = "A closure contains a missing or duplicate edge: " + closure; return false; }
            graph.edges.RemoveAll(edge => blocked.Contains(edge.edgeId) ||
                edge.restrictions.Any(restriction => !restriction.truckAllowed));
            // Use authored graph endpoints rather than planner's broad 35m road
            // connector tolerance, which could join adjacent fantasy roads.
            var tuning = LwsNavigationTuning.Default(); tuning.routeConnectorToleranceMeters = .01f;
            var planner = new LwsRoutePlanner(tuning);
            var result = new List<DragonEscapeRouteSnapshot>();
            foreach (string origin in origins)
            {
                if (string.IsNullOrWhiteSpace(origin) || !nodes.Contains(origin) || origin == destinationNode)
                { reason = "Escape starts must be valid road nodes outside the extraction: " + origin; return false; }
                var route = planner.PlanRoute(new LwsRouteRequest {
                    requestId = "dragon-escape:" + destinationId + ":" + result.Count,
                    originNodeId = origin, destinationNodeId = destinationNode,
                    destinationId = destinationId, truckRouteRequired = true
                }, graph);
                if (!route.succeeded || !RouteFollowsGraph(route, graph, origin, destinationNode))
                { reason = "No valid road route from " + origin + " to " + destinationId + " after closures."; return false; }
                result.Add(new DragonEscapeRouteSnapshot(route));
            }
            routes = new ReadOnlyCollection<DragonEscapeRouteSnapshot>(result);
            reason = string.Empty;
            return true;
        }

        // Validate a planner result, not another search. Every edge must continue
        // from the prior node with its authored direction and real sample geometry.
        static bool RouteFollowsGraph(LwsRouteResult route, LwsRoadGraph graph, string origin, string destination)
        {
            if (route.edgeIds == null || route.edgeIds.Count == 0 || route.waypoints == null ||
                route.waypoints.Count < 2 || !float.IsFinite(route.distanceMeters) || route.distanceMeters <= 0) return false;
            string node = origin;
            foreach (string edgeId in route.edgeIds)
            {
                var edge = graph.edges.Find(value => value.edgeId == edgeId);
                if (edge == null) return false;
                if (edge.fromNodeId == node) node = edge.toNodeId;
                else if (!edge.oneWay && edge.toNodeId == node) node = edge.fromNodeId;
                else return false;
            }
            return node == destination && route.waypoints.All(Finite);
        }

        static bool ValidateGraph(LwsRoadGraph graph, out string reason)
        {
            reason = "A valid, finite authored road graph with physical samples is required.";
            if (graph == null || graph.nodes == null || graph.edges == null || graph.nodes.Count < 2 || graph.edges.Count == 0) return false;
            if (graph.nodes.Any(node => node == null || !Finite(node.position)) ||
                graph.edges.Any(edge => edge == null || edge.samples == null || edge.samples.Count < 2 || edge.restrictions == null ||
                    edge.restrictions.Any(restriction => restriction == null || !float.IsFinite(restriction.maxVehicleHeightMeters) || !float.IsFinite(restriction.maxVehicleWeightKg) ||
                        restriction.maxVehicleHeightMeters < 0 || restriction.maxVehicleWeightKg < 0) ||
                    !float.IsFinite(edge.distanceMeters) || !float.IsFinite(edge.travelCost) || edge.travelCost < 0 ||
                    edge.samples.Any(sample => sample == null || !Finite(sample.position) || !Finite(sample.forward) ||
                        !float.IsFinite(sample.distanceFromStartMeters) || sample.distanceFromStartMeters < 0))) return false;
            var validation = graph.Validate();
            if (!validation.IsValid) { reason = validation.Summary; return false; }
            foreach (var edge in graph.edges)
            {
                var from = graph.nodes.Find(node => node.nodeId == edge.fromNodeId);
                var to = graph.nodes.Find(node => node.nodeId == edge.toNodeId);
                if (Vector3.Distance(edge.samples[0].position, from.position) > .1f ||
                    Vector3.Distance(edge.samples[edge.samples.Count - 1].position, to.position) > .1f)
                { reason = "Road samples must physically reach both authored nodes: " + edge.edgeId; return false; }
            }
            reason = string.Empty; return true;
        }

        static bool ValidateDefinitions(LwsRoadGraph graph, IReadOnlyList<DragonEscapeStartDefinition> starts,
            IReadOnlyList<DragonExtractionDefinition> extractions, IReadOnlyList<DragonEscapeClosurePlan> plans, out string reason)
        {
            reason = "Unique valid starts, extractions and explicit closure plans are required.";
            if (starts == null || starts.Count == 0 || extractions == null || extractions.Count == 0 || plans == null || plans.Count == 0) return false;
            var nodes = new HashSet<string>(graph.nodes.Select(node => node.nodeId), StringComparer.Ordinal);
            var edges = new HashSet<string>(graph.edges.Select(edge => edge.edgeId), StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var start in starts)
            {
                if (start == null || string.IsNullOrWhiteSpace(start.id) || !ids.Add(start.id) || string.IsNullOrWhiteSpace(start.regionId) ||
                    string.IsNullOrWhiteSpace(start.nodeId) || !nodes.Contains(start.nodeId) || start.participantNodeIds == null ||
                    start.participantNodeIds.Any(node => string.IsNullOrWhiteSpace(node) || !nodes.Contains(node))) return false;
                if (graph.nodes.Find(node => node.nodeId == start.nodeId).regionId != start.regionId ||
                    start.participantNodeIds.Any(id => graph.nodes.Find(node => node.nodeId == id).regionId != start.regionId))
                { reason = "All competitors must start in the selected shared region: " + start.regionId; return false; }
            }
            ids.Clear();
            foreach (var extraction in extractions)
            {
                if (extraction == null || string.IsNullOrWhiteSpace(extraction.id) || !ids.Add(extraction.id) ||
                    string.IsNullOrWhiteSpace(extraction.nodeId) || !nodes.Contains(extraction.nodeId) ||
                    !Enum.IsDefined(typeof(DragonExtractionKind), extraction.kind) || !Finite(extraction.position) ||
                    !Finite(extraction.parkingBounds.center) || !Finite(extraction.parkingBounds.size) ||
                    extraction.parkingBounds.size.x <= 0 || extraction.parkingBounds.size.y <= 0 || extraction.parkingBounds.size.z <= 0 ||
                    !float.IsFinite(extraction.secureSeconds) || extraction.secureSeconds <= 0 ||
                    !float.IsFinite(extraction.maximumRelativeSpeed) || extraction.maximumRelativeSpeed <= 0 ||
                    !float.IsFinite(extraction.minimumBoatDepartureDistance) ||
                    extraction.kind == DragonExtractionKind.Boat && extraction.minimumBoatDepartureDistance <= 0) return false;
            }
            ids.Clear();
            foreach (var plan in plans)
            {
                if (plan == null || string.IsNullOrWhiteSpace(plan.id) || !ids.Add(plan.id) || plan.edgeIds == null ||
                    plan.edgeIds.Any(edge => string.IsNullOrWhiteSpace(edge) || !edges.Contains(edge)) ||
                    plan.edgeIds.Distinct(StringComparer.Ordinal).Count() != plan.edgeIds.Length) return false;
            }
            reason = string.Empty; return true;
        }
        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
