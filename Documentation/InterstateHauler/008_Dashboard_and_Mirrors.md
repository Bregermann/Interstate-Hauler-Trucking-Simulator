# Prompt 008 - Dashboard, Mirrors, And Cab Accessory Anchors

## Result

Prompt 008 establishes the first LWS-owned cockpit layer for the NWH Euro semi:

- dashboard state is driven by LWS gameplay state and NWH telemetry adapters
- existing NWH speed/RPM/digital gear/instrument art is reused at runtime
- the stock NWH dash GUI controller is disabled on the spawned LWS truck instance so LWS owns the cab state presentation
- left/right mirror cameras are controlled by LWS mirror quality presets tied to Prompt 003 rendering settings
- Cab Life accessory anchors are created as stable semantic IDs under the spawned LWS truck instance
- a development hula-girl placeholder proves the anchor path without production collectible art

No vendor source or original vendor prefab was modified.

## Cab Hierarchy

Observed source prefab:

`Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab`

Useful cab objects:

| Element | Status | Notes |
| --- | --- | --- |
| `Cab` | Available art / partial logic | Primary cab body hierarchy. |
| `interior` | Available art | Interior mesh/material root. |
| `steering wheel` | LWS runtime binding | Prompt 008 rotates it from NWH/LWS gameplay steering readback. |
| `DashInstruments` | Working NWH native + LWS runtime binding | Hosts speed/RPM/gear and dash lights. |
| `Dash Lights` | Working NWH native + LWS runtime binding | Existing NWH lamps for signals/high beam/check engine. |
| `SpeedGaugeAnalog` | LWS runtime binding | Driven in MPH through `LwsTruckDashboardController`. |
| `RPMGaugeAnalog` | LWS runtime binding | Driven from `LwsNwhVehicleAdapter.ReadTelemetry().engineRpm`. |
| `GearGaugeDigital` | LWS runtime binding | Driven from Prompt 006 display labels. |
| `Left Blinker` / `Right Blinker` | LWS runtime binding | Uses Prompt 007 signal/hazard semantic state and one shared blink clock. |
| `High Beam` | LWS runtime binding | Uses Prompt 007 high-beam state. |
| `Check Engine` | Partial | Reserved for stall/severe transmission abuse/future engine faults. |
| `MirrorGlassL/R` | LWS runtime mirror texture assignment | Mirror materials are instanced at runtime. |
| `RenderTextureMirrorCameraL/R` | LWS runtime mirror control | Quality tier controls RT size, cadence, clip, FOV, and enable/disable. |

No usable wiper geometry/pivot or pedal animation path was confirmed in this pass.

## Dashboard Architecture

Runtime:

- `LwsTruckDashboardDefinition`
- `LwsTruckDashboardController`
- `LwsTruckDashboardService`
- `LwsTruckDashboardSnapshot`
- `LwsTruckDashboardDebugPanel`

Data asset:

`Assets/LWS/InterstateHauler/Vehicles/Dashboard/Data/IH_DashboardDefinition_NwhSemi.asset`

Truck definition reference:

`Assets/LWS/InterstateHauler/Vehicles/Data/IH_TruckDefinition_StarterNwhSemi.asset`

State sources:

- `LwsNwhVehicleAdapter` for speed, RPM, steering, pedals, engine readback, trailer readback
- `LwsTruckControlController.CurrentState` / `ILwsTruckControlService.ActiveState` for semantic controls
- `Lws18SpeedTransmissionController.DisplayState` for 18-speed gear/range/splitter display

The dashboard does not read DirectInput, DIManager, Logitech APIs, HID paths, Unity Input System hardware paths, or NWH input-provider state.

## Gauges

Speedometer:

- Existing `SpeedGaugeAnalog`
- LWS display unit: MPH
- Range: 0-100 MPH
- NWH source: signed speed in meters per second
- Conversion: absolute speed x 2.23693629

Tachometer:

- Existing `RPMGaugeAnalog`
- Range: 0-3000 RPM
- NWH source: engine output RPM

