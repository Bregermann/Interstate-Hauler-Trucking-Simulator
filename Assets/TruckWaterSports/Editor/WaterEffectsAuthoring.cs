using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LWS.TruckSports;
using LWS.TruckSports.Editor;

namespace LWS.TruckWaterSports.Editor
{
    public static class WaterEffectsAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Install active water venue splash and wake pools")]
        public static void InstallActiveWaterEffects() => InstallActiveScene();

        // Explicit saved-scene authoring only. The existing shared emitter owns
        // bounded pools, actual-body entry/wake events and audio; Archimeds
        // retains all displacement and buoyancy physics.
        public static SportsWaterInteraction InstallActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring water effects.");
            Scene scene = SceneManager.GetActiveScene();
            Type expected;
            switch (scene.path)
            {
                case "Assets/TruckWaterSports/Scenes/PaddlePals.unity": expected = typeof(PaddlePalsMatch); break;
                case "Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity": expected = typeof(WhitewaterWipeoutMatch); break;
                case "Assets/TruckWaterSports/Scenes/Blackwater.unity": expected = typeof(BlackwaterMatch); break;
                default: throw new InvalidOperationException("Open a saved PaddlePals, WhitewaterWipeout or Blackwater scene.");
            }

            var match = UnityEngine.Object.FindObjectsByType<WaterMatch>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SingleOrDefault(candidate => candidate.gameObject.scene == scene && candidate.GetType() == expected);
            if (match == null || match.course == null || match.course.stations == null || match.course.stations.Length < 2)
                throw new InvalidOperationException("The saved venue requires its original water match and authored course.");
            if (match.vessels == null || match.vessels.Length == 0 || match.vessels.Any(vessel => vessel == null || vessel.body == null))
                throw new InvalidOperationException("Every authored watercraft must retain its actual vessel Rigidbody.");

            var renderer = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SingleOrDefault(candidate => candidate.gameObject.scene == scene && candidate.name == "Owned River Modeler shaded flowing surface");
            if (renderer == null || renderer.GetComponent<MeshFilter>()?.sharedMesh == null)
                throw new InvalidOperationException("The existing rendered River Modeler water surface is required; no replacement surface is generated.");

            var bounds = renderer.bounds;
            if (bounds.size.x <= 0 || bounds.size.z <= 0)
                throw new InvalidOperationException("The saved water surface has no usable horizontal footprint.");
            var interaction = SportsWaterAuthoring.InstallForBodies(match, renderer.transform,
                match.vessels.Select(vessel => vessel.body).ToArray(), bounds, match.course);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Water venue effects configured for " + scene.name + ": existing River Modeler splash art, NWH particle pools, foam rings and shared water audio; course height sampled per body. Save the scene explicitly.");
            return interaction;
        }
    }
}
