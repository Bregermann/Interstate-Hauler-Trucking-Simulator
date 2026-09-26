# Truck Taxi Population Density and Steering Presentation

## Scope and Verification Status

Taxi-only density configuration, existing UTS population adapters, and visual steering animation. No Bootstrap, HUD, Session, Configuration, scene builder, vendor source, vehicle physics, or input architecture changes in this implementation slice. Integration and actual Editor/Windows validation belong to the main task.

Live Editor baseline: `host.Ready=True`, **12 pedestrians and 12 traffic vehicles**. The old pedestrian `maximumPeople=24` is a manual-spawn capacity, NOT the effective starting population. Requested and achieved Windows targets are **144 pedestrians (12x)** and **60 traffic (5x)**. Final player measurements are 251.53 FPS at 12/12 and 117.51 FPS at 144/60 on this machine. All tracked traffic moved. Exact conditions, frame/physics timings and limits are in `TruckTaxi_Systems_Validation.md`.

## Vendor and Baseline Audit

| Area | Existing implementation / baseline |
| --- | --- |
| Pedestrian spawning | `Assets/UTS_FullPack/Scripts/Paths/PeopleWalkPath.cs`: public `SpawnPeople`, `SpawnOnePeople`; public inherited `WalkPath.Density`, `_minimalObjectLength`, `par`, `points`. |
| Taxi sidewalk paths | Four existing loop paths, one way each, authored density 0.05, minimum object length 3 m. UTS derives initial count from path length, not `maximumPeople`. Current native result: 12. |
| Pedestrian movement | Existing UTS `Passersby` and `MovePath`. Movement/sight/animation run in Update; path following in FixedUpdate. No NavMesh system added. |
| Old pedestrian maintenance | Ragdoll expiry immediately recreated one through its original UTS path; no periodic full-city scan, pool, radius or LOD manager. |
| Traffic | Existing `TruckTaxiTrafficAdapter` uses `LwsUtsTrafficApi.CreatePath/SpawnVehicle`, UTS `CarMove` and `CarAIController`. Original cap 12, measured 12. |
| Traffic placement | Existing authored lane nodes; 14 m car clearance, 20 m player clearance. Original out-of-world removal below Y=-5 or outside X/Z +/-420 m. One refill attempt every 3 seconds. |
| Traffic simulation | Existing UTS AI Update raycasts/movement and FixedUpdate path/driving. Existing policy: speed scale 0.7, maximum 12 m/s. Unchanged. |
| Pooling | Neither Taxi adapter has an existing reusable object pool. UTS ragdoll activation irreversibly destroys walking components. Retire and respawn through UTS; no competing pool or cosmetic "pool capacity" control added. Live-instance caps are real capacity controls. |
| Render/AI LOD | UTS Girl_11/Girl_22 already contain five native LOD levels, Animator CullUpdateTransforms, root motion off and offscreen skinning off. The Taxi profile now adjusts native presentation budgets only; no vendor AI frequency edits or new culling manager. |
| Cost | Baseline/post-change main-thread time, physics time, FPS and stalls must be measured in the same new Windows build. Not inferred from counts or source. |

## Density Profile

`TruckTaxiPopulationProfile` is an ordinary ScriptableObject. Editor setup entry point:

`LWS.TruckTaxi.Editor.TruckTaxiPopulationAuthoring.EnsureProfile()`

Expected asset: `Assets/LWS/TruckTaxi/ScriptableObjects/Population/TruckTaxiCityPopulation.asset`.

The setup method creates only when absent, preserves existing authored settings, and records measured baselines 12/12. The separate `ApplyToLoadedDemoCity()` command assigns missing profile references only on the two existing adapters in the loaded `TruckTaxi_DemoCity`; it never opens/rebuilds another scene or auto-saves an unrelated scene. The main integrator must run and save this explicit asset/scene setup.

Defaults:

