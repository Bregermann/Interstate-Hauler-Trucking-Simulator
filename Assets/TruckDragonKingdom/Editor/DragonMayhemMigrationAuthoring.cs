#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using LWS.TruckDragonKingdom.Mayhem;
using LWS.TruckDragonKingdom.Mayhem.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LWS.TruckDragonKingdom.Editor
{
    public static class DragonMayhemMigrationAuthoring
    {
        [Serializable] sealed class SceneRecord { public string scene, beforeSha256, afterSha256; public bool added, presentationMigrated, originalControllerPreserved; public int originalBodies, originalColliders; }
        [Serializable] sealed class Receipt { public string scope = "EXPLICIT_ADDITIVE_MAYHEM_SCENE_MIGRATION_NOT_RUNTIME_ACCEPTANCE"; public SceneRecord[] scenes; }
        static string Hash(byte[] value) { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(value)).Replace("-", "").ToLowerInvariant(); }
        public static void InstallOriginalVariants()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var current = SceneManager.GetSceneAt(i);
                if (Application.isBatchMode && string.IsNullOrEmpty(current.path) && current.rootCount > 0)
                {
                    const string folder = "Assets/TruckDragonKingdom/Mayhem/Tests/Editor/Generated";
                    Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                    if (!EditorSceneManager.SaveScene(current, folder + "/MigrationRunner-" + Guid.NewGuid().ToString("N") + ".unity")) throw new IOException("Preserve the batch runner scene before explicit migration.");
                }
                if (current.isDirty || string.IsNullOrEmpty(current.path) && current.rootCount > 0) throw new InvalidOperationException("Preserve current scene edits before explicit migration.");
            }
            string[] modes = { "TruckKeepaway", "TruckBalloonBattle", "TruckCornhole", "TruckCheeseRolling" };
            string[] types = { "LWS.TruckKeepaway.KeepawayMatch", "LWS.TruckBalloonBattle.BalloonMatch", "LWS.TruckCornhole.CornholeMatch", "LWS.TruckCheeseRolling.CheeseMatch" };
            foreach (var mode in modes) if (!File.Exists("Assets/" + mode + "/Scenes/" + mode + ".unity")) throw new FileNotFoundException("An original sport scene is required: " + mode);
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-tsDragonReceipt");
            if (at < 0 || at + 1 >= args.Length) throw new InvalidOperationException("Provide a unique explicit migration receipt path before changing saved scenes.");
            string output = Path.GetFullPath(args[at + 1]); if (File.Exists(output)) throw new IOException("Preserve the previous receipt; choose a new migration attempt.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var setup = EditorSceneManager.GetSceneManagerSetup(); var records = new List<SceneRecord>();
            try
            {
                for (int index = 0; index < modes.Length; index++)
                {
                    string path = "Assets/" + modes[index] + "/Scenes/" + modes[index] + ".unity";
                    byte[] before = File.ReadAllBytes(path); string backup = Path.Combine(Path.GetDirectoryName(output), "original-scenes", modes[index] + ".unity");
                    if (File.Exists(backup)) throw new IOException("Preserve prior scene backup: " + backup);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.WriteAllBytes(backup, before);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var type = Type.GetType(types[index] + ", Assembly-CSharp", true);
                    var original = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren(type, true)).Single();
                    string json = EditorJsonUtility.ToJson(original);
                    var bodies = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Rigidbody>(true)).ToDictionary(body => body, body => EditorJsonUtility.ToJson(body));
                    var colliders = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Collider>(true)).ToDictionary(collider => collider, collider => EditorJsonUtility.ToJson(collider));
                    var director = original.GetComponentInChildren<DragonMayhemDirector>(true);
                    bool added = director == null;
                    if (added) director = DragonMayhemAuthoring.Install(original);
                    else DragonMayhemAuthoring.UpgradePresentation(director);
                    if (EditorJsonUtility.ToJson(original) != json || bodies.Any(pair => EditorJsonUtility.ToJson(pair.Key) != pair.Value) || colliders.Any(pair => EditorJsonUtility.ToJson(pair.Key) != pair.Value))
                        throw new InvalidOperationException("Original controller/physics changed during additive installation: " + path);
                    // Explicit migration owns this save. Repeating it must keep
                    // the complete serialized scene byte-identical, as checked
                    // against the retained before/after receipts.
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save additive variant presentation: " + path);
                    records.Add(new SceneRecord { scene = path, beforeSha256 = Hash(before), afterSha256 = Hash(File.ReadAllBytes(path)), added = added,
                        presentationMigrated = true, originalControllerPreserved = true, originalBodies = bodies.Count, originalColliders = colliders.Count });
                    File.WriteAllText(output, JsonUtility.ToJson(new Receipt { scenes = records.ToArray() }, true));
                }
                AssetDatabase.SaveAssets(); Debug.Log("DRAGON_MAYHEM_SCENES_MIGRATED " + records.Count + "; original rules and physics preserved; live ON/OFF acceptance remains required.");
            }
            finally
            {
                // A command-line Editor can begin with no loaded scene at all.
                // Unity rejects restoring that empty array; an empty scene is
                // its equivalent idle state. Saved GUI setups restore normally.
                if (setup.Length == 0 || setup.Length == 1 && string.IsNullOrEmpty(setup[0].path))
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}
#endif
