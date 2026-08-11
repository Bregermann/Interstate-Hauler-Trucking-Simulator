# Interstate Hauler - Prompt 002 Handoff

Status: READY WITH PRE-FLIGHT DECISIONS  
From: Prompt 001 full project/vendor audit  
Date: 2026-08-11

## Baseline

- Repository: `F:\Unity Projects\Interstate-Hauler-Trucking-Simulator`
- Branch: `main`
- Baseline commit: `a38a62052d2460f24a9dc7e815e26eb2dbf02b5e`
- Baseline commit message: `pre-codex-vendor-baseline-2026-08-11`
- Git tag: none found
- Initial working tree: task log recorded a non-clean starting state
- Final validation: only the four Prompt 001 documentation files are untracked
- Unity: `6000.4.10f1`
- Package state: URP 17.4.0, Input System 1.19.0, Splines 2.9.0, VFX Graph 17.4.0, NWH VP2 13.6, EasyRoads3D v3.2.4f5, Vista 3000.2.0, Weather Maker 8.0.9

Prompt 001 made documentation-only changes. Do not assume any vendor package, scene, prefab, or project setting was repaired by Prompt 001.

## Recommended Prompt 002 Objective

Create the project-owned LWS integration shell and preflight validators for Interstate Hauler without editing vendor assets.

The first implementation prompt should establish ownership boundaries and a bootstrap path, not build full gameplay. A good Prompt 002 target is:

1. Create a project-owned namespace and folder, for example `Assets/LWS/InterstateHauler`.
2. Add service contracts and lightweight adapters for vehicle input, transmission, road graph, weather, save, navigation, and bootstrap lifecycle.
3. Create or prepare a project-owned bootstrap scene only if project-setting edits are allowed.
4. Add editor/runtime validation that reports, but does not silently fix, the major setup issues found in Prompt 001.

## Do Not Do In Prompt 002 Unless Explicitly Authorized

- Do not edit files under vendor roots such as `Assets/NWH`, `Assets/EasyRoads3D`, `Assets/UTS_FullPack`, `Assets/WeatherMaker`, `Assets/NOT_Lonely`, `Assets/Plugins/Kronnect`, `Assets/Plugins/Pixel Crushers`, `Assets/Plugins/Demigiant`, `Assets/PinwheelStudio`, `Assets/RiverModeler`, or `Packages/xyz.staggart-creations.spline-spawner`.
- Do not upgrade Unity packages or Asset Store packages.
- Do not import optional `.unitypackage` files.
- Do not delete vendor demo scenes or assets.
- Do not mutate EasyRoads terrain at runtime without a restore/backup policy.
- Do not ship or store API keys in Unity assets.
- Do not commit automatically if unrelated user changes remain in the working tree.

## Pre-Flight Decisions Needed

| Decision | Recommended answer |
| --- | --- |
| Render pipeline | Activate a URP asset intentionally, likely `Assets/Settings/PC_RPAsset.asset` for Standalone. |
| Color space | Strongly consider switching to Linear during render setup; if deferred, record as a visual QA risk. |
| Build scene | Create or select a project-owned bootstrap scene and add it to build settings. |
| Missing scripts | Replace or clean `Assets/Settings/DefaultVolumeProfile.asset` before relying on it. |
| Save root | Create `LwsSaveService`; vendors provide child payloads. |
| Vehicle authority | NWH Vehicle Physics 2 owns player truck and trailers. |
| Transmission | Build an LWS 18-speed range/splitter adapter over NWH manual/external shift hooks. |
| Wheel hardware | Treat wheel/FFB as a future adapter because optional NWH steering-wheel package is not imported. |
| Road authority | EasyRoads owns authored road geometry; LWS road graph owns routing data. |
| Terrain authority | Vista owns terrain/biome generation. |
| Weather authority | Weather Maker owns weather/time/wind; Weatherade follows for wetness/snow. |
| GPS/minimap | Compass displays POIs/routes; LWS road graph computes truck routes. |
| Traffic | UTS is a reference/content source for NPC traffic and pedestrians only. |
| Vegetation | Decide whether Vista owns vegetation for now because planned Vegetation Spawner FREE is not installed. |

## Suggested LWS Architecture

Suggested namespace: `LWS.InterstateHauler`

Suggested folders:

