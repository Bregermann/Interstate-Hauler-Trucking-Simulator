# Prompt 015 - Coordinate Matrix

| System | Canonical Coordinate Type | Local Coordinate Type | Conversion Owner | Origin Aware | Persistent | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| World origin | `LwsWorldPositionD` | `Vector3` | `ILwsWorldOriginService` | Yes | Yes | Owns accumulated double origin offset. |
| Player truck telemetry | Global `Vector3` derived from `LwsWorldPositionD` | `Transform.position` | `LwsNwhVehicleAdapter` via origin service | Yes | Future | Existing telemetry shape remains compatible. |
| Player tractor physics | NWH local physics | Rigidbody/Transform local | Floating-origin participant | Yes | No | Save should use global position, not Rigidbody local position. |
| Player trailer physics | NWH local physics | Rigidbody/Transform local | Floating-origin participant | Yes | No | Attachment identity remains separate. |
| Streaming anchor | `LwsWorldPositionD` global tractor/trailer | local tractor/trailer copies | `LwsWorldStreamingCoordinator` | Yes | No | Streaming policy sees continuous global position. |
| Chunk manifest | Global `Vector3` bounds stored in assets | local bounds derived at runtime | `LwsWorldChunkDefinition` helpers | Yes | Yes | Canonical chunk bounds are not rebased. |
| Chunk scene root | Manifest global center | `Transform.position` | `LwsStreamedChunkSceneRoot` | Yes | No | New chunks align to current origin. |
| Road graph | Stable road IDs and graph sample positions | Debug/presentation only | Query caller through origin service | Yes | Yes | IDs must not change after shifts. |
| Road-condition lookup | Road graph/global position | Player transform local | `LwsRoadConditionRuntimeController` | Yes | Yes | Weather/ice state remains semantic. |
| Navigation route | Road graph/global route state | Cab/map presentation | `LwsCabGpsController` and navigation debug panel | Yes | Yes | Avoid false off-route from local rebase. |
| GPS map graphic | Route semantic/global source | Rendered cab UI presentation | Cab GPS controller | Partial | No | Manual visual route validation pending. |
| UTS lane samples | LWS global lane geometry | UTS local path points | `LwsUtsTrafficApi` and controller conversion delegates | Yes | Yes for lane IDs | UTS still owns movement. |
| UTS vehicles | Registry identity/global comparisons | Rigidbody/Transform local | `LwsUtsHighwayTrafficController` | Yes | No | Active cars shift as dynamic bodies. |
| Weather Maker | Weather state/service data | Camera/scene presentation | Existing weather service | Unchanged | Yes | No origin-specific global state added. |
| Weatherade | Road-condition semantic state | Coverage materials/targets | `LwsWeatheradeAdapter` | Yes | No | Rebinds after origin shift. |
| Save system future | `LwsWorldPositionD` | Spawn local from global minus offset | Prompt 016 | Prepared | Yes | Do not save raw local `Transform.position` as world position. |
