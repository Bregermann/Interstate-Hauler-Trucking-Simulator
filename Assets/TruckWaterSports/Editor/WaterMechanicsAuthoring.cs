using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckWaterSports.Editor
{
    public static class WaterMechanicsAuthoring
    {
        [MenuItem("Truck Sports/Alpha 0.10/Configure active water current sections and action cues")]
        public static void ConfigureActiveWaterMechanics() => UpgradeActiveScene();

        // Explicit opt-in configuration, never a Play Mode or scene-load rebuild.
        // Existing designer sections and nonempty audio choices are preserved.
        public static int UpgradeActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring water mechanics.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckWaterSports/Scenes/PaddlePals.unity" && scene.path != "Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity" && scene.path != "Assets/TruckWaterSports/Scenes/Blackwater.unity")
                throw new InvalidOperationException("Open an existing saved Alpha 0.10 water venue.");
            var match = UnityEngine.Object.FindAnyObjectByType<WaterMatch>();
            if (match == null || match.gameObject.scene != scene || match.course == null || match.feedback == null)
                throw new InvalidOperationException("Existing match, course and shared feedback service required.");
            var audio = match.GetComponent<WaterAudio>();
            if (audio == null) throw new InvalidOperationException("Preserve the venue's existing WaterAudio ambience adapter.");
            string[] paths = {
                "Assets/TruckFishing/Audio/Small bright water plop.wav",
                "Assets/WeatherMaker/Prefab/Sound/Water/WeatherMakerWaterSound_Splash.ogg",
                "Assets/TruckFishing/Audio/Missed fish ripple.wav",
                "Assets/NWH/Vehicle Physics 2/Sound/Crash/Crash-01.wav"
            };
            var clips = new AudioClip[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
                if (clips[i] == null) throw new InvalidOperationException("Required existing owned action sound missing: " + paths[i]);
            }
            int changed = 0;
            if (!match.course.lake && (match.course.currentSections == null || match.course.currentSections.Length == 0))
            {
                if (match.course.stations == null || match.course.stations.Length < 21)
                    throw new InvalidOperationException("Preserve the full authored river before assigning its current sections.");
                match.course.currentSections = new[] {
                    Section("Calm introductory pool",0,1,.4f,.25f),
                    Section("Rocky upper rapids",4,7,1.1f,1.25f),
                    Section("Recovery pool below first waterfall",11,12,.3f,.2f),
                    Section("Technical lower rapids",14,15,1.08f,1.2f),
                    Section("Recovery pool below large waterfall",18,19,.3f,.2f)
                };
                EditorUtility.SetDirty(match.course); changed++;
            }
            var assigned = match.feedback.clips ?? Array.Empty<AudioClip>();
            if (assigned.Length < 11) { Array.Resize(ref assigned, 11); changed++; }
            for (int i = 0; i < clips.Length; i++) if (assigned[i + 7] == null) { assigned[i + 7] = clips[i]; changed++; }
            match.feedback.clips = assigned;
            if (audio.paddleFeedbackIndex < 0) { audio.paddleFeedbackIndex = 7; changed++; }
            if (audio.capsizeFeedbackIndex < 0) { audio.capsizeFeedbackIndex = 8; changed++; }
            if (audio.recoveryFeedbackIndex < 0) { audio.recoveryFeedbackIndex = 9; changed++; }
            if (audio.impactFeedbackIndex < 0) { audio.impactFeedbackIndex = 10; changed++; }
            if (changed > 0)
            {
                EditorUtility.SetDirty(match.feedback); EditorUtility.SetDirty(audio); EditorSceneManager.MarkSceneDirty(scene);
            }
            Debug.Log("Water mechanics configured: " + changed + " changes; existing custom sections/cues retained. Current sections affect real WaterCourse forces. Save explicitly; changed profiles require focused gameplay acceptance.");
            return changed;
        }

        static WaterCurrentSection Section(string name, int first, int last, float speed, float turbulence)
            => new WaterCurrentSection { name = name, firstSegment = first, lastSegment = last, speedMultiplier = speed, turbulenceMultiplier = turbulence, edgeBlendMetres = 6 };
    }
}
