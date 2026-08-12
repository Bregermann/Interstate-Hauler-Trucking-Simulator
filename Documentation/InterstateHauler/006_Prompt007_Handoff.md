# Prompt 007 Handoff - Complete Truck Controls

Prompt 006 provides the 18-speed transmission interpretation layer. Prompt 007 should build the broader truck-control surface around it.

## Current Transmission Entry Points

Player truck prefab:

`Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`

TruckValidation scene:

`Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`

18-speed definition:

`Assets/LWS/InterstateHauler/Vehicles/Transmission/Data/IH_18SpeedTransmission_G29_EatonDevelopment.asset`

Controller:

`Lws18SpeedTransmissionController`

NWH adapter:

`LwsNwh18SpeedTransmissionAdapter`

## Inputs To Use

Prompt 007 should continue consuming project-owned LWS input abstractions:

- `LwsTruckShifterGate`
- `LwsTruckRange`
- `LwsTruckSplitter`
- `LwsVehicleContinuousInput.clutch`
- `LwsVehicleCommandFrame`

Do not read DirectInput, HID, Logitech SDK, or DIManager from truck-control gameplay code.

## Transmission State For UI/Cab

Use:

- `Lws18SpeedTransmissionController.CurrentState`
- `Lws18SpeedTransmissionController.DisplayState`
- `Lws18SpeedTransmissionController.AbuseDetected`

Important state fields:

- `logicalGear`
- `displayLabel`
- `logicalRatioIndex`
- `requestedRange`
- `engagedRange`
- `requestedSplitter`
- `engagedSplitter`
- `shiftState`
- `lastRejectionReason`
- `lastAbuseSeverity`
- `requiresShifterSynchronization`

## NWH API Boundary

Prompt 006 uses:

`VehicleController.powertrain.transmission.ShiftInto(int targetGear, bool instant = false)`

Runtime NWH gear indexes:

- `-1`: reverse
- `0`: neutral
- `1..18`: forward logical ratios

Prompt 007 should not bypass the LWS transmission controller for gear changes.

## Deferred Controls For Prompt 007

- ignition/start/stop
- parking brake
- trailer attach/detach binding
- lights/high beams/signals/hazards
- horn
- wipers
- engine brake/retarder inputs if supported by NWH or future LWS adapter
- camera cycle/look focus
- pause/menu intents
- control conflict rules between keyboard/controller/wheel

## Known Risks

- The current ratio set is a development seed, not a final verified Eaton ratio table.
- Hardcore float-shift behavior needs real NWH driving validation.
- NWH clutch semantics differ from LWS pedal semantics; Prompt 006 handles shift validation in LWS and bypasses the stock NWH shift gate on spawned instances.
- Physical G29 shifter verification depends on Prompt 005 hardware setup.

## Recommendation

Prompt 007 should treat `Lws18SpeedTransmissionController` as the single authority for transmission commands and build all remaining truck controls around the existing `ILwsVehicleInputService` ownership model.
