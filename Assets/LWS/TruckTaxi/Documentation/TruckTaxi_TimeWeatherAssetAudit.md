# Truck Taxi Time / Weather Asset Audit

## Scope and Evidence

Focused audit of installed assets and existing project adapters for the Taxi time/weather and driver-needs pass. No new weather package, custom sky shader, competing rendering framework, or vendor source edit was required. Weather Maker's installed changelog identifies version 8.0.9. Completed test/build results and confirmed sunset/night/storm visuals are recorded below. Audio and light-rain visibility remain unverified; the main task is correcting the fog transition and will validate it separately.

The selected integration is existing `ILwsGameClockService` -> `ILwsWeatherService` -> `LwsWeatherMakerAdapter` -> Weather Maker presentation. Taxi supplies scheduling and reaction policy only. The source of time is accumulated in-game seconds, not the computer clock.

## Sky and Lighting Assets

| Asset / exact path | Intended use | Pipeline / weather compatibility | Used in this pass |
| --- | --- | --- | --- |
| `Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab` | Complete vendor weather, sun/moon, clouds, precipitation, fog, audio | Installed URP feature and supported vendor shaders required; designed for Weather Maker transitions | Source of a project-owned scene-scoped Taxi prefab copy |
| `Assets/WeatherMaker/Prefab/Material/Sky/WeatherMakerSkySphereMaterial.mat` | Atmospheric sky sphere across day/night | Vendor sky presentation; use through Weather Maker, not an independent global sky writer | Existing vendor prefab sky layer retained |
| `Assets/WeatherMaker/Prefab/Material/Sky/WeatherMakerSkyBoxMaterial.mat` | Vendor skybox/background material | Serialized built-in sky shader; not itself evidence of true multi-skybox blending | Retained as vendor content; Taxi does not swap it independently |
| `Assets/WeatherMaker/Prefab/Material/Sky/WeatherMakerSunMaterial.mat` | Sun presentation | Vendor day/night/light managers | Retained vendor content |
| `Assets/WeatherMaker/Prefab/Material/Sky/WeatherMakerMoonMaterial.mat` | Moon/night presentation | Vendor day/night/light managers | Retained vendor content |
| `Assets/WeatherMaker/Prefab/Profiles/Sky/WeatherMakerSkyProfile_ProceduralMilkyWay.asset` | Procedural day/night sky with night content | Weather Maker sky-profile data, not a separate clock | Available installed profile; existing prefab/profile choices retained rather than forced by Taxi |
| `Assets/WeatherMaker/Prefab/Profiles/Sky/WeatherMakerSkyProfile_ProceduralMilkyWayPhysicallyBased.asset` | Alternative physically based vendor sky | Requires corresponding vendor presentation support | Available, not newly assigned/tuned |
| `Assets/WeatherMaker/Prefab/Profiles/Sky/WeatherMakerSkyProfile_Textured.asset` | Vendor textured sky option | Vendor sky/profile workflow | Available, not selected as an alternate Taxi sky |
| `Assets/UTS_FullPack/Skybox/Night.mat` | Static night sky | Serialized built-in sky shader; no demonstrated dynamic Weather Maker blending/lighting coordination | No; avoid a competing night-sky switch |
| `Assets/NWH/Common/Textures/Sky/Skybox.mat` | Static vehicle-demo sky | Serialized built-in sky shader; no weather/time schedule | No; NWH remains vehicle authority, not environment authority |

Taxi does not invent HDRI file names or import replacement sky content. Smooth time evolution and atmospheric transitions are delegated to installed vendor profiles and managers. Explicit debug time jumps are intentionally immediate; natural clock progression is continuous. No claim of custom true skybox interpolation is made.

## Weather Profiles

These are actual installed asset paths consumed by the existing LWS preset mapping, not new Taxi copies of weather definitions:

| LWS preset | Vendor asset | Intended condition | Used |
| --- | --- | --- | --- |
| `clear` | `Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_Clear.asset` | Clear sky | Yes, starting condition and weighted scheduling |
| `partly_cloudy` | `Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_LightCloudsScattered.asset` | Scattered cloud | Yes |
| `overcast` | `Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_OvercastClouds.asset` | Overcast | Yes |
| `light_rain` | `Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_LightRain.asset` | Light rain | Yes |
| `heavy_rain` | `Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_HeavyRain.asset` | Heavy rain | Yes |
| `thunderstorm` | `Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_Storm.asset` | Storm/thunder/lightning | Yes |
| `fog` | `Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Fog/WeatherMakerProfile_MediumFog.asset` | Fog | Yes |

