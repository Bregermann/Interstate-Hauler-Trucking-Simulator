# Prompt 015B - GPS Minimap And Full Map

## Current Presentation Policy

Compass Navigator Pro 4 is now the normal player-facing GPS/minimap presentation. LWS remains the only navigation authority; Compass receives LWS route, POI, and position data as a presentation layer.

Default camera behavior:

- Cockpit camera: world-space cab Compass GPS active, screen-space HUD minimap hidden.
- Exterior/chase/other camera: screen-space Compass HUD minimap visible, cab GPS may remain active.
- Full map: M key or MAP button opens the Compass Pro full-map state when the vendor runtime is available.

## Authority

There is still one navigation authority:

- Road geometry: `ILwsRoadGraphService.ActiveGraph`
- Active route: `ILwsNavigationService.CurrentRoute`
- Maneuver/runtime state: `ILwsNavigationService.RuntimeState`
- Player local pose: active `LwsPlayerTruck` or `ILwsWorldOriginService.PlayerLocalPosition`
- Player global pose: `ILwsWorldOriginService.LocalToGlobal`

Compass Navigator Pro does not own destinations, route planning, route progress, save data, or highway graph state.

## Compass Navigator Pro Adapter

Runtime adapter: `Assets/LWS/InterstateHauler/Navigation/Compass/LwsCompassNavigatorProAdapter.cs`

Vendor package:

- Product: Compass Navigator Pro 4
- Version: 6.0.2
- Root: `Assets/Plugins/Kronnect/CompassNavigatorPro`
- Namespace: `CompassNavigatorPro`
- Prefab: `Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab`

The adapter instantiates vendor prefab instances for:

- HUD/exterior minimap
- cockpit world-space GPS

It feeds both from the same `ILwsNavigationService` route state and converts global LWS route/POI positions to local Unity space through `ILwsWorldOriginService.GlobalToLocal`.

## Fallback Semantic Map

The old `LwsSemanticGpsMapGraphic` remains as a project-owned fallback and test utility. It is no longer the normal player-facing GPS presentation when Compass Navigator Pro is available, and the cab fallback canvas is hidden once the Compass cab GPS is created.

Fallback responsibilities:

- validate LWS road graph and route shape without vendor UI
- provide an emergency HUD/full-map fallback if Compass Pro is missing or not ready
- support existing EditMode/PlayMode tests that verify semantic map geometry

The development UI hides this fallback minimap while Compass Navigator Pro owns the HUD/cab/full-map presentation.

## Cockpit Cab GPS

The cockpit navigation display is a physical world-space UI mounted to the player truck cab.

- Controller: `LwsCabGpsController`
- Stable anchor: `IH_CabAnchor_GpsMount`
- Runtime hierarchy: `IH_PlayerTruck_NWH_Runtime/Cab/IH_CabAccessoryAnchors/IH_CabAnchor_GpsMount/IH Cab GPS Compass Navigator Pro`
- Canvas mode: `World Space`
- Orientation: heading-up through Compass `miniMapOrientation = Follow`
- Navigation source: the same LWS navigation services used by the HUD minimap and full map
- Placement reference: `LwsCabGpsController.PhysicalScreenTransform`
- Cab Compass sizing: `miniMapPositionAndSize = UserDefined`, with vendor `MiniMap Root` and `MiniMap` RectTransforms fitted to the physical GPS screen

The cab GPS is parented under the truck/cab hierarchy, so it follows truck movement and floating-origin shifts naturally. Its map content remains semantic/global and does not depend on loaded physical road chunk objects. The exterior HUD minimap still uses Compass-controlled bottom-right placement and is unchanged by the cab fit repair.

## HUD Minimap

- Position: bottom-right overlay through Compass `miniMapLocation = BottomRight`
- Default orientation: heading-up
- Visible while the full map is closed and the active camera is not cockpit
- Hidden automatically in cockpit camera mode
- Shows vendor-rendered roads/world view, LWS route, player icon, destination POI, and limited road POIs

## Full Map

- Toggle: M key or minimap MAP button
- Close: vendor full-map close behavior, CLOSE fallback button, or Escape
- Pause: pauses gameplay by default while open
- Preferred implementation: Compass `miniMapFullScreenState`
- Fallback implementation: project-owned semantic full map only if Compass is unavailable

Opening the full map does not change the camera-based minimap policy. Closing the full map restores the correct state: cockpit keeps the HUD minimap hidden, while exterior/chase shows it.

## Camera Presentation Policy

`ILwsCameraPresentationService` is the LWS presentation seam for GPS visibility. It observes the active NWH `CameraChanger` through `LwsNwhCameraPresentationMonitor` and classifies the active camera using NWH `CameraInsideVehicle` metadata:

- `Cockpit` hides the HUD minimap in `Auto`
- `Exterior` shows the HUD minimap in `Auto`
- development overrides: `Auto`, `ForceHudMinimapOn`, `ForceHudMinimapOff`, `ForceCabGpsOn`

These controls affect presentation only. They do not create a second route, destination, route progress model, player marker authority, or road graph.

## Route Actions

The GPS / Navigation tab provides:

- Start test route
- Route to Mile 50 when the 50-mile controller is present
- Clear route
- Recalculate route
- Center map
- Toggle GPS voice guidance

## Floating-Origin Safety

Route and POI positions are refreshed against `ILwsWorldOriginService.OriginVersion`. Physical streaming chunks may load/unload independently; the GPS presentation continues consuming LWS semantic road graph and route state.

## Performance

- Compass minimap capture is vendor-managed and should be profiled in later Steam Deck/performance passes.
- LWS route conversion is refreshed only when the active route or floating-origin version changes.
- Road POIs are capped by `LwsCompassNavigatorProAdapter.maximumRoadPoiCount` to avoid clutter and runtime cost.
- The fallback semantic map still rebuilds only on graph/route changes when it is used.

## Prompt 016 Handoff Note

Prompt 016 should consume the semantic route/map state already exposed here if it needs route-aware world generation, economy, or destination validation. Prompt 015B and the Compass repair did not begin Prompt 016.
