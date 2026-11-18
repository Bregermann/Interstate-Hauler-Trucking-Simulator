using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckHitchSports.Editor
{
    public static class TrebuchetTargetsAuthoring
    {
        const string Wall = "Assets/CoreCollapse/Demo/Models/Constructed/Wall.fbx";
        const string Concrete = "Assets/CoreCollapse/Demo/Materials/Concrete.mat";
        const string Stone = "Assets/TruckHitchSports/Materials/Owned siege masonry.mat";
        [MenuItem("Truck Sports/Alpha 0.10/Place active Trebuchet reachable siege targets")]
        public static void UpgradeActiveTrebuchetTargets() => SetActiveTrebuchetTargetDistance(50);

        // Explicit authoring only. Every original target body/component/GUID is
        // retained; only the requested target lane translation and render skin
        // change. No actor, loading dock, sling, arm or collision size changes.
        public static void SetActiveTrebuchetTargetDistance(float metresBeyondCradle)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/TruckHitchSports/Scenes/TrailerTrebuchet.unity") throw new InvalidOperationException("Open the saved TrailerTrebuchet scene explicitly.");
            if (metresBeyondCradle < 40 || metresBeyondCradle > 60) throw new ArgumentOutOfRangeException(nameof(metresBeyondCradle), "This existing apparatus needs a short-range 40-60m siege field; change its mechanics before authoring a longer range.");
            var match = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TrebuchetMatch>(true)).Single();
            var targets = match.GetComponentsInChildren<SiegeTarget>(true);
            if (targets.Length != 48) throw new InvalidOperationException("Preserve the four existing twelve-block physical target walls.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Wall) == null) throw new InvalidOperationException("Existing CoreCollapse constructed wall art missing.");
            var stone = MasonryMaterial();
            for (int lane = 0; lane < match.engines.Length; lane++)
            {
                var blocks = targets.Where(t => t.Match == match && t.Lane == lane).ToArray();
                if (blocks.Length != 12) throw new InvalidOperationException("Each lane must preserve its original physical wall.");
                var zone = match.targets[lane];
                var unloading = match.transform.Find("Physical siege unloading ramp " + lane);
                if (unloading == null) throw new InvalidOperationException("Preserve the proven tractor unloading ramp before placing a safe siege field.");
                float unloadEnd = unloading.GetComponent<Collider>().bounds.max.z;
                float nearestBlockOffset = blocks.Min(t => t.GetComponent<Collider>().bounds.min.z - zone.position.z);
                float z = Mathf.Max(match.engines[lane].LoadingCradle.bounds.center.z + metresBeyondCradle, unloadEnd + 8 - nearestBlockOffset);
                float delta = z - zone.position.z;
                zone.position += Vector3.forward * delta; EditorUtility.SetDirty(zone);
                foreach (var target in blocks)
                {
                    target.transform.position += Vector3.forward * delta;
                    var body = target.GetComponent<Rigidbody>(); body.position = target.transform.position;
                    var art = target.transform.Find("Owned constructed masonry wall segment");
                    if (art == null)
                    {
                        art = new GameObject("Owned constructed masonry wall segment").transform; art.SetParent(target.transform, false);
                        // Existing target roots scale unit collision boxes to
                        // full-size blocks. Match that unchanged local volume.
                        LWS.TruckKeepaway.Editor.KeepawayPresentationAuthoring.Fit(art, Wall, Vector3.one, -.5f, true, "TruckHitchSports");
                    }
                    foreach (var renderer in art.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = Enumerable.Repeat(stone, renderer.sharedMaterials.Length).ToArray();
                    var original = target.GetComponent<Renderer>(); if (original != null) { original.enabled = false; EditorUtility.SetDirty(original); }
                    EditorUtility.SetDirty(target.transform); EditorUtility.SetDirty(body);
                }
                Debug.Log("HH10_TREBUCHET_TARGET_TRANSLATED lane=" + lane + " targetZ=" + z.ToString("F2") + " beyondCradle=" + (z - match.engines[lane].LoadingCradle.bounds.center.z).ToString("F2") + " unloadingEnd=" + unloadEnd.ToString("F2") + "; original bodies/collision and physical damage rules retained.");
            }
            Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        static Material MasonryMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(Stone); if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Material>(Concrete); if (source == null) throw new InvalidOperationException("Installed CoreCollapse concrete material missing.");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Owned siege masonry", mainTexture = source.mainTexture, color = new Color(.72f, .69f, .61f) };
            material.SetFloat("_Smoothness", .1f);
            if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null) { material.SetTexture("_BumpMap", source.GetTexture("_BumpMap")); material.EnableKeyword("_NORMALMAP"); }
            AssetDatabase.CreateAsset(material, Stone); return material;
        }
    }
}
