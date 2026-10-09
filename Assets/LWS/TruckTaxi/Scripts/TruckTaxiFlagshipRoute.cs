using System;
using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Reads the regional authoring output; never manufactures a replacement road or access leg.
    public sealed class TruckTaxiFlagshipRoute
    {
        public const string DefaultLaneId = "taxi.regional.lane.0";
        public const int MaximumRivals = 20;
        public readonly Vector3[] Points, Gates, GateForwards;
        public readonly int[] GatePointIndices;
        public readonly float LengthMeters, LaneWidthMeters;
        public readonly string SourceLaneId;
        private readonly float[] distances;

        private TruckTaxiFlagshipRoute(List<Vector3> points, float width, string source)
        {
            Points = points.ToArray(); LaneWidthMeters = width; SourceLaneId = source;
            distances = new float[Points.Length];
            for (int i = 1; i < Points.Length; i++) distances[i] = distances[i - 1] + Vector3.Distance(Points[i - 1], Points[i]);
            LengthMeters = distances[distances.Length - 1] + Vector3.Distance(Points[Points.Length - 1], Points[0]);
            var indices = new List<int> { 0 };
            float last = 0;
            for (int i = 1; i < Points.Length; i++)
                if (distances[i] - last >= 100 && LengthMeters - distances[i] >= 60)
                { indices.Add(i); last = distances[i]; }
            GatePointIndices = indices.ToArray(); Gates = new Vector3[indices.Count]; GateForwards = new Vector3[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                int p = indices[i]; Gates[i] = Points[p];
                GateForwards[i] = Vector3.ProjectOnPlane(Points[(p + 1) % Points.Length] - Points[(p + Points.Length - 1) % Points.Length], Vector3.up).normalized;
            }
        }

        public int GridIndex(int slot)
        {
            if (slot < 0 || slot > MaximumRivals) throw new ArgumentOutOfRangeException(nameof(slot));
            float wanted = LengthMeters - 24 * (slot + 1);
            for (int i = distances.Length - 2; i >= 1; i--) if (distances[i] <= wanted) return i;
            throw new InvalidOperationException("Authored route has no room for this grid slot.");
        }

        public int RecoveryIndex(int nextCheckpoint) =>
            (GatePointIndices[Mathf.Clamp(nextCheckpoint, 0, Gates.Length - 1)] + Points.Length - 2) % Points.Length;

        public Vector3 Facing(int index) => Vector3.ProjectOnPlane(
            Points[(index + 1) % Points.Length] - Points[index], Vector3.up).normalized;

        public static bool TryCreate(LwsRoadGraph graph, IReadOnlyList<LwsTrafficLaneDefinition> lanes,
            out TruckTaxiFlagshipRoute route, out string reason, string laneId = DefaultLaneId)
        {
            route = null; reason = null;
            if (graph?.nodes == null || graph.edges == null || graph.edges.Count == 0 || graph.edges.Count > 10000)
            { reason = "Flagship race requires the bounded regional road graph."; return false; }
            foreach (var edge in graph.edges)
            {
                if (edge == null || edge.samples == null || edge.samples.Count > 8192)
                { reason = "Regional graph contains invalid or oversized edge geometry."; return false; }
                foreach (var sample in edge.samples)
                    if (sample == null || !Finite(sample.position) || !Finite(sample.forward))
                    { reason = "Regional graph contains nonfinite or missing samples."; return false; }
            }
            var validation = graph.Validate();
            if (!validation.IsValid) { reason = "Invalid regional road graph: " + validation.Summary; return false; }
            LwsTrafficLaneDefinition source = null;
            if (lanes != null)
                foreach (var lane in lanes)
                    if (lane != null && lane.laneId == laneId)
                    {
                        if (source != null) { reason = "Duplicate flagship source lane: " + laneId; return false; }
                        source = lane;
                    }
            if (source?.centerline == null || source.centerline.Length < 8 || source.centerline.Length > 4096 ||
                !float.IsFinite(source.laneWidthMeters) || source.laneWidthMeters < 3)
            { reason = "Authored regional UTS lane unavailable or too narrow: " + laneId; return false; }
            var points = new List<Vector3>();
            for (int i = 0; i < source.centerline.Length; i++)
            {
                Vector3 a = source.centerline[i], b = source.centerline[(i + 1) % source.centerline.Length];
                if (!Finite(a) || !Finite(b)) { reason = "Nonfinite authored race geometry."; return false; }
                float length = Vector3.Distance(a, b), horizontal = Vector3.ProjectOnPlane(b - a, Vector3.up).magnitude;
                if (length < .01f) continue;
                if (horizontal < .01f || Mathf.Abs(b.y - a.y) / horizontal > .08f)
                { reason = "Flagship route exceeds the flat-road grade limit (8%)."; return false; }
                int steps = Mathf.CeilToInt(length / 10);
                if (steps > 1024 || points.Count + steps > 8192)
                { reason = "Authored race route exceeds the bounded sample budget."; return false; }
                for (int n = 0; n < steps; n++) points.Add(Vector3.Lerp(a, b, n / (float)steps));
            }
            if (points.Count < 60) { reason = "Authored race route is too short for the grid."; return false; }
            float outbound = 0, city = 0, inbound = 0;
            // A service spur may be closer than the main carriageway; it must not split the highway leg.
            var highway = new LwsRoadGraph { edges = graph.edges.FindAll(edge => edge.roadClass == LwsRoadClass.Interstate) };
            int phase = 0; Vector3 outboundHeading = Vector3.zero;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 a = points[i], b = points[(i + 1) % points.Count];
                Vector3 heading = Vector3.ProjectOnPlane(b - a, Vector3.up).normalized;
                bool found = LwsRoadGraphQuery.TryFindNearestRoad(highway, a, 9, out var road) &&
                    Mathf.Abs(Vector3.Dot(heading, road.Forward)) > .9f;
                if (!found) found = LwsRoadGraphQuery.TryFindNearestRoad(graph, a, 9, out road);
                if (!found ||
                    Mathf.Abs(road.NearestPosition.y - a.y) > 1 ||
                    road.SurfaceType == LwsRoadSurfaceType.Unknown ||
                    (road.RoadClass != LwsRoadClass.Interstate && road.RoadClass != LwsRoadClass.LocalRoad))
                { reason = "Authored race lane leaves the supported highway/city road graph at sample " + i + "."; return false; }
                float length = Vector3.Distance(a, b);
                if (road.RoadClass == LwsRoadClass.Interstate)
                {
                    if (outboundHeading == Vector3.zero) outboundHeading = heading;
                    if (phase == 1 && Vector3.Dot(outboundHeading, heading) < -.7f) phase = 2;
                    if (phase == 0) outbound += length;
                    else if (phase == 2) inbound += length;
                    else { reason = "City loop does not return along the highway."; return false; }
                }
                else if (outbound > 1000 && phase < 2) { phase = 1; city += length; }
            }
            if (outbound < 2000 || inbound < 2000 || city < 250)
            { reason = "Route lacks the full highway / city loop / return-highway sequence."; return false; }
            route = new TruckTaxiFlagshipRoute(points, source.laneWidthMeters, source.laneId);
            return true;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
