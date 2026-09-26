using System;
using System.Collections.Generic;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public sealed class TruckTaxiWorldBuilder : EditorWindow
    {
        private Label result;

        [MenuItem("Truck Taxi/World Builder")]
        public static void Open() => GetWindow<TruckTaxiWorldBuilder>("World Builder");

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 10;
            var content = new ScrollView();
            root.Add(content);
            var heading = new Label("Truck Taxi World Builder");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            content.Add(heading);
            AddButton(content, "SCAN SELECTED ANCHORS", () => ConfigureSelection(false));
            AddButton(content, "SCAN ALL ANCHORS", () => ConfigureSelection(true));
            AddButton(content, "VALIDATE ANCHORS", () => Report(ValidateAnchors()));
            AddButton(content, "BUILD SELECTED ROAD", () => Report(TruckTaxiWorldBuilderRoads.BuildSelectedRoad(false)));
            AddButton(content, "UPDATE SELECTED ROAD", () => Report(TruckTaxiWorldBuilderRoads.BuildSelectedRoad(true)));
            AddButton(content, "BUILD NEW ROADS", () => Report(TruckTaxiWorldBuilderRoads.BuildNewRoads()));
            AddButton(content, "VALIDATE ROAD GRAPH", () => Report(TruckTaxiWorldBuilderRoads.ValidateRoadGraph()));
            result = new Label();
            result.style.whiteSpace = WhiteSpace.Normal;
            result.style.marginTop = 8;
            content.Add(result);
        }

        private void AddButton(VisualElement parent, string label, Action action)
        {
            var button = new Button(() =>
            {
                try { action(); }
                catch (Exception ex) { Report(ex.Message); Debug.LogException(ex); }
            }) { text = label };
            button.style.marginTop = 5;
            parent.Add(button);
        }

        private void Report(string message)
        {
            if (result != null) result.text = message;
            Debug.Log("Truck Taxi World Builder: " + message);
        }

        private void ConfigureSelection(bool all)
        {
            var anchors = all ? LoadedAnchors() : SelectedAnchors();
            var errors = ValidateAnchorIds(LoadedAnchors());
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            int configured = 0;
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure Truck Taxi anchors");
            foreach (var anchor in anchors)
            {
                if (!TryParseAnchorName(anchor.name, out _, out _, out _)) continue;
                if (ConfigureAnchor(anchor)) configured++;
            }
            Undo.CollapseUndoOperations(group);
            Report($"Scanned {anchors.Count} objects; configured {configured} anchors. Existing component settings were left intact.");
        }

        public static bool ConfigureAnchor(Transform anchor)
        {
            if (anchor == null || !anchor.gameObject.scene.IsValid() || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Select a scene anchor in Edit Mode.");
            if (!TryParseAnchorName(anchor.name, out var type, out var label, out var roadType)) return false;
            int componentCount = anchor.GetComponents<Component>().Length;
            int childCount = anchor.childCount;
            var metadata = anchor.GetComponent<TruckTaxiWorldAnchor>();
            if (metadata == null)
            {
                metadata = Undo.AddComponent<TruckTaxiWorldAnchor>(anchor.gameObject);
                Undo.RecordObject(metadata, "Assign Truck Taxi anchor identity");
                metadata.Initialize(Guid.NewGuid().ToString("D"), type, label, roadType);
                EditorUtility.SetDirty(metadata);
            }
            else if (metadata.AnchorType != type || (type == TruckTaxiWorldAnchorType.Road &&
                     !string.Equals(metadata.RoadType, roadType, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"{anchor.name}: name type differs from its stored identity. Keep the original type or author a new anchor.");
            if (type == TruckTaxiWorldAnchorType.Road)
            {
                if (anchor.GetComponents<Component>().Length != componentCount)
                    EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
                return true;
            }
            var duplicate = ValidateAnchorIds(LoadedAnchors());
            if (duplicate.Count > 0) throw new InvalidOperationException(string.Join("\n", duplicate));
            ConfigureComponents(anchor, metadata, label);
            if (anchor.GetComponents<Component>().Length != componentCount || anchor.childCount != childCount)
                EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
            return true;
        }

        private static void ConfigureComponents(Transform anchor, TruckTaxiWorldAnchor metadata, string label)
        {
            string id = metadata.StableId;
            switch (metadata.AnchorType)
            {
                case TruckTaxiWorldAnchorType.Scenic:
                case TruckTaxiWorldAnchorType.Illicit:
                case TruckTaxiWorldAnchorType.Private:
                    if (TryAdd(anchor, out TruckTaxiStopObjectivePoint stop))
                    {
                        stop.stableId = id; stop.displayName = label;
                        stop.district = "";
                        stop.category = metadata.AnchorType == TruckTaxiWorldAnchorType.Scenic ? TruckTaxiStopCategory.Scenic :
                            metadata.AnchorType == TruckTaxiWorldAnchorType.Illicit ? TruckTaxiStopCategory.IllicitPickup : TruckTaxiStopCategory.PrivateMeeting;
                        stop.radius = 9; stop.durationSeconds = 12;
                    }
                    AddMarker(anchor, id, label, metadata.AnchorType == TruckTaxiWorldAnchorType.Scenic ? TruckTaxiMapMarkerType.ScenicStop :
                        metadata.AnchorType == TruckTaxiWorldAnchorType.Illicit ? TruckTaxiMapMarkerType.IllicitStop : TruckTaxiMapMarkerType.PrivateEventStop);
                    break;
                case TruckTaxiWorldAnchorType.Pickup:
                case TruckTaxiWorldAnchorType.Destination:
                    AddLocation(anchor, id, label, TaxiLocationType.RandomStreet,
                        metadata.AnchorType == TruckTaxiWorldAnchorType.Pickup,
                        metadata.AnchorType == TruckTaxiWorldAnchorType.Destination);
                    AddMarker(anchor, id, label, metadata.AnchorType == TruckTaxiWorldAnchorType.Pickup ?
                        TruckTaxiMapMarkerType.PassengerPickup : TruckTaxiMapMarkerType.Destination);
                    break;
                case TruckTaxiWorldAnchorType.Store:
                    var storeLocation = AddLocation(anchor, id, label, TaxiLocationType.Store, false, true);
                    if (TryAdd(anchor, out TruckTaxiStorePoint store))
                    { store.stableId = id; store.displayName = label; store.location = storeLocation; store.stopRadius = 12; }
                    AddMarker(anchor, id, label, TruckTaxiMapMarkerType.FoodStop);
                    break;
                case TruckTaxiWorldAnchorType.Gas:
                    AddLocation(anchor, id, label, TaxiLocationType.GasStation, false, true);
                    if (TryAdd(anchor, out TruckTaxiGasStationPoint gas))
                    { gas.stableId = id; gas.displayName = label; gas.stoppingRadius = 7; gas.centsPerLiter = 150; gas.litersPerSecond = 8; gas.recoveryAnchor = anchor; }
                    AddMarker(anchor, id, label, TruckTaxiMapMarkerType.Destination);
                    break;
                case TruckTaxiWorldAnchorType.Restroom:
                    var restroomLocation = AddLocation(anchor, id, label, TaxiLocationType.RandomStreet, false, true);
                    if (TryAdd(anchor, out TruckTaxiBathroomPoint bathroom))
                    { bathroom.stableId = id; bathroom.displayName = label; bathroom.location = restroomLocation; }
                    AddMarker(anchor, id, label, TruckTaxiMapMarkerType.Bathroom);
                    break;
                case TruckTaxiWorldAnchorType.PedArea:
                    if (anchor.GetComponent<TruckTaxiPedestrianArea>() == null)
                    {
                        var area = EditorTrafficSandboxSetup.ConfigurePedestrianArea(anchor);
                        Undo.RecordObject(area, "Assign pedestrian area ID");
                        area.areaId = id;
                        EditorUtility.SetDirty(area);
                    }
                    break;
                case TruckTaxiWorldAnchorType.Crosswalk:
                case TruckTaxiWorldAnchorType.TrafficLight:
                    if (anchor.GetComponent<TruckTaxiIntersection>() == null)
                    {
                        var intersection = EditorTrafficSandboxSetup.ConfigureIntersection(anchor);
                        Undo.RecordObject(intersection, "Assign intersection ID");
                        intersection.intersectionId = id;
                        EditorUtility.SetDirty(intersection);
                    }
                    break;
                case TruckTaxiWorldAnchorType.Shortcut:
                    if (TryAdd(anchor, out BoxCollider shortcutZone))
                    { shortcutZone.isTrigger = true; shortcutZone.size = new Vector3(8, 4, 8); }
                    if (TryAdd(anchor, out ShortcutTrigger shortcut))
                    { shortcut.shortcutId = id; shortcut.displayName = label; }
                    AddMarker(anchor, id, label, TruckTaxiMapMarkerType.Shortcut);
                    break;
                case TruckTaxiWorldAnchorType.Property:
                    if (TryAdd(anchor, out TruckTaxiImpactTarget impact))
                    { impact.kind = TaxiImpactKind.Property; impact.targetId = id; }
                    if (TryAdd(anchor, out Rigidbody body)) body.isKinematic = true;
                    TryAdd(anchor, out BoxCollider _);
                    break;
                case TruckTaxiWorldAnchorType.Parking:
                case TruckTaxiWorldAnchorType.Building:
                    break;
            }
        }

        private static TruckTaxiRideLocation AddLocation(Transform anchor, string id, string label,
            TaxiLocationType locationType, bool pickup, bool dropoff)
        {
            if (TryAdd(anchor, out TruckTaxiRideLocation location))
            {
                location.locationId = id; location.locationName = label; location.locationType = locationType;
                location.pickupAllowed = pickup; location.dropoffAllowed = dropoff;
            }
            return location;
        }

        private static void AddMarker(Transform anchor, string id, string label, TruckTaxiMapMarkerType type)
        {
            if (TryAdd(anchor, out TruckTaxiMapMarker marker))
            {
                marker.stableId = id; marker.label = label; marker.markerType = type;
                marker.source = anchor; marker.sourceLocalOffset = Vector3.zero;
            }
        }

        private static bool TryAdd<T>(Transform anchor, out T component) where T : Component
        {
            component = anchor.GetComponent<T>();
            if (component != null) return false;
            component = Undo.AddComponent<T>(anchor.gameObject);
            Undo.RecordObject(component, "Configure Truck Taxi component");
            return true;
        }

        public static bool TryParseAnchorName(string name, out TruckTaxiWorldAnchorType type, out string label, out string roadType)
        {
            type = default; label = null; roadType = null;
            if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("TT_", StringComparison.OrdinalIgnoreCase)) return false;
            string[] names = { "SCENIC", "ILLICIT", "PRIVATE", "STORE", "GAS", "RESTROOM", "PICKUP", "DESTINATION",
                "PEDAREA", "CROSSWALK", "TRAFFICLIGHT", "SHORTCUT", "PROPERTY", "PARKING", "BUILDING" };
            for (int i = 0; i < names.Length; i++)
            {
                string prefix = "TT_" + names[i] + "_";
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                label = name.Substring(prefix.Length).Replace('_', ' ').Trim();
                if (label.Length == 0) return false;
                type = (TruckTaxiWorldAnchorType)i;
                return true;
            }
            if (!name.StartsWith("TT_ROAD_", StringComparison.OrdinalIgnoreCase)) return false;
            string remainder = name.Substring(8);
            int separator = remainder.IndexOf('_');
            if (separator <= 0 || separator == remainder.Length - 1) return false;
            roadType = remainder.Substring(0, separator);
            if (!TruckTaxiWorldBuilderRoads.IsSupportedRoadType(roadType)) return false;
            label = remainder.Substring(separator + 1).Replace('_', ' ').Trim();
            if (label.Length == 0) return false;
            type = TruckTaxiWorldAnchorType.Road;
            return true;
        }

        public static List<Transform> LoadedAnchors()
        {
            var result = new List<Transform>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects()) Collect(root.transform, result);
            }
            return result;
        }

        private static List<Transform> SelectedAnchors()
        {
            var result = new List<Transform>();
            var seen = new HashSet<Transform>();
            foreach (var go in Selection.gameObjects)
            {
                if (!go.scene.IsValid()) continue;
                var local = new List<Transform>();
                Collect(go.transform, local);
                foreach (var item in local) if (seen.Add(item)) result.Add(item);
            }
            return result;
        }

        private static void Collect(Transform root, List<Transform> result)
        {
            result.Add(root);
            foreach (Transform child in root) Collect(child, result);
        }

        public static List<string> ValidateAnchorIds(IEnumerable<Transform> objects)
        {
            var errors = new List<string>();
            var ids = new Dictionary<string, TruckTaxiWorldAnchor>(StringComparer.OrdinalIgnoreCase);
            foreach (var transform in objects)
            {
                var anchor = transform.GetComponent<TruckTaxiWorldAnchor>();
                if (anchor == null) continue;
                if (!Guid.TryParse(anchor.StableId, out _))
                    errors.Add($"{transform.name}: missing or invalid UUID.");
                else if (ids.TryGetValue(anchor.StableId, out var previous) && previous != anchor)
                    errors.Add($"Duplicate stable ID {anchor.StableId}: {previous.name} and {anchor.name}.");
                else ids[anchor.StableId] = anchor;
            }
            return errors;
        }

        public static string ValidateAnchors()
        {
            var all = LoadedAnchors();
            var errors = ValidateAnchorIds(all);
            int recognized = all.Count(t => TryParseAnchorName(t.name, out _, out _, out _));
            return errors.Count == 0 ? $"{recognized} named anchors; stored IDs are unique." : string.Join("\n", errors);
        }

        // Parent-owned integration entry point; stages a small opt-in DemoCity fixture.
        public static string ConfigureExamples()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "TruckTaxi_DemoCity" || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open TruckTaxi_DemoCity in Edit Mode before staging examples.");
            var errors = ValidateAnchorIds(LoadedAnchors());
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Transform root = scene.GetRootGameObjects().Select(go => go.transform)
                .FirstOrDefault(t => t.name == "Truck Taxi World Builder Examples");
            if (root == null)
            {
                var go = new GameObject("Truck Taxi World Builder Examples");
                Undo.RegisterCreatedObjectUndo(go, "Stage Truck Taxi examples");
                root = go.transform;
            }
            string[] names = { "TT_SCENIC_North_View", "TT_ILLICIT_Quiet_Lot", "TT_PRIVATE_Garden",
                "TT_STORE_Corner_Shop", "TT_GAS_Route_66", "TT_RESTROOM_Service_Stop",
                "TT_PICKUP_North_Corner", "TT_DESTINATION_West_Corner", "TT_PEDAREA_Outer_Plaza",
                "TT_CROSSWALK_Center", "TT_TRAFFICLIGHT_East", "TT_SHORTCUT_Service_Cut",
                "TT_PROPERTY_Test_Crate", "TT_PARKING_South_Lot", "TT_BUILDING_South_Block" };
            Vector3[] positions = {
                new Vector3(-307,.2f,-260), new Vector3(-147,.2f,-260), new Vector3(13,.2f,-260),
                new Vector3(173,.2f,-260), new Vector3(333,.2f,-260), new Vector3(-307,.2f,-100),
                new Vector3(-147,.2f,-100), new Vector3(13,.2f,-100), new Vector3(-230,.2f,-230),
                new Vector3(-160,.2f,-160), new Vector3(0,.2f,-160), new Vector3(173,.2f,60),
                new Vector3(90,.2f,90), new Vector3(230,.2f,230), new Vector3(250,.2f,250)
            };
            for (int i = 0; i < names.Length; i++) StageExample(root, names[i], positions[i]);
            var road = StageExample(root, "TT_ROAD_ServiceRoad_East_Connector", new Vector3(320,.15f,320));
            StageExample(road, "P00", new Vector3(320,.15f,320));
            StageExample(road, "P01", new Vector3(395,.15f,320));
            int count = 0;
            var failures = new List<string>();
            foreach (var example in names.Select(name => root.Find(name)).Where(t => t != null))
            {
                try { if (ConfigureAnchor(example)) count++; }
                catch (Exception ex) { failures.Add(example.name + ": " + ex.Message); }
            }
            ConfigureAnchor(road);
            EditorSceneManager.MarkSceneDirty(scene);
            return $"Staged {names.Length} example anchors and one unbuilt road; configured {count}." +
                (failures.Count == 0 ? "" : " Failures: " + string.Join("; ", failures));
        }

        private static Transform StageExample(Transform parent, string name, Vector3 position)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Stage Truck Taxi example");
            Undo.SetTransformParent(go.transform, parent, "Parent Truck Taxi example");
            go.transform.position = position;
            return go.transform;
        }
    }
}
