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

Dead Air uses the shared `LwsKeyboardGamepadTruckInputSource` with `LwsTruckInputMode.BasicAutomatic`, which reads Unity keyboard/gamepad input and routes continuous driving through `ILwsVehicleInputService`.

The final vehicle flow remains:

Input source -> LWS vehicle input service -> LWS NWH input adapter -> NWH vehicle controller

Transmission flow remains:

Input source -> `Lws18SpeedTransmissionController.TrySetAutomaticMode` / automatic mode -> NWH adapter

## Audio

`DeadAirAudioDirector` uses Unity `AudioSource` and `AudioClip` only. Missing clips fall back to subtitles so WebGL builds can still run before final audio is authored. Audio collision behavior is data-driven (`Queue`, `Interrupt`, `Ignore`, `Wait`) and does not use native plugins.

## Off-Road Failure

`DeadAirValidRoadZone` and `DeadAirOffRoadFailureController` use Unity transforms, box volumes, timers, and the existing UI ending overlay. The void failure path is WebGL-safe and does not require custom render features, native plugins, file IO, threads, or platform-specific input.

## Validation Status

Automated WebGL build was attempted through Unity 6000.4.10f1 batchmode on 2026-08-24, but the process stalled in Unity Licensing Client reconnect loops before the Dead Air scene builder or WebGL build could run. This is separate from the previously known vendor batchmode false-negative path and still requires normal Unity Editor validation.

Use the normal Unity Editor to:

1. Open `Assets/DeadAir/Scenes/DeadAir_Main.unity`.
2. Run `Dead Air/Build Or Refresh Main Scene`.
3. Run `Dead Air/Validate Jam Mode`.
4. Switch platform to WebGL.
5. Run `Dead Air/Build WebGL`, which calls `DeadAir.Editor.DeadAirWebGLBuilder.BuildWebGl`.

Do not overwrite Interstate production builds.
