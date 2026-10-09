using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static partial class TruckTaxiWorldColliderValidator
    {
        // Plain snapshots survive closing each isolated preview; no scene references escape Capture.
        private sealed class SemanticAudit
        {
            private readonly Dictionary<string, string> ids = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly List<(string id, Vector3 position)> stops = new List<(string, Vector3)>();
            private readonly List<LwsRoadGraph> graphs = new List<LwsRoadGraph>();
            private readonly List<TruckTaxiRegion> regions = new List<TruckTaxiRegion>();

            public void Capture(GameObject[] roots, TruckTaxiWorldAuditReport report)
            {
                T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Distinct().ToArray();
                void Id(string domain, string id, Component owner)
                {
                    string identity = TruckTaxiWorldColliderReview.Identity(owner.transform);
                    string key = domain + ":" + id;
                    if (string.IsNullOrWhiteSpace(id)) report.findings.Add("MISSING STABLE ID " + domain + ": " + identity);
                    else if (ids.TryGetValue(key, out string first)) report.findings.Add("DUPLICATE STABLE ID " + key + ": " + first + " <-> " + identity);
                    else ids.Add(key, identity);
                }
                foreach (var point in All<TruckTaxiRideLocation>())
                {
                    Id("location", point.locationId, point);
                    if (point.truckStopPoint == null) report.findings.Add("MISSING TRUCK ANCHOR: " + point.locationId);
                    else if (point.pickupAllowed || point.dropoffAllowed) stops.Add((point.locationId, point.StopPosition));
                    if (point.pickupAllowed && point.passengerSpawnPoint == null) report.findings.Add("MISSING PASSENGER ANCHOR: " + point.locationId);
                }
                foreach (var service in All<TruckTaxiServicePoint>()) Id("service", service.stableId, service);
                foreach (var marker in All<TruckTaxiMapMarker>()) Id("map", marker.stableId, marker);
                foreach (var point in All<TruckTaxiWorldInteractable>()) Id("interaction", point.StableId, point);
                foreach (var prop in All<TruckTaxiBreakawayProp>()) Id("prop", prop.stableId, prop);
                foreach (var provider in All<LwsRoadGraphProvider>()) if (provider.Graph != null) graphs.Add(provider.Graph);
                foreach (var world in All<TruckTaxiRegionalWorld>())
                    regions.AddRange((world.regions ?? Array.Empty<TruckTaxiRegion>()).Where(r => r != null));
                report.findings.AddRange(TruckTaxiServiceMapValidator.ValidateAuthoring(roots));
                report.findings.AddRange(ValidatePlacement(roots));
            }

            public void Complete(TruckTaxiWorldAuditReport report)
            {
                if (graphs.Count == 0) report.findings.Add("ROUTE COVERAGE UNAVAILABLE: include the authored graph provider scene.");
                foreach (var graph in graphs) report.findings.AddRange(ValidateRoutesAndSeams(graph, regions, stops));
                report.findings = report.findings.Distinct().OrderBy(f => f, StringComparer.Ordinal).ToList();
                report.limitation += " Semantic IDs aggregate across previews. Placement/entrance bounds are review candidates, not physical or NavMesh acceptance. Unloaded geometry and vendor streaming timing are not simulated.";
            }
        }

        public static List<string> ValidateRoutesAndSeams(LwsRoadGraph graph, IEnumerable<TruckTaxiRegion> regionSource,
            IEnumerable<(string id, Vector3 position)> stops)
        {
            var findings = new List<string>();
            if (graph?.nodes == null || graph.edges == null || graph.edges.Any(e => e == null || e.samples == null))
            { findings.Add("ROUTE GRAPH: missing graph/node/edge/sample collection"); return findings; }
            var validation = graph.Validate();
            if (!validation.IsValid) { findings.Add("ROUTE GRAPH: " + validation.Summary); return findings; }
            bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
            if (graph.nodes.Any(n => !Finite(n.position)) || graph.edges.Any(e =>
                !float.IsFinite(e.distanceMeters) || e.samples.Any(s => !Finite(s.position))))
            { findings.Add("ROUTE GRAPH: non-finite node/sample position or edge distance"); return findings; }
            if (graph.nodes.Count == 0) { findings.Add("ROUTE GRAPH: no nodes"); return findings; }
            var planner = new LwsRoutePlanner(LwsNavigationTuning.Default());
            string root = graph.nodes.OrderBy(n => n.nodeId, StringComparer.Ordinal).First().nodeId;
            // Both directions detect disconnected components and one-way traps using the production planner.
            foreach (var node in graph.nodes.OrderBy(n => n.nodeId, StringComparer.Ordinal))
                foreach (bool reverse in new[] { false, true })
                {
                    var route = planner.PlanRoute(new LwsRouteRequest { requestId = "authoring.audit",
                        originNodeId = reverse ? node.nodeId : root, destinationNodeId = reverse ? root : node.nodeId,
                        truckRouteRequired = true }, graph);
                    if (!route.succeeded) findings.Add("ROUTE DISCONNECTED: " + route.originNodeId + " " +
                        (reverse ? node.nodeId + " -> " + root : root + " -> " + node.nodeId) + "; " + route.message);
                }
            foreach (var stop in stops.OrderBy(s => s.id, StringComparer.Ordinal))
            {
                if (!Finite(stop.position)) { findings.Add("STOP NON-FINITE POSITION: " + stop.id); continue; }
                if (!LwsRoadGraphQuery.TryFindNearestRoad(graph, stop.position, 80, out var nearest))
                    findings.Add("STOP OFF GRAPH (>80m): " + stop.id);
                else if (Mathf.Abs(nearest.NearestPosition.y - stop.position.y) > 3)
                    findings.Add("STOP ROAD HEIGHT CANDIDATE (>3m): " + stop.id);
            }
            var regions = regionSource.Where(r => r != null).ToArray();
            foreach (var region in regions)
                if (!Finite(region.bounds.center) || !Finite(region.bounds.size) ||
                    region.bounds.size.x <= 0 || region.bounds.size.y <= 0 || region.bounds.size.z <= 0)
                    findings.Add("STREAM SEAM: invalid bounds " + region.id);
            foreach (var group in regions.GroupBy(r => r.id))
                if (string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1) findings.Add("STREAM SEAM: missing/duplicate region ID " + group.Key);
            var nodes = graph.nodes.ToDictionary(n => n.nodeId);
            foreach (var edge in graph.edges)
            {
                if (edge.laneWidthMeters > 0 && edge.laneWidthMeters < 3)
                    findings.Add("TRUCK WIDTH REVIEW (<3m lane): " + edge.edgeId);
                foreach (var restriction in edge.restrictions ?? new List<LwsRoadRestriction>())
                    if (restriction != null && (!restriction.truckAllowed ||
                        restriction.maxVehicleHeightMeters > 0 && restriction.maxVehicleHeightMeters < 4.2f))
                        findings.Add("TRUCK RESTRICTION REVIEW (not enforced by connectivity planner): " + edge.edgeId);
                if (edge.samples.Count < 2) { findings.Add("ROAD GEOMETRY: insufficient samples " + edge.edgeId); continue; }
                if (Vector3.Distance(edge.samples[0].position, nodes[edge.fromNodeId].position) > .25f ||
                    Vector3.Distance(edge.samples[edge.samples.Count - 1].position, nodes[edge.toNodeId].position) > .25f)
                    findings.Add("ROAD ENDPOINT SEAM (>0.25m): " + edge.edgeId);
                if (regions.Length == 0) continue;
                // Check every authored sample plus intervals; do not infer coverage from endpoints alone.
                for (int i = 1; i < edge.samples.Count; i++)
                {
                    Vector3 a = edge.samples[i - 1].position, b = edge.samples[i].position;
                    int count = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(a, b) / 10), 1, 10000);
                    for (int k = 0; k <= count; k++)
                    {
                        var point = Vector3.Lerp(a, b, k / (float)count);
                        if (regions.Any(r => r.bounds.Contains(point))) continue;
                        findings.Add("STREAM SEAM UNCOVERED ROAD SAMPLE: " + edge.edgeId + " at " + point.ToString("F2"));
                        break;
                    }
                }
            }
            if (regions.Length == 0) findings.Add("STREAM SEAM COVERAGE UNAVAILABLE: no region bounds in audit scope");
            return findings.Distinct().OrderBy(f => f, StringComparer.Ordinal).ToList();
        }

        public static List<string> ValidatePlacement(IEnumerable<GameObject> scope)
        {
            var roots = scope.Where(r => r != null).Distinct().ToArray();
            var solids = roots.SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                .Where(TruckTaxiWorldColliderReview.IsSolid).Distinct().ToArray();
            var findings = new List<string>();
            foreach (var prop in roots.SelectMany(r => r.GetComponentsInChildren<TruckTaxiBreakawayProp>(true)).Distinct())
            {
                string id = TruckTaxiWorldColliderReview.Identity(prop.transform);
                if (!Enum.IsDefined(typeof(TruckTaxiPropPhysicsCategory), prop.category) ||
                    prop.partRole == TruckTaxiPropPartRole.LoadBearing && prop.category != TruckTaxiPropPhysicsCategory.Structural)
                    findings.Add("INVALID BREAKAWAY CLASSIFICATION: " + id);
                var colliders = prop.GetComponentsInChildren<Collider>(true);
                if (!prop.PreservesAuthoredPhysics && (colliders.Length == 0 || colliders.Any(c => c is MeshCollider mesh && !mesh.convex)))
                    findings.Add("DESTRUCTIBLE COLLISION: missing collider or nonconvex moving mesh; " + id);
                var renderers = prop.GetComponentsInChildren<Renderer>(true).Where(TruckTaxiWorldColliderReview.IsVisible).ToArray();
                if (renderers.Length == 0) { findings.Add("PROP ART MISSING/DISABLED: " + id); continue; }
                var bounds = TruckTaxiWorldGeometryAuthoring.WorldMeshBounds(renderers[0]);
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(TruckTaxiWorldGeometryAuthoring.WorldMeshBounds(renderer));
                // Collider.Raycast confines preview auditing to supplied geometry; no global Physics query.
                float top = bounds.max.y + 20, ground = float.NegativeInfinity;
                var ray = new Ray(new Vector3(bounds.center.x, top, bounds.center.z), Vector3.down);
                foreach (var solid in solids)
                    if (!solid.transform.IsChildOf(prop.transform) && solid.attachedRigidbody == null &&
                        solid.Raycast(ray, out var hit, bounds.size.y + 100) && hit.normal.y > .5f)
                        ground = Mathf.Max(ground, hit.point.y);
                if (float.IsNegativeInfinity(ground)) findings.Add("PROP SUPPORT UNVERIFIED: " + id);
                else if (bounds.min.y - ground > .3f) findings.Add("FLOATING PROP CANDIDATE: " + id);
                else if (ground - bounds.min.y > .5f) findings.Add("BURIED PROP CANDIDATE: " + id);
            }
            foreach (var interaction in roots.SelectMany(r => r.GetComponentsInChildren<TruckTaxiWorldInteractable>(true)).Distinct())
            {
                if (interaction.interactionAnchor == null)
                { findings.Add("ENTRANCE APPROACH UNVERIFIED (no authored interaction anchor): " + interaction.StableId); continue; }
                var anchor = interaction.interactionAnchor;
                var approach = TruckTaxiWorldColliderReview.TransformBounds(new Bounds(Vector3.up, new Vector3(.6f, 1.8f, .6f)),
                    Matrix4x4.TRS(anchor.position, anchor.rotation, Vector3.one));
                foreach (var solid in solids)
                    if (approach.Intersects(solid.bounds)) findings.Add("ENTRANCE CLEARANCE CANDIDATE: " + interaction.StableId +
                        " <-> " + TruckTaxiWorldColliderReview.Identity(solid.transform));
            }
            return findings.Distinct().OrderBy(f => f, StringComparer.Ordinal).ToList();
        }
    }
}
