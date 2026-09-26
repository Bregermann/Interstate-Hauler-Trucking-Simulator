using System.Collections.Generic;
using LWS.InterstateHauler;
using UnityEngine;

namespace LWS.TruckTaxi
{
    // Bounded startup sampling. UTS still owns each actor's locomotion; this only supplies reachable points.
    public sealed class TruckTaxiPedestrianWalkGraph
    {
        private const int MaximumCells = 1024;
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<int>[] links;
        private readonly int[] component;
        private readonly int[] spawnOrder;
        private readonly LwsTrafficLaneDefinition[] lanes;
        private readonly List<RoadSegment> roadSegments = new List<RoadSegment>();
        private readonly List<Bounds> buildingBounds = new List<Bounds>();
        private readonly Collider[] obstacleHits = new Collider[32];
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private readonly int landNodeCount;
        private readonly bool hasRoadSurfaces;
        private readonly float setback;
        private int spawnCursor;
        public int Count => points.Count;
        public int WalkableSpawnCount => spawnOrder.Length;
        public int RoadSegmentCount => roadSegments.Count;
        public bool HasRoadBounds => roadSegments.Count > 0 || hasRoadSurfaces;
        public Vector3 this[int index] => points[index];

        public TruckTaxiPedestrianWalkGraph(LwsTrafficLaneDefinition[] roadLanes,
            TruckTaxiPedestrianArea[] areas, Component[] paths, TruckTaxiIntersection[] crossings,
            LwsRoadGraph roadGraph, TruckTaxiWorldAnchor[] anchors, TruckTaxiSurface[] surfaces,
            float maximumRadius, Vector3 origin)
        {
            lanes = roadLanes ?? System.Array.Empty<LwsTrafficLaneDefinition>();
            setback = 2f;
            foreach (var lane in lanes)
            {
                if (lane == null || lane.centerline == null) continue;
                float radius = Mathf.Max(1.5f, lane.laneWidthMeters * .5f) + setback;
                for (int i = 1; i < lane.centerline.Length; i++)
                    roadSegments.Add(new RoadSegment(lane.centerline[i - 1], lane.centerline[i], radius));
            }
            foreach (var edge in roadGraph?.edges ?? new List<LwsRoadEdge>())
            {
                if (edge?.samples == null) continue;
                for (int i = 1; i < edge.samples.Count; i++)
                {
                    var a = edge.samples[i - 1]; var b = edge.samples[i];
                    if (a == null || b == null) continue;
                    float roadWidth = Mathf.Max(a.roadWidthMeters, b.roadWidthMeters);
                    if (roadWidth <= 0) roadWidth = edge.laneCount * edge.laneWidthMeters + edge.medianWidthMeters +
                        edge.leftShoulderWidthMeters + edge.rightShoulderWidthMeters;
                    roadSegments.Add(new RoadSegment(a.position, b.position, Mathf.Max(2, roadWidth * .5f + 1.5f)));
                }
            }
            foreach (var anchor in anchors ?? System.Array.Empty<TruckTaxiWorldAnchor>())
            {
                if (anchor == null || anchor.AnchorType != TruckTaxiWorldAnchorType.Building) continue;
                bool found = false; Bounds footprint = default;
                foreach (var collider in anchor.GetComponentsInChildren<Collider>())
                {
                    if (collider == null || collider.isTrigger) continue;
                    if (!found) { footprint = collider.bounds; found = true; } else footprint.Encapsulate(collider.bounds);
                }
                if (!found)
                    foreach (var renderer in anchor.GetComponentsInChildren<Renderer>())
                    {
                        if (!found) { footprint = renderer.bounds; found = true; } else footprint.Encapsulate(renderer.bounds);
                    }
                if (!found) footprint = new Bounds(anchor.transform.position, new Vector3(8, 2, 8));
                footprint.Expand(new Vector3(2, 0, 2));
                buildingBounds.Add(footprint);
            }
            foreach (var surface in surfaces ?? System.Array.Empty<TruckTaxiSurface>())
                if (surface != null && surface.isRoad && surface.GetComponentInChildren<Collider>() != null)
                { hasRoadSurfaces = true; break; }
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var lane in lanes)
                if (lane != null && lane.centerline != null)
                    foreach (var p in lane.centerline) Include(p, ref min, ref max);
            foreach (var edge in roadGraph?.edges ?? new List<LwsRoadEdge>())
                if (edge?.samples != null)
                    foreach (var sample in edge.samples) if (sample != null) Include(sample.position, ref min, ref max);
            foreach (var surface in surfaces ?? System.Array.Empty<TruckTaxiSurface>())
            {
                if (surface == null || !surface.isRoad) continue;
                foreach (var collider in surface.GetComponentsInChildren<Collider>())
                {
                    Include(collider.bounds.min, ref min, ref max);
                    Include(collider.bounds.max, ref min, ref max);
                }
            }
            foreach (var area in areas ?? System.Array.Empty<TruckTaxiPedestrianArea>())
            {
                if (area == null) continue;
                var half = area.localSize * .5f;
                Include(area.transform.position + new Vector3(-half.x, 0, -half.y), ref min, ref max);
                Include(area.transform.position + new Vector3(half.x, 0, half.y), ref min, ref max);
            }
            foreach (var path in paths ?? System.Array.Empty<Component>())
                if (path != null) Include(path.transform.position, ref min, ref max);
            if (float.IsInfinity(min.x)) { min = new Vector2(origin.x - 80, origin.z - 80); max = new Vector2(origin.x + 80, origin.z + 80); }
            min -= Vector2.one * 35; max += Vector2.one * 35;
            if (maximumRadius > 0)
            {
                min = Vector2.Max(min, new Vector2(origin.x - maximumRadius, origin.z - maximumRadius));
                max = Vector2.Min(max, new Vector2(origin.x + maximumRadius, origin.z + maximumRadius));
            }
            float spacing = Mathf.Max(10, Mathf.Sqrt(Mathf.Max(1, (max.x - min.x) * (max.y - min.y)) / MaximumCells));
            int width = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / spacing));
            int height = Mathf.Max(1, Mathf.CeilToInt((max.y - min.y) / spacing));
            while (width * height > MaximumCells) { spacing *= 1.05f; width = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / spacing)); height = Mathf.Max(1, Mathf.CeilToInt((max.y - min.y) / spacing)); }
            var cells = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int z = 0; z < height; z++)
                {
                    cells[x, z] = -1;
                    var sample = new Vector3(min.x + (x + .5f) * spacing, origin.y, min.y + (z + .5f) * spacing);
                    if (!TryGround(sample, origin.y, false, out var grounded)) continue;
                    cells[x, z] = points.Count; points.Add(grounded);
                }
            int gridNodeCount = points.Count;
            landNodeCount = gridNodeCount;
            var edges = new List<List<int>>(points.Count);
            for (int i = 0; i < points.Count; i++) edges.Add(new List<int>(8));
            for (int x = 0; x < width; x++)
                for (int z = 0; z < height; z++)
                {
                    int a = cells[x, z]; if (a < 0) continue;
                    for (int dx = 0; dx <= 1; dx++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz <= 0) continue;
                            int nx = x + dx, nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= height || cells[nx, nz] < 0) continue;
                            int b = cells[nx, nz];
                            if (ClearSegment(points[a], points[b], false)) Connect(edges, a, b);
                        }
                }
            foreach (var crossing in crossings ?? System.Array.Empty<TruckTaxiIntersection>())
            {
                if (crossing == null || !crossing.IsConfigured) continue;
                var field = crossing.crosswalkPath.GetType().GetField("points");
                if (!(field?.GetValue(crossing.crosswalkPath) is Vector3[,] route) || route.GetLength(1) < 4) continue;
                int previous = -1, first = -1, second = -1, beforeLast = -1;
                for (int i = 1; i < route.GetLength(1) - 1; i++)
                {
                    if (!TryGround(route[0, i], origin.y, true, out var grounded)) continue;
                    int node = points.Count; points.Add(grounded); edges.Add(new List<int>(4));
                    if (first < 0) first = node;
                    else if (second < 0) second = node;
                    if (previous >= 0 && ClearSegment(points[previous], grounded, true)) Connect(edges, previous, node);
                    beforeLast = previous;
                    previous = node;
                }
                if (second >= 0) LinkEndpoint(edges, first, points[first] - points[second], gridNodeCount, spacing);
                if (beforeLast >= 0) LinkEndpoint(edges, previous, points[previous] - points[beforeLast], gridNodeCount, spacing);
            }
            links = new List<int>[edges.Count];
            for (int i = 0; i < edges.Count; i++) links[i] = edges[i];
            component = new int[points.Count];
            for (int i = 0; i < component.Length; i++) component[i] = -1;
            int id = 0;
            var queue = new Queue<int>();
            for (int i = 0; i < points.Count; i++)
            {
                if (component[i] >= 0) continue;
                component[i] = id; queue.Enqueue(i);
                while (queue.Count > 0)
                    foreach (int next in links[queue.Dequeue()])
                        if (component[next] < 0) { component[next] = id; queue.Enqueue(next); }
                id++;
            }
            var eligible = new List<int>(gridNodeCount);
            for (int i = 0; i < gridNodeCount; i++) if (links[i].Count > 0) eligible.Add(i);
            spawnOrder = eligible.ToArray();
            for (int i = spawnOrder.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int swap = spawnOrder[i]; spawnOrder[i] = spawnOrder[j]; spawnOrder[j] = swap;
            }
        }

        private static void Include(Vector3 p, ref Vector2 min, ref Vector2 max)
        {
            var point = new Vector2(p.x, p.z); min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        private bool TryGround(Vector3 sample, float originY, bool crossing, out Vector3 ground)
        {
            ground = default;
            if (BuildingAt(sample) || (!crossing && RoadAt(sample))) return false;
            int count = Physics.RaycastNonAlloc(sample + Vector3.up * 35, Vector3.down,
                groundHits, 70, ~0, QueryTriggerInteraction.Ignore);
            if (count == groundHits.Length) return false;
            float best = float.PositiveInfinity;
            Collider support = null;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.collider.attachedRigidbody != null || hit.normal.y < .7f) continue;
                float height = Mathf.Abs(hit.point.y - originY);
                if (height > 12 || height >= best) continue;
                best = height; ground = hit.point; support = hit.collider;
            }
            if (support == null) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.collider.attachedRigidbody != null ||
                    Mathf.Abs(hit.point.y - ground.y) > 1f) continue;
                var surface = hit.collider.GetComponentInParent<TruckTaxiSurface>();
                if (!crossing && surface != null && surface.isRoad) return false;
                var anchor = hit.collider.GetComponentInParent<TruckTaxiWorldAnchor>();
                if (anchor != null && (anchor.AnchorType == TruckTaxiWorldAnchorType.Building ||
                    (!crossing && anchor.AnchorType == TruckTaxiWorldAnchorType.Road))) return false;
            }
            return !HasStaticObstacle(ground);
        }
        private bool ClearSegment(Vector3 a, Vector3 b, bool crosswalk)
        {
            if (Mathf.Abs(a.y - b.y) > 2.5f) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 3f));
            for (int i = 1; i < steps; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, (float)i / steps);
                if (!TryGround(p, p.y, crosswalk, out var support) || Mathf.Abs(support.y - p.y) > .65f) return false;
            }
            return true;
        }
        private bool HasStaticObstacle(Vector3 ground)
        {
            int count = Physics.OverlapCapsuleNonAlloc(ground + Vector3.up * .65f,
                ground + Vector3.up * 1.55f, .3f, obstacleHits, ~0, QueryTriggerInteraction.Ignore);
            if (count == obstacleHits.Length) return true;
            for (int i = 0; i < count; i++)
                if (obstacleHits[i] != null && obstacleHits[i].attachedRigidbody == null) return true;
            return false;
        }
        private bool BuildingAt(Vector3 point)
        {
            foreach (var footprint in buildingBounds)
                if (point.x >= footprint.min.x && point.x <= footprint.max.x &&
                    point.z >= footprint.min.z && point.z <= footprint.max.z) return true;
            return false;
        }
        private bool RoadAt(Vector3 p)
        {
            var point = new Vector2(p.x, p.z);
            foreach (var segment in roadSegments)
            {
                if (point.x < segment.Min.x || point.x > segment.Max.x || point.y < segment.Min.y || point.y > segment.Max.y) continue;
                float t = segment.Direction.sqrMagnitude > .001f ?
                    Mathf.Clamp01(Vector2.Dot(point - segment.Start, segment.Direction) / segment.Direction.sqrMagnitude) : 0;
                if ((point - segment.Start - t * segment.Direction).sqrMagnitude < segment.Radius * segment.Radius) return true;
            }
            return false;
        }
        private readonly struct RoadSegment
        {
            public readonly Vector2 Start, Direction, Min, Max;
            public readonly float Radius;
            public RoadSegment(Vector3 start, Vector3 end, float radius)
            {
                Start = new Vector2(start.x, start.z);
                var b = new Vector2(end.x, end.z);
                Direction = b - Start;
                Radius = radius;
                Min = Vector2.Min(Start, b) - Vector2.one * radius;
                Max = Vector2.Max(Start, b) + Vector2.one * radius;
            }
        }
        private static void Connect(List<List<int>> edges, int a, int b)
        {
            if (a == b || edges[a].Contains(b)) return;
            edges[a].Add(b); edges[b].Add(a);
        }
        private void LinkEndpoint(List<List<int>> edges, int endpoint, Vector3 outward, int gridNodeCount, float spacing)
        {
            int best = -1; float distance = spacing * spacing * 2.25f;
            outward.y = 0;
            outward.Normalize();
            for (int i = 0; i < gridNodeCount; i++)
            {
                if (RoadAt(points[i])) continue;
                Vector3 offset = points[i] - points[endpoint]; offset.y = 0;
                float along = Vector3.Dot(offset, outward);
                if (along < 0 || (offset - outward * along).sqrMagnitude > spacing * spacing * .36f) continue;
                float d = (points[i] - points[endpoint]).sqrMagnitude;
                if (d >= distance || !ClearSegment(points[i], points[endpoint], true)) continue;
                best = i; distance = d;
            }
            if (best >= 0) Connect(edges, endpoint, best);
        }
        public int NextSpawnNode()
        {
            if (spawnOrder.Length == 0) return -1;
            int result = spawnOrder[spawnCursor++ % spawnOrder.Length];
            return result;
        }
        public int PickDestination(int start, float maximumDistance)
        {
            if (start < 0 || start >= points.Count || landNodeCount == 0) return -1;
            for (int i = 0; i < 32; i++)
            {
                int candidate = Random.Range(0, landNodeCount);
                float distance = (points[start] - points[candidate]).sqrMagnitude;
                if (links[candidate].Count > 0 && component[candidate] == component[start] &&
                    distance >= 15 * 15 && distance <= maximumDistance * maximumDistance)
                    return candidate;
            }
            foreach (int neighbor in links[start]) if (neighbor < landNodeCount) return neighbor;
            return start;
        }
        public bool Route(int start, int goal, List<int> result)
        {
            result.Clear();
            if (start < 0 || goal < 0 || start >= points.Count || goal >= points.Count || component[start] != component[goal]) return false;
            var previous = new int[points.Count];
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            var queue = new Queue<int>(); queue.Enqueue(start); previous[start] = start;
            while (queue.Count > 0 && previous[goal] < 0)
            {
                int node = queue.Dequeue();
                foreach (int next in links[node])
                    if (previous[next] < 0) { previous[next] = node; queue.Enqueue(next); }
            }
            if (previous[goal] < 0) return false;
            for (int node = goal; node != start; node = previous[node]) result.Add(node);
            result.Reverse();
            return true;
        }
    }
}
