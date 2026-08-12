# Prompt 005 - G29 Control Map

Backend: Unity-DirectInput / Unity Input System
Reference hardware: Logitech G29 + pedals + Driving Force Shifter
Mapping policy: calibration records actual control paths; no hardcoded G29 raw IDs.

| Hardware Control | Raw Input | Logical LWS Input | NWH Destination | Calibration | Default Binding | Notes |
|---|---|---|---|---|---|---|
| Steering wheel | Unity-DirectInput/Input System axis | `Steering` | `VehicleController.input.Steering` | center, min, max, deadzone, saturation, inversion, rotation | Unbound | Expected normalized range is -1 to +1. |
| Accelerator pedal | Unity-DirectInput/Input System axis | `Throttle` | `VehicleController.input.Throttle` | released, pressed, deadzone, saturation, inversion | Unbound | LWS convention is released 0, pressed 1. |
| Brake pedal | Unity-DirectInput/Input System axis | `Brake` | `VehicleController.input.Brakes` | released, pressed, deadzone, saturation, inversion | Unbound | Independent logical pedal, no combined-axis assumption. |
| Clutch pedal | Unity-DirectInput/Input System axis | `Clutch` | `VehicleController.input.Clutch` | released, pressed, deadzone, saturation, inversion | Unbound | Prompt 006 consumes this for 18-speed shifting. |
| Shifter gate 1 | Unity-DirectInput/Input System button/axis | `ShifterGate1` | validation-only `ShiftInto(1)` | bind during shifter calibration | Unbound | Physical gate only, not final truck gear. |
| Shifter gate 2 | Unity-DirectInput/Input System button/axis | `ShifterGate2` | validation-only `ShiftInto(2)` | bind during shifter calibration | Unbound | Physical gate only. |
| Shifter gate 3 | Unity-DirectInput/Input System button/axis | `ShifterGate3` | validation-only `ShiftInto(3)` | bind during shifter calibration | Unbound | Physical gate only. |
| Shifter gate 4 | Unity-DirectInput/Input System button/axis | `ShifterGate4` | validation-only `ShiftInto(4)` | bind during shifter calibration | Unbound | Physical gate only. |
| Shifter gate 5 | Unity-DirectInput/Input System button/axis | `ShifterGate5` | validation-only `ShiftInto(5)` | bind during shifter calibration | Unbound | Physical gate only. |
| Shifter gate 6 | Unity-DirectInput/Input System button/axis | `ShifterGate6` | validation-only `ShiftInto(6)` | bind during shifter calibration | Unbound | Physical gate only. |
| Reverse | Unity-DirectInput/Input System button/axis | `ShifterReverse` | validation-only `ShiftInto(-1)` | bind during shifter calibration | Unbound | Actual reverse encoding must be discovered physically. |
| Neutral | no gate active | `Neutral` | validation-only `ShiftInto(0)` | inferred from no gate binding active | Inferred | Prompt 006 keeps neutral as an explicit state. |
| Range button | Unity-DirectInput/Input System button | `RangeToggle` | none in Prompt 005 | bind during button calibration | Unbound | Prompt 006 maps range to logical gear. |
| Splitter button | Unity-DirectInput/Input System button | `SplitterToggle` | none in Prompt 005 | bind during button calibration | Unbound | Prompt 006 maps splitter to logical gear. |
| Trailer control | Unity-DirectInput/Input System button | `TrailerAttachDetach` | `VehicleController.input.TrailerAttachDetach` through LWS NWH bridge | bind optional button | Unbound | Validation only until final controls prompt. |
| Parking brake | Unity-DirectInput/Input System button/axis | `ParkingBrake` | `VehicleController.input.Handbrake` | bind optional button/axis | Unbound | Final control map deferred. |
| Horn | Unity-DirectInput/Input System button | `Horn` | future truck command | bind optional button | Unbound | Prompt 007 finalizes controls. |
| Camera cycle | Unity-DirectInput/Input System button | `CameraCycle` | LWS command frame | bind optional button | Unbound | Used for cab/chase validation focus. |
| D-pad | Unity-DirectInput/Input System D-pad/buttons | `DPadUp/Down/Left/Right` | LWS menu navigation intent | bind during button calibration | Unbound | Heat integration deferred. |
| Menu confirm/back/pause | Unity-DirectInput/Input System buttons | `MenuSubmit/MenuCancel/Pause` | LWS menu/navigation intent | bind optional buttons | Unbound | Heat integration deferred. |
| Force feedback | DIManager FFB effects | `LwsForceFeedbackSettings` | physical wheel only | enabled, master, alignment, damping, road, impact, preferred device search | `g29` search term | Uses DIManager constant, damper, and friction effects. |
