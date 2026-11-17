#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Landscape = LWS.TruckExtremeHauling.Editor.HaulingVenueLandscapeAuthoring;

namespace LWS.TruckHitchSports.Editor
{
    // Explicit peripheral scenery for the six saved core venues. Terrain,
    // render-only model conversion and LOD intake remain shared with hauling.
    public static class CoreHitchVenueLandscapeAuthoring
    {
        const string RootName = "Owned core trailer venue landscape";
        const string Folder = "Assets/TruckHitchSports/Landscapes/";
        const string City = "Assets/JC_LP_MegaCity/Prefabs/Buildings/SM_Buildings_";
        const string Container = "Assets/ShipContainerPack/URP/Prefabs/ShipContainer";
        const string Crate = "Assets/LeartesStudios/Garage Warehouse Environment/Art/Prefabs/SM_PK04_WoodenCrate_01.prefab";
        const string Scaffold = "Assets/ScaffoldsPack/URP/Prefabs/SM_StairTower.prefab";
        const string Carrier = "Assets/NWH/Vehicle Physics 2/Vehicles/RocketCarrier/RocketCarrier.prefab";
        const string Ramp = "Assets/TruckTerrierRacing/Prefabs/Timber launch ramp.prefab";
        const string CastleLibrary = "Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";
        const string StandLibrary = "Assets/TruckBeerPong/Carnival/Tailgate stadium and render library.prefab";
        static readonly string[] Venues = { "Trailerblazers", "JackknifeHighwire", "HitchAndRun", "TrailerTrebuchet", "HitchHandoff", "Unhitched" };
        struct PhysicalState { public Collider shape; public Matrix4x4 pose; public bool enabled, trigger; }

