# Prompt 015B - GPS Minimap And Full Map

## Result

Prompt 015B adds a functional semantic GPS minimap and full-screen map.

The map is not a 3D camera. It renders LWS road graph and route data through `LwsSemanticGpsMapGraphic`, a uGUI `MaskableGraphic`.

## Data Sources

- Road geometry: `ILwsRoadGraphService.ActiveGraph`
- Active route: `ILwsNavigationService.CurrentRoute`
- Maneuver/runtime state: `ILwsNavigationService.RuntimeState`
- Player local pose: active `LwsPlayerTruck` or `ILwsWorldOriginService.PlayerLocalPosition`
- Player global pose: `ILwsWorldOriginService.LocalToGlobal`

## Minimap

- Position: top-right overlay
- Default orientation: heading-up
- Visible by default while the big map is closed
- Shows cached road graph geometry
- Shows active route polyline
- Shows player marker
- Shows destination marker when a route exists
- Shows remaining distance, ETA, current road, and speed-limit text where available
- Includes a MAP button that opens the full map

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