| Setting | Value |
| --- | --- |
| Pedestrian multiplier | 12 |
| Traffic multiplier | 5 |
| Maximum active pedestrians | 288 safety cap, not target; measured baseline produces 144 |
| Maximum active traffic | 60 |
| Spawn footprint radius | 600 m from population host, horizontal distance |
| Despawn footprint radius | 700 m; clamped at least to spawn radius |
| Maintenance interval | 0.5 simulation seconds |
| Maximum new objects/pass | 8 |
| Maximum observed active ragdolls | 24 |
| Pedestrian refill entrance clearance | 1.5 m from people; 5 m from canonical player |
| Traffic / player spawn clearance | 14 m / 20 m (original values) |

Zero radius disables that radius check. Disabling the override or leaving the profile unset retains native initial spawning and the original capacity/maintenance behavior. Configured caps remain unchanged, allowing original baseline inspection.

Dense pedestrian startup first measures one original UTS batch, then replaces it with the UTS batch at scaled path density. All temporary baseline objects are deactivated before retirement. The authored UTS Density fields are restored immediately after the call. UTS remains responsible for sidewalk placement, grounded spawning, navigation, avoidance and animation. Dense startup does NOT pile all pedestrians on four endpoints.

Refill goes through the same public UTS single-spawn API after checking entrance clearance. Reflection metadata is cached per path, not searched each frame. Native paths must remain valid; rejected/blocked requests are counted, not solved by spawning at arbitrary coordinates. Traffic refill visits all eligible authored nodes with a coprime stride and keeps the existing UTS initialization lifecycle. Ragdoll lifetime cleanup still applies; the oldest *observed* fallen bodies above the cap retire on maintenance, free their slots, and replenish through UTS with fresh scoring IDs. There can be a brief cap overshoot between maintenance passes, never indefinite abandoned bodies.

Counts are derived from actual instances. Capability detection must continue to use the existing functional system checks, not merely target counts. If valid path capacity/clearance prevents reaching target, report the actual count and rejected requests; do not silently change the multipliers.

## Integration Hooks

Before either existing `Initialize` call:

```csharp
traffic.ConfigureDensity(profile);
pedestrians.ConfigureDensity(profile);
// Existing Initialize calls remain in the existing bootstrap.
```

For a comparable 1x density performance run in the **same new build**, before Initialize:

```csharp
traffic.ConfigureBaselineForValidation(profile);
pedestrians.ConfigureBaselineForValidation(profile);
```

This produces 12/12 using the same maintenance implementation. It changes only per-instance runtime state, NOT shared profile multipliers. Calls after initialization reject with a warning; they never silently delete a live city. Keep the main smoke runner's baseline flag handling outside these adapters. `ConfigureDensity(profile, false)` instead restores original pre-override behavior for native-baseline inspection.

Public diagnostics: `BaselineActiveCount`, `TargetCount`, `ActiveCount`, `DensityEnabled`, `IsBaselineValidation`, `RejectedSpawnAttempts`; pedestrians also expose `RagdollCount`, `RetiredCount`, `People`; traffic exposes `Vehicles`, `SpawnAttempts`. Allow the bounded traffic refill to settle while simulation is unpaused before sampling. Do not conflate paused initial counts with steady-state capacity.

`TruckTaxiPedestrianRuntimeProbe` now waits for replenishment after observing destruction. Dense refill is intentionally deferred up to a maintenance interval and entrance clearance may delay it further. The bounded wait checks for the original active count to return, with a deadline of the greater of 10 seconds or eight maintenance intervals, then retains the existing count/fresh-ID/animator assertions. Inactive retiring bodies are not counted as active population.

## Performance Follow-up: Same Counts, Less Presentation Work

Pre-optimization Windows reports are preserved in `Builds/TruckTaxiDemo/Validation/Population_*_BeforeOptimization.json`: 12/12 at 18.25 FPS and 144/60 at 8.63 FPS. After native presentation tuning plus removing per-frame whole-project weather diagnostic scans, the final `Population_Baseline.json` / `Population_Dense.json` measure 3.98ms / 8.51ms mean frames, 5.04ms / 11.61ms p95, and 251.53 / 117.51 FPS. Counts remain 12/12 and 144/60. All 12 baseline and 60 dense cars moved more than 2 metres in the 20-second samples. These uncapped development-player measurements are machine-specific, not a minimum-hardware promise.

