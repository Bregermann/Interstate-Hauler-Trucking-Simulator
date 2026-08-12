# Prompt 007 - Complete Truck Controls

## Result

Prompt 007 establishes the project-owned semantic truck-control layer around the Prompt 006 18-speed transmission.

Installed vehicle physics baseline remains NWH Vehicle Physics 2 13.6.

The implementation does not read DirectInput, DIManager, Logitech SDK, HID, or raw G29 controls from gameplay truck-control code. Hardware remains behind Prompt 005 input sources and calibration. Gear changes remain owned by `Lws18SpeedTransmissionController`; Prompt 007 does not call NWH `ShiftInto`.

## Architecture

Runtime truck-control components:

- `LwsTruckControlController`
- `LwsTruckControlService`
- `LwsNwhTruckControlAdapter`
- `LwsKeyboardGamepadTruckInputSource`
- `LwsPlayerGestureController`
- `LwsTruckControlDebugPanel`

The validation spawner installs these components onto the spawned LWS player truck at runtime. The vendor NWH tractor prefab remains unchanged.

Input flow:

`keyboard/controller/wheel -> ILwsVehicleInputSource -> ILwsVehicleInputService -> LwsTruckControlController -> LWS/NWH adapters`

Transmission flow remains:

`physical shifter/range/splitter/clutch -> Prompt 005 input -> Prompt 006 Lws18SpeedTransmissionController -> LwsNwh18SpeedTransmissionAdapter -> NWH ShiftInto`

## Semantic Command Model

`LwsVehicleCommandFrame` now carries semantic controls for:

- ignition toggle, engine start, engine stop
- parking brake
- low beams, high beams, turn signals, hazards
- wipers
- horn and air horn intent
- cruise set/resume/cancel/increase/decrease
- engine brake and retarder level intents
- differential lock
- trailer attach/detach and trailer brake intent
- camera cycle and look reset
- `FlipOffDriver`
- interaction, pause, and menu navigation

Toggle and one-shot commands use `LwsMomentaryIntent.Pressed`; held commands use `Pressed` or `Held`.

## Input Ownership

Continuous driving control remains single-owner through `ILwsVehicleInputService`.

Keyboard/controller semantic commands may still be read as a development overlay while wheel owns steering, throttle, brake, and clutch. This lets keyboard controls trigger lights, horn, camera, and gesture without stealing driving authority from the wheel.

`LwsNwhVehicleInputProvider` now consumes truck-control state for NWH-native actions:

- parking brake state
- horn hold state
- light/signal/hazard one-shot pulses
- LWS cruise fallback throttle/brake output

It suppresses legacy direct NWH start/stop, cruise-toggle, and trailer attach/detach commands while a truck-control controller is active.

## Exact NWH APIs Discovered

NWH Vehicle Physics 2 13.6 exposes the following useful controls:

- `VehicleInputProviderBase.EngineStartStop()`
- `VehicleInputProviderBase.Handbrake()`
- `VehicleInputProviderBase.LowBeamLights()`
- `VehicleInputProviderBase.HighBeamLights()`
- `VehicleInputProviderBase.LeftBlinker()`
- `VehicleInputProviderBase.RightBlinker()`
- `VehicleInputProviderBase.HazardLights()`
- `VehicleInputProviderBase.Horn()`
- `VehicleInputProviderBase.CruiseControl()`
- `VehicleInputProviderBase.TrailerAttachDetach()`
- `EngineComponent.ignition`
- `EngineComponent.StartEngine()`
- `EngineComponent.StopEngine()`
- `EngineComponent.StartStopEngine()`
- `EngineComponent.IsRunning`
- `EngineComponent.IsStalled`
- `LightsMananger.lowBeamLights`
- `LightsMananger.highBeamLights`
- `LightsMananger.leftBlinkers`
- `LightsMananger.rightBlinkers`
- `VehicleLight.On`
- `CruiseControlModule.cruiseControlActive`
- `CruiseControlModule.targetSpeed`
- `DifferentialComponent.slipTorque`
- `TrailerHitchModuleWrapper`
- `CameraChanger.NextCamera()`

No selected-truck NWH support was found for wiper animation, retarder, Jake/engine brake, separate air horn, or independent manual trailer brake.

## Ignition

LWS exposes:

- `LwsIgnitionState.Off`
- `LwsIgnitionState.Electrical`
- `LwsIgnitionState.EngineRunning`

The selected NWH engine supports `ignition`, `StartEngine()`, and `StopEngine()`. No deeper electrical accessory simulation was added.

## Brakes

Service brake remains continuous input from Prompt 004/005.

Parking brake is now a semantic toggle through `LwsTruckControlController`; NWH receives it as `VehicleInputProviderBase.Handbrake()`.

Independent trailer brake is represented semantically, but the selected NWH trailer integration does not expose a confirmed trailer-only brake input. It remains deferred.

## Lights

Low beams, high beams, left/right signals, and hazards are routed as one-shot pulses through the LWS NWH input provider into NWH's native light manager.

Hazards clear the LWS directional signal state. Directional signal input clears hazards before activating the signal.

## Wipers

LWS provides semantic states:

- Off
- Intermittent
- Low
- High

The selected NWH semi did not expose usable wiper animation or control APIs. Prompt 008 or later cab/weather work should bind this state to final art.

## Horn And Air Horn

NWH exposes one horn input and a horn sound component. LWS exposes both `horn` and `airHorn` semantic intents; both feed the same NWH horn path for the selected truck. Separate air horn audio is deferred.

## Engine Brake And Retarder

