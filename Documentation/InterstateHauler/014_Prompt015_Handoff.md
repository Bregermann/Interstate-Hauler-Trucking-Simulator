# Prompt 015 Handoff

Expected Prompt 015:

Floating Origin / Large World Coordinates

## Current Streaming State

Master scene:

`Assets/LWS/InterstateHauler/World/Streaming/Validation/StreamingHighwayValidation.unity`

Manifest:

`Assets/LWS/InterstateHauler/World/Streaming/Data/IH_WorldStreamingManifest_Validation.asset`

Policy:

`Assets/LWS/InterstateHauler/World/Streaming/Data/IH_WorldStreamingPolicy_Validation.asset`

Service:

`ILwsWorldStreamingService` / `LwsWorldStreamingService`

Coordinator:

`LwsWorldStreamingCoordinator`

Anchor state:

`LwsWorldStreamingAnchorState`

Important fields:

- `TractorPosition`
- `Heading`
- `SpeedMetersPerSecond`
- `HasTrailer`
- `TrailerPosition`

## Chunk Scenes

- `IH_Chunk_000_Start`
- `IH_Chunk_001_HighwayA`
- `IH_Chunk_002_HighwayB`
- `IH_Chunk_003_HighwayC`
- `IH_Chunk_004_Turnaround`

Chunk scenes are physical/presentation-only and must not become bootstrap/player/save owners.

## Floating-Origin Notes

Prompt 015 should add a project-owned origin service that can:

- track accumulated world offset
- shift loaded chunk roots
- shift persistent player tractor/trailer safely
- shift traffic vehicles and road-condition presentation
- notify GPS/road graph consumers
- preserve save-state coordinates in stable world space
- keep NWH physics stable during shifts

Do not store machine-local floating-origin offsets in career save as authoritative world positions.

## Risks To Carry Forward

- Scene Streamer 1.26.1 explicit `LoadScene(string)` path is unsafe in the installed source because the instance `Load(string sceneName)` ignores its argument. Do not edit vendor source casually; keep LWS explicit policy loads isolated unless a later vendor-package decision changes this.
- Prompt 014 validation scenes are runtime-generated meshes for validation, not production EasyRoads/Vista chunks.
- Additive load/unload performance needs normal Editor profiling with the Unity Profiler.
- Trailer straddling/seam behavior needs manual driving validation before this is considered production-safe.

## Prompt 015 Starting Point

Begin from `LwsWorldStreamingService`, `LwsWorldStreamingCoordinator`, and `LwsWorldStreamingAnchorState`. Add floating-origin events/state beside streaming lifecycle rather than embedding origin math directly into chunk builders.
