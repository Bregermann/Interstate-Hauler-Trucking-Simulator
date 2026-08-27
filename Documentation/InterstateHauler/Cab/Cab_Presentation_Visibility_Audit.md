# Cab Presentation Visibility Audit

## Scope

This audit covers the current Interstate: Hauler player truck cab presentation path without replacing the truck model, NWH physics, transmission, Compass navigation architecture, external HUD minimap, full map, save system, weather, traffic, or roads.

Current player truck prefab:

`Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`

The LWS prefab is a thin prefab instance of the installed NWH Euro semi. Cab/dashboard systems are added to the runtime truck by `LwsPlayerTruckSpawner`:

- `LwsTruckDashboardController`
- `LwsTruckMirrorController`
- `LwsCabAccessoryAnchorRegistry`
- `LwsTruckCabInteriorRecovery`
- `LwsCabGpsController`
- `LwsCompassNavigatorProAdapter`

## Result

Source and prefab audit found one credible cab UI collapse risk: the project-owned Compass Navigator Pro adapter fitted the cab GPS `MiniMap Root` and `MiniMap` with stretch anchors and zero `sizeDelta`. That can be valid when every parent has a positive resolved rect, but it is fragile if any inherited vendor parent collapses to zero during runtime generation.

The repair is cab-only and lives in `LwsCompassNavigatorProAdapter`. It does not modify vendor source or the Compass prefab. The cab Compass root, `MiniMap Root`, `MiniMap`, and `MiniMapMask` now receive explicit positive dimensions based on the physical cab GPS screen size before a one-time layout/canvas rebuild. A development-only validation pass then recursively checks active visible cab Compass RectTransforms and repairs/logs any genuinely collapsed visible element once after construction.

Normal Unity Play Mode visual inspection was not performed from this shell. Runtime cockpit visibility remains marked as requiring manual visual validation.

## Cab GPS

Project-owned controller:

`Assets/LWS/InterstateHauler/Navigation/LwsCabGpsController.cs`

Compass adapter:

`Assets/LWS/InterstateHauler/Navigation/Compass/LwsCompassNavigatorProAdapter.cs`

Intended runtime hierarchy:

`IH_PlayerTruck_NWH_Runtime/Cab/IH_CabAccessoryAnchors/IH_CabAnchor_GpsMount/IH Cab GPS Compass Navigator Pro`

Fallback physical screen hierarchy:

`IH_CabAnchor_GpsMount/IH Physical Cab GPS Screen`

Audited values from source:

| Element | Source Size | Scale | Classification |
| --- | ---: | ---: | --- |
| Fallback physical GPS canvas | 640 x 400 px | 0.00042 | Present, positive size, world-space UI |
| Fallback screen panel | 640 x 400 px | inherited | Present, positive size |
| Fallback semantic map | 612 x 304 px | inherited | Present, positive size |
| Instruction text | 616 x 34 px | inherited | Present, positive size |
| Distance text | 243.2 x 28 px | inherited | Present, positive size |
| Road text | 358.4 x 28 px | inherited | Present, positive size |
| Cab Compass root | 640 x 400 px after repair | 0.00042 | Present, explicit positive size |
| Compass `MiniMap Root` | 640 x 400 px after repair | 1 | Present, repaired explicit positive size |
| Compass `MiniMap` | 640 x 400 px after repair | 1 | Present, repaired explicit positive size |
| Compass `MiniMapMask` | 640 x 400 px after repair | 1 | Present, repaired explicit positive size |

Approximate physical GPS size:

- Width: 640 * 0.00042 = 0.2688 m
- Height: 400 * 0.00042 = 0.168 m

This matches the intended dashboard GPS display scale. The tiny world-space scale is intentional pixel-to-meter scaling, not a collapse.

### GPS Repair

Changed function:

`LwsCompassNavigatorProAdapter.FitCabCompassToPhysicalScreen(...)`

Repair behavior:

- sanitizes cab GPS screen size with a minimum 100 px fallback
- forces the cab Compass root to the physical screen size
- forces `MiniMap Root`, `MiniMap`, and `MiniMapMask` to explicit screen-sized RectTransforms
- runs `LayoutRebuilder.ForceRebuildLayoutImmediate(...)`
- runs `Canvas.ForceUpdateCanvases()` once after construction
- checks active visible cab Compass RectTransforms for inverted anchors, zero/tiny actual rects, and zero/tiny scale
- repairs/logs collapsed active visible cab Compass elements in editor/development builds only

