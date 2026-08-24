using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWS.InterstateHauler;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadAir.Editor
{
    public static class DeadAirValidation
    {
        private static readonly string[] RequiredRoots =
        {
            "DEAD_AIR",
            "START",
            "SYSTEMS",
            "STORY_TRIGGERS",
            "CHOICE_TRIGGERS",
            "ENVIRONMENT",
            "UI",
            "DEBUG"
        };

        [MenuItem("Dead Air/Validate Jam Mode")]
        public static void ValidateJamModeMenu()
        {
            ValidateJamMode();
        }

        public static bool ValidateJamMode()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            ValidateFolders(errors);
            ValidateScene(errors, warnings);
            ValidateWebGlSurface(warnings);

            foreach (string warning in warnings)
            {
                Debug.LogWarning($"[Dead Air Validation] {warning}");
            }

            foreach (string error in errors)
            {
                Debug.LogError($"[Dead Air Validation] {error}");
            }

            if (errors.Count == 0)
            {
                Debug.Log($"[Dead Air Validation] PASS with {warnings.Count} warning(s).");
                return true;
            }

            Debug.LogError($"[Dead Air Validation] FAIL with {errors.Count} error(s) and {warnings.Count} warning(s).");
            return false;
        }

        private static void ValidateFolders(List<string> errors)
        {
            string[] folders =
            {
                "Assets/DeadAir/Scenes",
                "Assets/DeadAir/Scripts/Core",
                "Assets/DeadAir/Scripts/Story",
                "Assets/DeadAir/Scripts/Vehicle",
                "Assets/DeadAir/Scripts/Audio",
                "Assets/DeadAir/Scripts/UI",
                "Assets/DeadAir/Scripts/Environment",
                "Assets/DeadAir/Scripts/Editor",
                "Assets/DeadAir/Prefabs",
                "Assets/DeadAir/Materials",
                "Assets/DeadAir/Audio",
                "Assets/DeadAir/Art",
                "Assets/DeadAir/Environment",
                "Assets/DeadAir/ScriptableObjects",
                "Assets/DeadAir/UI"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    errors.Add($"Missing folder: {folder}");
                }
            }
        }

        private static void ValidateScene(List<string> errors, List<string> warnings)
        {
            string scenePath = DeadAirSceneBuilder.ScenePath;
            if (!File.Exists(scenePath))
            {
                errors.Add($"Missing Dead Air scene: {scenePath}");
                return;
            }

            bool openedScene = false;
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                openedScene = true;
            }

            foreach (string root in RequiredRoots)
            {
                if (GameObject.Find(root) == null)
                {
                    errors.Add($"Missing scene root: {root}");
                }
            }

            if (Object.FindObjectsByType<DeadAirGameManager>(FindObjectsSortMode.None).Length > 1)
            {
                errors.Add("Duplicate DeadAirGameManager instances found.");
            }

            if (Object.FindFirstObjectByType<DeadAirSceneBootstrapper>() == null)
            {
                errors.Add("DeadAirSceneBootstrapper is missing.");
            }

            DeadAirTriggerZone[] triggers = Object.FindObjectsByType<DeadAirTriggerZone>(FindObjectsSortMode.None);
            HashSet<string> beatIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (DeadAirTriggerZone trigger in triggers)
            {
                if (trigger == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(trigger.BeatId))
                {
                    errors.Add($"Trigger {trigger.name} has no Beat ID.");
                    continue;
                }

                if (!beatIds.Add(trigger.BeatId))
                {
                    errors.Add($"Duplicate Beat ID: {trigger.BeatId}");
                }

                BoxCollider box = trigger.GetComponent<BoxCollider>();
                if (box == null || !box.isTrigger)
                {
                    errors.Add($"Trigger {trigger.BeatId} needs a trigger BoxCollider.");
                }

                if (trigger.TriggerOnce && trigger.Notes != null && trigger.Notes.Contains("UNPLACED"))
                {
                    warnings.Add($"Critical beat is still unplaced: {trigger.BeatId}");
                }
            }

            IReadOnlyList<DeadAirBeatDefinition> expected = DeadAirBeatLayoutUtility.CreateDefaultBeatDefinitions();
            foreach (DeadAirBeatDefinition beat in expected.Where(beat => beat.critical))
            {
                if (!beatIds.Contains(beat.beatId))
                {
                    warnings.Add($"Critical unplaced beat has not been instantiated yet: {beat.beatId}");
                }
            }

            if (openedScene)
            {
                Debug.Log("[Dead Air Validation] DeadAir_Main was opened for validation.");
            }
        }

        private static void ValidateWebGlSurface(List<string> warnings)
        {
            string[] deadAirScripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets/DeadAir" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.Replace('\\', '/').Contains("/Scripts/Editor/"))
                .ToArray();

            string[] blockedTokens = { "DirectInput", "System.IO.File", "DllImport", "Windows", "Thread" };
            foreach (string script in deadAirScripts)
            {
                string text = File.ReadAllText(script);
                foreach (string token in blockedTokens)
                {
                    if (text.Contains(token))
                    {
                        warnings.Add($"Review WebGL compatibility token '{token}' in {script}.");
                    }
                }
            }
        }
    }
}
