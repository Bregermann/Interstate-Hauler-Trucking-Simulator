# Prompt 009A Main-Thread Performance Fix

Date: 2026-08-13

## Result

Status: PASS WITH MANUAL EDITOR PERFORMANCE VERIFICATION REQUIRED

This fix addresses the shared main-thread regression observed in both:

- `Assets/LWS/InterstateHauler/Vehicles/Validation/TruckValidation.unity`
- `Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`

No vendor source was modified.

## Observed Symptom

The user-provided TruckValidation performance overlay showed:

- FPS: about 8.2 FPS
- Global frame time: about 122.6 ms
- GPU frame time: about 4.8 ms
- CPU main thread: about 99.1%

This points at script, GUI, input discovery, or physics work rather than GPU saturation.

## Root Cause

Both affected validation scenes had the G29 validation stack enabled:

- `LwsWheelInputSource`
- `LwsWheelInputBootstrap`
- `LwsWheelInputDebugPanel`
- `LwsWheelDeviceDiagnosticsPanel`
- `LwsWheelCalibrationPanel`
- `LwsDirectInputForceFeedbackCoordinator`

The dominant project-owned issue was live wheel diagnostics and discovery work running from validation UI/runtime paths:

- `LwsWheelDeviceDiagnosticsPanel.OnGUI()` called `LwsWheelDeviceDiscovery.GetConnectedWheelCandidates()` on every IMGUI pass.
- Device discovery built full descriptors for candidate devices, including all axes, buttons, D-pads, and DirectInput force-feedback descriptors.
- `LwsWheelInputSource.Update()` attempted preferred-wheel rediscovery every frame while no wheel was selected.
- `LwsDirectInputForceFeedbackCoordinator.Update()` repeatedly called FFB disable while already inactive.
- `LwsWheelInputDebugPanel.OnGUI()` performed a scene-wide player-truck lookup from IMGUI.

IMGUI can run multiple times per frame, so full device enumeration and DirectInput probing from `OnGUI` is not acceptable.

## Fix

Changed project-owned files only:

- `Assets/LWS/InterstateHauler/Input/Validation/LwsWheelDeviceDiagnosticsPanel.cs`
- `Assets/LWS/InterstateHauler/Input/Validation/LwsWheelInputDebugPanel.cs`
- `Assets/LWS/InterstateHauler/Input/Wheels/LwsWheelDeviceDiscovery.cs`
- `Assets/LWS/InterstateHauler/Input/Wheels/LwsWheelInputSource.cs`
- `Assets/LWS/InterstateHauler/Input/Wheels/LwsDirectInputForceFeedbackCoordinator.cs`
- `Assets/LWS/InterstateHauler/Tests/EditMode/LwsG29InputEditModeTests.cs`

Changes:

- Cached wheel-device diagnostics results instead of rebuilding them every `OnGUI` pass.
- Added an explicit "Refresh Devices" button for expensive diagnostics refresh.
- Left optional diagnostics auto-refresh available, but disabled by default.
- Added a summary wheel descriptor path for normal profile matching so wheel selection no longer enumerates every control on every candidate device.
- Throttled disconnected wheel rescans to a bounded interval.
- Stopped reapplying FFB settings every wheel-input `Update()`.
- Made inactive FFB shutdown idempotent in the coordinator.
- Cached wheel debug panel telemetry and moved player-truck lookup out of the per-IMGUI hot path.
- Added an EditMode regression test that G29 profile matching works from summary descriptor fields without populated control lists.

## Validation

Compile validation completed through project-owned assemblies:

- `dotnet build LWS.InterstateHauler.Runtime.csproj --no-restore -v:minimal`: PASS
- `dotnet build LWS.InterstateHauler.Editor.csproj --no-restore -v:minimal`: PASS
- `dotnet build LWS.InterstateHauler.Tests.EditMode.csproj --no-restore -v:minimal`: PASS
- `dotnet build LWS.InterstateHauler.Tests.PlayMode.csproj --no-restore -v:minimal`: PASS

Known warnings remain the existing PathPainter/DOTween framework warnings and Unity obsolete API warnings.

Normal Unity Editor profiling still needs to confirm the bad-frame samples are gone.

## Manual Performance Verification

Before Prompt 010, verify in the normal Unity Editor:

1. Open `TruckValidation.unity`.
2. Enter Play Mode.
3. Idle several seconds.
4. Start the engine and drive/steer with keyboard.
5. Confirm frame time remains responsive.
6. Confirm the Profiler no longer shows `LwsWheelDeviceDiagnosticsPanel.OnGUI()` doing device discovery every frame.
7. Confirm no Console spam occurs.
8. Repeat in `InterstateCorridorValidation.unity`.

Expected result:

- No sustained 8 FPS / 120 ms main-thread freeze from wheel diagnostics.
- Any remaining frame-time issue should be profiled as a separate root cause, likely physics, mirrors, or another script sample.

## Remaining Risks

- This was validated with code inspection and assembly compilation from Codex, not a live normal-Editor Profiler capture.
- If the user still sees a freeze after this fix, capture the top CPU Timeline samples again and treat that as a separate remaining bottleneck.
- Mirror rendering and NWH physics should remain on the watch list, but they were not changed here because the shared wheel diagnostics issue was concrete and project-owned.
