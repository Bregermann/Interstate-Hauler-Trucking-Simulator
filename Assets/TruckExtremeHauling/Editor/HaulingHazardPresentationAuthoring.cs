using System;
using System.Collections.Generic;
using System.Linq;
using LWS.TruckSports;
using LWS.TruckSports.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LWS.TruckExtremeHauling.Editor
{
    public static class HaulingHazardPresentationAuthoring
    {
        const string Stress = "Assets/NWH/Vehicle Physics 2/Sound/Suspension/SuspensionThump.wav";
        const string Machinery = "Assets/NWH/Vehicle Physics 2/Sound/Transmission/TransmissionWhine.wav";
        const string Alarm = "Assets/NWH/Vehicle Physics 2/Sound/ReverseBeep/ReverseBeep.wav";
        const string Splash = "Assets/WeatherMaker/Prefab/Sound/Water/WeatherMakerWaterSound_Splash.ogg";
        const string ParticleSource = "Assets/NWH/Vehicle Physics 2/Effects/Prefabs/ExhaustSmoke.prefab";
        const string IceChipMaterial = "Assets/TruckSkyTow/Materials/Arctic gate snow chips.mat";
        const string Folder = "Assets/TruckExtremeHauling/HazardPresentation";

        [MenuItem("Truck Sports/Alpha 0.10/Install active ice and cargo hazard presentation")]
        public static void InstallActivePresentation() => UpgradeActiveScene();

        public static HaulingHazardPresentation UpgradeActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring hazard presentation.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckExtremeHauling/Scenes/CargoCrisis.unity" && scene.path != "Assets/TruckExtremeHauling/Scenes/IceRoadReckoning.unity")
                throw new InvalidOperationException("Open the existing saved CargoCrisis or IceRoadReckoning venue.");
            var match = UnityEngine.Object.FindObjectsByType<ExtremeHaulingMatch>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m.gameObject.scene == scene);
            if (match == null || match.feedback == null) throw new InvalidOperationException("Preserve the existing match and shared feedback pool.");
            var existing = match.GetComponent<HaulingHazardPresentation>(); if (existing != null) return existing;
            var stress = Need<AudioClip>(Stress); var machinery = Need<AudioClip>(Machinery); var alarm = Need<AudioClip>(Alarm); var splash = Need<AudioClip>(Splash);
            var thin = match as ThinIceMatch; var cargo = match as CargoCrisisMatch;
            if (thin != null && (thin.rescue == null || thin.rescue.cargoFloat == null)) throw new InvalidOperationException("Install the original saved Convoy Rescue equipment first.");
            if (cargo != null && cargo.emergencySites?.floodedLoading?.environment == null) throw new InvalidOperationException("Install Cargo Crisis's saved flooded loading incident first.");
            var chipMaterial = thin != null ? Need<Material>(IceChipMaterial) : null;
            if (thin != null) Need<GameObject>(ParticleSource);
            EnsureFolder(Folder);
            var presenter = match.gameObject.AddComponent<HaulingHazardPresentation>(); presenter.match = match; presenter.thinIce = thin; presenter.cargo = cargo;
            presenter.stressClip = Append(match.feedback, stress); presenter.releaseClip = presenter.stressClip;
            presenter.winchClip = Append(match.feedback, machinery); presenter.heatAlarmClip = Append(match.feedback, alarm);
            var art = new GameObject("Actual hauling hazard presentation · no gameplay authority").transform; art.SetParent(match.transform, false);
            Transform water; Rigidbody[] bodies; Bounds bounds;
            if (thin != null)
            {
                var cracks = new List<LineRenderer>(); var crackMaterial = CrackMaterial();
                for (int i = 0; i < thin.ice.Length; i++)
                {
                    var b = thin.ice[i].surface.bounds;
                    cracks.Add(Crack(art, "Actual tile stress fissure " + i, b, crackMaterial, false, i));
                    cracks.Add(Crack(art, "Actual tile stress branch " + i, b, crackMaterial, true, i));
                }
                presenter.crackLines = cracks.ToArray(); presenter.iceChips = new ParticleSystem[4];
                for (int i = 0; i < presenter.iceChips.Length; i++) presenter.iceChips[i] = Chips(art, chipMaterial, i);
                water = thin.rescue.equipment.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Actual lake water below fractured ice sheets");
                bodies = match.trucks.Select(t => t.body).Concat(new[] { thin.rescue.strandedCargo.Body }).ToArray(); bounds = thin.rescue.waterBounds;
            }
            else
            {
                var flood = cargo.emergencySites.floodedLoading;
                var waterRenderers = flood.environment.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("River Modeler flooded loading-bay water ", StringComparison.Ordinal)).ToArray();
                if (waterRenderers.Length != 4) throw new InvalidOperationException("The four actual saved flooded-bay surfaces are required.");
                // One organizational surface reference lets the shared presentation pool
                // see all four saved bays. Geometry/world poses remain unchanged.
                var reference = new GameObject("Actual flooded-bay water presentation reference").transform;
                reference.SetParent(flood.environment.transform, false); reference.position = new Vector3(0, -.55f, 0);
                foreach (var renderer in waterRenderers) renderer.transform.SetParent(reference, true);
                water = reference; bodies = match.trucks.Select(t => t.body).Concat(flood.loads.Select(t => t.Body)).ToArray();
                bounds = flood.floodedVolumes[0]; foreach (var bay in flood.floodedVolumes) bounds.Encapsulate(bay);
            }
            if (water == null) throw new InvalidOperationException("The actual saved water renderer/reference must remain available.");
            // Reuse the established River Modeler/NWH spray, foam and sound pools.
            // The presenter enables them only for the actual selected rescue/flood site.
            var priorCameraEffects = new HashSet<LWS.TruckBogSnorkelling.BogCameraEffects>(UnityEngine.Object.FindObjectsByType<LWS.TruckBogSnorkelling.BogCameraEffects>(FindObjectsInactive.Include,FindObjectsSortMode.None));
            var waterFx = SportsWaterAuthoring.InstallForBodies(match, water, bodies, bounds);
            // Only newly added water-local camera adapters follow this selection gate;
            // preserve any independently authored original camera components.
            presenter.selectedWaterCameraEffects = UnityEngine.Object.FindObjectsByType<LWS.TruckBogSnorkelling.BogCameraEffects>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(e=>!priorCameraEffects.Contains(e)).ToArray();
            foreach(var cameraEffect in presenter.selectedWaterCameraEffects){cameraEffect.enabled=false;EditorUtility.SetDirty(cameraEffect);}
            foreach (var audio in waterFx.audio) { audio.clip = splash; audio.loop = false; EditorUtility.SetDirty(audio); }
            waterFx.enabled = false; presenter.waterEffects = waterFx;
            EditorUtility.SetDirty(presenter); EditorUtility.SetDirty(match.feedback); EditorSceneManager.MarkSceneDirty(scene);
            Undo.RegisterCreatedObjectUndo(art.gameObject, "Add actual hazard presentation");
            Debug.Log("HAULING_HAZARD_PRESENTATION_AUTHORED: real ice Stress/Breakthrough read-only fissures and pooled chips; existing positional feedback for loaded-suspension warnings, powered-winch gear and hot-zone warning beeps; shared actual-body River Modeler water FX with owned Weather Maker splash. Dedicated authentic ice-crack/fire-crackle recordings remain unavailable. Caller saves and validates; listening unverified.");
            return presenter;
        }
        static int Append(SportsFeedback feedback, AudioClip clip)
        {
            var clips = feedback.clips != null ? new List<AudioClip>(feedback.clips) : new List<AudioClip>();
            int existing = clips.IndexOf(clip); if (existing >= 0) return existing;
            clips.Add(clip); feedback.clips = clips.ToArray(); return clips.Count - 1;
        }
        static Material CrackMaterial()
        {
            string path = Folder + "/Actual loaded ice fissure.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Unlit"); if (shader == null) throw new InvalidOperationException("Existing URP Unlit shader required.");
            material = new Material(shader) { name = "Actual loaded ice fissure", color = new Color(.006f, .035f, .055f) }; AssetDatabase.CreateAsset(material, path); return material;
        }
        static LineRenderer Crack(Transform parent, string name, Bounds bounds, Material material, bool branch, int seed)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.sharedMaterial = material; line.useWorldSpace = true; line.positionCount = branch ? 5 : 9;
            line.startWidth = line.endWidth = 1; line.widthMultiplier = .015f; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                float x = branch ? Mathf.Lerp(0, -.4f, t) : Mathf.Lerp(-.42f, .42f, t);
                float z = branch ? Mathf.Lerp(0, .4f, t) : Mathf.Lerp(-.4f, .4f, t);
                x += Mathf.Sin((i + seed * .31f) * 2.7f) * .035f; z += Mathf.Cos((i + seed * .23f) * 3.4f) * .035f;
                line.SetPosition(i, new Vector3(bounds.center.x + x * bounds.size.x, bounds.max.y + .024f, bounds.center.z + z * bounds.size.z));
            }
            line.enabled = false; return line;
        }
        static ParticleSystem Chips(Transform parent, Material material, int index)
        {
            var go = UnityEngine.Object.Instantiate(Need<GameObject>(ParticleSource)); go.name = "Pooled actual ice breakthrough chips " + index;
            go.transform.SetParent(parent, false); go.transform.rotation = Quaternion.Euler(-90, 0, 0);
            var particles = go.GetComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 128; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.5f); main.startSize = new ParticleSystem.MinMaxCurve(.08f, .28f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 5);
            main.startColor = new Color(.68f, .88f, 1); main.gravityModifier = 1.2f;
            var emission = particles.emission; emission.enabled = true; emission.rateOverTime = 0; emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
            var shape = particles.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 65; shape.radius = 4;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material; return particles;
        }
        static T Need<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Installed owned dependency missing: " + path);
        static void EnsureFolder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; int i = path.LastIndexOf('/'); string parent = path.Substring(0, i); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(i + 1)); }
    }
}
