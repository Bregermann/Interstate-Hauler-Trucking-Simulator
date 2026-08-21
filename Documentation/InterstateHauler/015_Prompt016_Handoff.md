# Prompt 016 Handoff - Persistence and Save Architecture

Prompt 015 added the project-owned floating-origin foundation. Prompt 016 must save global coordinates, not raw local Unity `Transform.position`.

Important APIs:

- `ILwsGameClockService`
- `LwsGameClockSnapshot`
- `ILwsWorldOriginService`
- `LwsWorldPositionD`
- `ILwsWorldOriginService.CurrentOriginOffset`
- `ILwsWorldOriginService.PlayerGlobalPosition`
- `ILwsWorldOriginService.LocalToGlobal(Vector3 localPosition)`
- `ILwsWorldOriginService.GlobalToLocal(LwsWorldPositionD globalPosition)`
- `LwsWorldStreamingManifest`
- `LwsWorldChunkDefinition.stableId`
- `LwsRoadGraph` stable road/edge IDs

Save-facing rules:

- Save game date/time from `ILwsGameClockService`, not Weather Maker internal time.
- Save player/trailer positions as `LwsWorldPositionD`.
- Save stable vehicle, trailer, chunk, road, route, weather, and road-condition IDs where applicable.
- Do not treat local `Transform.position` as a stable world save position.
- Do not serialize vendor MonoBehaviour graphs as save state.

Expected load flow:

1. Read saved global player/trailer state.
2. Establish an appropriate current origin offset.
3. Load required world chunks by stable global chunk IDs/positions.
4. Spawn or restore player truck at `GlobalToLocal(savedGlobalPosition)`.
5. Restore trailer, route, weather, and road-condition state through LWS services.
6. Restore Weather Maker presentation by letting `LwsWeatherMakerAdapter` consume the LWS clock.

Prompt 015C added an effectively endless `StreamingHighwayValidation` road. Do not serialize physical pool slot IDs as world identity; use generated logical segment IDs and global positions when save support needs to reason about this validation environment.

Manual validation still needed before declaring full production readiness:

- streamed highway driving through multiple origin shifts
- attached trailer shift
- UTS traffic shift
- GPS route continuity
- Weatherade recentering
- Weather Maker continuity
