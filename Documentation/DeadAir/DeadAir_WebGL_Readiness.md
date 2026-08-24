# Dead Air WebGL Readiness

Dead Air is designed to build for itch.io WebGL.

## Explicitly Avoided

- DirectInput
- Logitech SDK
- native force feedback
- Windows-only wheel APIs
- direct file writes for gameplay state
- thread-dependent gameplay systems
- duplicate NWH vehicle control

## Input

Dead Air uses `DeadAirBasicAutomaticInputSource`, which reads Unity keyboard/gamepad input and routes continuous driving through `ILwsVehicleInputService`.

The final vehicle flow remains:

Input source -> LWS vehicle input service -> LWS NWH input adapter -> NWH vehicle controller

Transmission flow remains:

Input source -> `Lws18SpeedTransmissionController` automatic mode -> NWH adapter

## Audio

`DeadAirAudioDirector` uses Unity `AudioSource` and `AudioClip` only. Missing clips fall back to subtitles so WebGL builds can still run before final audio is authored.

## Validation Status

Automated WebGL build was not run in this pass. The project has known normal-Editor-authoritative validation policy because earlier batchmode paths reported false vendor negatives.

Use the normal Unity Editor to:

1. Open `Assets/DeadAir/Scenes/DeadAir_Main.unity`.
2. Run `Dead Air/Build Or Refresh Main Scene`.
3. Run `Dead Air/Validate Jam Mode`.
4. Switch platform to WebGL.
5. Build to `Builds/DeadAir_WebGL`.

Do not overwrite Interstate production builds.
