# Prompt 005 - Wheel Compatibility Matrix

Status meanings:

- SUPPORTED / PHYSICALLY VERIFIED BY INTERSTATE HAULER: tested on this project with real hardware.
- PLUGIN VERIFIED: listed as verified by Unity-DirectInput documentation, but not yet physically verified by Interstate: Hauler.
- POTENTIALLY SUPPORTED: expected to work through DirectInput, but not specifically verified here.
- UNKNOWN: not enough evidence.
- UNSUPPORTED: not supported by the current Prompt 005 backend.

| Device | Status | Source Of Claim | Steering/Pedals | Shifter | FFB | Notes |
|---|---|---|---|---|---|---|
| Logitech G29 + G29 pedals + Driving Force Shifter | PLUGIN VERIFIED | Unity-DirectInput README lists Logitech G29 / G920 verified | Required for Interstate physical test | Required for Interstate physical test | Unity-DirectInput / DIManager | Mandatory reference hardware; not yet marked physically verified by Interstate. |
| Logitech G920 | PLUGIN VERIFIED | Unity-DirectInput README lists Logitech G29 / G920 verified | Expected through same DirectInput architecture | Requires physical binding | Unity-DirectInput / DIManager | Not Interstate-verified. |
| Logitech PRO Racing Wheel | PLUGIN VERIFIED | Unity-DirectInput README lists PRO Racing Wheel verified | Expected through DirectInput profile/calibration | Unknown | Unity-DirectInput / DIManager | Future profile. |
| Fanatec DD1 | PLUGIN VERIFIED | Unity-DirectInput README verified list | Expected through DirectInput profile/calibration | Device-dependent | Unity-DirectInput / DIManager | Future profile. |
| Fanatec CSL DD | PLUGIN VERIFIED | Unity-DirectInput README verified list | Expected through DirectInput profile/calibration | Device-dependent | Unity-DirectInput / DIManager | Future profile. |
| Simucube Ultimate 2 | PLUGIN VERIFIED | Unity-DirectInput README verified list | Expected through DirectInput profile/calibration | Device-dependent | Unity-DirectInput / DIManager | Future profile. |
| Moza R16 / R12 / R9 / R5 / R3 | PLUGIN VERIFIED | Unity-DirectInput README verified list | Expected through DirectInput profile/calibration | Device-dependent | Unity-DirectInput / DIManager | Future profile. |
| Thrustmaster TX | PLUGIN VERIFIED | Unity-DirectInput README verified list | Expected through DirectInput profile/calibration | Device-dependent | Unity-DirectInput / DIManager | Future profile. |
| Other DirectInput-compatible wheels | POTENTIALLY SUPPORTED | Unity-DirectInput supports DirectInput devices broadly | Requires new profile/calibration | Unknown | Depends on FFB capability | Do not claim support without physical verification or plugin documentation. |
| LogitechGSDK proprietary path | UNSUPPORTED | Prompt 005 backend decision | Cancelled | Cancelled | Cancelled | Do not pursue. |
| NWH SteeringWheelInput Logitech SDK path | UNSUPPORTED | Prompt 005 backend decision | Cancelled | Cancelled | Cancelled | Optional import may exist locally but is not the selected backend. |
