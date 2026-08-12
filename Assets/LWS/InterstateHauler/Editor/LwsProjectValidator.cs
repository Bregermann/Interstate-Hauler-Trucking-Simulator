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
        private const string PlayerTruckPrefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab";
        private const string TestTrailerPrefabPath = "Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab";
        private const string TruckDefinitionPath = "Assets/LWS/InterstateHauler/Vehicles/Data/IH_TruckDefinition_StarterNwhSemi.asset";
        private const string TruckValidationScenePath = "Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity";
        private const string SelectedNwhTruckPath = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab";
        private const string SelectedNwhTrailerPath = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab";
        private const string LogitechG29ProfilePath = "Assets/LWS/InterstateHauler/Input/Data/IH_LogitechG29Profile.asset";
        private const string DefaultG29CalibrationPath = "Assets/LWS/InterstateHauler/Input/Data/IH_DefaultG29Calibration.asset";
        private const string UnityDirectInputPackageName = "com.directinput.unity";
        private const string UnityDirectInputPackageUrl = "https://github.com/imDanoush/Unity-DirectInput.git";
        private const string UnityDirectInputNwhSamplePackageName = "NWHVehiclePhysics2FFB.unitypackage";
        private const string CancelledNwhSteeringWheelInputImportedProviderPath = "Assets/NWH/Vehicle Physics 2/_OptionalPackages/Input/SteeringWheelInput/SteeringWheelInputProvider.cs";

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
            ValidateVehicleBaseline(report);
            ValidateWheelInputFoundation(report);
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

        private static void ValidateVehicleBaseline(LwsProjectValidationReport report)
        {
            report.Add(
                File.Exists(PlayerTruckPrefabPath) ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Player Truck Prefab",
                File.Exists(PlayerTruckPrefabPath)
                    ? $"{PlayerTruckPrefabPath} exists."
                    : $"{PlayerTruckPrefabPath} is missing.");

            report.Add(
                File.Exists(TestTrailerPrefabPath) ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Test Trailer Prefab",
                File.Exists(TestTrailerPrefabPath)
                    ? $"{TestTrailerPrefabPath} exists."
                    : $"{TestTrailerPrefabPath} is missing.");

            report.Add(
                File.Exists(TruckValidationScenePath) ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Validation Scene",
                File.Exists(TruckValidationScenePath)
                    ? $"{TruckValidationScenePath} exists and is intentionally not a production scene."
                    : $"{TruckValidationScenePath} is missing.");

            ValidateTruckDefinition(report);
            ValidateSelectedNwhPrefabs(report);
            ValidateTruckValidationSceneText(report);
            ValidateUtsPlayerVehicleSeparation(report);
        }

        private static void ValidateTruckDefinition(LwsProjectValidationReport report)
        {
            LwsTruckDefinition definition = AssetDatabase.LoadAssetAtPath<LwsTruckDefinition>(TruckDefinitionPath);
            if (definition == null)
            {
                report.Add(LwsValidationSeverity.Error, "Truck Definition", $"{TruckDefinitionPath} was not found or did not import.");
                return;
            }

            bool valid = definition.Validate(out string message);
            report.Add(valid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error, "Truck Definition", message);

            bool idsValid = LwsTruckDefinition.ValidateUniqueIds(new[] { definition }, out string idsMessage);
            report.Add(idsValid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error, "Truck Definition IDs", idsMessage);

            report.Add(
                definition.ValidationTrailerPrefab != null ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Definition Trailer",
                definition.ValidationTrailerPrefab != null
                    ? "Validation trailer prefab is assigned."
                    : "Validation trailer prefab is missing.");
        }

        private static void ValidateSelectedNwhPrefabs(LwsProjectValidationReport report)
        {
            GameObject selectedTruck = AssetDatabase.LoadAssetAtPath<GameObject>(SelectedNwhTruckPath);
            GameObject selectedTrailer = AssetDatabase.LoadAssetAtPath<GameObject>(SelectedNwhTrailerPath);
            report.Add(
                selectedTruck != null ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Selected NWH Tractor",
                selectedTruck != null ? SelectedNwhTruckPath : $"{SelectedNwhTruckPath} could not be loaded.");
            report.Add(
                selectedTrailer != null ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Selected NWH Trailer",
                selectedTrailer != null ? SelectedNwhTrailerPath : $"{SelectedNwhTrailerPath} could not be loaded.");

            string truckText = File.Exists(SelectedNwhTruckPath) ? File.ReadAllText(SelectedNwhTruckPath) : string.Empty;
            string trailerText = File.Exists(SelectedNwhTrailerPath) ? File.ReadAllText(SelectedNwhTrailerPath) : string.Empty;
            bool truckHasVehicleController = truckText.Contains("guid: fef33320c8b07754cbbcaa651106e4f3");
            bool truckHasHitch = truckText.Contains("guid: 75433d1dc80d018488a61160e924cce2");
            bool trailerHasModule = trailerText.Contains("guid: 3e5c0ce4bb7d92d49b1bac374cd2dd2c");

            report.Add(
                truckHasVehicleController ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "NWH Components",
                truckHasVehicleController ? "Selected tractor includes NWH VehicleController." : "Selected tractor is missing NWH VehicleController.");
            report.Add(
                truckHasHitch && trailerHasModule ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Trailer Coupling Components",
                truckHasHitch && trailerHasModule
                    ? "Selected tractor has NWH TrailerHitch and selected trailer has NWH TrailerModule."
                    : "Selected NWH coupling components are incomplete.");
        }

        private static void ValidateTruckValidationSceneText(LwsProjectValidationReport report)
        {
            if (!File.Exists(TruckValidationScenePath))
            {
                return;
            }

            string sceneText = File.ReadAllText(TruckValidationScenePath);
            bool hasSpawner = sceneText.Contains("LwsPlayerTruckSpawner") || sceneText.Contains("guid: a7d55a3f708f41dab0fb1dbfa279c0a1");
            bool hasMarker = sceneText.Contains("LwsTruckValidationSceneMarker") || sceneText.Contains("guid: 5f9ed500fc0f4459a3b4319a95d9ba7e");
            bool hasNwhInput = sceneText.Contains("guid: 0fe154161bba5034094381e28d5e1da4");
            bool hasGround = sceneText.Contains("m_Name: Validation Driving Pad");

            report.Add(
                hasSpawner && hasMarker && hasNwhInput && hasGround ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Validation Setup",
                hasSpawner && hasMarker && hasNwhInput && hasGround
                    ? "Truck validation scene contains the LWS spawner, marker, temporary NWH input provider, and driving pad."
                    : "Truck validation scene is missing one or more required validation objects.");
        }

        private static void ValidateUtsPlayerVehicleSeparation(LwsProjectValidationReport report)
        {
            string[] lwsVehicleFiles = Directory.Exists("Assets/LWS/InterstateHauler/Vehicles")
                ? Directory.GetFiles("Assets/LWS/InterstateHauler/Vehicles", "*.*", SearchOption.AllDirectories)
                : Array.Empty<string>();

            var offendingFiles = new List<string>();
            foreach (string file in lwsVehicleFiles.Where(path => path.EndsWith(".cs", StringComparison.Ordinal) ||
                                                                  path.EndsWith(".prefab", StringComparison.Ordinal) ||
                                                                  path.EndsWith(".unity", StringComparison.Ordinal)))
            {
                string text = File.ReadAllText(file);
                if (text.IndexOf("CarMove", StringComparison.Ordinal) >= 0 || text.IndexOf("AddTrailer", StringComparison.Ordinal) >= 0)
                {
                    offendingFiles.Add(file.Replace('\\', '/'));
                }
            }

            report.Add(
                offendingFiles.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "UTS Player Vehicle Separation",
                offendingFiles.Count == 0
                    ? "No UTS player-driving or UTS trailer-driving markers were found in LWS vehicle assets."
                    : "UTS player-driving markers found in LWS vehicle assets: " + string.Join(", ", offendingFiles));
        }

        private static void ValidateWheelInputFoundation(LwsProjectValidationReport report)
        {
            ValidateG29Profile(report);
            ValidateG29Calibration(report);
            ValidateDirectInputWheelIntegration(report);
            ValidateTruckValidationWheelSetup(report);
        }

        private static void ValidateG29Profile(LwsProjectValidationReport report)
        {
            LwsWheelDeviceProfile profile = AssetDatabase.LoadAssetAtPath<LwsWheelDeviceProfile>(LogitechG29ProfilePath);
            if (profile == null)
            {
                report.Add(LwsValidationSeverity.Error, "G29 Profile", $"{LogitechG29ProfilePath} was not found or did not import.");
                return;
            }

            bool valid = profile.Validate(out string message);
            report.Add(valid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error, "G29 Profile", message);
            report.Add(
                profile.CompatibilityStatus == LwsWheelCompatibilityStatus.SupportedPhysicallyVerified
                    ? LwsValidationSeverity.Info
                    : LwsValidationSeverity.Warning,
                "G29 Physical Verification",
                $"Current G29 compatibility status is {profile.CompatibilityStatus}.");
        }

        private static void ValidateG29Calibration(LwsProjectValidationReport report)
        {
            LwsWheelCalibrationAsset calibration = AssetDatabase.LoadAssetAtPath<LwsWheelCalibrationAsset>(DefaultG29CalibrationPath);
            if (calibration == null)
            {
                report.Add(LwsValidationSeverity.Error, "G29 Calibration", $"{DefaultG29CalibrationPath} was not found or did not import.");
                return;
            }

            bool valid = calibration.Validate(out string message);
            report.Add(valid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error, "G29 Calibration", message);

            LwsWheelCalibrationProfile profile = calibration.Profile;
            report.Add(
                profile != null && profile.HasRequiredDrivingBindings() ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "G29 Binding Completeness",
                profile != null && profile.HasRequiredDrivingBindings()
                    ? "Default G29 calibration has steering, pedals, shifter, range, and splitter bindings."
                    : "Default G29 calibration is intentionally unbound until hardware calibration records Unity control paths.");

            ValidateDuplicateWheelBindings(report, profile);
        }

        private static void ValidateDuplicateWheelBindings(LwsProjectValidationReport report, LwsWheelCalibrationProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            var paths = new Dictionary<string, LwsWheelLogicalControl>(StringComparer.Ordinal);
            var duplicates = new List<string>();
            LwsWheelLogicalControl[] controls =
            {
                LwsWheelLogicalControl.Steering,
                LwsWheelLogicalControl.Throttle,
                LwsWheelLogicalControl.Brake,
                LwsWheelLogicalControl.Clutch,
                LwsWheelLogicalControl.ShifterGate1,
                LwsWheelLogicalControl.ShifterGate2,
                LwsWheelLogicalControl.ShifterGate3,
                LwsWheelLogicalControl.ShifterGate4,
                LwsWheelLogicalControl.ShifterGate5,
                LwsWheelLogicalControl.ShifterGate6,
                LwsWheelLogicalControl.ShifterReverse,
                LwsWheelLogicalControl.RangeToggle,
                LwsWheelLogicalControl.SplitterToggle
            };

            foreach (LwsWheelLogicalControl control in controls)
            {
                LwsWheelControlBinding binding = profile.GetBinding(control);
                if (!binding.IsBound)
                {
                    continue;
                }

                if (paths.TryGetValue(binding.controlPath, out LwsWheelLogicalControl existing))
                {
                    duplicates.Add($"{existing}/{control}: {binding.controlPath}");
                }
                else
                {
                    paths.Add(binding.controlPath, control);
                }
            }

            report.Add(
                duplicates.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "G29 Duplicate Bindings",
                duplicates.Count == 0
                    ? "No duplicate wheel binding paths were found in the default calibration asset."
                    : "Duplicate wheel bindings found: " + string.Join(", ", duplicates));
        }

        private static void ValidateDirectInputWheelIntegration(LwsProjectValidationReport report)
        {
            string manifest = File.Exists("Packages/manifest.json") ? File.ReadAllText("Packages/manifest.json") : string.Empty;
            string lockFile = File.Exists("Packages/packages-lock.json") ? File.ReadAllText("Packages/packages-lock.json") : string.Empty;
            bool packageDeclared = manifest.Contains($"\"{UnityDirectInputPackageName}\"") && manifest.Contains(UnityDirectInputPackageUrl);
            bool packageLocked = lockFile.Contains($"\"{UnityDirectInputPackageName}\"") && lockFile.Contains("\"hash\":");
            bool backendAvailable = LwsDirectInputBackendDiscovery.IsUnityDirectInputAvailable();
            bool sampleAvailable = FindUnityDirectInputNwhSamplePath() != null;
            bool cancelledNwhImportPresent = File.Exists(CancelledNwhSteeringWheelInputImportedProviderPath);
            bool logitechSdkPresent = Directory.Exists("Assets/LogitechSDK") ||
                                      Directory.GetFiles("Assets", "*LogitechSteeringWheel*.cs", SearchOption.AllDirectories).Length > 0;

            report.Add(
                packageDeclared ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Unity-DirectInput Package",
                packageDeclared
                    ? $"{UnityDirectInputPackageName} is declared through {UnityDirectInputPackageUrl}."
                    : $"{UnityDirectInputPackageName} is not declared in Packages/manifest.json.");

            report.Add(
                packageLocked ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Unity-DirectInput Lock",
                packageLocked
                    ? $"{UnityDirectInputPackageName} is present in packages-lock.json."
                    : $"{UnityDirectInputPackageName} has not yet been resolved into packages-lock.json.");

            report.Add(
                backendAvailable ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "DirectInput Backend",
                backendAvailable
                    ? "Unity-DirectInput DIManager type is loaded."
                    : "Unity-DirectInput DIManager type is not loaded; restart/open the normal Editor after package resolution.");

            report.Add(
                sampleAvailable ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "DirectInput NWH v13 Sample",
                sampleAvailable
                    ? $"{UnityDirectInputNwhSamplePackageName} is available in the package cache for reference."
                    : $"{UnityDirectInputNwhSamplePackageName} was not found in the package cache.");

            report.Add(
                cancelledNwhImportPresent ? LwsValidationSeverity.Warning : LwsValidationSeverity.Info,
                "Cancelled NWH SteeringWheelInput Import",
                cancelledNwhImportPresent
                    ? "Cancelled NWH SteeringWheelInput provider source is present under Assets/NWH; do not use it for Prompt 005."
                    : "Cancelled NWH SteeringWheelInput provider source is not imported.");

            report.Add(
                logitechSdkPresent ? LwsValidationSeverity.Warning : LwsValidationSeverity.Info,
                "Logitech SDK",
                logitechSdkPresent
                    ? "Logitech SDK source appears present but is no longer the selected Prompt 005 backend."
                    : "Logitech SDK source is not required; Unity-DirectInput is the selected Windows backend.");
        }

        private static void ValidateTruckValidationWheelSetup(LwsProjectValidationReport report)
        {
            if (!File.Exists(TruckValidationScenePath))
            {
                return;
            }

            string sceneText = File.ReadAllText(TruckValidationScenePath);
            bool hasWheelSource = sceneText.Contains("guid: 0bc1603391544e9e9c71838698b99863");
            bool hasNwhBridge = sceneText.Contains("guid: aaf4c90435784e48b5c883f7477f3dfa");
            bool hasWheelBootstrap = sceneText.Contains("guid: d48253881e1b46158e2f4bec4932089c");
            bool hasCalibrationPanel = sceneText.Contains("guid: 97dea31e674549a89ea371c2ee7bae60");
            bool hasDebugPanel = sceneText.Contains("guid: 28075828471f47f9b09e54cc890234fd");
            bool hasDiscoveryPanel = sceneText.Contains("guid: 24b53896cf0f4b9fb9daffa32bbef6b5");
            bool hasFfbCoordinator = sceneText.Contains("guid: 1f05df0c93cf43d18a9420ac969bb4a7");
            bool hasStockNwhVehicleProvider = sceneText.Contains("guid: 0fe154161bba5034094381e28d5e1da4");

            report.Add(
                hasWheelSource && hasNwhBridge && hasWheelBootstrap && hasCalibrationPanel && hasDebugPanel && hasDiscoveryPanel && hasFfbCoordinator
                    ? LwsValidationSeverity.Info
                    : LwsValidationSeverity.Error,
                "G29 TruckValidation Setup",
                hasWheelSource && hasNwhBridge && hasWheelBootstrap && hasCalibrationPanel && hasDebugPanel && hasDiscoveryPanel && hasFfbCoordinator
                    ? "TruckValidation contains the LWS DirectInput wheel source, NWH bridge, ownership bootstrap, calibration panel, debug panel, discovery panel, and FFB coordinator."
                    : "TruckValidation is missing one or more LWS DirectInput/G29 validation components.");

            report.Add(
                hasStockNwhVehicleProvider && hasWheelBootstrap ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "NWH Input Double Feed",
                hasStockNwhVehicleProvider && hasWheelBootstrap
                    ? "Stock NWH vehicle input remains available for fallback and is disabled by LWS while wheel ownership is active."
                    : "Wheel/fallback ownership could not be fully confirmed from TruckValidation scene text.");
        }

        private static string FindUnityDirectInputNwhSamplePath()
        {
            if (!Directory.Exists("Library/PackageCache"))
            {
                return null;
            }

            foreach (string packageDirectory in Directory.GetDirectories("Library/PackageCache", "com.directinput.unity@*", SearchOption.TopDirectoryOnly))
            {
                string samplePath = Path.Combine(packageDirectory, "Samples~", "nwhvp", UnityDirectInputNwhSamplePackageName);
                if (File.Exists(samplePath))
                {
                    return samplePath;
                }
            }

            return null;
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
