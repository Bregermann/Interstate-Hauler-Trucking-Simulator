using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckBogSnorkelling;
using LWS.TruckSumo;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    // Explicit variants of the same host bindings/fitter. No venue builders,
    // replacement equipment, source physics or whole assembly hiding.
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Bog And Sumo Hosts")]
        public static void BindMissingBogAndSumoHosts()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            if (catalog == null || !catalog.Validate(out _)) throw new InvalidOperationException("Existing validated owned appearance catalog required.");
            BindEquipped("Assets/TruckBogSnorkelling/Scenes/TruckBogSnorkelling.unity", catalog, false);
            BindEquipped("Assets/TruckSumo/Scenes/TruckSumo.unity", catalog, true);
        }
        static void BindEquipped(string path, SportsTruckAppearanceCatalog catalog, bool sumo)
        {
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing scene edits first: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                SportsEventSession match; SportsEventTruck[] trucks; BogMatch bog = null; SumoMatch wrestle = null;
                if (sumo) { wrestle = roots.SelectMany(root => root.GetComponentsInChildren<SumoMatch>(true)).Single(); match = wrestle; trucks = wrestle.fighters.Select(fighter => fighter.truck).ToArray(); }
                else { bog = roots.SelectMany(root => root.GetComponentsInChildren<BogMatch>(true)).Single(); match = bog; trucks = bog.racers.Select(racer => racer.truck).ToArray(); }
                if (trucks.Length != 4 || trucks.Any(truck => truck == null) || trucks.Distinct().Count() != 4) throw new InvalidOperationException("Four distinct original fixed-owner trucks required.");
                if (trucks.Any(truck => truck.appearanceHost != null || truck.GetComponent<SportsTruckAppearanceHost>() != null))
                { Debug.Log("SPORTS_EQUIPPED_APPEARANCE_BINDINGS_PRESERVED " + path + " existing custom/partial bindings retained."); return; }
                var inner = new Transform[4]; var rolling = new Renderer[4][]; var protectedRoots = new Transform[4][];
                for (int i = 0; i < 4; i++)
                {
                    var truck = trucks[i];
                    if (truck.slot != i || truck.session != match || truck.paint == null || truck.visuals == null) throw new InvalidOperationException("Existing actor ownership and paint/model references required.");
                    inner[i] = truck.paint.transform.parent;
                    if (inner[i] == null || inner[i].name != "NWH GR3D visual geometry" || !truck.paint.transform.IsChildOf(inner[i])) throw new InvalidOperationException("Original Cab-owned inner semi rendering root required.");
                    CheckVehicle(truck.transform, inner[i], truck.wheels);
                    rolling[i] = WheelRenderers(truck.transform, truck.wheels, truck.GetComponent<SportsWheelMeshBinding>());
                    if (sumo)
                    {
                        var fighter = wrestle.fighters[i];
                        if (inner[i].parent != truck.visuals || truck.visuals == truck.transform || fighter.brows.Length != 2 || fighter.tails.Length != 2 || fighter.trim.Length < 4)
                            throw new InvalidOperationException("Preserve the original Sumo render assembly/costume/expression references.");
                        protectedRoots[i] = truck.visuals.Cast<Transform>().Where(part => part != inner[i]).ToArray();
                        if (protectedRoots[i].Length != 8 || truck.visuals.Find("Front mawashi costume") == null || fighter.brows.Concat(fighter.tails).Any(part => part == null || !part.IsChildOf(truck.visuals) || part.IsChildOf(inner[i])) || fighter.trim.Any(renderer => renderer == null || !renderer.transform.IsChildOf(truck.visuals) || renderer.transform.IsChildOf(inner[i])))
                            throw new InvalidOperationException("The actual mawashi, headband, knots, tails and live eyebrows must remain outside the replaced body.");
                    }
                    else
                    {
                        var racer = bog.racers[i]; var children = truck.transform.Cast<Transform>().ToArray();
                        var tubes = children.Where(part => part.name == "Modelled snorkel tube").ToArray(); var caps = children.Where(part => part.name == "Visible orange snorkel cap").ToArray(); var lamps = children.Where(part => part.name == "Submerged truck headlamp").ToArray();
                        if (inner[i] != truck.visuals || racer.intake == null || racer.intake.parent != truck.transform || (racer.intake.localPosition - new Vector3(.63f, 2.15f, .28f)).sqrMagnitude > 1e-8f || tubes.Length != 2 || caps.Length != 1 || lamps.Length != 2 || lamps.Any(lamp => lamp.GetComponent<Light>() == null))
                            throw new InvalidOperationException("Original direct-child gameplay intake, two snorkel pipes, cap and two real underwater lamps required; custom equipment is preserved.");
                        protectedRoots[i] = tubes.Concat(caps).Concat(lamps).Concat(new[] { racer.intake }).ToArray();
                    }
                    if (protectedRoots[i].Any(part => part == inner[i] || part.IsChildOf(inner[i]) || inner[i].IsChildOf(part))) throw new InvalidOperationException("Required equipment overlaps the explicit semi rendering subtree.");
                }
                if (sumo && (wrestle.replay == null || wrestle.replay.sources.Length != 5 || wrestle.replay.ghosts.Length != 5 || wrestle.replay.sources[4] != wrestle.ring || Enumerable.Range(0, 4).Any(i => wrestle.replay.sources[i] != trucks[i].visuals || wrestle.replay.ghosts[i] == null)))
                    throw new InvalidOperationException("Preserve the four actual full-assembly replay sources and fifth shrinking-ring source.");
                var hosts = new List<SportsTruckAppearanceHost>(match.appearanceHosts ?? Array.Empty<SportsTruckAppearanceHost>());
                for (int i = 0; i < 4; i++)
                {
                    var host = Create(trucks[i].transform, inner[i], trucks[i].wheels, rolling[i], catalog); host.participantSlot = i; host.protectedRoots = protectedRoots[i];
                    if (sumo)
                    {
                        host.modeOwnedSlots = new[] { SportsTruckAppearanceSlots.Brows };
                        foreach (var fit in host.compatibleBodies) fit.notice += " Sumo retains its required costume, headband and animated eyebrows; saved brow cosmetics remain available in other sports.";
                    }
                    else foreach (var fit in host.compatibleBodies) fit.notice += " This sport retains its standardized snorkel, air-intake height and underwater lamps.";
                    trucks[i].appearanceHost = host; hosts.Add(host); EditorUtility.SetDirty(host); EditorUtility.SetDirty(trucks[i]);
                }
                match.appearanceHosts = hosts.ToArray();
                var framings = new List<SportsBroadcastFraming>(match.appearanceFraming ?? Array.Empty<SportsBroadcastFraming>());
                foreach (var framing in roots.SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing => framing.session == match)) if (!framings.Contains(framing)) framings.Add(framing);
                match.appearanceFraming = framings.ToArray();
                if (sumo)
                {
                    var help = match.menus.pages[4].transform.Find("Rules and controls")?.GetComponent<TMP_Text>(); if (help != null) { help.text = match.Help; EditorUtility.SetDirty(help); }
                    foreach (int page in new[] { 0, 3 })
                    {
                        var subtitle = match.menus.pages[page].transform.Find("Subtitle")?.GetComponent<TMP_Text>(); if (subtitle == null) continue;
                        const string notice = "Sumo keeps its costume and animated eyebrows.";
                        if (!subtitle.text.Contains(notice)) subtitle.text = string.IsNullOrWhiteSpace(subtitle.text) ? notice : subtitle.text.TrimEnd() + "\n" + notice;
                        EditorUtility.SetDirty(subtitle);
                    }
                }
                EditorUtility.SetDirty(match); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_EQUIPPED_APPEARANCE_BOUND " + path + " four fixed-profile hosts; inner-body-only replacement, exact required equipment and existing NWH/camera/replay roots retained. Native verification pending.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
