# Interstate Hauler - Project and Vendor Audit

Prompt: 001 - Full Project and Vendor Audit  
Audit date: 2026-08-11  
Repository: `F:\Unity Projects\Interstate-Hauler-Trucking-Simulator`  
Branch audited: `main`  
Audit stance: documentation only. No gameplay, vendor, scene, prefab, package, or project-setting changes were made.

## Executive Status

Prompt 002 readiness: READY WITH PRE-FLIGHT DECISIONS.

The project is ready for an implementation prompt that creates an integration shell and resolves foundational project setup, but it is not ready for gameplay production until several high-risk configuration items are addressed. The strongest installed systems are:

- NWH Vehicle Physics 2 / Wheel Controller 3D for the player truck, trailers, drivetrain, clutch, and wheel physics.
- EasyRoads3D for road construction and possible road-data export.
- Vista for procedural terrain and biome generation.
- Weather Maker for global weather, sky, day/night, wind, and weather zones.
- Weatherade for surface-level snow/rain accumulation and wetness, subordinate to Weather Maker.
- Compass Navigator Pro for GPS/minimap/POI/route rendering, fed by a separate road graph service.
- Urban Traffic System as a demo/reference traffic and pedestrian system only, not player vehicle authority.
- Pixel Crushers Dialogue System, Scene Streamer, and Save System as optional narrative/streaming/save components, not automatically global simulator authority.

The most important risks for Prompt 002 are render-pipeline activation, empty build settings, a missing-script profile asset, road/terrain generation order, and input/transmission ownership for trucking-grade gearboxes and hardware wheels.

## Repository Baseline

| Item | Audit finding |
| --- | --- |
| Current branch | `main` |
| Latest commit | `a38a62052d2460f24a9dc7e815e26eb2dbf02b5e` |
| Latest commit message | `pre-codex-vendor-baseline-2026-08-11` |
| Remote | `origin https://github.com/Bregermann/Interstate-Hauler-Trucking-Simulator.git` |
| Tags | None found |
| Initial working tree | Task log recorded a non-clean starting state |
| Final validation status | Only the four Prompt 001 documentation files are untracked; no tracked diffs in `Assets`, `Packages`, or `ProjectSettings` |
| Commit decision | Do not commit Prompt 001 artifacts automatically because the task log did not satisfy the clean-start rule |

The repository has a useful baseline-named commit, but it does not have a matching Git tag. The final explicit status check with `--untracked-files=all` reports only the four new documentation files from this task.

## Git Ignore and LFS

The `.gitignore` is broadly Unity-oriented. It ignores generated Unity folders such as `/Library/`, `/Temp/`, `/Obj/`, `/Build/`, `/Builds/`, `/Logs/`, `/UserSettings/`, and `.vs/`. It does not ignore `.idea/`.

Git LFS is configured narrowly. `git lfs ls-files --long` reports three tracked large assets:

| Path | Approx size | LFS status |
| --- | ---: | --- |
| `Assets/NOT_Lonely/Weatherade SRS/Textures/Snow_01_n_h_sm.tif` | 130.99 MiB | Tracked |
| `Assets/UTS_FullPack/Models/Cars/Textures/Car_4/1.psd` | 103.15 MiB | Tracked |
| `Assets/UTS_FullPack/Models/Cars/Textures/Car_4/полосы камаро1.psd` | 103.64 MiB | Tracked |

The current Git tree does not contain repository blobs larger than 100 MiB. However, `.gitattributes` contains stale or suspicious LFS patterns for ignored/generated `Library` paths:

- `Library/ArtifactDB`
- `Library/PackageCache/com.unity.burst@6bb9aca3ef38/.Runtime/libburst-llvm-19.dylib`
- `Library/Search/98957a664bd18c47a3e41b2a0189ef53.SearchIndexArtifactImporter.262146.b.index`

Recommended Prompt 002 action: clean the LFS policy before more binary imports. Track broad Unity binary classes such as `*.fbx`, `*.psd`, `*.tif`, large audio, and large textures through LFS, and remove generated `Library/*` LFS patterns.

