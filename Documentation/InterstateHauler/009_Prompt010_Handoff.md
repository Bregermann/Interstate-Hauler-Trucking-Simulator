# Prompt 010 Handoff - UTS Highway Integration

Scene:

`Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`

Road graph/runtime:

- `Assets/LWS/InterstateHauler/Roads/LwsRoadGraph.cs`
- `Assets/LWS/InterstateHauler/Roads/LwsRoadGraphRuntime.cs`
- `Assets/LWS/InterstateHauler/Roads/LwsRoadGraphProvider.cs`
- `Assets/LWS/InterstateHauler/Roads/Validation/LwsInterstateCorridorRuntimeBuilder.cs`

Corridor:

- Mainline length: about 3.4 km / 2.1 miles
- Directional carriageways: northbound and southbound
- Lanes: 2 each direction
- Lane width: 3.7 m
- Shoulders: 3.0 m right, 1.2 m left
- Median: 14.0 m
- Surface: `LwsRoadSurfaceType.AsphaltInterstate`
- Mainline speed limit: 65 MPH

Prompt 010 should use LWS graph samples and lane metadata for UTS lane/spawn work. Do not make UTS own the player truck, player trailer, route graph, or GPS authority.

Recommended traffic spawn/despawn areas:

- Far northbound mainline beyond the merge
- Southbound return carriageway near the far turnaround
- Entry ramp after the service pad
- Off-camera ends near the corridor limits

Player-truck exclusion:

- Keep NWH/LWS player truck and trailer physics separate from UTS traffic controllers.
- Do not add `CarMove` or `AddTrailer` to the player truck/trailer.

Pre-traffic performance baseline:

- Prompt 009A fixed a shared validation-scene main-thread issue in the G29 diagnostics/runtime stack.
- Do not run wheel/device discovery, DirectInput enumeration, scene-wide object searches, or full diagnostic list rebuilds from `OnGUI`, `Update`, or traffic spawn loops.
- Measure in Editor before adding UTS vehicles, especially with mirrors enabled.
- If performance regresses again, capture CPU Timeline samples before changing traffic density or rendering settings.

Prompt 009A performance fix:

- Documented in `Documentation/InterstateHauler/009A_MainThread_Performance_Fix.md`.
- `LwsWheelDeviceDiagnosticsPanel` now caches connected-device diagnostics and refreshes manually unless auto-refresh is explicitly enabled.
- `LwsWheelInputSource` now throttles disconnected-wheel discovery and no longer reapplies FFB settings every frame.
- `LwsWheelDeviceDiscovery.FindFirstMatchingDevice()` now uses summary descriptors for normal matching instead of enumerating all controls.
- `LwsDirectInputForceFeedbackCoordinator` no longer repeats identical inactive FFB shutdown calls every frame.
- Normal Unity Editor performance verification is still required before treating the 009A fix as fully profiled and passed.

Known blockers:

- Prompt 009 road and G29 highway validation still need normal Unity Editor/manual verification.
- EasyRoads URP 17.4 visual compatibility must be confirmed; no older URP support package was imported.
