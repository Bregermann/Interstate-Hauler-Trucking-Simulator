using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckCheeseRolling.Editor
{
    public static class CheeseCatPresentationAuthoring
    {
        const string Private = "Assets/TruckCheeseRolling/PrivateLicensedAnimations/";
        const string Prefab = "Assets/TruckCheeseRolling/Prefabs/Animated owned cat.prefab";
        const string Controller = "Assets/TruckCheeseRolling/Art/Animals/Scenery cat gestures.controller";
        static AnimationClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Private + name + ".fbx").OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            if (clip.length < .5f || AnimationUtility.GetCurveBindings(clip).Length < 10 || AnimationUtility.GetAnimationEvents(clip).Length != 0) throw new InvalidOperationException("Expected a real, event-free owned cat clip: " + name);
            return clip;
        }
        [MenuItem("Truck Sports/Cheese Rolling/Restore owned animated cats")]
        public static void Upgrade()
        {
            if (!File.Exists(Private + "intake.json")) throw new InvalidOperationException("Run Tools/TruckSports/ImportCheesePresentationAssets.py with your licensed Milo the Cat installation first.");
            var scene = EditorSceneManager.OpenScene(CheeseAuthoring.Scene); var match = UnityEngine.Object.FindAnyObjectByType<CheeseMatch>();
            var cats = match.scenery.props.Where(t => t.name.IndexOf("cat", StringComparison.OrdinalIgnoreCase) >= 0).ToArray(); if (cats.Length != 4) throw new InvalidOperationException("Expected the four existing seeded cat props.");
            if (cats.All(t => t.GetComponent<CheeseCatPresentation>() != null)) { Debug.Log("CHEESE_ANIMATED_CATS_PRESERVED authored animator/material/layout customizations."); return; }
            if (cats.Any(t => t.GetComponent<CheeseCatPresentation>() != null)) throw new InvalidOperationException("Partial custom cat integration detected; preserve it for review rather than rebuilding.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Private + "Cat rig.fbx"); var avatar = AssetDatabase.LoadAllAssetsAtPath(Private + "Cat rig.fbx").OfType<Avatar>().Single();
            if (model == null || !avatar.isValid || avatar.isHuman) throw new InvalidOperationException("Owned generic cat model/avatar required.");
            var clips = new[] { Clip("Cat_Idle"), Clip("Cat_Idle_Look_Around"), Clip("Cat_Idle_Scratch_Ear_L"), Clip("Cat_Idle_Yawn") };
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckCheeseRolling/Materials/Scenery Cat.mat"); if (material == null) throw new InvalidOperationException("Existing orange cat material required.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(Controller); var machine = controller.layers[0].stateMachine;
                var idle = machine.AddState("Idle"); idle.motion = clips[0]; machine.defaultState = idle;
                for (int i = 1; i < clips.Length; i++) { var state = machine.AddState(i == 1 ? "Look" : i == 2 ? "Scratch" : "Yawn"); state.motion = clips[i]; var back = state.AddTransition(idle); back.hasExitTime = true; back.exitTime = .98f; back.duration = .18f; back.hasFixedDuration = true; }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (prefab == null)
            {
                var root = new GameObject("Owned animated Milo cat scenery"); var visual = (GameObject)PrefabUtility.InstantiatePrefab(model); visual.transform.SetParent(root.transform, false);
                foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.name.IndexOf("fur", StringComparison.OrdinalIgnoreCase) >= 0) { renderer.enabled = false; continue; }
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray(); renderer.updateWhenOffscreen = false;
                }
                var animator = visual.GetComponent<Animator>(); if (animator == null) animator = visual.AddComponent<Animator>();
                animator.avatar = avatar; animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                var skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.enabled).ToArray(); var bounds = skins[0].bounds; foreach (var skin in skins.Skip(1)) bounds.Encapsulate(skin.bounds);
                float scale = 2.8f / Mathf.Max(bounds.size.x, bounds.size.z); visual.transform.localScale *= scale; visual.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
                prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefab); UnityEngine.Object.DestroyImmediate(root);
            }
            for (int i = 0; i < cats.Length; i++)
            {
                var cat = cats[i]; var legacy = cat.Find("Vendor model visual"); if (legacy == null) throw new InvalidOperationException("Existing baked cat visual missing: " + cat.name);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab); visual.transform.SetParent(cat, false); legacy.gameObject.SetActive(false);
                var presentation = cat.gameObject.AddComponent<CheeseCatPresentation>(); presentation.animator = visual.GetComponentInChildren<Animator>(true); presentation.match = match; presentation.initialSeed = 8137 + i * 53;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("CHEESE_OWNED_CATS_ANIMATED four original scenery roots/colliders retained; vendor generic rig/idle/look/scratch/yawn clips, original orange material, no root motion or vendor source modifications.");
        }
    }
}