Transmission display:

- Existing `GearGaugeDigital`
- LWS labels support `N`, `R`, `LO-L`, `LO-H`, `1L` through `8H`
- Source: `Lws18SpeedTransmissionController.DisplayState.displayLabel`

Range/splitter:

- Primary readable value is the Prompt 006 logical gear label.
- Requested/engaged range and splitter are also exposed in dashboard snapshot/debug state.
- Requested state is not misrepresented as engaged state.

## Indicators

Existing native dash-light bindings:

- left signal
- right signal
- high beam
- check engine

LWS auxiliary physical status panel, created under the dashboard at runtime until final truck-specific indicator art exists:

- ignition / engine running
- parking brake
- engine brake level
- retarder level
- differential lock
- cruise enabled and target speed
- trailer attached

Engine/Jake brake and retarder remain semantic dashboard states, but the selected starter NWH semi does not support those systems through confirmed NWH APIs.

## Cab Visuals

Steering wheel:

- The visible `steering wheel` object rotates from gameplay steering readback, not raw wheel hardware.
- Works with keyboard/controller/G29 so long as the active input path drives NWH steering.
- Initial visual range is 450 degrees each direction.

Wipers:

- Prompt 007 semantic state exists.
- No suitable selected-truck wiper animation path was confirmed.
- Deferred to future cab/weather art integration.

Pedals:

- No practical visible pedal animation target was confirmed.
- Deferred; the gameplay pedal state remains available through telemetry.

Illumination:

- Existing NWH dash lamp UI/materials are reused.
- Runtime auxiliary TextMesh indicators use local colors only.
- No shared vendor material asset is modified.
- Dashboard brightness is represented on `LwsTruckDashboardDefinition` for future settings work.

## Mirrors

Runtime:

- `LwsTruckMirrorController`
- `LwsTruckMirrorRuntimeState`
- `LwsMirrorQualityPreset`

Existing mirror cameras:

- `RenderTextureMirrorCameraL`
- `RenderTextureMirrorCameraR`

Existing mirror surfaces:

- `MirrorGlassL`
- `MirrorGlassR`

Prompt 008 creates runtime `RenderTexture` instances and assigns them to runtime material instances. Original vendor render textures and materials remain untouched.

Mirror authority:

- NWH/vendor art provides mirror cameras and mirror glass.
- LWS owns quality, enable/disable policy, RT resolution, update cadence, culling mask adjustment, clip planes, FOV, and future mirror angle adjustment.

URP/Linear:

- Mirror cameras render through Unity cameras into runtime ARGB32 render textures.
- HDR/MSAA are disabled for mirror cameras by default to reduce cost.
- Post processing is disabled in the quality plan.
- Normal Unity Editor visual verification is still required for final URP material/glass brightness.

## Mirror Quality Presets

| Quality | Enabled | Resolution | Update Rate | Clip/FOV | Rendering Differences | Expected Platform |
| --- | --- | ---: | ---: | --- | --- | --- |
| Off | No | 0 | 0 | disabled | mirror cameras disabled | Extreme fallback/debug only |
| Low | Yes | 768 | every 3 frames | 0.05-120m / 58 deg | HDR/MSAA off, UI layer excluded where present | Low PC / Steam Deck |
| Medium | Yes | 1024 | every 2 frames | 0.05-160m / 56 deg | HDR/MSAA off, UI layer excluded | Balanced |
| High | Yes | 1536 | every frame | 0.05-220m / 55 deg | HDR/MSAA off, UI layer excluded | Main PC default |
| Ultra | Yes | 2048 | every frame | 0.05-260m / 55 deg | Shadows allowed by preset, post off | High-end PC |

Prompt 003 rendering profiles can override the active mirror resolution/update interval through `IH_RenderingSettings`.

Steam Deck profile:

- Prompt 003 Steam Deck tier maps mirrors to Low.
- Target intent remains 1280x800 and stable 30 FPS.
- Physical Steam Deck performance verification is still required.

## Mirror Adjustment Seam

