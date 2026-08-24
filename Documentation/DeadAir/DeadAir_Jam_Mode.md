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

`DeadAirSceneBootstrapper` repairs missing runtime systems on Play and can create the unplaced beat layout. The editor menu `Dead Air/Build Or Refresh Main Scene` performs the same setup in the Editor.

`DeadAir_Main` also carries a serialized reference to `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab` so the jam scene can spawn the project-owned player truck under `START` without duplicating NWH/vendor prefabs.

## Runtime Architecture

Dead Air systems:

- `DeadAirGameManager`: run lifecycle, restart, ending handoff, LWS bootstrap assurance.
- `DeadAirStoryDirector`: beat trigger log, choice commits, reset.
- `DeadAirVehicleAdapter`: reads LWS/NWH vehicle telemetry and sets development Basic Automatic mode.
- `DeadAirBasicAutomaticInputSource`: WebGL-safe keyboard/gamepad input source for W/S/A/D driving, horn, pause, camera, interact, and flip-off intent.
- `DeadAirAudioDirector`: radio/audio/subtitle queues with missing-clip-safe subtitle fallback.
- `DeadAirGPSDirector`: GPS presentation state, misinformation, recalculating/signal-lost/corrupt states.
- `DeadAirHud`: speed, subtitle, debug, GPS text, and shared semantic road/route map presentation.
- `DeadAirAnomalyDirector`: reusable horror hooks for weather, fog, time, GPS, dashboard/audio/traffic events.
- `DeadAirEndingDirector`: four ending IDs, fade/title/body/restart flow.

## Basic Automatic

Dead Air defaults the player truck into LWS automatic transmission mode by calling:

`Lws18SpeedTransmissionController.TrySetDevelopmentAutomaticTestMode(true, out message)`

The mode does not bypass `Lws18SpeedTransmissionController`, does not call NWH `ShiftInto` directly, and leaves 18-speed manual intact for the base game.

Default controls:

- W / Up: accelerate forward
- S / Down: brake or reverse when stopped
- A / D: steer
- H: horn / air horn intent
- Tab: camera cycle
- F: Flip Off Driver semantic intent
- Enter: interact
- Esc: pause/cancel

Controller:

- Left stick: steer
- Right trigger: throttle
- Left trigger: brake
- South button: horn / interact
- Select: camera cycle
- Start: pause

## GPS Presentation

Dead Air uses one navigation authority from LWS and one Dead Air GPS presentation state for misinformation. The HUD contains a heading-up `LwsSemanticGpsMapGraphic` instance so roads, active route, destination marker, and the player marker are drawn from the same semantic road graph and route data used by the base game.

## Story Triggers

`DeadAirTriggerZone` uses a trigger `BoxCollider`, beat metadata, editor gizmos, one-shot/cooldown behavior, and reports to `DeadAirStoryDirector`.

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

The unplaced layout exists in `DeadAir_Main` under `DEAD_AIR_UNPLACED_GAMEPLAY` and includes `DA_START_000`, `DA_001` through `DA_018`, `CHOICE_01`, `CHOICE_02`, `CHOICE_03_EXIT17`, and `ENDING_TRIGGER`.

## Choices

`DeadAirChoiceStartTrigger` marks a choice area. `DeadAirChoiceCommitTrigger` commits an outcome by choice ID. Once a choice ID has a committed outcome, a sibling outcome cannot overwrite it.

## Endings

Ending IDs:

- `Exit17`
- `TrustDispatch`
- `Lost`
- `TrustNoOne`

`TrustNoOne` is the secret positive ending slot. Ending configs are serialized on `DeadAirEndingDirector` and default configs are created if none are assigned.

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
- unplaced critical beat warnings
- WebGL-risk tokens inside `Assets/DeadAir`

## Deferred Polish

- final route placement
- final VO/audio clips
- final GPS art pass
- final cockpit mounting into a specific truck cab
- full WebGL build verification through Unity Editor
- ending-specific authored cinematics
