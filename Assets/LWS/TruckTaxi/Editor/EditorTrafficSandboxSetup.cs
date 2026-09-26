using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckTaxi.Editor
{
    public static class EditorTrafficSandboxSetup
    {
        private const string SignalPrefab = "Assets/UTS_FullPack/Models/Crossroad_prefabs/New Semaphore Prefabs/Standard Semaphore.prefab";

        [MenuItem("Truck Taxi/Traffic/Set Up Bounded UTS Sandbox")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode before authoring traffic.");
            var population = UnityEngine.Object.FindFirstObjectByType<TruckTaxiPedestrianPopulation>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<TruckTaxiTrafficAdapter>();
            if (population == null || traffic == null || population.gameObject.scene.name != "TruckTaxi_DemoCity")
                throw new InvalidOperationException("Open TruckTaxi_DemoCity and retain its existing traffic and population objects.");
            var source = population.peoplePaths != null && population.peoplePaths.Length > 0 ? population.peoplePaths[0] : null;
            if (source == null) throw new InvalidOperationException("An authored UTS pedestrian path is required as the prefab source.");
            var prefabs = Field(source, "walkingPrefabs") as GameObject[];
            if (prefabs == null || prefabs.Length == 0) throw new InvalidOperationException("The UTS source path has no pedestrian prefabs.");
            var areas = BuildAreas(population, traffic, prefabs);
            var intersection = BuildIntersection(population, prefabs);
            Undo.RecordObject(population, "Wire UTS sandbox paths");
            population.pedestrianAreas = areas.ToArray();
            population.signalIntersections = new[] { intersection };
            population.roadLanes = traffic.cityLanes;
            EditorUtility.SetDirty(population);
            EditorSceneManager.MarkSceneDirty(population.gameObject.scene);
            Debug.Log("TAXI UTS SANDBOX: local areas and one vendor signal/crosswalk staged. Save and run parent-owned Play Mode checks; this menu does not test the scene.");
        }

        // World Builder entry points. Both preserve any existing authored child path or signal prefab.
        public static TruckTaxiPedestrianArea ConfigurePedestrianArea(Transform anchor)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            var population = UnityEngine.Object.FindFirstObjectByType<TruckTaxiPedestrianPopulation>();
            var traffic = UnityEngine.Object.FindFirstObjectByType<TruckTaxiTrafficAdapter>();
            if (population == null || traffic == null || population.peoplePaths == null || population.peoplePaths.Length == 0)
                throw new InvalidOperationException("UTS pedestrian population and traffic must be present.");
            var prefabs = Field(population.peoplePaths[0], "walkingPrefabs") as GameObject[];
            if (prefabs == null || prefabs.Length == 0) throw new InvalidOperationException("UTS source path has no pedestrian prefabs.");
            var area = anchor.GetComponent<TruckTaxiPedestrianArea>() ?? Undo.AddComponent<TruckTaxiPedestrianArea>(anchor.gameObject);
            Undo.RecordObject(area, "Configure pedestrian area");
            area.Configure(anchor.name, area.localSize, area.roadSetback);
            if (area.walkingPaths == null || area.walkingPaths.Length == 0)
            {
                int variant = 0;
                foreach (char letter in anchor.name) variant += letter;
                var route = LocalRoute(anchor, variant);
                foreach (var point in route)
                    if (!area.Allows(point, traffic.cityLanes)) throw new InvalidOperationException("Pedestrian anchor overlaps an active road: " + anchor.name);
                area.walkingPaths = new[] { CreatePath(anchor, "UTS Local Walk", prefabs, route, true, .08f) };
            }
            var areas = new List<TruckTaxiPedestrianArea>(population.pedestrianAreas ?? Array.Empty<TruckTaxiPedestrianArea>());
            if (!areas.Contains(area)) areas.Add(area);
            Undo.RecordObject(population, "Wire pedestrian area");
            population.pedestrianAreas = areas.ToArray();
            population.roadLanes = traffic.cityLanes;
            EditorUtility.SetDirty(area); EditorUtility.SetDirty(population);
            EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
            return area;
        }

        public static TruckTaxiIntersection ConfigureIntersection(Transform anchor)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            var population = UnityEngine.Object.FindFirstObjectByType<TruckTaxiPedestrianPopulation>();
            if (population == null || population.peoplePaths == null || population.peoplePaths.Length == 0)
                throw new InvalidOperationException("UTS pedestrian population is missing.");
            var prefabs = Field(population.peoplePaths[0], "walkingPrefabs") as GameObject[];
            if (prefabs == null || prefabs.Length == 0) throw new InvalidOperationException("UTS source path has no pedestrian prefabs.");
            // A crossing and traffic-light anchor can describe the same junction.
            // Reuse its vendor gates instead of adding overlapping semaphore controllers.
            TruckTaxiIntersection nearby = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<TruckTaxiIntersection>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == anchor.gameObject.scene && candidate.IsConfigured &&
                    Vector3.Distance(candidate.transform.position, anchor.position) < 2f)
                { nearby = candidate; break; }
            var intersection = nearby != null
                ? anchor.GetComponent<TruckTaxiIntersection>() ?? Undo.AddComponent<TruckTaxiIntersection>(anchor.gameObject)
                : BuildIntersection(anchor, prefabs);
            if (nearby != null)
            {
                Undo.RecordObject(intersection, "Reuse UTS junction");
                intersection.Configure(anchor.name, nearby.signalSystem, nearby.movementGates, nearby.crosswalkPath);
                EditorUtility.SetDirty(intersection);
            }
            var intersections = new List<TruckTaxiIntersection>(population.signalIntersections ?? Array.Empty<TruckTaxiIntersection>());
            if (!intersections.Contains(intersection)) intersections.Add(intersection);
            Undo.RecordObject(population, "Wire UTS signal crossing");
            population.signalIntersections = intersections.ToArray();
            EditorUtility.SetDirty(population);
            EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
            return intersection;
        }

        private static List<TruckTaxiPedestrianArea> BuildAreas(TruckTaxiPedestrianPopulation population,
            TruckTaxiTrafficAdapter traffic, GameObject[] prefabs)
        {
            var result = new List<TruckTaxiPedestrianArea>();
            Transform root = Child(population.transform, "Sandbox UTS Areas");
            for (int sourceIndex = 0; sourceIndex < population.peoplePaths.Length; sourceIndex++)
            {
                var original = population.peoplePaths[sourceIndex];
                if (original == null) continue;
                var nodes = Field(original, "pathPointTransform") as IList;
                if (nodes == null || nodes.Count < 4) continue;
                Vector3 min = new Vector3(float.PositiveInfinity, 0, float.PositiveInfinity);
                Vector3 max = new Vector3(float.NegativeInfinity, 0, float.NegativeInfinity);
                foreach (GameObject node in nodes) { min = Vector3.Min(min, node.transform.position); max = Vector3.Max(max, node.transform.position); }
                bool longZ = max.z-min.z >= max.x-min.x;
                for (int part = 0; part < 3; part++)
                {
                    string name = $"Pedestrian Area {sourceIndex:00}-{part:00}";
                    Transform existingArea = root.Find(name);
                    Transform areaRoot = existingArea != null ? existingArea : Child(root, name);
                    Vector3 center = (min+max)*.5f;
                    if (longZ) center.z = Mathf.Lerp(min.z+11, max.z-11, (part+.5f)/3f);
                    else center.x = Mathf.Lerp(min.x+11, max.x-11, (part+.5f)/3f);
                    center.y = original.transform.position.y;
                    if (existingArea == null) areaRoot.position = center;
                    var area = areaRoot.GetComponent<TruckTaxiPedestrianArea>() ?? Undo.AddComponent<TruckTaxiPedestrianArea>(areaRoot.gameObject);
                    area.Configure(name, area.localSize, area.roadSetback);
                    if (area.walkingPaths == null || area.walkingPaths.Length == 0)
                    {
                        var route = LocalRoute(areaRoot, sourceIndex*3+part);
                        foreach (var point in route)
                            if (!area.Allows(point, traffic.cityLanes)) throw new InvalidOperationException($"Area {name} overlaps a road. Move its anchor and retry.");
                        area.walkingPaths = new[] { CreatePath(areaRoot, "UTS Local Walk", prefabs, route, true, .08f) };
                        EditorUtility.SetDirty(area);
                    }
                    result.Add(area);
                }
            }
            if (result.Count == 0) throw new InvalidOperationException("No viable UTS source path areas were found.");
            return result;
        }

        private static TruckTaxiIntersection BuildIntersection(TruckTaxiPedestrianPopulation population, GameObject[] prefabs)
        {
            Transform parent = population.transform.parent != null ? population.transform.parent : population.transform;
            Transform root = parent.Find("UTS Signal Crosswalk Example");
            if (root == null)
            {
                root = Child(parent, "UTS Signal Crosswalk Example");
                root.position = new Vector3(-160, .2f, -160);
            }
            return BuildIntersection(root, prefabs);
        }

        private static TruckTaxiIntersection BuildIntersection(Transform root, GameObject[] prefabs)
        {
            Transform signalRoot = root.Find("Vendor Standard Semaphore");
            if (signalRoot == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SignalPrefab);
                if (prefab == null) throw new InvalidOperationException("UTS standard semaphore prefab missing.");
                var signal = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(signal, "Add UTS standard semaphore");
                signal.name = "Vendor Standard Semaphore";
                Undo.SetTransformParent(signal.transform, root, "Parent UTS semaphore");
                signal.transform.localPosition = Vector3.zero;
                signalRoot = signal.transform;
            }
            Component controller = null;
            var gates = new List<Component>();
            foreach (var component in signalRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component.GetType().Name == "StandardSemaphoreSystem") controller = component;
                if (component.GetType().Name == "SemaphoreMovementSide") gates.Add(component);
            }
            if (controller == null || gates.Count == 0) throw new InvalidOperationException("UTS standard semaphore is missing its controller or movement gates.");
            var gateCollider = gates[0].GetComponent<BoxCollider>();
            if (gateCollider == null) throw new InvalidOperationException("UTS crosswalk gate collider missing.");
            var crossing = root.GetComponent<TruckTaxiIntersection>() ?? Undo.AddComponent<TruckTaxiIntersection>(root.gameObject);
            var bounds = gateCollider.bounds;
            Vector3 center = bounds.center; center.y = root.position.y;
            float half = Mathf.Max(bounds.extents.x, bounds.extents.z) + 3;
            bool alongX = bounds.size.x >= bounds.size.z;
            Vector3 direction = alongX ? Vector3.right : Vector3.forward;
            if (crossing.crosswalkPath == null)
            {
                Vector3[] points = { center-direction*half, center-direction*half*.5f, center,
                    center+direction*half*.5f, center+direction*half };
                crossing.crosswalkPath = CreatePath(root, "UTS Signal-Controlled Crossing", prefabs, points, false, .13f);
            }
            CreateMarkings(root, center, direction, half);
            crossing.Configure(root.name, controller, gates.ToArray(), crossing.crosswalkPath);
            EditorUtility.SetDirty(crossing);
            return crossing;
        }

        private static void CreateMarkings(Transform parent, Vector3 center, Vector3 crossingDirection, float half)
        {
            Vector3 across = Vector3.Cross(Vector3.up, crossingDirection);
            for (int i=-4;i<=4;i++) Mark(parent, "Crosswalk stripe " + (i+4), center + crossingDirection*i*1.7f,
                crossingDirection*1.05f + across*3.5f);
            Mark(parent, "Near stop line", center-across*6, crossingDirection*(half*1.5f)+across*.25f);
            Mark(parent, "Far stop line", center+across*6, crossingDirection*(half*1.5f)+across*.25f);
        }

        private static Vector3[] LocalRoute(Transform anchor, int variant)
        {
            Vector3[][] shapes = {
                new[] { new Vector3(-8,0,-4), new Vector3(-3,0,5), new Vector3(6,0,4), new Vector3(8,0,-3), new Vector3(1,0,-6) },
                new[] { new Vector3(-9,0,2), new Vector3(-5,0,-5), new Vector3(2,0,-6), new Vector3(8,0,-1), new Vector3(5,0,5) },
                new[] { new Vector3(-7,0,-5), new Vector3(-8,0,3), new Vector3(-1,0,6), new Vector3(8,0,4), new Vector3(6,0,-5) }
            };
            var shape = shapes[Mathf.Abs(variant) % shapes.Length];
            var route = new Vector3[shape.Length];
            for (int i=0;i<route.Length;i++) route[i] = anchor.TransformPoint(shape[i]);
            return route;
        }

        private static void Mark(Transform parent, string name, Vector3 at, Vector3 dimensions)
        {
            if (parent.Find(name) != null) return;
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(mark, "Add crossing paint");
            mark.name = name;
            Undo.SetTransformParent(mark.transform, parent, "Parent crossing paint");
            at.y = parent.position.y + .025f;
            mark.transform.position = at;
            mark.transform.localScale = new Vector3(Mathf.Abs(dimensions.x), .035f, Mathf.Abs(dimensions.z));
            Undo.DestroyObjectImmediate(mark.GetComponent<Collider>());
        }

        private static Component CreatePath(Transform parent, string name, GameObject[] prefabs,
            Vector3[] points, bool loop, float density)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                var existingPath = existing.GetComponent(Resolve("PeopleWalkPath"));
                if (existingPath != null) return existingPath;
            }
            Transform root = Child(parent, name);
            var type = Resolve("PeopleWalkPath");
            var path = Undo.AddComponent(root.gameObject, type);
            SetField(path, "walkingPrefabs", prefabs);
            SetField(path, "numberOfWays", 1);
            SetField(path, "loopPath", loop);
            SetField(path, "Density", density);
            SetField(path, "_minimalObjectLength", 3f);
            SetField(path, "disableLineDraw", true);
            SetField(path, "_ignoreCar", false);
            var transforms = (IList)Field(path, "pathPointTransform");
            var positions = (IList)Field(path, "pathPoint");
            for (int i=0;i<points.Length;i++)
            {
                Transform node = Child(root, $"P{i:00}");
                node.position = points[i];
                transforms.Add(node.gameObject);
                positions.Add(points[i]);
            }
            MethodInfo draw = type.GetMethod("DrawCurved");
            var direction = draw.GetParameters()[1].ParameterType;
            draw.Invoke(path, new object[] { false, Enum.Parse(direction, "Forward") });
            EditorUtility.SetDirty(path);
            return path;
        }

        private static Transform Child(Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null) return found;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create UTS sandbox object");
            Undo.SetTransformParent(go.transform, parent, "Parent UTS sandbox object");
            return go.transform;
        }
        private static Type Resolve(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new InvalidOperationException("UTS type not found: " + name);
        }
        private static object Field(Component component, string name) =>
            component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(component);
        private static void SetField(Component component, string name, object value) =>
            component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(component, value);
    }
}
