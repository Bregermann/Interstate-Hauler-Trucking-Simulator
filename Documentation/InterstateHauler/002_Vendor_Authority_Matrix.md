# Interstate Hauler - Vendor Authority Matrix

This matrix is governed by the mandatory Vendor Asset First Rule in `Documentation/InterstateHauler/000_Vendor_Asset_First_Rule.md`. Future work must audit relevant installed assets before creating custom functionality, use vendor systems wherever they provide the requested capability or a strong foundation, and report why any relevant installed asset was not used.

| Responsibility | Authoritative System | LWS Interface/Adapter | Non-Authoritative Systems | Reason | Future Prompt |
| --- | --- | --- | --- | --- | --- |
| Player tractor physics | NWH Vehicle Physics 2 | `LwsNwhVehicleAdapter` | UTS, DOTween, hand-rolled Rigidbody controllers | NWH owns drivetrain, Rigidbody, wheel, engine, clutch, and modules. | Vehicle integration |
| Player trailer physics | NWH Vehicle Physics 2 | `LwsNwhVehicleAdapter`, future trailer save participant | UTS `AddTrailer` | NWH trailer and hitch modules match player physics authority. | Trailer integration |
| Player vehicle input abstraction | LWS | `ILwsVehicleInputSource`, `ILwsVehicleInputService`, `LwsNwhVehicleInputProvider` | NWH input providers as root authority, Heat controller presets | LWS must support keyboard, gamepad, wheel, and truck controls behind one contract. | Input foundation |
| Truck transmission gameplay | LWS over NWH | `ILwsTruckTransmission`, `LwsNwhTransmissionAdapter` | NWH stock H-shifter alone | NWH stock shifter covers 1-8; trucking range/splitter needs LWS mapping. | Transmission prompt |
| Road geometry / authoring | EasyRoads3D | `LwsEasyRoadsExportBoundary` | UTS paths, Compass route points, Spline Spawner | EasyRoads is the installed road authoring/build system. | Road tooling |
| Gameplay road graph | LWS | `LwsRoadGraph`, `LwsRoadNode`, `LwsRoadEdge` | EasyRoads runtime objects, UTS paths, Compass, NavMesh | Routing, saves, jobs, GPS, and traffic need stable project-owned IDs. | Road graph/routing |
| GPS route computation | LWS | `ILwsNavigationService` | Compass, Unity NavMesh | Truck-legal routing needs road graph restrictions and costs. | Navigation |
| GPS/minimap/route display | Compass | `ILwsNavigationRoutePresenter`, `LwsCompassRoutePresenter` | LWS UI, NavMesh | Compass is strong presentation but not canonical routing. | Navigation presentation |
| Terrain / biomes | Vista | `ILwsWorldGenerationCoordinator` | EasyRoads terrain mutation, ad hoc terrain scripts | Vista is the procedural terrain/biome authority. | World generation |
| Vegetation | Vista provisional | `LwsWorldGenerationPlan` | Vegetation Spawner FREE | Prompt 001 found Vegetation Spawner FREE is not installed. | Vegetation/world prompt |
| Weather / time / wind | Weather Maker | `ILwsWeatherCoordinator`, `LwsWeatherMakerWeatheradeAdapter` | Weatherade, ad hoc weather scripts | Weather Maker owns global weather state and zones. | Render/weather foundation |
| Wetness / snow accumulation visuals | Weatherade | `LwsWeatherMakerWeatheradeAdapter` | Weather Maker surface effects alone | Weatherade specializes in surface accumulation. | Render/weather foundation |
| Traffic / pedestrians | UTS runtime content | `ILwsTrafficService`, `LwsTrafficTagLayerPolicy` | UTS as route owner, Compass, LWS jobs | UTS can provide traffic behavior/content but not canonical topology. | Traffic prompt |
| Dialogue / quest state | Pixel Crushers Dialogue System | LWS save child participant placeholder | LWS root save | Pixel Crushers owns dialogue/quest semantics, not full simulator state. | Narrative prompt |
| Root save orchestration | Pixel Crushers Save System | `ILwsSaveService`, `ILwsSaveParticipant`, `LwsPixelCrushersSemanticSaver` | Custom LWS storage, Compass save demos, vendor-specific file paths | Prompt 016 selected Pixel Crushers as the single production save authority; LWS owns semantic payloads and profile/slot mapping only. | Persistence foundation |
| Scene streaming | LWS facade over Scene Streamer if adopted | `ILwsWorldStreamingService`, `LwsSceneStreamerAdapter` | Direct Scene Streamer calls from gameplay | Keeps chunk/scene ownership replaceable. | Streaming/world prompt |
| UI presentation | Heat - Complete Modern UI | Future LWS UI presenters | Vendor demo UI state | Heat provides UI controls/visuals; LWS owns game state. | UI prompt |
| Tweening | DOTween | Future UI animation usage | Physics movement | DOTween is utility animation, not gameplay state or physics. | UI polish |
| Roadside prop distribution | Spline Spawner | Future world/roadside adapter | Road authority, terrain authority | Spline Spawner distributes objects after roads/terrain are known. | World dressing |
| Rivers / water geometry | River Modeler | Future world-water adapter | Terrain or road authority | Authoring helper for spline water, not world generation authority. | World art |
| Path/terrain painting | Path Painter II / TotalBrush | Future art tooling boundary | Runtime road/terrain authority | Editor/content helper only. | World art |
| Shift UI | Reference/fallback only | None yet | Heat UI | Prompt 001 did not find Shift installed. | UI prompt if imported |
