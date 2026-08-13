# Prompt 009 EasyRoads API Matrix

| Purpose | EasyRoads Class/API | Editor/Runtime | Used By LWS Adapter | Vendor Modification Required | Risk | Notes |
|---|---|---|---|---|---|---|
| Create road network | `EasyRoads3Dv3.ERRoadNetwork` | Runtime/public | Yes | No | Medium | Instantiated through reflection for validation scene. |
| Define road type | `EasyRoads3Dv3.ERRoadType` | Runtime/public | Yes | No | Medium | LWS creates validation-only runtime road types, not vendor presets. |
| Create road | `ERRoadNetwork.CreateRoad(string, ERRoadType, Vector3[])` | Runtime/public | Yes | No | Medium | Used for paired carriageways, ramp, and crossover. |
| Build meshes | `ERRoadNetwork.BuildRoadNetwork()` | Runtime/public | Yes | No | Medium | Fallback overload used if bool overload is unavailable. |
| Build without terrain extras | `ERRoadNetwork.BuildRoadNetwork(bool, bool, bool)` | Runtime/public | Yes | No | Medium | Called as `false, false, false` to avoid splatmap/tree/detail mutation. |
| Hide white surfaces | `ERRoadNetwork.HideWhiteSurfaces(bool)` | Runtime/public | Yes | No | Low | Non-critical visual cleanup. |
| Restore generated network | `ERRoadNetwork.RestoreRoadNetwork()` | Runtime/public | Yes | No | Medium | Called on destroy where a network exists. |
| Road resolution | `ERRoad.SetResolution(float)` | Runtime/public | Yes | No | Low | Development value only. |
| Mesh collider | `ERRoad.SetMeshCollider(bool)` | Runtime/public | Yes | No | High | Must be verified with NWH truck/trailer wheels. |
| Terrain deformation | `ERRoad.SetTerrainDeformation(bool)` | Runtime/public | Yes | No | High | Explicitly disabled for Prompt 009. |
| Terrain snapping | `ERRoad.SnapToTerrain(bool)` | Runtime/public | Yes | No | Medium | Disabled because the scene uses dedicated validation geometry. |
| Marker export | `ERRoad.GetMarkerPositions()` | Runtime/public | Yes | No | Low | Reflected by `LwsEasyRoadsExportBoundary`. |
| Centerline export | `ERRoad.GetSplinePointsCenter()` | Runtime/public | Yes | No | Low | Preferred for dense graph samples when available. |
| Road list export | `ERRoadNetwork.GetRoads()` / `GetRoadObjects()` | Runtime/public | Yes | No | Low | Reflected, no concrete dependency from gameplay. |
| URP package import | `URP_17_2_0.unitypackage` | Editor/import | No | No | Medium | Not imported because no exact URP 17.4 package was confirmed. |
