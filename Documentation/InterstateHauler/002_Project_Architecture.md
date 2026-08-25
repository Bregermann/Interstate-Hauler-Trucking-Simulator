# Interstate Hauler - LWS Project Architecture

Prompt: 002 - LWS Project Architecture + Vendor Authority Rules  
Date: 2026-08-11  
Unity: 6000.4.10f1  
Namespace: `LWS.InterstateHauler`

## 1. Overview

Prompt 002 establishes the project-owned architecture shell under `Assets/LWS/InterstateHauler/`. It does not implement trucking gameplay. The purpose is to make future prompts build against stable LWS-owned contracts instead of directly coupling production systems to vendor demos or package internals.

The core rule is one authority per responsibility:

- NWH owns player tractor/trailer physics.
- EasyRoads owns road geometry and authoring.
- LWS owns road graph and routing data.
- Vista owns terrain and biomes.
- Weather Maker owns weather, time, and wind.
- Weatherade owns visual surface accumulation.
- Compass owns navigation presentation only.
- UTS is traffic/pedestrian runtime content only.
- Pixel Crushers owns dialogue/quest state only.
- LWS owns root save orchestration.

The project also follows the mandatory Vendor Asset First Rule documented in `Documentation/InterstateHauler/000_Vendor_Asset_First_Rule.md`. Before creating substantial custom functionality, future prompts must audit relevant installed commercial assets and use vendor solutions wherever they provide the complete feature, most of the feature, or an appropriate foundation. If a relevant installed asset is not used, the prompt must explain why.

## 2. Namespace And Folder Organization

Runtime root:

`Assets/LWS/InterstateHauler/`

Required project-owned folders were created:

- `Bootstrap`
- `Core`
- `Input`
- `Vehicles`
- `Roads`
- `Navigation`
- `Weather`
- `Save`
- `Traffic`
- `World`
- `Data`
- `Tests/EditMode`
- `Tests/PlayMode`
- `Editor`

NWH-specific shells are under `Assets/LWS/InterstateHauler/Vehicles/NWH/`. EasyRoads export shells are under `Assets/LWS/InterstateHauler/Roads/EasyRoads/`.

## 3. Assembly Layout

Runtime assembly:

- `Assets/LWS/InterstateHauler/LWS.InterstateHauler.Runtime.asmdef`
- References NWH assemblies by GUID because NWH has asmdefs.
- Does not directly reference Compass, Weather Maker, Weatherade, UTS, Pixel Crushers, EasyRoads scripts, or Scene Streamer, because those packages are either compiled into predefined assemblies or should remain behind reflection/facade boundaries.

Editor assembly:

- `Assets/LWS/InterstateHauler/Editor/LWS.InterstateHauler.Editor.asmdef`
- References the runtime assembly.
- Contains the project validation menu and report code.

Test assemblies:

- `Assets/LWS/InterstateHauler/Tests/EditMode/LWS.InterstateHauler.Tests.EditMode.asmdef`
- `Assets/LWS/InterstateHauler/Tests/PlayMode/LWS.InterstateHauler.Tests.PlayMode.asmdef`

## 4. Service Lifecycle

The service lifecycle is defined in `LwsServiceLifecycle.cs`.

Core types:

- `ILwsService`
- `LwsServiceRegistry`
- `LwsServiceContext`
- `LwsServiceState`
- `LwsServiceResult`
- `LwsServiceDiagnostic`

Lifecycle states:

- `Uninitialized`
- `Initializing`
- `Ready`
- `ShuttingDown`
- `Shutdown`
- `Failed`

The registry uses explicit registration. It rejects duplicate service types and duplicate service IDs. Initialization order is deterministic and follows the registration list. Dependencies are declared as service types and must have initialized earlier in the explicit order.

Shutdown runs in reverse initialization order.

## 5. Bootstrap Lifecycle

Bootstrap class:

`Assets/LWS/InterstateHauler/Bootstrap/LwsApplicationBootstrap.cs`

Bootstrap scene:

`Assets/LWS/InterstateHauler/Bootstrap/Bootstrap.unity`

Build settings:

The project-owned bootstrap scene is scene index 0. No vendor demo scenes were added.

Bootstrap behavior:

- Prevents duplicate bootstrap instances.
- Optionally persists with `DontDestroyOnLoad`.
- Constructs the default service registry.
- Initializes services explicitly.
- Shuts down services in controlled reverse order.

## 6. Vendor Authority Table