## Unity Baseline

| Item | Audit finding |
| --- | --- |
| Unity editor version | `6000.4.10f1` |
| Revision | `feeafc12a938` |
| Serialization | Force Text (`m_SerializationMode: 2`) |
| Version control metadata | Visible/hidden meta files are inconsistent across settings files; `EditorSettings` says hidden meta files, `VersionControlSettings` says visible meta files |
| Product name | `18 Wheeler Simulator` |
| Project name | `UTS 2020` |
| Company | `DefaultCompany` |
| Bundle version | `0.1` |
| Color space | Gamma (`m_ActiveColorSpace: 0`) |
| Active input handler | Both legacy Input Manager and new Input System (`activeInputHandler: 2`) |
| Fixed timestep | 0.02 |
| Maximum allowed timestep | 0.33333334 |
| Physics solver iterations | 6 position, 1 velocity |
| Build settings scenes | Empty |

The Unity editor was already running for this project during the audit. I did not start a second batchmode Unity process because that can rewrite generated files and project state. The current Editor log was used for health checks instead.

## Package Manifest

Installed Unity packages in `Packages/manifest.json`:

| Package | Version |
| --- | --- |
| `com.unity.ai.navigation` | 2.0.12 |
| `com.unity.cinemachine` | 2.10.7 |
| `com.unity.collab-proxy` | 2.12.4 |
| `com.unity.editorcoroutines` | 1.1.0 |
| `com.unity.ide.rider` | 3.0.40 |
| `com.unity.ide.visualstudio` | 2.0.27 |
| `com.unity.inputsystem` | 1.19.0 |
| `com.unity.multiplayer.center` | 1.0.1 |
| `com.unity.postprocessing` | 3.5.4 |
| `com.unity.render-pipelines.universal` | 17.4.0 |
| `com.unity.splines` | 2.9.0 |
| `com.unity.test-framework` | 1.6.0 |
| `com.unity.timeline` | 1.8.12 |
| `com.unity.ugui` | 2.0.0 |
| `com.unity.visualeffectgraph` | 17.4.0 |
| `com.unity.visualscripting` | 1.9.11 |
| `xyz.staggart-creations.spline-spawner` | Embedded file package |

`packages-lock.json` confirms URP/Core/ShaderGraph/VFX at 17.4.0, Burst 1.8.29, Splines 2.9.0, and the embedded Spline Spawner package at `file:xyz.staggart-creations.spline-spawner`.

## Render Pipeline and Graphics

The project contains URP packages and URP assets, but no render pipeline asset is currently assigned in Graphics or Quality settings.

| Asset or setting | Audit finding |
| --- | --- |
| `ProjectSettings/GraphicsSettings.asset` | `m_CustomRenderPipeline: {fileID: 0}` |
| `ProjectSettings/QualitySettings.asset` | All quality levels have `customRenderPipeline: {fileID: 0}` |
| URP global settings | `GraphicsSettings` maps URP global settings to `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` |
| PC URP asset | `Assets/Settings/PC_RPAsset.asset`; renderer points to `PC_Renderer.asset` |
| Mobile URP asset | `Assets/Settings/Mobile_RPAsset.asset`; renderer points to `Mobile_Renderer.asset` |
| PC renderer | Universal Renderer with Screen Space Ambient Occlusion feature active; serialized rendering mode is `2`, likely Deferred |
| Mobile renderer | Universal Renderer with no renderer features; serialized rendering mode is `0`, likely Forward |

Important implications:

- Weather Maker 8.0.9 expects explicit URP enablement for URP projects. The project has Unity 6000.4 and URP 17.4, but no active render pipeline asset and no obvious `UNITY_URP` scripting define.
- Weatherade has URP support packages present, but active source code is gated by `USING_URP`, which is not currently in the Standalone define list.
- EasyRoads includes SRP support packages up through URP 17.2.0 and beta URP 17.3.0 materials, while the project is on URP 17.4.0. Material validation is required after URP activation.
- The project is in Gamma color space. NWH and Weather Maker documentation both favor Linear color space for final visuals.

