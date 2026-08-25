# Cab Interior Recovery Pass

This pass repairs the player-truck cab presentation without changing NWH physics, transmission, road, traffic, streaming, floating-origin, weather, save, or Compass route authority.

## Player Truck

Primary prefab: `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab`

The prefab remains a project-owned NWH semi variant. Runtime cab recovery is installed by `LwsPlayerTruckSpawner` on the spawned truck instance so the vendor NWH source prefab remains untouched.

## Vendor Assets Used

- NWH Vehicle Physics 2 selected truck: `Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab`
- NWH dashboard gauges: `NWH.VehiclePhysics2.VehicleGUI.AnalogGauge`, `DigitalGauge`, `DashLight`
- NWH speed source: `VehicleController.Speed` and `VehicleController.SpeedSigned`, wrapped by `LwsNwhVehicleAdapter.ReadTelemetry()`
- NWH RPM source: `VehicleController.powertrain.engine.OutputRPM`, wrapped by `LwsNwhVehicleAdapter.ReadTelemetry()`
- Compass Navigator Pro 4 package: 6.0.2
- Compass prefab: `Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab`
- Compass controller: `CompassNavigatorPro.CompassPro`

No vendor source was modified.

## Cab GPS

The in-cab GPS uses Compass Navigator Pro 4 as the normal cockpit presentation.

- Stable anchor: `IH_CabAnchor_GpsMount`
- Runtime Compass object: `IH Cab GPS Compass Navigator Pro`
- Parent: player truck cab accessory anchor root beneath `Cab`
- Canvas mode: `World Space`
- Orientation: heading-up via Compass `miniMapOrientation = Follow`
- Navigation authority: shared `ILwsNavigationService`, `ILwsRoadGraphService`, and `ILwsWorldOriginService`

`LwsCabGpsController` still creates the old semantic uGUI canvas as an emergency fallback. When `LwsCompassNavigatorProAdapter` successfully creates the Compass cab GPS, it disables that fallback canvas so the cockpit display is not a fake black/text map layered under the vendor GPS.

Camera presentation remains:

- Cockpit: cab Compass GPS active, HUD minimap hidden.
- Exterior/chase/other: cab Compass GPS may remain active, HUD Compass minimap visible.
- Full map: unchanged and still provided through Compass when available.

## Dashboard Decoration

The old visible text placeholder has been removed from the registry path.

New anchor:

- `IH_CabAnchor_DashDecoration`

New placeholder:

- `IH_DashDecoration_Placeholder`

The placeholder is a small 3D cube with no TextMesh, no Rigidbody, and no active collider. It exists only as development art until final cab accessory art is available.

## Sleeper Interior

`LwsTruckCabInteriorRecovery` creates a simple presentation-only sleeper read using lightweight cubes when no authored sleeper content is present:

- bunk base
- mattress
- pillow
- rear cabinet
- overhead storage
- side storage

The placeholder is attached through `IH_CabAnchor_Sleeper`, disables/removes colliders, and does not affect NWH vehicle physics.

## Cab Lighting

The recovery component first searches for existing cab/interior/dome/dashboard lights. If none are found, it adds two small local point lights:

- `IH_CabInterior_DashboardLight`
- `IH_CabInterior_SleeperDomeLight`

These are local readability lights only. Weather Maker remains the weather, sky, and exterior lighting authority.

## Gauges

`LwsTruckDashboardController` continues to drive the actual NWH gauge components on the truck:

- speedometer: LWS telemetry from NWH `SpeedSigned`, displayed as MPH
- tachometer: LWS telemetry from NWH `powertrain.engine.OutputRPM`
- gear display: Prompt 006 transmission display state

The native NWH `DashGUIController` stays disabled on the LWS player truck so dashboard state is controlled through LWS telemetry and semantic truck-control state rather than duplicate dashboard drivers.

## Mirrors

Existing mirror behavior is preserved:

- left/right NWH mirror cameras are reused
- left/right NWH mirror glass renderers are reused
- LWS mirror quality presets remain Off, Low, Medium, High, Ultra
- mirror RenderTextures are runtime-only
- mirror cameras exclude the UI layer when available

No mirror-quality redesign was included in this pass.

## Validation

Automated logic validation added/updated:

- cab anchor registry creates GPS and dash-decoration anchors
- recovery component creates non-text dash decoration and sleeper placeholders
- registry no longer creates the old visible text placeholder
- validator checks recovery script, spawner wiring, dash decoration, sleeper cues, and docs

Manual visual validation still required in the normal Unity Editor:

- cockpit GPS is visible and Compass-owned
- HUD minimap is hidden in cockpit and visible outside
- decoration cube is visible on the dash without text
- sleeper reads as a sleeper from cab/exterior inspection angles
- dashboard light levels are useful but not overbright
- NWH gauges and mirrors still render correctly under URP/Linear
