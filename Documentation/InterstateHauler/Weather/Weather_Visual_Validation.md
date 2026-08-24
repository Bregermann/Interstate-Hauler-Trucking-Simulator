# Weather Visual Validation

## Purpose

This stabilization pass makes Weather Maker visibly respond to LWS weather requests in gameplay scenes while keeping the authority chain intact:

`ILwsWeatherService -> LwsWeatherCoordinator -> LwsWeatherMakerAdapter -> Weather Maker`

LWS owns semantic weather and game-clock time. Weather Maker owns sky, clouds, precipitation, fog, lighting, sun, moon, and atmosphere presentation.

## Runtime Root Cause

Live weather requests were reaching the LWS weather service, but the Weather Maker runtime needed stronger presentation synchronization:

- Gameplay camera binding now prefers `ILwsCameraPresentationService` before falling back to `Camera.main`.
- Render-texture, mirror, reflection, preview, and depth cameras remain excluded from Weather Maker gameplay-camera binding.
- Instant preset requests now also push semantic precipitation/fog values directly into Weather Maker runtime components so rain, snow, and fog are immediately visible for development testing.
- Weather Maker's independent day/night clock is held at zero speed while the adapter follows `ILwsGameClockService`.

## Weather Maker Runtime

Runtime prefab:

`Assets/WeatherMaker/Prefab/WeatherMakerPrefab.prefab`

Project runtime name:

`IH Weather Maker Runtime`

The adapter expects one global Weather Maker runtime instance and reports the instance count through `ILwsWeatherRuntimeDiagnostics.WeatherMakerInstanceCount`.

## Weather Maker APIs Used

Weather profile apply:

`WeatherMakerScript.RaiseWeatherProfileChanged(oldProfile, newProfile, transitionDuration, holdDuration, forceTransition, connectionIds)`

Runtime profile field:

`WeatherMakerScript.LastLocalProfile`

Time:

`WeatherMakerDayNightCycleManagerScript.TimeOfDay`

Clock speed:

`WeatherMakerDayNightCycleManagerScript.Speed`

`WeatherMakerDayNightCycleManagerScript.NightSpeed`

Precipitation runtime hints:

`WeatherMakerPrecipitationManagerScript.Precipitation`

`WeatherMakerPrecipitationManagerScript.PrecipitationIntensity`

`WeatherMakerPrecipitationManagerScript.PrecipitationChangeDelay`

`WeatherMakerPrecipitationManagerScript.PrecipitationChangeDuration`

`WeatherMakerFallingParticleScript.Intensity`

`WeatherMakerFallingParticleScript.ExternalIntensityMultiplier`

Fog runtime hints:

`WeatherMakerFullScreenFogScript.FogProfile`

`WeatherMakerFullScreenFogProfileScript.FogDensity`

`WeatherMakerFullScreenFogProfileScript.MaxFogFactor`

Diagnostics:

`WeatherMakerFullScreenCloudsScript.CloudProfile`

`WeatherMakerFullScreenCloudProfileScript.CloudCoverTotal`

## Profile Mapping

| LWS Preset | Weather Maker Profile |
| --- | --- |
| Clear | `WeatherMakerProfile_Clear` |
| Partly Cloudy | `WeatherMakerProfile_LightCloudsScattered` |
| Cloudy | `WeatherMakerProfile_MediumHeavyClouds` |
| Overcast | `WeatherMakerProfile_OvercastClouds` |
| Light Rain | `WeatherMakerProfile_LightRain` |
| Heavy Rain | `WeatherMakerProfile_HeavyRain` |
| Thunderstorm | `WeatherMakerProfile_Storm` |
| Fog | `WeatherMakerProfile_MediumFog` |
| Light Snow | `WeatherMakerProfile_LightSnow` |
| Heavy Snow | `WeatherMakerProfile_HeavySnow` |

## Development UI

The unified Development Control Center Weather tab now shows:

- Requested LWS Weather
- Actual LWS Weather
- Weather Maker Runtime
- Weather Maker Applied Profile
- Precipitation
- Cloud Cover
- Fog
- Game Time
- Daylight
- Last Apply
- Last Error
- Instant Apply

Weather buttons cover all required validation presets. `INSTANT APPLY` mode is intended for rapid visual testing; smooth transitions remain available.

## Manual Visual Matrix

Run `InterstateCorridorValidation.unity` in the normal Unity Editor and use the Development Control Center Weather tab.

| State | Expected Visual Result |
| --- | --- |
| Clear | Clear sky and no precipitation or fog. |
| Partly Cloudy | Noticeable scattered clouds, no precipitation. |
| Cloudy | Heavier cloud cover than Partly Cloudy. |
| Overcast | Dense cloud cover and darker sky. |
| Light Rain | Visible light rainfall. |
| Heavy Rain | Stronger rainfall than Light Rain. |
| Thunderstorm | Heavy rain, darker storm profile, Weather Maker lightning/thunder where supported by the profile. |
| Fog | Obvious road-visibility reduction without making validation unusable. |
| Light Snow | Visible light snowfall. |
| Heavy Snow | Heavier snowfall than Light Snow. |

## Camera Validation

Check from cockpit and chase/exterior cameras:

- Heavy Rain remains visible through camera switches.
- Fog remains active through camera switches.
- Heavy Snow remains visible through camera switches.
- Mirror/render-texture cameras do not become the Weather Maker gameplay camera.

## Game Clock Validation

Use the Weather tab controls:

- 6 AM
- 8 AM
- Noon
- 5 PM
- 8 PM
- Midnight

Expected result: Weather Maker time, sky, sunlight/moonlight, and daylight state follow the LWS game clock instead of drifting on a separate Weather Maker clock.

## Weatherade And Road Conditions

Weatherade remains the Prompt 013 road-condition presentation adapter. This pass does not modify Weatherade source and does not redesign road physics.

Expected semantic chain remains:

- Heavy Rain -> LWS weather state -> wet road condition increases
- Snow -> LWS weather state -> snow condition develops through existing Prompt 013 logic

## Automated Coverage

Automated tests verify:

- Preset assets and installed profile paths exist.
- Runtime adapter source contains camera-presentation, precipitation, cloud, fog, and clock hooks.
- Development Weather tab exposes the visible runtime diagnostics and instant apply control.
- Weather Maker runtime can instantiate without duplicate runtime ownership.
- Heavy Rain requests reach the real runtime adapter and report precipitation diagnostics.
- The adapter follows `ILwsGameClockService`.

Automated tests do not prove that rain, snow, fog, cloud differences, or day/night changes are visually correct. The normal Unity Editor visual pass is required for full acceptance.
