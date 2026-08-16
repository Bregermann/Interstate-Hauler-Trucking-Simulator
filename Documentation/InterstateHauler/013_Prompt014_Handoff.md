# Prompt 014 Handoff

## Road Condition Runtime

Use:

- `ILwsRoadConditionService`
- `LwsRoadConditionSnapshot`
- `LwsRoadConditionType`
- `LwsRoadConditionRuntimeState`

Current flow:

`ILwsWeatherService -> LwsRoadConditionCoordinator -> LwsWeatheradeAdapter -> LwsNwhRoadConditionAdapter`

Default profile:

`Assets/LWS/InterstateHauler/Roads/Conditions/Data/IH_RoadConditionPhysics_Default.asset`

Validation tooling:

- `LwsRoadConditionRuntimeController`
- `LwsRoadConditionDebugPanel`
- `Interstate Hauler / Validate Project`

## Important Status

Weatherade installed version:

`1.1.8`

Weatherade URP support packages are present locally but were not imported/enabled by Prompt 013. Do that only through the normal Unity Editor with a clear vendor-package decision.

NWH road physics currently uses:

- `WheelUAPI.LongitudinalFrictionGrip`
- `WheelUAPI.LateralFrictionGrip`
- `WheelUAPI.RollingResistanceTorque`

The NWH adapter targets the LWS active player truck and attached NWH trailer only. UTS traffic remains untouched.

## Prompt 014 Recommendations

- Manually verify Weatherade URP visuals in `InterstateCorridorValidation` and `TruckValidation`.
- Use the debug panel to compare dry/wet/snow/ice stopping distances.
- Tune grip only in `IH_RoadConditionPhysics_Default.asset` or a new LWS-owned profile.
- Do not move Weatherade or NWH concrete API access into gameplay systems.
- Keep Prompt 014 consumers on semantic road-condition state rather than Weatherade/NWH internals.