All listed profiles are vendor weather data used through the existing adapter/public Weather Maker API and resource container. The Windows probe confirmed requests/application for light rain, fog, and storm; the build completed with zero errors. Storm rain is visibly present. Successful requests do not establish light-rain or fog visibility, which remain qualified below. The existing LWS `cloudy` condition is accepted if authored, but is not an additional default debug button/weighted entry.

Snow profiles and precipitation support are installed. They are deliberately not advertised as a working Taxi option because the current city has no validated snow-ground/accumulation setup. Weatherade wet/snow surface tooling and the existing NWH road-condition adapter are not reconfigured in this pass. Rain does not secretly replace or retune vehicle handling.

## URP Configuration and Isolation

Installed feature source: `Assets/WeatherMaker/Prefab/ScriptableRenderPipeline/URP/WeatherMakerURPRenderFeatureScript.cs`. Its documentation states URP 17.4+ with mandatory Render Graph APIs. It is used as supplied, not modified. The target Windows build completed with zero errors and its corrected sky/storm output was visually reviewed. This is not a fog visual pass or proof of compatibility on every platform.

The audited shared `Assets/Settings/PC_Renderer.asset` did not contain the Weather Maker feature. Existing Interstate quality pipelines reference that renderer. Consequently setup creates Taxi-owned copies:

| Source | Taxi destination / change |
| --- | --- |
| `Assets/Settings/PC_Renderer.asset` | `Assets/LWS/TruckTaxi/Rendering/TruckTaxi_Weather_Renderer.asset`, adds the installed Weather Maker feature while retaining existing renderer features |
| `Assets/LWS/InterstateHauler/Rendering/Quality/IH_Medium_RPAsset.asset` | `Assets/LWS/TruckTaxi/Rendering/TruckTaxi_Weather_RPAsset.asset`, replaces only the copied pipeline's first renderer reference |
| `Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab` | `Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_WeatherMaker.prefab`, `IsPermanent = false` for scene lifetime |

Creation uses `AssetDatabase`, `PrefabUtility`, and serialized Editor APIs, never hand-written asset YAML. Shared source assets and ProjectSettings are not changed. Runtime selects the copied pipeline for Taxi and restores the previous quality-pipeline reference on exit, provided Taxi still owns the override.

The adapter is authored disabled in the Taxi scene so Bootstrap applies the pipeline and establishes the current player/camera first. Setup can be rerun to migrate an earlier enabled adapter. It reuses one existing scene adapter, rejects multiple adapters, and does not spawn a second vendor weather system.

## Singleton Lifecycle Finding

### Unused Water Build Dependency

The first Windows build completed (main reported 477 MB) but reported a D3D11 error in `WeatherMakerWaterTesselationShader` for `UnityTessellationFactors`. The Taxi prefab retained the vendor default ResourceContainer, which explicitly references that water shader and water profiles/materials even though the scene has no water surface. The installed `WeatherMakerResourceContainerScript_NoWater.asset` removes water profiles but still explicitly lists the same shader; using it unchanged is insufficient.

`TruckTaxiEnvironmentSetup.ConfigureWeatherResources()` is a narrow Editor-only repair entry point that does not open or rebuild a scene. It copies the installed NoWater container to `Assets/LWS/TruckTaxi/ScriptableObjects/TruckTaxi_WeatherResources.asset`, clears ProfilesWater and removes water shader entries only on that project-owned copy through serialized Editor APIs, assigns the copy to the Taxi prefab, saves, and asserts that the prefab's recursive dependencies contain no water shaders. Normal scene setup uses the same operation. Rain, fog, clouds, sky, sound, and performance resources are retained. Vendor assets/source and global shader inclusion settings are unchanged. This setup was executed, its dependency regression passed, and the final Windows build completed with zero errors.

Main's original six-test PlayMode run reported duplicate `WeatherMakerScript` errors from `LwsWeatherMakerAdapter.EnsureWeatherMakerRuntime`, called again by `ApplyQualityTier` in `OnEnable`. Source inspection found that scene refresh discarded a direct newly-created component reference while `scene.isLoaded` was still false during activation.

