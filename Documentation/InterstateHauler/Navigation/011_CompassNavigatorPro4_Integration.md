# Compass Navigator Pro 4 Integration

This repair replaces the player-facing homemade GPS/minimap presentation with the installed Kronnect Compass Navigator Pro 4 package while preserving the existing LWS navigation authority.

## Vendor Detected

- Product: Compass Navigator Pro 4
- Version: 6.0.2
- Root: `Assets/Plugins/Kronnect/CompassNavigatorPro`
- Namespace: `CompassNavigatorPro`
- Runtime prefab: `Assets/Plugins/Kronnect/CompassNavigatorPro/Resources/CNPro/Prefabs/CompassNavigatorPro.prefab`
- Main controller: `CompassPro`
- POI component: `CompassProPOI`
- Optional NavMesh helper: `CompassProNavMeshRoute`

No Kronnect vendor source or prefab asset was modified.

## LWS Authority

LWS remains the sole route/navigation authority:

- `ILwsNavigationService`
- `LwsNavigationService`
- `LwsRoutePlanner`
- `ILwsRoadGraphService`
- `ILwsWorldOriginService`

Compass Navigator Pro is used as presentation middleware only. It receives already-planned LWS route points through `CompassPro.SetRoute(IList<Vector3>)`; it does not choose destinations, calculate truck routes, track route progress, or own road graph state.

## Adapter

Runtime adapter: `Assets/LWS/InterstateHauler/Navigation/Compass/LwsCompassNavigatorProAdapter.cs`

The adapter is added to the spawned player truck by `LwsPlayerTruckSpawner`. It creates two Compass Pro runtime instances from the vendor prefab:

- `IH Compass Navigator Pro HUD`: screen-space HUD minimap for exterior/chase/non-cockpit cameras.
- `IH Cab GPS Compass Navigator Pro`: world-space cab GPS under the existing cab GPS mount.

The adapter uses reflection for the vendor boundary because the LWS runtime assembly is asmdef-based while the Kronnect package is compiled in Unity's predefined script assemblies. This avoids editing vendor asmdefs and keeps the integration non-invasive.

## Cockpit And Exterior Policy

- Cockpit camera: cab world-space Compass GPS active, HUD minimap hidden.
- Exterior/chase/other camera: HUD Compass minimap visible, cab GPS may remain active.
- Full map: `M` and the MAP button prefer Compass Pro `miniMapFullScreenState` when the vendor runtime is available.
- Fallback: the old `LwsSemanticGpsMapGraphic` remains available only when the Compass vendor runtime is missing or not ready.

Camera identity still comes from `ILwsCameraPresentationService` and the NWH camera metadata path established earlier. The GPS integration does not guess camera mode from object names or positions.

## Route And POI Flow

1. LWS route planning creates/updates `LwsRouteResult`.
2. `LwsCompassNavigatorProAdapter` receives it through `ILwsNavigationRoutePresenter`.
3. Route waypoints are converted from LWS global coordinates into current Unity local space using `ILwsWorldOriginService.GlobalToLocal`.
4. Both Compass Pro instances receive the same converted route through `SetRoute`.
5. Destination and limited road-name POIs are created with `CompassProPOI` and registered with both Compass controllers.

The optional vendor `CompassProNavMeshRoute` helper was found and documented, but it is not used because it performs ordinary Unity NavMesh pathfinding instead of LWS highway graph routing.

## Floating Origin And Streaming

Route and POI positions are refreshed when the floating-origin version changes. Physical streaming chunks may load/unload independently; the GPS continues consuming LWS semantic road graph and route state rather than relying on physical road geometry objects as navigation authority.

## Development UI

The GPS tab now reports:

- Compass Pro 4 adapter status
- vendor runtime readiness
- package version
- route presentation state
- POI count
- cockpit and exterior presentation readiness
- NavMesh helper status
- last adapter error

The old normal status text `GPS MAP UNAVAILABLE` has been removed from the development UI. If LWS services are not available, the fallback message is `COMPASS GPS WAITING FOR LWS NAVIGATION`.

## Validation

`Interstate Hauler / Validate Project` now checks:

- Compass Navigator Pro vendor package files exist.
- LWS adapter uses actual vendor `CompassPro`, `CompassProPOI`, route, full-map, and NavMesh helper APIs.
- Player truck spawner adds the adapter.
- Development UI prefers the vendor fullscreen/minimap path and hides the fallback minimap while Compass owns presentation.
- Single LWS navigation authority is preserved.

## Known Limitations

- Physical cockpit readability still requires normal Unity Editor visual validation from the driver camera.
- Compass Pro minimap capture cost should be profiled later for Steam Deck and heavy traffic/weather scenes.
- Road POIs are intentionally capped for now to avoid clutter and runtime cost.
- Vendor full-screen map behavior is controlled through `miniMapFullScreenState`; deeper vendor map styling is deferred.
