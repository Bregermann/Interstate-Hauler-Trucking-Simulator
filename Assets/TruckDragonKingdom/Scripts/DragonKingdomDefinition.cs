using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckDragonKingdom
{
    [Serializable]
    public sealed class DragonRegionDefinition
    {
        public string id, label;
        public Vector3 center;
        public Bounds bounds;
    }

    // Kingdom semantics over the existing authored-road graph and planner.
    // Road geometry remains EasyRoads-owned; this is not a second road system.
    [CreateAssetMenu(menuName = "Truck Sports/Dragon Kingdom/Kingdom Definition")]
    public sealed class DragonKingdomDefinition : ScriptableObject
    {
        public LwsRoadGraph graph = new LwsRoadGraph();
        public DragonRegionDefinition[] regions = Array.Empty<DragonRegionDefinition>();
        public DragonEscapeStartDefinition[] escapeStarts = Array.Empty<DragonEscapeStartDefinition>();
        public DragonExtractionDefinition[] extractions = Array.Empty<DragonExtractionDefinition>();
        public DragonEscapeClosurePlan[] closurePlans = Array.Empty<DragonEscapeClosurePlan>();
        public float minimumRoadWidth = 14;

        public void CollectValidationIssues(List<string> issues)
        {
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            if (graph == null) issues.Add("The kingdom requires its own EasyRoads-derived road graph.");
            else if (!graph.Validate().IsValid) issues.Add(graph.Validate().Summary);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var region in regions ?? Array.Empty<DragonRegionDefinition>())
            {
                if (region == null || string.IsNullOrWhiteSpace(region.id) || !ids.Add(region.id))
                { issues.Add("Kingdom regions need unique stable IDs."); continue; }
                if (!Finite(region.center) || !Finite(region.bounds.center) || !Finite(region.bounds.size)
                    || region.bounds.size.x <= 0 || region.bounds.size.y <= 0 || region.bounds.size.z <= 0 || !region.bounds.Contains(region.center))
                    issues.Add("Invalid physical region bounds: " + region.id);
            }
            if (ids.Count < 9) issues.Add("All nine requested kingdom regions must be authored.");
            if (graph != null)
            {
                foreach (var node in graph.nodes)
                    if (node != null && !ids.Contains(node.regionId)) issues.Add("Road junction has no authored kingdom region: " + node.nodeId);
                foreach (var edge in graph.edges)
                    if (edge != null && edge.laneWidthMeters * edge.laneCount + edge.leftShoulderWidthMeters + edge.rightShoulderWidthMeters < minimumRoadWidth)
                        issues.Add("Road is narrower than the configured truck clearance: " + edge.edgeId);
            }
            var extractionIds = new HashSet<string>(StringComparer.Ordinal);
            var kinds = new HashSet<DragonExtractionKind>();
            foreach (var extraction in extractions ?? Array.Empty<DragonExtractionDefinition>())
                if (extraction == null || string.IsNullOrWhiteSpace(extraction.id) || !extractionIds.Add(extraction.id)) issues.Add("Extraction IDs must be present and unique.");
                else kinds.Add(extraction.kind);
            foreach (DragonExtractionKind kind in Enum.GetValues(typeof(DragonExtractionKind)))
                if (!kinds.Contains(kind)) issues.Add("Missing original extraction type: " + kind);
            if (escapeStarts == null || escapeStarts.Length < 2) issues.Add("Escape needs multiple real starting regions.");
        }

        public DragonRegionDefinition FindRegion(string id)
        {
            foreach (var region in regions ?? Array.Empty<DragonRegionDefinition>()) if (region != null && region.id == id) return region;
            return null;
        }
        static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