The project-owned adapter now preserves that direct live reference, discovers components in valid activating scenes, and no longer forces the prefab's public `IsPermanent` setting to true. The default Interstate prefab retains its authored permanent behavior; Taxi retains its authored scene scope. Vendor singleton code is untouched. The isolated reload/re-enable PlayMode regression passed; its earlier recorded run is `SavedSystemsWeatherReloadPlayMode.json` (1/1 in 7.57 seconds). The final seven-test PlayMode suite also passed. These lifecycle checks are not themselves visual weather tests.

## Adapter Diagnostic Performance

Live profiling identified repeated whole-project component discovery in the project-owned adapter: the precipitation/cloud/fog diagnostic formatter ran every Update, and day/night lookup also rediscovered its manager during clock application. `LwsWeatherMakerAdapter` now caches manager components from its known weather root, with typed scene discovery only as a fallback for existing separately authored managers. Root replacement and disable/re-enable invalidate the cache; destroyed manager references resolve again immediately. Missing optional managers retry at most once per unscaled second. There is no whole-project `Component` enumeration in the adapter.

Visual diagnostic strings refresh at a configurable one-second unscaled interval (minimum 0.25 seconds), plus immediate refresh after explicit preset changes. Actual clock application, weather transitions, and vendor presentation continue at their existing cadence. Four focused EditMode cases cover root preference/cache reuse, replacement/destruction, disable invalidation, and diagnostic throttling/forced refresh; the final EditMode baseline passed. Final player profiling measured baseline population 12/12 at 251.53 FPS / 3.98 ms and dense population 144/60 at 117.51 FPS / 8.51 ms. All 60 cars in the dense run moved more than two metres over 20 seconds. These are integrated-build measurements after the adapter fix and population optimizations, not an isolated benchmark attributing every gain to this adapter.

## City Lights / Wipers / Audio

- The audited Taxi city builder has its basic directional/ambient lighting but no usable authored street/building night-light collection. `TruckTaxiEnvironmentCoordinator.nightLights` is an explicit hookup for future/current scene-authored lights, initially empty. Traffic signals and truck lamps remain separately controlled. The corrected live PlayMode camera capture confirms a dark night sky with streets still visible; final Windows sunset/night captures also visually confirm the sky repair. This does not claim a new streetlight rig or validation of every driving view.
- Existing LWS truck controls expose wiper semantics. No reliable current-truck physical wiper animation or rain-on-glass presentation was found in the inspected project-owned setup. No working wiper feature is claimed or fabricated.
- Rain, wind, thunder, and storm sources remain Weather Maker sources. Taxi exposes the cached vendor runtime root to `TruckTaxiAudioController.RouteWorldTree`, which owns World-category routing and checks dynamically added child sources on its cached-root cadence. There is no new un-routed looping source in the environment coordinator.
- A real configured project mixer is required for category volume control. Missing mixer readiness is explicitly UNVERIFIED, not a pretend success. Audio validation remains PENDING: audible transitions, weather playback, and slider response still require a listening test regardless of passing automated checks.

## Validation Boundaries

### First Windows Capture Findings

Main's first Windows sunset/night captures were reviewed: time reads 19:00/22:00 and terrain darkens, but the sky remains pale blue/white. The fog capture does not establish obvious distance-fog visibility. These are not visual passes. The scene still serialized Unity's default skybox. Installed `WeatherMakerFullScreenCloudsScript.PreSetupCommandBuffer` explicitly draws its generated sky to the camera only when the camera is not clearing with Skybox OR `RenderSettings.skybox` is null. Keeping the default skybox therefore prevents that vendor-generated backdrop from replacing it.