`TruckTaxiPopulationPresentation` is a plain cached per-instance settings snapshot, NOT a MonoBehaviour, manager, spawner or AI. The existing adapters create it when binding a spawned UTS actor, refresh it on their existing 0.5-second maintenance pass and discard it on retirement. Component discovery occurs once per spawn, never per frame. Both adapters expose `PresentationCount` and `ReducedShadowCount`. Player position is the distance reference, falling back to the existing population host before the player exists.

The existing density profile now exposes:

| Setting | Default / scope |
| --- | --- |
| optimizePresentation | true; false restores captured native presentation settings without changing counts |
| cullOffscreenAnimation | true; CullCompletely only when root motion is off; no Animator enabled/disabled writes |
| lodTransitionMultiplier | 1.75; earlier transitions through the SAME meshes; final culling threshold unchanged |
| pedestrianShadowDistance | 60 m; outside this, only shadow casting is reduced, not body rendering |
| trafficShadowDistance | 100 m; same rule; explicitly ShadowsOnly renderers preserved |
| pedestrianReducedSkinningDistance | 50 m; two weights per vertex at distance, authored quality near player; authored Bone1 preserved |
| disableSkinnedMotionVectors | true; disables extra per-bone motion-vector work, not ordinary renderer visibility |
| presentationDistanceHysteresis | 8 m return band |

Zero shadow/skinning distance disables that distance override. Missing/disabled density profile and adapter disable restore settings; ragdoll-disabled Animators stay disabled. No global quality, shadow-distance, camera, material, GPS, weather, physics, sensing radius or simulation update setting changes. No renderer/GameObject is disabled. There is no ForceLOD override, so Unity still selects the proper authored LOD for each camera. Existing native spawn/deletion rules and 144/60 target counts are unchanged. Baseline validation uses the same presentation settings at 12/12; for same-density presentation A/B, toggle only optimizePresentation and keep counts at 144/60.

Main then supplied a paused live Editor sample: 94.92 ms main, 80.05 ms ScriptRunBehaviourUpdate, 0.44 ms Animators.Update, 0.07 ms MeshSkinning.Update, zero Physics.Simulate. Therefore the changes above are conservative secondary savings, NOT proof that the dominant cost is fixed. Source investigation identified `LwsWeatherMakerAdapter.Update -> RefreshWeatherMakerVisualDiagnostics -> FindSceneComponent`, where three diagnostic queries per frame each enumerate every loaded Component using Resources.FindObjectsOfTypeAll. Extra humanoid components increase that scan even while paused. This finding was handed to the main/environment owner; this population slice does not edit or disable weather. Measure individual update markers and re-run Windows A/B after the owning fix.

Added `TruckTaxiPopulationPresentationTests` cover unchanged targets, unchanged LOD mesh references/final culling threshold, distance hysteresis, root-motion exemption, preservation of disabled ragdoll animation, active renderer/collider/kinematic state, and restoration/idempotence. These tests passed in the final EditMode suite. Final Windows baseline/dense samples measured 251.53/117.51 FPS at the unchanged 12/12 and 144/60 populations; see the consolidated validation report for sample conditions and limitations.

## Steering Audit and Ownership

Original writers:

- `LwsTruckDashboardController.AnimateSteeringWheel` writes the auto-resolved `steering wheel` Transform each Update using raw `telemetry.steeringInput`, +/-450 degrees, around negative local Z. Binary input therefore snaps directly to a visual lock.
- Installed NWH `Steering.VisualUpdate` can also write its optional `steeringWheel` Transform in FixedUpdate using resolved `angle * steeringWheelTurnRatio` around positive local Z. Both paths must not compete on the same wheel.

`TruckTaxiSteeringWheelVisual` leases the existing wheel: disables only the dashboard's wheel animation, clears only NWH's optional visual Transform reference, and restores both on disable/destroy. All other gauges/indicators keep running. The single added dashboard opt-out defaults to enabled, so normal Interstate behavior is unchanged. No NWH source or road-wheel angle/curve/rate, Rigidbody, input, or arcade handling settings are modified.

Attach to the spawned player after existing handling initialization:

