# Prompt 015A - 50-Mile Floating-Origin Highway Test

## Purpose

Prompt 015A adds a development-only endurance scene for validating long-distance highway driving under the Prompt 014 streaming layer and Prompt 015 floating-origin layer.

The test is intentionally simple: a straight divided interstate with enough distance to force repeated origin shifts, streaming policy transitions, GPS updates, traffic spawning/despawning, weather changes, and road-condition updates while the NWH player truck drives normally.

## Scene

- Scene: `Assets/LWS/InterstateHauler/World/Origin/Validation/IH_50MileFloatingOriginValidation.unity`
- Master scene owns bootstrap, streaming, floating origin, navigation, weather, road conditions, UTS traffic, player spawn, and debug tooling.
- Chunk scenes are additive streamed content under `Assets/LWS/InterstateHauler/World/Origin/Validation/Chunks/`.
- Chunk scenes do not own global services or player-owned truck content.

## Exact Distance

- Total miles: 50.0
- Total meters: 80,467.2
- Meter conversion: 1 mile = 1,609.344 m
- Axis: global +Z
- Start: Mile 0 at global Z 0
- End: Mile 50 at global Z 80,467.2

## Chunk Layout

- Chunk count: 25
- Chunk length: 2 miles / 3,218.688 m
- Chunk IDs: `IH_50MI_CHUNK_000` through `IH_50MI_CHUNK_024`
- Each chunk scene root is authored at its canonical global start Z.
- Runtime local position is derived from global position minus the active floating-origin offset.
- Chunk bounds remain canonical global bounds in the streaming manifest.

## Road

- Road type: straight divided interstate validation road
- Lanes: 2 eastbound, 2 westbound
- Speed limit: 65 mph
- Surface: LWS asphalt interstate via `LwsRoadSurface`
- Start pad exists in chunk 000.
- Mile 50 turnaround/finish pad exists in chunk 024.
- Whole-mile markers are generated from Mile 0 through Mile 50.

## Floating Origin

- Tuning asset: `Assets/LWS/InterstateHauler/World/Origin/Validation/Data/IH_FloatingOrigin_50Mile.asset`
- Threshold: 1,000 m
- Grid: 500 m
- Axes: X and Z enabled, Y disabled
- Expected shift frequency on the straight road: about every 500 m after the truck crosses the 1,000 m local threshold.
- Expected approximate shifts over 50 miles: about 160, depending on manual teleports, speed, and exact centerline position.

## Streaming

- Manifest: `Assets/LWS/InterstateHauler/World/Origin/Validation/Data/IH_WorldStreamingManifest_50Mile.asset`
- Policy: `Assets/LWS/InterstateHauler/World/Origin/Validation/Data/IH_WorldStreamingPolicy_50Mile.asset`
- Load ahead: 7,000 m
- Keep behind: 3,800 m
- Preload margin: 500 m
- Unload distance: 10,000 m
- Maximum concurrent loads: 3
- Intended behavior: only the current neighborhood is requested, not all 25 chunks.

## Player Truck

- Player spawner: `LwsPlayerTruckSpawner`
- Truck definition: `Assets/LWS/InterstateHauler/Vehicles/Data/IH_TruckDefinition_StarterNwhSemi.asset`
- Truck prefab: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`
- Trailer prefab: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab`
- Startup transmission default remains the development automatic mode from the earlier targeted change.
- NWH remains authoritative for vehicle physics.

## GPS

- Road graph ID: `IH_50_MILE_GLOBAL_ROAD_GRAPH`
- Eastbound route road ID: `IH_TEST_50MI_EB`
- Westbound return road ID: `IH_TEST_50MI_WB`
- Initial route: Mile 0 eastbound to Mile 50 eastbound.
- Route distance: 80,467.2 m / 50.0 mi.
- The route graph uses canonical global coordinates; local player pose is converted through the active origin service before navigation updates.

## Traffic

- Traffic controller: `LwsUtsHighwayTrafficController`
- Profile: `Assets/LWS/InterstateHauler/Traffic/Data/IH_TrafficProfile_InterstateValidation.asset`
- Profile maximum active vehicles: 20
- Torture-test target: roughly 12-20 active vehicles when safe spawn points are available.
- UTS vehicle source remains the existing Prompt 010 integration.
- Traffic spawn/path positioning is expected to use global/local conversion through the Prompt 015 origin-aware traffic path.

## Weather

- Runtime weather authority: LWS weather service plus Weather Maker adapter.
- Weather is global, not per streamed chunk.
- Automatic weather cycle is enabled by default in the validation controller.
- Weather changes are requested by global mile:

| Miles | Preset |
| --- | --- |
| 0-5 | clear |
| 5-10 | partly_cloudy |
| 10-15 | overcast |
| 15-20 | light_rain |
| 20-25 | heavy_rain |
| 25-30 | thunderstorm |
| 30-35 | fog |
| 35-40 | light_snow |
| 40-45 | heavy_snow |
| 45-50 | clear |

## Road Conditions

- Road-condition runtime controller is present in the master validation scene.
- Weatherade adapter and NWH road-condition adapter are added by the validation controller.
- Road conditions should follow the global weather state and remain origin safe.
- This scene does not create per-mile physical weather volumes.

## Debug Panel

Component: `LwsFiftyMileHighwayDebugPanel`

Toggle: F10

Displays:

- distance driven and remaining
- current mile and chunk
- global and local player positions
- origin offset, shift count, last shift, origin version
- streaming load/unload counts
- traffic active and total spawned
- weather preset and precipitation
- road-condition snapshot
- GPS route state and remaining miles
- load/origin performance numbers
- last report and last error

Quick teleport buttons:

- Mile 0
- Mile 10
- Mile 25
- Mile 40
- Mile 49

Teleports use the validation origin API and update streaming around the target global mile.

## End Report

At Mile 50 the controller logs a completion report with:

- total distance
- origin shifts
- chunk loads and unloads
- max local distance observed
- traffic spawned
- weather change count
- GPS pass/fail state
- last error

## Known Limitations

- The road visuals are validation geometry, not production EasyRoads art.
- Runtime chunk meshes/materials are intentionally simple.
- Manual driving is still required to validate real NWH truck behavior, traffic behavior, weather visuals, hitch stability, and long-haul performance.
- The 50-mile scene is a development torture test, not a production world scene.

## Prompt 016 Boundary

Prompt 015A does not begin Prompt 016. It provides a long-distance validation scene for future world scale, terrain, and content prompts.
