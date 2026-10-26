using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.TruckSports.Editor
{
    // Explicit saved-scene entry points for the mode-owned adapters. These
    // never regenerate a venue or discover participants by hierarchy guessing.
    public static class SportsCarnivalRemainingHostAuthoring
    {
        public static void InstallTerrier()
        {
            InOriginalScene("TruckTerrierRacing", SportsTruckAppearanceHostAuthoring.InstallOpenTerrierAppearance, true);
        }

        public static void CreateDockSettings()
        {
            InOriginalScene("TruckDockDiving", SportsTruckAppearanceHostAuthoring.CreateDockSplitPlaneSettings, false);
        }

        public static void CaptureDockPlanes()
        {
            InOriginalScene("TruckDockDiving", LWS.TruckDockDiving.Editor.DockSplitBatchPreview.CaptureOpenDockSourcePlanes, false);
        }

        public static void InstallDock()
        {
            InOriginalScene("TruckDockDiving", SportsTruckAppearanceHostAuthoring.InstallOpenDockAppearance, true);
        }

        public static void InstallDockFit()
        {
            InOriginalScene("TruckDockDiving", () => { SportsTruckCompatibilityAuthoring.InstallOpenScene(); }, true);
        }

        public static void InstallCornFit()
        {
            InOriginalScene("TruckCornhole", () => { SportsTruckCompatibilityCornholeAuthoring.UpgradeOpenScene(); }, true);
        }

        public static void RepairCornBodyFits()
        {
            InOriginalScene("TruckCornhole", SportsTruckAppearanceHostAuthoring.RepairOwnedOpenCornholeFits, true);
        }

        public static void InstallFixedAndSkeeFits()
        {
            foreach (string mode in new[] { "TruckKeepaway", "TruckKeepawayFirstPerson", "TruckMattressRacing", "TruckMinefield", "TruckTrampolineDodgeball", "TruckLimbo", "TruckKickball", "TruckCheeseRolling", "TruckSkeeBall", "TruckTerrierRacing" })
                InOriginalScene(mode, () => { SportsTruckCompatibilityAuthoring.InstallOpenScene(); }, true);
        }

        static void InOriginalScene(string mode, Action action, bool save)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("A stopped, compiled Editor is required.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            if (!Application.isBatchMode && previous.Any(entry => string.IsNullOrEmpty(entry.path)))
                throw new InvalidOperationException("Save interactive scenes before explicit integration.");
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
            {
                var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneAt(index);
                if (loaded.isLoaded && loaded.isDirty)
                    throw new InvalidOperationException("Save pending scene edits before integration: " + loaded.name);
            }
            var saved = previous.Where(entry => !string.IsNullOrEmpty(entry.path)).ToArray();
            SportsTruckCompatibilityAuthoring.ShouldRestoreSceneSetup(saved, Application.isBatchMode);
            try
            {
                string folder = mode == "TruckKeepawayFirstPerson" ? "TruckKeepaway" : mode;
                var scene = EditorSceneManager.OpenScene("Assets/" + folder + "/Scenes/" + mode + ".unity", OpenSceneMode.Single);
                action();
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save integrated scene: " + scene.path);
            }
            finally { SportsTruckCompatibilityAuthoring.RestorePreviousSceneSetup(saved); }
        }
    }
}
