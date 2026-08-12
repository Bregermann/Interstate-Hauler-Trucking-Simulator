# Interstate Hauler - Logitech G29 Input

Prompt: 005 - Logitech G29 / Pedals / H-Shifter / Force Feedback
Windows wheel backend: Unity-DirectInput
Package: `com.directinput.unity`
Installed package version: `1.1.1`
Locked commit/hash: `b55765e17566bf877951e1437de1413d0c5ad3ec`
Reference hardware: Logitech G29 wheel, G29 pedals, clutch pedal, Logitech Driving Force Shifter

## Backend Decision

The previous LogitechGSDK and NWH `SteeringWheelInput` approach is cancelled. Interstate: Hauler no longer requires Logitech's proprietary Steering Wheel SDK for Prompt 005.

Windows PC wheel input and force feedback now use Unity-DirectInput:

`https://github.com/imDanoush/Unity-DirectInput.git`

The package is installed through Unity Package Manager in `Packages/manifest.json` and locked in `Packages/packages-lock.json`. Runtime integration stays behind LWS-owned abstractions so gameplay does not depend on DirectInput directly.

## NWH Integration Reference

The installed Unity-DirectInput package includes the expected NWH sample:

`Library/PackageCache/com.directinput.unity@b55765e17566/Samples~/nwhvp/NWHVehiclePhysics2FFB.unitypackage`

The sample's `SteeringWheelInputProvider` is for NWH Vehicle Physics 2 v13 and matches our NWH 13.6 baseline closely enough to use as the API reference. It reads Unity-DirectInput/Input System devices, maps DirectInput controls, calculates steering force from NWH front-wheel state, and sends FFB through `DirectInputManager.DIManager`.

Key package APIs:

- `DIManager.Initialize()`
- `DIManager.EnumerateDevices()`
- `DIManager.Devices`
- `DIManager.Attach(string guidInstance)`
- `DIManager.Destroy(string guidInstance)`
- `DIManager.GetDeviceState(string guidInstance)`
- `DIManager.GetDeviceStateRaw(string guidInstance)`
- `DIManager.GetDeviceCapabilities(string guidInstance)`
- `DIManager.GetDeviceFFBCapabilities(string guidInstance)`
- `DIManager.EnableFFBEffect(string guidInstance, FFBEffects effectType)`
- `DIManager.UpdateConstantForceSimple(string guidInstance, int magnitude)`
- `DIManager.UpdateDamperSimple(string guidInstance, int magnitude)`
- `DIManager.UpdateFrictionSimple(string guidInstance, int magnitude)`
- `DIManager.StopAllFFBEffects(string guidInstance)`

## LWS Architecture

Input flow:

Physical DirectInput wheel, pedals, and shifter -> Unity-DirectInput / Unity Input System -> `LwsWheelInputSource` -> `ILwsVehicleInputService` -> `LwsNwhVehicleInputProvider` -> NWH `VehicleController.input`.

Force feedback flow:

NWH vehicle telemetry -> `LwsDirectInputForceFeedbackCoordinator` -> `ILwsForceFeedbackService` -> Unity-DirectInput / `DIManager` -> physical wheel.

The LWS runtime does not expose `DIManager` to gameplay systems. The FFB service uses reflection so non-Windows or future console implementations can replace the backend without rewriting vehicle gameplay.

## G29 Calibration

The G29 remains the mandatory reference wheel. The default calibration asset is:

`Assets/LWS/InterstateHauler/Input/Data/IH_DefaultG29Calibration.asset`

It records:

- steering axis
- throttle axis
- brake axis
- clutch axis
- physical gates 1 through 6
- reverse
- range toggle candidate
- splitter toggle candidate
- wheel buttons
- D-pad/menu intents
- FFB settings

No raw button, axis, VID/PID, or shifter gate mapping is assumed. The calibration panel records the actual Unity-DirectInput/Input System control paths when the user moves the physical hardware.

## Input Ownership

`LwsWheelInputBootstrap` gives the wheel a single driving-input authority. When wheel ownership is active it disables other active NWH `VehicleInputProviderBase` instances except the LWS NWH bridge, preventing stock NWH input and LWS wheel input from double-feeding steering, throttle, brake, clutch, or shifting.

Keyboard/controller fallback can be restored when wheel ownership is released or the wheel disconnects.

## Force Feedback

The project-owned FFB service is:

`Assets/LWS/InterstateHauler/Input/Wheels/LwsForceFeedbackService.cs`

The TruckValidation coordinator is:

`Assets/LWS/InterstateHauler/Input/Wheels/LwsDirectInputForceFeedbackCoordinator.cs`

Current supported effect calls:

- alignment/self-centering: DirectInput constant force
- damping: DirectInput damper condition force
- road/base tire feel: DirectInput friction condition force
- impact: safely folded into the alignment/constant force channel for now

The service clamps normalized LWS force values to `[-1, 1]` and converts them to DirectInput's `[-10000, 10000]` range. It stops effects on disable, shutdown, disconnect, and application quit.

The Unity-DirectInput NWH sample contains deeper self-aligning torque logic using `WheelUAPI`, front-wheel lateral slip, wheel load, caster, steering ratio, and pneumatic trail estimation. That sample is the tuning reference for a later realism pass; Prompt 005 only establishes the safe backend and integration path.

## Device Support

Unity-DirectInput documents Logitech G29 / G920 as plugin-verified hardware. Interstate: Hauler still requires local physical verification with the actual G29, pedals, clutch, and Driving Force Shifter before full hardware PASS.

The architecture is DirectInput-generic and can support other compatible wheel ecosystems through new LWS profiles and calibration, without changing player vehicle gameplay.

## Cancelled Path

Do not use:

- LogitechGSDK
- Logitech proprietary Steering Wheel SDK
- NWH `SteeringWheelInput` as the selected Prompt 005 backend

If `Assets/NWH/Vehicle Physics 2/_OptionalPackages/Input/SteeringWheelInput/` is present in the working tree, treat it as cancelled/uncommitted Prompt 005 experiment work unless the user explicitly chooses to remove it.

## Validation

Automated tests cover normalization, calibration serialization, shifter gate representation, duplicate wheel ownership rejection, disconnect neutralization, and FFB clamping/safe failure.

Physical G29 validation still required:

- G29 detected through Unity-DirectInput
- steering center/full left/full right
- accelerator/brake/clutch 0 to 100 percent
- gates 1 through 6
- reverse
- neutral
- buttons and D-pad
- range and splitter binding
- trailer control binding
- FFB enable/strength/disable
- disconnect and reconnect

## Prompt 006 Recommendations

Prompt 006 should consume only LWS transmission intent:

- `LwsTruckShifterGate`
- `LwsTruckRange`
- `LwsTruckSplitter`
- clutch position

Prompt 006 should not read DirectInput or DIManager directly. It should map the calibrated physical gate/range/splitter state to the final 18-speed truck transmission and continue to use NWH `ShiftInto(int targetGear, bool instant = false)` as the physical drivetrain authority.
