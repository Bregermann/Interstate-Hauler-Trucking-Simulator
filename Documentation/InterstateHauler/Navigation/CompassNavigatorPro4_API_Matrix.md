# Compass Navigator Pro 4 API Matrix

Source package: Kronnect Compass Navigator Pro 4
Installed version: 6.0.2
Vendor root: `Assets/Plugins/Kronnect/CompassNavigatorPro`
Namespace: `CompassNavigatorPro`

| Feature | Vendor class/API | Native support | LWS usage | Notes |
| --- | --- | --- | --- | --- |
| Main controller | `CompassNavigatorPro.CompassPro` | Native | Reflected by `LwsCompassNavigatorProAdapter` | Main runtime component on the vendor prefab. |
| Runtime prefab | `Resources/CNPro/Prefabs/CompassNavigatorPro` | Native | Instantiated for HUD and cab presentations | Source asset remains untouched. |
| Minimap visibility | `CompassPro.showMiniMap` | Native | Camera-policy controlled for HUD; always on for cab GPS | Cockpit hides only the HUD presentation. |
| Minimap placement | `CompassPro.miniMapLocation` | Native | HUD uses `BottomRight`; cab uses `MiddleCenter` inside world canvas | Vendor enum discovered in `CompassProMiniMap.cs`. |
| Minimap sizing | `CompassPro.miniMapSize` | Native | Separate HUD/cab tuning values | Cab display is mounted under `IH_CabAnchor_GpsMount`. |
| Minimap contents | `CompassPro.miniMapContents` | Native | `TopDownWorldView` | Uses vendor world-view renderer instead of the homemade grid. |
| Minimap orientation | `CompassPro.miniMapOrientation` | Native | `Follow` | Heading-up presentation for truck GPS use. |
| Minimap camera mode | `CompassPro.miniMapCameraMode` | Native | `Orthographic` | Vendor-managed map camera, not LWS route authority. |
| Capture frequency | `CompassPro.miniMapCameraSnapshotFrequency` | Native | `Continuous` | May be tuned later for Steam Deck/performance. |
| Full map | `CompassPro.miniMapFullScreenState` | Native | Used by `M`/MAP when available | Falls back to LWS semantic map only if vendor runtime is absent. |
| Route rendering | `CompassPro.SetRoute(IList<Vector3> worldPoints)` | Native | LWS route waypoints converted from global to current local coordinates | `ILwsNavigationService` remains route authority. |
| Route clearing | `CompassPro.ClearRoute()` | Native | Called when LWS route is cleared/invalid | Keeps both HUD and cab vendor views in sync. |
| Route to destination | `CompassPro.SetRouteToDestination(Vector3)` | Native | Not used | Would bypass LWS route planning. |
| Route to POI | `CompassPro.SetRouteToPOI(CompassProPOI)` | Native | Not used | Would bypass LWS route planning. |
| POI component | `CompassNavigatorPro.CompassProPOI` | Native | Destination and limited road-name POIs | POIs are presentation-only metadata. |
| POI stable ID | `CompassProPOI.StableId` | Native | Set to LWS route/road identifiers | No career save data is stored in vendor POIs. |
| POI registration | `CompassPro.POIRegister(CompassProPOI)` | Native | Reflected against both HUD and cab controllers | Supports multiple Compass groups. |
| NavMesh helper | `CompassProNavMeshRoute` | Native | Available, intentionally not used | Uses `NavMesh.CalculatePath`; LWS road graph remains highway route authority. |
| Compass bar | `CompassPro.showCompassBar` | Native | Disabled for the GPS replacement path | Prompt request is GPS/minimap presentation. |
| World route line | `CompassPro.routeShowInWorld` | Native | Disabled | Avoids extra in-world route geometry during validation. |
| Save/load DTOs | `CompassProSaveLoad` | Native | Not used | LWS save architecture remains authoritative. |
