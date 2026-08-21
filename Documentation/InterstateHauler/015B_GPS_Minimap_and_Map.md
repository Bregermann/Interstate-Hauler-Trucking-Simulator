# Prompt 015B - GPS Minimap And Full Map

## Result

Prompt 015B adds a functional semantic GPS minimap and full-screen map. The later GPS presentation repair keeps the same navigation authority while splitting the display policy between cockpit and exterior cameras.

The map is not a 3D camera. It renders LWS road graph and route data through `LwsSemanticGpsMapGraphic`, a uGUI `MaskableGraphic`.

## Data Sources

- Road geometry: `ILwsRoadGraphService.ActiveGraph`
- Active route: `ILwsNavigationService.CurrentRoute`
- Maneuver/runtime state: `ILwsNavigationService.RuntimeState`
- Player local pose: active `LwsPlayerTruck` or `ILwsWorldOriginService.PlayerLocalPosition`
- Player global pose: `ILwsWorldOriginService.LocalToGlobal`

## Minimap

- Position: bottom-right overlay
- Default orientation: heading-up
- Visible by default while the big map is closed and the active camera is not cockpit
- Hidden automatically in cockpit camera mode
- Shows cached road graph geometry
- Shows active route polyline
- Shows player marker
- Shows destination marker when a route exists
- Shows remaining distance, ETA, current road, and speed-limit text where available
- Includes a MAP button that opens the full map

## Cockpit Cab GPS

The cockpit navigation display is a physical world-space UI mounted to the player truck cab.

- Controller: `LwsCabGpsController`
- Stable anchor: `IH_CabAnchor_GpsMount`
- Runtime hierarchy: `IH_PlayerTruck_NWH_Runtime/Cab/IH_CabAccessoryAnchors/IH_CabAnchor_GpsMount/IH Physical Cab GPS Screen`
- Canvas mode: `World Space`
- Map renderer: `LwsSemanticGpsMapGraphic`
- Orientation: heading-up
- Navigation source: the same `ILwsNavigationService`, `ILwsRoadGraphService`, and `ILwsWorldOriginService` used by the HUD minimap and full map

Default cockpit policy:

- Cockpit camera: world-space cab GPS active, screen-space HUD minimap hidden
- Exterior/chase camera: world-space cab GPS may remain active, screen-space HUD minimap visible

The cab GPS is parented under the truck/cab hierarchy, so it follows truck movement and floating-origin shifts naturally. Its map content remains semantic/global and does not depend on loaded physical road chunk objects.

## Full Map

- Toggle: M key or minimap MAP button
- Close: CLOSE button or Escape
- Pause: pauses gameplay by default while open
- Controls:
  - Center player
  - Center route
  - Zoom in
  - Zoom out
  - Reset zoom
  - Pan north/south/east/west

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

The map converts local player position into semantic global position using `ILwsWorldOriginService.LocalToGlobal`. Road graph samples and route waypoints are already semantic/global positions, so the map remains stable through floating-origin shifts and streamed highway chunks.

## Performance

- Road graph presentation is rebuilt only when the active `LwsRoadGraph` reference changes.
- Route presentation is rebuilt only when the active `LwsRouteResult` reference changes.
- Player marker/heading and map text refresh frequently.
- No minimap camera, render texture, or scene-wide object search is used for road presentation.

## Prompt 016 Handoff Note

Prompt 016 should consume the semantic route/map state already exposed here if it needs route-aware world generation, economy, or destination validation. Prompt 015B did not begin Prompt 016.
