using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LWS.InterstateHauler.Editor
{
    public enum LwsValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class LwsValidationItem
    {
        public LwsValidationItem(LwsValidationSeverity severity, string title, string message)
        {
            Severity = severity;
            Title = title;
            Message = message;
        }

        public LwsValidationSeverity Severity { get; }
        public string Title { get; }
        public string Message { get; }
    }

    public sealed class LwsProjectValidationReport
    {
        private readonly List<LwsValidationItem> _items = new List<LwsValidationItem>();

        public IReadOnlyList<LwsValidationItem> Items => _items;
        public bool HasErrors => _items.Any(i => i.Severity == LwsValidationSeverity.Error);

        public void Add(LwsValidationSeverity severity, string title, string message)
        {
            _items.Add(new LwsValidationItem(severity, title, message));
        }
    }

    public static class LwsProjectValidator
    {
        public const string MenuPath = "Interstate Hauler/Validate Project";
        private const string PcRpAssetPath = "Assets/Settings/PC_RPAsset.asset";
        private const string DefaultVolumeProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";

        private static readonly string[] VendorRoots =
        {
            "Assets/NWH",
            "Assets/EasyRoads3D",
            "Assets/UTS_FullPack",
            "Assets/WeatherMaker",
            "Assets/NOT_Lonely",
            "Assets/Plugins/Kronnect",
            "Assets/Plugins/Pixel Crushers",
            "Assets/Plugins/Demigiant",
            "Assets/PinwheelStudio",
            "Assets/RiverModeler",
            "Packages/xyz.staggart-creations.spline-spawner"
        };

        [MenuItem(MenuPath)]
        public static void ValidateFromMenu()
        {
            LwsProjectValidationReport report = RunValidation();
            foreach (LwsValidationItem item in report.Items)
            {
                string line = $"[{item.Severity}] {item.Title}: {item.Message}";
                if (item.Severity == LwsValidationSeverity.Error)
                {
                    Debug.LogError(line);
                }
                else if (item.Severity == LwsValidationSeverity.Warning)
                {
                    Debug.LogWarning(line);
                }
                else
                {
                    Debug.Log(line);
                }
            }

            Debug.Log($"Interstate Hauler validation complete. Items: {report.Items.Count}, Errors: {report.Items.Count(i => i.Severity == LwsValidationSeverity.Error)}");
        }

        public static LwsProjectValidationReport RunValidation()
        {
            var report = new LwsProjectValidationReport();
            ValidateRenderPipeline(report);
            ValidateColorSpace(report);
            ValidateBuildScenes(report);
            ValidateDefaultVolumeProfile(report);
            ValidateServiceRegistration(report);
            ValidateVendorManagersInProjectScenes(report);
            ValidateSaveParticipants(report);
            ValidateRoadGraphData(report);
            ValidateProjectOwnership(report);
            return report;
        }

        private static void ValidateRenderPipeline(LwsProjectValidationReport report)
        {
            bool graphicsHasNoPipeline = File.ReadAllText("ProjectSettings/GraphicsSettings.asset")
                .Contains("m_CustomRenderPipeline: {fileID: 0}");
            bool qualityHasNoPipeline = File.ReadAllText("ProjectSettings/QualitySettings.asset")
                .Contains("customRenderPipeline: {fileID: 0}");
            bool pcAssetExists = File.Exists(PcRpAssetPath);

            if (graphicsHasNoPipeline)
            {
                report.Add(LwsValidationSeverity.Error, "Render Pipeline", "Graphics Settings has no active custom render pipeline asset.");
            }

            if (qualityHasNoPipeline)
            {
                report.Add(LwsValidationSeverity.Warning, "Quality Render Pipeline", "At least one quality level has no custom render pipeline asset.");
            }

            report.Add(
                pcAssetExists ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Intended PC URP Asset",
                pcAssetExists ? $"{PcRpAssetPath} exists but was not activated by Prompt 002." : $"{PcRpAssetPath} was not found.");
        }

        private static void ValidateColorSpace(LwsProjectValidationReport report)
        {
            ColorSpace colorSpace = PlayerSettings.colorSpace;
            report.Add(
                colorSpace == ColorSpace.Linear ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Color Space",
                $"Current color space is {colorSpace}. Prompt 003 should decide whether to switch to Linear.");
        }

        private static void ValidateBuildScenes(LwsProjectValidationReport report)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            bool bootstrapIsIndexZero = scenes.Length > 0 &&
                                        scenes[0].enabled &&
                                        scenes[0].path == LwsApplicationBootstrap.BootstrapScenePath;
            report.Add(
                bootstrapIsIndexZero ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Build Scenes",
                bootstrapIsIndexZero
                    ? $"{LwsApplicationBootstrap.BootstrapScenePath} is build scene index 0."
                    : "LWS Bootstrap scene is not enabled at build scene index 0.");
        }

        private static void ValidateDefaultVolumeProfile(LwsProjectValidationReport report)
        {
            if (!File.Exists(DefaultVolumeProfilePath))
            {
                report.Add(LwsValidationSeverity.Warning, "Default Volume Profile", $"{DefaultVolumeProfilePath} was not found.");
                return;
            }

            string text = File.ReadAllText(DefaultVolumeProfilePath);
            int missingScriptCount = CountOccurrences(text, "m_Script: {fileID: 0}");
            report.Add(
                missingScriptCount > 0 ? LwsValidationSeverity.Error : LwsValidationSeverity.Info,
                "Default Volume Profile",
                missingScriptCount > 0
                    ? $"{DefaultVolumeProfilePath} contains {missingScriptCount} missing script references."
                    : $"{DefaultVolumeProfilePath} has no exact missing script references.");
        }

        private static void ValidateServiceRegistration(LwsProjectValidationReport report)
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            IReadOnlyList<LwsServiceDiagnostic> diagnostics = registry.ValidateRegistration();
            if (diagnostics.Count > 0)
            {
                foreach (LwsServiceDiagnostic diagnostic in diagnostics)
                {
                    report.Add(LwsValidationSeverity.Error, "Service Registration", $"{diagnostic.ServiceId}: {diagnostic.Message}");
                }
                return;
            }

            LwsServiceResult init = registry.InitializeAll();
            if (!init.Succeeded)
            {
                report.Add(LwsValidationSeverity.Error, "Service Initialization", init.Message);
                return;
            }

            report.Add(LwsValidationSeverity.Info, "Service Initialization", "Default LWS services initialize in deterministic order.");
            LwsServiceResult shutdown = registry.ShutdownAll();
            report.Add(
                shutdown.Succeeded ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Service Shutdown",
                shutdown.Message);
        }

        private static void ValidateVendorManagersInProjectScenes(LwsProjectValidationReport report)
        {
            string[] lwsScenes = Directory.GetFiles("Assets/LWS/InterstateHauler", "*.unity", SearchOption.AllDirectories);
            string[] vendorMarkers =
            {
                "CompassNavigatorPro",
                "WeatherMaker",
                "PixelCrushers",
                "UTS_FullPack",
                "NWH.VehiclePhysics2"
            };

            foreach (string scenePath in lwsScenes)
            {
                string text = File.ReadAllText(scenePath);
                foreach (string marker in vendorMarkers)
                {
                    if (text.Contains(marker))
                    {
                        report.Add(LwsValidationSeverity.Warning, "Vendor Managers", $"{scenePath} contains vendor marker {marker}.");
                    }
                }
            }

            report.Add(LwsValidationSeverity.Info, "Vendor Managers", "Project-owned scenes were checked for obvious vendor manager markers.");
        }

        private static void ValidateSaveParticipants(LwsProjectValidationReport report)
        {
            LwsServiceRegistry registry = LwsApplicationBootstrap.CreateDefaultRegistry();
            registry.InitializeAll();
            ILwsSaveService saveService = registry.GetRequired<ILwsSaveService>();
            LwsSaveOperationResult result = saveService.ValidateParticipants();
            report.Add(
                result.Succeeded ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Save Participants",
                result.Succeeded ? $"Save participant IDs are unique. Count: {saveService.Participants.Count}" : result.Message);
            registry.ShutdownAll();
        }

        private static void ValidateRoadGraphData(LwsProjectValidationReport report)
        {
            var graph = new LwsRoadGraph
            {
                graphId = "validator",
                nodes = new List<LwsRoadNode>
                {
                    new LwsRoadNode { nodeId = "a", position = Vector3.zero },
                    new LwsRoadNode { nodeId = "b", position = Vector3.forward }
                },
                edges = new List<LwsRoadEdge>
                {
                    new LwsRoadEdge { edgeId = "ab", fromNodeId = "a", toNodeId = "b", distanceMeters = 1f }
                }
            };

            LwsRoadGraphValidationResult validation = graph.Validate();
            string json = graph.ToJson();
            LwsRoadGraph copy = LwsRoadGraph.FromJson(json);
            bool roundTrip = copy != null && copy.nodes.Count == 2 && copy.edges.Count == 1;

            report.Add(
                validation.IsValid && roundTrip ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Road Graph Data",
                validation.IsValid && roundTrip ? "Road graph IDs validate and JSON round-trip succeeds." : validation.Summary);
        }

        private static void ValidateProjectOwnership(LwsProjectValidationReport report)
        {
            var misplaced = new List<string>();
            foreach (string root in VendorRoots.Where(Directory.Exists))
            {
                foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    string text = File.ReadAllText(file);
                    if (text.Contains("namespace LWS.InterstateHauler"))
                    {
                        misplaced.Add(file.Replace('\\', '/'));
                    }
                }
            }

            report.Add(
                misplaced.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Project Ownership",
                misplaced.Count == 0
                    ? "No LWS scripts were found under known vendor roots."
                    : "LWS scripts found under vendor roots: " + string.Join(", ", misplaced));
        }

        private static int CountOccurrences(string text, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }
    }
}