## Speedometer

Vendor prefab object:

`SpeedGaugeAnalog`

Source prefab:

`Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab`

Audited source values:

- RectTransform size: 240 x 240 px
- Local scale: approximately 0.84, 0.84, 0.84
- Active: yes in source prefab
- Presentation type: NWH `AnalogGauge`
- LWS binding: `LwsTruckDashboardController` writes `AnalogGauge.Value` from `LwsNwhVehicleAdapter.ReadTelemetry().signedSpeedMetersPerSecond`, converted to MPH through `LwsTruckDashboardDefinition`

Classification:

`VISIBLE + FUNCTIONAL` by source architecture, runtime visual response `UNVERIFIED`.

## Tachometer

Vendor prefab object:

`RPMGaugeAnalog`

Audited source values:

- RectTransform size: 240 x 240 px
- Local scale: approximately 0.84, 0.84, 0.84
- Active: yes in source prefab
- Presentation type: NWH `AnalogGauge`
- LWS binding: `LwsTruckDashboardController` writes `AnalogGauge.Value` from engine RPM telemetry

Classification:

`VISIBLE + FUNCTIONAL` by source architecture, runtime visual response `UNVERIFIED`.

## Gear Display And Dashboard Indicators

Vendor prefab dashboard objects found:

| Element | Source Size | Binding | Classification |
| --- | ---: | --- | --- |
| `GearGaugeDigital` | 129 x 55.17 px | Prompt 006 transmission display label | Present, positive size |
| `Left Blinker` | 20 x 20 px | LWS signal/hazard state | Present, positive size |
| `Right Blinker` | 20 x 20 px | LWS signal/hazard state | Present, positive size |
| `High Beam` | 20 x 20 px | LWS high-beam state | Present, positive size |
| `Check Engine` | 31.834 x 29.101 px | stall/abuse indicator seam | Present, positive size |

The LWS auxiliary dashboard status panel is physical `TextMesh` based, not RectTransform/layout based. It creates rows for ignition, engine running, parking brake, engine brake, retarder, differential lock, cruise, and trailer state. Those rows use positive local positions and `characterSize = 0.035`.

Classification:

`VISIBLE + FUNCTIONAL` by source architecture for bound indicators, runtime visual response `UNVERIFIED`.

## Dashboard Backlighting And Instrument Visibility

Vendor prefab object:

`DashInstruments`

Audited source values:

- RectTransform size: 624.74 x 304.7 px
- Local scale: approximately 0.0003522793
- Canvas render mode: World Space
- Active: yes in source prefab

Approximate physical instrument cluster size:

- Width: 624.74 * 0.0003522793 = 0.220 m
- Height: 304.7 * 0.0003522793 = 0.107 m

Classification:

`PRESENT BUT RUNTIME VISUAL UNVERIFIED`. The small scale is expected world-space dashboard scaling, not a zero-size bug.

## Mirrors

Project-owned controller:

`Assets/LWS/InterstateHauler/Vehicles/Mirrors/LwsTruckMirrorController.cs`

Vendor prefab objects found:

- `MirrorGlassL`
- `MirrorGlassR`
- `RenderTextureMirrorCameraL`
- `RenderTextureMirrorCameraR`

Runtime behavior from source:

- `LwsTruckMirrorController` resolves the left/right cameras and glass renderers by name.
- Runtime `RenderTexture` instances are created at quality-controlled resolution.
- Default mirror quality from `IH_DashboardDefinition_NwhSemi` is High, 1536 px, every frame.
- Camera target textures are assigned through `Camera.targetTexture`.
- Mirror material texture is set through `_BaseMap` and `_MainTex` when available.
- UI layer is excluded where the project has a UI layer.

Classification:

`PRESENT`, runtime visual `UNVERIFIED`. No mirror rebuild was performed.

## Accessory Anchors

Project-owned registry:

`Assets/LWS/InterstateHauler/Vehicles/Cab/Accessories/LwsCabAccessoryAnchorRegistry.cs`

Default runtime anchors under `IH_CabAccessoryAnchors`:

