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
        private const string TransmissionDefinitionPath = "Assets/LWS/InterstateHauler/Vehicles/Transmission/Data/IH_18SpeedTransmission_G29_EatonDevelopment.asset";
        private const string TransmissionDefinitionGuid = "8f6ab81b8b1c4561bbfa66fb83e64b22";
        private const string TransmissionControllerPath = "Assets/LWS/InterstateHauler/Vehicles/Transmission/Lws18SpeedTransmissionController.cs";
        private const string TransmissionNwhAdapterPath = "Assets/LWS/InterstateHauler/Vehicles/Transmission/NWH/LwsNwh18SpeedTransmissionAdapter.cs";
        private const string NwhVehicleInputProviderPath = "Assets/LWS/InterstateHauler/Vehicles/NWH/LwsNwhVehicleInputProvider.cs";
        private const string TruckControlControllerPath = "Assets/LWS/InterstateHauler/Vehicles/Controls/LwsTruckControlController.cs";
        private const string TruckControlServicePath = "Assets/LWS/InterstateHauler/Vehicles/Controls/LwsTruckControlService.cs";
        private const string TruckControlTypesPath = "Assets/LWS/InterstateHauler/Vehicles/Controls/LwsTruckControlTypes.cs";
        private const string NwhTruckControlAdapterPath = "Assets/LWS/InterstateHauler/Vehicles/Controls/NWH/LwsNwhTruckControlAdapter.cs";
        private const string PlayerGestureControllerPath = "Assets/LWS/InterstateHauler/Vehicles/Controls/Gestures/LwsPlayerGestureController.cs";
        private const string KeyboardGamepadTruckInputPath = "Assets/LWS/InterstateHauler/Input/LwsKeyboardGamepadTruckInputSource.cs";
        private const string TruckControlDocsPath = "Documentation/InterstateHauler/007_Complete_Truck_Controls.md";
        private const string TruckControlMatrixPath = "Documentation/InterstateHauler/007_Truck_Control_Matrix.md";
        private const string NwhControlApiMatrixPath = "Documentation/InterstateHauler/007_NWH_Control_API_Matrix.md";
        private const string Prompt008HandoffPath = "Documentation/InterstateHauler/007_Prompt008_Handoff.md";
        private const string DashboardDefinitionPath = "Assets/LWS/InterstateHauler/Vehicles/Dashboard/Data/IH_DashboardDefinition_NwhSemi.asset";
        private const string DashboardControllerPath = "Assets/LWS/InterstateHauler/Vehicles/Dashboard/LwsTruckDashboardController.cs";
        private const string DashboardTypesPath = "Assets/LWS/InterstateHauler/Vehicles/Dashboard/LwsTruckDashboardTypes.cs";
        private const string MirrorControllerPath = "Assets/LWS/InterstateHauler/Vehicles/Mirrors/LwsTruckMirrorController.cs";
        private const string CabAnchorRegistryPath = "Assets/LWS/InterstateHauler/Vehicles/Cab/Accessories/LwsCabAccessoryAnchorRegistry.cs";
        private const string CabAnchorPath = "Assets/LWS/InterstateHauler/Vehicles/Cab/Accessories/LwsCabAccessoryAnchor.cs";
        private const string DashboardDocsPath = "Documentation/InterstateHauler/008_Dashboard_and_Mirrors.md";
        private const string DashboardBindingMatrixPath = "Documentation/InterstateHauler/008_Dashboard_Binding_Matrix.md";
        private const string MirrorQualityMatrixPath = "Documentation/InterstateHauler/008_Mirror_Quality_Matrix.md";
        private const string CabAnchorMatrixPath = "Documentation/InterstateHauler/008_Cab_Anchor_Matrix.md";
        private const string Prompt009HandoffPath = "Documentation/InterstateHauler/008_Prompt009_Handoff.md";
        private const string InterstateCorridorScenePath = "Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity";
        private const string EasyRoadsExportBoundaryPath = "Assets/LWS/InterstateHauler/Roads/EasyRoads/LwsEasyRoadsExportBoundary.cs";
        private const string RoadGraphRuntimePath = "Assets/LWS/InterstateHauler/Roads/LwsRoadGraphRuntime.cs";
        private const string RoadGraphProviderPath = "Assets/LWS/InterstateHauler/Roads/LwsRoadGraphProvider.cs";
        private const string RoadSurfacePath = "Assets/LWS/InterstateHauler/Roads/LwsRoadSurface.cs";
        private const string RoadDebugPanelPath = "Assets/LWS/InterstateHauler/Roads/LwsRoadGraphDebugPanel.cs";
        private const string InterstateCorridorBuilderPath = "Assets/LWS/InterstateHauler/Roads/Validation/LwsInterstateCorridorRuntimeBuilder.cs";
        private const string InterstateCorridorMarkerPath = "Assets/LWS/InterstateHauler/Roads/Validation/LwsInterstateCorridorSceneMarker.cs";
        private const string Prompt009DocsPath = "Documentation/InterstateHauler/009_EasyRoads_Interstate_Corridor.md";
        private const string RoadGraphMatrixPath = "Documentation/InterstateHauler/009_Road_Graph_Matrix.md";
        private const string EasyRoadsApiMatrixPath = "Documentation/InterstateHauler/009_EasyRoads_API_Matrix.md";
        private const string CorridorTestMatrixPath = "Documentation/InterstateHauler/009_Corridor_Test_Matrix.md";
        private const string Prompt010HandoffPath = "Documentation/InterstateHauler/009_Prompt010_Handoff.md";
        private const string TrafficServicePath = "Assets/LWS/InterstateHauler/Traffic/LwsTraffic.cs";
        private const string TrafficLaneTypesPath = "Assets/LWS/InterstateHauler/Traffic/LwsTrafficLaneTypes.cs";
        private const string TrafficLaneBuilderPath = "Assets/LWS/InterstateHauler/Traffic/LwsTrafficLaneBuilder.cs";
        private const string TrafficIdentityPath = "Assets/LWS/InterstateHauler/Traffic/LwsTrafficIdentity.cs";
        private const string UtsTrafficApiPath = "Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsTrafficApi.cs";
        private const string UtsTrafficControllerPath = "Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsHighwayTrafficController.cs";
        private const string UtsTrafficDebugPanelPath = "Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsTrafficDebugPanel.cs";
        private const string UtsTrafficProfileScriptPath = "Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsTrafficProfile.cs";
        private const string InterstateTrafficProfilePath = "Assets/LWS/InterstateHauler/Traffic/Data/IH_TrafficProfile_InterstateValidation.asset";
        private const string Prompt010DocsPath = "Documentation/InterstateHauler/010_UTS_Highway_Integration.md";
        private const string UtsApiMatrixPath = "Documentation/InterstateHauler/010_UTS_API_Matrix.md";
        private const string TrafficLaneMatrixPath = "Documentation/InterstateHauler/010_Traffic_Lane_Matrix.md";
        private const string TrafficVehicleMatrixPath = "Documentation/InterstateHauler/010_Traffic_Vehicle_Matrix.md";
        private const string TrafficPerformanceMatrixPath = "Documentation/InterstateHauler/010_Performance_Matrix.md";
        private const string Prompt011HandoffPath = "Documentation/InterstateHauler/010_Prompt011_Handoff.md";
        private const string NavigationServicePath = "Assets/LWS/InterstateHauler/Navigation/LwsNavigation.cs";
        private const string NavigationTypesPath = "Assets/LWS/InterstateHauler/Navigation/LwsNavigationTypes.cs";
        private const string RoutePlannerPath = "Assets/LWS/InterstateHauler/Navigation/LwsRoutePlanner.cs";
        private const string CabGpsControllerPath = "Assets/LWS/InterstateHauler/Navigation/LwsCabGpsController.cs";
        private const string GpsMapGraphicPath = "Assets/LWS/InterstateHauler/Navigation/LwsGpsMapGraphic.cs";
        private const string GpsVoiceGuidancePath = "Assets/LWS/InterstateHauler/Navigation/LwsGpsVoiceGuidance.cs";
        private const string GpsDebugPanelPath = "Assets/LWS/InterstateHauler/Navigation/LwsNavigationDebugPanel.cs";
        private const string GpsSettingsPanelPath = "Assets/LWS/InterstateHauler/Navigation/LwsGpsSettingsPanel.cs";
        private const string PlayerSettingsPath = "Assets/LWS/InterstateHauler/Core/LwsPlayerSettings.cs";
        private const string DefaultGpsVoicePackPath = "Assets/LWS/InterstateHauler/Navigation/Data/IH_GpsVoicePack_Default.asset";
        private const string Prompt011DocsPath = "Documentation/InterstateHauler/011_GPS_Routing_and_Voice_Guidance.md";
        private const string CompassApiMatrixPath = "Documentation/InterstateHauler/011_Compass_API_Matrix.md";
        private const string RouteManeuverMatrixPath = "Documentation/InterstateHauler/011_Route_Maneuver_Matrix.md";
        private const string GpsVoiceMatrixPath = "Documentation/InterstateHauler/011_GPS_Voice_Matrix.md";
        private const string GpsTestMatrixPath = "Documentation/InterstateHauler/011_GPS_Test_Matrix.md";
        private const string Prompt012HandoffPath = "Documentation/InterstateHauler/011_Prompt012_Handoff.md";
        private const string WeatherCorePath = "Assets/LWS/InterstateHauler/Weather/LwsWeather.cs";
        private const string WeatherPresetDefinitionPath = "Assets/LWS/InterstateHauler/Weather/LwsWeatherPresetDefinition.cs";
        private const string WeatherMakerAdapterPath = "Assets/LWS/InterstateHauler/Weather/LwsWeatherMakerAdapter.cs";
        private const string WeatherDebugPanelPath = "Assets/LWS/InterstateHauler/Weather/LwsWeatherDebugPanel.cs";
        private const string WeatherMakerReadmePath = "Assets/WeatherMaker/Readme.txt";
        private const string WeatherMakerManualPath = "Assets/WeatherMaker/UserManual.md";
        private const string WeatherMakerPrefabPath = "Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab";
        private const string WeatherMakerScriptPath = "Assets/WeatherMaker/Prefab/Scripts/Manager/WeatherMakerScript.cs";
        private const string WeatherMakerDayNightPath = "Assets/WeatherMaker/Prefab/Scripts/Sky/WeatherMakerDayNightCycleManagerScript.cs";
        private const string WeatherDocsPath = "Documentation/InterstateHauler/012_Weather_Maker_Integration.md";
        private const string WeatherMakerApiMatrixPath = "Documentation/InterstateHauler/012_WeatherMaker_API_Matrix.md";
        private const string WeatherPresetMatrixPath = "Documentation/InterstateHauler/012_Weather_Preset_Matrix.md";
        private const string WeatherTestMatrixPath = "Documentation/InterstateHauler/012_Weather_Test_Matrix.md";
        private const string Prompt013HandoffPath = "Documentation/InterstateHauler/012_Prompt013_Handoff.md";
        private const string WeatheradeRootPath = "Assets/NOT_Lonely/Weatherade SRS";
        private const string WeatheradeVersionPath = "Assets/NOT_Lonely/Weatherade SRS/Scripts/StartScreen/CurrentVersion.txt";
        private const string WeatheradeReadmePath = "Assets/NOT_Lonely/Weatherade SRS/Readme.txt";
        private const string WeatheradeUrpPackagePath = "Assets/NOT_Lonely/Weatherade SRS/URP Support/WeatheradeSRS_URP_17_1.unitypackage";
        private const string WeatheradeRainCoveragePath = "Assets/NOT_Lonely/Weatherade SRS/Scripts/RainCoverage.cs";
        private const string WeatheradeSnowCoveragePath = "Assets/NOT_Lonely/Weatherade SRS/Scripts/SnowCoverage.cs";
        private const string WeatheradeCoverageBasePath = "Assets/NOT_Lonely/Weatherade SRS/Scripts/CoverageBase.cs";
        private const string RoadConditionCorePath = "Assets/LWS/InterstateHauler/Roads/Conditions/LwsRoadCondition.cs";
        private const string RoadConditionProfilePath = "Assets/LWS/InterstateHauler/Roads/Conditions/LwsRoadConditionPhysicsProfile.cs";
        private const string RoadConditionRuntimeControllerPath = "Assets/LWS/InterstateHauler/Roads/Conditions/LwsRoadConditionRuntimeController.cs";
        private const string WeatheradeAdapterPath = "Assets/LWS/InterstateHauler/Roads/Conditions/LwsWeatheradeAdapter.cs";
        private const string NwhRoadConditionAdapterPath = "Assets/LWS/InterstateHauler/Roads/Conditions/LwsNwhRoadConditionAdapter.cs";
        private const string RoadConditionDebugPanelPath = "Assets/LWS/InterstateHauler/Roads/Conditions/LwsRoadConditionDebugPanel.cs";
        private const string DefaultRoadConditionProfilePath = "Assets/LWS/InterstateHauler/Roads/Conditions/Data/IH_RoadConditionPhysics_Default.asset";
        private const string RoadConditionDocsPath = "Documentation/InterstateHauler/013_Weatherade_NWH_Road_Conditions.md";
        private const string WeatheradeApiMatrixPath = "Documentation/InterstateHauler/013_Weatherade_API_Matrix.md";
        private const string NwhSurfaceApiMatrixPath = "Documentation/InterstateHauler/013_NWH_Surface_API_Matrix.md";
        private const string RoadConditionMatrixPath = "Documentation/InterstateHauler/013_Road_Condition_Matrix.md";
        private const string RoadConditionTestMatrixPath = "Documentation/InterstateHauler/013_Road_Condition_Test_Matrix.md";
        private const string Prompt014HandoffPath = "Documentation/InterstateHauler/013_Prompt014_Handoff.md";
        private const string SelectedNwhTruckPath = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab";
        private const string SelectedNwhTrailerPath = "Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTrailer Variant.prefab";
        private const string LogitechG29ProfilePath = "Assets/LWS/InterstateHauler/Input/Data/IH_LogitechG29Profile.asset";
        private const string DefaultG29CalibrationPath = "Assets/LWS/InterstateHauler/Input/Data/IH_DefaultG29Calibration.asset";
        private const string UnityDirectInputPackageName = "com.directinput.unity";
        private const string UnityDirectInputPackageUrl = "https://github.com/imDanoush/Unity-DirectInput.git";
        private const string UnityDirectInputNwhSamplePackageName = "NWHVehiclePhysics2FFB.unitypackage";
        private const string CancelledNwhSteeringWheelInputImportedProviderPath = "Assets/NWH/Vehicle Physics 2/_OptionalPackages/Input/SteeringWheelInput/SteeringWheelInputProvider.cs";

        private static readonly string[] InterstateTrafficPrefabPaths =
        {
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_3.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Car_5.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Jeep.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Taxi.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_2.prefab",
            "Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/City_bus.prefab"
        };

        private static readonly string[] WeatherPresetPaths =
        {
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Clear.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_PartlyCloudy.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Cloudy.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Overcast.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_LightRain.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_HeavyRain.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Thunderstorm.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_LightSnow.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_HeavySnow.asset",
            "Assets/LWS/InterstateHauler/Weather/Data/IH_Weather_Fog.asset"
        };

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
            Validate18SpeedTransmissionFoundation(report);
            ValidateTruckControlFoundation(report);
            ValidateDashboardMirrorCabFoundation(report);
            ValidateEasyRoadsInterstateCorridorFoundation(report);
            ValidateUtsHighwayTrafficFoundation(report);
            ValidateGpsRoutingVoiceFoundation(report);
            ValidateWeatherMakerAtmosphereFoundation(report);
            ValidateWeatheradeRoadConditionFoundation(report);
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
            bool controlsValid = definition.ControlCapabilities.Validate(out string controlsMessage);
            report.Add(
                controlsValid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Control Capabilities",
                controlsMessage);

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

        private static void Validate18SpeedTransmissionFoundation(LwsProjectValidationReport report)
        {
            Validate18SpeedDefinition(report);
            ValidateTruckValidationTransmissionSetup(report);
            ValidateTransmissionRuntimeBoundary(report);
            ValidatePrompt005ValidationMappingDisabled(report);
        }

        private static void Validate18SpeedDefinition(LwsProjectValidationReport report)
        {
            Lws18SpeedTransmissionDefinition definition = AssetDatabase.LoadAssetAtPath<Lws18SpeedTransmissionDefinition>(TransmissionDefinitionPath);
            if (definition == null)
            {
                report.Add(LwsValidationSeverity.Error, "18-Speed Definition", $"{TransmissionDefinitionPath} was not found or did not import.");
                return;
            }

            bool valid = definition.ValidateDefinition(out string message);
            report.Add(valid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error, "18-Speed Definition", message);

            bool allRatiosPositive = definition.ForwardRatios.Count == 18 && definition.ForwardRatios.All(ratio => ratio > 0f);
            report.Add(
                allRatiosPositive ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "18-Speed Ratios",
                allRatiosPositive
                    ? "18 unique positive forward ratios are available for the runtime NWH gear list."
                    : "The 18-speed definition does not expose 18 positive forward ratios.");

            bool reverseValid = definition.ResolveReverse().nwhGearIndex == -1 && definition.ResolveReverse().reverse;
            report.Add(
                reverseValid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "18-Speed Reverse",
                reverseValid
                    ? "Reverse resolves to a semantic LWS reverse state and NWH reverse gear index -1."
                    : "Reverse mapping is not valid.");
        }

        private static void ValidateTruckValidationTransmissionSetup(LwsProjectValidationReport report)
        {
            if (!File.Exists(TruckValidationScenePath))
            {
                return;
            }

            string sceneText = File.ReadAllText(TruckValidationScenePath);
            bool sceneReferencesDefinition = sceneText.Contains(TransmissionDefinitionGuid);
            bool spawnerAddsController = File.Exists("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs") &&
                                         File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs").Contains("Lws18SpeedTransmissionController");
            bool spawnerAddsAdapter = File.Exists("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs") &&
                                      File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs").Contains("LwsNwh18SpeedTransmissionAdapter");

            report.Add(
                sceneReferencesDefinition && spawnerAddsController && spawnerAddsAdapter ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "TruckValidation 18-Speed Setup",
                sceneReferencesDefinition && spawnerAddsController && spawnerAddsAdapter
                    ? "TruckValidation references the 18-speed definition and the LWS spawner installs the controller/NWH adapter at runtime."
                    : "TruckValidation or the LWS spawner is missing the 18-speed controller/adapter/definition wiring.");
        }

        private static void ValidateTransmissionRuntimeBoundary(LwsProjectValidationReport report)
        {
            string controllerText = File.Exists(TransmissionControllerPath) ? File.ReadAllText(TransmissionControllerPath) : string.Empty;
            string adapterText = File.Exists(TransmissionNwhAdapterPath) ? File.ReadAllText(TransmissionNwhAdapterPath) : string.Empty;

            bool controllerConsumesLwsInput = controllerText.Contains("ILwsVehicleInputService") &&
                                             controllerText.Contains("ILwsVehicleInputSource") &&
                                             controllerText.Contains("ReadGearIntent()");
            bool controllerAvoidsPrompt005Hardware = !controllerText.Contains("DirectInput") &&
                                                     !controllerText.Contains("DIManager") &&
                                                     !controllerText.Contains("Logitech") &&
                                                     !controllerText.Contains("HID");
            bool adapterUsesNwhShiftInto = adapterText.Contains("transmission.ShiftInto") &&
                                           adapterText.Contains("transmission.gears") &&
                                           adapterText.Contains("TransmissionShiftType.Manual");
            bool saveParticipant = controllerText.Contains("ILwsSaveParticipant") &&
                                   controllerText.Contains("vehicle.transmission.player") &&
                                   controllerText.Contains("requiresShifterSynchronization");

            report.Add(
                controllerConsumesLwsInput && controllerAvoidsPrompt005Hardware ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "18-Speed Input Boundary",
                controllerConsumesLwsInput && controllerAvoidsPrompt005Hardware
                    ? "The 18-speed controller consumes LWS gear intent only and does not reference DirectInput, DIManager, Logitech, or HID classes."
                    : "The 18-speed controller input boundary is invalid.");

            report.Add(
                adapterUsesNwhShiftInto ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "18-Speed NWH Boundary",
                adapterUsesNwhShiftInto
                    ? "The NWH adapter configures the runtime gear list and shifts through TransmissionComponent.ShiftInto."
                    : "The NWH adapter does not expose the required runtime gear/ShiftInto boundary.");

            report.Add(
                saveParticipant ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "18-Speed Save Shape",
                saveParticipant
                    ? "The 18-speed controller owns a compact save participant with shifter synchronization on restore."
                    : "The 18-speed save participant shape is incomplete.");
        }

        private static void ValidatePrompt005ValidationMappingDisabled(LwsProjectValidationReport report)
        {
            LwsWheelCalibrationAsset calibration = AssetDatabase.LoadAssetAtPath<LwsWheelCalibrationAsset>(DefaultG29CalibrationPath);
            bool defaultMappingDisabled = calibration != null &&
                                          calibration.Profile != null &&
                                          !calibration.Profile.validationGearMappingEnabled;
            string providerText = File.Exists(NwhVehicleInputProviderPath) ? File.ReadAllText(NwhVehicleInputProviderPath) : string.Empty;
            bool providerSuppressesDirectShift = providerText.Contains("validationGearMappingEnabled") &&
                                                 providerText.Contains("return -999");

            report.Add(
                defaultMappingDisabled && providerSuppressesDirectShift ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 005 Validation Mapping",
                defaultMappingDisabled && providerSuppressesDirectShift
                    ? "The temporary Prompt 005 gate-to-NWH mapping is disabled by default so Truck18Speed owns shifting."
                    : "The temporary Prompt 005 validation mapping may still be able to double-drive NWH gears.");
        }

        private static void ValidateTruckControlFoundation(LwsProjectValidationReport report)
        {
            ValidateTruckControlFiles(report);
            ValidateTruckControlSpawnerSetup(report);
            ValidateTruckControlRuntimeBoundary(report);
            ValidateTruckControlInputBindings(report);
            ValidateTruckControlDocumentation(report);
        }

        private static void ValidateTruckControlFiles(LwsProjectValidationReport report)
        {
            string[] requiredFiles =
            {
                TruckControlControllerPath,
                TruckControlServicePath,
                TruckControlTypesPath,
                NwhTruckControlAdapterPath,
                PlayerGestureControllerPath,
                KeyboardGamepadTruckInputPath
            };

            var missing = requiredFiles.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Control Runtime Files",
                missing.Count == 0
                    ? "Prompt 007 truck-control controller, service, NWH adapter, keyboard/controller source, and gesture controller exist."
                    : "Missing Prompt 007 files: " + string.Join(", ", missing));
        }

        private static void ValidateTruckControlSpawnerSetup(LwsProjectValidationReport report)
        {
            string spawnerText = File.Exists("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs")
                : string.Empty;
            string bootstrapText = File.Exists("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                : string.Empty;
            string playerTruckText = File.Exists("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruck.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruck.cs")
                : string.Empty;

            bool spawnerInstallsControls = spawnerText.Contains("LwsTruckControlController") &&
                                           spawnerText.Contains("LwsNwhTruckControlAdapter") &&
                                           spawnerText.Contains("LwsPlayerGestureController") &&
                                           spawnerText.Contains("LwsTruckControlDebugPanel");
            bool serviceRegistered = bootstrapText.Contains("ILwsTruckControlService") &&
                                     bootstrapText.Contains("LwsTruckControlService");
            bool playerTruckExposesControls = playerTruckText.Contains("TruckControlController");

            report.Add(
                spawnerInstallsControls && serviceRegistered && playerTruckExposesControls ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Control Runtime Wiring",
                spawnerInstallsControls && serviceRegistered && playerTruckExposesControls
                    ? "The validation spawner installs truck controls, the bootstrap registers the truck-control service, and LwsPlayerTruck exposes the controller."
                    : "Truck-control runtime wiring is incomplete.");
        }

        private static void ValidateTruckControlRuntimeBoundary(LwsProjectValidationReport report)
        {
            string controlsText = Directory.Exists("Assets/LWS/InterstateHauler/Vehicles/Controls")
                ? string.Join("\n", Directory.GetFiles("Assets/LWS/InterstateHauler/Vehicles/Controls", "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText))
                : string.Empty;
            string inputProviderText = File.Exists(NwhVehicleInputProviderPath) ? File.ReadAllText(NwhVehicleInputProviderPath) : string.Empty;

            bool avoidsHardwareApis = !controlsText.Contains("DirectInput") &&
                                      !controlsText.Contains("DIManager") &&
                                      !controlsText.Contains("Logitech") &&
                                      !controlsText.Contains("HID");
            bool avoidsTransmissionBypass = !controlsText.Contains("ShiftInto(") &&
                                            !controlsText.Contains("powertrain.transmission");
            bool nwhProviderConsumesControlState = inputProviderText.Contains("SetTruckControlController") &&
                                                   inputProviderText.Contains("ConsumeNativePulse") &&
                                                   inputProviderText.Contains("cruiseThrottleOutput") &&
                                                   inputProviderText.Contains("parkingBrakeOn");

            report.Add(
                avoidsHardwareApis ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Control Hardware Boundary",
                avoidsHardwareApis
                    ? "Truck-control gameplay scripts avoid DirectInput, DIManager, Logitech, and HID APIs."
                    : "Truck-control gameplay scripts reference hardware-specific APIs.");
            report.Add(
                avoidsTransmissionBypass ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Control Transmission Boundary",
                avoidsTransmissionBypass
                    ? "Prompt 007 controls do not call NWH ShiftInto or bypass the 18-speed controller."
                    : "Prompt 007 controls appear to bypass the Prompt 006 transmission authority.");
            report.Add(
                nwhProviderConsumesControlState ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "NWH Control Bridge",
                nwhProviderConsumesControlState
                    ? "The LWS NWH input provider consumes truck-control state and one-shot native pulses."
                    : "The LWS NWH input provider does not expose the required truck-control bridge.");
        }

        private static void ValidateTruckControlInputBindings(LwsProjectValidationReport report)
        {
            string commandText = File.Exists("Assets/LWS/InterstateHauler/Input/LwsVehicleInput.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Input/LwsVehicleInput.cs")
                : string.Empty;
            string keyboardText = File.Exists(KeyboardGamepadTruckInputPath) ? File.ReadAllText(KeyboardGamepadTruckInputPath) : string.Empty;
            string wheelTypesText = File.Exists("Assets/LWS/InterstateHauler/Input/Wheels/LwsWheelInputTypes.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Input/Wheels/LwsWheelInputTypes.cs")
                : string.Empty;
            string runtimeAsmdefText = File.Exists("Assets/LWS/InterstateHauler/LWS.InterstateHauler.Runtime.asmdef")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/LWS.InterstateHauler.Runtime.asmdef")
                : string.Empty;

            string[] requiredCommands =
            {
                "ignitionToggle",
                "engineStart",
                "engineStop",
                "parkingBrakeToggle",
                "airHorn",
                "trailerBrake",
                "lookReset",
                "flipOffDriver"
            };
            bool commandFrameComplete = requiredCommands.All(commandText.Contains);
            bool keyboardHasDefaults = keyboardText.Contains("keyboard.fKey") &&
                                       keyboardText.Contains("kb.parkingBrake") &&
                                       keyboardText.Contains("kb.airHorn");
            bool wheelHasTruckBindings = wheelTypesText.Contains("FlipOffDriver") &&
                                         wheelTypesText.Contains("AirHorn") &&
                                         wheelTypesText.Contains("DifferentialLock");
            bool runtimeReferencesInputSystem = runtimeAsmdefText.Contains("75469ad4d38634e559750d17036d5f7c");

            report.Add(
                commandFrameComplete && keyboardHasDefaults && wheelHasTruckBindings && runtimeReferencesInputSystem ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Semantic Truck Input Bindings",
                commandFrameComplete && keyboardHasDefaults && wheelHasTruckBindings && runtimeReferencesInputSystem
                    ? "Semantic truck commands include keyboard/controller defaults, wheel logical bindings, and the LWS runtime asmdef references Unity Input System."
                    : "Semantic truck command bindings are incomplete.");
        }

        private static void ValidateTruckControlDocumentation(LwsProjectValidationReport report)
        {
            string[] docs =
            {
                TruckControlDocsPath,
                TruckControlMatrixPath,
                NwhControlApiMatrixPath,
                Prompt008HandoffPath
            };
            var missingDocs = docs.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missingDocs.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 007 Documentation",
                missingDocs.Count == 0
                    ? "Prompt 007 documentation and Prompt 008 handoff exist."
                    : "Missing Prompt 007 documentation: " + string.Join(", ", missingDocs));
        }

        private static void ValidateDashboardMirrorCabFoundation(LwsProjectValidationReport report)
        {
            ValidateDashboardRuntimeFiles(report);
            ValidateDashboardDefinition(report);
            ValidateDashboardRuntimeBoundary(report);
            ValidateDashboardSpawnerSetup(report);
            ValidateSourceCabInventory(report);
            ValidateMirrorFoundation(report);
            ValidateCabAccessoryAnchors(report);
            ValidatePrompt008Documentation(report);
        }

        private static void ValidateDashboardRuntimeFiles(LwsProjectValidationReport report)
        {
            string[] requiredFiles =
            {
                DashboardDefinitionPath,
                DashboardControllerPath,
                DashboardTypesPath,
                MirrorControllerPath,
                CabAnchorRegistryPath,
                CabAnchorPath
            };

            var missing = requiredFiles.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Dashboard/Mirror/Cab Runtime Files",
                missing.Count == 0
                    ? "Prompt 008 dashboard, mirror, and cab accessory runtime files exist."
                    : "Missing Prompt 008 files: " + string.Join(", ", missing));
        }

        private static void ValidateDashboardDefinition(LwsProjectValidationReport report)
        {
            LwsTruckDashboardDefinition definition = AssetDatabase.LoadAssetAtPath<LwsTruckDashboardDefinition>(DashboardDefinitionPath);
            if (definition == null)
            {
                report.Add(LwsValidationSeverity.Error, "Dashboard Definition", $"{DashboardDefinitionPath} was not found or did not import.");
                return;
            }

            bool valid = definition.Validate(out string message);
            report.Add(valid ? LwsValidationSeverity.Info : LwsValidationSeverity.Error, "Dashboard Definition", message);

            LwsTruckDefinition truckDefinition = AssetDatabase.LoadAssetAtPath<LwsTruckDefinition>(TruckDefinitionPath);
            bool assigned = truckDefinition != null && truckDefinition.DashboardDefinition == definition;
            report.Add(
                assigned ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Truck Definition Dashboard Reference",
                assigned
                    ? "Starter NWH semi truck definition references the Prompt 008 dashboard definition."
                    : "Starter NWH semi truck definition does not reference the Prompt 008 dashboard definition.");
        }

        private static void ValidateDashboardRuntimeBoundary(LwsProjectValidationReport report)
        {
            string dashboardText = File.Exists(DashboardControllerPath) ? File.ReadAllText(DashboardControllerPath) : string.Empty;
            string dashboardTypesText = File.Exists(DashboardTypesPath) ? File.ReadAllText(DashboardTypesPath) : string.Empty;
            string mirrorText = File.Exists(MirrorControllerPath) ? File.ReadAllText(MirrorControllerPath) : string.Empty;
            string cabText = Directory.Exists("Assets/LWS/InterstateHauler/Vehicles/Cab")
                ? string.Join("\n", Directory.GetFiles("Assets/LWS/InterstateHauler/Vehicles/Cab", "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText))
                : string.Empty;
            string combinedText = dashboardText + "\n" + dashboardTypesText + "\n" + mirrorText + "\n" + cabText;

            bool avoidsHardwareApis = !combinedText.Contains("DirectInput") &&
                                      !combinedText.Contains("DIManager") &&
                                      !combinedText.Contains("Logitech") &&
                                      !combinedText.Contains("HID") &&
                                      !combinedText.Contains("UnityEngine.InputSystem");
            bool consumesSemanticState = dashboardText.Contains("LwsTruckControlState") &&
                                         dashboardText.Contains("LwsTransmissionDisplayState") &&
                                         dashboardText.Contains("LwsNwhVehicleAdapter");
            bool doesNotReadNwhInputProvider = !dashboardText.Contains("VehicleInputProvider") &&
                                               !dashboardText.Contains("NwhVehicleInputProvider");

            report.Add(
                avoidsHardwareApis && consumesSemanticState && doesNotReadNwhInputProvider ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Dashboard State Boundary",
                avoidsHardwareApis && consumesSemanticState && doesNotReadNwhInputProvider
                    ? "Dashboard/mirror/cab scripts avoid hardware APIs and bind to LWS semantic state plus telemetry."
                    : "Dashboard/mirror/cab scripts appear to cross the Prompt 008 state boundary.");
        }

        private static void ValidateDashboardSpawnerSetup(LwsProjectValidationReport report)
        {
            string spawnerText = File.Exists("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs")
                : string.Empty;
            string bootstrapText = File.Exists("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                : string.Empty;
            string playerTruckText = File.Exists("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruck.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruck.cs")
                : string.Empty;

            bool spawnerInstalls = spawnerText.Contains("LwsTruckDashboardController") &&
                                   spawnerText.Contains("LwsTruckMirrorController") &&
                                   spawnerText.Contains("LwsCabAccessoryAnchorRegistry") &&
                                   spawnerText.Contains("LwsTruckDashboardDebugPanel");
            bool serviceRegistered = bootstrapText.Contains("ILwsTruckDashboardService") &&
                                     bootstrapText.Contains("LwsTruckDashboardService");
            bool playerTruckExposes = playerTruckText.Contains("DashboardController") &&
                                      playerTruckText.Contains("MirrorController") &&
                                      playerTruckText.Contains("CabAccessoryAnchorRegistry");

            report.Add(
                spawnerInstalls && serviceRegistered && playerTruckExposes ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Dashboard Runtime Wiring",
                spawnerInstalls && serviceRegistered && playerTruckExposes
                    ? "The validation spawner installs dashboard/mirror/cab components, bootstrap registers the dashboard service, and LwsPlayerTruck exposes the cab systems."
                    : "Dashboard/mirror/cab runtime wiring is incomplete.");
        }

        private static void ValidateSourceCabInventory(LwsProjectValidationReport report)
        {
            string truckText = File.Exists(SelectedNwhTruckPath) ? File.ReadAllText(SelectedNwhTruckPath) : string.Empty;
            string[] requiredNames =
            {
                "m_Name: SpeedGaugeAnalog",
                "m_Name: RPMGaugeAnalog",
                "m_Name: GearGaugeDigital",
                "m_Name: Left Blinker",
                "m_Name: Right Blinker",
                "m_Name: High Beam",
                "m_Name: steering wheel",
                "m_Name: RenderTextureMirrorCameraL",
                "m_Name: RenderTextureMirrorCameraR"
            };
            var missing = requiredNames.Where(name => !truckText.Contains(name)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "NWH Cab Inventory",
                missing.Count == 0
                    ? "Selected NWH semi exposes the expected dashboard gauges, indicators, steering wheel, and mirror cameras."
                    : "Selected NWH semi is missing expected cab objects: " + string.Join(", ", missing));
        }

        private static void ValidateMirrorFoundation(LwsProjectValidationReport report)
        {
            LwsTruckDashboardDefinition definition = AssetDatabase.LoadAssetAtPath<LwsTruckDashboardDefinition>(DashboardDefinitionPath);
            bool presetsValid = definition != null && definition.Validate(out _);
            string mirrorText = File.Exists(MirrorControllerPath) ? File.ReadAllText(MirrorControllerPath) : string.Empty;
            bool renderingConnected = mirrorText.Contains("ILwsRenderingService") &&
                                      mirrorText.Contains("mirrorResolutionPixels") &&
                                      mirrorText.Contains("mirrorUpdateIntervalFrames");
            bool offSupported = mirrorText.Contains("LwsMirrorQuality.Off") && mirrorText.Contains("gameObject.SetActive(enabled)");
            bool runtimeTexture = mirrorText.Contains("new RenderTexture") && mirrorText.Contains("HideFlags.DontSave");

            report.Add(
                presetsValid && renderingConnected && offSupported && runtimeTexture ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Mirror Quality Foundation",
                presetsValid && renderingConnected && offSupported && runtimeTexture
                    ? "Mirror quality presets connect to Prompt 003 rendering settings, support OFF, and use runtime RenderTextures."
                    : "Mirror quality foundation is incomplete.");
        }

        private static void ValidateCabAccessoryAnchors(LwsProjectValidationReport report)
        {
            string registryText = File.Exists(CabAnchorRegistryPath) ? File.ReadAllText(CabAnchorRegistryPath) : string.Empty;
            string anchorText = File.Exists(CabAnchorPath) ? File.ReadAllText(CabAnchorPath) : string.Empty;
            string[] requiredIds =
            {
                "IH_CabAnchor_Dashboard01",
                "IH_CabAnchor_Dashboard02",
                "IH_CabAnchor_Hanging01",
                "IH_CabAnchor_PassengerSeat",
                "IH_CabAnchor_Sleeper",
                "IH_CabAnchor_Memento01"
            };
            var missingIds = requiredIds.Where(id => !registryText.Contains(id)).ToList();
            bool physicsSafe = anchorText.Contains("GetComponent<Rigidbody>()") &&
                               anchorText.Contains("Collider[]") &&
                               anchorText.Contains("colliders[i].enabled = false");
            bool hulaPlaceholder = registryText.Contains("IH_DevHulaGirl_Placeholder") &&
                                   registryText.Contains("LwsCabAccessoryBobble");

            report.Add(
                missingIds.Count == 0 && physicsSafe && hulaPlaceholder ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Cab Life Anchors",
                missingIds.Count == 0 && physicsSafe && hulaPlaceholder
                    ? "Required cab accessory anchor IDs exist, accessory attachments are presentation-only, and the hula placeholder hook exists."
                    : "Cab accessory anchor foundation is incomplete.");
        }

        private static void ValidatePrompt008Documentation(LwsProjectValidationReport report)
        {
            string[] docs =
            {
                DashboardDocsPath,
                DashboardBindingMatrixPath,
                MirrorQualityMatrixPath,
                CabAnchorMatrixPath,
                Prompt009HandoffPath
            };
            var missingDocs = docs.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missingDocs.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 008 Documentation",
                missingDocs.Count == 0
                    ? "Prompt 008 documentation and Prompt 009 handoff exist."
                    : "Missing Prompt 008 documentation: " + string.Join(", ", missingDocs));
        }

        private static void ValidateEasyRoadsInterstateCorridorFoundation(LwsProjectValidationReport report)
        {
            ValidateInterstateCorridorRuntimeFiles(report);
            ValidateInterstateCorridorSceneText(report);
            ValidateInterstateCorridorRoadGraphShape(report);
            ValidateEasyRoadsCorridorApiUsage(report);
            ValidatePrompt009Documentation(report);
        }

        private static void ValidateInterstateCorridorRuntimeFiles(LwsProjectValidationReport report)
        {
            string[] requiredFiles =
            {
                InterstateCorridorScenePath,
                EasyRoadsExportBoundaryPath,
                RoadGraphRuntimePath,
                RoadGraphProviderPath,
                RoadSurfacePath,
                RoadDebugPanelPath,
                InterstateCorridorBuilderPath,
                InterstateCorridorMarkerPath
            };

            var missing = requiredFiles.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 009 Road Runtime Files",
                missing.Count == 0
                    ? "Prompt 009 corridor scene, EasyRoads export boundary, road graph service, surface identity, debug panel, marker, and builder files exist."
                    : "Missing Prompt 009 road files: " + string.Join(", ", missing));
        }

        private static void ValidateInterstateCorridorSceneText(LwsProjectValidationReport report)
        {
            if (!File.Exists(InterstateCorridorScenePath))
            {
                report.Add(LwsValidationSeverity.Error, "Interstate Corridor Scene", $"{InterstateCorridorScenePath} is missing.");
                return;
            }

            string sceneText = File.ReadAllText(InterstateCorridorScenePath);
            bool hasCorridorMarker = sceneText.Contains("guid: bc52c81362c14b3b9e07a0fda6aae962");
            bool hasCorridorBuilder = sceneText.Contains("guid: f5b998d4943c4fbda8781c0efa7f0099");
            bool hasTruckSpawner = sceneText.Contains("guid: a7d55a3f708f41dab0fb1dbfa279c0a1");
            bool hasTruckDefinition = sceneText.Contains("guid: cee9c401c1154ff1b626cb6e3ec06e16");
            bool hasTruckPrefab = sceneText.Contains("guid: 4382f9912191416795da77ffba3a3e60");
            bool hasTrailerPrefab = sceneText.Contains("guid: 79fc3e840f60421f95741824a25a41e8");
            bool hasBootstrap = sceneText.Contains("guid: 0bcd3a841dce4fc4bf9a83b48ac2dc21");
            bool hasWheelValidation = sceneText.Contains("LWS G29 Wheel Input") || sceneText.Contains("guid: 6d40428a50d4459eb00a73b0d0091ff4");
            bool hasNoTraffic = !sceneText.Contains("UTS_FullPack") &&
                                !sceneText.Contains("CarMove") &&
                                !sceneText.Contains("AddTrailer") &&
                                !sceneText.Contains("CompassNavigatorPro");

            report.Add(
                hasCorridorMarker && hasCorridorBuilder && hasTruckSpawner && hasTruckDefinition && hasTruckPrefab && hasTrailerPrefab && hasBootstrap
                    ? LwsValidationSeverity.Info
                    : LwsValidationSeverity.Error,
                "Interstate Corridor Scene",
                hasCorridorMarker && hasCorridorBuilder && hasTruckSpawner && hasTruckDefinition && hasTruckPrefab && hasTrailerPrefab && hasBootstrap
                    ? "InterstateCorridorValidation contains the corridor marker/builder, existing LWS truck spawner, starter truck definition, player truck prefab, dry-van trailer, and bootstrap."
                    : "InterstateCorridorValidation is missing corridor, truck, trailer, or bootstrap wiring.");

            report.Add(
                hasWheelValidation ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Interstate Corridor Input Regression Hooks",
                hasWheelValidation
                    ? "The corridor scene retains the Prompt 005 wheel/input validation object from TruckValidation."
                    : "The corridor scene does not visibly retain the Prompt 005 wheel/input validation object.");

            report.Add(
                hasNoTraffic ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 009 Authority Boundary",
                hasNoTraffic
                    ? "No UTS traffic, UTS player-driving, UTS trailer-driving, or Compass GPS authority markers were found in the corridor scene."
                    : "Prompt 009 corridor scene appears to include traffic/GPS authority too early.");
        }

        private static void ValidateInterstateCorridorRoadGraphShape(LwsProjectValidationReport report)
        {
            string builderText = File.Exists(InterstateCorridorBuilderPath) ? File.ReadAllText(InterstateCorridorBuilderPath) : string.Empty;
            string roadGraphText = File.Exists("Assets/LWS/InterstateHauler/Roads/LwsRoadGraph.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Roads/LwsRoadGraph.cs")
                : string.Empty;
            string runtimeText = File.Exists(RoadGraphRuntimePath) ? File.ReadAllText(RoadGraphRuntimePath) : string.Empty;

            string[] stableIds =
            {
                "IH_TEST_I000_NB_MAIN",
                "IH_TEST_I000_SB_MAIN",
                "IH_TEST_I000_NB_ENTRY_RAMP",
                "IH_TEST_I000_TURNAROUND_CROSSOVER"
            };
            bool idsPresent = stableIds.All(id => builderText.Contains(id));
            bool metadataPresent = builderText.Contains("LwsRoadClass.Interstate") &&
                                   builderText.Contains("LwsRoadClass.Ramp") &&
                                   builderText.Contains("LwsRoadDirection.Northbound") &&
                                   builderText.Contains("LwsRoadDirection.Southbound") &&
                                   builderText.Contains("LwsRoadSurfaceType.AsphaltInterstate") &&
                                   builderText.Contains("65f") &&
                                   builderText.Contains("laneWidthMeters") &&
                                   builderText.Contains("laneCenterOffsetsMeters");
            bool graphSupportsPrompt009 = roadGraphText.Contains("LwsRoadDirection") &&
                                          roadGraphText.Contains("LwsRoadSurfaceType") &&
                                          roadGraphText.Contains("laneCenterOffsetsMeters") &&
                                          roadGraphText.Contains("Duplicate road ID");
            bool lookupExists = runtimeText.Contains("ILwsRoadGraphService") &&
                                runtimeText.Contains("TryFindNearestRoad") &&
                                runtimeText.Contains("LwsRoadLookupResult");

            report.Add(
                idsPresent && metadataPresent && graphSupportsPrompt009 && lookupExists ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Interstate Corridor Road Graph",
                idsPresent && metadataPresent && graphSupportsPrompt009 && lookupExists
                    ? "Corridor graph declares stable segment IDs, Interstate/ramp classes, directionality, dry asphalt surface identity, 65 MPH metadata, lane metadata, and nearest-road lookup."
                    : "Prompt 009 road graph metadata or lookup support is incomplete.");
        }

        private static void ValidateEasyRoadsCorridorApiUsage(LwsProjectValidationReport report)
        {
            string easyRoadsRuntime = File.Exists("Assets/EasyRoads3D/Scripts/runtimeScript.cs")
                ? File.ReadAllText("Assets/EasyRoads3D/Scripts/runtimeScript.cs")
                : string.Empty;
            string builderText = File.Exists(InterstateCorridorBuilderPath) ? File.ReadAllText(InterstateCorridorBuilderPath) : string.Empty;
            string exportText = File.Exists(EasyRoadsExportBoundaryPath) ? File.ReadAllText(EasyRoadsExportBoundaryPath) : string.Empty;

            bool easyRoadsPresent = Directory.Exists("Assets/EasyRoads3D") &&
                                    easyRoadsRuntime.Contains("namespace EasyRoads3Dv3") &&
                                    easyRoadsRuntime.Contains("ERRoadNetwork") &&
                                    easyRoadsRuntime.Contains("CreateRoad");
            bool versionKnown = Directory.Exists("Assets/EasyRoads3D") &&
                                (File.Exists("Assets/EasyRoads3D/Release - Install Notes.txt") ||
                                 File.Exists("Assets/EasyRoads3D/readme.txt"));
            bool builderUsesPublicBoundary = builderText.Contains("ERRoadNetwork") &&
                                             builderText.Contains("ERRoadType") &&
                                             builderText.Contains("CreateRoad") &&
                                             builderText.Contains("BuildRoadNetwork") &&
                                             builderText.Contains("SetTerrainDeformation") &&
                                             builderText.Contains("RestoreRoadNetwork") &&
                                             builderText.Contains("BuildRoadNetwork\", false, false, false");
            bool exportBoundaryExtended = exportText.Contains("GetRoads") &&
                                          exportText.Contains("GetMarkerPositions") &&
                                          exportText.Contains("GetSplinePointsCenter") &&
                                          exportText.Contains("LwsRoadGraph") &&
                                          exportText.Contains("LwsRoadSample");
            bool noDirectGameplayDependency = !builderText.Contains("using EasyRoads3Dv3") &&
                                              !exportText.Contains("using EasyRoads3Dv3");

            report.Add(
                easyRoadsPresent ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "EasyRoads Package",
                easyRoadsPresent
                    ? "EasyRoads runtimeScript.cs exposes EasyRoads3Dv3 ERRoadNetwork/CreateRoad APIs for the Prompt 009 boundary."
                    : "EasyRoads package or expected runtime API could not be confirmed.");

            report.Add(
                versionKnown ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "EasyRoads Version Evidence",
                versionKnown
                    ? "EasyRoads documentation/release notes are present; Prompt 009 documentation records v3.2.4f5 from the installed package audit."
                    : "EasyRoads version documentation was not found in the expected package root.");

            report.Add(
                builderUsesPublicBoundary && exportBoundaryExtended && noDirectGameplayDependency ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "EasyRoads Export Boundary",
                builderUsesPublicBoundary && exportBoundaryExtended && noDirectGameplayDependency
                    ? "LWS uses reflected EasyRoads public APIs for validation generation/export and keeps gameplay independent of EasyRoads concrete classes."
                    : "EasyRoads generation/export boundary is incomplete or too tightly coupled.");
        }

        private static void ValidatePrompt009Documentation(LwsProjectValidationReport report)
        {
            string[] docs =
            {
                Prompt009DocsPath,
                RoadGraphMatrixPath,
                EasyRoadsApiMatrixPath,
                CorridorTestMatrixPath,
                Prompt010HandoffPath
            };

            var missingDocs = docs.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missingDocs.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 009 Documentation",
                missingDocs.Count == 0
                    ? "Prompt 009 corridor documentation, road graph matrix, EasyRoads API matrix, test matrix, and Prompt 010 handoff exist."
                    : "Missing Prompt 009 documentation: " + string.Join(", ", missingDocs));
        }

        private static void ValidateUtsHighwayTrafficFoundation(LwsProjectValidationReport report)
        {
            ValidateUtsTrafficRuntimeFiles(report);
            ValidateUtsPackageApi(report);
            ValidateUtsTrafficValidationProfile(report);
            ValidateUtsTrafficRoadGraphIntegration(report);
            ValidateUtsTrafficAuthorityBoundary(report);
            ValidateUtsTrafficPerformanceGuards(report);
            ValidatePrompt010Documentation(report);
        }

        private static void ValidateUtsTrafficRuntimeFiles(LwsProjectValidationReport report)
        {
            string[] requiredFiles =
            {
                TrafficServicePath,
                TrafficLaneTypesPath,
                TrafficLaneBuilderPath,
                TrafficIdentityPath,
                UtsTrafficApiPath,
                UtsTrafficControllerPath,
                UtsTrafficDebugPanelPath,
                UtsTrafficProfileScriptPath,
                InterstateTrafficProfilePath
            };

            var missing = requiredFiles.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 010 UTS Runtime Files",
                missing.Count == 0
                    ? "Prompt 010 traffic service, lane builder, identity, UTS adapter, controller, profile, profile asset, and debug panel files exist."
                    : "Missing Prompt 010 traffic runtime files: " + string.Join(", ", missing));
        }

        private static void ValidateUtsPackageApi(LwsProjectValidationReport report)
        {
            string carMove = File.Exists("Assets/UTS_FullPack/Scripts/Car/CarMove.cs")
                ? File.ReadAllText("Assets/UTS_FullPack/Scripts/Car/CarMove.cs")
                : string.Empty;
            string carAi = File.Exists("Assets/UTS_FullPack/Scripts/Car/CarAIController.cs")
                ? File.ReadAllText("Assets/UTS_FullPack/Scripts/Car/CarAIController.cs")
                : string.Empty;
            string carPath = File.Exists("Assets/UTS_FullPack/Scripts/Paths/CarWalkPath.cs")
                ? File.ReadAllText("Assets/UTS_FullPack/Scripts/Paths/CarWalkPath.cs")
                : string.Empty;
            string movePath = File.Exists("Assets/UTS_FullPack/Scripts/Paths/MovePath.cs")
                ? File.ReadAllText("Assets/UTS_FullPack/Scripts/Paths/MovePath.cs")
                : string.Empty;
            string walkPath = File.Exists("Assets/UTS_FullPack/Scripts/Paths/WalkPath.cs")
                ? File.ReadAllText("Assets/UTS_FullPack/Scripts/Paths/WalkPath.cs")
                : string.Empty;
            string carWheels = File.Exists("Assets/UTS_FullPack/Scripts/Car/CarWheels.cs")
                ? File.ReadAllText("Assets/UTS_FullPack/Scripts/Car/CarWheels.cs")
                : string.Empty;
            string utsAdapter = File.Exists(UtsTrafficApiPath) ? File.ReadAllText(UtsTrafficApiPath) : string.Empty;

            bool packageApiPresent = carMove.Contains("public void Move(") &&
                                     carAi.Contains("public class CarAIController") &&
                                     carPath.Contains("public class CarWalkPath") &&
                                     carPath.Contains("SpawnOnePeople") &&
                                     movePath.Contains("InitStartPosition") &&
                                     walkPath.Contains("public class WalkPath") &&
                                     carWheels.Contains("public class CarWheels");
            bool reflectedBoundary = utsAdapter.Contains("ResolveType(\"CarWalkPath\")") &&
                                     utsAdapter.Contains("ResolveType(\"MovePath\")") &&
                                     utsAdapter.Contains("ResolveType(\"CarAIController\")") &&
                                     utsAdapter.Contains("ResolveType(\"CarMove\")") &&
                                     utsAdapter.Contains("ResolveType(\"CarWheels\")") &&
                                     utsAdapter.Contains("TypeAvailabilityReport") &&
                                     !utsAdapter.Contains("using UTS");

            report.Add(
                packageApiPresent ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "UTS Package API",
                packageApiPresent
                    ? "Installed UTS exposes CarWalkPath, MovePath, CarAIController, and CarMove APIs used by the Prompt 010 boundary."
                    : "Expected UTS traffic scripts or methods were not found.");

            report.Add(
                reflectedBoundary ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "UTS Adapter Boundary",
                reflectedBoundary
                    ? "LWS traffic uses reflected UTS public class/member names so no UTS asmdef/vendor-source edits are required."
                    : "UTS adapter boundary is missing expected reflected type usage.");
        }

        private static void ValidateUtsTrafficValidationProfile(LwsProjectValidationReport report)
        {
            LwsUtsTrafficProfile profile = AssetDatabase.LoadAssetAtPath<LwsUtsTrafficProfile>(InterstateTrafficProfilePath);
            if (profile == null)
            {
                report.Add(LwsValidationSeverity.Error, "UTS Traffic Validation Profile", $"{InterstateTrafficProfilePath} is missing or did not import.");
                return;
            }

            bool profileValid = profile.ValidateProfile(out string profileMessage);
            bool prefabsValid = profile.TryGetTrafficPrefabsCopy(out GameObject[] prefabs, out string prefabMessage);
            bool expectedCount = prefabsValid && prefabs.Length == InterstateTrafficPrefabPaths.Length;
            bool expectedPaths = expectedCount;
            bool supportedByUtsAdapter = expectedCount;
            var supportMessages = new List<string>();
            var utsApi = new LwsUtsTrafficApi();

            if (expectedCount)
            {
                for (int i = 0; i < prefabs.Length; i++)
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefabs[i]);
                    expectedPaths &= string.Equals(assetPath, InterstateTrafficPrefabPaths[i], StringComparison.Ordinal);
                    bool supported = utsApi.TryDescribePrefabSupport(prefabs[i], out string supportMessage);
                    supportedByUtsAdapter &= supported;
                    supportMessages.Add(supportMessage);
                }
            }

            report.Add(
                profileValid && prefabsValid && expectedCount && expectedPaths && supportedByUtsAdapter
                    ? LwsValidationSeverity.Info
                    : LwsValidationSeverity.Error,
                "UTS Traffic Validation Profile",
                profileValid && prefabsValid && expectedCount && expectedPaths && supportedByUtsAdapter
                    ? "Interstate validation traffic profile resolves all 8 selected UTS vehicle prefab references and the UTS adapter supports each prefab."
                    : $"Interstate validation traffic profile is invalid. Profile={profileMessage}; Prefabs={prefabMessage}; Count={prefabs?.Length ?? 0}/{InterstateTrafficPrefabPaths.Length}; ExpectedPaths={expectedPaths}; Supported={supportedByUtsAdapter}; Details={string.Join(" | ", supportMessages)}.");
        }

        private static void ValidateUtsTrafficRoadGraphIntegration(LwsProjectValidationReport report)
        {
            string builderText = File.Exists(TrafficLaneBuilderPath) ? File.ReadAllText(TrafficLaneBuilderPath) : string.Empty;
            string corridorBuilderText = File.Exists(InterstateCorridorBuilderPath) ? File.ReadAllText(InterstateCorridorBuilderPath) : string.Empty;
            string sceneText = File.Exists(InterstateCorridorScenePath) ? File.ReadAllText(InterstateCorridorScenePath) : string.Empty;

            bool laneMetadataUsed = builderText.Contains("LwsRoadGraph") &&
                                    builderText.Contains("laneCenterOffsetsMeters") &&
                                    builderText.Contains("LwsRoadClass.Interstate") &&
                                    builderText.Contains("LwsRoadClass.Ramp") &&
                                    builderText.Contains("MilesPerHourToMetersPerSecond");
            bool corridorWired = corridorBuilderText.Contains("createUtsTrafficValidation") &&
                                 corridorBuilderText.Contains("validationTrafficProfile") &&
                                 corridorBuilderText.Contains("ConfigureValidationProfile") &&
                                 corridorBuilderText.Contains("LwsUtsHighwayTrafficController") &&
                                 corridorBuilderText.Contains("InitializeFromGraph(roadGraphProvider, LastGraph)");
            bool sceneWired = sceneText.Contains("createUtsTrafficValidation: 1") &&
                              sceneText.Contains("validationTrafficProfile: {fileID: 11400000, guid: 9d8357e4f21e4f8baef1ddad600bdb27, type: 2}");
            bool expectedLaneIds = builderText.Contains("_TRAFFIC_L") &&
                                   builderText.Contains("LwsRoadClass.Interstate") &&
                                   builderText.Contains("includeRampTraffic");

            report.Add(
                laneMetadataUsed && corridorWired && sceneWired && expectedLaneIds ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "UTS Road Graph Integration",
                laneMetadataUsed && corridorWired && sceneWired && expectedLaneIds
                    ? "UTS traffic lanes are derived from LWS road graph/lane metadata, the corridor builder initializes the validation traffic controller, and the scene assigns the traffic profile."
                    : "UTS traffic is not fully wired to the LWS road graph/corridor builder.");
        }

        private static void ValidateUtsTrafficAuthorityBoundary(LwsProjectValidationReport report)
        {
            string playerTruck = File.Exists(PlayerTruckPrefabPath) ? File.ReadAllText(PlayerTruckPrefabPath) : string.Empty;
            string playerTrailer = File.Exists(TestTrailerPrefabPath) ? File.ReadAllText(TestTrailerPrefabPath) : string.Empty;
            string trafficIdentity = File.Exists(TrafficIdentityPath) ? File.ReadAllText(TrafficIdentityPath) : string.Empty;
            string controllerText = File.Exists(UtsTrafficControllerPath) ? File.ReadAllText(UtsTrafficControllerPath) : string.Empty;

            bool playerClean = !playerTruck.Contains("CarMove") &&
                               !playerTruck.Contains("CarAIController") &&
                               !playerTruck.Contains("AddTrailer") &&
                               !playerTrailer.Contains("CarMove") &&
                               !playerTrailer.Contains("CarAIController") &&
                               !playerTrailer.Contains("AddTrailer");
            bool trafficIdentityRegistersAiVehicle = trafficIdentity.Contains("LwsVehicleRole.AiVehicle") &&
                                                     trafficIdentity.Contains("LwsVehicleIdentity");
            bool controllerAvoidsPlayerAuthority = controllerText.Contains("PrefabLooksLikeUtsVehicle") &&
                                                   !controllerText.Contains("IH_PlayerTruck_NWH") &&
                                                   !controllerText.Contains("IH_TestTrailer_DryVan");

            report.Add(
                playerClean ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "UTS Player Authority Boundary",
                playerClean
                    ? "Player tractor and LWS test trailer prefabs do not contain obvious UTS player-driving or trailer-driving scripts."
                    : "Player tractor or trailer prefab appears to contain UTS driving/trailer scripts.");

            report.Add(
                trafficIdentityRegistersAiVehicle && controllerAvoidsPlayerAuthority ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "UTS Traffic Identity Boundary",
                trafficIdentityRegistersAiVehicle && controllerAvoidsPlayerAuthority
                    ? "Spawned UTS NPCs are registered as LWS AI vehicles and the controller does not target the NWH player truck/trailer."
                    : "Traffic identity or player-authority isolation is incomplete.");
        }

        private static void ValidateUtsTrafficPerformanceGuards(LwsProjectValidationReport report)
        {
            string controllerText = File.Exists(UtsTrafficControllerPath) ? File.ReadAllText(UtsTrafficControllerPath) : string.Empty;
            string debugText = File.Exists(UtsTrafficDebugPanelPath) ? File.ReadAllText(UtsTrafficDebugPanelPath) : string.Empty;

            bool spawnCapped = controllerText.Contains("maxActiveVehicles") &&
                               controllerText.Contains("spawnIntervalSeconds") &&
                               controllerText.Contains("_nextSpawnTime");
            bool playerLookupThrottled = controllerText.Contains("_nextPlayerResolveTime") &&
                                         controllerText.Contains("+ 2f") &&
                                         controllerText.Contains("FindFirstObjectByType<LwsPlayerTruck>()");
            bool debugCached = debugText.Contains("refreshIntervalSeconds") &&
                               debugText.Contains("_cachedStats") &&
                               !debugText.Contains("FindObjectsByType");
            bool noBroadSearches = !controllerText.Contains("FindObjectsByType") &&
                                   !controllerText.Contains("Resources.FindObjectsOfTypeAll");

            report.Add(
                spawnCapped && playerLookupThrottled && debugCached && noBroadSearches ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "UTS Traffic Performance Guards",
                spawnCapped && playerLookupThrottled && debugCached && noBroadSearches
                    ? "Traffic spawn count/intervals are capped, player lookup is throttled, and the debug panel uses cached values."
                    : "Traffic runtime/debug code may contain unsafe hot-path searches or uncapped spawn behavior.");
        }

        private static void ValidatePrompt010Documentation(LwsProjectValidationReport report)
        {
            string[] docs =
            {
                Prompt010DocsPath,
                UtsApiMatrixPath,
                TrafficLaneMatrixPath,
                TrafficVehicleMatrixPath,
                TrafficPerformanceMatrixPath,
                Prompt011HandoffPath
            };

            var missingDocs = docs.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missingDocs.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 010 Documentation",
                missingDocs.Count == 0
                    ? "Prompt 010 UTS highway integration documentation, matrices, performance matrix, and Prompt 011 handoff exist."
                    : "Missing Prompt 010 documentation: " + string.Join(", ", missingDocs));
        }

        private static void ValidateGpsRoutingVoiceFoundation(LwsProjectValidationReport report)
        {
            ValidateGpsRuntimeFiles(report);
            ValidateGpsRouteAuthority(report);
            ValidateGpsVoicePack(report);
            ValidateGpsSceneWiring(report);
            ValidateGpsSettingsAndPresentation(report);
            ValidatePrompt011Documentation(report);
        }

        private static void ValidateGpsRuntimeFiles(LwsProjectValidationReport report)
        {
            string[] requiredFiles =
            {
                NavigationServicePath,
                NavigationTypesPath,
                RoutePlannerPath,
                CabGpsControllerPath,
                GpsMapGraphicPath,
                GpsVoiceGuidancePath,
                GpsDebugPanelPath,
                GpsSettingsPanelPath,
                PlayerSettingsPath,
                DefaultGpsVoicePackPath
            };

            var missing = requiredFiles.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 011 GPS Runtime Files",
                missing.Count == 0
                    ? "Prompt 011 navigation service, route planner, cab GPS, voice guidance, settings panel, player settings, and default voice-pack asset exist."
                    : "Missing Prompt 011 GPS files: " + string.Join(", ", missing));
        }

        private static void ValidateGpsRouteAuthority(LwsProjectValidationReport report)
        {
            string routePlannerText = File.Exists(RoutePlannerPath) ? File.ReadAllText(RoutePlannerPath) : string.Empty;
            string navigationText = File.Exists(NavigationServicePath) ? File.ReadAllText(NavigationServicePath) : string.Empty;
            string roadGraphText = File.Exists("Assets/LWS/InterstateHauler/Roads/LwsRoadGraph.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Roads/LwsRoadGraph.cs")
                : string.Empty;

            bool plannerUsesGraphAlgorithm = routePlannerText.Contains("TryFindPath") &&
                                             routePlannerText.Contains("EnumerateOutgoingSegments") &&
                                             routePlannerText.Contains("costs") &&
                                             routePlannerText.Contains("previous");
            bool maneuversCentralized = routePlannerText.Contains("ClassifyManeuver") &&
                                        routePlannerText.Contains("LwsNavigationManeuverType") &&
                                        routePlannerText.Contains("LwsRoadClass.Ramp");
            bool routeStateExists = navigationText.Contains("UpdateVehiclePose") &&
                                    navigationText.Contains("HandleOffRoute") &&
                                    navigationText.Contains("RecalculateRoute") &&
                                    navigationText.Contains("LwsNavigationRuntimeState");
            bool compassPresenterOnly = navigationText.Contains("LwsCompassRoutePresenter") &&
                                        navigationText.Contains("SetRoute") &&
                                        !routePlannerText.Contains("Compass") &&
                                        !routePlannerText.Contains("UTS") &&
                                        !routePlannerText.Contains("CarAI") &&
                                        !routePlannerText.Contains("EasyRoads");
            bool routeRequestShape = roadGraphText.Contains("useOriginWorldPosition") &&
                                     roadGraphText.Contains("useDestinationWorldPosition") &&
                                     roadGraphText.Contains("List<LwsRouteStep>");

            report.Add(
                plannerUsesGraphAlgorithm && maneuversCentralized && routeStateExists && compassPresenterOnly && routeRequestShape
                    ? LwsValidationSeverity.Info
                    : LwsValidationSeverity.Error,
                "GPS Route Authority",
                plannerUsesGraphAlgorithm && maneuversCentralized && routeStateExists && compassPresenterOnly && routeRequestShape
                    ? "LWS owns route solving/progress/maneuvers/off-route handling; Compass is present only as an optional route presenter."
                    : "Prompt 011 route authority, maneuver generation, route state, or middleware boundary is incomplete.");
        }

        private static void ValidateGpsVoicePack(LwsProjectValidationReport report)
        {
            LwsGpsVoicePack voicePack = AssetDatabase.LoadAssetAtPath<LwsGpsVoicePack>(DefaultGpsVoicePackPath);
            if (voicePack == null)
            {
                report.Add(LwsValidationSeverity.Error, "GPS Voice Pack", $"{DefaultGpsVoicePackPath} is missing or did not import.");
                return;
            }

            bool valid = voicePack.ValidateSlots(out string message);
            bool distanceSlotsPresent = voicePack.DistanceSlots != null &&
                                        voicePack.DistanceSlots.Count == Enum.GetValues(typeof(LwsGpsDistanceVoicePrompt)).Length;

            report.Add(
                valid && distanceSlotsPresent ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "GPS Voice Pack",
                valid && distanceSlotsPresent
                    ? $"{voicePack.DisplayName} exposes one assignable AudioClip slot for all {LwsNavigationManeuverCatalog.All.Count} maneuver instructions plus distance/context slots. Empty clips are allowed."
                    : $"{message}; DistanceSlots={voicePack.DistanceSlots?.Count ?? 0}.");
        }

        private static void ValidateGpsSceneWiring(LwsProjectValidationReport report)
        {
            string sceneText = File.Exists(InterstateCorridorScenePath) ? File.ReadAllText(InterstateCorridorScenePath) : string.Empty;
            string builderText = File.Exists(InterstateCorridorBuilderPath) ? File.ReadAllText(InterstateCorridorBuilderPath) : string.Empty;
            string truckSpawnerText = File.Exists("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Vehicles/LwsPlayerTruckSpawner.cs")
                : string.Empty;
            string bootstrapText = File.Exists("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                : string.Empty;

            bool sceneEnabled = sceneText.Contains("createNavigationValidation: 1");
            bool builderCreatesPanels = builderText.Contains("createNavigationValidation") &&
                                        builderText.Contains("LwsNavigationDebugPanel") &&
                                        builderText.Contains("LwsGpsSettingsPanel");
            bool truckGpsController = truckSpawnerText.Contains("LwsCabGpsController");
            bool servicesRegistered = bootstrapText.Contains("ILwsPlayerSettingsService") &&
                                      bootstrapText.Contains("ILwsGpsVoiceGuidanceService") &&
                                      bootstrapText.Contains("ILwsNavigationService") &&
                                      bootstrapText.Contains("typeof(ILwsGpsVoiceGuidanceService)");

            report.Add(
                sceneEnabled && builderCreatesPanels && truckGpsController && servicesRegistered
                    ? LwsValidationSeverity.Info
                    : LwsValidationSeverity.Error,
                "GPS Validation Scene Wiring",
                sceneEnabled && builderCreatesPanels && truckGpsController && servicesRegistered
                    ? "InterstateCorridorValidation enables navigation validation, creates GPS/settings debug panels, registers navigation services, and attaches the cab GPS controller to the player truck."
                    : "Prompt 011 GPS validation scene, service registration, or player-truck GPS wiring is incomplete.");
        }

        private static void ValidateGpsSettingsAndPresentation(LwsProjectValidationReport report)
        {
            string settingsText = File.Exists(PlayerSettingsPath) ? File.ReadAllText(PlayerSettingsPath) : string.Empty;
            string voiceText = File.Exists(GpsVoiceGuidancePath) ? File.ReadAllText(GpsVoiceGuidancePath) : string.Empty;
            string cabText = File.Exists(CabGpsControllerPath) ? File.ReadAllText(CabGpsControllerPath) : string.Empty;
            string mapText = File.Exists(GpsMapGraphicPath) ? File.ReadAllText(GpsMapGraphicPath) : string.Empty;

            bool settingDefaultOn = settingsText.Contains("GpsVoiceGuidanceEnabled { get; private set; } = true") &&
                                    settingsText.Contains("PlayerPrefs.GetInt(GpsVoiceGuidanceKey, 1)") &&
                                    settingsText.Contains("SetGpsVoiceGuidanceEnabled");
            bool voiceSuppression = voiceText.Contains("GpsVoiceGuidanceEnabled") &&
                                    voiceText.Contains("Stop()") &&
                                    voiceText.Contains("!_settingsService.GpsVoiceGuidanceEnabled");
            bool physicalDisplay = cabText.Contains("IH Physical Cab GPS Screen") &&
                                   cabText.Contains("RenderMode.WorldSpace") &&
                                   cabText.Contains("LwsGpsMapGraphic") &&
                                   cabText.Contains("AudioSource");
            bool routeGraphic = mapText.Contains("SetRoute") &&
                                mapText.Contains("VertexHelper") &&
                                mapText.Contains("AddLine") &&
                                mapText.Contains("AddPlayerMarker");

            report.Add(
                settingDefaultOn && voiceSuppression && physicalDisplay && routeGraphic
                    ? LwsValidationSeverity.Info
                    : LwsValidationSeverity.Error,
                "GPS Settings and Physical Presentation",
                settingDefaultOn && voiceSuppression && physicalDisplay && routeGraphic
                    ? "GPS Voice Guidance defaults ON, can be toggled through player settings, suppresses clips without clearing the route, and renders on a world-space physical cab GPS display."
                    : "GPS voice setting, suppression behavior, or physical route display is incomplete.");
        }

        private static void ValidatePrompt011Documentation(LwsProjectValidationReport report)
        {
            string[] docs =
            {
                Prompt011DocsPath,
                CompassApiMatrixPath,
                RouteManeuverMatrixPath,
                GpsVoiceMatrixPath,
                GpsTestMatrixPath,
                Prompt012HandoffPath
            };

            var missingDocs = docs.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missingDocs.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 011 Documentation",
                missingDocs.Count == 0
                    ? "Prompt 011 GPS routing, Compass API matrix, maneuver matrix, voice matrix, test matrix, and Prompt 012 handoff exist."
                    : "Missing Prompt 011 documentation: " + string.Join(", ", missingDocs));
        }

        private static void ValidateWeatherMakerAtmosphereFoundation(LwsProjectValidationReport report)
        {
            ValidateWeatherRuntimeFiles(report);
            ValidateWeatherMakerPackage(report);
            ValidateWeatherPresetAssets(report);
            ValidateWeatherServiceAndSceneWiring(report);
            ValidateWeatherAuthorityAndPerformanceBoundary(report);
            ValidatePrompt012Documentation(report);
        }

        private static void ValidateWeatherRuntimeFiles(LwsProjectValidationReport report)
        {
            string[] requiredFiles =
            {
                WeatherCorePath,
                WeatherPresetDefinitionPath,
                WeatherMakerAdapterPath,
                WeatherDebugPanelPath
            };

            var missing = requiredFiles.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 012 Weather Runtime Files",
                missing.Count == 0
                    ? "LWS weather service, preset definition, Weather Maker adapter, and debug panel exist."
                    : "Missing Prompt 012 weather runtime files: " + string.Join(", ", missing));
        }

        private static void ValidateWeatherMakerPackage(LwsProjectValidationReport report)
        {
            string readme = File.Exists(WeatherMakerReadmePath) ? File.ReadAllText(WeatherMakerReadmePath) : string.Empty;
            string manual = File.Exists(WeatherMakerManualPath) ? File.ReadAllText(WeatherMakerManualPath) : string.Empty;
            string weatherMakerScript = File.Exists(WeatherMakerScriptPath) ? File.ReadAllText(WeatherMakerScriptPath) : string.Empty;
            string dayNightScript = File.Exists(WeatherMakerDayNightPath) ? File.ReadAllText(WeatherMakerDayNightPath) : string.Empty;

            bool packagePresent = File.Exists(WeatherMakerPrefabPath) &&
                                  File.Exists(WeatherMakerScriptPath) &&
                                  readme.Contains("Current Version : 8.0.9");
            bool urpCompatible = manual.Contains("Unity 6000 or newer") &&
                                 manual.Contains("URP 17.3 or newer") &&
                                 manual.Contains("Window") &&
                                 manual.Contains("Enable URP");
            bool apiPresent = weatherMakerScript.Contains("RaiseWeatherProfileChanged") &&
                              weatherMakerScript.Contains("LoadResource<T>") &&
                              weatherMakerScript.Contains("PerformanceProfile") &&
                              dayNightScript.Contains("public float TimeOfDay") &&
                              dayNightScript.Contains("public float Speed");

            report.Add(
                packagePresent ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weather Maker Package",
                packagePresent
                    ? "Weather Maker 8.0.9 package root, runtime prefab, and WeatherMakerScript are present."
                    : "Weather Maker 8.0.9 package/runtime prefab was not confirmed.");

            report.Add(
                urpCompatible ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Weather Maker Unity/URP Compatibility",
                urpCompatible
                    ? "Weather Maker documentation confirms Unity 6000+ and URP 17.3+ support."
                    : "Weather Maker URP compatibility was not confirmed from installed docs.");

            report.Add(
                apiPresent ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weather Maker Runtime API",
                apiPresent
                    ? "Weather Maker exposes profile transition, resource loading, performance profile, and day/night time APIs used by LWS."
                    : "Expected Weather Maker runtime APIs were not found.");
        }

        private static void ValidateWeatherPresetAssets(LwsProjectValidationReport report)
        {
            var missing = WeatherPresetPaths.Where(path => !File.Exists(path)).ToList();
            if (missing.Count > 0)
            {
                report.Add(LwsValidationSeverity.Error, "Weather Preset Assets", "Missing weather preset assets: " + string.Join(", ", missing));
                return;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var invalid = new List<string>();
            foreach (string path in WeatherPresetPaths)
            {
                LwsWeatherPresetDefinition definition = AssetDatabase.LoadAssetAtPath<LwsWeatherPresetDefinition>(path);
                if (definition == null)
                {
                    invalid.Add($"{path}: asset did not import as a weather preset definition.");
                    continue;
                }

                if (!definition.ValidateDefinition(out string message))
                {
                    invalid.Add($"{path}: {message}");
                    continue;
                }

                if (!ids.Add(definition.PresetId))
                {
                    invalid.Add($"{path}: duplicate weather preset ID {definition.PresetId}");
                    continue;
                }

                if (!File.Exists(definition.Preset.weatherMakerProfilePath))
                {
                    invalid.Add($"{path}: Weather Maker profile path missing: {definition.Preset.weatherMakerProfilePath}");
                }
            }

            bool requiredMappings = ids.Contains(LwsWeatherPresetCatalog.ClearId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.PartlyCloudyId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.CloudyId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.OvercastId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.LightRainId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.HeavyRainId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.ThunderstormId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.LightSnowId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.HeavySnowId) &&
                                    ids.Contains(LwsWeatherPresetCatalog.FogId);

            report.Add(
                invalid.Count == 0 && requiredMappings ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weather Preset Assets",
                invalid.Count == 0 && requiredMappings
                    ? "All Prompt 012 LWS weather presets import, validate, and reference existing Weather Maker profile assets."
                    : "Weather preset assets are incomplete or invalid: " + string.Join("; ", invalid));
        }

        private static void ValidateWeatherServiceAndSceneWiring(LwsProjectValidationReport report)
        {
            string bootstrapText = File.Exists("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                : string.Empty;
            string builderText = File.Exists(InterstateCorridorBuilderPath) ? File.ReadAllText(InterstateCorridorBuilderPath) : string.Empty;
            string gpsText = File.Exists(CabGpsControllerPath) ? File.ReadAllText(CabGpsControllerPath) : string.Empty;

            bool serviceRegistered = bootstrapText.Contains("ILwsWeatherService") &&
                                     bootstrapText.Contains("LwsWeatherCoordinator");
            bool builderCreatesWeather = builderText.Contains("createWeatherValidation") &&
                                         builderText.Contains("LwsWeatherMakerAdapter") &&
                                         builderText.Contains("LwsWeatherDebugPanel");
            bool gpsUsesWeatherSeam = gpsText.Contains("ILwsWeatherService") &&
                                      gpsText.Contains("CurrentSnapshot.Daylight01") &&
                                      !gpsText.Contains("WeatherMaker");

            report.Add(
                serviceRegistered && builderCreatesWeather && gpsUsesWeatherSeam ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weather Service and Validation Scene Wiring",
                serviceRegistered && builderCreatesWeather && gpsUsesWeatherSeam
                    ? "Weather service is registered, corridor validation creates the adapter/debug panel, and cab GPS consumes only the LWS daylight seam."
                    : "Weather service registration, validation wiring, or GPS day/night seam is incomplete.");
        }

        private static void ValidateWeatherAuthorityAndPerformanceBoundary(LwsProjectValidationReport report)
        {
            string weatherCore = File.Exists(WeatherCorePath) ? File.ReadAllText(WeatherCorePath) : string.Empty;
            string adapter = File.Exists(WeatherMakerAdapterPath) ? File.ReadAllText(WeatherMakerAdapterPath) : string.Empty;
            string debugPanel = File.Exists(WeatherDebugPanelPath) ? File.ReadAllText(WeatherDebugPanelPath) : string.Empty;

            bool noRoadPhysics = !weatherCore.Contains("WheelCollider") &&
                                 !weatherCore.Contains("NWH") &&
                                 !weatherCore.Contains("friction") &&
                                 !weatherCore.Contains("Weatherade") &&
                                 adapter.Contains("Weatherade remains deferred to Prompt 013");
            bool reflectedBoundary = adapter.Contains("ResolveType(WeatherMakerScriptTypeName)") &&
                                     adapter.Contains("RaiseWeatherProfileChanged") &&
                                     adapter.Contains("LoadWeatherMakerResource") &&
                                     !weatherCore.Contains("DigitalRuby.WeatherMaker");
            bool noHotSceneSearches = !adapter.Contains("FindObjectsByType") &&
                                      !adapter.Contains("Resources.FindObjectsOfTypeAll") &&
                                      !debugPanel.Contains("FindObjectsByType") &&
                                      debugPanel.Contains("RefreshCache");

            report.Add(
                noRoadPhysics ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 013 Road Physics Separation",
                noRoadPhysics
                    ? "Prompt 012 weather code exposes semantic precipitation/wind/temperature but does not add road traction, Weatherade accumulation, or NWH tire coupling."
                    : "Weather code appears to cross into road-condition physics or Weatherade/NWH authority.");

            report.Add(
                reflectedBoundary ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weather Maker Adapter Boundary",
                reflectedBoundary
                    ? "Weather Maker concrete classes stay inside the reflection adapter; Prompt 013-facing LWS weather service has no DigitalRuby concrete dependency."
                    : "Weather Maker dependency may be leaking outside the adapter boundary.");

            report.Add(
                noHotSceneSearches ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weather Runtime Performance Guards",
                noHotSceneSearches
                    ? "Weather debug diagnostics use cached state and the adapter avoids broad per-frame scene searches."
                    : "Weather runtime/debug code may contain unsafe hot-path scene searches.");
        }

        private static void ValidatePrompt012Documentation(LwsProjectValidationReport report)
        {
            string[] docs =
            {
                WeatherDocsPath,
                WeatherMakerApiMatrixPath,
                WeatherPresetMatrixPath,
                WeatherTestMatrixPath,
                Prompt013HandoffPath
            };

            var missingDocs = docs.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missingDocs.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 012 Documentation",
                missingDocs.Count == 0
                    ? "Prompt 012 Weather Maker integration docs, API matrix, preset matrix, test matrix, and Prompt 013 handoff exist."
                    : "Missing Prompt 012 documentation: " + string.Join(", ", missingDocs));
        }

        private static void ValidateWeatheradeRoadConditionFoundation(LwsProjectValidationReport report)
        {
            ValidateRoadConditionRuntimeFiles(report);
            ValidateWeatheradePackage(report);
            ValidateRoadConditionServiceAndSceneWiring(report);
            ValidateRoadConditionPhysicsProfile(report);
            ValidateRoadConditionAuthorityAndPerformance(report);
            ValidatePrompt013Documentation(report);
        }

        private static void ValidateRoadConditionRuntimeFiles(LwsProjectValidationReport report)
        {
            string[] requiredFiles =
            {
                RoadConditionCorePath,
                RoadConditionProfilePath,
                RoadConditionRuntimeControllerPath,
                WeatheradeAdapterPath,
                NwhRoadConditionAdapterPath,
                RoadConditionDebugPanelPath,
                DefaultRoadConditionProfilePath
            };

            var missing = requiredFiles.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missing.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 013 Road Condition Runtime Files",
                missing.Count == 0
                    ? "Road condition service, physics profile, runtime controller, Weatherade adapter, NWH adapter, debug panel, and tuning asset exist."
                    : "Missing Prompt 013 road condition files: " + string.Join(", ", missing));
        }

        private static void ValidateWeatheradePackage(LwsProjectValidationReport report)
        {
            string version = File.Exists(WeatheradeVersionPath) ? File.ReadAllText(WeatheradeVersionPath).Trim() : string.Empty;
            string readme = File.Exists(WeatheradeReadmePath) ? File.ReadAllText(WeatheradeReadmePath) : string.Empty;
            string rain = File.Exists(WeatheradeRainCoveragePath) ? File.ReadAllText(WeatheradeRainCoveragePath) : string.Empty;
            string snow = File.Exists(WeatheradeSnowCoveragePath) ? File.ReadAllText(WeatheradeSnowCoveragePath) : string.Empty;
            string coverage = File.Exists(WeatheradeCoverageBasePath) ? File.ReadAllText(WeatheradeCoverageBasePath) : string.Empty;

            bool rootExists = Directory.Exists(WeatheradeRootPath);
            bool expectedVersion = version.Contains("1.1.8");
            bool runtimeApiPresent = rain.Contains("class RainCoverage") &&
                                     rain.Contains("wetnessAmount") &&
                                     rain.Contains("puddlesAmount") &&
                                     snow.Contains("class SnowCoverage") &&
                                     snow.Contains("coverageAmount") &&
                                     coverage.Contains("UpdateCoverageMaterials");
            bool urpPackagePresent = File.Exists(WeatheradeUrpPackagePath);

            report.Add(
                rootExists && expectedVersion ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weatherade Package",
                rootExists && expectedVersion
                    ? "Weatherade Snow and Rain System 1.1.8 is present under Assets/NOT_Lonely/Weatherade SRS."
                    : $"Weatherade package/version was not confirmed. Version text: {version}");

            report.Add(
                runtimeApiPresent ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weatherade Runtime API",
                runtimeApiPresent
                    ? "Weatherade RainCoverage, SnowCoverage, wetness/puddle/snow fields, and UpdateCoverageMaterials APIs are present."
                    : "Expected Weatherade runtime accumulation APIs were not found.");

            report.Add(
                urpPackagePresent && readme.Contains("Start Screen") ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Weatherade URP Support",
                urpPackagePresent
                    ? "Weatherade URP 17.1 support package is present locally; import/setup remains a manual vendor step if visual validation requires it."
                    : "Weatherade URP support package was not found locally.");
        }

        private static void ValidateRoadConditionServiceAndSceneWiring(LwsProjectValidationReport report)
        {
            string bootstrapText = File.Exists("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                ? File.ReadAllText("Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs")
                : string.Empty;
            string builderText = File.Exists(InterstateCorridorBuilderPath) ? File.ReadAllText(InterstateCorridorBuilderPath) : string.Empty;

            bool serviceRegistered = bootstrapText.Contains("ILwsRoadConditionService") &&
                                     bootstrapText.Contains("LwsRoadConditionCoordinator") &&
                                     bootstrapText.Contains("typeof(ILwsWeatherService)") &&
                                     bootstrapText.Contains("typeof(ILwsRoadGraphService)") &&
                                     bootstrapText.Contains("typeof(ILwsNavigationService)");
            bool validationWiring = builderText.Contains("createRoadConditionValidation") &&
                                    builderText.Contains("LwsRoadConditionRuntimeController") &&
                                    builderText.Contains("LwsWeatheradeAdapter") &&
                                    builderText.Contains("LwsNwhRoadConditionAdapter") &&
                                    builderText.Contains("LwsRoadConditionDebugPanel");

            report.Add(
                serviceRegistered && validationWiring ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Road Condition Service and Validation Wiring",
                serviceRegistered && validationWiring
                    ? "Road condition service is registered after weather/navigation/road graph, and the corridor builder creates runtime/adapters/debug tooling."
                    : "Road condition service registration or validation-scene builder wiring is incomplete.");
        }

        private static void ValidateRoadConditionPhysicsProfile(LwsProjectValidationReport report)
        {
            LwsRoadConditionPhysicsProfile profileAsset = AssetDatabase.LoadAssetAtPath<LwsRoadConditionPhysicsProfile>(DefaultRoadConditionProfilePath);
            if (profileAsset == null)
            {
                report.Add(LwsValidationSeverity.Error, "Road Condition Physics Profile", $"{DefaultRoadConditionProfilePath} did not import as a LWS road condition physics profile.");
                return;
            }

            bool valid = profileAsset.ValidateProfile(out string message);
            LwsRoadConditionProfile profile = profileAsset.Profile;
            LwsRoadConditionGripProfile dry = profile.GetGripProfile(LwsRoadConditionType.Dry);
            LwsRoadConditionGripProfile wet = profile.GetGripProfile(LwsRoadConditionType.Wet);
            LwsRoadConditionGripProfile snow = profile.GetGripProfile(LwsRoadConditionType.LightSnow);
            LwsRoadConditionGripProfile ice = profile.GetGripProfile(LwsRoadConditionType.Ice);
            bool hierarchy = wet.longitudinalGripMultiplier01 < dry.longitudinalGripMultiplier01 &&
                             snow.longitudinalGripMultiplier01 < wet.longitudinalGripMultiplier01 &&
                             ice.longitudinalGripMultiplier01 < snow.longitudinalGripMultiplier01 &&
                             wet.brakingGripMultiplier01 < dry.brakingGripMultiplier01 &&
                             snow.brakingGripMultiplier01 < wet.brakingGripMultiplier01 &&
                             ice.brakingGripMultiplier01 < snow.brakingGripMultiplier01;

            report.Add(
                valid && hierarchy ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Road Condition Physics Profile",
                valid && hierarchy
                    ? "Default road condition profile validates and keeps Dry > Wet > Snow > Ice for longitudinal and braking grip."
                    : $"Road condition physics profile is invalid: {message}");
        }

        private static void ValidateRoadConditionAuthorityAndPerformance(LwsProjectValidationReport report)
        {
            string core = File.Exists(RoadConditionCorePath) ? File.ReadAllText(RoadConditionCorePath) : string.Empty;
            string weatherade = File.Exists(WeatheradeAdapterPath) ? File.ReadAllText(WeatheradeAdapterPath) : string.Empty;
            string nwh = File.Exists(NwhRoadConditionAdapterPath) ? File.ReadAllText(NwhRoadConditionAdapterPath) : string.Empty;
            string runtime = File.Exists(RoadConditionRuntimeControllerPath) ? File.ReadAllText(RoadConditionRuntimeControllerPath) : string.Empty;

            bool semanticBoundary = !core.Contains("WeatherMaker") &&
                                    !core.Contains("DigitalRuby") &&
                                    !core.Contains("NOT_Lonely") &&
                                    !core.Contains("NWH.");
            bool weatheradeReflected = weatherade.Contains("NOT_Lonely.Weatherade.RainCoverage") &&
                                       weatherade.Contains("SetMember") &&
                                       weatherade.Contains("UpdateCoverageMaterials") &&
                                       !core.Contains("Weatherade");
            bool nwhOnlyInAdapter = nwh.Contains("NWH.Common.Vehicles") &&
                                    nwh.Contains("LongitudinalFrictionGrip") &&
                                    nwh.Contains("LateralFrictionGrip") &&
                                    nwh.Contains("RollingResistanceTorque") &&
                                    nwh.Contains("RestoreDryBaseline") &&
                                    !weatherade.Contains("NWH.");
            bool throttledRuntime = runtime.Contains("updateIntervalSeconds") &&
                                    nwh.Contains("rebindIntervalSeconds") &&
                                    nwh.Contains("maintenanceApplyIntervalSeconds") &&
                                    weatherade.Contains("EnsureCoverage") &&
                                    !runtime.Contains("FindObjectsByType");

            report.Add(
                semanticBoundary ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Road Condition Semantic Boundary",
                semanticBoundary
                    ? "ILwsRoadConditionService and road-condition snapshots expose only LWS semantic weather/road/physics data."
                    : "Road condition core appears to leak Weather Maker, Weatherade, or NWH concrete dependencies.");

            report.Add(
                weatheradeReflected && nwhOnlyInAdapter ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Weatherade/NWH Adapter Boundaries",
                weatheradeReflected && nwhOnlyInAdapter
                    ? "Weatherade concrete API is isolated in its adapter, and NWH tire API use is isolated in the NWH adapter."
                    : "Weatherade or NWH concrete dependencies may be leaking into the wrong layer.");

            report.Add(
                throttledRuntime ? LwsValidationSeverity.Info : LwsValidationSeverity.Warning,
                "Road Condition Performance Guards",
                throttledRuntime
                    ? "Road condition simulation, NWH rebinding, and NWH maintenance apply are cadence-driven; runtime controller avoids broad scene searches."
                    : "Road condition code may need performance review for scene searches or missing update cadence guards.");
        }

        private static void ValidatePrompt013Documentation(LwsProjectValidationReport report)
        {
            string[] docs =
            {
                RoadConditionDocsPath,
                WeatheradeApiMatrixPath,
                NwhSurfaceApiMatrixPath,
                RoadConditionMatrixPath,
                RoadConditionTestMatrixPath,
                Prompt014HandoffPath
            };

            var missingDocs = docs.Where(path => !File.Exists(path)).ToList();
            report.Add(
                missingDocs.Count == 0 ? LwsValidationSeverity.Info : LwsValidationSeverity.Error,
                "Prompt 013 Documentation",
                missingDocs.Count == 0
                    ? "Prompt 013 Weatherade/NWH road condition docs, API matrices, condition/test matrices, and Prompt 014 handoff exist."
                    : "Missing Prompt 013 documentation: " + string.Join(", ", missingDocs));
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