| Responsibility | Authority | LWS Boundary |
| --- | --- | --- |
| Player vehicle physics | NWH Vehicle Physics 2 | `LwsNwhVehicleAdapter`, `LwsNwhVehicleInputProvider` |
| Player input abstraction | LWS | `ILwsVehicleInputSource`, `ILwsVehicleInputService` |
| Truck transmission abstraction | LWS over NWH | `ILwsTruckTransmission`, `LwsNwhTransmissionAdapter` |
| Road geometry | EasyRoads3D | `LwsEasyRoadsExportBoundary` |
| Road graph/routing data | LWS | `LwsRoadGraph`, `LwsRoadNode`, `LwsRoadEdge` |
| GPS/minimap display | Compass | `ILwsNavigationRoutePresenter`, `LwsCompassRoutePresenter` |
| Weather/time/wind | Weather Maker | `LwsWeatherMakerWeatheradeAdapter` |
| Wetness/snow visuals | Weatherade | `LwsWeatherMakerWeatheradeAdapter` |
| Traffic/pedestrians | UTS content | `ILwsTrafficService` |
| Dialogue/quests | Pixel Crushers | Save child participant placeholder |
| Scene streaming | Scene Streamer behind LWS | `LwsSceneStreamerAdapter` |
| UI | Heat UI | Future UI consumes LWS state |

## 7. Save Architecture

LWS owns simulator-level save orchestration.

Created:

- `ILwsSaveService`
- `ILwsSaveParticipant`
- `ILwsSaveStorage`
- `LwsSaveService`
- `LwsSaveSnapshot`
- `LwsSaveParticipantState`
- `LwsInMemorySaveStorage`

Current placeholder participants:

- `pixel-crushers.dialogue`
- `compass.navigator`
- `vehicle.truck`
- `world.state`
- `weather.state`
- `jobs.state`

These placeholders establish stable child payload slots without implementing the final career save system.

Physical storage remains abstract. No gameplay system calls `System.IO`, `Application.persistentDataPath`, Steam APIs, or console APIs.

## 8. Input Architecture

Created:

- `ILwsVehicleInputSource`
- `ILwsVehicleInputService`
- `LwsVehicleContinuousInput`
- `LwsVehicleCommandFrame`
- `LwsTruckGearIntent`
- `LwsVehicleInputService`

The model separates continuous controls from momentary/button command intents.

Continuous controls include steering, throttle, brake, clutch, and parking brake. Command frame supports ignition, horn, lights, indicators, wipers, cruise control, engine brake, retarder, differential lock, and trailer attach/detach.

No Logitech wheel, force feedback, or optional NWH wheel package was imported.

## 9. Vehicle / NWH Boundary

Created:

- `LwsNwhVehicleAdapter`
- `LwsNwhVehicleInputProvider`
- `LwsVehicleTelemetry`
- `ILwsVehicleRuntimeService`
- `LwsVehicleRuntimeService`

`LwsNwhVehicleAdapter` reads public NWH APIs for speed, engine RPM, current gear, clutch input, engine running/stalled state, and optional trailer-hitch attachment state.

`LwsNwhVehicleInputProvider` inherits NWH `VehicleInputProviderBase` and feeds NWH from an LWS input source. It only maps NWH stock shifter values for reverse, neutral, and gears 1-8.

## 10. Transmission Boundary

Created:

- `ILwsTruckTransmission`
- `LwsTransmissionState`
- `LwsTransmissionMode`
- `LwsNwhTransmissionAdapter`

Prompt 001 found that NWH stock H-shifter support only covers reverse, neutral, and gears 1-8. The LWS transmission layer therefore represents physical shifter gate, range, splitter, logical gear, clutch, neutral, reverse, and engine-stall state.

Final 18-speed range/splitter logic is deliberately deferred.

## 11. Road Graph Ownership

Created:

- `LwsRoadGraph`
- `LwsRoadNode`
- `LwsRoadEdge`
- `LwsRoadSample`
- `LwsRoadRestriction`
- `LwsRoadClass`
- `LwsRouteRequest`
- `LwsRouteResult`

The graph is serialization-safe and uses stable IDs. It supports node/edge validation and JSON round-trip through `JsonUtility`.

No U.S. map data or production routing algorithm was added.

## 12. EasyRoads Export Boundary

Created:

- `ILwsEasyRoadsRoadGraphExporter`
- `LwsEasyRoadsExportBoundary`
- `LwsEasyRoadsExportAdapter`
- `LwsEasyRoadsExportOptions`
- `LwsEasyRoadsExportReport`

This shell defines EasyRoads-to-LWS export ownership. It does not mutate terrain, execute EasyRoads build/finalize steps, or generate production graph data.

## 13. Compass Presentation Boundary

Created:

- `ILwsNavigationService`
- `ILwsNavigationRoutePresenter`
- `LwsNavigationService`
- `LwsCompassRoutePresenter`

LWS owns route computation. Compass receives route waypoints for display. `LwsCompassRoutePresenter` uses reflection to avoid hard assembly coupling and invokes Compass route display APIs if present.

NavMesh is not used as authoritative trucking routing.

## 14. Weather Ownership