`LwsTruckMirrorController.SetMirrorAdjustment(bool left, Vector2 adjustmentDegrees)` provides per-mirror horizontal/vertical adjustment without building final UI. Future profile/settings save can persist semantic adjustment values rather than camera transforms.

## Cab Life Anchors

Runtime:

- `LwsCabAccessoryAnchor`
- `LwsCabAccessoryAnchorRegistry`
- `LwsCabAccessoryAnchorType`
- `LwsCabAccessoryBobble`

Stable anchors created on the spawned LWS truck instance:

| Anchor ID | Type | Expected Content |
| --- | --- | --- |
| `IH_CabAnchor_Dashboard01` | Dashboard accessory | hula girl / bobblehead |
| `IH_CabAnchor_Dashboard02` | Dashboard accessory | souvenir / mini flag / coffee cup |
| `IH_CabAnchor_Hanging01` | Hanging accessory | dice / air freshener |
| `IH_CabAnchor_PassengerSeat` | Passenger seat | future dog companion / bag |
| `IH_CabAnchor_Sleeper` | Sleeper | future dog/cat companion / bedding |
| `IH_CabAnchor_Memento01` | Personal memento | photo / postcard / kid drawing |

These IDs are the future save/load handle. Save systems should store equipped accessory IDs and anchor IDs, not world-space transforms.

## Hula Placeholder

The registry attaches `IH_DevHulaGirl_Placeholder` to `IH_CabAnchor_Dashboard01` at runtime when validation tooling is enabled.

The placeholder:

- is TextMesh-based development art, not production collectible content
- has no Rigidbody
- disables any colliders found under attached accessories
- uses `LwsCabAccessoryBobble` for lightweight presentation-only motion
- does not affect NWH center of mass, wheels, trailer, or collision response

Prompt 074 owns production hula-girl art, unlocks, inventory, and cab customization UI.

## Performance

Automated performance measurements were not taken in this environment. Expected costs:

- Cab anchors: negligible, static transforms plus a small registry list
- Hula placeholder: trivial TextMesh plus one lightweight bobble update
- Mirrors: primary cost risk, because left/right mirror cameras render extra views

Manual profiling still required:

- Mirrors Off/Low/Medium/High/Ultra CPU and GPU frame impact
- RenderTexture memory cost
- Steam Deck 1280x800 cockpit/mirror usability
- Weather Maker precipitation/fog cost inside mirror cameras

## Aspect Ratios

Required normal Editor visual checks:

| Aspect | Status | Notes |
| --- | --- | --- |
| 16:9 | Manual required | Verify gauges, mirrors, and hula placeholder from cab camera. |
| 16:10 / 1280x800 | Manual required | Steam Deck readability check. |
| 21:9 | Manual required | Verify dashboard framing and mirror usefulness. |
| 32:9 | Manual required | Verify no stretched mirror/cockpit artifacts. |

## Tests

Added EditMode tests:

- dashboard speed conversion
- gauge mapping validation
- signal/hazard blink behavior
- mirror preset validation
- cab anchor registry required categories
- unsafe accessory Rigidbody rejection
- dashboard service duplicate rejection

Added PlayMode tests:

- dashboard service/controller publish path
- mirror Off disables mirror cameras
- hula placeholder attaches without physics
- dashboard snapshot consumes Prompt 007 control state

Normal Unity Editor Test Runner remains authoritative.

## Limitations

- Normal Editor visual/manual validation is still required.
- The selected truck uses validation sample art, not final Interstate: Hauler truck art.
- Existing vendor digital gear display may need visual legibility work after Editor inspection.
- Engine brake, retarder, independent trailer brake, and wipers are semantic states only for the starter truck.
- No full dashboard illumination/day-night dimmer system yet.
- No final mirror adjustment UI yet.
- No production hula art or Cab Life inventory/economy.

## Prompt 009 Recommendations

Prompt 009 should keep Cab Life untouched except for preserving the spawned truck prefab and validation scene. Road work should use EasyRoads as road-geometry authority and LWS as road-graph/routing authority.
