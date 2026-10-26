using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckBowling;
using LWS.TruckMusicalParking;
using LWS.TruckBuzkashi;
using LWS.TruckFishing;
using LWS.TruckDanceOff;
using NWH.WheelController3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckSports.Editor
{
    // Explicit additive bindings only. Existing custom hosts are preserved;
    // venue builders, vehicle factories and vendor prefabs are never invoked.
    public static partial class SportsTruckAppearanceHostAuthoring
    {
        [MenuItem("Truck Sports/Customizer/Bind Missing Parking And Bowling Hosts")]
        public static void BindMissingHosts()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            if (catalog == null || !catalog.Validate(out _)) throw new InvalidOperationException("Author and validate the owned appearance catalog first.");
            Bind("Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity", catalog, true);
            Bind("Assets/TruckBowling/Scenes/TruckBowling.unity", catalog, false);
        }
        [MenuItem("Truck Sports/Customizer/Bind Missing Buzkashi Fishing And Dance Hosts")]
        public static void BindMissingCleanModelHosts()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            if (catalog == null || !catalog.Validate(out _)) throw new InvalidOperationException("Author and validate the owned appearance catalog first.");
            BindClean("Assets/TruckBuzkashi/Scenes/TruckBuzkashi.unity", catalog);
            BindClean("Assets/TruckFishing/Scenes/TruckFishing.unity", catalog);
            BindClean("Assets/TruckDanceOff/Scenes/TruckDanceOff.unity", catalog);
        }
        const string LiveryPart = "Owned surface-following participant livery";
        const string LiveryMaterial = "Sports participant livery";
        [MenuItem("Truck Sports/Customizer/Refresh Owned Atlas Livery Paint Slots")]
        public static void RefreshOwnedLiveryPaintSlots()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            var catalog = AssetDatabase.LoadAssetAtPath<SportsTruckAppearanceCatalog>("Assets/TruckSports/Resources/SportsTruckAppearanceCatalog.asset");
            if (catalog == null || !catalog.Validate(out _)) throw new InvalidOperationException("Author and validate the owned appearance catalog first.");
            var slots = new Dictionary<string, SportsTruckAppearanceCompatibility.PaintSlot[]>();
            foreach (string id in new[] { "truck.fire", "novelty.icecream" }) slots.Add(id, OwnedLiverySlots(catalog.FindBody(id)));
            foreach (string path in new[] { "Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity", "Assets/TruckBowling/Scenes/TruckBowling.unity", "Assets/TruckBuzkashi/Scenes/TruckBuzkashi.unity", "Assets/TruckFishing/Scenes/TruckFishing.unity", "Assets/TruckDanceOff/Scenes/TruckDanceOff.unity" })
                RefreshLivery(path, catalog, slots);
        }
        static SportsTruckAppearanceCompatibility.PaintSlot[] OwnedLiverySlots(SportsTruckAppearanceCatalog.Body body)
        {
            if (body == null || body.visualPrefab == null) throw new InvalidOperationException("The owned atlas body is missing.");
            var renderers = body.visualPrefab.GetComponentsInChildren<Renderer>(true); var slots = new List<SportsTruckAppearanceCompatibility.PaintSlot>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer.name != LiveryPart || renderer.transform.parent != body.visualPrefab.transform || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                for (int k = 0; k < renderer.sharedMaterials.Length; k++)
                {
                    var material = renderer.sharedMaterials[k]; if (material == null || material.name != LiveryMaterial) continue;
                    slots.Add(new SportsTruckAppearanceCompatibility.PaintSlot { rendererIndex = i, materialIndex = k });
                }
            }
            if (slots.Count == 0) throw new InvalidOperationException("Run AddMissingAtlasLivery before this explicit paint-slot upgrade: " + body.id);
            return slots.ToArray();
        }
        static void RefreshLivery(string path, SportsTruckAppearanceCatalog catalog, Dictionary<string, SportsTruckAppearanceCompatibility.PaintSlot[]> ownedSlots)
        {
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing scene edits first: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                bool changed = false; int upgraded = 0, preserved = 0;
                var roots = scene.GetRootGameObjects();
                foreach (var host in roots.SelectMany(root => root.GetComponentsInChildren<SportsTruckAppearanceHost>(true)))
                {
                    // The old initial slice predates an authoring marker. Match
                    // its exact saved source bindings conservatively; never
                    // guess at a custom catalog or partial equipment binding.
                    if (!OwnedHostSignature(host, catalog)) { preserved++; continue; }
                    foreach (var fit in host.compatibleBodies.Where(fit => fit != null && (fit.bodyId == "truck.fire" || fit.bodyId == "novelty.icecream")))
                    {
                        if (fit.notice != "Visual fit uses this sport's existing four wheel positions and balanced chassis." && fit.notice != "This sport temporarily uses its standardized original rolling wheels.")
                        { preserved++; continue; }
                        var slots = new List<SportsTruckAppearanceCompatibility.PaintSlot>(fit.paintSlots ?? Array.Empty<SportsTruckAppearanceCompatibility.PaintSlot>());
                        // Preserve all custom/previous slots. Only append exact
                        // owned surfaces, never broaden an atlas paint target.
                        bool added = false;
                        foreach (var owned in ownedSlots[fit.bodyId])
                        {
                            if (slots.Any(slot => slot.rendererIndex == owned.rendererIndex && slot.materialIndex == owned.materialIndex)) continue;
                            slots.Add(owned); added = true;
                        }
                        if (!added) continue;
                        fit.paintSlots = slots.ToArray(); EditorUtility.SetDirty(host); changed = true; upgraded++;
                    }
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                Debug.Log("SPORTS_OWNED_LIVERY_PAINT_SLOTS " + path + " upgradedFits=" + upgraded + " preservedCustomBindings=" + preserved + "; all pose, wheel, notice and existing paint fields retained.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        static bool OwnedHostSignature(SportsTruckAppearanceHost host, SportsTruckAppearanceCatalog catalog)
        {
            if (host.catalog != catalog || host.presentationParent == null || host.presentationParent == host.transform || host.compatibleBodies == null || host.compatibleBodies.Length != catalog.bodies.Length) return false;
            if (!new HashSet<string>(host.compatibleBodies.Where(fit => fit != null).Select(fit => fit.bodyId)).SetEquals(catalog.bodies.Select(body => body.id))) return false;
            var truck = host.GetComponent<SportsEventTruck>(); var bowling = host.GetComponent<BowlingTruck>();
            Transform presentation; WheelController[] wheels; SportsWheelMeshBinding meshBinding;
            if (truck != null && truck.appearanceHost == host && truck.session != null && (truck.session.appearanceHosts ?? Array.Empty<SportsTruckAppearanceHost>()).Contains(host))
            { presentation = truck.visuals; wheels = truck.wheels; meshBinding = truck.GetComponent<SportsWheelMeshBinding>(); }
            else if (bowling != null && bowling.appearanceHost == host)
            { presentation = bowling.exterior.transform; wheels = bowling.wheels; meshBinding = null; }
            else return false;
            if (host.presentationParent != presentation || wheels == null || host.wheels == null || !host.wheels.SequenceEqual(wheels)) return false;
            var rolling = WheelRenderers(host.transform, wheels, meshBinding);
            var original = presentation.GetComponentsInChildren<Renderer>(true).Where(renderer => !rolling.Contains(renderer));
            return new HashSet<Renderer>(rolling).SetEquals(host.originalWheelRenderers ?? Array.Empty<Renderer>()) && new HashSet<Renderer>(original).SetEquals(host.originalBodyRenderers ?? Array.Empty<Renderer>());
        }
        static void BindClean(string path, SportsTruckAppearanceCatalog catalog)
        {
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing scene edits first: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var match = roots.SelectMany(root => root.GetComponentsInChildren<SportsEventSession>(true)).Single();
                SportsEventTruck[] trucks; Transform[] protectedRoots;
                var buz = match as BuzMatch; var fishing = match as FishingMatch; var dance = match as DanceMatch;
                if (buz != null)
                {
                    trucks = buz.trucks;
                    if (trucks.Length != 6 || buz.cab == null || buz.cab.interiors.Length != 4 || buz.cab.views.Length != 4 || buz.cargo == null || buz.rope == null)
                        throw new InvalidOperationException("Existing Buzkashi actor, equipment and cab bindings required.");
                    protectedRoots = new[] { buz.cargo.transform, buz.rope.transform }.Concat(buz.floppy.Select(body => body.transform)).Concat(buz.cab.interiors).ToArray();
                    for (int i = 0; i < 4; i++) if (buz.cab.interiors[i] == null || buz.cab.views[i] == null || buz.cab.views[i].GetComponent<BowlingCabCamera>() == null)
                        throw new InvalidOperationException("Existing Buzkashi per-camera interior visibility required.");
                }
                else if (fishing != null)
                {
                    if (fishing.streams == null || fishing.streams.Length != 4 || fishing.streams.Any(stream => stream == null || stream.truck == null || stream.net == null || stream.netBag == null))
                        throw new InvalidOperationException("Existing four Fishing stream/net/truck bindings required.");
                    trucks = fishing.streams.Select(stream => stream.truck).ToArray();
                    protectedRoots = fishing.streams.SelectMany(stream => new[] { stream.net, stream.netBag }).ToArray();
                }
                else if (dance != null)
                {
                    if (dance.dancers == null || dance.dancers.Length != 4 || dance.dancers.Any(dancer => dancer == null || dancer.truck == null || dancer.deck == null))
                        throw new InvalidOperationException("Existing four Dance truck/deck bindings required.");
                    trucks = dance.dancers.Select(dancer => dancer.truck).ToArray();
                    protectedRoots = dance.dancers.SelectMany(dancer => new[] { dancer.deck, dancer.accent != null ? dancer.accent.transform : null, dancer.sparks != null ? dancer.sparks.transform : null }).Where(root => root != null).ToArray();
                }
                else throw new InvalidOperationException("This explicit entry supports only Buzkashi, Fishing and Dance Off.");
                if (trucks.Distinct().Count() != trucks.Length) throw new InvalidOperationException("Each participant must own one distinct existing truck.");
                // Validate the complete venue before adding components. No whole
                // root replacement or equipment inside a supposedly clean body.
                var rolling = new Renderer[trucks.Length][];
                for (int i = 0; i < trucks.Length; i++)
                {
                    var truck = trucks[i];
                    if (truck == null || truck.session != match || truck.slot != i) throw new InvalidOperationException("Stable actor-to-participant ownership required.");
                    CheckVehicle(truck.transform, truck.visuals, truck.wheels);
                    if (protectedRoots.Any(root => root == truck.visuals || root.IsChildOf(truck.visuals) || truck.visuals.IsChildOf(root)))
                        throw new InvalidOperationException("Required equipment overlaps this clean-model appearance subtree.");
                    rolling[i] = WheelRenderers(truck.transform, truck.wheels, truck.GetComponent<SportsWheelMeshBinding>());
                    var old = truck.GetComponents<SportsTruckAppearanceHost>();
                    if (old.Length > 1 || truck.appearanceHost != null && (old.Length != 1 || truck.appearanceHost != old[0]))
                        throw new InvalidOperationException("Conflicting custom appearance bindings are preserved; resolve them explicitly.");
                }
                bool changed = false;
                var hosts = new List<SportsTruckAppearanceHost>(match.appearanceHosts ?? Array.Empty<SportsTruckAppearanceHost>());
                for (int i = 0; i < trucks.Length; i++)
                {
                    var truck = trucks[i]; var host = truck.GetComponent<SportsTruckAppearanceHost>();
                    if (host == null)
                    {
                        host = Create(truck.transform, truck.visuals, truck.wheels, rolling[i], catalog);
                        host.participantSlot = truck.slot; host.protectedRoots = protectedRoots; EditorUtility.SetDirty(host); changed = true;
                    }
                    if (truck.appearanceHost == null) { truck.appearanceHost = host; EditorUtility.SetDirty(truck); changed = true; }
                    if (!hosts.Contains(host)) { hosts.Add(host); changed = true; }
                }
                if (buz != null) for (int i = 0; i < 4; i++)
                {
                    var interior = buz.cab.interiors[i]; var old = interior.GetComponent<SportsTruckAppearanceCabBinding>();
                    if (old != null) continue; // Preserve existing custom cab mounts and owner configuration.
                    var binding = Cab(interior); binding.visibility = buz.cab.views[i].GetComponent<BowlingCabCamera>(); EditorUtility.SetDirty(binding); changed = true;
                }
                var framings = roots.SelectMany(root => root.GetComponentsInChildren<SportsBroadcastFraming>(true)).Where(framing => framing.session == match).ToArray();
                if (match.appearanceFraming == null || match.appearanceFraming.Length == 0)
                { if (framings.Length > 0) { match.appearanceFraming = framings; changed = true; } }
                if (!changed) { Debug.Log("SPORTS_APPEARANCE_CLEAN_BINDINGS_PRESERVED " + path); return; }
                match.appearanceHosts = hosts.ToArray(); EditorUtility.SetDirty(match);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_APPEARANCE_CLEAN_HOSTS_BOUND " + path + " participants=" + trucks.Length + " existing physics, equipment and cab ownership retained; native/player validation pending.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        static void Bind(string path, SportsTruckAppearanceCatalog catalog, bool parking)
        {
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save existing scene edits first: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                if (roots.SelectMany(root => root.GetComponentsInChildren<SportsTruckAppearanceHost>(true)).Any())
                { Debug.Log("SPORTS_APPEARANCE_BINDINGS_PRESERVED " + path); return; }
                if (parking)
                {
                    var match = roots.SelectMany(root => root.GetComponentsInChildren<ParkingMatch>(true)).Single();
                    var views = match.GetComponent<SportsDrivingViews>();
                    if (views == null || views.cabs.Length != 4) throw new InvalidOperationException("Existing Parking cab/view adapter required.");
                    foreach (var truck in match.trucks) CheckVehicle(truck.transform, truck.visuals, truck.wheels);
                    var hosts = new List<SportsTruckAppearanceHost>();
                    foreach (var truck in match.trucks)
                    {
                        var rolling = WheelRenderers(truck.transform, truck.wheels, truck.GetComponent<SportsWheelMeshBinding>());
                        var host = Create(truck.transform, truck.visuals, truck.wheels, rolling, catalog);
                        host.participantSlot = truck.slot; truck.appearanceHost = host; hosts.Add(host); EditorUtility.SetDirty(truck);
                    }
                    match.appearanceHosts = hosts.ToArray(); EditorUtility.SetDirty(match);
                    for (int i = 0; i < views.cabs.Length; i++)
                    { var binding = Cab(views.cabs[i]); binding.drivingViews = views; binding.viewIndex = i; EditorUtility.SetDirty(binding); }
                }
                else
                {
                    var match = roots.SelectMany(root => root.GetComponentsInChildren<BowlingMatch>(true)).Single();
                    foreach (var lane in match.lanes)
                    {
                        CheckVehicle(lane.truck.transform, lane.truck.exterior.transform, lane.truck.wheels);
                        if (lane.cab.GetComponent<SportsLegacyCabLook>()?.cockpit == null || lane.highlights?.replay == null)
                            throw new InvalidOperationException("Existing canonical Bowling cab and replay bindings required.");
                    }
                    foreach (var lane in match.lanes)
                    {
                        var truck = lane.truck; var rolling = WheelRenderers(truck.transform, truck.wheels, null);
                        truck.appearanceHost = Create(truck.transform, truck.exterior.transform, truck.wheels, rolling, catalog); EditorUtility.SetDirty(truck);
                        var binding = Cab(lane.cab.GetComponent<SportsLegacyCabLook>().cockpit); binding.bowlingLane = lane; EditorUtility.SetDirty(binding);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("SPORTS_APPEARANCE_HOSTS_BOUND " + path + " existing physics, equipment, replay roots and cab owners retained; native/player validation pending.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        static void CheckVehicle(Transform root, Transform presentation, WheelController[] wheels)
        {
            if (presentation == null || presentation == root || !presentation.IsChildOf(root)) throw new InvalidOperationException("Explicit rendering subtree required; full tractor root replacement is forbidden.");
            if (wheels == null || wheels.Length != 4 || wheels.Any(wheel => wheel == null || wheel.wheel.rotatingContainer == null))
                throw new InvalidOperationException("Four already-authored NWH rotating visual containers required; author the original sport wheel repair first.");
            if (presentation.GetComponentsInChildren<Renderer>(true).Length == 0) throw new InvalidOperationException("Original presentation has no rendering.");
        }
        static Renderer[] WheelRenderers(Transform root, WheelController[] wheels, SportsWheelMeshBinding meshBinding)
        {
            var set = new HashSet<Renderer>();
            foreach (var wheel in wheels) foreach (var renderer in wheel.wheel.rotatingContainer.GetComponentsInChildren<Renderer>(true)) set.Add(renderer);
            if (meshBinding != null) foreach (var mesh in meshBinding.meshes) if (mesh != null) foreach (var renderer in mesh.GetComponentsInChildren<Renderer>(true)) set.Add(renderer);
            if (set.Count == 0 || set.Any(renderer => !renderer.transform.IsChildOf(root))) throw new InvalidOperationException("Explicit original rolling renderer references missing.");
            return set.ToArray();
        }
        static SportsTruckAppearanceHost Create(Transform root, Transform presentation, WheelController[] wheels, Renderer[] rolling, SportsTruckAppearanceCatalog catalog)
        {
            var original = presentation.GetComponentsInChildren<Renderer>(true).Where(renderer => !rolling.Contains(renderer)).ToArray();
            if (original.Length == 0) throw new InvalidOperationException("No distinct body presentation renderers.");
            var bounds = LocalBounds(root, original.Concat(rolling));
            var fits = catalog.bodies.Select(body => Fit(body, bounds, root, wheels)).ToArray();
            var host = root.gameObject.AddComponent<SportsTruckAppearanceHost>(); host.catalog = catalog; host.presentationParent = presentation;
            host.originalBodyRenderers = original; host.originalWheelRenderers = rolling; host.wheels = wheels; host.compatibleBodies = fits;
            // Both first-slice venues keep their real interiors outside the exterior subtree.
            host.protectedRoots = Array.Empty<Transform>(); EditorUtility.SetDirty(host); return host;
        }
        static SportsTruckAppearanceCompatibility Fit(SportsTruckAppearanceCatalog.Body body, Bounds target, Transform root, WheelController[] wheels)
        {
            var rotation = Quaternion.Euler(body.presentationEuler); var envelope = Rotated(body.visualEnvelope, rotation);
            float scale = Mathf.Min(target.size.x / envelope.size.x, target.size.y / envelope.size.y, target.size.z / envelope.size.z);
            var fit = new SportsTruckAppearanceCompatibility { bodyId = body.id, scale = Vector3.one * scale,
                position = new Vector3(target.center.x - envelope.center.x * scale, target.min.y - envelope.min.y * scale, target.center.z - envelope.center.z * scale) };
            var renderers = body.visualPrefab.GetComponentsInChildren<Renderer>(true);
            var slots = new List<SportsTruckAppearanceCompatibility.PaintSlot>();
            for (int i = 0; i < renderers.Length; i++) for (int k = 0; k < renderers[i].sharedMaterials.Length; k++)
            {
                var material = renderers[i].sharedMaterials[k];
                if (SportsTruckAppearanceCompatibility.IsPaintMaterial(body.id, material))
                    slots.Add(new SportsTruckAppearanceCompatibility.PaintSlot { rendererIndex = i, materialIndex = k });
            }
            fit.paintSlots = slots.ToArray();
            var mounts = body.wheelMounts ?? Array.Empty<SportsTruckAppearanceCatalog.Mount>();
            if (mounts.Length < 4) { fit.notice = "This sport temporarily uses its standardized original rolling wheels."; return fit; }
            var points = mounts.Select(mount => rotation * mount.position).ToArray();
            var tireNames = new HashSet<string>(body.wheelRendererNames ?? Array.Empty<string>(), StringComparer.Ordinal);
            var unused = new HashSet<int>(Enumerable.Range(0, mounts.Length)); var mapped = new List<SportsTruckAppearanceCompatibility.WheelGeometry>();
            var physical = wheels.Select(wheel => root.InverseTransformPoint(wheel.transform.position)).ToArray();
            for (int i = 0; i < wheels.Length; i++)
            {
                // Author once into serialized indices; runtime never guesses wheel names or order.
                var goal = Normalize(physical[i], physical); int nearest = unused.OrderBy(index => (Normalize(points[index], points) - goal).sqrMagnitude).First(); unused.Remove(nearest);
                var indices = new List<int>();
                for (int k = 0; k < renderers.Length; k++)
                {
                    if (!tireNames.Contains(renderers[k].name)) continue;
                    var center = body.visualPrefab.transform.InverseTransformPoint(renderers[k].bounds.center);
                    int group = Enumerable.Range(0, mounts.Length).OrderBy(index => (mounts[index].position - center).sqrMagnitude).First();
                    if (group == nearest) indices.Add(k);
                }
                if (indices.Count == 0) { fit.notice = "This sport temporarily uses its standardized original rolling wheels."; return fit; }
                float physicalDiameter = wheels[i].wheel.radius * 2;
                bool large = body.id == "truck.monster" || body.id == "truck.lifted";
                mapped.Add(new SportsTruckAppearanceCompatibility.WheelGeometry { physicalWheel = i, rendererIndices = indices.ToArray(), center = mounts[nearest].position,
                    euler = (rotation * Quaternion.Euler(mounts[nearest].euler)).eulerAngles, diameter = mounts[nearest].scale.x,
                    visualDiameter = Mathf.Clamp(mounts[nearest].scale.x * scale, physicalDiameter * (large ? 1.15f : .8f), physicalDiameter * (large ? 1.5f : 1.1f)) });
            }
            fit.wheelGeometry = mapped.ToArray();
            fit.notice = "Visual fit uses this sport's existing four wheel positions and balanced chassis.";
            return fit;
        }
        static Vector3 Normalize(Vector3 point, Vector3[] all) => new Vector3(Mathf.InverseLerp(all.Min(v => v.x), all.Max(v => v.x), point.x), 0, Mathf.InverseLerp(all.Min(v => v.z), all.Max(v => v.z), point.z));
        static Bounds Rotated(Bounds value, Quaternion rotation)
        {
            var result = new Bounds(rotation * value.center, Vector3.zero);
            for (int i = 0; i < 8; i++) result.Encapsulate(rotation * (value.center + Vector3.Scale(value.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return result;
        }
        static Bounds LocalBounds(Transform root, IEnumerable<Renderer> renderers)
        {
            bool first = true; Bounds result = default;
            foreach (var renderer in renderers)
            {
                var bounds = renderer.localBounds; var matrix = root.worldToLocalMatrix * renderer.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var point = matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (first) { result = new Bounds(point, Vector3.zero); first = false; } else result.Encapsulate(point);
                }
            }
            if (first || result.size.x < .001f || result.size.y < .001f || result.size.z < .001f) throw new InvalidOperationException("Original sport visual envelope missing.");
            return result;
        }
        static SportsTruckAppearanceCabBinding Cab(Transform interior)
        {
            var old = interior.GetComponent<SportsTruckAppearanceCabBinding>(); if (old != null) return old;
            var mount = new GameObject("Sports profile dashboard mount").transform; mount.SetParent(interior, false);
            mount.gameObject.layer = interior.gameObject.layer; mount.localPosition = new Vector3(.16f, -.22f, .48f);
            var binding = interior.gameObject.AddComponent<SportsTruckAppearanceCabBinding>(); binding.accessoryMount = mount; return binding;
        }
    }
}
