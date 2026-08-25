# Dead Air Jam Mode

Dead Air is a contained game-jam mode inside the Interstate: Hauler Unity project. It lives under `Assets/DeadAir` and uses LWS services for vehicle input, vehicle state, navigation state, weather requests, and truck systems without creating a second vehicle or route authority.

## Scene

Main scene:

`Assets/DeadAir/Scenes/DeadAir_Main.unity`

Required roots:

- `DEAD_AIR`
- `START`
- `SYSTEMS`
- `STORY_TRIGGERS`
- `CHOICE_TRIGGERS`
- `ENVIRONMENT`
- `UI`
- `DEBUG`

`DeadAirSceneBootstrapper` repairs missing runtime systems on Play and can create the unplaced beat layout plus `DEAD_AIR_CONSTRUCTION_KIT`. The editor menu `Dead Air/Build Or Refresh Main Scene` performs the same setup in the Editor when the normal Unity Editor is available.

`Dead Air/Prepare Playable Blockout` creates the disposable validation world under:

`DEAD_AIR/ENVIRONMENT/FINAL_WORLD/DEAD_AIR_PLAYABLE_BLOCKOUT`

`FINAL_WORLD` contains the designer-facing roots:

- `DEPOT`
- `ROADS`
- `ROAD_PROPS`
- `SIGNS`
- `VEGETATION`
- `LANDMARKS`
- `LIGHTING`
- `VALID_ROAD_ZONES`
- `GAMEPLAY_PLACEMENT`

`DEAD_AIR_PLAYABLE_BLOCKOUT` is not the final map. It is a small collider-backed depot/road/fork/merge route for proving truck start, hitched trailer, cockpit lock, Basic Automatic, story triggers, choices, restart, valid-road zones, and the void failure flow.

`DeadAir_Main` carries serialized references to the project-owned Interstate validation rig prefabs:

- Truck: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`
- Delivery trailer: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab`

`DeadAirStartRigController` places the complete truck-plus-trailer rig from `DeadAirStartMarker` and attempts trailer coupling through the existing LWS/NWH coupling adapter. The source truck and trailer prefabs are not modified.

## Runtime Architecture

Dead Air systems:

- `DeadAirGameManager`: run lifecycle, restart, ending handoff, LWS bootstrap assurance.
- `DeadAirStoryDirector`: beat trigger log, choice commits, reset.
- `DeadAirVehicleAdapter`: reads LWS/NWH vehicle telemetry and selects shared LWS Basic Automatic input mode.
- `DeadAirStartRigController`: truck/trailer start placement, restart reset, delivery trailer reference, and NWH coupling request.
- `DeadAirCockpitCameraLock`: cockpit-only camera policy, camera-cycle suppression, cab GPS presentation signal, and no-on-foot seam.
- `DeadAirOffRoadFailureController`: Dead Air-only legal-road monitoring for the complete truck/trailer rig and the void-loss sequence.
- `DeadAirValidRoadZone`: authorable, overlapping valid driving corridor volumes for depot yards, roads, forks, exits, shoulders, and endings.
- `LwsKeyboardGamepadTruckInputSource`: WebGL-safe keyboard/gamepad input source for W/S/A/D driving, horn, pause, interact, and flip-off intent. Dead Air suppresses its camera-cycle command while leaving base-game camera cycling intact.
- `DeadAirAudioDirector`: radio/audio/subtitle sequences with queue, interrupt, ignore, and wait collision behavior plus static/CB squelch timing hooks.
- `DeadAirGPSDirector`: GPS presentation state, misinformation, recalculating/signal-lost/corrupt states.
- `DeadAirHud`: speed, subtitle, debug, GPS text, and shared semantic road/route map presentation.
- `DeadAirAnomalyDirector`: reusable horror hooks for weather, fog, time, GPS, dashboard/audio/traffic events.
- `DeadAirDashboardMisinformationDirector`: temporary dashboard misinformation state for validation and future cockpit instruments.
- `DeadAirTrafficHorrorDirector`: lightweight traffic horror event hook/spawn cleanup without owning base-game traffic AI.
- `DeadAirEndingDirector`: four ending IDs, deterministic choice-based resolver, fade/title/body/restart flow.

