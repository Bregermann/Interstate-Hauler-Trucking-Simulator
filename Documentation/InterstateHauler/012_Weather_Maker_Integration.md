# Prompt 012 - Weather Maker Atmosphere and Weather

## Summary

Prompt 012 establishes the first LWS-owned weather integration boundary for Interstate: Hauler using the installed Weather Maker package.

Weather Maker owns atmospheric presentation:

- sky
- clouds
- rain and snow precipitation rendering
- fog
- wind presentation
- lightning and storm atmosphere
- sun/moon/day-night environment

LWS owns stable weather preset IDs, gameplay-safe snapshots, weather requests, service lifecycle, future save/restore state shape, and development validation controls.

Prompt 012 intentionally does not implement Weatherade accumulation, road wetness, snow traction, ice, hydroplaning, NWH tire friction changes, or weather-aware traffic behavior. Those belong to Prompt 013.

## Weather Maker Audit

Package:

`Weather Maker`

Installed version evidence:

`8.0.9`, from `Assets/WeatherMaker/Readme.txt`.

Root:

`Assets/WeatherMaker`

Namespace:

`DigitalRuby.WeatherMaker`

Primary runtime prefab:

`Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab`

Primary manager:

`DigitalRuby.WeatherMaker.WeatherMakerScript`

Important runtime APIs:

- `WeatherMakerScript.Instance`
- `WeatherMakerScript.RaiseWeatherProfileChanged(...)`
- `WeatherMakerScript.LoadResource<T>(string name)`
- `WeatherMakerScript.PerformanceProfile`
- `WeatherMakerScript.AllowCameras`
- `WeatherMakerDayNightCycleManagerScript.Instance`
- `WeatherMakerDayNightCycleManagerScript.TimeOfDay`
- `WeatherMakerDayNightCycleManagerScript.Speed`
- `WeatherMakerDayNightCycleManagerScript.NightSpeed`

Compatibility evidence:

The installed Weather Maker documentation references Unity 6000+ and URP 17.3+ support. The project is Unity 6000.4.10f1, URP 17.4, Linear color.

## LWS Weather Boundary

Runtime flow:

`gameplay/debug request -> ILwsWeatherService -> LwsWeatherCoordinator -> LwsWeatherMakerAdapter -> Weather Maker`

State flow:

`Weather Maker active profile/time -> LwsWeatherSnapshot -> future gameplay systems`

Important files:

- `Assets/LWS/InterstateHauler/Weather/LwsWeather.cs`
- `Assets/LWS/InterstateHauler/Weather/LwsWeatherPresetDefinition.cs`
- `Assets/LWS/InterstateHauler/Weather/LwsWeatherMakerAdapter.cs`
- `Assets/LWS/InterstateHauler/Weather/LwsWeatherDebugPanel.cs`
- `Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs`
- `Assets/LWS/InterstateHauler/Roads/Validation/LwsInterstateCorridorRuntimeBuilder.cs`

Weather Maker concrete types are isolated inside `LwsWeatherMakerAdapter` through reflection. The gameplay-facing weather service does not expose `DigitalRuby.WeatherMaker` types.

## Runtime Lifecycle

Weather is global world state.

`LwsApplicationBootstrap` registers one `ILwsWeatherService` backed by `LwsWeatherCoordinator`.

`LwsWeatherCoordinator` prevents duplicate active weather services. `LwsWeatherMakerAdapter` attaches as the runtime adapter, can instantiate the Weather Maker prefab for validation in the Editor, and then applies the current LWS preset/time to Weather Maker.

The validation corridor runtime builder adds:

- `LwsWeatherMakerAdapter`
- `LwsWeatherDebugPanel`

to the project-owned validation runtime object when weather validation is enabled.

## Weather Snapshot

`LwsWeatherSnapshot` exposes:

- `weatherPresetId`
- `condition`
- `precipitationType`
- `precipitationIntensity01`
- `cloudCover01`
- `fogIntensity01`
- `windSpeedMetersPerSecond`
- `windDirectionWorld`
- `lightningActive`
- `stormIntensity01`
- `ambientTemperatureC`
- `visibilityMeters`
- `timeOfDayHours`
- `daylight01`
- `isDay`
- `isNight`
- `transitioning`
- `transitionProgress01`
- `transitionTargetPresetId`
- `versionTicks`

Prompt 013-facing fields are present for precipitation, temperature/freeze input, wind, preset identity, and transition state.

`Wetness` and `SnowAmount` remain zero in Prompt 012 so visual weather cannot accidentally become road physics.

## Presets

Project-owned preset wrapper assets live under:

`Assets/LWS/InterstateHauler/Weather/Data/`

Validation presets:

- `IH_Weather_Clear.asset`
- `IH_Weather_PartlyCloudy.asset`
- `IH_Weather_Cloudy.asset`
- `IH_Weather_Overcast.asset`
- `IH_Weather_LightRain.asset`
- `IH_Weather_HeavyRain.asset`
- `IH_Weather_Thunderstorm.asset`
- `IH_Weather_LightSnow.asset`
- `IH_Weather_HeavySnow.asset`
- `IH_Weather_Fog.asset`