        [MenuItem("Truck Sports/Alpha 0.10/Upgrade active core trailer surroundings")]
        public static void UpgradeActiveVenue()
        {
            InstallActiveVenue();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
        public static GameObject InstallActiveVenue()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before adding core trailer surroundings.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!Venues.Contains(scene.name) || scene.path != "Assets/TruckHitchSports/Scenes/" + scene.name + ".unity") throw new InvalidOperationException("Open one of the six saved core trailer venues.");
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (existing != null)
            {
                if (!existing.activeSelf) throw new InvalidOperationException("A partial scenery intake remains inactive. Inspect it before continuing.");
                Debug.Log("HH10_CORE_LANDSCAPE_PRESERVED " + scene.name); return existing;
            }
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HitchCompetitionSession>(true)).Single();
            var originals = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true)).Select(c => new PhysicalState { shape = c, pose = c.transform.localToWorldMatrix, enabled = c.enabled, trigger = c.isTrigger }).ToArray();
            var truckPoses = match.trucks.Select(t => t.transform.localToWorldMatrix).ToArray();
            bool highwire = scene.name == "JackknifeHighwire", siege = scene.name == "TrailerTrebuchet", stunt = scene.name == "Unhitched";
            // Audit the actual production render assets before creating anything.
            foreach (string path in new[] { City + "Warehouse_01.prefab", City + "Factory_01.prefab", Container + "Blue_URP.prefab", Container + "Red_URP.prefab", Crate, Scaffold }) Need<GameObject>(path);
            GameObject castle = siege ? Subtree(CastleLibrary, "Fortified castle sanctuary") : null;
            GameObject stands = stunt ? Subtree(StandLibrary, "Union Field Grandstand tier") : null;
            if (scene.name == "Trailerblazers") Need<GameObject>(Carrier);
            if (stunt) Need<GameObject>(Ramp);
            var floor = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Industrial competition apron");
            Bounds safe;
            if (highwire)
            {
                var roads = originals.Where(p => p.shape.name == "Suspended narrow physical roadway" || p.shape.name == "Finish receiving platform").ToArray();
                if (roads.Length == 0) throw new InvalidOperationException("Preserve the original suspended physical roadways.");
                safe = roads[0].shape.bounds; foreach (var road in roads) safe.Encapsulate(road.shape.bounds);
                safe.Expand(new Vector3(18, 0, 20));
            }
            else if (floor != null && floor.GetComponent<Collider>() != null) safe = floor.GetComponent<Collider>().bounds;
            else throw new InvalidOperationException("The original industrial physical apron must remain present.");
            string output = Folder + scene.name;
            var root = new GameObject(RootName); root.SetActive(false);
            var terrain = Landscape.CreatePeripheralTerrain(root.transform, output, safe, highwire);
            if (highwire) MountainRoute(root.transform, terrain, safe, output);
            else if (siege) SiegeGrounds(root.transform, terrain, safe, output, castle);
            else if (stunt) StuntGrounds(root.transform, terrain, safe, output, stands);
            else FreightGrounds(root.transform, terrain, safe, output, scene.name == "Trailerblazers", scene.name == "HitchHandoff");
            // Some production vehicle prefabs include cameras/audio. Only their
            // authored render geometry is scenery, never another live service.
            foreach(var flare in root.GetComponentsInChildren<FlareLayer>(true))UnityEngine.Object.DestroyImmediate(flare);
            foreach (var camera in root.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera);
            foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) UnityEngine.Object.DestroyImmediate(listener);
            foreach (var audio in root.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(audio);
            foreach (var light in root.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
            foreach (var probe in root.GetComponentsInChildren<ReflectionProbe>(true)) UnityEngine.Object.DestroyImmediate(probe);
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
            if (root.GetComponentsInChildren<Collider>(true).Length != 0 || root.GetComponentsInChildren<Rigidbody>(true).Length != 0) throw new InvalidOperationException("Peripheral scenery must be render-only.");
            foreach (var state in originals) if (state.shape == null || state.shape.transform.localToWorldMatrix != state.pose || state.shape.enabled != state.enabled || state.shape.isTrigger != state.trigger) throw new InvalidOperationException("Existing gameplay collision changed during scenery intake.");
            for (int i = 0; i < truckPoses.Length; i++) if (match.trucks[i].transform.localToWorldMatrix != truckPoses[i]) throw new InvalidOperationException("A physical truck start changed.");
            root.SetActive(true); Undo.RegisterCreatedObjectUndo(root, "Add owned core trailer surroundings"); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("HH10_CORE_LANDSCAPE_ADDED " + scene.name + "; production relief/models only; original starts, objectives, floor materials and colliders preserved. Gameplay/visual acceptance still required.");
            return root;
        }
        static void FreightGrounds(Transform root, Terrain terrain, Bounds safe, string output, bool rocket, bool relay)
        {
            foreach (int side in new[] { -1, 1 }) for (int row = 0; row < 4; row++)
            {
                float x = side < 0 ? safe.min.x - 43 : safe.max.x + 43, z = safe.min.z + 65 + row * (safe.size.z - 130) / 3;
                Place(root, City + (rocket && row % 2 == 0 ? "Factory_01.prefab" : "Warehouse_01.prefab"), terrain, x, z, new Vector3(62, rocket ? 46 : 30, 70), side * 90, output);
                float edge = side < 0 ? safe.min.x - 14 : safe.max.x + 14;
                Place(root, Container + (row % 2 == 0 ? "Red_URP.prefab" : "Blue_URP.prefab"), terrain, edge, z, new Vector3(10, 7, relay ? 28 : 24), row * 90, output);
                if (rocket) Place(root, Scaffold, terrain, edge, z + 30, new Vector3(14, 36, 14), 90, output);
                else for (int crate = 0; crate < 3; crate++) Place(root, Crate, terrain, edge + side * 5, z + 22 + crate * 5, new Vector3(4, 4 + crate, 4), crate * 17, output);
            }
            if (rocket) foreach (int side in new[] { -1, 1 }) Place(root, Carrier, terrain, safe.center.x + side * 90, safe.min.z - 35, new Vector3(11, 8, 26), 90, output);
        }
        static void MountainRoute(Transform root, Terrain terrain, Bounds safe, string output)
        {
            foreach (int side in new[] { -1, 1 }) for (int station = 0; station < 3; station++)
            {
                float x = side < 0 ? safe.min.x - 50 : safe.max.x + 50, z = safe.min.z + station * safe.size.z / 2;
                Place(root, City + "Warehouse_01.prefab", terrain, x, z, new Vector3(42, 24, 38), side * 90, output);
                Place(root, Scaffold, terrain, x - side * 20, z + 18, new Vector3(10, 38, 10), side * 90, output);
            }
            // The shared authored relief supplies real valley/cliff silhouettes,
            // while these mountain service stations stay outside the bridge.
        }
        static void SiegeGrounds(Transform root, Terrain terrain, Bounds safe, string output, GameObject castle)
        {
            foreach (int side in new[] { -1, 1 })
            {
                float x = side < 0 ? safe.min.x - 55 : safe.max.x + 55;
                Landscape.PlacePeripheralModel(root, castle, new Vector3(x, Ground(terrain, x, safe.center.z + 140), safe.center.z + 140), new Vector3(140, 75, 125), side * 90, output);
                for (int row = 0; row < 6; row++) Place(root, Crate, terrain, side < 0 ? safe.min.x - 20 : safe.max.x + 20, safe.min.z + 90 + row * 55, new Vector3(6, 5 + row % 3, 6), row * 29, output);
            }
            Landscape.PlacePeripheralModel(root, castle, new Vector3(safe.center.x, Ground(terrain, safe.center.x, safe.max.z + 100), safe.max.z + 100), new Vector3(180, 95, 150), 180, output);
        }
        static void StuntGrounds(Transform root, Terrain terrain, Bounds safe, string output, GameObject stands)
        {
            foreach (int side in new[] { -1, 1 }) for (int bay = 0; bay < 4; bay++)
            {
                float x = side < 0 ? safe.min.x - 30 : safe.max.x + 30, z = safe.min.z + 65 + bay * (safe.size.z - 130) / 3;
                Landscape.PlacePeripheralModel(root, stands, new Vector3(x, Ground(terrain, x, z), z), new Vector3(28, 16, 82), side < 0 ? 90 : -90, output);
                Place(root, Ramp, terrain, x + side * 25, z + 20, new Vector3(16, 8, 35), side * -90, output);
                Place(root, Container + "Blue_URP.prefab", terrain, x + side * 25, z - 30, new Vector3(11, 8, 28), 90, output);
            }
        }
        static void Place(Transform root, string path, Terrain terrain, float x, float z, Vector3 size, float yaw, string output)
            => Landscape.PlacePeripheralModel(root, path, new Vector3(x, Ground(terrain, x, z), z), size, yaw, output);
        static float Ground(Terrain terrain, float x, float z) => terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
        static GameObject Subtree(string path, string name)
        {
            var source = Need<GameObject>(path);
            var child = source.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (child == null || child.GetComponentsInChildren<Renderer>(true).Length == 0) throw new InvalidOperationException("Existing production render subtree missing: " + name);
            return child.gameObject;
        }
        static T Need<T>(string path) where T : UnityEngine.Object
        { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset == null) throw new InvalidOperationException("Installed production scenery missing: " + path); return asset; }
    }
}
#endif