## Basic Automatic

Dead Air defaults the player truck into LWS automatic transmission mode by calling:

`LwsKeyboardGamepadTruckInputSource.TrySetInputMode(LwsTruckInputMode.BasicAutomatic, out message)`

The input source then calls:

`Lws18SpeedTransmissionController.TrySetAutomaticMode(out message)`

The mode does not bypass `Lws18SpeedTransmissionController`, does not call NWH `ShiftInto` directly, and leaves 18-speed manual intact for the base game.

Default controls:

- W / Up: accelerate forward
- S / Down: brake or reverse when stopped
- A / D: steer
- H: horn / air horn intent
- F: Flip Off Driver semantic intent
- Enter: interact
- Esc: pause/cancel

Controller:

- Left stick: steer
- Right trigger: throttle
- Left trigger: brake
- South button: horn / interact
- Start: pause

Dead Air is cockpit-only. Tab/select camera cycling is disabled only while Dead Air owns the player truck.

## Cockpit-Only Camera

`DeadAirCockpitCameraLock` finds the existing NWH/LWS cockpit camera through the truck camera changer, selects the cockpit index, and republishes `LwsVehicleCameraMode.Cockpit` through `ILwsCameraPresentationService`. Mouse/controller interior look remains available if the underlying cockpit camera supports it; Dead Air does not add an exterior/free camera and does not switch out of the cab to solve horror visibility.

There is no on-foot mode in Dead Air. The lock exposes an explicit no-on-foot state for validation and future menus, while Interstate's normal future on-foot seams remain untouched.

## Starting Rig

New runs and restarts follow this order:

1. place the truck at `DeadAirStartMarker/TRUCK_START_REFERENCE`
2. place the trailer at `DeadAirStartMarker/TRAILER_START_REFERENCE`
3. reset truck and trailer rigidbody velocities
4. request trailer coupling through `LwsNwhTrailerCouplingAdapter`
5. select Basic Automatic
6. lock the cockpit camera
7. reset HUD/story/audio/GPS/anomaly state

`DeadAirStartMarker` draws a `[TRUCK]---[TRAILER]` Scene View preview and child reference transforms so moving or rotating the marker moves the full starting rig layout.

When a placed start marker exists inside `DEAD_AIR_PLAYABLE_BLOCKOUT`, Dead Air prefers that runtime marker over construction-kit depot templates. Blank scenes still fall back to `START/DeadAirStartMarker`.

## GPS Presentation

Dead Air uses one navigation authority from LWS and one Dead Air GPS presentation state for misinformation. The HUD contains a heading-up `LwsSemanticGpsMapGraphic` instance so roads, active route, destination marker, and the player marker are drawn from the same semantic road graph and route data used by the base game.

## Story Triggers

`DeadAirTriggerZone` uses a trigger `BoxCollider`, beat metadata, editor gizmos, one-shot/cooldown behavior, and reports to `DeadAirStoryDirector`.

Triggers only activate while `DeadAirGameManager.State == Playing`, so choices/story beats stop continuing after ending or void-failure state begins.

Categories:

- Dispatch
- GPS
- CB Radio
- Environment
- Traffic
- Dashboard
- Story
- Choice Start
- Choice Commit
- Ending

The unplaced layout is created under `DEAD_AIR_UNPLACED_GAMEPLAY` and includes `DA_START_000`, `DA_001` through `DA_018`, choice groups, and `ENDING_TRIGGER`.

## Choices

`DeadAirChoiceStartTrigger` marks a choice area. `DeadAirChoiceCommitTrigger` commits an outcome by choice ID. Once a choice ID has a committed outcome, a sibling outcome cannot overwrite it.

Default choice scaffold:

- `CHOICE_01_START`
- `CHOICE_01_LEFT_COMMIT`
- `CHOICE_01_RIGHT_COMMIT`
- `CHOICE_01_STRAIGHT_COMMIT`

The same pattern is created for `CHOICE_02` and `CHOICE_03_EXIT17`.

## Construction Kit

`DEAD_AIR_CONSTRUCTION_KIT` is a designer-facing authoring root created by the bootstrapper or editor builder. It contains the requested staging tree:

