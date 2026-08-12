# Prompt 006 Handoff - 18-Speed Range/Splitter Transmission

Expected Prompt 006: final 18-speed range/splitter transmission mapping. Do not read DirectInput directly in Prompt 006.

## Backend State

Windows wheel backend: Unity-DirectInput
Package: `com.directinput.unity`
Version: `1.1.1`
Locked hash: `b55765e17566bf877951e1437de1413d0c5ad3ec`
FFB backend: Unity-DirectInput / `DirectInputManager.DIManager`

The cancelled LogitechGSDK / NWH `SteeringWheelInput` path is not the selected backend.

## Physical Input Representation

Prompt 005 exposes the physical shifter through LWS as:

- `LwsTruckShifterGate.Neutral`
- `LwsTruckShifterGate.Gate1`
- `LwsTruckShifterGate.Gate2`
- `LwsTruckShifterGate.Gate3`
- `LwsTruckShifterGate.Gate4`
- `LwsTruckShifterGate.Gate5`
- `LwsTruckShifterGate.Gate6`
- `LwsTruckShifterGate.Reverse`

Range and splitter are exposed as:

- `LwsTruckRange.Low`
- `LwsTruckRange.High`
- `LwsTruckSplitter.Low`
- `LwsTruckSplitter.High`

Clutch is exposed in `LwsVehicleContinuousInput.clutch` with LWS convention:

- released: `0`
- fully pressed: `1`

Actual G29 DirectInput/Input System control paths are stored in `LwsWheelCalibrationProfile` after physical calibration. They are intentionally unbound until the user calibrates the wheel.

## Temporary Prompt 005 Mapping

Prompt 005 validation-only mapping:

- Reverse -> NWH `ShiftInto(-1)`
- Neutral -> NWH `ShiftInto(0)`
- Gate 1 -> NWH `ShiftInto(1)`
- Gate 2 -> NWH `ShiftInto(2)`
- Gate 3 -> NWH `ShiftInto(3)`
- Gate 4 -> NWH `ShiftInto(4)`
- Gate 5 -> NWH `ShiftInto(5)`
- Gate 6 -> NWH `ShiftInto(6)`

This is not the production trucking transmission.

## NWH APIs

Known direct shift API:

```csharp
VehicleController.powertrain.transmission.ShiftInto(int targetGear, bool instant = false);
```

Known read state:

```csharp
VehicleController.powertrain.transmission.Gear
VehicleController.powertrain.transmission.GearName
VehicleController.powertrain.clutch.clutchInput
VehicleController.input.Clutch
```

Prompt 006 should continue to let NWH own drivetrain state and only translate LWS physical gate/range/splitter/clutch intent into NWH-compatible shift commands.

## Unity-DirectInput FFB Reference

Prompt 005 installed/uses Unity-DirectInput and inspected:

`Samples~/nwhvp/NWHVehiclePhysics2FFB.unitypackage`

The sample's NWH v13 provider calculates self-aligning force from front wheels using `WheelUAPI`, caster, lateral slip, wheel load, steering ratio, and pneumatic trail estimates, then sends forces through DIManager.

LWS Prompt 005 coordinator currently sends safe baseline alignment/damping/road values through:

- `ILwsForceFeedbackService.SetForces(alignment, damping, road, impact)`
- `DIManager.UpdateConstantForceSimple`
- `DIManager.UpdateDamperSimple`
- `DIManager.UpdateFrictionSimple`
- `DIManager.StopAllFFBEffects`

Prompt 006 should not change FFB architecture unless transmission behavior reveals a real clutch/gear feedback need.

## Save-State Shape

Prompt 006 should persist gameplay transmission state, not raw hardware state:

- truck definition ID
- current NWH gear / neutral / reverse
- physical gate
- range state
- splitter state
- clutch position if needed for restart consistency
- pending shift intent if needed

Do not serialize DirectInput device GUIDs into career saves. Wheel calibration remains machine/global settings.

## Limitations

- Actual G29 control paths require physical calibration.
- Full 18-speed range/splitter math is not implemented in Prompt 005.
- FFB physical verification is required with the actual G29.
- Unity-DirectInput FFB is Windows-focused; console input backends must use the same LWS interfaces with platform-specific implementations.

## Recommended Prompt 006 Architecture

Create an LWS transmission mapper that consumes:

- `LwsTruckShifterGate`
- `LwsTruckRange`
- `LwsTruckSplitter`
- clutch position

and outputs:

- intended logical 18-speed gear
- neutral/reverse requests
- NWH `ShiftInto` command timing
- validation/debug state for the cab/dashboard

Keep DirectInput, DIManager, and hardware profile details out of Prompt 006 gameplay logic.