NWH search did not reveal a selected-truck Jake/engine brake or retarder API. LWS represents the capability model and state fields, but the starter NWH semi marks both unsupported.

## Differential Lock

No explicit NWH differential-lock toggle was found. NWH does expose `DifferentialComponent.slipTorque`. `LwsNwhTruckControlAdapter` uses a project-owned, reversible slip-torque override as the current differential-lock approximation.

This is functional validation plumbing, not final heavy-truck driveline tuning.

## Cruise Control

NWH includes `CruiseControlModule`, but the selected semi prefab does not currently include a confirmed cruise wrapper. LWS therefore supports two paths:

- native NWH module if present
- LWS fallback that feeds normalized throttle/brake assist through `LwsNwhVehicleInputProvider`

Cruise cancels on service brake, clutch, or explicit cancel.

## Trailer Controls

`TrailerAttachDetach` is now a semantic one-shot command routed to `LwsNwhTrailerCouplingAdapter`. No second trailer system was added.

Manual trailer brake is represented but not wired because no independent NWH trailer-brake input was confirmed.

## Camera Controls

`CameraCycle` calls `CameraChanger.NextCamera()` where available. `LookReset` is a semantic seam; the selected NWH camera stack did not expose a stable reset-look API.

## Flip Off Driver

`FlipOffDriver` is a real semantic truck/player action.

Implementation:

- `LwsTruckGestureType.FlipOffDriver`
- `LwsPlayerGestureController.RequestFlipOff`
- duration and cooldown
- no-target safe behavior
- start/end events for future animation
- `LwsDriverGestureEvent` for future NPC reactions

Targeting is intentionally lightweight. The gesture controller queries nearby `LwsVehicleIdentity` instances only when the gesture is triggered and chooses the nearest eligible non-player vehicle within radius and rough forward attention.

No traffic behavior or brake-check reaction is implemented in Prompt 007.

## Truck Capability Model

`LwsTruckDefinition` now owns `LwsTruckControlCapabilities`.

The starter NWH semi marks the following as supported:

- ignition/start/stop
- parking brake
- headlights/high beams
- turn signals/hazards
- horn
- differential lock approximation
- cruise control through LWS fallback/native-if-present
- trailer attach/detach
- camera cycle/look reset seam
- FlipOffDriver

Unsupported or partial:

- wiper visuals
- separate air horn
- engine/Jake brake
- retarder
- independent trailer brake

## Dashboard State Seam

`LwsTruckControlState` exposes read-only dashboard-facing state for:

- ignition/engine
- parking/service/trailer brake
- headlights/high beams
- signals/hazards
- wipers
- horn/air horn
- engine brake/retarder
- differential lock
- cruise enabled/target/output
- trailer attached
- camera requests
- last gesture/target/cooldown
- Prompt 006 transmission display state

Prompt 008 should consume this state rather than reading NWH or input devices directly.

## Keyboard Controls

Development defaults:

- `I`: ignition toggle
- `E`: engine start
- `Shift+E`: engine stop
- `P`: parking brake
- `L`: headlights
- `K`: high beams
- `Z` / `X`: left/right signals
- `J`: hazards
- `V`: wipers
- `H`: horn
- `B`: air horn
- `M`: engine brake toggle
- `PageUp/PageDown`: engine brake level
- `Home/End`: retarder level
- `O`: differential lock
- `C`: cruise toggle
- `R`: cruise set
- `Shift+R`: cruise resume
- `Backspace`: cruise cancel
- `-` / `=`: cruise adjust
- `T`: trailer attach/detach
- `Space`: trailer brake intent
- `Tab`: camera cycle
- Backquote: look reset
- `F`: FlipOffDriver
- `Enter`: interact/submit
- `Esc`: cancel/pause

## Controller Controls

Development defaults are present for core controls and navigation, but final controller ergonomics are deferred. Prompt 007 intentionally does not build a radial menu.

## Wheel Controls

Prompt 005 wheel calibration now has logical binding slots for Prompt 007 actions, including air horn, signals, lights, differential lock, trailer brake, look reset, and FlipOffDriver.

No raw G29 button IDs are hardcoded in truck gameplay.

## Tests

Added EditMode tests:

- command-frame edge combining
- toggle debounce
- signal/hazard transitions
- wiper state progression
- unsupported engine brake/retarder behavior
- LWS cruise fallback output
- duplicate truck-control service rejection
- FlipOffDriver no-target cooldown

Added PlayMode tests:

- controller/service initialization
- NWH provider consumes truck-control pulses/state
- cruise fallback reaches NWH provider and cancels on brake
- FlipOffDriver raises gesture event without a target

Automated tests require the normal Unity Editor Test Runner for authoritative results. A generated `.csproj` `dotnet build` sanity check is not authoritative for this project because those project files are Unity-generated, stale until Editor regeneration, and do not mirror the normal Editor assembly graph.

## Known Limitations

- Normal Unity Editor validation is still required for live truck driving, lights, cruise, and G29 bindings.
- Wipers have semantic state only.
- Air horn shares the NWH horn path.
- Engine/Jake brake and retarder are capability gaps for the selected NWH semi.
- Independent trailer brake is not available from the discovered NWH trailer APIs.
- Differential lock uses a reversible NWH slip-torque override, not a dedicated NWH lock API.
- Look reset is a seam until the cab camera stack exposes a stable reset hook.

## Prompt 008 Recommendations

Prompt 008 should bind dashboard indicators to `ILwsTruckControlService.ActiveState` and `LwsTruckControlController.CurrentState`.

Dashboard/mirror work should not read wheel hardware, keyboard keys, or raw NWH input providers. Use the semantic truck state and Prompt 006 transmission display state.