Recommended Prompt 002 action: decide and activate the project render pipeline intentionally, likely `Assets/Settings/PC_RPAsset.asset` for Standalone, then validate Weather Maker, Weatherade, EasyRoads, Compass, UI materials, and demo scene rendering in Unity.

## Scripting Symbols

Standalone currently includes:

`CROSS_PLATFORM_INPUT;UNITY_POST_PROCESSING_STACK_V2;EASYROADS3D_PRO;VISTA;NWH_WC3D;NWH_NVP2;WEATHER_MAKER_PRESENT;TMP_PRESENT;DOTWEEN;DOTWEEN_UITOOLKIT`

Notable missing defines for planned integrations:

- `USING_URP` for Weatherade URP source paths.
- `UNITY_URP` or equivalent Weather Maker URP enablement define, depending on vendor menu setup.
- `WEATHERADE_INCLUDED`, which Weatherade's version-check editor script can add when it detects version changes.
- `USE_OPENAI`, which gates the Pixel Crushers OpenAI runtime code.

Android and console build targets have smaller define sets. If multi-platform support matters, Prompt 002 should normalize define symbols per target group rather than only Standalone.

## Build Settings and Scenes

`ProjectSettings/EditorBuildSettings.asset` has no scenes in its `m_Scenes` list. This blocks build validation and also affects vendor examples that require scene names in build settings.

Representative scene inventory:

| Area | Scenes |
| --- | --- |
| Project root | `Assets/Scenes/SampleScene.unity` |
| NWH Vehicle Physics 2 | `NVP2 Main Demo`, `NVP2 Mobile Demo`, runtime spawn/setup/teleport/reload test scenes |
| NWH Wheel Controller | `WC3D Main Demo` |
| UTS Full Pack | Car, crossroad, one-way, semaphore, terrain, night, pedestrian, bike, and ragdoll demos |
| EasyRoads3D | `scene main`, runtime existing-road-network, runtime new-road-network |
| Vista | Hex map and multi-layer biome blending demos |
| Weather Maker | URP demo, precipitation, weather API, weather zones, rolling storm, mirror, origin offset, wetness, and other demos |
| Weatherade | Basic rain and snow setup samples |
| Compass Navigator Pro | Main demo and split-screen demo |
| Pixel Crushers Dialogue System | Dialogue demos and Camera Angle Studio |
| Pixel Crushers Scene Streamer | Start scene plus Scene 1 through Scene 6 examples |
| Heat UI | Main menu and in-game scenes for desktop, cross-platform, and console |
| River Modeler | River demo |
| Spline Spawner | Embedded package demo |
| Path Painter II | Editor demo scene |
| TotalBrush | Painter scene under Resources |

Recommended Prompt 002 action: create or choose a non-vendor bootstrap scene and add it to build settings only after render pipeline and missing-script profile cleanup are decided.

## Static Project Health

Current Editor log check:

- Current Editor log reported successful Mono assembly reload.
- No `error CS`, `warning CS`, or `Compilation failed` matches were found in the current `Editor.log`.
- No batchmode compile was run because the Unity editor was already open.

Static YAML scan:

- Exact `m_Script: {fileID: 0}` occurrences were found only in `Assets/Settings/DefaultVolumeProfile.asset`.
- The missing entries appear to be Render Pipeline Core editor test volume components, including `VolumeComponentCopyPasteTests/CopyPasteTestComponent2` style residues.
- Broad zero GUID hits in demo scenes were normal empty references, not confirmed missing MonoBehaviour scripts.

Recommended Prompt 002 action: replace or clean `DefaultVolumeProfile.asset` before using it as an active volume profile. Do not edit vendor demo scenes as part of this cleanup.

## Asset and Vendor Inventory

The complete CSV inventory is in `Documentation/InterstateHauler/001_Asset_Inventory.csv`. Key installed third-party assets:

| Asset | Version evidence | Recommended authority |
| --- | --- | --- |
| NWH Vehicle Physics 2 | 13.6 | Player truck, drivetrain, clutch, wheel, trailer physics |
| Wheel Controller 3D | Included with NWH VP2 13.6; no separate version marker found | Low-level wheel physics under NWH |
| EasyRoads3D Pro v3 | v3.2.4f5 | Road construction and road-network source |
| Urban Traffic System Full Pack | 2.0 | NPC traffic/pedestrian/semaphore reference only |
| Vista Personal | 3000.2.0 | Procedural terrain and biome generation |
| Weather Maker | 8.0.9 | Weather, sky, wind, time-of-day, weather zones |
| Weatherade: Snow and Rain System | 1.1.8 | Surface wetness/snow accumulation, driven by Weather Maker |
| Compass Navigator Pro 4 | 6.0.2 | GPS, minimap, POI, route visualization |
| Dialogue System for Unity | 2.2.73.2 | Dialogue, quests, NPC narrative state if needed |
| Dialogue System Addon for OpenAI | 1.0.37 | Optional editor/runtime AI dialogue generation; security-sensitive |
| Scene Streamer | 1.26.1 from readme | Optional additive scene streaming; not map authority by default |
| Heat - Complete Modern UI | 1.1.8 | UI component/prefab library |
| DOTween | 1.2.825 | Tweening utility |
| Spline Spawner | `package.json` 1.2.0; meta 1.2.1 | Roadside/object distribution after spline authority is chosen |
| River Modeler | `package.json` 1.0.5; meta 1.0.4 | Spline-based water geometry authoring |
| Path Painter II | Version log 2.1.13; meta showed 2.1.12 | Terrain/path painting authoring |
| TotalBrush | 1.1.8 metadata | Mesh/terrain vertex and mask painting authoring |

Planned or referenced assets not found as standalone installs:

- Vegetation Spawner FREE
- Shift - Complete Sci-Fi UI
- Weather Sound FX Loops
- MapMagic
- Terrain Composer
- Curvy
- Digger
- UniStorm
- Wingman
- Poly Few

## Version Mismatches

| Asset | Stronger evidence | Conflicting evidence | Risk |
| --- | --- | --- | --- |
| Spline Spawner | `package.json` version 1.2.0 | Unity `.meta` packageVersion 1.2.1 | Imported metadata and manifest disagree |
| River Modeler | `package.json` version 1.0.5 | Unity `.meta` packageVersion 1.0.4 | Imported metadata and manifest disagree |
| Path Painter II | Version log begins at 2.1.13 | Asset metadata sweep showed 2.1.12 | Possible partial update or stale metadata |
| Scene Streamer | `_README.txt` reports 1.26.1 | Asset metadata sweep showed 1.26 | Minor version marker mismatch |

Prompt 002 should avoid upgrading any vendor package during gameplay implementation. Log mismatches, then validate only the integration surface actually used.

## Authoritative System Boundaries

| Domain | Authority | Do not let this system own |
| --- | --- | --- |
| Player vehicle dynamics | NWH Vehicle Physics 2 | UTS `CarMove`, DOTween, hand-rolled Rigidbody controllers |
| Wheel physics | NWH Wheel Controller 3D through NWH vehicle setup | Independent WheelCollider traffic/player systems |
| Trailers | NWH Trailer and TrailerHitch modules | UTS `AddTrailer` for player trailers |
| NPC traffic/pedestrians | Future LWS traffic layer, optionally adapted from UTS concepts/assets | Player truck physics or core road graph |
| Roads | EasyRoads3D road network/export | UTS paths, Spline Spawner, Compass |
| Road graph/routing | New LWS road graph service generated from road data | Compass route rendering or UTS paths |
| Terrain/biomes | Vista | EasyRoads runtime terrain mutation as an uncontrolled source |
| Roadside props | Spline Spawner after road splines are stable | Terrain authority or road authority |
| Weather/time/wind | Weather Maker | Weatherade or ad hoc scripts |
| Surface accumulation | Weatherade, driven by Weather Maker state | Weather orchestration |
| GPS/minimap/POI route display | Compass Navigator Pro | Pathfinding, road graph, job economy |
| UI skin/components | Heat UI and UGUI | Gameplay state ownership |
| Tweening | DOTween | Physics motion or persistent gameplay state |
| Dialogue/quests | Pixel Crushers Dialogue System if used | Freight economy, save root, road graph |
| Save root | New LWS save service | Pixel Crushers SaveSystem, Compass, or NWH directly |
| Scene/map streaming | New LWS streaming decision; Scene Streamer only if adopted deliberately | Vendor demos or build settings side effects |

