using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckTargetLaunch;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Open Target Actual Wreck Owners")]
        public static void InstallOpenTargetAppearance()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity") throw new InvalidOperationException("Explicit original Target scene required; caller opens/saves.");
            var match = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TargetMatch>(true)).Single();
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            if (catalog == null || !catalog.Validate(out _) || original == null || match.racers.Length != 4 || match.volcano == null || match.volcano.wrecks == null || match.volcano.wrecks.pieces.Length != 48 || match.replay.sources.Length != 9 || match.replay.ghosts.Length != 9 || !match.volcano.wrecks.externalReplay) throw new InvalidOperationException("Existing catalog/four racers/48 original physical fragments/nine-source external review required.");
            var recipes = catalog.bodies.Select(body => AssetDatabase.LoadAssetAtPath<SportsTruckFragmentRecipe>(SportsTruckFragmentRecipeAuthoring.Folder + "/" + body.id + ".asset")).ToArray();
            for (int i = 0; i < recipes.Length; i++) if (recipes[i] == null || !recipes[i].ValidFor(catalog.bodies[i], out _) || recipes[i].sourceDependencyHash != AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(catalog.bodies[i].visualPrefab)).ToString()) throw new InvalidOperationException("Validate all seven existing private licensed recipes first; this author does not generate or repair them.");
            var expected = original.GetComponentsInChildren<MeshFilter>(true).Where(mesh => mesh.GetComponent<MeshRenderer>() != null).ToArray(); var models = new Transform[4]; var rolling = new Renderer[4][];
            for (int slot = 0; slot < 4; slot++)
            {
                var racer = match.racers[slot]; var truck = racer.truck;
                if (racer.slot != slot || racer.match != match || truck.session != match || truck.slot != slot || racer.wings.Length != 2 || racer.wings.Any(wing => wing == null) || match.replay.sources[slot] != truck.visuals || match.replay.sources[5 + slot] != match.volcano.debrisSources[slot]) throw new InvalidOperationException("Preserve exact owner, wings and intact/debris replay roots.");
                var model = truck.visuals.Find("NWH GR3D visual geometry"); CheckVehicle(truck.transform, model, truck.wheels); var meshes = model.GetComponentsInChildren<MeshFilter>(true);
                if (meshes.Length != 33 || meshes.Any(mesh => mesh.sharedMesh == null || mesh.GetComponent<MeshRenderer>() == null || expected.Count(source => source.name == mesh.name && source.sharedMesh == mesh.sharedMesh) != 1) || racer.wings.Any(wing => wing.IsChildOf(model))) throw new InvalidOperationException("Only exact33 NWH meshes may be replaced; both wing subtrees remain protected.");
                var binding = truck.GetComponent<SportsWheelMeshBinding>(); if (binding == null || binding.truck != truck || binding.meshes.Length != 4) throw new InvalidOperationException("Original four-wheel mesh/controller binding required.");
                rolling[slot] = WheelRenderers(truck.transform, truck.wheels, binding); if (rolling[slot].Length != 4) throw new InvalidOperationException("Exactly four original rolling renderer identities required."); models[slot] = model;
                foreach (string role in SportsTruckFragmentRecipe.BodyRoles.Concat(SportsTruckFragmentRecipe.WheelRoles))
                {
                    var pieces = match.volcano.wrecks.pieces.Where(piece => piece.owner == slot && piece.source.name == role).ToArray();
                    if (pieces.Length != 1) throw new InvalidOperationException("Exact twelve original physical role names per racer required.");
                    var piece = pieces[0]; if (piece.body == null || piece.shape == null || piece.renderer == null || piece.ghost == null || piece.ghostRenderer == null || piece.body.transform.parent != match.replay.sources[5 + slot] || piece.ghost.parent != match.replay.ghosts[5 + slot] || !piece.renderer.enabled || piece.source.GetComponent<MeshFilter>().sharedMesh != piece.renderer.GetComponent<MeshFilter>().sharedMesh) throw new InvalidOperationException("Existing source/body/collider/ghost identities must be retained.");
                    string oldName = "Recorded wreck part " + slot + " · " + role;
                    if (piece.ghost.name != oldName && piece.ghost.name != piece.body.name) throw new InvalidOperationException("Custom ghost naming retained for explicit inspection.");
                }
            }
            if (match.racers.Any(racer => racer.truck.appearanceHost != null || racer.truck.GetComponent<SportsTruckAppearanceHost>() != null))
            {
                if (match.racers.All(racer => racer.truck.appearanceHost != null && match.appearanceHosts.Contains(racer.truck.appearanceHost)) && match.volcano.appearanceWrecks != null) return;
                throw new InvalidOperationException("Existing partial/custom Target bindings retained.");
            }
            var hosts = match.appearanceHosts.ToList();
            for (int slot = 0; slot < 4; slot++)
            {
                var racer = match.racers[slot]; var host = Create(racer.truck.transform, models[slot], racer.truck.wheels, rolling[slot], catalog); host.participantSlot = slot; host.protectedRoots = racer.wings.Concat(new[] { racer.crash.transform }).ToArray(); host.modeOwnedSlots = new[] { SportsTruckAppearanceSlots.Dashboard };
                foreach (var fit in host.compatibleBodies) fit.notice += " Target retains its original wings, physical truck and48-piece lava pool. No dashboard view is available here.";
                racer.truck.appearanceHost = host; hosts.Add(host); EditorUtility.SetDirty(host); EditorUtility.SetDirty(racer.truck);
            }
            // Align only known original render-ghost names. SyncHierarchy then
            // preserves every cached Piece.ghost / ghostRenderer object reference.
            foreach (var piece in match.volcano.wrecks.pieces) { piece.ghost.name = piece.body.name; EditorUtility.SetDirty(piece.ghost.gameObject); }
            var bridge = match.gameObject.AddComponent<TargetAppearanceWreckBridge>(); bridge.match = match; bridge.recipes = recipes; match.volcano.appearanceWrecks = bridge;
            match.appearanceHosts = hosts.ToArray(); match.appearanceFraming = match.appearanceFraming.Concat(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(frame => frame.session == match)).Distinct().ToArray();
            EditorUtility.SetDirty(bridge); EditorUtility.SetDirty(match); EditorUtility.SetDirty(match.volcano);
            if (PrefabUtility.IsPartOfPrefabInstance(match.volcano)) PrefabUtility.RecordPrefabInstancePropertyModifications(match.volcano);
            EditorSceneManager.MarkSceneDirty(scene); Debug.Log("TARGET_APPEARANCE_BOUND four fixed owners; original48 physical pieces/Emit, wings and nine replay roots retained. Caller saves; native validation pending.");
        }
    }
}
