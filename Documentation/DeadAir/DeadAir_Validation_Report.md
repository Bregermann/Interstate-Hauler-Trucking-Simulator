# Dead Air Validation Report

## Implemented

- `Assets/DeadAir` folder structure.
- `DeadAir_Main` scene skeleton.
- Serialized reference to the project-owned LWS player truck prefab for runtime spawn.
- Serialized delivery trailer prefab seam using `IH_TestTrailer_DryVan` for the jam-mode delivery trailer.
- Cockpit-only camera lock for Dead Air with base-game camera cycling preserved outside Dead Air.
- Start rig controller that places truck and trailer from `DeadAirStartMarker`, resets rigidbodies, selects Basic Automatic, and requests NWH trailer coupling through the existing LWS adapter.
- Dead Air-only valid-road volume system and off-road void failure controller.
- `SUCKED INTO THE VOID` loss ending with existing Try Again/restart flow.
- Runtime managers for game lifecycle, story, vehicle adapter, audio, GPS, anomalies, dashboard misinformation, traffic horror hooks, and endings.
- Shared LWS Basic Automatic keyboard/gamepad input mode.
- Physical trigger-zone authoring model.
- Choice start/commit model with grouped start/left/right/straight scaffolds.
- Unplaced full beat layout utility and exact construction kit builder for roads, choices, story triggers, signs, traffic, environment, dashboard, GPS, audio, endings, and debug reference roots.
- `DeadAirStartMarker` complete `[TRUCK]---[TRAILER]` rig preview, straight-line pacing labels, and `DeadAirRouteFlowGizmo` narrative order visualization.
- `DA_DEPOT_START_TEMPLATE` with truck reference, trailer reference, forward marker, and initial run trigger.
- `DA_VALID_ROAD_ZONE`, `DA_VALID_ROAD_DEPOT`, `DA_VALID_ROAD_HIGHWAY`, and `DA_VALID_ROAD_FORK` construction-kit templates.
- `Dead Air/Prepare Playable Blockout` editor command for the disposable validation route.
- `DEAD_AIR/ENVIRONMENT/FINAL_WORLD` hierarchy with `DEPOT`, `ROADS`, `ROAD_PROPS`, `SIGNS`, `VEGETATION`, `LANDMARKS`, `LIGHTING`, `VALID_ROAD_ZONES`, and `GAMEPLAY_PLACEMENT`.
- `DEAD_AIR_PLAYABLE_BLOCKOUT` route scaffold for depot start, straight road, gentle curve, fork, branch commits, merge, temporary end, and void-failure test area.
- Runtime valid-road copies for depot, highway, curve, fork/branches, and merge/end. Construction-kit valid-road templates remain ignored at runtime.
- Temporary validation triggers: `TEST_STORY_TRIGGER`, `TEST_CHOICE_START`, `TEST_LEFT_COMMIT`, `TEST_RIGHT_COMMIT`, and `TEMP_END_TRIGGER`.
- Runtime start-marker selection that prefers a placed `DEAD_AIR_PLAYABLE_BLOCKOUT` depot marker over construction-kit templates.
- Development HUD, subtitles, GPS status, speed readout.
- Dashboard misinformation HUD seam.
- Ending overlay, deterministic choice resolver, and restart path.
- Editor scene builder and validator.
- EditMode and PlayMode smoke tests.

## Shared Interstate Changes

Added shared LWS Basic Automatic selection and optional camera-cycle suppression to `LwsKeyboardGamepadTruckInputSource`, plus public transmission mode helpers on `Lws18SpeedTransmissionController`. Dead Air consumes those APIs and does not modify vendor or NWH source.

## Manual Unity Editor Validation Required

