# Stabilization 002 - Highway Baseline

Date: 2026-08-24

## Result

Stabilization 002 completes the current development highway baseline without starting Prompt 016.

The pass focuses on current Editor/runtime blockers and baseline validation quality:

- Runtime UI font references now use Unity 6 `LegacyRuntime.ttf` in project-owned UI code.
- GPS voice-pack serialization was isolated into `LwsGpsVoicePack.cs` so the default asset no longer points at the mixed guidance/service script.
- UTS path creation is guarded project-side so vendor `CarWalkPath.Awake()` does not run before LWS has populated path points.
- UTS traffic vehicles now recycle if they fall below the safety floor or remain outside the active road neighborhood.
- Highway validation scenes now share one reusable cross-section presentation builder for ground, shoulders, ditches, median, and physical guardrails.
- Automatic keyboard W/S direction changes now go through `Lws18SpeedTransmissionController.TrySetAutomaticSelector`.

## Console Stabilization

The known current failures are addressed in project-owned code:

- `Arial.ttf`: removed from `LwsGameClock`, `LwsCabGpsController`, and `LwsDevelopmentUiRoot`.
- GPS voice pack: `IH_GpsVoicePack_Default.asset` points at `Assets/LWS/InterstateHauler/Navigation/LwsGpsVoicePack.cs`.
- UTS `CreateSpawnPoints`: path owners are temporarily inactive while LWS configures path data.
- Main camera warnings remain routed through the existing player camera and Weather Maker camera binding seams.
- Development UI and GPS runtime construction continue to use `CanvasRenderer`, `GraphicRaycaster` only where needed, and semantic map graphics.

## Road Baseline

Reusable builder:

`Assets/LWS/InterstateHauler/Roads/Presentation/LwsInterstateRoadsideBuilder.cs`

Runtime component:

`LwsRoadsideRuntimeRoot`

Default profile:

`LwsInterstateCrossSectionProfile.CreateValidationDefault()`

Validation integrations:

- `TruckValidation`: `LwsPlayerTruckSpawner.EnsureValidationRoadside()`
- `InterstateCorridorValidation`: `LwsInterstateCorridorRuntimeBuilder`
- `StreamingHighwayValidation`: `LwsStreamingHighwayChunkBuilder` and `LwsEndlessStreamingHighwayController`
- `IH_50MileFloatingOriginValidation`: `LwsFiftyMileHighwayChunkBuilder`

## Automatic Keyboard

Automatic W/S behavior is development-only quality-of-life input shaping for keyboard:

- Automatic mode only.
- `W` requests Drive at rest, or brakes while reversing.
- `S` requests Reverse at rest, or brakes while moving forward.
- Direction selection uses `Lws18SpeedTransmissionController.TrySetAutomaticSelector`.
- Manual 18-speed mode remains untouched.
- No direct NWH `ShiftInto` call was added.

## Traffic

Traffic remains lane/path-owned by UTS with project-side lifecycle control:

- No player proximity swerve was added.
- No direct steering corrections are applied to UTS vehicles.
- Vehicles outside the active lane neighborhood are recycled after a grace period.
- Vehicles below the safety floor are recycled immediately.
- Horn/air-horn reaction remains deferred and must stay inside lane bounds when implemented.

## GPS / UI

The Prompt 015B GPS presentation policy remains:

- Cockpit camera: cab world-space GPS on, HUD corner minimap hidden.
- Exterior/chase camera: HUD bottom-right minimap visible.
- Full map remains independent of camera mode.

Development Control Center remains the unified development UI, with bottom-left `[ DEV ]`, F1 toggle, and GPS/traffic/streaming diagnostics.

## Validation

Automated coverage added:

- Cross-section profile and generated roadside physical colliders.
- Automatic W/S direction policy.
- Manual-mode bypass.
- Project-owned UTS path/font stabilization source checks.
- GPS voice-pack asset load.
- Stabilization documentation presence.

Manual Editor validation still required for:

- Full truck drive/reverse feel under NWH physics.
- Guardrail collision behavior with the NWH tractor/trailer.
- Traffic recycling under real UTS runtime motion.
- 50-mile drive-through with origin shifts, weather changes, GPS, and traffic.

## Known Remaining Issues

- Guardrail and roadside visuals are validation-grade generated meshes, not final production art.
- UTS traffic light integration is audited only; implementation remains future work.
- Physical vehicle collision/traffic behavior requires normal Unity Editor Play Mode validation.
