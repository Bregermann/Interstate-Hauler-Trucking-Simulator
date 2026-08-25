# Prompt 012 Weather Maker Integration - Recovered

## Result

Status: IMPLEMENTED - VISUAL VALIDATION REQUIRED
Visible status: UNVERIFIED

This recovery keeps LWS as the semantic weather authority and uses the installed Weather Maker package for actual sky, cloud, rain, snow, fog, storm, sun, moon, and day/night presentation. It does not create custom LWS precipitation, fog, cloud, or lightning renderers.

## Installed Vendor Audit

Weather Maker version: 8.0.9
Path: Assets/WeatherMaker
Namespace: DigitalRuby.WeatherMaker
Runtime prefab: Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab
Runtime manager: DigitalRuby.WeatherMaker.WeatherMakerScript

Demo scenes inspected:

- Assets/WeatherMaker/Demo/Scenes/DemoScene.unity
- Assets/WeatherMaker/Demo/Scenes/DemoSceneWeatherApi.unity
- Assets/WeatherMaker/Demo/Scenes/DemoSceneRollingStorm.unity
- Assets/WeatherMaker/Demo/Scenes/DemoScenePrecipitationZones.unity
- Assets/WeatherMaker/Demo/Scenes/DemoSceneOriginOffset.unity
- Assets/WeatherMaker/Demo/Scenes/DemoSceneMultipleCameras.unity
- Assets/WeatherMaker/Demo/Scenes/DemoSceneMirror.unity
- Assets/WeatherMaker/Demo/Scenes/Prefab/ScriptableRenderPipeline/URP/DemoSceneURP.unity

Installed docs confirm Unity 6000+ and URP 17.3+ support. The project uses URP 17.4 and Linear color space.

## LWS Authority

LWS owns:

- ILwsWeatherService
- LwsWeatherCoordinator
- weather preset IDs
- semantic precipitation, cloud, fog, wind, temperature, storm, and visibility values
- game clock authority through ILwsGameClockService
- road-condition semantic handoff

Weather Maker owns:

- sky rendering
- clouds
- precipitation visuals
- fog rendering
- lightning/thunder presentation
- sun/moon/day-night visuals
- visual transitions

## Runtime Integration

LwsWeatherMakerAdapter resolves Weather Maker by reflection so gameplay services do not take a hard dependency on DigitalRuby concrete types.

The adapter uses:

- WeatherMakerScript prefab instancing from Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab
- WeatherMakerScript.RaiseWeatherProfileChanged(oldProfile, newProfile, transitionDuration, holdDuration, forceTransition, connectionIds)
- WeatherMakerScript.LoadResource<T>(profileName)
- WeatherMakerScript.LastLocalProfile
- WeatherMakerScript.AllowCameras
- WeatherMakerScript.AutoFindMainCamera
- WeatherMakerPrecipitationManagerScript.Precipitation
- WeatherMakerPrecipitationManagerScript.PrecipitationIntensity
- WeatherMakerPrecipitationManagerScript.RainIntensity and SnowIntensity diagnostics
- WeatherMakerFullScreenFogScript.FogProfile
- WeatherMakerDayNightCycleManagerScript.TimeOfDay
- WeatherMakerDayNightCycleManagerScript.Speed

Exactly one Weather Maker global runtime should exist. Streamed road chunks must not instantiate Weather Maker.

## Preset Mapping

| LWS Preset | Weather Maker Profile | Asset Path |
|---|---|---|
| Clear | WeatherMakerProfile_Clear | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_Clear.asset |
| Partly Cloudy | WeatherMakerProfile_LightCloudsScattered | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_LightCloudsScattered.asset |
| Cloudy | WeatherMakerProfile_MediumHeavyClouds | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_MediumHeavyClouds.asset |
| Overcast | WeatherMakerProfile_OvercastClouds | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Clouds/WeatherMakerProfile_OvercastClouds.asset |
| Light Rain | WeatherMakerProfile_LightRain | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_LightRain.asset |
| Heavy Rain | WeatherMakerProfile_HeavyRain | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_HeavyRain.asset |
| Thunderstorm | WeatherMakerProfile_Storm | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Rain/WeatherMakerProfile_Storm.asset |
| Fog | WeatherMakerProfile_MediumFog | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Fog/WeatherMakerProfile_MediumFog.asset |
| Light Snow | WeatherMakerProfile_LightSnow | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Snow/WeatherMakerProfile_LightSnow.asset |
| Heavy Snow | WeatherMakerProfile_HeavySnow | Assets/WeatherMaker/Prefab/Profiles/Weather/Individual/Snow/WeatherMakerProfile_HeavySnow.asset |

The storm profile includes CloudProfile, PrecipitationProfile, FogProfile, and LightningProfile references.

## Camera Policy

The adapter binds Weather Maker to the active gameplay camera from ILwsCameraPresentationService when available, with Camera.main as fallback. Mirror, reflection, preview, depth, and target-texture cameras are filtered out from the main weather camera binding.

Mirror weather support remains a quality/performance policy item. Do not register every mirror RenderTexture camera as an atmospheric authority.

## Game Clock

ILwsGameClockService remains the semantic time authority. The Weather Maker day/night cycle is slaved to LWS time through TimeOfDay, and Weather Maker time scale is set by the LWS clock state rather than running independently.

## Development Controls

The Development Control Center Weather tab invokes ILwsWeatherService, not Weather Maker directly. It exposes:

- Clear
- Partly Cloudy
- Cloudy
- Overcast
- Light Rain
- Heavy Rain
- Storm
- Fog
- Light Snow
- Heavy Snow
- instant/smooth apply
- time controls

## Validation Required

Normal Unity Editor visual validation is still required. Automated tests can verify that Weather Maker resolves, one runtime exists, presets map to installed profile assets, and the adapter receives game clock/camera state. They cannot prove that the Game view visibly looks like heavy rain, fog, or snow.

Required visible checks are documented in Weather_Visual_Test_Matrix.md.