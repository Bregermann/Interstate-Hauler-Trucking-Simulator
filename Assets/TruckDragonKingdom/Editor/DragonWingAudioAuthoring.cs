#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MalbersAnimations.Utilities;
using UnityEditor;
using UnityEngine;

namespace LWS.TruckDragonKingdom.Editor
{
    public static class DragonWingAudioAuthoring
    {
        public const string PrivateRoot = "Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/MalbersWings/";
        const string EmitterName = "Owned Malbers wing sound";
        [Serializable] sealed class MotionAudit
        {
            public string species, action, clip, leftTip, rightTip;
            public float leftExcursion, rightExcursion;
            public int originalAnimationEvents;
            public string[] originalEventFunctions;
            public bool supportsStrongBilateralStroke;
        }
        [Serializable] sealed class Receipt
        {
            public string scope = "Explicit saved configured-prefab wing audio setup; sampled real rig motion, not native playback/listening acceptance. Vendor audio source/clip bytes unchanged.";
            public string[] savedPrefabs;
            public MotionAudit[] actualWingMotion;
        }
        static readonly List<MotionAudit> motion = new List<MotionAudit>();

        // Explicit Foundation hook AFTER configured animals exist. Existing
        // Actor nested prefab references inherit it; no actor/core source edit.
        public static void InstallConfiguredWingAudio()
        {
            Guard(); motion.Clear();
            var catalog = AssetDatabase.LoadAssetAtPath<DragonSpeciesCatalog>(DragonSpeciesAuthoring.CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Actual saved species catalog required.");
            RequireAudio(); var saved = new List<string>();
            foreach (var species in catalog.species.Where(item => item != null && item.Supports(DragonSpeciesRole.Flying)))
            {
                string path = "Assets/TruckDragonKingdom/Prefabs/Dragons/" + species.id + "/" + species.id + " Dragon Animal.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new InvalidOperationException("Actual configured prefab required: " + path);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = InstallOnConfiguredInstance(contents, species);
                    if (changed) PrefabUtility.SaveAsPrefabAsset(contents, path);
                    saved.Add(path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            string output = Output("WingAudio/configured-wing-audio.json");
            File.WriteAllText(output, JsonUtility.ToJson(new Receipt { savedPrefabs = saved.ToArray(), actualWingMotion = motion.ToArray() }, true) + "\n");
            Debug.Log("Owned wing audio explicitly configured: " + saved.Count + "; receipt=" + output + ". Native playback/listening pending.");
        }

        public static bool InstallOnConfiguredInstance(GameObject contents, DragonSpeciesDefinition species)
        {
            Guard();
            if (contents == null || species == null) throw new ArgumentNullException();
            if (!species.Supports(DragonSpeciesRole.Flying)) return false;
            var motor = contents.GetComponentInChildren<DragonAnimalMotor>(true);
            if (motor == null || motor.animator == null) throw new InvalidOperationException("Actual configured Malbers animator required.");
            var prior = contents.GetComponentInChildren<DragonWingAudio>(true);
            if (prior != null) { prior.ValidateBindings(); return false; } // preserve selected valid customization/bytes
            if (motor.transform.Find(EmitterName) != null) throw new InvalidOperationException("Preserve conflicting authored wing emitter.");
            var audio = RequireAudio();
            Transform leftHinge = Bone(motor.transform, "RigLWing01"), rightHinge = Bone(motor.transform, "RigRWing01");
            Transform leftTip = LastWing(motor.transform, "RigLWing"), rightTip = LastWing(motor.transform, "RigRWing");
            if (leftHinge == null || rightHinge == null || leftTip == null || rightTip == null || leftTip == leftHinge || rightTip == rightHinge)
                throw new InvalidOperationException("Genuine paired source wing chain required: " + species.id);
            float excursion; var flaps = DiscoverMotion(species, out excursion);
            var emitter = new GameObject(EmitterName); emitter.transform.SetParent(motor.transform, false);
            var source = emitter.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = false;
            source.spatialBlend = 1; source.rolloffMode = AudioRolloffMode.Logarithmic; source.minDistance = 6; source.maxDistance = 110;
            source.dopplerLevel = .15f; source.priority = 140; source.volume = 0;
            var vendor = emitter.AddComponent<AnimatorEventSounds>(); vendor._audioSource = source;
            vendor.m_EventSound = new List<EventSound> { new EventSound { name = DragonWingAudio.EventName, source = source, Clips = audio, volume = 0, pitch = 1, active = true, interval = 0 } };
            var adapter = motor.gameObject.AddComponent<DragonWingAudio>(); adapter.motor = motor;
            adapter.leftHinge = leftHinge; adapter.rightHinge = rightHinge; adapter.leftTip = leftTip; adapter.rightTip = rightTip;
            adapter.vendorSounds = vendor; adapter.source = source; adapter.flappingClips = flaps;
            // Relative travel is measured from actual owned clips. Require a
            // meaningful rise and departure from crest before each sound.
            adapter.minimumRise = Mathf.Clamp(excursion * .20f, .025f, .10f);
            adapter.crestDeparture = Mathf.Clamp(excursion * .06f, .008f, .025f);
            adapter.ValidateBindings(); EditorUtility.SetDirty(adapter); return true;
        }
        static AnimationClip[] DiscoverMotion(DragonSpeciesDefinition species, out float excursion)
        {
            var result = new List<AnimationClip>(); excursion = float.PositiveInfinity;
            var sample = (GameObject)PrefabUtility.InstantiatePrefab(species.visualPrefab);
            try
            {
                sample.hideFlags = HideFlags.HideAndDontSave;
                var hingeL = Bone(sample.transform, "RigLWing01"); var hingeR = Bone(sample.transform, "RigRWing01");
                var tipL = LastWing(sample.transform, "RigLWing"); var tipR = LastWing(sample.transform, "RigRWing");
                foreach (var action in new[] { DragonAnimationAction.Fly, DragonAnimationAction.FlyIdle, DragonAnimationAction.Takeoff })
                {
                    if (!species.TryGetAnimation(action, out var mapping)) continue;
                    float lowL = float.PositiveInfinity, lowR = lowL, highL = float.NegativeInfinity, highR = highL;
                    for (int step = 0; step <= 120; step++)
                    {
                        mapping.clip.SampleAnimation(sample, mapping.clip.length * step / 120f);
                        float left = Vector3.Dot((tipL.position - hingeL.position).normalized, sample.transform.up);
                        float right = Vector3.Dot((tipR.position - hingeR.position).normalized, sample.transform.up);
                        lowL = Mathf.Min(lowL, left); highL = Mathf.Max(highL, left); lowR = Mathf.Min(lowR, right); highR = Mathf.Max(highR, right);
                    }
                    var events = AnimationUtility.GetAnimationEvents(mapping.clip);
                    float range = Mathf.Min(highL - lowL, highR - lowR);
                    motion.Add(new MotionAudit { species = species.id, action = action.ToString(), clip = AssetDatabase.GetAssetPath(mapping.clip), leftTip = tipL.name, rightTip = tipR.name, leftExcursion = highL - lowL, rightExcursion = highR - lowR, originalAnimationEvents = events.Length, originalEventFunctions = events.Select(item => item.functionName).ToArray(), supportsStrongBilateralStroke = float.IsFinite(range) && range >= .12f });
                    if (!float.IsFinite(range) || range < .12f)
                    {
                        if (action == DragonAnimationAction.Fly) throw new InvalidOperationException("Actual bilateral wing stroke not demonstrated for required forward flight: " + species.id + " " + action + " range=" + range);
                        continue; // genuinely static/glide-like idle or takeoff stays silent
                    }
                    result.Add(mapping.clip); excursion = Mathf.Min(excursion, range);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(sample); }
            if (!species.TryGetAnimation(DragonAnimationAction.Fly, out var forward) || !result.Contains(forward.clip) || !float.IsFinite(excursion))
                throw new InvalidOperationException("Genuine strong forward-flight wing stroke required; other actions are configured only when their actual motion supports a stroke.");
            return result.Distinct().ToArray();
        }
        static AudioClip[] RequireAudio()
        {
            var result = new[] { "wing-flap-Big.wav", "Wing Flap Big1.wav" }.Select(name => AssetDatabase.LoadAssetAtPath<AudioClip>(PrivateRoot + name)).ToArray();
            if (result.Any(clip => clip == null)) throw new InvalidOperationException("Run exact private ImportDragonWingAudio.py --apply and native asset import first; actual owned adult wing takes required.");
            return result;
        }
        static Transform Bone(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).SingleOrDefault(item => item.name == name);
        static Transform LastWing(Transform root, string prefix) => root.GetComponentsInChildren<Transform>(true).Where(item => item.name.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(item.name.Substring(prefix.Length), out _)).OrderBy(item => item.name, StringComparer.Ordinal).LastOrDefault();
        static void Guard() { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Explicit stopped authoring only."); }
        static string Output(string relative)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsDragonOutput");
            string root = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Logs/TruckSports/DragonKingdom";
            string path = Path.GetFullPath(Path.Combine(root, relative)); Directory.CreateDirectory(Path.GetDirectoryName(path)); return path;
        }
    }
}
#endif
