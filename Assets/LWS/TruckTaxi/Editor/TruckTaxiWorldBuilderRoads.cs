using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static class TruckTaxiWorldBuilderRoads
    {
        private const float EndpointMergeMeters = 5f;
        private const string VendorNamePrefix = "TT World Road ";
        private static readonly string[] Kinds = { "CityStreet", "Avenue", "Highway", "Alley", "ServiceRoad", "DirtRoad", "ParkingConnector" };

        private readonly struct RoadSpec
        {
            public RoadSpec(float width, int lanes, float laneWidth, float speed, LwsRoadClass roadClass, LwsRoadSurfaceType surface)
            { Width = width; Lanes = lanes; LaneWidth = laneWidth; Speed = speed; RoadClass = roadClass; Surface = surface; }
            public float Width { get; }
            public int Lanes { get; }
            public float LaneWidth { get; }
            public float Speed { get; }
            public LwsRoadClass RoadClass { get; }
            public LwsRoadSurfaceType Surface { get; }
        }

        public static bool IsSupportedRoadType(string value) => Kinds.Any(kind => string.Equals(kind, value, StringComparison.OrdinalIgnoreCase));

        public static string BuildSelectedRoad(bool update)
        {
            if (Selection.activeTransform == null) throw new InvalidOperationException("Select a TT_ROAD anchor or one of its points.");
            Transform root = Selection.activeTransform;
            while (root != null && !root.name.StartsWith("TT_ROAD_", StringComparison.OrdinalIgnoreCase)) root = root.parent;
            if (root == null) throw new InvalidOperationException("Selection has no TT_ROAD ancestor.");
            return BuildRoad(root, update);
        }

        public static string BuildNewRoads()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build roads only in Edit Mode.");
            var anchors = TruckTaxiWorldBuilder.LoadedAnchors().Where(t =>
                TruckTaxiWorldBuilder.TryParseAnchorName(t.name, out var type, out _, out _) && type == TruckTaxiWorldAnchorType.Road).ToList();
            var duplicates = TruckTaxiWorldBuilder.ValidateAnchorIds(TruckTaxiWorldBuilder.LoadedAnchors());
            if (duplicates.Count > 0) throw new InvalidOperationException(string.Join("\n", duplicates));
            int built = 0;
            foreach (var anchor in anchors)
            {
                var metadata = anchor.GetComponent<TruckTaxiWorldAnchor>();
                if (metadata != null && FindVendorRoad(VendorRoadName(metadata.StableId), anchor.gameObject.scene) != null) continue;
                BuildRoad(anchor, false);
                built++;
            }
            return $"Built {built} new EasyRoads roads. Existing roads and city graph entries were retained.";
        }

        public static string BuildRoad(Transform anchor, bool update)
        {
            if (anchor == null || EditorApplication.isPlayingOrWillChangePlaymode || !anchor.gameObject.scene.IsValid())
                throw new InvalidOperationException("Build roads only from scene anchors in Edit Mode.");
            if (!TruckTaxiWorldBuilder.TryParseAnchorName(anchor.name, out var type, out var label, out var kind) || type != TruckTaxiWorldAnchorType.Road)
                throw new InvalidOperationException("Road name must be TT_ROAD_<Type>_<Name>.");
            Vector3[] points = ParseRoadPoints(anchor);
            var duplicates = TruckTaxiWorldBuilder.ValidateAnchorIds(TruckTaxiWorldBuilder.LoadedAnchors());
            if (duplicates.Count > 0) throw new InvalidOperationException(string.Join("\n", duplicates));
            var provider = FindProvider(anchor.gameObject.scene);
            if (provider == null) throw new InvalidOperationException("A single LwsRoadGraphProvider in this scene is required; city graph replacement is not allowed.");
            var before = provider.ValidateGraph();
            if (!before.IsValid) throw new InvalidOperationException("Existing road graph is invalid: " + before.Summary);

            var metadata = anchor.GetComponent<TruckTaxiWorldAnchor>();
            if (metadata != null && (!string.Equals(metadata.RoadType, kind, StringComparison.OrdinalIgnoreCase) || metadata.AnchorType != TruckTaxiWorldAnchorType.Road))
                throw new InvalidOperationException("Road name/type differs from its stored anchor identity.");
            if (update && metadata == null) throw new InvalidOperationException("Update requires a previously built road with a stored anchor ID.");
            if (metadata == null)
            {
                TruckTaxiWorldBuilder.ConfigureAnchor(anchor);
                metadata = anchor.GetComponent<TruckTaxiWorldAnchor>();
            }
            string vendorName = VendorRoadName(metadata.StableId);
            var oldObjects = SnapshotSceneObjects(anchor.gameObject.scene);
            object existingRoad = FindVendorRoad(vendorName, anchor.gameObject.scene);
            if (update && existingRoad == null) throw new InvalidOperationException($"EasyRoads road {vendorName} is missing. Use BUILD SELECTED ROAD.");
            if (!update && existingRoad != null) throw new InvalidOperationException($"EasyRoads road {vendorName} already exists. Use UPDATE SELECTED ROAD.");

            var merged = MergeRoad(provider.Graph, metadata.StableId, kind, points);
            var validation = merged.Validate();
            if (!validation.IsValid) throw new InvalidOperationException("Proposed road graph is invalid: " + validation.Summary);

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(update ? "Update Truck Taxi road" : "Build Truck Taxi road");
            RecordVendorNetwork(anchor.gameObject.scene);
            object network = NewNetwork();
            object road = existingRoad;
            if (update)
            {
                if (road == null) throw new InvalidOperationException("EasyRoads road disappeared during update.");
                Call(road, "SetMarkerPositions", points);
            }
            else
            {
                Type roadTypeClass = Resolve("EasyRoads3Dv3.ERRoadType") ??
                    throw new InvalidOperationException("EasyRoads ERRoadType is unavailable.");
                object roadType = Construct(roadTypeClass);
                var spec = Spec(kind);
                SetMember(roadType, "roadTypeName", "Truck Taxi " + kind);
                SetMember(roadType, "roadWidth", spec.Width);
                SetMember(roadType, "hasMeshCollider", true);
                SetMember(roadType, "roadMaterial", AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/EasyRoads3D/Resources/Materials/roads/road material.mat"));
                road = Call(network, "CreateRoad", vendorName, roadType, points);
                if (road == null) throw new InvalidOperationException("EasyRoads CreateRoad returned null.");
                Call(road, "SetMeshCollider", true);
                Call(road, "SetTerrainDeformation", false);
                Call(road, "SnapToTerrain", false);
            }
            Call(network, "BuildRoadNetwork", false, false, false);
            RegisterCreatedSceneObjects(anchor.gameObject.scene, oldObjects);
            Undo.RecordObject(provider, "Merge Truck Taxi road graph");
            provider.SetGraph(merged, false);
            EditorUtility.SetDirty(provider);
            EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            return $"{(update ? "Updated" : "Built")} {label} ({kind}); graph has {merged.nodes.Count} nodes and {merged.edges.Count} edges.";
        }

        public static Vector3[] ParseRoadPoints(Transform anchor)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            var points = new SortedDictionary<int, Vector3>();
            foreach (Transform child in anchor)
            {
                string name = child.name;
                if (!name.StartsWith("P", StringComparison.OrdinalIgnoreCase) || name.Length < 3 ||
                    !int.TryParse(name.Substring(1), out int index)) continue;
                if (points.ContainsKey(index))
                    throw new InvalidOperationException($"{anchor.name}: duplicate road point P{index:00}.");
                points.Add(index, child.position);
            }
            if (points.Count < 2) throw new InvalidOperationException($"{anchor.name}: at least P00 and P01 are required.");
            int expected = 0;
            Vector3? previous = null;
            foreach (var point in points)
            {
                if (point.Key != expected++) throw new InvalidOperationException($"{anchor.name}: road points must be contiguous from P00.");
                if (previous.HasValue && Vector3.Distance(previous.Value, point.Value) < .5f)
                    throw new InvalidOperationException($"{anchor.name}: adjacent points are too close.");
                previous = point.Value;
            }
            return points.Values.ToArray();
        }

        public static LwsRoadGraph MergeRoad(LwsRoadGraph source, string stableId, string kind, IReadOnlyList<Vector3> points)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Guid.TryParse(stableId, out _) || !IsSupportedRoadType(kind) || points == null || points.Count < 2)
                throw new ArgumentException("A UUID, supported type, and at least two points are required.");
            var graph = LwsRoadGraph.FromJson(source.ToJson());
            string prefix = "tt.world.road." + stableId.ToLowerInvariant();
            graph.edges.RemoveAll(e => e != null && e.edgeId == prefix);
            string startId = prefix + ".start";
            string endId = prefix + ".end";
            graph.nodes.RemoveAll(n => (n.nodeId == startId || n.nodeId == endId) &&
                !graph.edges.Any(e => e.fromNodeId == n.nodeId || e.toNodeId == n.nodeId));
            string from = FindOrAddEndpoint(graph, points[0], startId);
            string to = FindOrAddEndpoint(graph, points[points.Count - 1], endId);
            var spec = Spec(kind);
            var edge = new LwsRoadEdge
            {
                edgeId = prefix, roadId = prefix, segmentId = prefix,
                fromNodeId = from, toNodeId = to, roadClass = spec.RoadClass,
                direction = LwsRoadDirection.Bidirectional, oneWay = false, surfaceType = spec.Surface,
                speedLimitMph = spec.Speed, laneCount = spec.Lanes, laneWidthMeters = spec.LaneWidth,
                leftShoulderWidthMeters = Mathf.Max(0, (spec.Width - spec.Lanes * spec.LaneWidth) / 2),
                rightShoulderWidthMeters = Mathf.Max(0, (spec.Width - spec.Lanes * spec.LaneWidth) / 2),
                stateOrRegionId = "taxi.world", sceneChunkId = "taxi.world"
            };
            float distance = 0;
            for (int segment = 1; segment < points.Count; segment++)
            {
                Vector3 a = points[segment - 1], b = points[segment];
                float length = Vector3.Distance(a, b);
                if (length < .5f) throw new InvalidOperationException("Adjacent road points are too close.");
                int steps = Mathf.Max(1, Mathf.CeilToInt(length / 5));
                for (int step = segment == 1 ? 0 : 1; step <= steps; step++)
                {
                    edge.samples.Add(new LwsRoadSample
                    {
                        roadId = edge.roadId, segmentId = edge.segmentId,
                        position = Vector3.Lerp(a, b, step / (float)steps), forward = (b - a).normalized,
                        up = Vector3.up, distanceFromStartMeters = distance + length * step / steps,
                        direction = edge.direction, roadWidthMeters = spec.Width,
                        laneCount = spec.Lanes, laneWidthMeters = spec.LaneWidth, speedLimitMph = spec.Speed
                    });
                }
                distance += length;
            }
            edge.distanceMeters = edge.travelCost = distance;
            for (int i = 0; i < spec.Lanes; i++)
                edge.laneCenterOffsetsMeters.Add((i - (spec.Lanes - 1) / 2f) * spec.LaneWidth);
            graph.edges.Add(edge);
            return graph;
        }

        private static string FindOrAddEndpoint(LwsRoadGraph graph, Vector3 position, string ownedId)
        {
            var nearest = graph.nodes.OrderBy(n => (n.position - position).sqrMagnitude).FirstOrDefault();
            if (nearest != null && Vector3.Distance(nearest.position, position) <= EndpointMergeMeters)
                return nearest.nodeId;
            graph.nodes.Add(new LwsRoadNode { nodeId = ownedId, position = position,
                regionId = "taxi.world", sceneChunkId = "taxi.world" });
            return ownedId;
        }

        private static RoadSpec Spec(string kind)
        {
            switch (Kinds.First(k => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase)))
            {
                case "Avenue": return new RoadSpec(16, 4, 3.5f, 40, LwsRoadClass.LocalRoad, LwsRoadSurfaceType.AsphaltInterstate);
                case "Highway": return new RoadSpec(22, 4, 3.7f, 65, LwsRoadClass.UsHighway, LwsRoadSurfaceType.AsphaltInterstate);
                case "Alley": return new RoadSpec(5, 1, 4, 15, LwsRoadClass.LocalRoad, LwsRoadSurfaceType.AsphaltInterstate);
                case "ServiceRoad": return new RoadSpec(7, 2, 3, 25, LwsRoadClass.DepotAccess, LwsRoadSurfaceType.AsphaltInterstate);
                case "DirtRoad": return new RoadSpec(7, 2, 3, 20, LwsRoadClass.LocalRoad, LwsRoadSurfaceType.Dirt);
                case "ParkingConnector": return new RoadSpec(6, 1, 4, 10, LwsRoadClass.DepotAccess, LwsRoadSurfaceType.AsphaltInterstate);
                default: return new RoadSpec(10, 2, 3.5f, 30, LwsRoadClass.LocalRoad, LwsRoadSurfaceType.AsphaltInterstate);
            }
        }

        private static LwsRoadGraphProvider FindProvider(Scene scene)
        {
            var providers = Object.FindObjectsByType<LwsRoadGraphProvider>(FindObjectsSortMode.None)
                .Where(p => p.gameObject.scene == scene).ToArray();
            if (providers.Length > 1) throw new InvalidOperationException("Multiple road graph providers found; select a scene with one authority.");
            return providers.FirstOrDefault();
        }

        public static string ValidateRoadGraph()
        {
            var scene = SceneManager.GetActiveScene();
            var provider = FindProvider(scene);
            if (provider == null) return "No LwsRoadGraphProvider in active scene.";
            var graph = provider.ValidateGraph();
            var ids = TruckTaxiWorldBuilder.ValidateAnchorIds(TruckTaxiWorldBuilder.LoadedAnchors());
            return graph.Summary + (ids.Count == 0 ? " Anchor IDs are unique." : "\n" + string.Join("\n", ids));
        }

        private static string VendorRoadName(string id) => VendorNamePrefix + id.ToLowerInvariant();

        private static object FindVendorRoad(string name, Scene scene)
        {
            Type rootType = Resolve("EasyRoads3Dv3.ERModularBase");
            if (rootType == null) throw new InvalidOperationException("EasyRoads ERModularBase is unavailable.");
            bool hasNetwork = Object.FindObjectsByType(rootType, FindObjectsSortMode.None)
                .OfType<Component>().Any(component => component.gameObject.scene == scene);
            if (!hasNetwork) return null;
            Type networkType = Resolve("EasyRoads3Dv3.ERRoadNetwork");
            if (networkType == null) throw new InvalidOperationException("EasyRoads ERRoadNetwork is unavailable.");
            var getRoad = networkType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetRoadByName" && m.GetParameters().Length == 1);
            if (getRoad == null) throw new InvalidOperationException("EasyRoads GetRoadByName API is unavailable.");
            if (getRoad.IsStatic) return getRoad.Invoke(null, new object[] { name });
            return getRoad.Invoke(NewNetwork(), new object[] { name });
        }

        private static object NewNetwork() => Construct(Resolve("EasyRoads3Dv3.ERRoadNetwork") ??
            throw new InvalidOperationException("EasyRoads ERRoadNetwork is unavailable."));

        private static object Construct(Type type)
        {
            var constructor=type.GetConstructors().FirstOrDefault(c=>c.GetParameters().All(p=>p.IsOptional));
            if(constructor==null) throw new MissingMethodException(type.FullName,"optional constructor");
            return constructor.Invoke(constructor.GetParameters().Select(p=>p.DefaultValue).ToArray());
        }

        private static Type Resolve(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        private static object Call(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == name && m.GetParameters().Length >= args.Length &&
                    m.GetParameters().Take(args.Length).Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(v => v) &&
                    m.GetParameters().Skip(args.Length).All(p=>p.IsOptional));
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            return method.Invoke(target, args.Concat(method.GetParameters().Skip(args.Length).Select(p=>p.DefaultValue)).ToArray());
        }

        private static void SetMember(object target, string name, object value)
        {
            var type = target.GetType();
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); return; }
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite) { property.SetValue(target, value); return; }
            throw new MissingMemberException(type.FullName, name);
        }

        private static HashSet<int> SnapshotSceneObjects(Scene scene)
        {
            var result = new HashSet<int>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true)) result.Add(transform.gameObject.GetInstanceID());
            return result;
        }

        private static void RegisterCreatedSceneObjects(Scene scene, HashSet<int> before)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (!before.Contains(transform.gameObject.GetInstanceID()) &&
                        (transform.parent == null || before.Contains(transform.parent.gameObject.GetInstanceID())))
                        Undo.RegisterCreatedObjectUndo(transform.gameObject, "Create EasyRoads object");
        }

        private static void RecordVendorNetwork(Scene scene)
        {
            var type = Resolve("EasyRoads3Dv3.ERModularBase");
            if (type == null) return;
            foreach (var network in Object.FindObjectsByType(type, FindObjectsSortMode.None))
            {
                if (!(network is Component component) || component.gameObject.scene != scene) continue;
                var objects = component.GetComponentsInChildren<Component>(true).Where(c => c != null).Cast<Object>().ToArray();
                if (objects.Length > 0) Undo.RegisterCompleteObjectUndo(objects, "Update EasyRoads network");
            }
        }
    }
}
