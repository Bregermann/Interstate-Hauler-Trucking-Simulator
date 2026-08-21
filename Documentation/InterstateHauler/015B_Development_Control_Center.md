# Prompt 015B - Development Control Center

## Result

Prompt 015B adds a unified runtime-created development control center for Interstate: Hauler validation scenes.

The overlay is created by `ILwsDevelopmentUiService` through the normal `LwsApplicationBootstrap` registry, so validation scenes that use the LWS bootstrap receive one shared development UI instance without scene-local Canvas duplication.

## Runtime Architecture

- Service: `ILwsDevelopmentUiService`
- Runtime root: `LwsDevelopmentUiRoot`
- Bootstrap registration: `LwsApplicationBootstrap.CreateDefaultRegistry`
- Canvas mode: Screen Space Overlay
- Scale mode: Scale With Screen Size, 1920x1080 reference
- Event system: existing EventSystem or a development-created EventSystem
- UI toolkit: uGUI `Canvas`, `CanvasScaler`, `GraphicRaycaster`, `ScrollRect`, `Button`, and `Text`

TextMeshPro is not wired into the current LWS runtime asmdef. This prompt uses standard uGUI `Text`, matching the existing cab GPS UI path, to avoid package/asmdef churn.

## Visibility And Ownership

- Default state: hidden
- F1: toggles the development control center
- M: toggles the full GPS map
- Escape: closes the control center first, then the full GPS map
- F1 while the full map is open: closes the full map before opening/toggling the control center
- Cursor: shown/unlocked while an overlay is visible, restored when all overlays close
- Full map pause: opening the big map stores/restores `Time.timeScale`

## Tabs

- Overview
- Truck
- Transmission
- Input / Wheel
- Traffic
- GPS / Navigation
- Weather
- Road Conditions
- Streaming
- Floating Origin
- 50-Mile Test
- Performance
- Systems

## Migrated Development Functions

The control center exposes existing project-owned APIs rather than direct vendor calls.

- Truck controls send `LwsVehicleCommandFrame` to `LwsTruckControlController.ApplyCommandFrame`
- Transmission mode toggles through `Lws18SpeedTransmissionController.TrySetDevelopmentAutomaticTestMode`
- GPS route actions use `ILwsNavigationService`
- Weather buttons use `ILwsWeatherService.RequestWeather`
- Road condition buttons use `ILwsRoadConditionService`
- Streaming buttons use `ILwsWorldStreamingService`
- Floating-origin buttons use `ILwsWorldOriginService`
- 50-mile actions use `LwsFiftyMileHighwayValidationController`

## Legacy Debug Panels

Project-owned legacy IMGUI panels remain in place but now default hidden. They are fallback developer tools, not the default interface.

Affected panels include navigation, GPS settings, weather, road conditions, traffic, streaming, floating origin, 50-mile validation, truck controls, transmission, dashboard, player truck, and wheel diagnostics/calibration/input panels.

## Validation Scenes

The bootstrap-based runtime path applies to:

- `TruckValidation.unity`
- `InterstateCorridorValidation.unity`
- `StreamingHighwayValidation.unity`
- `IH_50MileFloatingOriginValidation.unity`

No vendor scenes or prefabs were modified.

## Known Limitations

- The control center is a development overlay, not a production Heat UI.
- uGUI `Text` is used until the runtime asmdef intentionally references TMP.
- Some buttons expose validation-level commands only where the underlying system already has a safe API.
- Physical/manual verification in the Unity Editor is still required for interactive layout polish.
