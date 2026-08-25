# Dead Air Construction Kit

Use `Dead Air/Build Or Refresh Main Scene` in the normal Unity Editor to create or refresh the staging area:

`DEAD_AIR_CONSTRUCTION_KIT`

This is a designer staging area. Duplicate templates from it, move the duplicates into the playable route, then mark placement status on the duplicated trigger.

Use `Dead Air/Prepare Playable Blockout` to create or repair the disposable validation route under:

`DEAD_AIR/ENVIRONMENT/FINAL_WORLD/DEAD_AIR_PLAYABLE_BLOCKOUT`

The command does not delete designer-authored content. It creates missing named blockout objects and leaves existing blockout transforms in place.

## Road Pieces

Use `ROAD_PIECES` for modular examples:

- `DA_ROAD_STRAIGHT_SHORT`
- `DA_ROAD_STRAIGHT_LONG`
- `DA_ROAD_CURVE_LEFT`
- `DA_ROAD_CURVE_RIGHT`
- `DA_ROAD_FORK`
- `DA_ROAD_EXIT_RIGHT`
- `DA_ROAD_MERGE`

Legal driving corridor templates are also staged here:

- `DA_VALID_ROAD_ZONE`
- `DA_VALID_ROAD_DEPOT`
- `DA_VALID_ROAD_HIGHWAY`
- `DA_VALID_ROAD_FORK`

Each `DeadAirRoadPiece` has start/end anchors and Scene View direction gizmos.

Each `DeadAirValidRoadZone` is a box volume that can be duplicated, moved, scaled, and rotated over the authored route. Overlapping zones are valid. Construction-kit template zones are ignored by runtime until duplicated out of `DEAD_AIR_CONSTRUCTION_KIT`. Cover depot yards, depot exit roads, shoulders that should be safe, main highway segments, choice branches, Exit 17, and ending roads. If no runtime valid-road zones are placed, validation warns and the off-road controller does not fail the player in the un-authored scene.

## Choices

Use `DA_CHOICE_TEMPLATE` for a two-way fork and `DA_EXIT_CHOICE_TEMPLATE` for an exit/straight fork. Move the parent object as a group, then tune individual commit trigger volumes.

Sibling commits share one choice ID; `DeadAirStoryDirector` prevents a later sibling from overwriting the committed outcome.

## Story Triggers

Use `STORY_TRIGGERS` for dispatch, CB, GPS, environment, traffic, dashboard, generic story, and ending templates. Triggers fire only from physical collider entry by the player truck.

## Depot Start

Use `DEBUG_REFERENCE/DA_DEPOT_START_TEMPLATE` as the opening depot-start package. Duplicate it into the playable scene and move/rotate the parent as a group.

The template contains:

- `DeadAirStartMarker`
- `TRUCK_START_REFERENCE`
- `TRAILER_START_REFERENCE`
- `FORWARD_DIRECTION`
- `DA_START_000_INITIAL_TRIGGER`

The truck and trailer references drive `DeadAirStartRigController`, so designers do not need to manually calculate trailer coordinates every time the depot parking spot moves. `DA_START_000_INITIAL_TRIGGER` is for run initialization; place `DA_001` and the first spoken dispatcher beat later on the depot route when desired.

## Disposable Playable Blockout

`Dead Air/Prepare Playable Blockout` creates a clearly labeled, temporary 1-2 minute route for validation only:

- depot start with `DA_DEPOT_START_TEMPLATE`
- straight road out of the depot
- gentle curve
- straight approach to a simple fork
- left and right branch lanes
- merge
- temporary ending straight
- `VOID_FAILURE_TEST_AREA` beside the road, intentionally outside valid-road zones

The blockout uses `DeadAirRoadPiece` instances plus simple collider-backed cube surfaces. These are gameplay/blockout slabs, not final road art.

Runtime valid-road copies are placed under the blockout:

- `DA_RUNTIME_VALID_ROAD_DEPOT`
- `DA_RUNTIME_VALID_ROAD_HIGHWAY_00`
- `DA_RUNTIME_VALID_ROAD_HIGHWAY_CURVE`
- `DA_RUNTIME_VALID_ROAD_FORK_AND_BRANCHES`
- `DA_RUNTIME_VALID_ROAD_MERGE_TO_TEMP_END`

The construction-kit `DA_VALID_ROAD_*` templates remain ignored by runtime. The runtime copies are outside `DEAD_AIR_CONSTRUCTION_KIT`, so `DeadAirOffRoadFailureController` can use them during Play Mode.

Temporary gameplay validation pieces:

- `TEST_STORY_TRIGGER`: displays/logs `TEST STORY TRIGGER FIRED`
- `TEST_CHOICE_START`
- `TEST_LEFT_COMMIT`
- `TEST_RIGHT_COMMIT`
- `TEMP_END_TRIGGER`

These are disposable validation objects. Do not treat them as final narrative pacing, dialogue, endings, or map layout.

## Audio

Use `AUDIO_EVENTS` for dispatcher, CB, GPS, and multi-channel sequence templates. Each sequence step supports clip, subtitle, speaker, channel, delay before/after, volume, static before/after, CB squelch before/after, and collision behavior.

Missing clips fall back to subtitle-only validation.

## GPS

Use `GPS_EVENTS` to author normal, turn, exit, recalculating, no-signal, corruption, and restore states. GPS misinformation is allowed and does not create a second navigation authority.

## Dashboard

Use `DASHBOARD_EVENTS` for fake fuel, fake clock, speed glitch, warning lamp, and restore display states. These change displayed state only; they do not alter real truck physics or simulation state.

## Traffic

Use `TRAFFIC_EVENTS` for spawn, despawn, repeating car, follower, and headlights templates. Temporary traffic horror objects are owned by `DeadAirTrafficHorrorDirector` and cleaned up on restart.

## Gizmos

Use `Dead Air/Toggle Authoring Gizmos` to hide/show labels. Trigger labels show beat ID, display name, category, placement status, route-mile metadata, straight-line distance from `DeadAirStartMarker`, estimated time at 55 MPH, target pacing time, and delta.

`DeadAirRouteFlowGizmo` draws narrative order lines between beat triggers. It is an authoring visualization only.

## WebGL Build

Use `Dead Air/Build WebGL` to build `Assets/DeadAir/Scenes/DeadAir_Main.unity` to `Builds/DeadAir_WebGL`. The builder applies release-minded serialized defaults where those objects exist, including disabling the runtime debug overlay and verbose trigger logging for the build scene.