- `Assets/LWS/InterstateHauler/Core`
- `Assets/LWS/InterstateHauler/Bootstrap`
- `Assets/LWS/InterstateHauler/Vehicles`
- `Assets/LWS/InterstateHauler/Input`
- `Assets/LWS/InterstateHauler/Roads`
- `Assets/LWS/InterstateHauler/Navigation`
- `Assets/LWS/InterstateHauler/Weather`
- `Assets/LWS/InterstateHauler/Save`
- `Assets/LWS/InterstateHauler/Traffic`
- `Assets/LWS/InterstateHauler/World`
- `Assets/LWS/InterstateHauler/Editor`

Suggested first contracts:

- `ILwsService` with explicit initialize/shutdown lifecycle.
- `ILwsSaveParticipant` for vendor and project save payloads.
- `ILwsVehicleInputSource` for hardware/gamepad/keyboard abstraction.
- `ILwsTruckTransmission` for gear, range, splitter, clutch intent, and save state.
- `ILwsRoadGraph` for nodes, edges, road samples, restrictions, and route queries.
- `ILwsNavigationRoutePresenter` for feeding Compass route points/POIs.
- `ILwsWeatherState` and `ILwsWeatherCoordinator` for Weather Maker to Weatherade flow.
- `ILwsWorldGenerationCoordinator` for Vista/EasyRoads/Spline/vegetation ordering.

## Suggested Prompt 002 Implementation Order

1. Add a project-owned asmdef and namespace only under `Assets/LWS/InterstateHauler`.
2. Add a bootstrap service registry with deterministic initialization order.
3. Add validators that report:
   - No active URP asset.
   - Empty build settings.
   - Missing scripts in `DefaultVolumeProfile.asset`.
   - Missing LFS policy patterns.
   - Duplicate vendor managers if scenes contain them.
4. Add NWH input/transmission adapter stubs that compile but do not replace gameplay yet.
5. Add road graph data models and an EasyRoads export stub.
6. Add Compass presentation adapter stub that accepts LWS route points.
7. Add Weather Maker to Weatherade coordinator stub.
8. Add save payload interfaces and vendor payload placeholders.
9. Add tests or editor validation for service registration and data-model serialization.

## Vendor-Specific Handoff Notes

NWH:

- Use NWH for player truck/trailer physics.
- Do not use UTS `CarMove` or `AddTrailer` for the player.
- Built-in H-shifter only covers reverse, neutral, and gears 1-8; create a trucking adapter.
- Optional steering-wheel/force-feedback support is not imported.

EasyRoads:

- Use as road geometry/source data.
- Export or sample center/side spline points into an immutable LWS road graph.
- Avoid runtime terrain mutation until backed by restore/backup policy.

Vista:

- Use as terrain/biome authority.
- Coordinate generation order before roads, props, traffic, and navigation.

Weather Maker and Weatherade:

- Weather Maker owns weather state.
- Weatherade owns visual accumulation only.
- Resolve URP setup before enabling Weatherade URP code paths.

Compass:

- Use for POI/minimap/GPS display.
- Feed routes from LWS road graph, not from generic NavMesh pathing.
- Save Compass state as a child payload.

Pixel Crushers:

- Dialogue System can own dialogue and quest state.
- Pixel Crushers Save System should be adapted into the simulator save root, not replace it.
- Scene Streamer should be behind an LWS facade if used.
- Runtime OpenAI is disabled unless `USE_OPENAI` is defined and a secure key flow exists.

UTS:

- Use as traffic and pedestrian reference/content only.
- Do not let UTS pathing become the road graph.
- Create tag/layer policy before using UTS logic in project scenes.

## Acceptance Criteria For Prompt 002

- New project-owned code is under `Assets/LWS/InterstateHauler` or another explicitly chosen project namespace.
- Vendor roots remain unmodified unless the user explicitly authorizes a specific vendor setup action.
- Project compiles in Unity after changes.
- Validators produce actionable reports for render pipeline, build scenes, missing scripts, and service ownership.
- NWH player/trailer authority, EasyRoads road authority, Vista terrain authority, Weather Maker weather authority, Weatherade accumulation authority, Compass display authority, and LWS save authority are all encoded in code or documentation.
- Git status is reviewed before commit; unrelated `Documents/` remains uncommitted unless user directs otherwise.

## Prompt 002 Suggested Opening Text

Implement the LWS integration shell for Interstate Hauler based on Prompt 001. Keep vendor assets untouched. Create project-owned service contracts, bootstrap lifecycle, validation checks, and stubs for NWH input/transmission, EasyRoads road graph export, Compass route presentation, Weather Maker to Weatherade coordination, and simulator save payloads. Do not build gameplay yet. Validate compile and confirm no vendor roots changed.