## NWH Vehicle Physics 2 and Wheel Controller 3D

NWH is the correct player vehicle authority. Its central type is `NWH.VehiclePhysics2.VehicleController`, supported by powertrain, input, module, damage, effects, sound, and Rigidbody systems. Wheel Controller 3D provides the lower-level wheel/tire behavior under `NWH.WheelController3D`.

Important capabilities:

- Vehicle dynamics, powertrain, torque flow, braking, damage, effects, sound, and modules.
- ICE and electric engine modes, idle/stall/rev limiter, start/stop, forced induction, and power/loss modifiers.
- Transmission modes: manual, automatic, CVT, external, and related shift controls.
- User clutch mode and configurable engagement/slip behavior.
- Differential settings including open/LSD/locked-style behavior.
- Trailer and trailer hitch modules with attach/detach events, configurable joint setup, break force, and trailer input/gear synchronization.
- New Input System, legacy Input Manager, and mobile input providers.
- Built-in H-shifter support for reverse, neutral, and gears 1-8.

Important constraints:

- Intended 18-speed trucking behavior needs a new range/splitter adapter. NWH's built-in H-shifter is not enough by itself.
- The optional SteeringWheelInput package is present only as a `.unitypackage`, not imported. Active source did not show Logitech, DirectInput, or force-feedback APIs.
- The project's legacy `InputManager.asset` does not contain NWH's expected named axes such as `Steering`, `Throttle`, `Brakes`, `Clutch`, and `Handbrake`; the new Input System path is safer.
- NWH `InputProvider` instances are registered statically. Multi-scene setups should avoid duplicate active providers.
- NWH has a simple shifting-origin component with a 500-unit threshold that moves loaded root objects relative to `Camera.main`. Treat it as reference only until a large-world strategy is designed.

Recommended Prompt 002 work:

- Create an `LwsVehicleInputProvider` deriving from or adapting `VehicleInputProviderBase`.
- Create an `LwsTruckTransmissionAdapter` for clutch, splitter, range, gear validation, and saveable gear state.
- Wrap NWH trailer attach/detach in LWS events and save-state adapters.
- Keep UTS player/trailer code away from NWH player prefabs.

## Urban Traffic System Full Pack

UTS is installed at version 2.0 and has demo roads, path editors, vehicles, pedestrians, semaphores, and simple traffic AI. Runtime scripts are mostly in the global namespace.

Important capabilities:

- `CarMove` drives NPC vehicles using Unity `WheelCollider`, motor torque, steering, braking, speed caps, and simple traction logic.
- `CarAIController` follows `MovePath`, raycasts for obstacle/traffic detection, and stops for tags such as `Car`, `Bcycle`, `PeopleSemaphore`, `Player`, and `People`.
- `CarWalkPath` spawns vehicles on author-authored traffic paths.
- `AddTrailer` creates a separate ConfigurableJoint trailer connection for UTS vehicles.
- Semaphore scripts provide one-way, T-junction, standard crossroad, together-arrow, car, and pedestrian traffic light behavior.
- People and bicycle systems are available as examples/content.

Important constraints:

- UTS traffic physics are not NWH physics.
- UTS path data is authored for demos and is not a highway-scale road graph.
- UTS trailers must not be used for the player truck because NWH has its own trailer modules.
- UTS tag assumptions can collide with project-wide gameplay tags if copied directly.

Recommended Prompt 002 work: treat UTS as a reference/content package for NPC traffic and semaphore behavior. If used, create an adapter layer that maps EasyRoads/LWS lane graph data into UTS-style spawn and movement concepts, rather than letting UTS own roads or the player truck.

## EasyRoads3D Pro

