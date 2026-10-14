using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static partial class TruckTaxiEdenboroughAuthoring
    {
        private const float PopulationRoadElevation = .2f;
        private const float PopulationRoadSpacing = 8;
        private const float PopulationTurnTrim = 6;

        private static KeyValuePair<string, string[]>[] PopulationCircuits()
        {
            var circuits = new List<KeyValuePair<string, string[]>>();
            foreach (var district in TruckTaxiEdenboroughLayout.Districts)
            {
                string key = district.Id.Substring("taxi.edenborough.".Length);
                string[] nodes;
                if (key == "central")
                    nodes = new[] { "city.-2.-2", "city.-2.-1", "city.-2.0", "city.-2.1", "city.-2.2",
                        "city.-1.2", "city.0.2", "city.1.2", "city.2.2", "city.2.1", "city.2.0",
                        "city.2.-1", "city.2.-2", "city.1.-2", "city.0.-2", "city.-1.-2" };
                else if (key == "hillend")
                    nodes = new[] { "hillend", "loanhead", "straiton", "hermiston" };
                else
                    nodes = new[] { key + ".street.-1.-1", key + ".street.-1.0", key + ".street.-1.1",
                        key + ".street.0.1", key + ".street.1.1", key + ".street.1.0",
                        key + ".street.1.-1", key + ".street.0.-1" };
                circuits.Add(new KeyValuePair<string, string[]>(key, nodes));
            }
            circuits.Add(new KeyValuePair<string, string[]>("bypass", new[] { "bypass.west", "hermiston", "straiton",
                "gilmerton", "sheriffhall", "musselburgh", "east", "city.2.0", "city.1.0", "city.0.0",
                "city.-1.0", "city.-2.0", "west", "zoo", "gogar" }));
            circuits.Add(new KeyValuePair<string, string[]>("waterfront", new[] { "west", "northwest", "leith",
                "northeast", "east", "city.2.0", "city.2.1", "city.2.2", "city.1.2", "city.0.2",
                "city.-1.2", "city.-2.2", "city.-2.1", "city.-2.0" }));
            circuits.Add(new KeyValuePair<string, string[]>("southern", new[] { "straiton", "loanhead", "bonnyrigg",
                "dalkeith", "sheriffhall", "gilmerton" }));
            return circuits.ToArray();
        }

        private static LwsTrafficLaneDefinition[] CreateTrafficLoops()
        {
            var lanes = new List<LwsTrafficLaneDefinition>();
            foreach (var circuit in PopulationCircuits())
            {
                lanes.Add(PopulationLane(circuit.Key, circuit.Value, false));
                lanes.Add(PopulationLane(circuit.Key, circuit.Value.Reverse().ToArray(), true));
            }
            return lanes.ToArray();
        }

        private static TruckTaxiEdenboroughLayout.Road PopulationRoad(string from, string to)
        {
            foreach (var road in TruckTaxiEdenboroughLayout.Roads)
                if (road.From == from && road.To == to || road.From == to && road.To == from)
                {
                    if (road.Points == null || road.Points.Length < 2) break;
                    return road;
                }
            throw new InvalidOperationException("Edenborough circuit has no real connecting road: " + from + " -> " + to);
        }
        private static LwsTrafficLaneDefinition PopulationLane(string key, string[] nodes, bool reverse)
        {
            var roads = new TruckTaxiEdenboroughLayout.Road[nodes.Length];
            var polylines = new Vector3[nodes.Length][];
            float width = float.PositiveInfinity, speed = float.PositiveInfinity;
            bool highway = false;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!TruckTaxiEdenboroughLayout.Nodes.ContainsKey(nodes[i])) throw new InvalidOperationException("Missing circuit node: " + nodes[i]);
                var road = PopulationRoad(nodes[i], nodes[(i + 1) % nodes.Length]); roads[i] = road;
                width = Mathf.Min(width, road.LaneWidth); speed = Mathf.Min(speed, road.SpeedMph);
                highway |= road.Kind == "Highway";
                var source = road.From == nodes[i] ? road.Points : road.Points.Reverse().ToArray();
                var offset = new Vector3[source.Length];
                for (int p = 0; p < source.Length; p++)
                {
                    Vector3 before = PopulationFlatDirection(source[p] - source[Mathf.Max(0, p - 1)]);
                    Vector3 after = PopulationFlatDirection(source[Mathf.Min(source.Length - 1, p + 1)] - source[p]);
                    if (p == 0) before = after;
                    if (p == source.Length - 1) after = before;
                    var rightBefore = Vector3.Cross(Vector3.up, before);
                    var rightAfter = Vector3.Cross(Vector3.up, after);
                    var miter = (rightBefore + rightAfter).normalized;
                    float denominator = Vector3.Dot(miter, rightBefore);
                    if (denominator < .3f) throw new InvalidOperationException("Invalid traffic bend: " + road.Id);
                    offset[p] = source[p] + miter * (road.LaneWidth * .5f / denominator) + Vector3.up * PopulationRoadElevation;
                }
                polylines[i] = PopulationTrimAndSample(offset);
            }
            var points = new List<Vector3>();
            for (int i = 0; i < polylines.Length; i++)
            {
                var line = polylines[i]; var next = polylines[(i + 1) % polylines.Length];
                foreach (var point in line) PopulationAppend(points, point);
                Vector3 enter = line[line.Length - 1], exit = next[0];
                Vector3 incoming = PopulationFlatDirection(line[line.Length - 1] - line[line.Length - 2]);
                Vector3 outgoing = PopulationFlatDirection(next[1] - next[0]);
                float cross = PopulationCross(incoming, outgoing);
                Vector3 control;
                if (Mathf.Abs(cross) < .001f)
                {
                    if (Vector3.Dot(incoming, outgoing) < 0) throw new InvalidOperationException("Circuit contains a U-turn: " + key);
                    control = (enter + exit) * .5f;
                }
                else control = enter + incoming * (PopulationCross(exit - enter, outgoing) / cross);
                control.y = TruckTaxiEdenboroughLayout.Nodes[nodes[(i + 1) % nodes.Length]].y + PopulationRoadElevation;
                int steps = Mathf.Max(3, Mathf.CeilToInt((Vector3.Distance(enter, control) + Vector3.Distance(control, exit)) / 2));
                for (int s = 1; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    PopulationAppend(points, (1 - t) * (1 - t) * enter + 2 * (1 - t) * t * control + t * t * exit);
                }
            }
            if ((points[points.Count - 1] - points[0]).sqrMagnitude < .001f) points.RemoveAt(points.Count - 1);
            // Put the UTS/logical loop seam on a straight road, not at a junction. Both adapters
            // can wrap their endpoint guards without skipping across a corner or an entire edge.
            Vector3 seamTarget = polylines[0][polylines[0].Length / 2];
            int seam = 0; float nearest = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 a = PopulationFlatDirection(points[i] - points[(i + points.Count - 1) % points.Count]);
                Vector3 b = PopulationFlatDirection(points[(i + 1) % points.Count] - points[i]);
                float distance = (points[i] - seamTarget).sqrMagnitude;
                if (Vector3.Dot(a, b) > .999f && distance < nearest) { nearest = distance; seam = i; }
            }
            if (float.IsPositiveInfinity(nearest)) throw new InvalidOperationException("No safe traffic seam: " + key);
            var centerline = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++) centerline[i] = points[(seam + i) % points.Count];
            float length = 0;
            for (int i = 0; i < centerline.Length; i++) length += Vector3.Distance(centerline[i], centerline[(i + 1) % centerline.Length]);
            Vector3 heading = centerline[2] - centerline[1];
            var lane = new LwsTrafficLaneDefinition { laneId = "taxi.edenborough.traffic." + key + (reverse ? ".reverse" : ".forward"),
                roadId = "taxi.edenborough.circuit." + key, segmentId = "taxi.edenborough.circuit." + key,
                edgeId = "taxi.edenborough.circuit." + key + (reverse ? ".reverse" : ".forward"), laneIndex = 0,
                roadClass = highway ? LwsRoadClass.StateHighway : LwsRoadClass.LocalRoad,
                direction = Mathf.Abs(heading.x) > Mathf.Abs(heading.z) ? heading.x > 0 ? LwsRoadDirection.Eastbound : LwsRoadDirection.Westbound :
                    heading.z > 0 ? LwsRoadDirection.Northbound : LwsRoadDirection.Southbound,
                laneWidthMeters = width, laneCenterOffsetMeters = width * .5f, speedLimitMph = speed,
                lengthMeters = length, centerline = centerline, spawnEnabled = true };
            if (!lane.Validate(out string reason)) throw new InvalidOperationException(reason);
            return lane;
        }
        private static Vector3[] PopulationTrimAndSample(Vector3[] line)
        {
            var distances = new float[line.Length];
            for (int i = 1; i < line.Length; i++) distances[i] = distances[i - 1] + Vector3.Distance(line[i - 1], line[i]);
            float length = distances[distances.Length - 1];
            float trim = Mathf.Min(PopulationTurnTrim, length * .15f);
            int count = Mathf.Max(2, Mathf.CeilToInt((length - 2 * trim) / PopulationRoadSpacing));
            var points = new Vector3[count + 1]; int segment = 1;
            for (int i = 0; i <= count; i++)
            {
                float distance = Mathf.Lerp(trim, length - trim, i / (float)count);
                while (segment < line.Length - 1 && distances[segment] < distance) segment++;
                float t = Mathf.InverseLerp(distances[segment - 1], distances[segment], distance);
                points[i] = Vector3.Lerp(line[segment - 1], line[segment], t);
            }
            return points;
        }
        private static void PopulationAppend(List<Vector3> points, Vector3 point)
        {
            if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z)) throw new InvalidOperationException("Non-finite population path point.");
            if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > .0001f) points.Add(point);
        }
        private static Vector3 PopulationFlatDirection(Vector3 direction) { direction.y = 0; return direction.normalized; }
        private static float PopulationCross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;

        private sealed class PopulationWalkPlan
        {
            public string Id;
            public Vector3 Center;
            public Vector2 Size;
            public Vector3[] Points;
        }
        private static PopulationWalkPlan[] PopulationWalkPlans(LwsTrafficLaneDefinition[] lanes)
        {
            if (lanes == null || lanes.Length == 0) throw new ArgumentException("Authored traffic lanes are required before pedestrians.");
            var result = new List<PopulationWalkPlan>();
            foreach (var district in TruckTaxiEdenboroughLayout.Districts)
            {
                float block = district.Id.EndsWith(".central", StringComparison.Ordinal) ? 130 : 90;
                PopulationWalkPlan chosen = null;
                foreach (var shift in new[] { new Vector2(-block,-block), new Vector2(-block,block), new Vector2(block,-block), new Vector2(block,block),
                    new Vector2(-block * 2, -block * 2), new Vector2(block * 2, block * 2) })
                {
                    Vector3 center = TruckTaxiEdenboroughLayout.At(district.Center.x + shift.x, district.Center.z + shift.y);
                    var size = new Vector2(64, 52);
                    if (!PopulationWalkRectangleClear(center, size)) continue;
                    var corners = new[] { center + new Vector3(-24,0,-18), center + new Vector3(-24,0,18),
                        center + new Vector3(24,0,18), center + new Vector3(24,0,-18) };
                    var points = new List<Vector3>();
                    for (int c = 0; c < corners.Length; c++)
                    {
                        Vector3 a = corners[c], b = corners[(c + 1) % corners.Length];
                        int count = Mathf.CeilToInt(Vector3.Distance(a, b) / 4);
                        for (int i = 0; i < count; i++)
                        {
                            Vector3 point = Vector3.Lerp(a, b, i / (float)count);
                            point.y = TruckTaxiEdenboroughLayout.Height(point.x, point.z) + .1f; points.Add(point);
                        }
                    }
                    chosen = new PopulationWalkPlan { Id = district.Id + ".walk-gather", Center = center, Size = size, Points = points.ToArray() };
                    break;
                }
                if (chosen == null) throw new InvalidOperationException("No road-clear walk/gather footprint in " + district.Id);
                result.Add(chosen);
            }
            return result.ToArray();
        }
        private static bool PopulationWalkRectangleClear(Vector3 center, Vector2 size)
        {
            var world = TruckTaxiEdenboroughLayout.WorldBounds;
            if (center.x - size.x * .5f < world.min.x || center.x + size.x * .5f > world.max.x ||
                center.z - size.y * .5f < world.min.z || center.z + size.y * .5f > world.max.z) return false;
            foreach (var road in TruckTaxiEdenboroughLayout.Roads)
            {
                // Include the actual carriageway/shoulders. Highway gathering is kept an additional 24m away.
                float margin = road.Width * .5f + (road.Kind == "Highway" ? 24 : 6.5f);
                var min = new Vector2(center.x - size.x * .5f - margin, center.z - size.y * .5f - margin);
                var max = new Vector2(center.x + size.x * .5f + margin, center.z + size.y * .5f + margin);
                for (int i = 1; i < road.Points.Length; i++)
                    if (PopulationSegmentInRectangle(road.Points[i - 1], road.Points[i], min, max)) return false;
            }
            return true;
        }
        private static bool PopulationSegmentInRectangle(Vector3 a, Vector3 b, Vector2 min, Vector2 max)
        {
            float low = 0, high = 1;
            return PopulationClip(a.x, b.x - a.x, min.x, max.x, ref low, ref high) &&
                PopulationClip(a.z, b.z - a.z, min.y, max.y, ref low, ref high);
        }
        private static bool PopulationClip(float origin, float direction, float min, float max, ref float low, ref float high)
        {
            if (Mathf.Abs(direction) < .0001f) return origin >= min && origin <= max;
            float a = (min - origin) / direction, b = (max - origin) / direction;
            low = Mathf.Max(low, Mathf.Min(a,b)); high = Mathf.Min(high, Mathf.Max(a,b)); return low <= high;
        }

        private static TruckTaxiPedestrianPopulation CreatePedestrians(TruckTaxiPedestrianPopulation template,
            LwsTrafficLaneDefinition[] lanes, TruckTaxiPopulationProfile profile)
        {
            if (template == null || profile == null) throw new ArgumentException("Existing pedestrian template and population profile are required.");
            var binding = new PopulationPeopleBinding();
            var prefabs = new List<GameObject>();
            var sourcePaths = new List<Component>(template.peoplePaths ?? Array.Empty<Component>());
            foreach (var area in template.pedestrianAreas ?? Array.Empty<TruckTaxiPedestrianArea>())
                if (area != null) sourcePaths.AddRange(area.walkingPaths ?? Array.Empty<Component>());
            foreach (var path in sourcePaths)
                if (path != null && binding.Type.IsInstanceOfType(path))
                    foreach (var prefab in (GameObject[])binding.Prefabs.GetValue(path) ?? Array.Empty<GameObject>())
                        if (prefab != null && EditorUtility.IsPersistent(prefab) && PrefabUtility.IsPartOfPrefabAsset(prefab) && !prefabs.Contains(prefab)) prefabs.Add(prefab);
            if (prefabs.Count == 0) throw new InvalidOperationException("Source population has no actual persistent UTS/Citizens pedestrian prefabs.");
            var plans = PopulationWalkPlans(lanes);
            var root = New("Edenborough UTS Pedestrians", null, Vector3.zero); root.SetActive(false);
            try
            {
                var population = root.AddComponent<TruckTaxiPedestrianPopulation>(); EditorUtility.CopySerialized(template, population);
                population.roadLanes = lanes; population.densityProfile = profile; population.useDensityOverride = true;
                population.maxFullPedestrians = 48; population.maxVisiblePedestrians = 96;
                population.signalIntersections = Array.Empty<TruckTaxiIntersection>();
                var areas = new List<TruckTaxiPedestrianArea>(); var paths = new List<Component>();
                foreach (var plan in plans)
                {
                    var area = New(plan.Id, root.transform, plan.Center).AddComponent<TruckTaxiPedestrianArea>();
                    area.Configure(plan.Id, plan.Size, 6);
                    var go = New(plan.Id + ".uts-walk", area.transform, plan.Center); go.SetActive(false);
                    var path = go.AddComponent(binding.Type);
                    binding.Prefabs.SetValue(path, prefabs.ToArray());
                    binding.Field("numberOfWays").SetValue(path, 1); binding.Field("loopPath").SetValue(path, true);
                    binding.Field("lineSpacing").SetValue(path, 1f); binding.Field("Density").SetValue(path, .05f);
                    binding.Field("_minimalObjectLength").SetValue(path, 3f); binding.Field("disableLineDraw").SetValue(path, true);
                    binding.Field("randXPos").SetValue(path, .1f); binding.Field("randZPos").SetValue(path, .1f);
                    Set(path, "_ignorePeople", false); Set(path, "_ignoreCar", false); Set(path, "_ignoreBicycle", false);
                    var serialized = new SerializedObject(path); var direction = serialized.FindProperty("direction");
                    if (direction == null) throw new MissingFieldException(binding.Type.Name, "direction");
                    direction.enumValueIndex = Array.IndexOf(direction.enumNames, "Forward"); serialized.ApplyModifiedPropertiesWithoutUndo();
                    var positions = (List<Vector3>)binding.Field("pathPoint").GetValue(path);
                    var transforms = (List<GameObject>)binding.Field("pathPointTransform").GetValue(path);
                    positions.Clear(); transforms.Clear();
                    for (int p = 0; p < plan.Points.Length; p++)
                    {
                        var point = New("Walk point " + p, go.transform, plan.Points[p]); positions.Add(point.transform.position); transforms.Add(point);
                    }
                    binding.Draw.Invoke(path, new[] { (object)false, Enum.Parse(binding.Draw.GetParameters()[1].ParameterType, "Forward") });
                    var native = binding.Field("points").GetValue(path) as Vector3[,];
                    if (native == null || native.GetLength(1) < 5) throw new InvalidOperationException("UTS generated no walking points.");
                    for (int p = 1; p < native.GetLength(1) - 1; p++)
                        if (!area.Allows(native[0,p], lanes)) throw new InvalidOperationException("Unsafe UTS walking point in " + plan.Id);
                    area.walkingPaths = new[] { path }; paths.Add(path); areas.Add(area); go.SetActive(true);
                }
                population.peoplePaths = paths.ToArray(); population.pedestrianAreas = areas.ToArray();
                population.enabled = true; root.SetActive(true); Undo.RegisterCreatedObjectUndo(root, "Create Edenborough UTS population");
                return population;
            }
            catch { Object.DestroyImmediate(root); throw; }
        }
        private sealed class PopulationPeopleBinding
        {
            public readonly Type Type;
            public readonly FieldInfo Prefabs;
            public readonly MethodInfo Draw;
            private readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
            public PopulationPeopleBinding()
            {
                Type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PeopleWalkPath", false)).FirstOrDefault(t => t != null);
                if (Type == null || !typeof(Component).IsAssignableFrom(Type)) throw new TypeLoadException("Installed UTS PeopleWalkPath is missing.");
                foreach (string name in new[] { "walkingPrefabs", "numberOfWays", "loopPath", "lineSpacing", "Density", "_minimalObjectLength",
                    "disableLineDraw", "randXPos", "randZPos", "pathPoint", "pathPointTransform", "points" })
                    fields.Add(name, Type.GetField(name) ?? throw new MissingFieldException(Type.Name, name));
                Prefabs = Field("walkingPrefabs");
                Draw = Type.GetMethods().FirstOrDefault(m => m.Name == "DrawCurved" && m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(bool) && m.GetParameters()[1].ParameterType.IsEnum);
                if (Draw == null) throw new MissingMethodException(Type.Name, "DrawCurved");
            }
            public FieldInfo Field(string name) => fields[name];
        }
    }
}
