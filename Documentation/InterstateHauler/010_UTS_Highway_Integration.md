# Prompt 010 - UTS Highway Integration

## Summary

Prompt 010 establishes the first project-owned highway traffic boundary for Interstate: Hauler using Urban Traffic System Full Pack as the NPC traffic authority.

Installed UTS evidence:

- Package root: `Assets/UTS_FullPack`
- Package metadata: `Urban Traffic System Full Pack`
- Package version from installed `.meta` files: `2.0`
- Demo scenes: `Assets/UTS_FullPack/Scenes`
- Vehicle prefabs: `Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars`
- Tutorials: `Assets/UTS_FullPack/PDF Tutorials`

UTS is used only for NPC traffic vehicle/path behavior. It is not used for the player tractor, the NWH dry-van trailer, drivetrain, trailer coupling, or player input.

## Architecture

LWS owns the traffic integration boundary:

- `Assets/LWS/InterstateHauler/Traffic/LwsTraffic.cs`
- `Assets/LWS/InterstateHauler/Traffic/LwsTrafficLaneTypes.cs`
- `Assets/LWS/InterstateHauler/Traffic/LwsTrafficLaneBuilder.cs`
- `Assets/LWS/InterstateHauler/Traffic/LwsTrafficIdentity.cs`
- `Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsTrafficApi.cs`
- `Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsHighwayTrafficController.cs`
- `Assets/LWS/InterstateHauler/Traffic/UTS/LwsUtsTrafficDebugPanel.cs`

UTS classes live in the predefined Unity assembly with no namespace. LWS runtime code is in an asmdef, so the adapter resolves actual UTS classes by reflection rather than adding or modifying vendor asmdefs.

## UTS APIs Used

The adapter uses the installed public UTS classes:

- `CarWalkPath`
- `WalkPath`
- `MovePath`
- `CarAIController`
- `CarMove`
- `CarWheels`
- `AddTrailer` for detection only

No UTS source files were modified.

## Road Graph Integration

Traffic lanes are generated from the Prompt 009 LWS road graph:

- Northbound interstate: 2 lanes
- Southbound interstate: 2 lanes
- Northbound entry ramp: 1 optional traffic lane
- Turnaround crossover: excluded by default

`LwsTrafficLaneBuilder` consumes:

- `LwsRoadGraph`
- `LwsRoadEdge`
- `LwsRoadSample`
- `laneCenterOffsetsMeters`
- `speedLimitMph`
- `laneWidthMeters`
- `roadClass`
- `direction`

The corridor builder initializes traffic in `InterstateCorridorValidation` through `LwsUtsHighwayTrafficController`.

## Spawn Policy

Default validation policy:

- Density: Sparse
- Max active vehicles: 8
- Spawn interval: 4 seconds
- Minimum player spawn distance: 140 meters
- Maximum player spawn distance: 650 meters
- Despawn distance: 850 meters
- Near-lane-end despawn: 90 meters
- Traffic speed scale: 72 percent of road speed, capped at 22 m/s

This is intentionally conservative for Prompt 010. Final traffic density and performance scaling belong to later world/traffic prompts.

## Traffic Identity

Spawned UTS traffic receives:

- `LwsTrafficIdentity`
- `LwsVehicleIdentity`
- `LwsVehicleRole.AiVehicle`

This allows Prompt 007 gesture targeting and future traffic reaction systems to reason about NPC vehicles without depending on UTS concrete scripts.

## Authority Boundary

UTS owns:

- NPC vehicle following
- NPC wheel/vehicle movement
- NPC path behavior
- NPC traffic-light awareness where UTS traffic-light systems are later used

LWS owns:

- road graph/lane metadata
- traffic spawn policy
- traffic identity/registry
- traffic density tiers
- traffic debug tooling
- future traffic gameplay/reaction orchestration

NWH remains authoritative for the player tractor and player trailer.

## Performance Guards

Prompt 010 avoids Prompt 009A-style hot-path diagnostics:

- Spawns are interval-gated.
- Active traffic is capped.
- Player lookup is throttled.
- Debug panel values are cached.
- No `FindObjectsByType` or scene-wide scans run in traffic update loops.

## Validation Scene

Use:

`Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`

Expected runtime objects:

- Prompt 009 generated interstate corridor
- LWS road graph provider
- LWS UTS highway traffic controller
- generated UTS path objects under `IH UTS Highway Traffic Runtime`
- capped UTS traffic vehicles registered as LWS AI vehicles

## Prompt 010A Runtime Integration Fix

Normal Unity Editor validation exposed a real Prompt 010 failure: the corridor scene ran at healthy performance, but no `IH UTS Highway Traffic Runtime` appeared and no NPC traffic spawned.

Root cause:

- `InterstateCorridorValidation.unity` had the corridor builder script but was saved before the traffic validation fields were serialized.
- The intended lifecycle was runtime-created traffic through `LwsInterstateCorridorRuntimeBuilder`, not a scene-serialized `LwsUtsHighwayTrafficController`.
- The traffic controller created the visible runtime hierarchy only after UTS types, prefab references, policy validation, and lane generation succeeded, so early failure looked like no traffic system existed.
- Validation prefabs were not represented by a runtime-safe project-owned asset, leaving the baseline too dependent on editor-only fallback behavior.

Fix:

- Added `IH_TrafficProfile_InterstateValidation.asset` with serialized references to the selected UTS validation prefabs.
- Serialized the traffic profile into the corridor builder in `InterstateCorridorValidation.unity`.
- The corridor builder now calls `ConfigureValidationProfile()` before `InitializeFromGraph()`.
- `LwsUtsHighwayTrafficController` now creates exactly one visible root named `IH UTS Highway Traffic Runtime` before any possible initialization failure.
- The runtime root is organized into `Lanes` and `Vehicles` children.
- Initialization logs a one-time UTS type report for `CarMove`, `CarAIController`, `CarWalkPath`, `WalkPath`, `MovePath`, and `CarWheels`.
- Initialization uses a bounded lightweight retry so graph timing issues do not fail forever.
- Failure is loud through one clear Console error and the debug panel's Last Error field.
- First normal spawn is scheduled one second after successful initialization, then follows the sparse four-second interval.

Validation hierarchy after the fix:

- `IH UTS Highway Traffic Runtime`
- `IH UTS Highway Traffic Runtime/Lanes`
- `IH UTS Highway Traffic Runtime/Vehicles`

Expected generated lane IDs:

- `IH_TEST_I000_NB_MAIN_TRAFFIC_L1`
- `IH_TEST_I000_NB_MAIN_TRAFFIC_L2`
- `IH_TEST_I000_SB_MAIN_TRAFFIC_L1`
- `IH_TEST_I000_SB_MAIN_TRAFFIC_L2`
- `IH_TEST_I000_NB_ENTRY_RAMP_TRAFFIC_L1`

The development-only traffic panel now shows initialization, UTS availability, graph availability, generated lanes, prefab count, active vehicles, density, last spawn result, and last error. It also includes `SPAWN TEST TRAFFIC NOW` and `FILL TRAFFIC TO MAX` buttons. Panel state is cached at no more than four refreshes per second and does not search the scene from `OnGUI`.

## Manual Validation Required

Codex cannot physically drive the normal Unity Editor scene in this environment. The following remain normal-Editor manual checks:

- Interstate corridor loads with player truck and traffic.
- UTS vehicles spawn on lane centers.
- UTS vehicles travel in the correct direction.
- Traffic does not spawn inside the player/trailer.
- Player truck remains controlled by NWH and Prompt 005/006/007 systems.
- G29 driving and 18-speed shifting still work.
- No repeating LWS Console errors occur.
- Traffic can be disabled by setting density tier to Off or max active vehicles to 0.

## Known Limitations

- UTS path behavior is validation baseline quality, not final traffic AI.
- The validation speed is capped below full 65 MPH to keep early traffic stable.
- Lane changing, merging, realistic following gaps, traffic reactions, CB/police behavior, and traffic-light road networks are deferred.
- UTS trailer NPC support is not enabled for Prompt 010.
- UTS vehicle visuals are vendor demo art and are not final production traffic art.

## Prompt 011 Recommendations

Prompt 011 should consume the LWS road graph and traffic registry without depending directly on UTS classes. Recommended next work:

- define GPS route graph consumption
- expose player and route context to traffic systems
- add traffic-aware road events only through LWS identity/registry
- keep UTS out of player vehicle control