- `ROAD_PIECES`: `DA_ROAD_STRAIGHT_SHORT`, `DA_ROAD_STRAIGHT_LONG`, `DA_ROAD_CURVE_LEFT`, `DA_ROAD_CURVE_RIGHT`, `DA_ROAD_FORK`, `DA_ROAD_EXIT_RIGHT`, `DA_ROAD_MERGE`.
- `ROAD_PIECES` also contains valid-road volume templates: `DA_VALID_ROAD_ZONE`, `DA_VALID_ROAD_DEPOT`, `DA_VALID_ROAD_HIGHWAY`, `DA_VALID_ROAD_FORK`.
- `CHOICE_PIECES`: `DA_CHOICE_TEMPLATE` with `CHOICE_START`, `LEFT_COMMIT`, `RIGHT_COMMIT`; `DA_EXIT_CHOICE_TEMPLATE` with `CHOICE_START`, `EXIT_COMMIT`, `STRAIGHT_COMMIT`.
- `STORY_TRIGGERS`: `DA_TRIGGER_DISPATCH`, `DA_TRIGGER_CB`, `DA_TRIGGER_GPS`, `DA_TRIGGER_ENVIRONMENT`, `DA_TRIGGER_TRAFFIC`, `DA_TRIGGER_DASHBOARD`, `DA_TRIGGER_GENERIC_STORY`, `DA_TRIGGER_ENDING`.
- `SIGNS`: `DA_SIGN_DESTINATION`, `DA_SIGN_MILEAGE`, `DA_SIGN_EXIT`, `DA_SIGN_WARNING`, `DA_SIGN_ROUTE`.
- `TRAFFIC_EVENTS`: `DA_TRAFFIC_SPAWN`, `DA_TRAFFIC_DESPAWN`, `DA_TRAFFIC_REPEATING_CAR`, `DA_TRAFFIC_FOLLOWER`, `DA_TRAFFIC_HEADLIGHTS`.
- `ENVIRONMENT_EVENTS`: `FOG_START`, `FOG_STOP`, `RAIN_START`, `RAIN_STOP`, `LIGHTNING_EVENT`, `LIGHTING_CHANGE`, `AMBIENT_SILENCE`, `AMBIENT_RESTORE`.
- `DASHBOARD_EVENTS`: `FUEL_DISPLAY_LIE`, `CLOCK_LIE`, `SPEED_DISPLAY_GLITCH`, `WARNING_LIGHT_EVENT`, `DASHBOARD_RESTORE`.
- `GPS_EVENTS`: `GPS_NORMAL`, `GPS_TURN`, `GPS_EXIT`, `GPS_RECALCULATING`, `GPS_NO_SIGNAL`, `GPS_CORRUPTION`, `GPS_RESTORE`.
- `AUDIO_EVENTS`: `AUDIO_DISPATCH`, `AUDIO_CB`, `AUDIO_GPS`, `AUDIO_MULTI_CHANNEL_SEQUENCE`.
- `ENDING_PIECES` and `DEBUG_REFERENCE`.
- `DEBUG_REFERENCE/DA_DEPOT_START_TEMPLATE`: movable depot-start template containing `DeadAirStartMarker`, `TRUCK_START_REFERENCE`, `TRAILER_START_REFERENCE`, `FORWARD_DIRECTION`, and `DA_START_000_INITIAL_TRIGGER`.

Use `Dead Air/Toggle Authoring Gizmos` to hide/show construction gizmos while placing content.

`DeadAirStartMarker` supplies the reference point and default 55 MPH pacing speed. Trigger labels show straight-line distance from start, estimated drive time at the reference speed, target pacing time, and delta. This is editor/design information only; physical trigger entry remains the only runtime activation rule.

`DeadAirRouteFlowGizmo` draws Scene View lines between ordered beats and marks choice branches with a distinct color.

## Playable Blockout

The temporary blockout route uses these road-piece instances:

