# F2 Weather Test Panel

## Purpose

The F2 Weather Test Panel is a large, readable development-only UI for testing weather, time of day, and road-condition presentation without digging through the general F1 Development Control Center.

Weather Maker is not controlled directly from this panel. Weather buttons call the existing LWS semantic weather service, which then drives the existing Weather Maker adapter. Road-condition buttons call the existing LWS road-condition service, which then drives Weatherade visuals and NWH grip adapters through the established road-condition architecture.

## How To Open

Press `F2`.

Press `F2` again or click the large `CLOSE` button to hide it.

While the panel is open, the mouse cursor is visible and unlocked. Keyboard/gamepad truck driving input from the LWS fallback truck input source is suppressed while the panel is open, then restored when it closes.

## UI Framework

The project contains `Assets/Heat - Complete Modern UI`, including panels, buttons, icons, animations, and a Heat UI manager asset. For this development-only runtime tool, the panel uses project-owned Unity uGUI on the existing LWS development UI runtime because it avoids importing or coupling Heat prefabs into the validation scenes and keeps the panel available through the shared bootstrap.

The panel does not use `OnGUI`.

## Weather Buttons

Weather buttons call `ILwsWeatherService.RequestWeather(presetId, 0f, true)`.

- `CLEAR` -> `clear`
- `PARTLY CLOUDY` -> `partly_cloudy`
- `CLOUDY` -> `cloudy`
- `OVERCAST` -> `overcast`
- `LIGHT RAIN` -> `light_rain`
- `HEAVY RAIN` -> `heavy_rain`
- `STORM` -> `thunderstorm`
- `FOG` -> `fog`
- `LIGHT SNOW` -> `light_snow`
- `HEAVY SNOW` -> `heavy_snow`

## Time Buttons

Time buttons call `ILwsGameClockService.SetTimeOfDayHours(...)` when the game clock service is available, then synchronize the semantic weather time via `ILwsWeatherService.SetTimeOfDayHours(...)`.

- `DAWN` -> `06:00`
- `MORNING` -> `09:00`
- `NOON` -> `12:00`
- `EVENING` -> `18:00`
- `DUSK` -> `20:00`
- `MIDNIGHT` -> `00:00`

## Road Condition Buttons

Road buttons call `ILwsRoadConditionService.ForceCondition(...)`.

- `DRY ROAD` -> `ForceDry`
- `WET ROAD` -> `ForceWet`
- `PUDDLED ROAD` -> `ForceStandingWater`
- `SNOWY ROAD` -> `ForceSnow`
- `ICY ROAD` -> `ForceIce`

Weather and road-condition buttons are intentionally separate. For example, `HEAVY RAIN` tests atmospheric Weather Maker weather, while `WET ROAD` tests Weatherade/NWH road-condition presentation independently.

## Status Values

The status column shows a small, readable set of values:

- current weather condition and preset ID
- current game time
- current road condition and override mode
- current camera mode
- Weather Maker ready state
- current Weather Maker profile where diagnostics expose it
- precipitation diagnostic and intensity
- Weatherade ready state
- rain coverage active/inactive
- rain wetness
- puddles
- snow coverage active/inactive
- snow amount
- compatible road renderer count
- incompatible road renderer count
- last coverage update status

Weatherade coverage activity and update status are derived from the existing `LwsWeatheradeAdapter` diagnostics. Wetness, puddles, and snow amount come from the current LWS road-condition snapshot so the semantic state is visible even if road materials still need a separate compatibility pass.

## How To Test Weatherade

1. Open `Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`.
2. Press `F2`.
3. Press `DRY ROAD`.
4. Observe asphalt and Weatherade values.
5. Press `WET ROAD`.
6. Observe wetness value and asphalt.
7. Press `PUDDLED ROAD`.
8. Observe puddle value and asphalt.
9. Press `SNOWY ROAD`.
10. Observe SnowCoverage and asphalt.

If Weatherade values respond but the road appearance does not, investigate road material compatibility separately. Do not treat that as a Weather Maker failure.

## Supported Scenes

The panel is installed through the shared LWS development UI runtime and should be available in scenes that use the shared bootstrap, including:

- `Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`
- `Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`
- `Assets/LWS/InterstateHauler/World/Streaming/Validation/StreamingHighwayValidation.unity`
- `Assets/LWS/InterstateHauler/World/Origin/Validation/IH_50MileFloatingOriginValidation.unity`

## Boundaries

This panel does not modify Weather Maker, Weatherade, EasyRoads, NWH, weather profiles, precipitation configuration, or road materials. It is a presentation and test-command surface for existing LWS semantic services.