The assets wrap stable LWS IDs and map to installed Weather Maker profile assets instead of modifying vendor profiles.

## Transitions

`ILwsWeatherService.RequestWeather(...)` accepts a preset ID or preset value, a transition duration, and an instant flag.

Weather Maker profile changes are applied through:

`WeatherMakerScript.RaiseWeatherProfileChanged(...)`

The LWS snapshot tracks transition target and progress for future UI/save/gameplay consumers.

## Weather Conditions

Clear uses Weather Maker's clear profile and is the default baseline.

Partly cloudy, cloudy, and overcast use installed Weather Maker cloud profiles.

Light rain, heavy rain, and thunderstorm use installed Weather Maker rain/storm profiles. Thunderstorm exposes storm and lightning semantic state.

Light snow and heavy snow use Weather Maker snow precipitation profiles only. No accumulating snow road state is enabled by Prompt 012.

Fog uses the installed Weather Maker medium fog profile and exposes reduced visibility through the LWS snapshot.

Wind is represented semantically by speed and world direction. Prompt 012 does not apply wind forces to NWH vehicles.

## Day and Night

Weather Maker is the time-of-day authority.

`LwsWeatherMakerAdapter` applies time through:

`WeatherMakerDayNightCycleManagerScript.TimeOfDay`

and time scale through:

- `Speed`
- `NightSpeed`

Development controls include sunrise, noon, sunset, midnight, paused, 1x, 10x, and 60x.

Sun/moon visual correctness still requires normal Unity Editor validation.

## Camera Binding

Weather Maker camera binding is owned by `LwsWeatherMakerAdapter`.

Policy:

- bind `Camera.main` when it is a gameplay camera
- skip cameras with `targetTexture`
- skip mirror/render texture cameras for screen-space weather overlays
- do not run expensive camera discovery every frame

Mirror cameras should receive world lighting/fog naturally where Weather Maker and URP support it, but should not receive cockpit windshield rain overlays.

## GPS Integration

`LwsCabGpsController` consumes `ILwsWeatherService` and uses `LwsWeatherSnapshot.Daylight01` to adjust the physical GPS panel theme between day and night colors.

Weather Maker does not reference GPS classes directly.

Navigation route solving, current road, voice guidance, off-route state, and rerouting remain owned by LWS navigation.

## Dashboard and Wipers

Prompt 012 does not redesign cab illumination or dashboard logic.

Weather state exposes `IsNight`, `Daylight01`, precipitation intensity, and condition for future dashboard/cab lighting.

Prompt 007 wipers remain player-controlled. Weather does not automatically override wiper state. If Weather Maker windshield drops are visible but not cleared by physical wipers, that is a Prompt 008/cab-art limitation to resolve later.

## Traffic Regression

Weather Maker is not traffic authority.

UTS traffic spawning and movement remain under the Prompt 010 traffic integration. Weather does not read or mutate UTS paths, `CarAIController`, or traffic lane data.

Normal Editor validation should verify traffic still spawns and moves under rain, storm, snow, fog, and night conditions.

## Quality Tiers

Weather quality maps through `LwsWeatherMakerAdapter.ApplyProjectQualityTier`.

Current mapping:

- Ultra -> `WeatherMakerPerformanceProfile_Fantastic`
- High -> `WeatherMakerPerformanceProfile_Beautiful`
- Medium -> `WeatherMakerPerformanceProfile_Good`
- Low -> `WeatherMakerPerformanceProfile_Fast`
- Steam Deck -> `WeatherMakerPerformanceProfile_Fastest`

The adapter reports a failure if a Weather Maker performance profile is missing rather than silently inventing settings.

## Performance

Prompt 012 avoids the Prompt 009A class of freeze:

- no weather scene search every frame
- no reflection lookup every frame after type/member caches are built
- debug panel refresh is throttled
- no RenderTexture/material creation every frame
- no weather runtime reinitialization every frame

Live performance numbers must be collected in the normal Unity Editor/Profiler because Codex cannot measure gameplay FPS, GPU frame time, mirror cost, or weather particles without running the actual Editor scene.

Required manual profiles:

- clear/day
- overcast/day
- heavy rain
- thunderstorm
- heavy snow
- fog
- night
- each relevant mirror quality tier

## Prompt 013 Seam

Prompt 013 should consume only `ILwsWeatherService`.

Prompt 013 should not read Weather Maker vendor classes directly.

Expected Prompt 013 flow:

`ILwsWeatherService -> Weatherade accumulation -> LWS road condition state -> NWH tire/road physics response`

Authority split:

- Weather Maker: atmospheric weather
- Weatherade: road/environment accumulation
- LWS: semantic road-condition orchestration
- NWH: player tire/road physics response

## Known Limitations

- Full visual weather validation is pending normal Unity Editor play-mode testing.
- Weather Maker sun/moon/light authority must be visually checked against the existing corridor directional light and Global Volume.
- Mirror overlay behavior must be validated in cab, exterior, and mirror quality modes.
- Weather Maker audio is not yet routed through a production mixer.
- No physical road wetness, snow buildup, ice, hydroplaning, or tire friction changes are implemented.
- No Weatherade runtime integration is implemented.
- No weather-aware NPC driving behavior is implemented.
