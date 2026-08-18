# Prompt 015 - Origin Participant Matrix

| System/Object | Spatial | Shifted | Method | Global State | Cache Refresh | Risk | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `LwsApplicationBootstrap` | No | No | Not registered | Service registry only | None | Low | Non-spatial manager stays fixed. |
| `LwsWorldOriginService` | No | No | Owns origin offset | `LwsWorldPositionD CurrentOriginOffset` | Emits origin version/events | Low | Does not move itself. |
| `LwsFloatingOriginCoordinator` | No | No | Queues and executes shifts | Reads player local, updates global through service | None | Medium | Must remain in master/global scene. |
| Loaded chunk root | Yes | Yes | `LwsStreamedChunkSceneRoot` participant | Manifest keeps canonical global bounds | Aligns on registration after prior shifts | Medium | Root shift moves chunk presentation. |
| Player tractor | Yes | Yes | `LwsFloatingOriginRigidbodyParticipant` | Telemetry exposes global position | Rigidbody velocity restored | High | Requires normal Editor driving validation. |
| Player trailer | Yes | Yes | `LwsFloatingOriginRigidbodyParticipant` | Trailer identity stays LWS/NWH-owned | Rigidbody velocity restored | High | Must shift atomically with tractor. |
| Detached player trailer | Yes | Yes | Same trailer participant | Trailer ID remains stable | Rigidbody velocity restored | Medium | Active detached validation still needs manual test. |
| NWH `VehicleController` internals | Yes | Indirect | Shift parent/root only | NWH remains local physics | None from LWS | High | Vendor source untouched. |
| Physical cab GPS | Yes | Yes | Child of player truck | Route state remains global | Map graphic uses global player position | Medium | Visual route line requires manual check. |
| Road graph service | No | No | Not registered | Stable road/edge IDs and global positions | Queries receive global player position | Low | Do not mutate road IDs on shift. |
| Road graph debug panel | No | No | Not registered | Converts local player to global for lookup | Cached UI refresh | Low | Development-only. |
| Road-condition runtime | No | No | Not registered | Semantic condition per road segment | Player lookup uses global position | Medium | Ice/wet state manual validation needed. |
| Weather Maker | Mixed | No direct LWS shift | Vendor owns atmosphere | Weather state remains service-owned | None | Medium | Do not restart weather on shift. |
| Weatherade coverage | Yes | Indirect | Adapter follows target and refreshes materials | Semantic conditions unchanged | `OriginShiftCompleted` refresh | Medium | Presentation recentering needs visual test. |
| UTS lanes root | Yes | Yes | `LwsFloatingOriginTransformParticipant` | Lane IDs/global lane samples stay stable | Path point cache refresh | High | UTS cache behavior needs driving test. |
| UTS NPC vehicle roots | Yes | Yes | `LwsFloatingOriginRigidbodyParticipant` | Traffic identity remains in registry | Rigidbody velocity restored | High | Confirm no path reset/jump in Editor. |
| UTS path components | Yes | Via lanes root | LWS converts global samples to local | Lane samples remain global | `RefreshPathPointCache` | High | Public adapter refresh only, no vendor edits. |
| Streaming debug panel | No | No | Not registered | Shows global and local anchors | Throttled refresh | Low | Development-only. |
| Floating origin debug panel | No | No | Not registered | Reads origin service state | Throttled/cached diagnostics | Low | F9 toggle and dev buttons only. |
