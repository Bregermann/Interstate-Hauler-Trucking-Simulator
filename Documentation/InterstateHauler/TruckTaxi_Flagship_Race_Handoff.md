# Priority 13 Race Checkpoint

## Implemented

- New appended `TruckTaxiRaceMode.FlagshipRace` (serialized value 6), one ordered
  loop, 20 cab-only UTS rivals. Existing six mode values and venue scenery remain.
- Course reads `TruckTaxiTrafficAdapter.cityLanes` entry `taxi.regional.lane.0`.
  This existing regional authoring output crosses the highway, loops through an
  eastern-city block, returns across the highway, and turns through western-city
  streets to finish near the start. No alternate coordinates or oval fallback.
- `TruckTaxiFlagshipRoute` validates every resampled point against the road graph,
  road class, height and <=8% grade. Existing bridge approaches are retained, not
  flattened. Missing graph/lane, incomplete highway/city sequence, nonfinite or
  oversized geometry, off-road samples and excessive grades fail explicitly.
- Global authored geometry is converted through ILwsWorldOriginService. A live
  origin shift cancels and cleans the race before relocation rather than running
  stale UTS paths. No floating-origin or navigation authority was replaced.
- Gates roughly every 100 m, explicit travel directions, bounded step checks,
  grounded single-file grid and recovery positions derived from course samples.
  Recovery retains the next required checkpoint and the endurance clock.
- UTS owns driving, steering and collision response. Existing personality speed
  scales remain; following distances vary by personality. AddTrailer is removed
  from runtime race instances using the existing taxi-rival adapter pattern.
- One race-owned path for flagship rivals; actor roots also catch failed vendor
  spawns. Countdown, entry rollback, disable/cancel/result cleanup are bounded.
- At most six recovery attempts per flagship rival, at most one attempt per
  0.5 s globally and per-rival retry spacing of 3 s. Unsafe recovery does not
  teleport near the player. Missing/destroyed/exhausted rivals become DNF.
- Unloaded current/next-gate regions freeze rivals for <=30 s, then DNF. This does
  not pin world chunks or simulate unseen race progress. Parent streaming policy
  may need a race footprint lease if all 20 must remain active far from the player.

## Parent hooks

Use the existing Bootstrap-owned `Races` instance and its Initialize(host) path.
No Bootstrap, HUD, Session.Racing, MegaDebug, vendor or YAML changes were made.

Production: `BeginFlagshipRace()`, `CancelRace(reason)`, `ResetToTrack()`.
Availability: `RefreshFlagshipRoute()`, `FlagshipUnavailableReason`, `Feedback`.
Read-only presentation: `FlagshipGlobalCheckpoints`, `NextCheckpoint`,
`CheckpointCount`, `OpponentCount`, `RivalCheckpointDiagnostics()`.

Central development menu (all mutation hooks check Debug.isDebugBuild):

- `DebugStartFlagship()` / `DebugResetRace()` / `DebugRestartFlagship()`
- `DebugTeleportToCheckpoint(zeroBasedIndex)`
- `DebugAdvanceCheckpoint()` / `DebugBackCheckpoint()`
- `DebugSetRivalCount(0..20)` (debug starts/spawns only; production stays 20)
- `DebugSpawnRivals()` / `DebugRemoveRivals()`
- `DebugCrashRival(zeroBasedIndex)` / `DebugForceFinish()`

Debug starts do not charge or enter race statistics; debug-mutated runs cannot
award results/prizes. Normal races use the existing Session accounting authority.
Flagship timing is not written into the oval-specific ThunderBowl best-lap field;
parent persistence can add a distinct endurance record later.

## Authoring and validation

Selected-host editor commands validate geometry or configure installed UTS truck
references. They do not regenerate roads, touch the venue, open/save scenes, or
add another debug panel. No authoring command was executed in this checkpoint.

Changed: RaceRuntime.cs, RaceRules.cs, RaceAuthoring.cs. Added narrowly scoped
TruckTaxiFlagshipRoute.cs and TruckTaxiFlagshipRaceTests.cs (25 test cases). Tests reuse
actual regional authoring methods; ambient service-stop spurs are excluded from
their fixture to avoid dependence on the open editor scene.

All referenced methods are implemented. Static review and scoped whitespace
checks only: no Unity, build, test execution, commit or push. The prior batch3
compile ran during the incomplete patch and is not validation of this checkpoint.
Still required: parent compile/tests, grounded 21-slot grid in the actual scene,
full route drive, rival recovery/streaming behavior, collision feel, and profiling
20 UTS vehicles. Presence of source geometry is not proof of runtime driveability.

## Post-review safety checkpoint

- Runtime and selected-host authoring now read the explicit `host.traffic` reference,
  including the default scene's separately authored traffic GameObject.
- Player eligibility uses the existing NWH adapter/controller, `IsInitialized`, input,
  enabled driver and canonical non-kinematic Rigidbody. A stale/different body fails closed.
- Current NWH coupling, capture, rescue, roadside tow and fast-travel authorities are
  checked before starts, countdown holds, player placement/reset/debug teleport and
  during active updates. New coupling cancels without moving the tractor alone.
- Failed-start rollback does not relocate or restore pause over another authority
  after eligibility is lost. No replacement physics or trailer reset is introduced.
- Added 15 regression cases for separate authored traffic, driver/body eligibility,
  coupling during countdown/reset/debug placement, and capture/recovery rejection.
  EditMode readiness is explicitly supplied by the fixture, not claimed as live NWH
  initialization. These new cases have not been executed by this agent.
- No parent integration API changes. Parent compile/tests and live grid/route checks
  remain required. No Unity invocation, build, scene/prefab/YAML or vendor edits.

## Vendor report

- VENDOR ASSETS AUDITED: UTS Full Pack 2.0 CarWalkPath, WalkPath, MovePath,
  CarAIController, AddTrailer, Truck_1/Truck_2 prefabs; existing NWH countdown/input
  adapter; LWS road graph, regional authoring, world-origin and recovery APIs.
- VENDOR ASSETS USED: LwsUtsTrafficApi.CreatePath/SpawnVehicle, UTS MovePath
  InitStartPosition/SetLookPosition, CarAIController TO_CAR and existing speed
  policies; NWH player physics/input remains authoritative.
- RELEVANT ASSETS NOT USED: EasyRoads authoring/regeneration and new Compass UI
  are unnecessary for reusing this existing authored lane. Scene Streamer remains
  behind existing regional readiness; no competing loader or chunk pin manager.
- CUSTOM SYSTEMS CREATED: bounded route validator/sampler and existing race
  framework extensions, focused tests and parent debug hooks; no new manager.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
