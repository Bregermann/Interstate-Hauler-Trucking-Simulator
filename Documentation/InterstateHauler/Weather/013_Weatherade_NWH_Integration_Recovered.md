# Prompt 013 Weatherade + NWH Road Conditions - Recovered

## Result

Status: IMPLEMENTED - VISUAL VALIDATION REQUIRED
Visible status: UNVERIFIED

This recovery keeps LWS road-condition semantics and NWH tire/grip mapping intact, while ensuring LWS-generated validation road surfaces use Weatherade-compatible material workflow for visible wetness and snow accumulation.

## Installed Vendor Audit

Weatherade SRS version: 1.1.8
Path: Assets/NOT_Lonely/Weatherade SRS
Namespace: NOT_Lonely.Weatherade

Demo scenes inspected:

- Assets/NOT_Lonely/Weatherade SRS/Samples/BasicSetupRain.unity
- Assets/NOT_Lonely/Weatherade SRS/Samples/BasicSetupSnow.unity

Relevant vendor scripts:

- NOT_Lonely.Weatherade.CoverageBase
- NOT_Lonely.Weatherade.RainCoverage
- NOT_Lonely.Weatherade.SnowCoverage

Relevant vendor shaders:

- NOT_Lonely/Weatherade/Rain Coverage
- NOT_Lonely/Weatherade/Snow Coverage

## Root Cause Of Live Dry-Road Behavior

The previous LWS road-condition architecture could drive RainCoverage and SnowCoverage values, but generated validation highway meshes used plain URP Lit runtime materials in several paths. Weatherade coverage components cannot make an incompatible renderer visibly wet or snowy.

Recovered fix:

- Added LwsWeatheradeMaterialFactory to create LWS-owned runtime road materials using Weatherade's installed Rain Coverage and Snow Coverage mesh shaders.
- Updated InterstateCorridorValidation generated road materials to start Weatherade-compatible.
- Updated StreamingHighwayValidation generated road materials to start Weatherade-compatible.
- Updated IH_50MileFloatingOriginValidation generated road materials to start Weatherade-compatible.
- Updated LwsWeatheradeAdapter to scan LwsRoadSurface renderers on a throttled cadence and switch rain/snow-compatible runtime material modes for newly loaded or recycled road chunks.

No vendor material or vendor source file was manually modified.

## Runtime Flow

LWS semantic weather -> LwsRoadConditionCoordinator -> LwsWeatheradeAdapter -> Weatherade RainCoverage/SnowCoverage visuals

LWS semantic weather -> LwsRoadConditionCoordinator -> LwsNwhRoadConditionAdapter -> NWH tire/grip parameters

Weatherade does not own tire friction. NWH does not own road wetness visuals.

## Weatherade Adapter Behavior

Rain or wet roads:

- Ensures RainCoverage is the active Weatherade coverage component.
- Sets wetnessAmount from LwsRoadConditionSnapshot.wetness01.
- Sets puddlesAmount from LwsRoadConditionSnapshot.standingWater01.
- Sets ripple/spots/drips intensity from semantic wetness and standing water.
- Calls UpdateCoverageMaterials.

Snow, packed snow, or ice:

- Ensures SnowCoverage is the active Weatherade coverage component.
- Sets coverageAmount from max(snowDepth01, packedSnow01, ice01 * 0.35).
- Calls UpdateCoverageMaterials.

Dry baseline:

- Uses RainCoverage with zero wetness/puddles to clear the active Weatherade visual state.
- Uses rain-compatible road materials as the default dry-ready material mode.

## Streaming And Floating Origin

Weather Maker remains global and is not per chunk.

Weatherade road material binding runs on LwsRoadSurface objects in loaded scenes on a throttled interval. New streamed/recycled road chunks become compatible without resetting semantic weather. On floating-origin completion the adapter reconfigures coverage follow target and refreshes coverage/material application.

## Diagnostics

The Road Conditions development UI and legacy road-condition debug panel now expose:

- Weatherade runtime status
- RainCoverage actual component/value diagnostics
- SnowCoverage actual component/value diagnostics
- road material compatibility
- number of bound and incompatible road renderers
- depth coverage settings
- last vendor UpdateCoverageMaterials result

## NWH Preservation

LwsNwhRoadConditionAdapter remains separate and continues to map semantic road-condition grip into NWH. This pass does not touch truck forward/reverse controls, transmission, DirectInput, force feedback, or NWH source.

## Validation Required

Normal Unity Editor visual validation is required to confirm wet appearance, puddles, and snow accumulation are visible in Game view. Automated tests verify adapter/material wiring only.