# Prompt 006 - Eaton-Style 18-Speed Transmission

## Result

Prompt 006 establishes the first LWS-owned Eaton-style 18-speed range/splitter transmission layer for the NWH semi baseline.

NWH remains authoritative for vehicle physics, drivetrain simulation, clutch physics, and `TransmissionComponent.ShiftInto(...)`. LWS now owns the interpretation of the physical H-pattern shifter, range switch, splitter switch, clutch validation, logical gear state, shift rejection, abuse events, save payload shape, and debug display.

The official Eaton RT-18 page confirms the reference transmission class as an 18-speed heavy-duty transmission. It does not expose a complete ratio table in the accessible page text, so the current project asset uses a clearly marked development ratio set until production ratio tuning is verified from licensed/vendor documentation or design data.

Reference: https://www.eaton.com/us/en-us/catalog/transmissions/rt-18-transmission.html

## Runtime Assets

Primary definition:

`Assets/LWS/InterstateHauler/Vehicles/Transmission/Data/IH_18SpeedTransmission_G29_EatonDevelopment.asset`

Runtime controller:

`Assets/LWS/InterstateHauler/Vehicles/Transmission/Lws18SpeedTransmissionController.cs`

NWH adapter:

`Assets/LWS/InterstateHauler/Vehicles/Transmission/NWH/LwsNwh18SpeedTransmissionAdapter.cs`

Debug panel:

`Assets/LWS/InterstateHauler/Vehicles/Transmission/Debug/Lws18SpeedTransmissionDebugPanel.cs`

TruckValidation scene:

`Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`

## Input Boundary

Prompt 006 does not read DirectInput, DIManager, HID, Logitech SDK, or steering-wheel plugin APIs.

The transmission consumes only project-owned LWS input state:

- `LwsTruckShifterGate`
- `LwsTruckRange`
- `LwsTruckSplitter`
- LWS clutch value, where `0` means released and `1` means fully depressed

Prompt 005 still owns physical wheel/shifter/pedal discovery and calibration.

## Physical Gate Layout

The development G29 shifter layout uses the six physical gates as follows:

- Gate 1: intentionally unused/reserved
- Gate 2: low/crawler position
- Gate 3: main position 1
- Gate 4: main position 2
- Gate 5: main position 3
- Gate 6: main position 4
- Reverse: reverse
- Neutral: neutral

This matches the future 18-speed model where physical gate plus range plus splitter equals one logical transmission gear.

## Logical Gear Model

The LWS logical model is:

- Neutral
- Reverse 1
- Low Low
- Low High
- 1 Low through 8 High

That produces 18 forward logical ratios:

- Low/crawler: 2 split ratios
- Low range: gears 1-4, each low/high split
- High range: gears 5-8, each low/high split

## Range Behavior

Range state is split into requested and engaged state:

- `requestedRange` follows the physical range switch.
- `engagedRange` changes only after the shifter enters neutral.

If the driver preselects high range while still in low range, LWS reports `WaitingForNeutral` and continues using the currently engaged range until neutral is reached.

## Splitter Behavior

Splitter state is also split into requested and engaged state:

- In assisted mode, splitter changes can engage immediately.
- In hardcore mode, splitter changes require a depressed clutch or an RPM-synchronized float-shift condition.

Prompt 006 does not implement final air-system timing, switch delay, or hardware detents.

## Clutch Model

LWS uses the Prompt 005 convention:

- `0` = clutch released
- `1` = clutch fully depressed

The development threshold is `0.75`.

NWH internally uses its own clutch component semantics. To avoid NWH's stock shift gate fighting the LWS range/splitter validation, the runtime NWH adapter sets `TransmissionComponent.clutchInputShiftThreshold` above the normal input range on the spawned instance. This is a runtime instance configuration only; vendor prefabs and vendor source are not modified.

## NWH Gear Configuration