EasyRoads3D Pro v3.2.4f5 is installed as mostly compiled vendor DLLs with a runtime script example. It is the strongest road-construction authority in the project.

Important capabilities:

- `ERRoadNetwork` can create or reference a road network.
- `ERRoad` supports marker creation/insertion/deletion, road width/material/collider changes, terrain snapping, length queries, sampled position/heading, and spline-point exports for center, left, and right sides.
- Road networks can build/finalize/restore, connect roads, access connections, access road types, and use callbacks for road/side-object updates.
- SRP support packages are present, including URP 17.2.0 and beta URP 17.3 material packages.
- Conditional Vegetation Studio integration scripts exist, but Vegetation Studio/Pro is not installed.

Important constraints:

- Runtime EasyRoads terrain modification can persist terrain changes after leaving play mode if restore/cleanup is not handled. Treat runtime road edits as dangerous until wrapped and backed up.
- EasyRoads does not automatically provide truck routing, AI traffic lanes, or GPS ETA logic. It can be a data source for those systems.
- URP 17.4 material validation is needed because included material packages top out below the exact project package version.

Recommended Prompt 002 work: build or plan an `LwsRoadGraph` exporter from EasyRoads road samples, with immutable nodes/edges for routing, GPS, traffic lanes, depot placement, and saveable world identity. Do not directly query EasyRoads from every gameplay system.

## Vista Personal

Vista Personal 3000.2.0 is active through the `VISTA` scripting symbol. It should be treated as the terrain and biome generation authority.

Important capabilities:

- `VistaManager` manages terrain systems, biomes, tiles, generation tasks, and output events.
- `LocalProceduralBiome` owns seeded biome graphs, masks, cached biome data, and request/cleanup lifecycle.
- Outputs include height maps, hole maps, mesh density, albedo, metallic, layer weights, tree instances, detail density/instances, object instances, and generic textures/buffers.
- Generation is coroutine/progressive-task based and only one active generation task should run at a time.

Important constraints:

- Roads, road masks, vegetation, traffic, and prop distribution need a deterministic generation order.
- Vista should not be bypassed by independent terrain mutators unless the write path is coordinated.

Recommended Prompt 002 work: define generation order explicitly: Vista terrain and biomes first, EasyRoads road construction and road masks second, Spline Spawner and vegetation/probe content third, then road graph/nav/traffic export.

## Spline Spawner

Spline Spawner is embedded at `Packages/xyz.staggart-creations.spline-spawner`. Its manifest reports version 1.2.0 while Unity metadata reports 1.2.1.

Important capabilities:

- Runtime namespace `sc.splines.spawner.runtime`.
- Core classes include `SplineSpawner`, `SplineSpawnerMask`, `SplineInstanceContainer`, and `DistributionSettings`.
- Supports distribution on curves, areas, knots, radial zones, and grids.
- Uses Unity Splines, Burst, Collections, Mathematics, and Terrain integration version defines.
- Includes object pooling and linked-prefab instance containers.

Recommended Prompt 002 work: use Spline Spawner for roadside props, guardrails, fences, signs, and environment dressing after road splines and terrain masks are stable. Do not use it as road authority.

## Weather Maker

Weather Maker 8.0.9 is installed and has `WEATHER_MAKER_PRESENT` in Standalone symbols. It is the strongest weather, sky, wind, and time-of-day authority.

Important capabilities:

- `WeatherMakerScript` singleton/provider with precipitation, clouds, sky, aurora, fog, wind, thunder/lightning, sound, resource, and performance profile managers.
- Weather profile objects can transition and hold profile state.
- Weather zones can trigger local/global weather changes.
- Wind exposes current wind velocity and animated profile changes.
- URP support is documented for Unity 6000+ and URP 17.3+.

Important constraints:

- URP is not currently active in Graphics/Quality settings.
- Gamma color space conflicts with final visual recommendations.
- Weatherade has overlapping rain/snow visual concerns, so Weather Maker should drive Weatherade rather than competing with it.

Recommended Prompt 002 work: create an `LwsWeatherCoordinator` that owns simulator weather state and sends profile changes to Weather Maker first, then translates wetness/snow accumulation values into Weatherade.

