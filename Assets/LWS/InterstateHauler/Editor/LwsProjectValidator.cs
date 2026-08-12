using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

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
        private const string LwsRenderingSettingsPath = "Assets/LWS/InterstateHauler/Rendering/Data/IH_RenderingSettings.asset";
        private const string LwsDefaultVolumeProfilePath = "Assets/LWS/InterstateHauler/Rendering/Volume/IH_DefaultVolumeProfile.asset";
        private const string RenderValidationScenePath = "Assets/LWS/InterstateHauler/Rendering/Validation/RenderValidation.unity";

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
            ValidateRenderingFoundation(report);
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
                pcAssetExists ? $"{PcRpAssetPath} exists for the active PC render pipeline." : $"{PcRpAssetPath} was not found.");
        }

        private static void ValidateColorSpace(LwsProjectValidationReport report)
        {
            ColorSpace colorSpace = PlayerSettings.colorSpace;
            report.Add(
                colorSpace == ColorSpace.Linear ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Color Space",
                $"Current color space is {colorSpace}.");
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
            bool pcPipelineReferencesLegacyProfile = File.Exists(PcRpAssetPath) &&
                                                     File.ReadAllText(PcRpAssetPath).Contains("guid: ab09877e2e707104187f6f83e2f62510");

            LwsValidationSeverity severity = LwsValidationSeverity.Info;
            if (missingScriptCount > 0)
            {
                severity = pcPipelineReferencesLegacyProfile ? LwsValidationSeverity.Error : LwsValidationSeverity.Warning;
            }

            report.Add(
                severity,
                "Legacy Default Volume Profile",
                missingScriptCount > 0
                    ? $"{DefaultVolumeProfilePath} contains {missingScriptCount} missing script references, but the active PC pipeline no longer references it."
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

        private static void ValidateRenderingFoundation(LwsProjectValidationReport report)
        {
            ValidateActiveRenderPipeline(report);
            ValidateQualityRenderPipelines(report);
            ValidateCleanLwsVolumeProfile(report);
            ValidateRenderingSettingsAsset(report);
            ValidateRenderValidationScene(report);
            ValidateWeatherMakerUrpStatus(report);
            ValidateWeatheradeUrpStatus(report);
            ValidateEasyRoadsUrpStatus(report);
            ValidateVfxGraphStatus(report);
        }

        private static void ValidateActiveRenderPipeline(LwsProjectValidationReport report)
        {
            RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
            string pipelinePath = pipeline != null ? AssetDatabase.GetAssetPath(pipeline) : string.Empty;
            bool isUrp = pipeline != null &&
                         pipeline.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";

            report.Add(
                isUrp ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Active Render Pipeline",
                isUrp ? $"URP is active through {pipelinePath}." : "URP is not active in Graphics Settings.");

            string manifest = File.Exists("Packages/manifest.json") ? File.ReadAllText("Packages/manifest.json") : string.Empty;
            string lockFile = File.Exists("Packages/packages-lock.json") ? File.ReadAllText("Packages/packages-lock.json") : string.Empty;
            bool urp174 = manifest.Contains("\"com.unity.render-pipelines.universal\"") && lockFile.Contains("\"version\": \"17.4.0\"");
            report.Add(
                urp174 ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "URP Version",
                urp174 ? "URP 17.4.0 is present in the package lock." : "URP 17.4.0 was not confirmed from package files.");

            report.Add(
                PlayerSettings.colorSpace == ColorSpace.Linear ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Linear Color Space",
                $"Current color space is {PlayerSettings.colorSpace}.");
        }

        private static void ValidateQualityRenderPipelines(LwsProjectValidationReport report)
        {
            string text = File.Exists("ProjectSettings/QualitySettings.asset")
                ? File.ReadAllText("ProjectSettings/QualitySettings.asset")
                : string.Empty;

            string[] requiredNames = { "Ultra", "High", "Medium", "Low", "Steam Deck" };
            foreach (string requiredName in requiredNames)
            {
                bool hasName = TryGetQualitySettingsBlock(text, requiredName, out string qualityBlock);
                bool sectionHasPipeline = qualityBlock.Contains("customRenderPipeline: {fileID: 11400000");
                report.Add(
                    hasName && sectionHasPipeline ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                    "Quality Pipeline",
                    hasName && sectionHasPipeline
                        ? $"{requiredName} quality tier has a URP pipeline asset assigned."
                        : $"{requiredName} quality tier is missing or has no URP pipeline asset.");
            }
        }

        private static bool TryGetQualitySettingsBlock(string text, string qualityName, out string block)
        {
            string marker = $"    name: {qualityName}";
            int start = text.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
            {
                block = string.Empty;
                return false;
            }

            int next = text.IndexOf("\n  - serializedVersion:", start + marker.Length, StringComparison.Ordinal);
            block = next >= 0 ? text.Substring(start, next - start) : text.Substring(start);
            return true;
        }

        private static void ValidateCleanLwsVolumeProfile(LwsProjectValidationReport report)
        {
            if (!File.Exists(LwsDefaultVolumeProfilePath))
            {
                report.Add(LwsValidationSeverity.Error, "LWS Default Volume", $"{LwsDefaultVolumeProfilePath} does not exist.");
                return;
            }

            string text = File.ReadAllText(LwsDefaultVolumeProfilePath);
            int missingScriptCount = CountOccurrences(text, "m_Script: {fileID: 0}");
            report.Add(
                missingScriptCount == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "LWS Default Volume",
                missingScriptCount == 0
                    ? $"{LwsDefaultVolumeProfilePath} has no exact missing script references."
                    : $"{LwsDefaultVolumeProfilePath} contains {missingScriptCount} missing script references.");
        }

        private static void ValidateRenderingSettingsAsset(LwsProjectValidationReport report)
        {
            LwsRenderingSettings settings = AssetDatabase.LoadAssetAtPath<LwsRenderingSettings>(LwsRenderingSettingsPath);
            if (settings == null)
            {
                report.Add(LwsValidationSeverity.Error, "Rendering Settings", $"{LwsRenderingSettingsPath} was not found or did not import.");
                return;
            }

            bool valid = settings.Validate(out string message);
            report.Add(valid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error, "Rendering Settings", message);

            bool steamDeckExists = settings.TryGetProfile(LwsRenderQualityTier.SteamDeck, out LwsRenderingQualityProfile profile) &&
                                   profile.renderPipelineAsset != null;
            report.Add(
                steamDeckExists ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Steam Deck Profile",
                steamDeckExists ? "Steam Deck rendering profile exists." : "Steam Deck rendering profile is missing or incomplete.");
        }

        private static void ValidateRenderValidationScene(LwsProjectValidationReport report)
        {
            report.Add(
                File.Exists(RenderValidationScenePath) ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Render Validation Scene",
                File.Exists(RenderValidationScenePath)
                    ? $"{RenderValidationScenePath} exists and is intentionally not in Build Settings."
                    : $"{RenderValidationScenePath} is missing.");
        }

        private static void ValidateWeatherMakerUrpStatus(LwsProjectValidationReport report)
        {
            string projectSettings = File.Exists("ProjectSettings/ProjectSettings.asset")
                ? File.ReadAllText("ProjectSettings/ProjectSettings.asset")
                : string.Empty;
            bool defineSet = projectSettings.Contains("UNITY_URP");
            bool renderFeatureSourcePresent = File.Exists("Assets/WeatherMaker/Prefab/ScriptableRenderPipeline/URP/WeatherMakerURPRenderFeatureScript.cs");
            bool rendererFeatureOnPcRenderer = File.Exists("Assets/Settings/PC_Renderer.asset") &&
                                               File.ReadAllText("Assets/Settings/PC_Renderer.asset").Contains("WeatherMakerURPRenderFeatureScript");

            report.Add(
                defineSet && renderFeatureSourcePresent ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Weather Maker URP",
                defineSet && renderFeatureSourcePresent
                    ? "UNITY_URP define is present and Weather Maker URP render feature source exists."
                    : "Weather Maker URP support is not fully confirmed.");

            report.Add(
                rendererFeatureOnPcRenderer ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Weather Maker Render Feature",
                rendererFeatureOnPcRenderer
                    ? "PC renderer includes a Weather Maker URP render feature."
                    : "PC renderer does not show a Weather Maker render feature; verify through the normal Unity Editor vendor setup.");
        }

        private static void ValidateWeatheradeUrpStatus(LwsProjectValidationReport report)
        {
            bool supportPackageExists = File.Exists("Assets/NOT_Lonely/Weatherade SRS/URP Support/WeatheradeSRS_URP.unitypackage") &&
                                        File.Exists("Assets/NOT_Lonely/Weatherade SRS/URP Support/WeatheradeSRS_URP_17_1.unitypackage");
            string projectSettings = File.Exists("ProjectSettings/ProjectSettings.asset")
                ? File.ReadAllText("ProjectSettings/ProjectSettings.asset")
                : string.Empty;
            bool usingUrpDefine = projectSettings.Contains("USING_URP");

            report.Add(
                supportPackageExists ? LwsValidationSeverity.Warning : LwsValidationSeverity.Error,
                "Weatherade URP",
                supportPackageExists
                    ? (usingUrpDefine
                        ? "Weatherade URP support packages exist and USING_URP is defined; verify imported render features in Editor."
                        : "Weatherade URP support packages exist but were not imported/enabled by Prompt 003.")
                    : "Weatherade URP support packages were not found.");
        }

        private static void ValidateEasyRoadsUrpStatus(LwsProjectValidationReport report)
        {
            bool urp172Package = File.Exists("Assets/EasyRoads3D/SRP Support Packages/URP_17_2_0.unitypackage");
            bool urp174Package = Directory.Exists("Assets/EasyRoads3D/SRP Support Packages") &&
                                 Directory.GetFiles("Assets/EasyRoads3D/SRP Support Packages", "*17_4*", SearchOption.TopDirectoryOnly).Length > 0;

            report.Add(
                urp174Package ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "EasyRoads URP",
                urp174Package
                    ? "EasyRoads includes a URP 17.4 support package."
                    : urp172Package
                        ? "EasyRoads includes URP 17.2 support but no exact URP 17.4 package; visual validation is required."
                        : "No EasyRoads URP 17.x support package was found.");
        }

        private static void ValidateVfxGraphStatus(LwsProjectValidationReport report)
        {
            string lockFile = File.Exists("Packages/packages-lock.json") ? File.ReadAllText("Packages/packages-lock.json") : string.Empty;
            bool vfx174 = lockFile.Contains("\"com.unity.visualeffectgraph\"") && lockFile.Contains("\"version\": \"17.4.0\"");
            bool validationVfxExists = Directory.Exists("Assets/RiverModeler/VFX (URP)") &&
                                       Directory.GetFiles("Assets/RiverModeler/VFX (URP)", "*.vfx", SearchOption.AllDirectories).Length > 0;

            report.Add(
                vfx174 ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "VFX Graph",
                vfx174 ? "VFX Graph 17.4.0 is present in the package lock." : "VFX Graph 17.4.0 was not confirmed.");
            report.Add(
                validationVfxExists ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "River Modeler VFX",
                validationVfxExists
                    ? "River Modeler includes a URP VFX Graph asset for water surface splashes."
                    : "No River Modeler URP VFX Graph asset was found.");
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
