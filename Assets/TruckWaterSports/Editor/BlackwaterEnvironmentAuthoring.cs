using System;
using System.Collections.Generic;
using LWS.TruckKeepaway.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckWaterSports.Editor
{
    /// <summary>
    /// Explicit, additive saved-scene dressing. Uses installed commercial dock-building
    /// modules/cargo/buildings and Fishing's boulders; never rebuilds water or gameplay.
    /// Abandoned craft are installed separately through BlackwaterAbandonedCraftAuthoring using owned Fantastic Village rowboats.
    /// </summary>
    public static class BlackwaterEnvironmentAuthoring
    {
        const string ScenePath = "Assets/TruckWaterSports/Scenes/Blackwater.unity";
        const string RootName = "Owned Blackwater shoreline facilities and submerged shelves";
        const string Folder = "Assets/TruckWaterSports/BlackwaterEnvironment";
        const string Deck = "Assets/ScaffoldsPack/Mesh/SM_RollingScaffoldsTop.fbx";
        const string Support = "Assets/ScaffoldsPack/Mesh/SM_RollingScaffoldMid.fbx";
        const string Warehouse = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_Warehouse_01.prefab";
        const string Crates = "Assets/ConstructionPropsBundle/URP/Prefabs/SM_BoxesPack01 Variant.prefab";
        const string Barrel = "Assets/ConstructionPropsBundle/URP/Prefabs/SM_Barrel01 Variant.prefab";
        const string Pallet = "Assets/ConstructionPropsBundle/URP/Prefabs/SM_Pallet Variant.prefab";
        const string Boulder = "Assets/TruckFishing/Prefabs/River boulder 0.prefab";
        const string MudLayer = "Assets/3D Haven/Path Painter II/Editor/Demo/Terrains/DocsTerrain/TerrainLayers/stdMudRocky.terrainlayer";
        const string SourceTerrain = "Assets/RiverModeler/Demo/River Modeler Demo Terrain.asset";
        const string Contact = "Assets/TruckFishing/Materials/Dock contact.asset";
        const float SafeLane = 55;

        readonly struct Lane
        {
            public readonly Vector2 a, b;
            public Lane(Vector3 from, Vector3 to) { a = new Vector2(from.x, from.z); b = new Vector2(to.x, to.z); }
            public float Distance(Vector2 p)
            {
                var delta = b - a;
                float t = delta.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot(p - a, delta) / delta.sqrMagnitude) : 0;
                return Vector2.Distance(p, a + delta * t);
            }
        }

        [MenuItem("Truck Sports/Alpha 0.10/Add owned Blackwater facilities and submerged shelves")]
        public static void UpgradeActiveVenue() => InstallActiveVenue();

        /// <summary>Call only with the saved Blackwater venue open and stopped. Caller saves.</summary>
        public static GameObject InstallActiveVenue()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring Blackwater facilities.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the existing saved Blackwater scene.");
            BlackwaterMatch match = null; Renderer water = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == RootName) { Debug.Log("BLACKWATER_ENVIRONMENT_PRESERVED"); return child.gameObject; }
                var candidate = root.GetComponentInChildren<BlackwaterMatch>(true); if (candidate != null) match = candidate;
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.name == "Owned River Modeler shaded flowing surface") water = renderer;
            }
            if (match == null || match.course == null || match.course.stations == null || match.course.stations.Length < 2 || match.dock == null || water == null)
                throw new InvalidOperationException("Preserve the original Blackwater match, lake, course and delivery dock.");
            foreach (string asset in new[] { Deck, Support, Warehouse, Crates, Barrel, Pallet, Boulder }) Required<GameObject>(asset);
            var source = Required<TerrainData>(SourceTerrain); var mud = Required<TerrainLayer>(MudLayer);
            if (mud.diffuseTexture == null) throw new InvalidOperationException("Owned rocky mud diffuse texture required.");
            var contact = Required<PhysicsMaterial>(Contact);
            var lanes = DescribeLanes(match);
            EnsureFolder(Folder + "/Materials"); EnsureFolder(Folder + "/Meshes");
            // Existing model-conversion helper writes only this owned material directory.
            EnsureFolder("Assets/TruckWaterSports/Materials");
            string meshPath = Folder + "/Meshes/Peripheral submerged lake shelves.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
                throw new InvalidOperationException("Shelf asset exists without its scene root; review it before retrying: " + meshPath);
            var shelfMaterial = MudMaterial(mud);
            var environment = new GameObject(RootName); environment.SetActive(false);
            try
            {
                float west = water.bounds.min.x + 22, east = water.bounds.max.x - 22;
                // Wide exclusion includes both normal buoy navigation and direct crate-return paths.
                Platform(environment.transform, new Vector3(west, .35f, 70), -1, lanes, contact);
                Platform(environment.transform, new Vector3(east, .35f, 280), 1, lanes, contact);
                Facility(environment.transform, scene, new Vector3(water.bounds.min.x - 70, 0, 70), 90, lanes, contact);
                Facility(environment.transform, scene, new Vector3(water.bounds.max.x + 70, 0, 280), -90, lanes, contact);
                AddShelves(environment.transform, water.bounds, source, lanes, shelfMaterial, contact, meshPath);
                for (int i = 0; i < 8; i++)
                {
                    float x = i % 2 == 0 ? west + 4 : east - 4, z = 25 + i * 43;
                    if (Clear(lanes, new Vector3(x, 0, z), 9))
                        Prop(environment.transform, "Submerged owned rock outcrop " + i, Boulder, new Vector3(x, -5.75f, z), new Vector3(10, 4.2f, 12), i * 47, lanes, contact);
                }
                environment.SetActive(true); Undo.RegisterCreatedObjectUndo(environment, "Add owned Blackwater shoreline facilities");
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("BLACKWATER_ENVIRONMENT_ADDED: 2 modular supply platforms, 2 shoreline freight facilities, submerged textured shelves and owned rocks. Navigation/objectives/forces unchanged. Abandoned craft use the separate explicit owned-rowboat installer. Caller must save scene/assets and inspect rendering.");
                return environment;
            }
            catch { UnityEngine.Object.DestroyImmediate(environment); throw; }
        }

        static List<Lane> DescribeLanes(BlackwaterMatch match)
        {
            var result = new List<Lane>(); var points = match.course.stations;
            for (int i = 0; i < points.Length - 1; i++) result.Add(new Lane(points[i], points[i + 1]));
            if (match.supplies != null) foreach (var supply in match.supplies)
                if (supply != null) result.Add(new Lane(match.dock.position, supply.position));
            // Also protect direct shelter approaches to every original buoy station.
            foreach (var station in points) result.Add(new Lane(match.dock.position, station));
            return result;
        }
        static bool Clear(List<Lane> lanes, Vector3 p, float radius)
        {
            var flat = new Vector2(p.x, p.z);
            foreach (var lane in lanes) if (lane.Distance(flat) < SafeLane + radius) return false;
            return true;
        }
        static void Platform(Transform parent, Vector3 center, int shoreSide, List<Lane> lanes, PhysicsMaterial contact)
        {
            var root = new GameObject(shoreSide < 0 ? "Abandoned west freight landing" : "East supply transfer landing").transform;
            root.SetParent(parent, false);
            for (int x = 0; x < 4; x++) for (int z = 0; z < 3; z++)
            {
                var p = center + new Vector3((x - 1.5f) * 5, 0, (z - 1) * 4);
                Prop(root, "Production grated deck module " + x + "-" + z, Deck, p, new Vector3(5, 12, 4), 0, lanes, contact);
            }
            for (int i = 0; i < 7; i++)
                Prop(root, "Shore access deck " + i, Deck, center + new Vector3(shoreSide * (12.5f + i * 4), 0, 0), new Vector3(4, 12, 5), 90, lanes, contact);
            // These are the real steel support models, not invisible collision pillars.
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
                for (int level = 0; level < 2; level++)
                    Prop(root, "Submerged frame support " + x + "-" + z + "-" + level, Support, center + new Vector3(x * 7.5f, -6 + level * 3, z * 4), new Vector3(2, 3, 2), 0, lanes, contact);
            // Sample the installed module's actual deck triangles, not its railing bounds.
            // This keeps cargo resting on the deck even when the production model changes.
            var cargoPoint = center + new Vector3(-2.5f, 0, 0); cargoPoint.y = TopAt(root, cargoPoint);
            var pallet = Prop(root, "Supply pallet", Pallet, cargoPoint, new Vector3(2.3f, .8f, 2), 15, lanes, contact);
            cargoPoint.y = TopAt(pallet.transform, cargoPoint);
            Prop(root, "Crated supplies", Crates, cargoPoint, new Vector3(2.1f, 2, 1.8f), 15, lanes, contact);
            var drumPoint = center + new Vector3(7.5f, 0, 0); drumPoint.y = TopAt(root, drumPoint);
            Prop(root, "Stored oil drum", Barrel, drumPoint, new Vector3(1.1f, 1.7f, 1.1f), 0, lanes, contact);
            // Amber does not qualify as an original green healing/navigation shelter.
            var lamp = new GameObject("Warm freight landing work light").AddComponent<Light>();
            lamp.transform.SetParent(root, false); lamp.transform.position = center + Vector3.up * 5;
            lamp.type = LightType.Point; lamp.color = new Color(1, .5f, .12f); lamp.intensity = 1.3f; lamp.range = 18; lamp.shadows = LightShadows.Soft;
        }
        static void Facility(Transform parent, Scene scene, Vector3 p, float yaw, List<Lane> lanes, PhysicsMaterial contact)
        {
            p.y = GroundHeight(scene, p);
            Prop(parent, "Shoreline freight warehouse", Warehouse, p, new Vector3(24, 14, 26), yaw, lanes, contact);
            var crates = p + new Vector3(0, 0, 20); crates.y = GroundHeight(scene, crates);
            var pallets = p + new Vector3(8, 0, 19); pallets.y = GroundHeight(scene, pallets);
            Prop(parent, "Freight yard cargo", Crates, crates, new Vector3(6, 4, 5), yaw + 12, lanes, contact);
            Prop(parent, "Freight yard pallets", Pallet, pallets, new Vector3(3, 1, 3), yaw, lanes, contact);
        }
        static GameObject Prop(Transform parent, string name, string path, Vector3 p, Vector3 size, float yaw, List<Lane> lanes, PhysicsMaterial contact)
        {
            // Preflight and then check actual imported renderer extents before adding collision.
            float plannedRadius = new Vector2(size.x, size.z).magnitude * .5f;
            if (!Clear(lanes, p, plannedRadius)) throw new InvalidOperationException("Prop would enter the protected Blackwater navigation corridor: " + name);
            var holder = new GameObject(name); holder.transform.SetParent(parent, false);
            KeepawayPresentationAuthoring.Fit(holder.transform, path, size, 0, false, "TruckWaterSports");
            holder.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
            var renderers = holder.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (!Clear(lanes, bounds.center, new Vector2(bounds.extents.x, bounds.extents.z).magnitude))
                throw new InvalidOperationException("Imported prop bounds enter the protected Blackwater navigation corridor: " + name);
            foreach (var filter in holder.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null && filter.GetComponent<Renderer>() != null)
                {
                    var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; collider.sharedMaterial = contact;
                    GameObjectUtility.SetStaticEditorFlags(filter.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
                }
            return holder;
        }

        static void AddShelves(Transform parent, Bounds lake, TerrainData source, List<Lane> lanes, Material material, PhysicsMaterial contact, string path)
        {
            const int nx = 61, nz = 83;
            var vertices = new Vector3[nx * nz]; var uv = new Vector2[vertices.Length]; var triangles = new List<int>();
            float dx = lake.size.x / (nx - 1), dz = lake.size.z / (nz - 1), triangleRadius = Mathf.Sqrt(dx * dx + dz * dz);
            for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++)
            {
                int k = z * nx + x; float u = x / (float)(nx - 1), v = z / (float)(nz - 1);
                float wx = Mathf.Lerp(lake.min.x, lake.max.x, u), wz = Mathf.Lerp(lake.min.z, lake.max.z, v);
                // Existing landscape's lake bowl is at -6m. These visible shelves sit above
                // that bowl, using the installed height field instead of an invisible flat floor.
                float relief = source.GetInterpolatedHeight(u, v) / Mathf.Max(1, source.size.y);
                vertices[k] = new Vector3(wx, lake.center.y - 5.65f + relief * 1.1f + Mathf.PerlinNoise(wx * .035f, wz * .03f) * .55f, wz);
                uv[k] = new Vector2(wx, wz) / 5;
                if (x < nx - 1 && z < nz - 1 && Clear(lanes, vertices[k], triangleRadius))
                { triangles.Add(k); triangles.Add(k + nx); triangles.Add(k + 1); triangles.Add(k + 1); triangles.Add(k + nx); triangles.Add(k + nx + 1); }
            }
            if (triangles.Count == 0) throw new InvalidOperationException("No peripheral lakebed area remains clear of the protected navigation lanes.");
            var mesh = new Mesh { name = "Course-safe submerged rocky mud shelves" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            var shelf = new GameObject("Textured submerged lakebed shelves outside navigation"); shelf.transform.SetParent(parent, false);
            shelf.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = shelf.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            var shape = shelf.AddComponent<MeshCollider>(); shape.sharedMesh = mesh; shape.sharedMaterial = contact;
        }
        static Material MudMaterial(TerrainLayer source)
        {
            string path = Folder + "/Materials/Submerged rocky lakebed.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("Existing URP Lit shader required.");
            material = new Material(shader) { name = "Owned submerged rocky lakebed", color = new Color(.38f, .42f, .35f), mainTexture = source.diffuseTexture };
            material.SetFloat("_Smoothness", .45f);
            if (source.normalMapTexture != null) { material.SetTexture("_BumpMap", source.normalMapTexture); material.EnableKeyword("_NORMALMAP"); }
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static float GroundHeight(Scene scene, Vector3 point)
        {
            foreach (var root in scene.GetRootGameObjects()) foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
            {
                var origin = terrain.transform.position; var size = terrain.terrainData.size;
                if (point.x >= origin.x && point.x <= origin.x + size.x && point.z >= origin.z && point.z <= origin.z + size.z)
                    return terrain.SampleHeight(point) + origin.y;
            }
            throw new InvalidOperationException("The owned surrounding landscape must exist before adding shoreline facilities.");
        }
        static float TopAt(Transform root, Vector3 point)
        {
            float highest = float.NegativeInfinity;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh; if (mesh == null) continue;
                // Editor-only public API reads the original import without enabling vendor
                // Read/Write or retaining CPU mesh copies in the Windows build.
                Vector3[] vertices; var triangles = new List<int>();
                using (var dataArray = MeshUtility.AcquireReadOnlyMeshData(mesh))
                {
                    var data = dataArray[0];
                    using (var positions = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp))
                    { data.GetVertices(positions); vertices = positions.ToArray(); }
                    for (int sub = 0; sub < data.subMeshCount; sub++)
                    {
                        var descriptor = data.GetSubMesh(sub); if (descriptor.topology != MeshTopology.Triangles) continue;
                        using (var indices = new NativeArray<int>(descriptor.indexCount, Allocator.Temp))
                        { data.GetIndices(indices, sub, true); foreach (int index in indices) triangles.Add(index); }
                    }
                }
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    var a = filter.transform.TransformPoint(vertices[triangles[i]]);
                    var b = filter.transform.TransformPoint(vertices[triangles[i + 1]]);
                    var c = filter.transform.TransformPoint(vertices[triangles[i + 2]]);
                    var normal = Vector3.Cross(b - a, c - a);
                    if (Mathf.Abs(normal.y) < normal.magnitude * .6f) continue;
                    float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                    if (Mathf.Abs(denominator) < .00001f) continue;
                    float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.z - c.z)) / denominator;
                    float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.z - c.z)) / denominator;
                    if (u >= -.001f && v >= -.001f && u + v <= 1.001f)
                        highest = Mathf.Max(highest, a.y * u + b.y * v + c.y * (1 - u - v));
                }
            }
            if (float.IsNegativeInfinity(highest)) throw new InvalidOperationException("No actual deck surface beneath intended cargo position: " + point);
            return highest;
        }
        static T Required<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Installed owned dependency missing: " + path);
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/'); string parent = path.Substring(0, split); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
