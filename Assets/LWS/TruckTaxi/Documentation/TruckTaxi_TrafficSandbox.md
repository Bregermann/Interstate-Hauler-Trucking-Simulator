# Truck Taxi UTS traffic sandbox

## Ownership

UTS retains pedestrian pathing, animation, spawning and ragdoll activation, plus car pathing/steering and traffic signal timing/gates. LWS owns bounded area/road validation, population accounting, event attribution, temporary road-rage knobs, stable traffic IDs, and editor placement. The example uses the installed `Standard Semaphore.prefab`; EasyRoads road authoring and vendor source are untouched.

## Editor setup

Open `TruckTaxi_DemoCity` in the Unity Editor, then run `Truck Taxi > Traffic > Set Up Bounded UTS Sandbox` once. The helper leaves the existing city and its four original paths in place. It adds twelve small irregular local walk areas based on those path footprints, one UTS standard semaphore at the central crossing, UTS crossing path, stop lines and crosswalk paint. It wires `pedestrianAreas`, `signalIntersections`, and `roadLanes` on the existing population. Re-running uses stable child names and keeps existing paths and prefab instance; save the scene through Editor after inspection.

World Builder can instead call `EditorTrafficSandboxSetup.ConfigurePedestrianArea(anchor)` for a named `TT_PEDAREA_` empty and `ConfigureIntersection(anchor)` for **one** chosen `TT_TRAFFICLIGHT_` or `TT_CROSSWALK_` empty. The area exposes `Configure(id, size, setback)`, `areaId`, `localSize`, `roadSetback`, `walkingPaths`; the intersection exposes `Configure(id, signal, gates, path)`, `intersectionId`, `signalSystem`, `movementGates`, `crosswalkPath`, `VehicleMayProceed`, `PedestrianMayCross`. Both calls are idempotent on the same anchor. Use one shared intersection anchor; do not call the latter for both prefixes at the same crossing.

Area routes are rejected when a generated point lies within `roadSetback` of a traffic lane centerline, and the population validates every UTS path point and bound pedestrian position before accepting it. A configured area supersedes the old giant path loops without deleting them. The explicit signal-controlled crossing is the sole road-crossing exception. The population profile still controls the 144-pedestrian/60-traffic targets; no new hardcoded target count was added.

## Traffic integration

`TruckTaxiTrafficAdapter.TrySpawnDedicatedVehicle(near, out vehicle, out stableId)` chooses a clear existing UTS lane point within 100 m. `TrySpawnDedicatedVehicle(stableId, near, out vehicle)` accepts a caller-owned stable ID and refuses duplicates. Both return UTS road-following cars, not custom controllers, and use at most eight extra dedicated slots. `ActiveVehicles` is the cached stable-ID map; `TryResolveVehicle(stableId, out vehicle)` resolves live IDs; `TryGetVehicleAi(stableId, out ai)` exposes the existing UTS `CarAIController` component for supported behavior knobs; `ReleaseDedicatedVehicle(stableId)` returns the car to ordinary despawn policy. Ambient cars have stable IDs but are **not** mission targets by default. UTS has no supported dynamic pursuit destination/chase API, so no pursuit hook is offered.

`TruckTaxiAiPedestrianImpact.WorldImpact` is a world-only event raised on genuine AI-car collision after one ragdoll transition. It never calls `Session.RecordPedestrianHit`; only the player collision observer does that. AI hit audio is a short runtime-generated, non-voice thud routed to the existing World bus; an authored clip can later replace it. Road rage has a two-car global cap, low collision trigger chance, four-second duration, 45-second cooldown, modest UTS acceleration/following-distance changes, then restores defaults. If a vehicle already has a horn clip, it plays; no horn asset is synthesized. Nearby pedestrians temporarily use the existing UTS run animation state.

## Parent-owned validation

The expansion pass ran `TruckTaxiTrafficSandboxTests` and existing population/steering logic tests successfully. `TruckTaxiAiPedestrianImpactPlayModeTests` passed a genuine Rigidbody impact, ragdoll transition and exactly one world event without a player observer. The Editor helper was executed and the scene saved. Integration checked actual vendor signal references, registered area count, and a real UTS car's temporary aggression returning to normal. Actual stop-line compliance, pedestrian crossing timing and a new measured NWH impact-speed-loss sweep still need manual review; configured references alone do not prove those behaviors. `Observe UTS Signal Phases (60s)` is available for signal diagnostics.