At runtime, `LwsNwh18SpeedTransmissionAdapter` configures the spawned NWH `TransmissionComponent`:

- 1 reverse ratio
- neutral
- 18 forward ratios
- manual NWH transmission mode
- direct `ShiftInto(targetGear, instant: true)` calls from LWS after validation

The configured NWH forward gear indexes are `1` through `18`. Reverse uses NWH gear index `-1`.

## Shift Assist Modes

`AssistedManual`:

- accepts clutchless logical shifts for validation usability
- blocks severe/catastrophic predicted overspeed downshifts
- still records abuse events

`HardcoreManual`:

- requires clutch above threshold or RPM synchronization for shifts
- emits grind/abuse state when clutchless shifts miss synchronization
- preserves the same NWH drivetrain boundary

Prompt 006 defaults to assisted mode so the truck remains testable before Prompt 007 controls and Prompt 008 cab feedback mature.

## Development Automatic Transmission Toggle

The validation debug panel added by `LwsPlayerTruckSpawner` exposes a development-only `ENABLE TEST AUTOMATIC` button under `UNITY_EDITOR || DEVELOPMENT_BUILD`. The button calls `Lws18SpeedTransmissionController.TrySetDevelopmentAutomaticTestMode(...)`, which switches the existing `LwsTransmissionMode` between `Automatic` and `Truck18Speed`.

Automatic test mode stays inside `Lws18SpeedTransmissionController`; the UI does not call NWH directly. While active, the controller ignores physical H-pattern gate/range/splitter gear-selection authority, chooses validation forward gears from the active 18-speed definition, and still applies accepted shifts through `LwsNwh18SpeedTransmissionAdapter.TryShiftInto(...)`.

Switching modes requires the truck to be stopped, forces Neutral through the controller, and returning to `Truck18Speed` marks shifter synchronization as required so the current physical shifter position is not reinterpreted dangerously. This is temporary validation tooling for WASD driving in `TruckValidation.unity` and `InterstateCorridorValidation.unity`; it does not persist to career save data and is not the final player-facing automatic transmission option.

## Abuse And Damage Hooks

Prompt 006 emits project-owned abuse events but does not implement final damage.

Current abuse causes:

- excessive RPM mismatch
- clutchless poor synchronization
- high-speed low-gear selection
- reverse while moving forward
- extreme downshift
- unavailable gear

Current severity:

- none
- minor
- moderate
- severe
- catastrophic risk

Future wear/damage systems can subscribe to the controller's `AbuseDetected` event or consume the save/debug state.

## Save State

The transmission save participant ID is:

`vehicle.transmission.player`

The payload stores logical state only:

- mode
- logical gear
- logical ratio index
- NWH gear index
- physical gate
- requested/engaged range
- requested/engaged splitter
- shift state
- restore synchronization flag

It does not serialize DirectInput, Logitech, HID, or arbitrary NWH MonoBehaviour graphs.

On restore, the controller requests the saved NWH gear and sets `requiresShifterSynchronization` so the current physical shifter must match before normal shifting resumes.

## Validator

`Interstate Hauler / Validate Project` now checks:

- 18-speed definition asset exists and validates
- 18 unique positive forward ratios exist
- reverse mapping resolves to LWS reverse and NWH gear index `-1`
- TruckValidation references the 18-speed definition
- LWS spawner installs the 18-speed controller and NWH adapter
- controller consumes LWS input only
- controller avoids Prompt 005 hardware APIs
- NWH adapter configures the runtime gear list and uses `ShiftInto`
- save participant shape exists
- Prompt 005 validation shifter mapping is disabled by default

## Deferred Work

- Final licensed/verified Eaton ratio table or design-approved fictional ratio table
- Final gear whine, shift sounds, missed-shift audio, and mechanical feel
- Real drivetrain wear/damage economics
- Final cab dashboard integration
- Air-system behavior and range/splitter timing polish
- Production tutorial and control hints
- Prompt 007 complete truck controls
