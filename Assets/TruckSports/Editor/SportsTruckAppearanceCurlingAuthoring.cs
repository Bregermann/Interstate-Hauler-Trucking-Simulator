using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckCurling;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Open Curling Delivery Owners")]
        public static void InstallOpenCurlingAppearance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckCurling/Scenes/TruckCurling.unity") throw new InvalidOperationException("Open original Curling explicitly; caller owns save.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CurlingMatch>(true)).Single();
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            var look = match.cabCamera.view.GetComponent<SportsLegacyCabLook>();
            if (catalog == null || !catalog.Validate(out _) || original == null || match.stones.Length != 16 || match.cab == null || look == null || look.curling != match || look.cockpit != match.cab.transform || match.cab.layer != 26 || (match.houseCamera.cullingMask & (1 << 26)) != 0) throw new InvalidOperationException("Existing16 stones and actual layer26 cab/house isolation required.");
            var expected = original.GetComponentsInChildren<MeshFilter>(true).Where(mesh => mesh.GetComponent<MeshRenderer>() != null).ToArray();
            var rolling = new Renderer[16][];
            for (int i = 0; i < 16; i++)
            {
                var stone = match.stones[i]; var truck = stone.truck; CheckVehicle(truck.transform, truck.visuals, truck.wheels);
                var shape = truck.GetComponent<MeshCollider>();
                if (truck.session != match || stone.match != match || stone.footprint == null || stone.protectedRing == null || shape == null || !shape.convex || shape.sharedMesh == null || truck.shape != null || truck.GetComponentsInChildren<NWH.WheelController3D.WheelController>(true).Length != 4) throw new InvalidOperationException("Exact existing circular physical stone and four NWH controllers required.");
                var meshes = truck.visuals.GetComponentsInChildren<MeshFilter>(true);
                if (meshes.Length != 33 || meshes.Any(mesh => mesh.sharedMesh == null || mesh.GetComponent<MeshRenderer>() == null || expected.Count(source => source.name == mesh.name && source.sharedMesh == mesh.sharedMesh) != 1)) throw new InvalidOperationException("Preserve custom rendering; exact33 installed NWH mesh identities required.");
                if (stone.footprint.transform.IsChildOf(truck.visuals) || stone.protectedRing.transform.IsChildOf(truck.visuals)) throw new InvalidOperationException("Original footprint and guard ring must remain outside the body subtree.");
                // Same existing static-mesh binding used by Base; controller /
                // rotating-container references are already saved, never created.
                rolling[i] = BaseOriginalRollingRenderers(truck);
            }
            if (match.stones.Any(stone => stone.truck.appearanceHost != null || stone.truck.GetComponent<SportsTruckAppearanceHost>() != null))
            {
                if (match.stones.All(stone => stone.truck.appearanceHost != null && match.appearanceHosts.Contains(stone.truck.appearanceHost)) && match.appearanceCab != null) return;
                throw new InvalidOperationException("Existing partial/custom Curling bindings preserved for explicit review.");
            }
            var hosts = match.appearanceHosts.ToList();
            for (int i = 0; i < 16; i++)
            {
                var stone = match.stones[i]; var truck = stone.truck; var host = Create(truck.transform, truck.visuals, truck.wheels, rolling[i], catalog);
                host.participantSlot = -1; host.protectedRoots = new[] { stone.footprint.transform, stone.protectedRing.transform, match.cab.transform };
                foreach (var fit in host.compatibleBodies) fit.notice += " Curling retains its original circular physical stone, footprint and guard rules.";
                truck.appearanceHost = host; hosts.Add(host); EditorUtility.SetDirty(host); EditorUtility.SetDirty(truck);
            }
            match.appearanceHosts = hosts.ToArray(); match.appearanceCab = Cab(match.cab.transform);
            EditorUtility.SetDirty(match.appearanceCab); EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("CURLING_APPEARANCE_BOUND16 original physical stones,64 existing NWH controllers/rotating containers; actual delivery owner and layer26 cab retained. Caller saves; native proof pending.");
        }
    }
}