- `BLOCKOUT_00_DEPOT_EXIT_STRAIGHT`
- `BLOCKOUT_01_STRAIGHT_TO_CURVE`
- `BLOCKOUT_02_GENTLE_CURVE_A`
- `BLOCKOUT_03_GENTLE_CURVE_B`
- `BLOCKOUT_04_STRAIGHT_TO_FORK`
- `BLOCKOUT_05_SIMPLE_FORK`
- `BLOCKOUT_06_LEFT_BRANCH`
- `BLOCKOUT_07_RIGHT_BRANCH`
- `BLOCKOUT_08_MERGE`
- `BLOCKOUT_09_TEMP_END_STRAIGHT`

The start package is `DEPOT_START/DA_DEPOT_START_TEMPLATE` and uses:

- Truck: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`
- Trailer: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_TestTrailer_DryVan.prefab`

Temporary validation gameplay:

- `TEST_STORY_TRIGGER`
- `TEST_CHOICE_START`
- `TEST_LEFT_COMMIT`
- `TEST_RIGHT_COMMIT`
- `TEMP_END_TRIGGER`
- `VOID_FAILURE_TEST_AREA`

The void test area is an intentionally driveable surface beside the road with no valid-road volume. There is no invisible wall; the complete rig must be allowed to leave the legal road corridor so `SUCKED INTO THE VOID` can be validated.

## Off-Road Void Failure

Dead Air uses designer-placed `DeadAirValidRoadZone` volumes to define the legal drivable corridor. Zones are trigger-style box volumes; they may overlap and can be duplicated, scaled, and rotated along depot yards, access roads, highways, shoulders, forks, Exit 17, and ending roads. Construction-kit copies are templates only until duplicated out of `DEAD_AIR_CONSTRUCTION_KIT`.

`DeadAirOffRoadFailureController` samples four default rig points:

- tractor front
- tractor rear
- trailer front
- trailer rear

The run is considered fully off-road only when every required rig point is outside every active `DeadAirValidRoadZone`. If the tractor is outside but the trailer remains valid, or the trailer swings outside while the tractor remains valid, the controller does not fail the run. Once the full rig is outside, a default 1.25 second grace timer starts. Returning any required point to a valid zone cancels the timer.

When the grace expires, Dead Air suppresses normal driving input, stops new story/choice trigger activation by leaving `Playing` state, and plays the `SuckedIntoVoid` ending:

`SUCKED INTO THE VOID`

The ending body remains designer-editable on `DeadAirEndingDirector`. Pressing Enter performs the existing restart flow: truck/trailer reset to `DeadAirStartMarker`, Basic Automatic and cockpit lock are restored, and story/GPS/audio/anomaly/traffic/off-road state is reset.

## Endings

Ending IDs:

- `Exit17`
- `TrustDispatch`
- `Lost`
- `TrustNoOne`

`TrustNoOne` is the secret positive ending slot. Ending configs are serialized on `DeadAirEndingDirector` and default configs are created if none are assigned.

Default deterministic ending rules:

- `CHOICE_03_EXIT17 = Exit17` resolves `Exit17`.
- `CHOICE_03_EXIT17 = TrustNoOne` resolves `TrustNoOne`.
- `CHOICE_01 = TrustDispatch` resolves `TrustDispatch`.
- Otherwise, fallback resolves `Lost`.

## Validation

Editor menu:

`Dead Air/Validate Jam Mode`

Checks:

- folder layout
- `DeadAir_Main` exists
- required roots
- duplicate managers
- bootstrapper
- trigger Beat IDs
- trigger colliders
- construction kit scaffolding
- final-world/playable-blockout scaffolding
- cockpit-only camera lock
- camera-cycle suppression for Dead Air
- start rig truck/trailer configuration
- delivery trailer reference
- trailer coupling request seam
- Dead Air valid-road zones and off-road void failure controller
- void loss screen config
- choice group start/commit lanes
- `DeadAirStartMarker` pacing labels
- route-flow gizmo
- shared Basic Automatic input seam
- unplaced critical beat warnings
- WebGL-risk tokens inside `Assets/DeadAir`

## Deferred Polish

- final route placement
- final VO/audio clips
- final GPS art pass
- final cockpit look clamp tuning against production cab art
- normal Unity Editor verification that the NWH fifth-wheel settles coupled from the depot start pose
- full WebGL build verification through Unity Editor
- ending-specific authored cinematics