## Weatherade Snow and Rain System

Weatherade 1.1.8 is installed under `Assets/NOT_Lonely/Weatherade SRS`. It should be treated as a specialized surface accumulation and wetness layer.

Important capabilities:

- `SnowCoverage` handles snow amount, displacement, tessellation, traces, sparkles, and shader/material updates.
- `RainCoverage` handles wetness, puddles, ripples, spots, and drips.
- `CoverageBase` manages follow target, depth camera, RenderTextures, material scanning, terrain support, and global shader texture state.
- URP support packages are present: `WeatheradeSRS_URP_17_1.unitypackage` and `WeatheradeSRS_URP.unitypackage`.

Important constraints:

- Active URP code paths are behind `USING_URP`, which is absent.
- Some referenced URP support classes were not found in imported source until support packages are imported/enabled.
- Weatherade can create hidden scene objects and global material/shader state; use one coordinated manager.

Recommended Prompt 002 work: do not let Weatherade decide weather. Use Weather Maker state as the source and drive Weatherade coverage values through a small adapter after render pipeline activation.

## Compass Navigator Pro

Compass Navigator Pro 4 version 6.0.2 is installed under `Assets/Plugins/Kronnect/CompassNavigatorPro`. It is a strong HUD/GPS/minimap/POI visualization package.

Important capabilities:

- `CompassPro` singleton-style instance, camera/follow assignment, compass bar, minimap, POI registration, POI focus/visited state, and route APIs.
- Route APIs include setting route world points, route-to-POI, route-to-destination, clear route, route progress, line rendering, and route events.
- `CompassProNavMeshRoute` can compute a NavMesh path and feed corners into Compass.
- `CompassProSaveLoad` can save/load fog, route, visited POIs, runtime POIs, minimap view, or all Compass state.
- `CompassProPOI` includes stable IDs and save keys.

Important constraints:

- Compass renders and tracks route points; it does not compute truck-legal highway routes.
- NavMesh route helper is not enough for trucking routes across EasyRoads highway data.
- Compass save data should be a child payload under the global simulator save, not the root save authority.

Recommended Prompt 002 work: create an `LwsNavigationService`/`LwsRoadGraph` that produces route polylines, ETA, distance, job destination, and depot POI identity, then feed route display into Compass.

## Pixel Crushers Dialogue System, OpenAI Addon, Save System, and Scene Streamer

Installed Pixel Crushers components:

- Dialogue System for Unity 2.2.73.2.
- Dialogue System Addon for OpenAI 1.0.37.
- Scene Streamer 1.26.1 from `_README.txt`.
- Pixel Crushers Common Save System.

Important capabilities:

- `DialogueManager` provides conversation start/stop, barks, localized text, Lua observers, and tracker updates.
- `DialogueSystemTrigger` can drive quest, Lua, sequence, bark, and conversation actions from trigger/collision/use/start events.
- Pixel Crushers Save System provides `SaveSystem`, `Saver`, slot save/load, additive scene load/unload integration, serialized data, and recursive saver record/apply.
- Dialogue System wrappers include `GameSaver`, `DialogueSystemSaver`, and `ConversationStateSaver`.
- Scene Streamer can load/unload additive scenes by name, track current loaded scenes, record state, and apply state; its saver integrates with Pixel Crushers Save System.
- OpenAI addon runtime code is gated by `USE_OPENAI` and includes explicit warnings against shipping plain-text API keys.

Important constraints:

- This project needs a simulator save root for truck state, trailers, jobs, cargo, route, world seed, terrain/road identity, weather, economy, and UI state.
- Pixel Crushers Save System can be an adapter, but should not become the unplanned global save owner by accident.
- Scene Streamer examples require scenes to be added to build settings; current build settings are empty.
- Runtime OpenAI features require security architecture. Do not ship API keys in Unity assets.

Recommended Prompt 002 work: define an `LwsSaveService` first. If Dialogue System is used, serialize Pixel Crushers dialogue/quest state as a child payload through an adapter. If Scene Streamer is adopted, use it behind an LWS streaming facade with explicit build-settings control.

