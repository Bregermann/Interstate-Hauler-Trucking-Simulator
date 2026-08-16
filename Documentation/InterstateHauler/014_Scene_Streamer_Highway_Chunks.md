# Prompt 014 - Scene Streamer Highway Chunks

## Summary

Prompt 014 establishes the first project-owned streamed highway validation world for Interstate: Hauler.

Master validation scene:

`Assets/LWS/InterstateHauler/World/Streaming/Validation/StreamingHighwayValidation.unity`

Project-owned manifest:

`Assets/LWS/InterstateHauler/World/Streaming/Data/IH_WorldStreamingManifest_Validation.asset`

Project-owned policy:

`Assets/LWS/InterstateHauler/World/Streaming/Data/IH_WorldStreamingPolicy_Validation.asset`

Scene Streamer package:

`Assets/Plugins/Pixel Crushers/Scene Streamer`

Installed version:

`1.26.1`

## Architecture

LWS owns:

- stable chunk IDs
- chunk manifest
- load policy
- active chunk state
- player/trailer streaming anchor
- service events
- debug tooling
- road, traffic, and road-condition registration state

Scene Streamer owns or participates in:

- additive scene-streaming package availability
- current-scene API
- neighbor metadata model through `NeighboringScenes`
- vendor singleton/runtime component in the master validation scene

Important installed-package caveat:

`SceneStreamer.LoadScene(string sceneName)` exists, but the installed `SceneStreamer.Load(string sceneName)` implementation calls its internal load method with the current scene name rather than the passed argument. Because of that, the LWS adapter exposes the Scene Streamer boundary and current-scene API, but uses Unity additive scene load/unload for explicit policy-driven chunk requests. This is documented as an integration risk rather than edited in vendor code.

## Persistent Content

Persistent/global systems stay in `StreamingHighwayValidation.unity`:

- `LwsApplicationBootstrap`
- `LwsWorldStreamingService`
- `LwsSceneStreamerAdapter`
- `LwsWorldStreamingCoordinator`
- `LwsStreamingHighwayGraphBootstrap`
- player tractor/trailer spawner
- road graph provider
- navigation/GPS debug services
- UTS highway traffic controller/debug services
- Weather Maker adapter/debug services
- road condition runtime, Weatherade adapter, and NWH road-condition adapter

The player tractor and trailer are not placed in chunk scenes. They spawn through the existing Prompt 004 lifecycle using:

- `IH_PlayerTruck_NWH.prefab`
- `IH_TestTrailer_DryVan.prefab`
- `IH_TruckDefinition_StarterNwhSemi.asset`
- `IH_18SpeedTransmission_G29_EatonDevelopment.asset`

## Streamed Chunk Content

Chunk scenes contain only local world presentation:

- chunk root metadata
- Scene Streamer neighbor scene names
- runtime highway mesh/colliders
- `LwsRoadSurface` presentation
- lane debug lines
- service area / turnaround presentation where applicable

Chunk scenes intentionally do not contain:

- bootstrap
- player truck
- trailer
- input/G29/FFB services
- transmission ownership
- truck controls
- global GPS service
- global traffic service
- global Weather Maker service
- save roots

## Runtime Types

Core:

- `LwsWorldChunkDefinition`
- `LwsWorldStreamingPolicy`
- `LwsWorldStreamingManifest`
- `LwsWorldChunkRuntimeState`
- `LwsWorldStreamingAnchorState`
- `LwsWorldStreamingPolicyDecision`

Service and adapters:

- `ILwsWorldStreamingService`
- `LwsWorldStreamingService`
- `ILwsSceneStreamerAdapter`
- `LwsSceneStreamerAdapter`
- `LwsWorldStreamingCoordinator`

Validation:

- `LwsStreamedChunkSceneRoot`
- `LwsStreamingHighwayChunkBuilder`
- `LwsStreamingHighwayGraphBootstrap`
- `LwsStreamingDebugPanel`

## Load Policy

Validation policy defaults:

- load ahead: 1500 m
- keep behind: 800 m
- preload margin: 250 m
- unload distance: 2100 m
- unload hysteresis: 300 m
- tractor safety margin: 140 m
- trailer safety margin: 180 m
- max concurrent loads: 2
- minimum neighbor depth: 1

The policy protects both tractor and trailer positions, so a trailer straddling a chunk boundary should keep the relevant chunk loaded.

## Road, GPS, Traffic, Weather, and Road Conditions

The semantic road graph remains global and is built by `LwsStreamingHighwayGraphBootstrap`.

Stable road IDs carried forward:

- `IH_TEST_I000_NB`
- `IH_TEST_I000_SB`
- `IH_TEST_I000_RAMP`
- `IH_TEST_I000_TURN`

GPS consumes the global road graph. It should not depend on loaded chunk scene objects.

UTS traffic remains a global service. Prompt 014 does not make UTS vehicles player vehicles and does not move traffic authority into chunks.

Weather Maker remains global. Weatherade/road-condition visual presentation remains semantic and adapter-owned; streamed chunks expose road surfaces for future per-chunk visual binding.

## Debug Tooling

`LwsStreamingDebugPanel` shows:

- active world
- active chunk
- tractor/trailer anchor
- speed
- freeze/resume
- load all
- unload distant
- reload neighborhood
- per-chunk runtime state
- road/traffic/Weatherade registration flags
- last load/unload durations
- last error

The panel refreshes on a cadence and does not scan the scene every frame.

## Validation Notes

Automated tests cover manifest validation, policy decisions, trailer protection, service load tracking, freeze behavior, and global road graph creation.

Manual Unity Editor validation still needs to confirm:

- `StreamingHighwayValidation.unity` opens cleanly.
- The first chunk loads at startup.
- The player truck spawns outside chunk ownership.
- Driving across chunk boundaries loads/unloads chunks without unloading the tractor or trailer.
- GPS, traffic, Weather Maker, and road-condition services remain live during streaming.
- No repeated LWS console errors appear.

## Deferred Work

Prompt 015 owns floating origin. Prompt 014 deliberately does not shift world origin, rebase physics, rebase loaded scenes, or serialize world-origin offsets.

Future prompts should add:

- production chunk generation from EasyRoads/Vista data
- per-chunk POI declarations
- per-chunk scenery quality budgets
- true streamed traffic presentation handoff
- Weatherade material/render validation under final streamed chunks
- floating-origin-aware chunk activation
