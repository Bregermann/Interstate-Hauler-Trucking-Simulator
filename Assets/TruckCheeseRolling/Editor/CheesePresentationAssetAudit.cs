using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckCheeseRolling.Editor
{
    // Read-only evidence before replacing the legacy baked cat poses or VFX.
    public static class CheesePresentationAssetAudit
    {
        public static void Inspect()
        {
            var log = new StringBuilder(); const string modelPath = "Assets/TruckCheeseRolling/Art/Animals/MalbersCat.fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath); if (source == null) throw new InvalidOperationException("Existing owned cat model is missing.");
            log.AppendLine("OWNED CAT MODEL " + modelPath);
            foreach (var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true)) log.AppendLine("SKIN " + skin.name + " bones=" + skin.bones.Length + " vertices=" + (skin.sharedMesh != null ? skin.sharedMesh.vertexCount : 0));
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)))
            {
                var bindings = AnimationUtility.GetCurveBindings(clip); int varying = 0;
                foreach (var binding in bindings) { var curve = AnimationUtility.GetEditorCurve(clip, binding); if (curve != null && curve.keys.Length > 1 && curve.keys.Max(k => k.value) - curve.keys.Min(k => k.value) > .0001f) varying++; }
                log.AppendLine("EMBEDDED CLIP " + clip.name + " duration=" + clip.length + " fps=" + clip.frameRate + " bindings=" + bindings.Length + " varying=" + varying + " loop=" + clip.isLooping);
            }
            var legacy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TruckCheeseRolling/Prefabs/SceneryCat.prefab");
            log.AppendLine("LEGACY CAT animators=" + legacy.GetComponentsInChildren<Animator>(true).Length + " skins=" + legacy.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length + " staticMeshes=" + legacy.GetComponentsInChildren<MeshFilter>(true).Length);
            EditorSceneManager.OpenScene(CheeseAuthoring.Scene); var match = UnityEngine.Object.FindAnyObjectByType<CheeseMatch>();
            foreach (var prop in match.scenery.props.Where(t => t.name.IndexOf("cat", StringComparison.OrdinalIgnoreCase) >= 0)) log.AppendLine("SAVED CAT " + prop.name + " animators=" + prop.GetComponentsInChildren<Animator>(true).Length + " skins=" + prop.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
            foreach (var fx in match.explosions) log.AppendLine("EXISTING EXPLOSION " + fx.name + " children=" + fx.GetComponentsInChildren<ParticleSystem>(true).Length + " maxParticles=" + fx.main.maxParticles + " renderer=" + AssetDatabase.GetAssetPath(fx.GetComponent<ParticleSystemRenderer>().sharedMaterial));
            foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => (p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)) && (p.IndexOf("explosion", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("kitten", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("/cat", StringComparison.OrdinalIgnoreCase) >= 0)).Take(70)) log.AppendLine("AVAILABLE " + path);
            Directory.CreateDirectory("Logs/TruckSports/PlaytestRepair"); File.WriteAllText("Logs/TruckSports/PlaytestRepair/cheese-presentation-asset-audit.txt", log.ToString()); Debug.Log("CHEESE_PRESENTATION_ASSETS_INSPECTED read-only; no assets or scene saved.\n" + log);
        }
    }
}
