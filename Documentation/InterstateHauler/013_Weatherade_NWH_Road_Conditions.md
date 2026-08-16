# Prompt 013 - Weatherade, NWH, and LWS Road Conditions

## Result

Prompt 013 establishes the project-owned road condition layer for Interstate: Hauler.

Runtime flow:

`ILwsWeatherService -> ILwsRoadConditionService -> Weatherade visual adapter -> LWS road condition snapshot -> NWH tire physics adapter`

Weather Maker remains the atmospheric weather authority. Weatherade owns visible rain/snow accumulation only. LWS owns semantic road state, accumulation, persistence shape, stable IDs, debug controls, events, and adapters. NWH Vehicle Physics 2 owns the player tractor and trailer tire response.

## Installed Package Audit

Weatherade package root:

`Assets/NOT_Lonely/Weatherade SRS`

Package:

Weatherade Snow and Rain System

Version:

`1.1.8`

Runtime namespace:

`NOT_Lonely.Weatherade`

Relevant runtime classes:

- `CoverageBase`
- `RainCoverage`
- `SnowCoverage`

URP support packages found locally:

- `Assets/NOT_Lonely/Weatherade SRS/URP Support/WeatheradeSRS_URP.unitypackage`
- `Assets/NOT_Lonely/Weatherade SRS/URP Support/WeatheradeSRS_URP_17_1.unitypackage`

Prompt 013 did not import the Weatherade URP packages or enable `USING_URP`. That remains a deliberate manual vendor setup/validation decision because Prompt 003 left Weatherade URP disabled.

## LWS Road Condition Service

Core file:

`Assets/LWS/InterstateHauler/Roads/Conditions/LwsRoadCondition.cs`

Service:

`ILwsRoadConditionService`

Implementation:

`LwsRoadConditionCoordinator`

Registered in:

`Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs`

Registered dependencies:

- `ILwsWeatherService`
- `ILwsRoadGraphService`
- `ILwsNavigationService`

The service tracks road condition snapshots by stable road/edge IDs. It prefers the current navigation edge when available, falls back to nearest road graph lookup, and uses `offroad.default` when no road context is known.

## Semantic States

Prompt 013 supports:

- `Dry`
- `Damp`
- `Wet`
- `StandingWater`
- `LightSnow`
- `PackedSnow`
- `Ice`
- `Mixed`

Major gameplay buckets:

- `DRY`
- `WET`
- `SNOW`
- `ICE`

These buckets are intentionally simpler than the stored simulation state so gameplay can make stable decisions without reading vendor systems.

## Simulation Model

The simulation stores:

- wetness
- standing water
- snow depth
- packed snow
- ice
- surface temperature
- hydroplaning risk
- longitudinal grip
- lateral grip
- braking grip
- rolling resistance

Accumulation is gradual. Rain increases wetness, heavy rain can build standing water, dry/warm/windy weather dries the road, snow accumulates when the surface is cold, snow compacts, snow/ice melt above the melt threshold, and moisture can freeze into ice.

The default profile asset is:

`Assets/LWS/InterstateHauler/Roads/Conditions/Data/IH_RoadConditionPhysics_Default.asset`

## Weatherade Visual Adapter

Adapter:

`LwsWeatheradeAdapter`

File:

`Assets/LWS/InterstateHauler/Roads/Conditions/LwsWeatheradeAdapter.cs`

The adapter uses reflection so the LWS runtime assembly does not hard-reference Weatherade. It resolves `RainCoverage`, `SnowCoverage`, and `CoverageBase` by type name at runtime.

Rain values mapped:

- `wetnessAmount`
- `puddlesAmount`
- `ripplesAmount`
- `ripplesIntensity`
- `spotsIntensity`
- `dripsIntensity`

Snow values mapped:

- `coverageAmount`

Update method:

- `UpdateCoverageMaterials()`

