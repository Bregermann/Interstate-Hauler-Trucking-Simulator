#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class DragonKingdomWorldAuthoringTests
{
    const string Prefab = "Assets/TruckDragonKingdom/Prefabs/Shared Dragon Kingdom.prefab";
    const string Preview = "Assets/TruckDragonKingdom/Scenes/DragonKingdomPreview.unity";
    const string Definition = "Assets/TruckDragonKingdom/Configuration/Dragon Kingdom.asset";
    static Type Authoring => Type.GetType("LWS.TruckDragonKingdom.Editor.DragonKingdomWorldAuthoring, Assembly-CSharp-Editor", true);
    static Type World => Type.GetType("LWS.TruckDragonKingdom.DragonKingdomWorld, Assembly-CSharp", true);
    static object Field(object source, string name) => source.GetType().GetField(name).GetValue(source);

    [UnityTest, Timeout(1200000)]
    public IEnumerator FullOwnedWorldBuildSavesOnlyAfterOriginalPhysicalGatesPass()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False, "Use native EditMode for explicit authoring.");
        Assert.That(File.Exists(Prefab) || File.Exists(Preview) || File.Exists(Definition), Is.False,
            "Existing authored output is preserved. Review it before explicitly requesting a replacement test pass.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (Application.isBatchMode && string.IsNullOrEmpty(scene.path))
            {
                // Unity's native batch runner owns an unsaved scratch scene
                // with objects. Preserve its complete serialized state before
                // isolation instead of treating it as disposable test debris.
                const string generated = "Assets/TruckDragonKingdom/Tests/World/Generated";
                if (!Directory.Exists(generated)) { Directory.CreateDirectory(generated); AssetDatabase.Refresh(); }
                string snapshot = generated + "/RunnerScene-" + Guid.NewGuid().ToString("N") + ".unity";
                Assert.That(EditorSceneManager.SaveScene(scene, snapshot), Is.True,
                    "The batch runner scene must save successfully before any scene closes.");
                Assert.That(File.Exists(snapshot), Is.True);
                Debug.Log("DRAGON_KINGDOM_RUNNER_SCENE_PRESERVED " + snapshot + " roots=" + scene.rootCount);
            }
            Assert.That(scene.isDirty, Is.False, "Do not close a scene containing unsaved user edits: " + scene.name);
            Assert.That(!string.IsNullOrEmpty(scene.path) || scene.rootCount == 0, Is.True,
                "An unsaved scene with objects cannot be safely restored; preserve it before the isolated authoring slot.");
        }
        // Refresh only after successful snapshots so restoration reopens their
        // real saved copies. GUI scenes retain the original strict guards.
        var prior = EditorSceneManager.GetSceneManagerSetup();
        bool restoreEmpty = prior.Length == 1 && string.IsNullOrEmpty(prior[0].path);
        var originalHashes = prior.Where(s => !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
            .ToDictionary(s => s.path, s => File.ReadAllBytes(s.path));
        bool completed = false;
        try
        {
            var isolated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Unity 6000.4 refuses additive scene creation while the current
            // scene is untitled, even when it contains no objects or edits.
            const string isolatedFolder = "Assets/TruckDragonKingdom/Tests/World/Generated";
            if (!Directory.Exists(isolatedFolder)) { Directory.CreateDirectory(isolatedFolder); AssetDatabase.Refresh(); }
            string isolatedPath = isolatedFolder + "/IsolatedAuthoringScene-" + Guid.NewGuid().ToString("N") + ".unity";
            Assert.That(EditorSceneManager.SaveScene(isolated, isolatedPath), Is.True,
                "Save the isolated empty setup scene before the original additive builder runs.");
            Assert.That(File.Exists(isolatedPath), Is.True);
            Assert.That(Terrain.activeTerrains, Is.Empty, "The vendor terrain pass must be isolated from all original terrain assets.");
            var build = (IEnumerator)Authoring.GetMethod("BuildSharedWorld", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            Assert.That(build, Is.Not.Null);
            // This executes the real Vista/EasyRoads/model/water/boarding pipeline,
            // including its original native support/clearance/material gates.
            yield return build;
            Assert.That((bool)Authoring.GetProperty("IsBuilding").GetValue(null), Is.False);
            string status = (string)Authoring.GetProperty("LastBuildStatus").GetValue(null);
            Assert.That(status, Does.StartWith("Ready"), status);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var definition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Definition);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(definition, Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Preview), Is.Not.Null);
            var bound = prefab.GetComponent(World);
            Assert.That(bound, Is.Not.Null, "The saved shared asset must bind the original runtime world contract.");
            Assert.That(Field(bound, "definition"), Is.SameAs(definition));
            Assert.That(((Array)Field(bound, "regions")).Length, Is.EqualTo(9));
            Assert.That(((Array)Field(bound, "extractionSites")).Length, Is.EqualTo(3));
            Assert.That(((Array)Field(bound, "aerialCheckpoints")).Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(((Array)Field(bound, "riderStarts")).Length, Is.EqualTo(4));
            Assert.That(prefab.GetComponentsInChildren<Terrain>(true).Length, Is.EqualTo(4));
            Assert.That(prefab.GetComponentsInChildren<MeshFilter>(true).Length, Is.GreaterThan(100), "Actual owned scenery must be present in the saved world.");
            var loaded = SceneManager.GetSceneByPath(Preview);
            Assert.That(loaded.IsValid() && loaded.isLoaded, Is.True);
            Assert.That(loaded.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren(World, true)).Count(), Is.EqualTo(1));
            foreach (var original in originalHashes)
                Assert.That(File.ReadAllBytes(original.Key), Is.EqualTo(original.Value), "Original scene bytes changed: " + original.Key);
            completed = true;
        }
        finally
        {
            // Failed native geometry remains reviewable as a clearly failed
            // diagnosis scene; it is never promoted to the accepted output paths.
            if (!completed)
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.rootCount > 0 && string.IsNullOrEmpty(scene.path))
                    {
                        Directory.CreateDirectory("Assets/TruckDragonKingdom/Scenes");
                        AssetDatabase.Refresh();
                        EditorSceneManager.SaveScene(scene, "Assets/TruckDragonKingdom/Scenes/DragonKingdomFailedNativePreview.unity");
                    }
                }
            if (restoreEmpty) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else EditorSceneManager.RestoreSceneManagerSetup(prior);
        }
    }
}
#endif
