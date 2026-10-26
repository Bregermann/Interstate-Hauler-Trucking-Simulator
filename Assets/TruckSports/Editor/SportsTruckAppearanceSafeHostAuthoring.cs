using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckBaseJumping;
using LWS.TruckBungeeDemolition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Base Jumping And Bungee Hosts")]
        public static void BindMissingBaseAndBungeeHosts()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            if (catalog == null || !catalog.Validate(out _)) throw new InvalidOperationException("Existing validated owned appearance catalog required.");
            BindSuspended("Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity", catalog, false);
            BindSuspended("Assets/TruckBungeeDemolition/Scenes/TruckBungeeDemolition.unity", catalog, true);
        }

        static void BindSuspended(string path, SportsTruckAppearanceCatalog catalog, bool bungee)
        {
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing scene edits first: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects(); SportsEventSession match; SportsEventTruck[] trucks;
                var protectedRoots = new Transform[4][];
                if (bungee)
                {
                    var demolition = roots.SelectMany(root => root.GetComponentsInChildren<BungeeMatch>(true)).Single(); match = demolition;
                    if (demolition.bays == null || demolition.bays.Length != 4 || demolition.bays.Any(bay => bay == null || bay.truck == null)) throw new InvalidOperationException("Four original independent Bungee bays required.");
                    trucks = demolition.bays.Select(bay => bay.truck).ToArray();
                    for (int i = 0; i < 4; i++)
                    {
                        var bay = demolition.bays[i];
                        if (bay.slot != i || bay.match != demolition || bay.craneAnchor == null || bay.towerMount == null || bay.towerPrefab == null || bay.spring == null || bay.spring.gameObject != bay.truck.gameObject || bay.spring.connectedBody != null || bay.rope == null || bay.rope.transform == bay.truck.transform)
                            throw new InvalidOperationException("Preserve actual fixed-owner crane, structural prefab, spring and rope bindings.");
                        protectedRoots[i] = new[] { bay.craneAnchor, bay.towerMount, bay.rope.transform }.Concat(bay.transform.Cast<Transform>().Where(part => part != bay.truck.transform)).Distinct().ToArray();
                    }
                }
                else
                {
                    var jumping = roots.SelectMany(root => root.GetComponentsInChildren<BaseJumpMatch>(true)).Single(); match = jumping;
                    if (jumping.jumpers == null || jumping.jumpers.Length != 4 || jumping.jumpers.Any(jumper => jumper == null || jumper.truck == null) || jumping.transport == null || jumping.transport.model == null || jumping.transport.staging == null || jumping.transport.staging.Length != 4 || jumping.transport.staging.Any(part => part == null) || jumping.transport.exit == null || jumping.transport.doorway == null)
                        throw new InvalidOperationException("Preserve the four actual fixed-owner parachutists and existing helicopter exit choreography.");
                    trucks = jumping.jumpers.Select(jumper => jumper.truck).ToArray();
                    for (int i = 0; i < 4; i++)
                    {
                        var jumper = jumping.jumpers[i];
                        if (jumper.slot != i || jumper.match != jumping || jumper.canopy == null || jumper.canopy.GetComponent<MeshFilter>()?.sharedMesh == null || jumper.lines == null || jumper.lines.Length != 4 || jumper.lines.Any(line => line == null) || jumper.crash == null)
                            throw new InvalidOperationException("Preserve actual canopy geometry, four independent harness lines and crash effects.");
                        protectedRoots[i] = new[] { jumper.canopy, jumper.crash.transform, jumping.transport.model.transform, jumping.transport.exit, jumping.transport.doorway }.Concat(jumper.lines.Select(line => line.transform)).Concat(jumping.transport.staging).Distinct().ToArray();
                    }
                }
                if (trucks.Distinct().Count() != 4) throw new InvalidOperationException("Four distinct physical trucks required.");
                if (trucks.Any(truck => truck.appearanceHost != null || truck.GetComponent<SportsTruckAppearanceHost>() != null))
                { Debug.Log("SPORTS_SUSPENDED_APPEARANCE_PRESERVED " + path + " existing custom or partial hosts retained."); return; }
                var rolling = new Renderer[4][];
                for (int i = 0; i < 4; i++)
                {
                    var truck = trucks[i];
                    if (truck.session != match || truck.slot != i || truck.paint == null || truck.visuals == null || truck.visuals != truck.paint.transform.parent || truck.visuals.name != "NWH GR3D visual geometry")
                        throw new InvalidOperationException("Only the original clean inner NWH rendering root and fixed participant ownership are supported.");
                    CheckVehicle(truck.transform, truck.visuals, truck.wheels);
                    if (protectedRoots[i].Any(part => part == null || part == truck.visuals || part.IsChildOf(truck.visuals) || truck.visuals.IsChildOf(part)))
                        throw new InvalidOperationException("Required equipment overlaps the clean semi subtree; retain custom scene bindings.");
                    rolling[i] = bungee ? WheelRenderers(truck.transform, truck.wheels, truck.GetComponent<SportsWheelMeshBinding>()) : BaseOriginalRollingRenderers(truck);
                }
                var hosts = new List<SportsTruckAppearanceHost>(match.appearanceHosts ?? Array.Empty<SportsTruckAppearanceHost>());
                for (int i = 0; i < 4; i++)
                {
                    var truck = trucks[i]; var host = Create(truck.transform, truck.visuals, truck.wheels, rolling[i], catalog);
                    host.participantSlot = i; host.protectedRoots = protectedRoots[i];
                    foreach (var fit in host.compatibleBodies) fit.notice += bungee ? " This sport retains its existing crane, rope, spring and physical wrecking envelope." : " This sport retains its existing helicopter exit, canopy and harness attachment points.";
                    truck.appearanceHost = host; hosts.Add(host); EditorUtility.SetDirty(host); EditorUtility.SetDirty(truck);
                }
                match.appearanceHosts = hosts.ToArray();
                match.appearanceFraming = (match.appearanceFraming ?? Array.Empty<SportsBroadcastFraming>()).Concat(roots.SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing => framing.session == match)).Distinct().ToArray();
                EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_SUSPENDED_APPEARANCE_BOUND " + path + " four fixed-profile render-only hosts; original equipment, physics, slot ownership and camera authorities retained. Native validation pending.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        // Base Jumping intentionally never called FitTruck: its original tire
        // meshes are static geometry and the existing NWH containers are empty.
        // Bind the four exact source meshes without invoking that physics fitter.
        static Renderer[] BaseOriginalRollingRenderers(SportsEventTruck truck)
        {
            if (truck.GetComponent<SportsWheelMeshBinding>() != null || truck.wheels.Any(wheel => wheel.wheel.rotatingContainer.GetComponentsInChildren<Renderer>(true).Length != 0))
                throw new InvalidOperationException("Preserve custom Base Jumping wheel bindings; only the audited static source wheels are supported.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab");
            if (source == null) throw new InvalidOperationException("Installed original GR3D semi visual source required.");
            var meshes = truck.visuals.GetComponentsInChildren<MeshFilter>(true); var originals = source.GetComponentsInChildren<MeshFilter>(true); var result = new List<Renderer>();
            foreach (string name in new[] { "Whl HD FL", "Whl HD FR", "Whl HD RL", "Whl HD RR" })
            {
                var actual = meshes.Where(mesh => mesh.name == name).ToArray(); var expected = originals.Where(mesh => mesh.name == name).ToArray();
                if (actual.Length != 1 || expected.Length != 1 || actual[0].sharedMesh == null || actual[0].sharedMesh != expected[0].sharedMesh || actual[0].GetComponent<MeshRenderer>() == null)
                    throw new InvalidOperationException("Preserve custom tire geometry; exact installed GR3D source mesh required: " + name);
                result.Add(actual[0].GetComponent<MeshRenderer>());
            }
            return result.ToArray();
        }
    }
}
