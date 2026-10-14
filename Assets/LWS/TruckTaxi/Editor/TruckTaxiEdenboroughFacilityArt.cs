using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    /// <summary>Explicit, scene-independent assembly of inspected owned art. No startup hooks.</summary>
    public static class TruckTaxiEdenboroughFacilityArt
    {
        private const string GarageRoot = "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/";
        private const string WarehouseRoot = "Assets/Hivemind/ModularWarehouse/URP/Art/Prefabs/";
        private const string ShopRoot = "Assets/Hivemind/ShopStore/URP/Art/Prefabs/";
        private const string RestaurantRoot = "Assets/Modern Resaturant - building and interior/Models/Prefabs/";
        private const string GarageFloor = GarageRoot + "SM_Floor_4x6m_01.prefab";
        private const string GarageWall = GarageRoot + "SM_Wall_4x3m_01.prefab";
        private const string GarageCeiling = GarageRoot + "SM_Celling_4x6m_01.prefab";
        private const string ToolCabinet = GarageRoot + "SM_ToolCabinet_01.prefab";
        private const string WorkTable = GarageRoot + "SM_MetalTable.prefab";
        private const string MetalShelf = GarageRoot + "SM_MetalShelve_01.prefab";
        private const string Crate = GarageRoot + "SM_PK04_WoodenCrate_01.prefab";
        private const string WarehouseFloor = WarehouseRoot + "SM_Floor_Concrete_600_A.prefab";
        private const string WarehouseWall = WarehouseRoot + "SM_Wall_Hangar_600_300_A.prefab";
        private const string WarehouseCeiling = WarehouseRoot + "SM_Ceiling_Hangar_600_A.prefab";
        private const string Counter = ShopRoot + "SM_Counter_1.prefab";
        private const string Register = ShopRoot + "SM_Register.prefab";
        private const string ShopShelf = ShopRoot + "SM_Shelf_Unit_Reg.prefab";
        private const string RoomFloor = RestaurantRoot + "Floor_200x200.prefab";
        private const string RoomWall = RestaurantRoot + "Wall_200x300.prefab";
        private const string RoomDoorway = RestaurantRoot + "Wall_Doorway_200x300.prefab";
        private const string RoomHeader = RestaurantRoot + "Wall_Block_50x50.prefab";
        private const string RoomWindow = RestaurantRoot + "Wall_SmallWindow_200x300.prefab";
        private const string DiningTable = RestaurantRoot + "Restaurant_Table_Rectangular_1.prefab";
        private const string Chair = RestaurantRoot + "Restaurant_chair.prefab";
        private const string Reception = RestaurantRoot + "Restaurant_Reception_Desk.prefab";
        private const string Oven = RestaurantRoot + "kitchen_module_oven.prefab";
        public const string HotelPrefabPath = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Hotel_01.prefab";
        public const string BedPrefabPath = "Assets/HQ_ResidentialHouse/Prefabs/Bed.prefab";
        private const string Toilet = "Assets/ToiletSet/URP/Prefabs/SM_Toilet Variant.prefab";
        private const string Sink = "Assets/ToiletSet/URP/Prefabs/SM_Sink Variant.prefab";
        private const string Hammer = "Assets/HandToolsPack/URP/Prefabs/Hammer_GameReady_LODs.prefab";
        private const string Wrench = "Assets/HandToolsPack/URP/Prefabs/Wrench_GameReady_LODs.prefab";
        private const string Vice = "Assets/HandToolsPack/URP/Prefabs/Vice_GameReady_LODs.prefab";
        private const string FreightCrate = "Assets/IndieKit/Crates and Barrels/Prefabs/Non-Destructible/crate_01.prefab";
        private const string FreightBarrel = "Assets/IndieKit/Crates and Barrels/Prefabs/Non-Destructible/barrel_01.prefab";

        public static IReadOnlyList<string> RequiredPrefabPaths { get; } = Array.AsReadOnly(new[]
        {
            GarageFloor, GarageWall, GarageCeiling, ToolCabinet, WorkTable, MetalShelf, Crate,
            WarehouseFloor, WarehouseWall, WarehouseCeiling, Counter, Register, ShopShelf,
            RoomFloor, RoomWall, RoomHeader, RoomWindow, DiningTable, Chair, Reception, Oven,
            HotelPrefabPath, BedPrefabPath, Toilet, Sink, Hammer, Wrench, Vice, FreightCrate, FreightBarrel
        });

        // Origin is floor height at bay center; travel is along local Z in either direction.
        public static Bounds GarageBayBounds => new Bounds(new Vector3(0, 3, 0), new Vector3(7.8f, 5.8f, 23.8f));

        public sealed class Result
        {
            public GameObject Garage { get; internal set; }
            public GameObject Warehouse { get; internal set; }
            public GameObject Diner { get; internal set; }
            public GameObject Store { get; internal set; }
            public GameObject Motel { get; internal set; }
            public GameObject Restroom { get; internal set; }
            public Bounds GarageFootprint => GeometryBounds(Garage);
            public Bounds WarehouseFootprint => GeometryBounds(Warehouse);
            public Bounds DinerFootprint => GeometryBounds(Diner);
            public Bounds StoreFootprint => GeometryBounds(Store);
            public Bounds MotelFootprint => GeometryBounds(Motel);
        }

        /// <summary>
        /// Save assembled prefab assets under a dedicated Assets/LWS/TruckTaxi subfolder.
        /// reviewedPrefab must return a persistent prefab asset with reviewed URP materials,
        /// unchanged geometry and native scale (for example main's project-owned material variant).
        /// Existing outputs are reused unless rebuild is explicitly requested.
        /// </summary>
        public static Result Build(string assetFolder, Func<GameObject, GameObject> reviewedPrefab, bool rebuild = false)
        {
            if (reviewedPrefab == null) throw new ArgumentNullException(nameof(reviewedPrefab));
            assetFolder = ValidateFolder(assetFolder);
            var modules = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            var missing = new List<string>();
            foreach (string path in RequiredPrefabPaths)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null) { missing.Add(path); continue; }
                var reviewed = reviewedPrefab(source);
                if (reviewed == null || !EditorUtility.IsPersistent(reviewed) || !PrefabUtility.IsPartOfPrefabAsset(reviewed))
                    throw new InvalidOperationException("Reviewed adapter must return a prefab asset: " + path);
                if ((reviewed.transform.localScale - source.transform.localScale).sqrMagnitude > .000001f ||
                    (GeometryBounds(reviewed).size - GeometryBounds(source).size).sqrMagnitude > .0001f)
                    throw new InvalidOperationException("Reviewed adapter changed native art scale/geometry: " + path);
                ValidateMaterials(reviewed, path);
                modules.Add(path, reviewed);
            }
            if (missing.Count != 0)
                throw new InvalidOperationException("Import these exact inspected prefab roots with dependencies:\n" + string.Join("\n", missing));
            EnsureFolder(assetFolder);
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var assembly = new Assembly(modules, scene, assetFolder, rebuild);
                return new Result
                {
                    Garage = assembly.Save("Garage", assembly.Garage),
                    Warehouse = assembly.Save("Warehouse", assembly.Warehouse),
                    Diner = assembly.Save("Diner", assembly.Diner),
                    Store = assembly.Save("Store", assembly.Store),
                    Motel = assembly.Save("Motel", assembly.Motel),
                    Restroom = assembly.Save("Restroom", assembly.Restroom)
                };
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private sealed class Assembly
        {
            private readonly Dictionary<string, GameObject> modules;
            private readonly Scene scene;
            private readonly string folder;
            private readonly bool rebuild;
            public Assembly(Dictionary<string, GameObject> modules, Scene scene, string folder, bool rebuild)
            { this.modules = modules; this.scene = scene; this.folder = folder; this.rebuild = rebuild; }

            public GameObject Save(string kind, Action<GameObject> construct)
            {
                string name = "Edenborough_" + kind;
                string path = folder + "/Edenborough_" + kind + ".prefab";
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null && existing.name != name)
                    throw new InvalidOperationException("Refusing to overwrite unrelated prefab: " + path);
                if (existing != null && !rebuild) return existing;
                var root = new GameObject(name);
                SceneManager.MoveGameObjectToScene(root, scene);
                try
                {
                    construct(root);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                    if (!success || prefab == null) throw new InvalidOperationException("Could not save assembled facility: " + path);
                    return prefab;
                }
                finally { Object.DestroyImmediate(root); }
            }

            public void Garage(GameObject root)
            {
                Shell(root, GarageFloor, GarageCeiling, GarageWall, 4, 6, 4, 4, 4, 3, 2,
                    (column, north) => column == 1 || column == 2 ? null : GarageWall);
                Prop(root, ToolCabinet, "Garage.ToolCabinet", new Vector3(-6, 0, -8), 90, new Vector2(3, 4));
                var bench = Prop(root, WorkTable, "Garage.Workbench", new Vector3(-6, 0, 0), 90, new Vector2(3, 4));
                float benchTop = GeometryBounds(bench).max.y + .01f;
                Prop(root, Vice, "Garage.Workbench.Vice", new Vector3(-6, benchTop, -.6f), 90, new Vector2(1, 1));
                Prop(root, Hammer, "Garage.Workbench.Hammer", new Vector3(-6, benchTop, .2f), 0, new Vector2(1, 1));
                Prop(root, Wrench, "Garage.Workbench.Wrench", new Vector3(-6, benchTop, .8f), 90, new Vector2(1, 1));
                Prop(root, MetalShelf, "Garage.PartsShelf", new Vector3(6, 0, 6), 90, new Vector2(3, 5));
                Prop(root, FreightCrate, "Garage.PartsCrate", new Vector3(6, 0, -6), 0, new Vector2(3, 3));
                Anchor(root, "Garage.Bay.Center", Vector3.zero);
                Anchor(root, "Entrance.South", new Vector3(0, 0, -12));
                Anchor(root, "Entrance.North", new Vector3(0, 0, 12));
                Light(root, new Vector3(-5, 5.5f, -6), 12);
                Light(root, new Vector3(5, 5.5f, 6), 12);
                RequireClearance(root, GarageBayBounds);
            }

            public void Warehouse(GameObject root)
            {
                // The hangar ceiling is a sloped member, not a 6m planar tile. Reuse the
                // native concrete panels as a flat roof instead of stretching that mesh.
                Shell(root, WarehouseFloor, WarehouseFloor, WarehouseWall, 6, 6, 3, 4, 6, 3, 2,
                    (column, north) => !north && column == 1 ? null : WarehouseWall, true);
                Prop(root, MetalShelf, "Warehouse.Storage.Left", new Vector3(-7, 0, 5), 90, new Vector2(3, 5));
                Prop(root, MetalShelf, "Warehouse.Storage.Right", new Vector3(7, 0, 5), 90, new Vector2(3, 5));
                Prop(root, Crate, "Warehouse.Freight", new Vector3(6, 0, -5), 0, new Vector2(3, 4));
                Prop(root, FreightCrate, "Warehouse.FreightCrate", new Vector3(6, 0, -8), 0, new Vector2(3, 3));
                Prop(root, FreightBarrel, "Warehouse.FreightBarrel", new Vector3(-6, 0, -5), 0, new Vector2(3, 3));
                Anchor(root, "Entrance.South", new Vector3(0, 0, -12));
                Anchor(root, "Warehouse.Loading", new Vector3(0, 0, -6));
                Light(root, new Vector3(0, 5.5f, 0), 18);
                RequireClearance(root, new Bounds(new Vector3(0, 2.5f, -6), new Vector3(5.5f, 4.8f, 12)));
            }

            public void Diner(GameObject root)
            {
                RoomShell(root, 5, 4, new[] { 2 });
                foreach (float x in new[] { -3f, 3f })
                {
                    string side = x < 0 ? "Left" : "Right";
                    var table = Prop(root, DiningTable, "Diner.Table." + side, new Vector3(x, 0, 0), 0, new Vector2(3.5f, 2.5f));
                    float edge = GeometryBounds(table).extents.z + GeometryBounds(modules[Chair]).extents.z + .15f;
                    Prop(root, Chair, "Diner.Seat." + side + ".South", new Vector3(x, 0, -edge), 0, new Vector2(1.2f, 1.2f));
                    Prop(root, Chair, "Diner.Seat." + side + ".North", new Vector3(x, 0, edge), 180, new Vector2(1.2f, 1.2f));
                }
                Prop(root, Oven, "Diner.Kitchen.Oven", new Vector3(2.5f, 0, 3), 180, new Vector2(3, 1.6f));
                Prop(root, Reception, "Diner.Counter", new Vector3(-2.5f, 0, 3), 180, new Vector2(4, 1.6f));
                Anchor(root, "Diner.Order", new Vector3(-2.5f, 0, 1.8f));
                CheckEntrance(root, 0, -4);
                RequirePropsInside(root, new Bounds(new Vector3(0, 1.5f, 0), new Vector3(9.7f, 3, 7.7f)));
            }

            public void Store(GameObject root)
            {
                RoomShell(root, 5, 4, new[] { 2 });
                var counter = Prop(root, Counter, "Store.Checkout.Counter", new Vector3(-2.8f, 0, -1), 90, new Vector2(3.5f, 3.5f));
                float top = GeometryBounds(counter).max.y;
                Prop(root, Register, "Store.Checkout.Register", new Vector3(-2.8f, top, -1), 90, new Vector2(1.2f, 1.2f));
                Prop(root, ShopShelf, "Store.Shelf.Right", new Vector3(3.2f, 0, 0), 90, new Vector2(3, 5));
                Prop(root, ShopShelf, "Store.Shelf.Rear", new Vector3(0, 0, 2.8f), 0, new Vector2(5, 1.8f));
                Anchor(root, "Store.Checkout", new Vector3(-1.2f, 0, -1));
                CheckEntrance(root, 0, -4);
                RequirePropsInside(root, new Bounds(new Vector3(0, 1.5f, 0), new Vector3(9.7f, 3, 7.7f)));
            }

            public void Motel(GameObject root)
            {
                RoomShell(root, 9, 3, new[] { 1, 4, 7 });
                foreach (float x in new[] { -3f, 3f })
                    for (int z = 0; z < 3; z++) Wall(root, RoomWall, new Vector3(x, 0, -2 + z * 2), 90, 2, 3);
                for (int room = 0; room < 3; room++)
                {
                    float x = (room - 1) * 6;
                    Prop(root, BedPrefabPath, "Motel.Bed." + room, new Vector3(x + 1.25f, 0, .5f), 0, new Vector2(2.8f, 3.5f));
                    Prop(root, Chair, "Motel.Chair." + room, new Vector3(x - 1.6f, 0, 1.8f), 180, new Vector2(1.2f, 1.2f));
                    Anchor(root, "Motel.Room." + room, new Vector3(x, 0, -1));
                    CheckEntrance(root, x, -3);
                }
                Prop(root, Reception, "Motel.Desk", new Vector3(-7, 0, -1.4f), 90, new Vector2(2.2f, 2.5f));
                RequirePropsInside(root, new Bounds(new Vector3(0, 1.5f, 0), new Vector3(17.7f, 3, 5.7f)));
                // Reuse the assembled City Hotel for frontage, not an invented wall proxy.
                // Its unverified/possibly opaque exterior stays behind the accessible room annex.
                var exterior = Place(root, HotelPrefabPath, "Motel.HotelExterior", Vector3.zero, 0, false);
                var bounds = GeometryBounds(exterior);
                exterior.transform.localPosition += new Vector3(0, 0, 5 - bounds.min.z);
                Anchor(root, "Motel.FrontCourt", new Vector3(0, 0, -5));
            }

            private void RoomShell(GameObject root, int columns, int rows, int[] entrances)
            {
                Shell(root, RoomFloor, RoomFloor, RoomWall, 2, 2, columns, rows, 2, 3, 1,
                    (column, north) => north ? (column % 2 == 0 ? RoomWindow : RoomWall) :
                        entrances.Contains(column) ? null : RoomWall, true);
                foreach (int column in entrances)
                {
                    Anchor(root, "Entrance.South." + column, new Vector3((column + .5f - columns * .5f) * 2, 0, -rows));
                    // The vendor doorway is only 1.742 x 2.072m, not a 2 x 3m wall.
                    // Keep an accessible 2m portal and close its top with native half-meter blocks.
                    for (int block = 0; block < 4; block++)
                        Wall(root, RoomHeader, new Vector3((column + .5f - columns * .5f) * 2 + (block - 1.5f) * .5f,
                            2.5f, -rows), 0, .5f, .5f);
                }
                for (int column = 1; column < columns; column += 3)
                    Light(root, new Vector3((column + .5f - columns * .5f) * 2, 2.6f, 0), 8);
            }
            public void Restroom(GameObject root)
            {
                RoomShell(root, 2, 2, new[] { 0 });
                Prop(root, Toilet, "Restroom.Toilet", new Vector3(.6f, 0, .7f), 180, new Vector2(1.5f, 1.5f));
                Prop(root, Sink, "Restroom.Sink", new Vector3(1.1f, 0, -.6f), 270, new Vector2(1.5f, 1.5f));
                CheckEntrance(root, -1, -2);
                RequirePropsInside(root, new Bounds(new Vector3(0, 1.5f, 0), new Vector3(3.8f, 3, 3.8f)));
            }

            private void Shell(GameObject root, string floor, string ceiling, string wall, float tileX, float tileZ,
                int columns, int rows, float wallSpan, float wallHeight, int courses,
                Func<int, bool, string> endWall, bool reverseCeiling = false)
            {
                float width = columns * tileX, depth = rows * tileZ, height = courses * wallHeight;
                for (int x = 0; x < columns; x++)
                    for (int z = 0; z < rows; z++)
                    {
                        var position = new Vector3((x + .5f) * tileX - width * .5f, 0, (z + .5f) * tileZ - depth * .5f);
                        Slab(root, floor, "Floor", position, tileX, tileZ, true, false);
                        Slab(root, ceiling, "Roof", position + Vector3.up * height, tileX, tileZ, false, reverseCeiling);
                    }
                int sideCount = Mathf.RoundToInt(depth / wallSpan);
                if (Mathf.Abs(sideCount * wallSpan - depth) > .01f || Mathf.Abs(tileX - wallSpan) > .01f)
                    throw new InvalidOperationException("Shell grid does not match native wall module spans.");
                for (int course = 0; course < courses; course++)
                {
                    for (int z = 0; z < sideCount; z++)
                    {
                        float center = (z + .5f) * wallSpan - depth * .5f;
                        Wall(root, wall, new Vector3(-width * .5f, course * wallHeight, center), 90, wallSpan, wallHeight);
                        Wall(root, wall, new Vector3(width * .5f, course * wallHeight, center), 270, wallSpan, wallHeight);
                    }
                    for (int x = 0; x < columns; x++)
                        foreach (bool north in new[] { false, true })
                        {
                            string path = endWall(x, north); if (path == null) continue;
                            Wall(root, path, new Vector3((x + .5f) * wallSpan - width * .5f,
                                course * wallHeight, (north ? 1 : -1) * depth * .5f), north ? 180 : 0, wallSpan, wallHeight);
                        }
                }
            }

            private void Wall(GameObject root, string path, Vector3 position, float yaw, float span, float height)
            {
                var go = Place(root, path, "Shell.Wall", Vector3.zero, 0, false);
                var b = GeometryBounds(go);
                if (b.size.z > b.size.x) go.transform.localRotation = Quaternion.Euler(0, 90, 0) * go.transform.localRotation;
                b = GeometryBounds(go); RequireSpan(path, b.size.x, span); RequireSpan(path, b.size.y, height);
                go.transform.localRotation = Quaternion.Euler(0, yaw, 0) * go.transform.localRotation;
                Align(go, position, false);
            }

            private void Slab(GameObject root, string path, string name, Vector3 position, float width, float depth, bool top, bool reverse)
            {
                var go = Place(root, path, "Shell." + name, Vector3.zero, 0, false);
                var b = GeometryBounds(go);
                if (Mathf.Abs(b.size.x - depth) < .03f && Mathf.Abs(b.size.z - width) < .03f && width != depth)
                    go.transform.localRotation = Quaternion.Euler(0, 90, 0) * go.transform.localRotation;
                if (reverse) go.transform.localRotation = Quaternion.Euler(180, 0, 0) * go.transform.localRotation;
                b = GeometryBounds(go); RequireSpan(path, b.size.x, width); RequireSpan(path, b.size.z, depth);
                Align(go, position, top);
            }

            private GameObject Prop(GameObject root, string path, string name, Vector3 position, float yaw, Vector2 space)
            {
                var go = Place(root, path, name, position, yaw, false);
                var b = GeometryBounds(go);
                if (b.size.x > space.x || b.size.z > space.y)
                    throw new InvalidOperationException("Native furnishing does not fit assigned space; do not rescale: " + path + " size=" + b.size);
                return go;
            }

            private GameObject Place(GameObject root, string path, string name, Vector3 position, float yaw, bool top)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(modules[path], scene);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                go.name = name;
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0, yaw, 0) * go.transform.localRotation;
                Align(go, position, top);
                // Collision follows real visible mesh topology, including actual doorway holes.
                // Vendor broad box colliders must not turn an open frame into an invisible wall.
                foreach (var collider in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                foreach (var body in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
                foreach (var filter in CollisionMeshes(go))
                {
                    var collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh; collider.convex = false;
                }
                return go;
            }

            private static void Align(GameObject go, Vector3 position, bool top)
            {
                var bounds = GeometryBounds(go);
                go.transform.localPosition += new Vector3(position.x - bounds.center.x,
                    position.y - (top ? bounds.max.y : bounds.min.y), position.z - bounds.center.z);
            }

            private static void CheckEntrance(GameObject root, float x, float z)
            {
                // Verify a 1.1m pedestrian opening up to 2.1m high against mesh-shaped collision,
                // rather than a bounds proxy spanning the empty center of a doorway frame.
                Physics.SyncTransforms();
                foreach (float offset in new[] { -.55f, -.275f, 0, .275f, .55f })
                    foreach (float y in new[] { .15f, .8f, 1.5f, 2.1f })
                    {
                        var ray = new Ray(new Vector3(x + offset, y, z - .6f), Vector3.forward);
                        foreach (var collider in root.GetComponentsInChildren<MeshCollider>())
                            if (collider.Raycast(ray, out _, 1.2f))
                                throw new InvalidOperationException("Blocked native doorway in " + root.name + ": " + collider.name);
                    }
            }
        }

        private static IEnumerable<MeshFilter> CollisionMeshes(GameObject root)
        {
            var lower = new HashSet<Renderer>(); var highest = new HashSet<Renderer>();
            foreach (var group in root.GetComponentsInChildren<LODGroup>(true))
            {
                var levels = group.GetLODs();
                if (levels.Length == 0) continue;
                foreach (var renderer in levels[0].renderers) highest.Add(renderer);
                foreach (var level in levels.Skip(1)) foreach (var renderer in level.renderers) lower.Add(renderer);
            }
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (filter.sharedMesh != null && renderer != null && renderer.enabled &&
                    AuthoredActive(filter.transform, root.transform) && (!lower.Contains(renderer) || highest.Contains(renderer))) yield return filter;
            }
        }

        private static bool AuthoredActive(Transform transform, Transform root)
        {
            // Prefab assets are not scene instances: activeInHierarchy is not their authored state.
            for (var current = transform; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf) return false;
                if (current == root) return true;
            }
            return false;
        }

        private static Bounds GeometryBounds(GameObject root)
        {
            bool found = false; Bounds result = default;
            foreach (var filter in CollisionMeshes(root))
            {
                var bounds = MeshBounds(filter);
                if (!found) { result = bounds; found = true; } else result.Encapsulate(bounds);
            }
            if (!found) throw new InvalidOperationException("Inspected art has no active static mesh geometry: " + root.name);
            return result;
        }

        private static Bounds MeshBounds(MeshFilter filter)
        {
            var bounds = filter.sharedMesh.bounds; var matrix = filter.transform.localToWorldMatrix;
            var result = new Bounds(matrix.MultiplyPoint3x4(bounds.min), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
                result.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z)));
            return result;
        }

        private static void RequireClearance(GameObject root, Bounds space)
        {
            if (space.size.x < 3.5f || space.size.y < 4.6f || (space.size.z < 16 && root.name.EndsWith("Garage", StringComparison.Ordinal)))
                throw new InvalidOperationException("Insufficient truck clearance: " + root.name);
            foreach (var filter in CollisionMeshes(root))
                if (MeshBounds(filter).Intersects(space))
                    throw new InvalidOperationException("Geometry intrudes into the truck clearance volume: " + root.name + "/" + filter.name);
        }

        private static void RequirePropsInside(GameObject root, Bounds room)
        {
            foreach (Transform child in root.transform)
            {
                if (child.name.StartsWith("Shell.", StringComparison.Ordinal) || child.GetComponentsInChildren<MeshFilter>(true).Length == 0) continue;
                var b = GeometryBounds(child.gameObject);
                if (b.min.x < room.min.x || b.max.x > room.max.x || b.min.z < room.min.z || b.max.z > room.max.z || b.max.y > room.max.y)
                    throw new InvalidOperationException("Native furnishing leaves accessible room: " + child.name);
            }
        }

        private static void RequireSpan(string path, float actual, float expected)
        {
            if (!float.IsFinite(actual) || Mathf.Abs(actual - expected) > .03f)
                throw new InvalidOperationException("Native module span differs from the authored grid; review without scaling: " + path +
                    " expected=" + expected + " measured=" + actual);
        }

        private static void ValidateMaterials(GameObject prefab, string sourcePath)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || ShaderUtil.ShaderHasError(material.shader) ||
                        !(material.GetTag("RenderPipeline", false, "") == "UniversalPipeline" ||
                          material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) ||
                          material.FindPass("UniversalForward") >= 0 || material.FindPass("UniversalForwardOnly") >= 0 || material.FindPass("SRPDefaultUnlit") >= 0))
                        throw new InvalidOperationException("Review/convert materials on a project-owned prefab copy before assembly: " + sourcePath);
        }

        private static void Anchor(GameObject root, string name, Vector3 position)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false); go.transform.localPosition = position;
        }

        private static void Light(GameObject root, Vector3 position, float range)
        {
            var go = new GameObject("Interior Light"); go.transform.SetParent(root.transform, false); go.transform.localPosition = position;
            var light = go.AddComponent<Light>(); light.type = LightType.Point; light.range = range; light.intensity = 2;
            light.color = new Color(1, .96f, .88f); light.shadows = LightShadows.None;
        }

        private static string ValidateFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A dedicated project-owned asset folder is required.");
            path = path.Replace('\\', '/').TrimEnd('/');
            if (!path.StartsWith("Assets/LWS/TruckTaxi/", StringComparison.Ordinal) || path.Split('/').Any(p =>
                p.Length == 0 || p == "." || p == ".." || p.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0))
                throw new ArgumentException("Output must be a dedicated folder beneath Assets/LWS/TruckTaxi.");
            return path;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }
    }
}
