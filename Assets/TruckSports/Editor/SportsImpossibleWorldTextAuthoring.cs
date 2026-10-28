using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    public static class SportsImpossibleWorldTextAuthoring
    {
        public static void InstallAuthoredWorldText()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stopped compiled Editor required.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Preserve dirty scene before explicit world lettering repair.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/TruckSports/Resources/Depth Tested World Font.shader");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Supported owned URP world-font material required.");
            int count = 0;
            foreach (var entry in SportsImpossibleGames.All.Where(entry => File.Exists(entry.ScenePath)))
            {
                var scene = EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);
                var labels = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TextMesh>(true)).ToArray();
                foreach (var label in labels)
                {
                    // Preserve the original vehicle/cab presentation hierarchy.
                    if (label.GetComponentInParent<SportsEventTruck>() != null) continue;
                    var existing = label.GetComponent<SportsDepthTestedWorldText>();
                    var material = label.GetComponent<MeshRenderer>().sharedMaterial;
                    if (existing != null && existing.worldTextShader != shader)
                        throw new InvalidOperationException("Preserve customized world-font binding: " + entry.scene + "/" + label.name);
                    if (material == null || material.shader == null || material.shader.name != "GUI/Text Shader") continue;
                    var adapter = existing != null ? existing : label.gameObject.AddComponent<SportsDepthTestedWorldText>();
                    adapter.worldTextShader = shader; EditorUtility.SetDirty(adapter); count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save world lettering in " + entry.scene);
            }
            Debug.Log("IMPOSSIBLE_DEPTH_TESTED_WORLD_LABELS=" + count + "; original TextMesh values/poses/fonts and all old sports untouched; actual occlusion proof remains required.");
        }
    }
}
