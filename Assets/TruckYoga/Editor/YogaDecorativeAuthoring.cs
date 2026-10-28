using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckYoga.Editor
{
    // Only Yoga owns this fit. The shared scenery helper, purchased mesh/material
    // assets, meadow terrain, physics and original truck rigs remain untouched.
    public static class YogaDecorativeAuthoring
    {
        const string Source = "Assets/Toon Gas Station/Prefabs/Rock_Cluster_1A.prefab";
        const string Meadow = "Installed retreat trees and scenic rocks";
        const string Garden = "Retreat stone lanterns and waterfall garden";
        public const float MeadowFootprint = 12, BackdropFootprint = 12;
        public static readonly Vector3 CascadeBase = new Vector3(-48, 0, 43);
        sealed class Spec
        {
            public string label, parent;
            public Vector3 legacy, ground;
            public float legacyHeight;
            public Quaternion rotation;
        }
        static Vector3 Ground(Terrain terrain, Vector3 p) { p.y = terrain.SampleHeight(p) + terrain.transform.position.y; return p; }
        static Spec MeadowSpec(Terrain terrain, int index)
        {
            float angle = index * 2.39996f, radius = 42 + index % 5 * 10;
            var position = Ground(terrain, new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius + 10));
            return new Spec { label = "Owned Yoga V4 meadow rock " + index, parent = Meadow, legacy = position, ground = position, legacyHeight = 12 + index % 4 * 4, rotation = Quaternion.identity };
        }
        static Spec BackdropSpec(Terrain terrain, int index)
        {
            return new Spec { label = "Owned Yoga V4 waterfall outcrop " + index, parent = Garden,
                legacy = index == 0 ? new Vector3(-43, 0, 33) : new Vector3(-34, 0, 35), legacyHeight = index == 0 ? 22 : 21,
                ground = Ground(terrain, index == 0 ? new Vector3(-51, 0, 46) : new Vector3(-44, 0, 47)), rotation = Quaternion.Euler(90, 35, 0) };
        }
        public static void AddMeadowRock(Transform parent, Terrain terrain, int index) => Add(parent, MeadowSpec(terrain, index));
        static void Add(Transform parent, Spec spec)
        {
            LWS.TruckBaseJumping.Editor.BaseSkyAuthoring.Scenery(parent, spec.ground, 1, false, YogaAuthoring.Folder);
            Fit(parent.GetChild(parent.childCount - 1), spec); 
        }
        public static void CompleteWaterfall(Transform parent, Transform cascade)
        {
            var terrain = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Single(t => t.gameObject.scene == parent.gameObject.scene && t.name == "Vista grass mountain meadow");
            Add(parent, BackdropSpec(terrain, 0)); Add(parent, BackdropSpec(terrain, 1)); FitCascade(cascade, terrain);
        }
        static Renderer[] Rendering(Transform root) => root.GetComponentsInChildren<Renderer>(true);
        static Bounds BoundsOf(Transform root)
        {
            var renderers = Rendering(root); if (renderers.Length == 0) throw new InvalidOperationException("Yoga rock has no real mesh renderers.");
            Bounds bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        static void Fit(Transform root, Spec spec)
        {
            root.name = spec.label; root.SetPositionAndRotation(spec.ground, spec.rotation); root.localScale = Vector3.one;
            var bounds = BoundsOf(root); float maximum = Mathf.Max(bounds.size.x, bounds.size.z);
            if (!float.IsFinite(maximum) || maximum <= .001f) throw new InvalidOperationException("The installed decorative mesh has invalid dimensions.");
            // Preserve the purchased art's proportions, never stretch the flat
            // cluster to a requested cliff height. Outcrops may stand upright.
            root.localScale *= (spec.parent == Meadow ? MeadowFootprint : BackdropFootprint) / maximum;
            bounds = BoundsOf(root); root.position += spec.ground - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        static void FitCascade(Transform cascade, Terrain terrain)
        {
            cascade.position = Ground(terrain, CascadeBase) + Vector3.up * .05f;
            cascade.rotation = Quaternion.Euler(0, 35, 0); cascade.localScale = new Vector3(1, .45f, 1);
        }
        static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < .003f;
        static bool SameArt(Transform root, GameObject source)
        {
            if (root.childCount != 1 || root.GetComponentsInChildren<Collider>(true).Length != 0 || root.GetComponentsInChildren<Rigidbody>(true).Length != 0 || root.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) return false;
            var actual = root.GetComponentsInChildren<MeshFilter>(true); var expected = source.GetComponentsInChildren<MeshFilter>(true);
            if (actual.Length != expected.Length || actual.Where((a, i) => a.sharedMesh != expected[i].sharedMesh).Any()) return false;
            var instance = root.GetChild(0);
            if (Quaternion.Angle(instance.localRotation, source.transform.localRotation) > .01f) return false;
            var ratio = instance.localScale; var originalScale = source.transform.localScale;
            float uniform = ratio.x / originalScale.x;
            if (uniform <= 0 || !float.IsFinite(uniform) || !Near(ratio, originalScale * uniform)) return false;
            var nested = instance.GetComponentsInChildren<Transform>(true); var vendorTransforms = source.GetComponentsInChildren<Transform>(true);
            if (nested.Length != vendorTransforms.Length) return false;
            for (int i = 1; i < nested.Length; i++) if (nested[i].name != vendorTransforms[i].name || !Near(nested[i].localPosition, vendorTransforms[i].localPosition) || !Near(nested[i].localScale, vendorTransforms[i].localScale) || Quaternion.Angle(nested[i].localRotation, vendorTransforms[i].localRotation) > .01f) return false;
            var renderers = Rendering(root); var vendor = source.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != vendor.Length) return false;
            var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckYoga/Materials/Scenery stone.mat");
            for (int r = 0; r < renderers.Length; r++)
            {
                var materials = vendor[r].sharedMaterials.ToArray();
                if (materials.Length > 0 && (materials[0] == null || materials[0].shader.name == "Standard")) materials[0] = stone;
                if (!renderers[r].sharedMaterials.SequenceEqual(materials)) return false;
            }
            return true;
        }
        static bool Legacy(Transform root, Spec spec)
        {
            var bounds = BoundsOf(root);
            return root.name == "Installed Toon Gas Station rock" && Near(root.position, spec.legacy) && Near(root.localScale, Vector3.one) && Quaternion.Angle(root.rotation, Quaternion.identity) < .01f &&
                Near(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), spec.legacy) && Mathf.Abs(bounds.size.y - spec.legacyHeight) < .003f;
        }
        static bool Current(Transform root, Spec spec)
        {
            var bounds = BoundsOf(root);
            return root.name == spec.label && Near(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), spec.ground) &&
                Quaternion.Angle(root.rotation, spec.rotation) < .01f && Mathf.Abs(Mathf.Max(bounds.size.x, bounds.size.z) - (spec.parent == Meadow ? MeadowFootprint : BackdropFootprint)) < .003f;
        }
        [MenuItem("Truck Sports/Impossible Games/Repair EXACT Owned Yoga Decorative Fits V4")]
        public static void RepairOwnedDecorativeFits()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped, compiled Editor required.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save existing scene edits before explicit Yoga repair.");
            var scene = SceneManager.GetSceneByPath(YogaAuthoring.Scene);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(YogaAuthoring.Scene, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var meadow = all.Single(t => t.name == Meadow); var garden = all.Single(t => t.name == Garden);
            var terrain = all.Select(t => t.GetComponent<Terrain>()).Single(t => t != null && t.name == "Vista grass mountain meadow");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source); if (source == null) throw new InvalidOperationException("Original installed rock prefab required.");
            var specs = new List<Spec>(); for (int i = 0; i < 38; i += 3) specs.Add(MeadowSpec(terrain, i)); specs.Add(BackdropSpec(terrain, 0)); specs.Add(BackdropSpec(terrain, 1));
            var mapped = new Dictionary<Transform, Spec>(); bool legacy = false, current = false;
            foreach (var spec in specs)
            {
                var parent = spec.parent == Meadow ? meadow : garden;
                var matches = Enumerable.Range(0, parent.childCount).Select(parent.GetChild).Where(t => SameArt(t, source) && (Legacy(t, spec) || Current(t, spec))).ToArray();
                if (matches.Length != 1 || mapped.ContainsKey(matches[0])) throw new InvalidOperationException("Preserve custom/partial Yoga decoration: expected exact owned source/pose/material signature for " + spec.label);
                mapped.Add(matches[0], spec); legacy |= Legacy(matches[0], spec); current |= Current(matches[0], spec);
            }
            var cascade = garden.Find("Flowing River Modeler waterfall");
            if (cascade == null || cascade.GetComponentsInChildren<Collider>(true).Length != 0 || cascade.GetComponent<LWS.TruckFishing.FishingWaterFlow>() == null ||
                cascade.GetComponent<MeshFilter>()?.sharedMesh != AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TruckYoga/Models/Curving waterfall sheet.asset") ||
                cascade.GetComponent<Renderer>()?.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>("Assets/TruckYoga/Materials/River Modeler flowing mountain cascade.mat")) throw new InvalidOperationException("Preserve customized waterfall components/mesh/material.");
            bool oldCascade = Near(cascade.position, new Vector3(-38, 0, 31)) && Near(cascade.localScale, Vector3.one) && Quaternion.Angle(cascade.rotation, Quaternion.Euler(0, 35, 0)) < .01f;
            bool newCascade = Near(cascade.position, Ground(terrain, CascadeBase) + Vector3.up * .05f) && Near(cascade.localScale, new Vector3(1, .45f, 1)) && Quaternion.Angle(cascade.rotation, Quaternion.Euler(0, 35, 0)) < .01f;
            if (legacy && current || legacy && !oldCascade || current && !newCascade) throw new InvalidOperationException("Partial/custom Yoga garden retained for review.");
            if (current) { Debug.Log("YOGA_V4_DECORATIVE_FITS_PRESERVED all15 exact owned bounded fits already present."); return; }
            foreach (var pair in mapped) Fit(pair.Key, pair.Value); FitCascade(cascade, terrain);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("YOGA_V4_DECORATIVE_FITS_REPAIRED fifteen render-only rock transforms bounded12m; terrain-grounded8.1m cascade and upright natural-proportion backdrop. Original terrain, allcolliders/rigidbodies/joints, truck rigs, cameras, trees, wildlife, meshes and materials unchanged.");
        }
    }
}