## Heat UI and DOTween

Heat - Complete Modern UI 1.1.8 is installed with menu, in-game, HUD, localization, controller, button, slider, dropdown, switch, modal, notification, shop, achievement, and selector components under `Michsky.UI.Heat`.

DOTween 1.2.825 is installed under `DG.Tweening` with UI, UI Toolkit, physics, audio, sprite, and Unity-version modules. Project symbols include `DOTWEEN` and `DOTWEEN_UITOOLKIT`.

Recommended Prompt 002 work: use Heat for UI presentation and DOTween for interface animation. Avoid DOTween-driven physics motion for the player truck or trailers. UI should consume simulator state from LWS services, not vendor scene demo state.

## River Modeler, Path Painter II, and TotalBrush

River Modeler is a Staggart Creations spline-based procedural river geometry and VFX toolkit. Its manifest says 1.0.5, while meta files report 1.0.4. Runtime namespace is `sc.modeling.river.runtime`.

Path Painter II appears installed with version log 2.1.13 while metadata showed 2.1.12. It is an editor/content-authoring tool for painting paths and terrain-related data.

TotalBrush is under `NOT_Lonely.TotalBrush`, version metadata 1.1.8. It provides mesh/terrain painting, vertex color, mask, fill, gradient, and editor overlay tooling.

Recommended Prompt 002 work: treat these as world-art authoring helpers. They should not own terrain generation, roads, traffic, weather, or save state.

## Missing and Overlapping Systems

Vegetation is unresolved. Vista can output tree/detail/object instances, EasyRoads has conditional Vegetation Studio integration, Weather Maker has documentation references to Vegetation Studio Pro, and the planned `Vegetation Spawner FREE` asset is not installed. Prompt 002 must choose a temporary vegetation authority before procedural content work begins.

Weather has two systems. Weather Maker should own global weather/time/wind, while Weatherade should render local wet/snow accumulation as an effect layer.

Routing has two display/path systems but no truck road graph. Compass displays routes, UTS follows demo paths, and EasyRoads contains road geometry. Prompt 002 needs a new road graph and route service.

Save has multiple partial systems. NWH, Compass, Pixel Crushers, Weather, and world generation all need saveable state. Prompt 002 needs one simulator save root.

Input has both Unity input systems enabled, NWH input providers, Heat UI controller presets, and missing optional wheel/FFB support. Prompt 002 needs one input ownership plan.

## Prompt 002 Pre-Flight Decisions

Mandatory decisions before gameplay code:

1. Choose and activate the render pipeline asset for Standalone.
2. Decide whether to switch color space to Linear now or defer with a tracked risk.
3. Create or choose a non-vendor bootstrap scene and add build settings intentionally.
4. Clean or replace `DefaultVolumeProfile.asset` missing script entries.
5. Define the LWS integration namespace/folder/assembly boundary.
6. Define the save root and child payloads for NWH, Compass, Pixel Crushers, weather, jobs, routes, and world generation.
7. Define the road graph export path from EasyRoads into routing, GPS, and traffic.
8. Define the terrain/road/vegetation generation order.
9. Define NWH input and 18-speed transmission adapter behavior.
10. Decide whether Prompt 002 may edit ProjectSettings, or whether it must only create code and scenes.

Recommended Prompt 002 objective: create the LWS integration shell, bootstrap scene, service contracts, and preflight validators without modifying vendor assets. Keep all new code under a new project-owned namespace such as `LWS.InterstateHauler`.

## No-Touch Audit Validation

Prompt 001 intentionally avoided:

- Gameplay implementation.
- Vendor package edits.
- Vendor upgrades.
- Scene or prefab edits.
- Project setting changes.
- Asset deletes or moves.

Final validation:

- `git diff --name-only -- Assets Packages ProjectSettings` returned no tracked diffs.
- The four Prompt 001 deliverables are under `Documentation/InterstateHauler/`.
- `git status --short --untracked-files=all` reported only the four new documentation files.
- Prompt 001 did not stage or commit anything because the task log did not satisfy the clean-start commit rule.