Created:

- `ILwsWeatherState`
- `LwsWeatherState`
- `ILwsWeatherCoordinator`
- `LwsWeatherCoordinator`
- `LwsWeatherMakerWeatheradeAdapter`

Weather state currently includes weather ID, precipitation type/intensity, temperature, wetness, snow amount, wind velocity, visibility, and world time ticks.

Weather Maker remains the future weather/time/wind authority. Weatherade follows for surface wetness and snow visuals.

## 15. Traffic Ownership

Created:

- `ILwsTrafficService`
- `LwsTrafficService`
- `LwsTrafficProfile`
- `LwsTrafficTagLayerPolicy`

UTS remains traffic/pedestrian runtime content only. It does not own canonical road graph, route computation, or player routing. The tag/layer policy records the known UTS tags from Prompt 001 without changing project tags or layers.

## 16. World Generation Ordering

Created:

- `ILwsWorldGenerationCoordinator`
- `LwsWorldGenerationCoordinator`
- `LwsWorldGenerationPlan`
- `LwsWorldGenerationStep`

Default future order:

1. World/chunk definition
2. Vista terrain and biomes
3. EasyRoads geometry
4. LWS road graph export
5. Roadside spawning
6. Vegetation
7. Navigation
8. Traffic
9. Weather regions
10. Scene streaming validation

Vegetation Spawner FREE was not installed in Prompt 001, so Vista remains provisional vegetation authority.

## 17. Scene Streaming Facade

Created:

- `ILwsWorldStreamingService`
- `LwsWorldStreamingService`
- `LwsSceneStreamerAdapter`

Scene Streamer stays behind an LWS facade. No continent streaming, floating origin, or production chunk loading was implemented.

## 18. Initialization Order

Default bootstrap order:

1. Save/profile infrastructure: `lws.save`
2. Input abstraction: `lws.input.vehicle`
3. Weather/time coordination: `lws.weather`
4. World streaming: `lws.world.streaming`
5. Road/navigation data: `lws.navigation`
6. Traffic: `lws.traffic`
7. World generation coordination: `lws.world.generation`
8. Player vehicle runtime: `lws.vehicle.runtime`

Why this order:

- Save starts early so later systems can register stable payloads and profile context.
- Input starts before vehicle runtime so player input can be routed into NWH adapters.
- Weather starts before world generation because weather regions and surface accumulation depend on project state.
- Streaming starts before navigation/traffic because route and traffic data will eventually be chunk-scoped.
- Navigation starts before traffic because traffic should consume route/road graph metadata, not own it.
- Vehicle runtime starts after input so it can publish state from NWH without owning input.

## 19. Validation Framework

Created:

`Assets/LWS/InterstateHauler/Editor/LwsProjectValidator.cs`

Menu:

`Interstate Hauler/Validate Project`

Validators report:

- Graphics render pipeline status.
- Quality render pipeline status.
- Intended `Assets/Settings/PC_RPAsset.asset` existence.
- Color-space status.
- Bootstrap scene index 0.
- Missing scripts in `Assets/Settings/DefaultVolumeProfile.asset`.
- Service registration and initialization/shutdown.
- Vendor markers in LWS-owned scenes.
- Save participant ID uniqueness.
- Road graph validation and JSON round-trip.
- LWS scripts accidentally placed under vendor roots.

Validators report only. They do not activate URP, change color space, repair profiles, import packages, or edit vendor assets.

## 20. Test Coverage

EditMode tests:

- Duplicate service registration rejection.
- Deterministic initialization order.
- Reverse shutdown order.
- Save participant uniqueness.
- Road graph serialization/deserialization.
- Road node/edge ID validation.
- Transmission state serialization safety.
- Validator returns a useful report.

PlayMode tests:

- Bootstrap initializes once.
- Duplicate bootstrap is rejected.
- Registered services reach ready state.
- Shutdown does not throw.

## 21. Known Deferred Issues

- URP is not activated yet.
- Color space remains unchanged.
- `DefaultVolumeProfile.asset` missing script references are reported but not fixed.
- No full route solving exists yet.
- No final 18-speed transmission logic exists yet.
- No force feedback or wheel hardware integration exists yet.
- No production traffic behavior exists yet.
- No world generation or streaming implementation exists yet.
- No final physical save storage exists yet.

## 22. Prompt 003 Recommendations

Prompt 003 should focus on URP / Quality / Platform Foundation.

Recommended scope:

- Activate the intended Standalone URP asset if approved.
- Decide Linear color space.
- Resolve or replace `DefaultVolumeProfile.asset`.
- Validate Weather Maker and Weatherade URP support.
- Validate EasyRoads URP material support against URP 17.4.
- Validate quality levels for PC and Steam Deck.
- Keep Bootstrap scene as build index 0.
- Do not implement gameplay.