Coverage follows the player truck when available, then `Camera.main`, then the adapter transform. Visual area/depth are scaled by render quality tier, but physics values are not quality-scaled.

## NWH Physics Adapter

Adapter:

`LwsNwhRoadConditionAdapter`

File:

`Assets/LWS/InterstateHauler/Roads/Conditions/LwsNwhRoadConditionAdapter.cs`

NWH version:

Vehicle Physics 2 13.6

The adapter binds only the LWS active player truck and, when an attached trailer identity can be resolved, its NWH trailer wheels. It does not touch UTS traffic vehicles.

NWH APIs used:

- `VehicleController.powertrain.wheels`
- `WheelComponent.wheelUAPI`
- `WheelUAPI.LongitudinalFrictionGrip`
- `WheelUAPI.LateralFrictionGrip`
- `WheelUAPI.RollingResistanceTorque`

The adapter caches dry baselines per wheel and applies condition multipliers from that baseline. It restores the dry baseline on shutdown/disable and when a road condition reset occurs.

## Debug and Validation Tooling

Runtime controller:

`LwsRoadConditionRuntimeController`

Debug panel:

`LwsRoadConditionDebugPanel`

Validation scene wiring:

`Assets/LWS/InterstateHauler/Roads/Validation/LwsInterstateCorridorRuntimeBuilder.cs`

The corridor runtime builder can add the road-condition runtime controller, Weatherade adapter, NWH adapter, and development debug panel to the validation setup.

Debug controls include:

- Auto from weather
- Force dry
- Force wet
- Force standing water
- Force snow
- Force packed snow
- Force ice
- Reset current road
- Accumulation speed 1x, 10x, 60x
- Temperature override +10 C, 0 C, -5 C, off
- Stopping-distance measurement seam

## Validator Coverage

`Interstate Hauler / Validate Project` now checks:

- Prompt 013 runtime files exist
- Weatherade root, version, core APIs, and URP support package presence
- road condition service registration
- corridor validation wiring
- default road condition physics profile
- semantic boundary separation
- Weatherade reflection boundary
- NWH API isolation in the NWH adapter
- throttled runtime update behavior
- documentation set presence

## Automated Tests

EditMode:

- default grip hierarchy
- rain accumulation rate behavior
- drying behavior
- snow accumulation/melt behavior
- ice formation and melt behavior
- stable IDs and clamping
- vendor type leakage guard in road-condition core

PlayMode:

- default bootstrap registers one road condition service
- forced condition reaches fake visual/physics adapters
- reset restores dry baseline
- Weather Maker semantic rain snapshot drives wetness
- default registry has a single road condition service

## Manual Validation Still Required

Normal Unity Editor validation is still required for:

- Weatherade URP visual accumulation rendering
- Weatherade coverage follow behavior in the active URP renderer
- wet, standing water, snow, packed snow, ice, and mixed visual transitions
- player truck handling differences under NWH wet/snow/ice multipliers
- player trailer stability under changed tire grip
- braking/stopping-distance comparison in TruckValidation and InterstateCorridorValidation

## Known Limitations

- Weatherade URP support package is present but not imported/enabled.
- The Weatherade adapter cannot verify visuals until Weatherade URP setup is confirmed in the normal Editor.
- NWH tire grip values are adjusted at the wheel API level; final tire-material tuning and exact stopping-distance calibration are deferred.
- Hydroplaning is represented as a risk scalar and grip reduction, not a full tire-water physical simulation.
- Save/load stores a road condition state shape, but full world persistence is deferred.

## Prompt 014 Recommendations

- Run normal Unity Editor visual validation after any Weatherade URP import/define decision.
- Tune NWH grip values with real stopping-distance measurements for dry/wet/snow/ice.
- Add production UI hooks for current road condition, hydroplaning risk, and advisory warnings only after dashboard/mirror priorities remain stable.
- Keep UTS traffic road-condition response separate from NWH player physics.