Taxi Editor `ConfigureScene` now clears only that scene's skybox through the public `RenderSettings` API while the Taxi scene is active, restoring the previously active scene afterward. The Taxi environment coordinator also captures and clears the skybox once before enabling the weather adapter, covering previously authored scenes and builder resets without depending on an Editor rerun. On destruction it restores the captured material only if that same scene is active and no later owner has assigned a replacement skybox. Neither path modifies cameras, vendor source, other scenes, or global graphics assets. Run `TruckTaxiEnvironmentSetup.UpdateScene()` to serialize the authored scene correction; the narrower resource-only method does not edit scene rendering settings. Runtime inspection confirmed the previous setup still used Default-Skybox / Skybox/Procedural with a Skybox-clearing gameplay camera. The subsequent actual live PlayMode camera render, `Editor_NightSkyRepair.png`, was visually confirmed: the sky is now dark at night and streets remain visible. The existing reload PlayMode fixture also asserts that the default procedural sky is absent after readiness (null/vendor sky is allowed). Final Windows `Windows_Environment_Sunset.png` and `Windows_Environment_Night.png` captures visually confirm the correction; storm rain is also visibly present.

Probe timing is deliberately short: sunset/night screenshots are one second after explicit time changes; weather screenshots follow a five-second semantic transition plus one extra real second. Existing adapter precipitation hints clamp their visual ramp to at most four seconds, and Weather Maker receives the five-second profile request. Thus a longer wait is useful for inspecting particles/visual settling, but cannot fix the explicit default-skybox bypass. Semantic completion and `LastWeatherMakerApplySucceeded` only confirm requests, not rendered fog/cloud/sky output.

### Fog Correction / Remaining Light-Rain Gap

After correction, `Windows_Environment_fog.png` visibly shows haze over distant roads/buildings relative to `Windows_Environment_light_rain.png`. Light-rain particle visibility remains UNVERIFIED. The authored LWS rain intensity is 0.28; the installed rain prefab's mist threshold is 0.5, so storm mist does not imply that light-rain mist should be present. Particle rendering still requires visual confirmation; no blind emission increase was applied during this audit.

The installed `WeatherMakerFullScreenFogProfile_Medium` uses Linear fog at density 0.00175. Its shader's linear factor is `depth * 0.1 * density`, approximately 0.0525 at 300 metres before other modulation. The adapter previously wrote its semantic density hint after `ShowFogAnimated` captured the vendor target, so the tween overwrote it. It now supplies density to that public vendor transition. The rebuilt Windows player retained the expected 0.01591 after transition completion and produced visible haze. `FogCorrectionEditMode.json` records 9/9 shared-weather tests; `WindowsSystemsFogCorrection.log` records 227/227 system checks and exit 0, and `FogCorrectionEditor.log` records the zero-error rebuild. No vendor edits or replacement rendering framework were needed.

### Final Established Validation Baseline

| Validation | Executed result |
| --- | --- |
| Truck Taxi EditMode suite | 188 passed, 2 skipped, 0 failed |
| Shared weather regression tests | 8 passed, 0 failed |
| Truck Taxi PlayMode suite | 7 passed, 0 failed |
| Isolated environment reload/re-enable regression | 1 passed, 0 failed |
| Windows systems validation | 226 checks, 0 failures |
| Windows build | Succeeded, 0 errors |

These are the suite-wide baseline results, with the fog follow-up recorded above. Skips are not passes; the isolated reload check is recorded separately rather than folded into the seven-test PlayMode count. Earlier singleton, pause-cleanup, and destroyed input-lease failures were resolved before this baseline. Audio remains PENDING and light-rain particle visibility UNVERIFIED.

The runtime probe checks semantic time/weather state, vendor apply diagnostics, and mixer assignments when available. It captures sunset, night, rain, fog, storm, and driver-needs screenshots for review. Screenshots must be inspected before reporting visible weather or UI readability PASS. Adapter success cannot substitute for actual road visibility, night-playability, weather audio, physical wipers, or hardware-controller tests.

## Vendor-First Report

VENDOR ASSETS AUDITED: installed Weather Maker 8.0.9 profiles/materials/prefab/URP feature; NWH and UTS sky alternatives; existing LWS clock/weather/road-condition integration; Heat HUD controls; existing GPS/dialogue/audio integration.

VENDOR ASSETS USED: Weather Maker presentation through the current LWS adapter, Heat UI through the current HUD, existing Compass navigation and passenger-dialogue playback.

RELEVANT ASSETS NOT USED: alternate static NWH/UTS skies, snow content without validated world support, new Weatherade material/traction changes, unverified physical wiper functionality.

CUSTOM SYSTEMS CREATED: Taxi time/weather policy and reactions, driver-needs session/QTE semantics, actual service-location components, optional scenic metadata, existing-Canvas panel, focused Editor setup/tests/probe.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