| Anchor ID | Local Position | Local Rotation | Local Scale | Notes |
| --- | --- | --- | --- | --- |
| `IH_CabAnchor_Dashboard01` | -0.38, 1.35, 1.15 | 0, 0, 0 | 1, 1, 1 | dashboard accessory |
| `IH_CabAnchor_Dashboard02` | 0.34, 1.35, 1.15 | 0, 0, 0 | 1, 1, 1 | dashboard accessory |
| `IH_CabAnchor_GpsMount` | 0.36, 1.15, 1.08 | 0, 0, 0 | 1, 1, 1 | world-space cab GPS |
| `IH_CabAnchor_DashDecoration` | -0.46, 1.31, 1.10 | 0, 12, 0 | 1, 1, 1 | small dash decoration |
| `IH_CabAnchor_Hanging01` | 0, 1.82, 1.02 | 0, 0, 0 | 1, 1, 1 | hanging accessory |
| `IH_CabAnchor_PassengerSeat` | 0.82, 0.82, -0.20 | 0, -12, 0 | 1, 1, 1 | passenger seat accessory |
| `IH_CabAnchor_Sleeper` | 0, 0.72, -1.35 | 0, 180, 0 | 1, 1, 1 | sleeper area |
| `IH_CabAnchor_Memento01` | -0.74, 1.48, 0.86 | 0, 18, 0 | 1, 1, 1 | memento surface |

No zero-scale anchor defaults were found. `OnValidate` repairs any default anchor definition whose scale is accidentally set to zero.

## Zero-Size Findings

Source/prefab audit:

- Cab GPS fallback RectTransforms: positive sizes found.
- Compass vendor prefab: several stretch-driven RectTransforms have `sizeDelta = 0`, which is valid only when their parent rect resolves positive.
- Cab Compass adapter prior behavior: `MiniMap Root` and `MiniMap` were stretch-driven with zero `sizeDelta`; fragile in a world-space mounted instance.
- NWH dashboard gauges: positive RectTransform sizes found.
- NWH dashboard indicators: positive RectTransform sizes found.
- NWH `DashInstruments`: positive RectTransform size found with legitimate tiny world-space scale.
- Accessory anchors: positive local scales found.
- Mirrors: renderer/camera based, not UI layout based.

Runtime visual audit:

- Not visually observed in normal Unity Play Mode from this shell.
- Cockpit visual confirmation remains required.

## Tests And Validation

Added/updated EditMode coverage:

- `CabGpsFallbackCanvasCanBeHiddenForCompassPresentation` now asserts the fallback world-space GPS canvas and semantic map have positive RectTransform sizes.
- `CabCompassFitRepairsCriticalRuntimeRectsToPositiveScreenSize` invokes the cab Compass fit helper against a collapsed test hierarchy and verifies the cab Compass root, `MiniMap Root`, `MiniMap`, and `MiniMapMask` resolve to 640 x 400 px with non-inverted anchors and nonzero scale.

Compile checks performed:

- `dotnet build LWS.InterstateHauler.Runtime.csproj`: PASS, 0 errors.
- `dotnet build LWS.InterstateHauler.Editor.csproj`: PASS, 0 errors.
- `dotnet build LWS.InterstateHauler.Tests.EditMode.csproj`: PASS, 0 errors.
- `dotnet build LWS.InterstateHauler.Tests.PlayMode.csproj`: PASS, 0 errors.
- Unity Editor log scan for C# compiler errors: no current `error CS` entries found.

A broad Unity-generated solution build was attempted and failed in `Assembly-CSharp-firstpass.csproj` because DOTween module `.cs` files could not resolve DOTween core/plugin types. That broad generated-project issue is unrelated to this cab visibility repair and no vendor source was modified.

A Unity batchmode open/quit compile attempt returned before import/compile and produced no useful success marker, so normal Unity Play Mode cockpit visual validation is still required.

## Future Truck Replacement Revalidation

When the current tractor is replaced with the intended American sleeper tractor, revalidate:

- `IH_CabAnchor_GpsMount` physical position, rotation, and windshield visibility.
- Cab Compass world-space screen readability from the cockpit camera.
- Speedometer/tachometer/gauge object names and LWS bindings.
- Dashboard indicator lamp names and LWS bindings.
- Mirror glass/camera names, render texture orientation, FOV, and clipping.
- Accessory anchor positions for dashboard, hanging, passenger seat, sleeper, and mementos.
- Wipers and pedal animation targets, which are still not confirmed on the current starter truck.