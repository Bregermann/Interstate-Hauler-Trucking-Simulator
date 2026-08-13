# Prompt 011 - Compass API Matrix

| Feature | Compass Class | Method/Property | Runtime/Editor | LWS Adapter | Authority | Risk | Notes |
|---|---|---|---|---|---|---|---|
| Package root | N/A | `Assets/Plugins/Kronnect/CompassNavigatorPro` | Both | N/A | Presentation | Low | Installed owned middleware. |
| Version evidence | N/A | README `Version 6.0` | Editor audit | N/A | Presentation | Low | README history records Version 6.0. |
| Namespace | N/A | `CompassNavigatorPro` | Runtime | N/A | Presentation | Low | Scripts use this namespace. |
| Main component | `CompassPro` | Component fields/properties | Runtime | `LwsCompassRoutePresenter` | Presentation | Medium | Optional; not required for physical GPS overlay. |
| Follow target | `CompassPro` | `follow` | Runtime | Future | Presentation | Medium | Can point to player transform if Compass map is used later. |
| Minimap visibility | `CompassPro` | `showMiniMap` | Runtime | Future | Presentation | Medium | Useful for future UI, not route authority. |
| Route polyline | `CompassPro` | `SetRoute(IList<Vector3>)` | Runtime | `LwsCompassRoutePresenter.PresentRoute` | Presentation | Low | LWS route waypoints can be passed in. |
| POI route | `CompassPro` | `SetRouteToPOI(CompassProPOI)` | Runtime | None | Presentation only | Medium | Direct POI routing is not used as LWS route authority. |
| World destination route | `CompassPro` | `SetRouteToDestination(Vector3)` | Runtime | None | Presentation only | Medium | Direct line route is not truck GPS authority. |
| Clear route | `CompassPro` | `ClearRoute()` | Runtime | `LwsCompassRoutePresenter.ClearRoute` | Presentation | Low | Optional presenter cleanup. |
| Route exists | `CompassPro` | `hasRoute` | Runtime | Future diagnostics | Presentation | Low | Can be queried if Compass presentation is enabled. |
| NavMesh route helper | `CompassProNavMeshRoute` | NavMesh path helper | Runtime | None | Not LWS authority | High | Not suitable for truck-road graph routing authority. |
| RenderTexture/world-space UI | Compass internals | Minimap/camera UI | Runtime | Deferred | Presentation | Medium | Prompt 011 uses LWS world-space cab screen instead. |