```csharp
var presenter = player.GetComponent<TruckTaxiSteeringWheelVisual>()
    ?? player.gameObject.AddComponent<TruckTaxiSteeringWheelVisual>();
presenter.Initialize(player.NwhAdapter.VehicleController);
```

Initialization is idempotent. Source is NWH resolved `steering.angle + externallyAddedAngle`, divided by full road-wheel lock (NOT raw input and NOT speed-limited instantaneous maximum). Thus high-speed partial road-wheel deflection does not misleadingly show full cockpit lock. Missing existing wheel binding warns and does not invent geometry.

| Live Inspector setting | Default |
| --- | --- |
| Lock-to-lock degrees | 900 (+/-450) |
| Steer visual response | 0.16 s |
| Return-to-center response | 0.23 s |
| Angular velocity limit | 900 degrees/s |
| Damping ratio | 1 (critical); tunable 1-2 overdamped |
| Local rotation axis | (0,0,-1) |

Neutral rotation comes from the dashboard's captured authored pose. Motion uses a continuous signed float, not wrapped Euler angles or shortest-path quaternion interpolation. An analytic damped spring is integrated in bounded 1/240-second substeps for consistent velocity limiting; simulation pauses preserve angle/velocity and hitches advance at most 0.1 seconds. Changing camera/settings does not reset the presenter.

`DiagnosticSummary` and individual getters expose raw input (diagnostic only), resolved road angle, target/current visual angle and visual velocity for the existing HUD/debug surface.

## Tests and Runtime Probe

Added EditMode tests cover measured baseline scaling, explicit caps, profile immutability/1x configuration, radius hysteresis, full traffic-node traversal, resolved target mapping, signed multi-turn tracking, bounded velocity, gradual return, 30/60/120 FPS equivalence (critical and overdamped), paused/nonfinite inputs, writer lease/restoration, idempotent initialization and unchanged physics configuration.

Added PlayMode integration test expects the main integration to wire the profile and presenter, verifies 12/12 baselines and 144/60 targets, waits for actual population, checks ragdoll cap and exclusive wheel ownership. It is not a substitute for driving/performance validation.

Development-only `TruckTaxiSteeringRuntimeProbe.Run(host, check, capture)` is available to the main `-systems-smoke` runner. It starts a fresh test shift, switches to the existing cockpit camera, holds the service brake, and sends left/right/release/rapid/slow tap commands through the real Input System. It requests timed screenshots and logs resolved/visual traces at 30/60/120 simulated frame steps, checks per-frame rotation bounds and return-to-center, then restores InputSettings, virtual keyboard, camera, frame settings, ride frequency and prior pause flag. It intentionally changes the test session; never invoke during a player career session. It does not teleport or modify physics. Simulated capture rates do not prove achieved hardware FPS; profile that separately. Real gamepad/wheel hardware and a driven corner/U-turn still require manual testing.

Executed validation: C# compilation and zero-error Windows build; 188 Taxi EditMode passes (two external voice skips), all seven PlayMode integration tests, Windows population/performance runs, steering timing/screenshot sequence, and actual Windows tractor-impact/ragdoll cleanup/replenishment at dense population. No original recording file was available for direct video comparison. Physical wheel/gamepad hardware feel and human-driven visual cornering remain manual validation.

## Vendor Report

- VENDOR ASSETS AUDITED: installed UTS PeopleWalkPath/WalkPath/Passersby/CarAIController/CarMove and NWH Steering public APIs; existing LWS UTS adapter, pedestrian ragdoll lifecycle and dashboard.
- VENDOR ASSETS USED: same UTS spawning/AI/physics/ragdoll paths; same NWH resolved steering. No new vendor package.
- RELEVANT ASSETS NOT USED: no new pooling/AI controller, DOTween or vehicle-input replacement. Deterministic response smoothing requires continuous resolved-angle tracking and explicit visual ownership rather than a canned animation tween.
- CUSTOM SYSTEMS CREATED: Taxi density configuration, a Taxi-only visual presenter/pure motion calculation, cached per-instance native presentation settings, narrowly scoped editor setup and opt-in test probe. No second spawner or traffic manager.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
