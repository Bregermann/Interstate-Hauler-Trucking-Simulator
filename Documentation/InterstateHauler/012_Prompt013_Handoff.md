# Prompt 013 Handoff - Weatherade and NWH Road Conditions

Use Prompt 012 only as weather context.

Weather service:

- `ILwsWeatherService`
- implementation: `LwsWeatherCoordinator`
- adapter: `LwsWeatherMakerAdapter`

Prompt 013 should consume `ILwsWeatherService.CurrentSnapshot`, not Weather Maker vendor classes.

Relevant snapshot fields:

- `weatherPresetId`
- `condition`
- `precipitationType`
- `precipitationIntensity01`
- `ambientTemperatureC`
- `visibilityMeters`
- `windSpeedMetersPerSecond`
- `windDirectionWorld`
- `transitioning`
- `transitionProgress01`
- `transitionTargetPresetId`
- `timeOfDayHours`
- `daylight01`

Current road services:

- `ILwsRoadGraphService`
- `ILwsNavigationService`

Navigation provides current road/edge context. Weather Maker is not route authority.

Authority for Prompt 013:

- Weather Maker: atmospheric weather
- Weatherade: road/environment accumulation
- LWS: semantic road-condition state/orchestration
- NWH: player tire/road physics response

Prompt 013 target states:

- Dry
- Wet
- Snow
- Ice

Prompt 012 deliberately did not add:

- Weatherade runtime integration
- road water accumulation
- snow accumulation
- ice
- hydroplaning
- wet-road tire friction
- snow tire friction
- NWH road-condition coupling

Prompt 013 must re-audit the installed Weatherade package/API before using it.
