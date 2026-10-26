using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckTerrierRacing;
namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        // Unity's original Instantiate call appends (Clone) to channel0 only.
        // Child identity/order and actual mesh references remain exact.
        public static bool TerrierOriginalReplayChannelsMatch(Transform[] source, Transform[] ghost)
        {
            if (source == null || ghost == null || source.Length != 34 || ghost.Length != 34 || source.Any(item => item == null) || ghost.Any(item => item == null)) return false;
            if (ghost[0].name != source[0].name && ghost[0].name != source[0].name + "(Clone)") return false;
            for (int index = 1; index < 34; index++)
            {
                var a = source[index].GetComponent<MeshFilter>(); var b = ghost[index].GetComponent<MeshFilter>();
                if (source[index].name != ghost[index].name || a == null || b == null || a.sharedMesh == null || a.sharedMesh != b.sharedMesh || source[index].GetComponent<MeshRenderer>() == null || ghost[index].GetComponent<MeshRenderer>() == null) return false;
                int parent = Array.IndexOf(source, source[index].parent); if (parent < 0 || ghost[index].parent != ghost[parent]) return false;
            }
            return true;
        }
        [MenuItem("Truck Sports/Customizer/Bind Open Terrier Twelve Actual Wreck Owners")]
        public static void InstallOpenTerrierAppearance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = SceneManager.GetActiveScene(); if (scene.path != "Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity") throw new InvalidOperationException("Open the original Terrier scene explicitly; caller owns save.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TerrierMatch>(true)).Single();
            if (match.appearanceBridge != null)
            {
                if (match.appearanceBridge.owners.Length == 12 && match.appearanceBridge.owners.All(owner => owner.host != null && owner.originalParts.Length == 34 && owner.originalGhostParts.Length == 34)) return;
                throw new InvalidOperationException("Existing partial/custom Terrier bindings retained for explicit review.");
            }
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            if (catalog == null || !catalog.Validate(out _) || original == null || match.racers.Length != 12 || match.replay == null || match.replay.ghosts.Length != 12 || match.replay.capacity != 2200 || match.wrecks == null || match.wrecks.pieces.Length != 144 || match.wrecks.externalReplay || match.wrecks.capacity != 2200 || match.cabViews.Length != 4 || match.snow == null) throw new InvalidOperationException("Original12 racers/144pieces/2200frame custom replay/four cabs and snow foundation required.");
            var recipes = catalog.bodies.Select(body => AssetDatabase.LoadAssetAtPath<SportsTruckFragmentRecipe>(SportsTruckFragmentRecipeAuthoring.Folder + "/" + body.id + ".asset")).ToArray();
            for (int i = 0; i < recipes.Length; i++) if (recipes[i] == null || !recipes[i].ValidFor(catalog.bodies[i], out _) || recipes[i].sourceDependencyHash != AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(catalog.bodies[i].visualPrefab)).ToString()) throw new InvalidOperationException("All seven current private recipes must validate first; this author never changes them.");
            if (catalog.bodies.Any(body => body.visualPrefab.GetComponentsInChildren<LODGroup>(true).Length != 0 || body.visualPrefab.GetComponentsInChildren<Renderer>(true).Any(renderer => !(renderer is MeshRenderer)))) throw new InvalidOperationException("Explicit normalized static LOD0 body content required.");
            var expected = original.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.GetComponent<MeshRenderer>() != null).ToArray();
            var bindings = new TerrierAppearanceBridge.Binding[12]; var interiors = new Transform[4]; var rolling = new Renderer[12][];
            for (int view = 0; view < 4; view++)
            {
                var look = match.cabViews[view].view.GetComponent<SportsLegacyCabLook>();
                if (look == null || look.terrier != match || look.viewIndex != view || look.cockpit == null || look.cockpit.gameObject.layer != 25 + view) throw new InvalidOperationException("Preserve each real Terrier driver seat and private25–28 layers."); interiors[view] = look.cockpit;
            }
            for (int slot = 0; slot < 12; slot++)
            {
                var racer = match.racers[slot]; var truck = racer.truck; CheckVehicle(truck.transform, truck.visuals, truck.wheels);
                var wheel = truck.GetComponent<TerrierWheelPresentation>(); var meshes = truck.visuals.GetComponentsInChildren<MeshFilter>(true);
                if (racer.slot != slot || racer.match != match || truck.session != match || truck.appearanceHost != null || truck.GetComponent<SportsTruckAppearanceHost>() != null || truck.visuals.gameObject.layer != 10 + slot || wheel == null || wheel.racer != racer || wheel.meshes.Length != 4 || wheel.centers.Length != 4 || wheel.alignment.Length != 4 || truck.GetComponent<SportsWheelMeshBinding>() != null || meshes.Length != 33 || meshes.Any(mesh => mesh.GetComponent<MeshRenderer>() == null || expected.Count(source => source.name == mesh.name && source.sharedMesh == mesh.sharedMesh) != 1)) throw new InvalidOperationException("Exact original33 meshes, four Terrier wheel bindings and fixed participant identity required; preserve custom state.");
                string[] names = SportsTruckFragmentRecipe.WheelRoles;
                if (Enumerable.Range(0, 4).Any(index => wheel.meshes[index] == null || wheel.meshes[index].name != names[index] || !wheel.meshes[index].IsChildOf(truck.visuals) || wheel.meshes[index].GetComponent<MeshRenderer>() == null)) throw new InvalidOperationException("Original Terrier wheel presentation source identity changed.");
                rolling[slot] = wheel.meshes.Select(mesh => mesh.GetComponent<Renderer>()).ToArray();
                var sourceParts = truck.visuals.GetComponentsInChildren<Transform>(true); var ghostParts = match.replay.ghosts[slot].GetChild(0).GetComponentsInChildren<Transform>(true);
                if (!TerrierOriginalReplayChannelsMatch(sourceParts, ghostParts) || match.replay.ghosts[slot].GetComponentsInChildren<Collider>(true).Length != 0 || match.replay.ghosts[slot].GetComponentsInChildren<Rigidbody>(true).Length != 0) throw new InvalidOperationException("Original34 replay channels and render-only ghost roots must remain exact.");
                bindings[slot] = new TerrierAppearanceBridge.Binding { wheels = wheel, originalParts = sourceParts, originalGhostParts = ghostParts };
                foreach (string role in SportsTruckFragmentRecipe.BodyRoles.Concat(SportsTruckFragmentRecipe.WheelRoles))
                {
                    var parts = match.wrecks.pieces.Where(piece => piece.owner == slot && piece.source.name == role).ToArray();
                    if (parts.Length != 1) throw new InvalidOperationException("Exactly twelve original physical roles per owner required."); var piece = parts[0];
                    if (piece.body == null || piece.shape == null || piece.renderer == null || piece.ghost == null || piece.ghostRenderer == null || !piece.source.IsChildOf(truck.visuals) || piece.source.GetComponent<MeshFilter>().sharedMesh != piece.renderer.GetComponent<MeshFilter>().sharedMesh || piece.ghostRenderer.transform != piece.ghost) throw new InvalidOperationException("Preserve all original physical/renderer/ghost identities.");
                }
            }
            for (int slot = 0; slot < 12; slot++)
            {
                var truck = match.racers[slot].truck; var host = Create(truck.transform, truck.visuals, truck.wheels, rolling[slot], catalog); host.participantSlot = slot;
                host.protectedRoots = interiors.Concat(new[] { match.course.geometry, match.dessert.basin, match.replay.ghosts[slot] }).ToArray();
                foreach (var fit in host.compatibleBodies) fit.notice += " Terrier retains its exact original race/finish envelope and144 physical wreck pieces; body choice changes rendering only.";
                truck.appearanceHost = host; bindings[slot].host = host; EditorUtility.SetDirty(host); EditorUtility.SetDirty(truck);
            }
            var bridge = match.gameObject.AddComponent<TerrierAppearanceBridge>(); bridge.match = match; bridge.owners = bindings; bridge.recipes = recipes; bridge.cabs = interiors.Select(Cab).ToArray(); match.appearanceBridge = bridge;
            // Explicit local preparation AFTER original NewHeat reset. Do not
            // register these hosts in Session's before-BeginGame prepare list.
            match.appearanceFraming = match.appearanceFraming.Concat(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(frame => frame.session == match)).Distinct().ToArray();
            foreach (var cab in bridge.cabs) EditorUtility.SetDirty(cab); EditorUtility.SetDirty(bridge); EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("TERRIER_APPEARANCE_BOUND twelve fixed owners/48 existing NWH wheels/144 existing physical+ghost pieces; fixed34+4 channels=28,089,600 part-pose bytes at2200frames (+2,956,800). Caller saves; native/Windows proof pending.");
        }
    }
}