- Open `Assets/DeadAir/Scenes/DeadAir_Main.unity`.
- Run `Dead Air/Build Or Refresh Main Scene`.
- Run `Dead Air/Prepare Playable Blockout`.
- Confirm scene roots and unplaced layout are saved.
- Confirm `DEAD_AIR_CONSTRUCTION_KIT` is saved with road/sign/trigger template roots.
- Confirm `DEAD_AIR/ENVIRONMENT/FINAL_WORLD/DEAD_AIR_PLAYABLE_BLOCKOUT` exists and is clearly labeled as disposable.
- Confirm `FINAL_WORLD` contains `DEPOT`, `ROADS`, `ROAD_PROPS`, `SIGNS`, `VEGETATION`, `LANDMARKS`, `LIGHTING`, `VALID_ROAD_ZONES`, and `GAMEPLAY_PLACEMENT`.
- Confirm `DEBUG_REFERENCE/DA_DEPOT_START_TEMPLATE` is present and can be moved as a group.
- Confirm the playable blockout contains `DEPOT_START/DA_DEPOT_START_TEMPLATE` and that `DeadAirStartRigController` references that placed marker.
- Confirm `DeadAirStartMarker` previews the complete truck/trailer start rig.
- Confirm the delivery trailer prefab is assigned to `DeadAirStartRigController`.
- Confirm the truck and trailer begin aligned as `[ TRUCK ]---[ TRAILER ]` facing the depot exit road.
- Confirm Play Mode starts in the cockpit camera and Tab/select do not switch to exterior cameras.
- Confirm valid-road volumes cover the depot start, truck reference, trailer reference, route, shoulders, forks, and ending branches.
- Confirm `VOID_FAILURE_TEST_AREA` is outside all runtime valid-road zones and has no wall preventing entry.
- Confirm putting only one point/wheel/truck-or-trailer end outside the valid zone does not fail the run.
- Confirm leaving the entire rig outside valid zones starts the grace timer and returning cancels it.
- Confirm staying fully outside beyond the grace timer shows `SUCKED INTO THE VOID`.
- Confirm Try Again restarts the complete rig and clears off-road state.
- Confirm mouse/controller cockpit look still behaves within the normal cab camera limits.
- Confirm the truck starts with the trailer positioned behind it and NWH reports the hitch coupled after physics settles.
- Confirm Restart returns truck/trailer to the marker, reapplies Basic Automatic, and locks cockpit mode.
- Confirm route-flow and trigger pacing gizmos are visible and can be toggled from `Dead Air/Toggle Authoring Gizmos`.
- Press Play.
- Confirm Basic Automatic mode shows on the player truck when an LWS truck is present.
- Confirm W/S/A/D driving in a scene with the player truck.
- Drive the full disposable blockout: depot exit, straight, gentle curve, fork, either branch, merge, temporary end.
- Drive through `TEST_STORY_TRIGGER` and confirm the overlay/log reports `TEST STORY TRIGGER FIRED`.
- Drive through `TEST_CHOICE_START`, then either `TEST_LEFT_COMMIT` or `TEST_RIGHT_COMMIT`, and confirm sibling commits do not overwrite the first outcome.
- Drive through `TEMP_END_TRIGGER` and confirm temporary ending flow is invoked.
- Confirm HUD speed/GPS/subtitles display.
- Confirm triggers fire when the truck collider enters trigger zones.
- Confirm choice commits cannot be overwritten by sibling outcomes.
- Confirm dashboard misinformation and traffic horror hooks fire without creating a second GPS/traffic authority.
- Confirm ending overlay appears and restart resets runtime state.
- Run EditMode and PlayMode tests from Unity Test Runner.
- Build WebGL to `Builds/DeadAir_WebGL`.

## Known Risks

- Dead Air content placement remains intentionally unplaced.
- Final audio clips are not authored yet.
- A truck must be present in the target validation scene for actual driving tests.
- Actual NWH fifth-wheel coupling requires normal Unity Editor Play Mode verification; automation only verifies the Dead Air start-rig state and attach request seam.
- Dead Air cockpit look limits depend on the existing NWH cockpit camera configuration.
- WebGL build was attempted, but Unity batchmode stalled in Licensing Client reconnect loops before the build method could execute.
- The manually-authored `DeadAir_Main` scene includes the unplaced beat layout and relies on the bootstrapper/editor builder to repair or refresh runtime systems.
