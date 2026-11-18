using System;
using System.Collections.Generic;
using LWS.TruckWaterSports;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using RiverVfx = sc.modeling.water.common.runtime.VFX;

namespace LWS.TruckWaterSports.Editor
{
    /// <summary>Explicit saved-course authoring; never rebuilds the river or changes existing planar eddies.</summary>
    public static class RiverWhirlpoolCurrentAuthoring
    {
        const string ScenePath = "Assets/TruckWaterSports/Scenes/WhitewaterWipeout.unity";
        const string FoamPrefab = "Assets/RiverModeler/Prefabs/River Foam VFX.prefab";
        const string Folder = "Assets/TruckWaterSports/Whirlpools";

        [MenuItem("Truck Sports/Alpha 0.10/Install active Whitewater physical whirlpools")]
        public static void InstallActiveWhirlpools() => UpgradeActiveScene();

        public static RiverWhirlpoolCurrentField UpgradeActiveScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring current fields.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open the existing saved WhitewaterWipeout course.");
            WhitewaterWipeoutMatch match = null;
            foreach (var root in scene.GetRootGameObjects()) { var candidate = root.GetComponentInChildren<WhitewaterWipeoutMatch>(true); if (candidate != null) match = candidate; }
            if (match == null || match.course == null || match.course.stations == null || match.course.stations.Length < 22)
                throw new InvalidOperationException("Preserve the complete authored Whitewater course and its two waterfall reaches.");
            var prior = match.course.GetComponent<RiverWhirlpoolCurrentField>(); if (prior != null) return prior;
            var courseObject = new SerializedObject(match.course); var hook = courseObject.FindProperty("whirlpoolField");
            if (hook == null) throw new InvalidOperationException("Integrate the optional WaterCourse.whirlpoolField + Current SampleCurrent hook before installing. No scene was changed.");
            var foamSource = AssetDatabase.LoadAssetAtPath<GameObject>(FoamPrefab);
            if (foamSource == null || foamSource.GetComponent<VisualEffect>() == null)
                throw new InvalidOperationException("The installed River Modeler URP foam VFX prefab is required; no placeholder foam is substituted.");
            for (int i = 0; i < 2; i++) foreach (string part in new[] { "positions", "velocities" })
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Whirlpool " + i + " " + part + ".asset") != null)
                    throw new InvalidOperationException("Owned whirlpool cache exists without its field; preserve/review it before an explicit retry.");
            EnsureFolder(Folder);
            var field = match.course.gameObject.AddComponent<RiverWhirlpoolCurrentField>(); field.match = match; field.course = match.course;
            field.variants = RiverWhirlpoolVariants.Slalom | RiverWhirlpoolVariants.Survival;
            field.whirlpools = new RiverWhirlpool[2]; field.foamEffects = new VisualEffect[2];
            var art = new GameObject("Authored River Modeler whirlpool reaches").transform; art.SetParent(match.transform, false);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    // Separate from the original point6/11 planar eddies, the vertical
                    // drops and their recovery pools. Bank-side cores leave a readable
                    // central line and stay within the existing 29m usable river.
                    int segment = i == 0 ? 13 : 20;
                    Vector3 a = match.course.Point(segment), b = match.course.Point(segment + 1);
                    Vector3 forward = Vector3.ProjectOnPlane(b - a, Vector3.up).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    float radius = Mathf.Min(7.5f, match.course.width * .25f);
                    float offset = Mathf.Min(6, Mathf.Max(0, match.course.width * .5f - radius - 1));
                    Vector3 center = Vector3.Lerp(a, b, .5f) + right * offset * (i == 0 ? 1 : -1);
                    var marker = new GameObject(i == 0 ? "Technical bank-side whirlpool" : "Lower survival whirlpool").transform;
                    marker.SetParent(art, false); marker.position = center;
                    var vortex = new RiverWhirlpool { name = marker.name, center = marker, radius = radius, coreRadius = 2, clockwise = i == 1 };
                    field.whirlpools[i] = vortex;
                    var foam = UnityEngine.Object.Instantiate(foamSource); foam.name = "Owned River Modeler whirlpool foam " + i;
                    foam.transform.SetParent(marker, false); foam.transform.localPosition = Vector3.up * .04f; foam.transform.localRotation = Quaternion.identity;
                    foreach (var generator in foam.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(generator);
                    var effect = foam.GetComponent<VisualEffect>(); effect.Stop(); effect.enabled = false;
                    if (!RiverVfx.PointCache.IsValidTargetEffect(effect))
                        throw new InvalidOperationException("The original River Modeler URP graph must expose its public point-cache properties. Reimport/compile the existing graph, not a substitute renderer.");
                    var emitters = new List<RiverVfx.Emitter>(96);
                    for (int ring = 0; ring < 3; ring++) for (int n = 0; n < 32; n++)
                    {
                        float angle = (n + ring * .27f) * Mathf.PI * 2 / 32, r = Mathf.Lerp(vortex.coreRadius * .75f, radius * .9f, ring / 2f);
                        Vector3 local = new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
                        Vector3 world = foam.transform.TransformPoint(local);
                        Vector3 velocity = RiverWhirlpoolCurrentField.Sample(vortex, center, world, center.y, 0); velocity.y = 0;
                        emitters.Add(new RiverVfx.Emitter { position = new Unity.Mathematics.float3(local.x, local.y, local.z), velocity = new Unity.Mathematics.float3(velocity.x, 0, velocity.z) });
                    }
                    var cache = new RiverVfx.PointCache(); cache.SetData(emitters, new Bounds(Vector3.zero, new Vector3(radius * 2 + 2, 2, radius * 2 + 2)));
                    AssetDatabase.CreateAsset(cache.positions, Folder + "/Whirlpool " + i + " positions.asset");
                    AssetDatabase.CreateAsset(cache.velocities, Folder + "/Whirlpool " + i + " velocities.asset");
                    cache.ApplyToEffect(effect);
                    Set(effect, "Spawnrate", 24); Set(effect, "Gravity", 0); Set(effect, "Splash Force (up)", .05f); Set(effect, "Flow Velocity", 1);
                    if (effect.HasVector2("Scale Min Max")) effect.SetVector2("Scale Min Max", new Vector2(.22f, .65f));
                    if (effect.HasVector2("Lifetime Min Max")) effect.SetVector2("Lifetime Min Max", new Vector2(1, 2.4f));
                    EditorUtility.SetDirty(effect); field.foamEffects[i] = effect;
                }
                hook.objectReferenceValue = field; courseObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(field); EditorSceneManager.MarkSceneDirty(scene);
                Undo.RegisterCreatedObjectUndo(art.gameObject, "Add physical whirlpool current presentation");
                Debug.Log("RIVER_WHIRLPOOLS_AUTHORED: two bank-side 3D current fields for Slalom/Survival, original planar eddies/geometry untouched, owned River Modeler public foam point caches. WaterCourse remains current authority and Archimeds remains buoyancy. Save explicitly; actual force integration, navigation and rendering require focused validation.");
                return field;
            }
            catch { UnityEngine.Object.DestroyImmediate(art.gameObject); UnityEngine.Object.DestroyImmediate(field); throw; }
        }
        static void Set(VisualEffect effect, string property, float value) { if (effect.HasFloat(property)) effect.SetFloat(property, value); }
        static void EnsureFolder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; int i = path.LastIndexOf('/'); string parent = path.Substring(0, i); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(i + 1)); }
    }
}
