using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadAir.Editor
{
    public static class DeadAirWebGLBuilder
    {
        public const string BuildPath = "Builds/DeadAir_WebGL";

        [MenuItem("Dead Air/Build WebGL")]
        public static void BuildWebGl()
        {
            Directory.CreateDirectory(BuildPath);
            Scene scene = EditorSceneManager.OpenScene(DeadAirSceneBuilder.ScenePath, OpenSceneMode.Single);
            ApplyReleaseConfiguration(scene);

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { DeadAirSceneBuilder.ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception($"Dead Air WebGL build failed: {summary.result} ({summary.totalErrors} error(s)).");
            }

            Debug.Log($"[Dead Air] WebGL build succeeded at {BuildPath}.");
        }

        private static void ApplyReleaseConfiguration(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (DeadAirHud hud in root.GetComponentsInChildren<DeadAirHud>(true))
                {
                    SerializedObject serialized = new SerializedObject(hud);
                    SerializedProperty debug = serialized.FindProperty("showDebugOverlay");
                    if (debug != null)
                    {
                        debug.boolValue = false;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                foreach (DeadAirStoryDirector story in root.GetComponentsInChildren<DeadAirStoryDirector>(true))
                {
                    SerializedObject serialized = new SerializedObject(story);
                    SerializedProperty verbose = serialized.FindProperty("logTriggerEvents");
                    if (verbose != null)
                    {
                        verbose.boolValue = false;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }
    }
}
