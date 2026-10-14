using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LWS.TruckTaxi.Editor
{
    public static partial class TruckTaxiEdenboroughAuthoring
    {
        public const string Folder = "Assets/LWS/TruckTaxi/Edenborough";
        public const string PalettePath = Folder + "/ReviewedArtPalette.asset";
        public const string BuildPath = "Builds/TruckTaxiEdenborough/TruckTaxi.exe";

        [MenuItem("Truck Taxi/Edenborough/Create Reviewed Art Palette")]
        public static void CreatePalette()
        {
            EnsureEditMode(); Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            if (AssetDatabase.LoadMainAssetAtPath(PalettePath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TruckTaxiEdenboroughPalette>(), PalettePath);
            AssetDatabase.SaveAssets(); Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(PalettePath);
        }

        [MenuItem("Truck Taxi/Edenborough/Build Fresh Dedicated Scene")]
        public static void BuildFreshScene()
        {
            EnsureEditMode();
            EnsureNoLoadedRoadNetwork();
            if (File.Exists(TruckTaxiEdenboroughLayout.ScenePath))
                throw new InvalidOperationException("The dedicated scene already exists. Preserve designer edits; this command never replaces it.");
            var palette = AssetDatabase.LoadAssetAtPath<TruckTaxiEdenboroughPalette>(PalettePath);
            ValidatePalette(palette);
            Directory.CreateDirectory(Folder + "/RoadMeshes"); AssetDatabase.Refresh();
            var source = EditorSceneManager.OpenPreviewScene(TruckTaxiDemoBuilder.ScenePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
            try
            {
                TruckTaxiDemoBuilder.BuildLighting();
                // Existing Weather Maker presents the sky; an inherited Unity skybox suppresses it.
                RenderSettings.skybox = null;
                var template = Find<TruckTaxiBootstrap>(source);
                var host = Object.Instantiate(template.gameObject).GetComponent<TruckTaxiBootstrap>();
                SceneManager.MoveGameObjectToScene(host.gameObject, scene); host.name = "Edenborough Truck Taxi Bootstrap";
                var core = Object.Instantiate(Find<LwsApplicationBootstrap>(source).gameObject);
                SceneManager.MoveGameObjectToScene(core, scene);
                Set(core.GetComponent<LwsApplicationBootstrap>(), "dontDestroyOnLoad", false);
                Set(core.GetComponent<LwsApplicationBootstrap>(), "drivingSandbox", true);
                host.spawner = Object.Instantiate(template.spawner.gameObject).GetComponent<LwsPlayerTruckSpawner>();
                SceneManager.MoveGameObjectToScene(host.spawner.gameObject, scene);
                host.playerSpawn = New("Edenborough Tractor Spawn", null, TruckTaxiEdenboroughLayout.Spawn).transform;
                host.playerSpawn.rotation = Quaternion.LookRotation(TruckTaxiEdenboroughLayout.Nodes["west"] - TruckTaxiEdenboroughLayout.Nodes["spawn"], Vector3.up);
                Set(host.spawner, "truckSpawnPoint", host.playerSpawn); Set(host.spawner, "spawnOnStart", false);
                Set(host.spawner, "buildValidationRoadside", false);
                host.configuration = SaveCopy(template.configuration, Folder + "/Configuration.asset");
                ConfigureCanonicalTruck(host);
                host.roadGraph = New("Edenborough LWS Road Graph", null, Vector3.zero).AddComponent<LwsRoadGraphProvider>();
                host.roadGraph.SetGraph(TruckTaxiEdenboroughRoadAuthoring.CreateGraph(), false);
                var region = host.GetComponent<TruckTaxiRegionalWorld>() ?? host.gameObject.AddComponent<TruckTaxiRegionalWorld>();
                region.authoredSingleScene = true; region.authoredWorldBounds = TruckTaxiEdenboroughLayout.WorldBounds;
                region.streamingManifest = null;
                region.regions = TruckTaxiEdenboroughLayout.Districts.Select(d => new TruckTaxiRegion {
                    id = d.Id, displayName = d.Name, sceneName = TruckTaxiEdenboroughLayout.SceneName,
                    bounds = new Bounds(new Vector3(d.Center.x, 180, d.Center.z), new Vector3(d.Size.x, 600, d.Size.y)) }).ToArray();
                host.traffic = New("Edenborough UTS Traffic", null, Vector3.zero).AddComponent<TruckTaxiTrafficAdapter>();
                EditorUtility.CopySerialized(template.traffic, host.traffic);
                host.traffic.useTaxiBoulevardLanes = false;
                host.traffic.cityLanes = CreateTrafficLoops();
                host.traffic.maxFullTraffic = 24; host.traffic.maxVisibleTraffic = 40;
                host.traffic.maximumDedicatedVehicles = 4;
                host.traffic.densityProfile = SaveCopy(template.traffic.densityProfile, Folder + "/Population.asset");
                host.traffic.densityProfile.spawnRadius = host.traffic.densityProfile.despawnRadius = 0;
                host.traffic.densityProfile.maximumActiveTraffic = 150;
                host.traffic.densityProfile.maximumActivePedestrians = 720;
                EditorUtility.SetDirty(host.traffic.densityProfile);
                host.pedestrians = CreatePedestrians(template.pedestrians, host.traffic.cityLanes, host.traffic.densityProfile);

                var world = New("Authored Edenborough World", null, Vector3.zero).transform;
                var asphalt = RoadMaterial();
                var stops = CreateDestinations(world, palette, asphalt);
                var garage = CreateGarage(world, palette, asphalt, out var volume);
                var pads = stops.Select(FacilityPad).ToList();
                pads.Add(new Bounds(TruckTaxiEdenboroughLayout.Garage - Vector3.up * .06f, new Vector3(40, 2, 50)));
                TruckTaxiEdenboroughTerrainAuthoring.Build(world, Folder + "/Terrain", pads);
                TruckTaxiEdenboroughRoadAuthoring.Bake(world, Folder + "/RoadMeshes", asphalt);
                Physics.SyncTransforms();
                var bakedRoads = world.GetComponentsInChildren<TruckTaxiSurface>(true).Where(s => s.isRoad)
                    .Select(s => s.GetComponent<Collider>()).Where(c => c != null).ToArray();
                TruckTaxiEdenboroughTerrainAuthoring.Build(world, Folder + "/Terrain", pads, bakedRoads);
                CreateDressing(world, host.roadGraph, stops, volume, palette);
                var binding = host.GetComponent<TruckTaxiEdenboroughSceneBinding>() ?? host.gameObject.AddComponent<TruckTaxiEdenboroughSceneBinding>();
                binding.host = host; binding.palette = palette; binding.garage = garage; binding.garageVolume = volume;
                RemoveStaleSceneReferences(host, source);
                EditorSceneManager.SaveScene(scene, TruckTaxiEdenboroughLayout.ScenePath);
                host.mapTiles = TruckTaxiMapTileBaker.BakeEdenborough(scene, Folder + "/MapTiles");
                EditorSceneManager.SaveScene(scene);
                var builds = EditorBuildSettings.scenes.ToList();
                if (!builds.Any(s => s.path == TruckTaxiEdenboroughLayout.ScenePath))
                    builds.Add(new EditorBuildSettingsScene(TruckTaxiEdenboroughLayout.ScenePath, true));
                EditorBuildSettings.scenes = builds.ToArray(); AssetDatabase.SaveAssets();
                Debug.Log("EDENBOROUGH AUTHORED. New scene only; inspect road junctions, facilities, truck fit and performance before acceptance.");
            }
            catch
            {
                if (scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                throw;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(source);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        private static void ValidatePalette(TruckTaxiEdenboroughPalette palette)
        {
            if (palette == null || !palette.HasCoreContent || string.IsNullOrWhiteSpace(palette.inspectionManifest))
                throw new InvalidOperationException("Assign inspected art, paint materials and exact-path inspection manifest before authoring. No placeholder world is substituted.");
            foreach (var entry in new[] { palette.trees, palette.roadside, palette.industrial, palette.construction,
                palette.urbanBuildings, palette.residentialBuildings, palette.warehouseBuildings, palette.dinerModules,
                palette.motelModules, palette.storeModules, palette.garageModules, palette.bathroomFixtures, palette.pumps, palette.signs,
                palette.dinerSeats, palette.motelBeds, palette.shopCounters, palette.shopProducts })
            {
                if (entry == null || entry.Length == 0) throw new InvalidOperationException("Every first-wave palette category must contain reviewed usable art.");
                foreach (var asset in entry)
                    if (asset == null || !EditorUtility.IsPersistent(asset) || !TruckTaxiDressingProfile.TryPrefabBounds(asset, out _))
                        throw new InvalidOperationException("Palette contains a missing, scene-only or zero-bounds art reference.");
            }
        }
        private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        private static void EnsureEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scene edits before authoring.");
        }
        private static void EnsureNoLoadedRoadNetwork()
        {
            // ERRoadNetwork's default constructor can reuse a loaded network. This one-time
            // guard prevents an explicit new-world bake from touching another authored network.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                        if (component != null && component.GetType().FullName == "EasyRoads3Dv3.ERModularBase")
                            throw new InvalidOperationException("Close the scene containing the existing EasyRoads network before authoring Edenborough. The network will not be reused or modified.");
        }
        private static GameObject New(string name, Transform parent, Vector3 position)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position; return go; }
        private static void Set(Component component, string field, Object value)
        { var so = new SerializedObject(component); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Component component, string field, bool value)
        { var so = new SerializedObject(component); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static T SaveCopy<T>(T source, string path) where T : Object
        {
            if (source == null) throw new InvalidOperationException("Configured source asset missing: " + path);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path); if (existing != null) return existing;
            var copy = Object.Instantiate(source); AssetDatabase.CreateAsset(copy, path); return copy;
        }
        private static Material RoadMaterial()
        {
            string path = Folder + "/Asphalt.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.15f,.17f,.18f) };
            material.enableInstancing = true; AssetDatabase.CreateAsset(material, path); return material;
        }
        private static Bounds FacilityPad(TruckTaxiRideLocation stop)
        {
            var bounds = new Bounds(stop.StopPosition, new Vector3(18, 2, 34));
            foreach (var renderer in stop.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
            var center = bounds.center; center.y = stop.StopPosition.y - .12f;
            return new Bounds(center, new Vector3(bounds.size.x + 6, 2, bounds.size.z + 6));
        }
        private static void ConfigureCanonicalTruck(TruckTaxiBootstrap host)
        {
            var data = new SerializedObject(host.spawner);
            var existing = data.FindProperty("truckDefinition").objectReferenceValue as LwsTruckDefinition;
            var prefab = existing != null ? existing.PlayerPrefab : data.FindProperty("fallbackPlayerTruckPrefab").objectReferenceValue as GameObject;
            if (prefab == null) throw new InvalidOperationException("Existing Taxi canonical prefab is missing.");
            var baseDefinition = existing ?? AssetDatabase.LoadAssetAtPath<LwsTruckDefinition>("Assets/LWS/InterstateHauler/Vehicles/Data/IH_TruckDefinition_StarterNwhSemi.asset");
            var definition = SaveCopy(baseDefinition, Folder + "/CanonicalTaxiTruck.asset");
            var identity = new SerializedObject(definition);
            identity.FindProperty("stableId").stringValue = existing != null ? existing.StableId : "ih.truck.nwh.euro.semi";
            identity.FindProperty("playerPrefab").objectReferenceValue = prefab;
            identity.FindProperty("validationTrailerPrefab").objectReferenceValue = null;
            identity.ApplyModifiedPropertiesWithoutUndo();
            Set(host.spawner, "truckDefinition", definition);
            Set(host.spawner, "trailerPrefab", null);
            var sourceCatalog = host.configuration.careerCatalog ?? Resources.Load<TruckTaxiCareerCatalog>("TruckTaxi/Career/Catalog");
            var catalog = SaveCopy(sourceCatalog, Folder + "/CareerCatalog.asset");
            if (catalog.trucks.Length != 1) throw new InvalidOperationException("Review the current multiple-truck catalog before remapping the canonical starter.");
            string previousId = catalog.trucks[0].StableId;
            catalog.trucks[0].stableId = definition.StableId; catalog.trucks[0].truck = definition;
            foreach (var upgrade in catalog.upgrades)
                for (int i = 0; i < upgrade.compatibleTruckIds.Length; i++)
                    if (upgrade.compatibleTruckIds[i] == previousId) upgrade.compatibleTruckIds[i] = definition.StableId;
            foreach (var cosmetic in catalog.cosmetics)
                for (int i = 0; i < cosmetic.compatibleTruckIds.Length; i++)
                    if (cosmetic.compatibleTruckIds[i] == previousId) cosmetic.compatibleTruckIds[i] = definition.StableId;
            host.configuration.careerCatalog = catalog;
            EditorUtility.SetDirty(definition); EditorUtility.SetDirty(catalog); EditorUtility.SetDirty(host.configuration);
        }
        private static void RemoveStaleSceneReferences(TruckTaxiBootstrap host, Scene source)
        {
            foreach (var component in host.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var data = new SerializedObject(component); var property = data.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var reference = property.objectReferenceValue;
                    var go = reference as GameObject ?? (reference as Component)?.gameObject;
                    if (go != null && go.scene == source)
                        throw new InvalidOperationException("Old-world reference still present on " + component.name + ": " + property.propertyPath);
                }
            }
        }
        [MenuItem("Truck Taxi/Edenborough/Build Windows")]
        public static void BuildWindows()
        { BuildWindowsAt(BuildPath); }

        [MenuItem("Truck Taxi/Regional/Build Dual World Windows")]
        public static void BuildDualWorldWindows()
        { BuildWindowsAt("Builds/TruckTaxiDualWorld/TruckTaxi.exe"); }

        private static void BuildWindowsAt(string output)
        {
            EnsureEditMode();
            if (!File.Exists(TruckTaxiEdenboroughLayout.ScenePath)) throw new InvalidOperationException("Author and validate Edenborough first.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scenes = new[] { EditorTruckTaxiMainMenuSetup.ScenePath, TruckTaxiEdenboroughLayout.ScenePath, TruckTaxiDemoBuilder.ScenePath }
                .Concat(TruckTaxiRegionalAuthoring.RegionScenePaths).Concat(TruckTaxiMegaWorldAuthoring.NewScenePaths.Where(File.Exists))
                .Concat(File.Exists(TruckTaxiSpeedwayAuthoring.ScenePath) ? new[] { TruckTaxiSpeedwayAuthoring.ScenePath } : Array.Empty<string>()).Distinct().ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            Debug.Log("EDENBOROUGH WINDOWS: " + report.summary.result + "; errors=" + report.summary.totalErrors + "; bytes=" + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Edenborough Windows build failed.");
        }
    }
}
