using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using LWS.TruckFieldGoal;

namespace LWS.TruckHotPotato.Editor
{
    // Reuse the existing two-pose spectator assets/controller. Seat sampling is
    // editor-only against the actual stand surfaces; temporary colliders are removed.
    public static class HotPotatoCrowdAuthoring
    {
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(HotPotatoAuthoring.Scene);
            var match = UnityEngine.Object.FindAnyObjectByType<HotPotatoMatch>();
            if (match.crowd != null) return;
            var stadium = scene.GetRootGameObjects().First(g => g.name == "Reused Hot Potato Existing Union Field grandstands and press boxes");
            var shapes = new List<MeshCollider>();
            try
            {
                foreach (var mesh in stadium.GetComponentsInChildren<MeshFilter>())
                    if (mesh.sharedMesh != null) { var collider = mesh.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = mesh.sharedMesh; shapes.Add(collider); }
                Physics.SyncTransforms();
                var positions = new List<Vector3>();
                for (float z = -125; z <= 125; z += 3.4f) for (float x = -125; x <= 125; x += 3.4f)
                {
                    if (x * x + z * z < 72 * 72) continue;
                    var ray = new Ray(new Vector3(x, 70, z), Vector3.down); float height = -1;
                    foreach (var collider in shapes) if (collider.Raycast(ray, out var hit, 90) && hit.normal.y > .8f && hit.point.y > .5f && hit.point.y < 40) height = Mathf.Max(height, hit.point.y);
                    if (height >= 0 && positions.Count < 1000) positions.Add(new Vector3(x, height + .04f, z));
                }
                Directory.CreateDirectory("Logs/TruckSports/EighteenSports");
                File.WriteAllText("Logs/TruckSports/EighteenSports/hot-potato-stand-sampling.txt", "Actual tier seats=" + positions.Count + " render meshes=" + shapes.Count);
                if (positions.Count < 50) throw new InvalidOperationException("Actual grandstand seat sampling failed; do not guess floating crowd positions.");
                var source = EditorSceneManager.OpenScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene, OpenSceneMode.Additive);
                FieldGoalCrowd.Batch[] originals;
                try { originals = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FieldGoalCrowd>(true)).First().batches; }
                finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(scene); }
                InstallCrowd(match, positions.ToArray(), originals);
                foreach (var shape in shapes) if (shape != null) UnityEngine.Object.DestroyImmediate(shape);
                shapes.Clear();
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            finally { foreach (var shape in shapes) if (shape != null) UnityEngine.Object.DestroyImmediate(shape); }
        }
        static void InstallCrowd(HotPotatoMatch match, Vector3[] positions, FieldGoalCrowd.Batch[] originals)
        {
            if (match.crowd != null) UnityEngine.Object.DestroyImmediate(match.crowd.gameObject);
            var root = new GameObject("Dense event-reactive tier spectators");
            var crowd = root.AddComponent<FieldGoalCrowd>(); match.crowd = crowd; crowd.eventSession = match; crowd.nearFans = Array.Empty<Animator>(); crowd.runnerChance = 0;
            crowd.response = root.AddComponent<AudioSource>(); crowd.response.playOnAwake = false; crowd.response.spatialBlend = .5f;
            crowd.cheer = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TruckDockDiving/Audio/Exceptional cheer.wav"); crowd.boo = crowd.cheer;
            crowd.batches = new FieldGoalCrowd.Batch[originals.Length];
            for (int variant = 0; variant < originals.Length; variant++)
            {
                var original = originals[variant];
                var seats = positions.Where((p, i) => i % originals.Length == variant).Select(p => Matrix4x4.TRS(p, Quaternion.LookRotation(-Vector3.ProjectOnPlane(p, Vector3.up)), Vector3.one * 1.25f)).ToArray();
                var batch = new FieldGoalCrowd.Batch { idle = original.idle, cheer = original.cheer, materials = original.materials, seats = seats, fans = new MeshFilter[seats.Length] }; crowd.batches[variant] = batch;
                for (int i = 0; i < seats.Length; i++)
                {
                    var fan = new GameObject("Shared-mesh posed spectator " + variant + " seat " + i); fan.transform.SetParent(root.transform, false);
                    fan.transform.SetPositionAndRotation(seats[i].GetColumn(3), seats[i].rotation); fan.transform.localScale = seats[i].lossyScale;
                    var filter = fan.AddComponent<MeshFilter>(); filter.sharedMesh = original.idle; batch.fans[i] = filter;
                    var renderer = fan.AddComponent<MeshRenderer>(); renderer.sharedMaterials = original.materials; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                }
            }
        }
        public static void RepackInstanced()
        {
            var scene = EditorSceneManager.OpenScene(HotPotatoAuthoring.Scene); var match = UnityEngine.Object.FindAnyObjectByType<HotPotatoMatch>();
            if (match.crowd == null) throw new InvalidOperationException("Existing authored tier seats required.");
            if (match.crowd.batches.All(b => !b.idle.name.StartsWith("Tier crowd"))) return;
            var seats = match.crowd.batches.SelectMany(b => b.seats).Select(matrix => (Vector3)matrix.GetColumn(3)).ToArray();
            var source = EditorSceneManager.OpenScene(LWS.TruckFieldGoal.Editor.FieldGoalAuthoring.Scene, OpenSceneMode.Additive); FieldGoalCrowd.Batch[] originals;
            try { originals = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FieldGoalCrowd>(true)).First().batches; }
            finally { EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(scene); }
            InstallCrowd(match, seats, originals); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            string folder = "Assets/TruckHotPotato/Models", backup = "Logs/TruckSports/EighteenSports/CrowdBeforeInstancing"; Directory.CreateDirectory(backup);
            var dependencies = new HashSet<string>(AssetDatabase.GetDependencies(HotPotatoAuthoring.Scene, true));
            foreach (var path in Directory.GetFiles(folder, "Tier crowd*.asset"))
            {
                string asset = path.Replace(Path.DirectorySeparatorChar, '/'); if (dependencies.Contains(asset)) throw new InvalidOperationException("Crowd duplicate is still referenced: " + asset);
                File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), true); if (File.Exists(path + ".meta")) File.Copy(path + ".meta", Path.Combine(backup, Path.GetFileName(path) + ".meta"), true);
                if (!AssetDatabase.DeleteAsset(asset)) throw new IOException("Could not remove unreferenced owned crowd duplicate " + asset);
            }
            Debug.Log("HOT_POTATO_SHARED_MESH_CROWD seats=" + seats.Length + " sourceVariants=" + originals.Length);
        }
    }
}
