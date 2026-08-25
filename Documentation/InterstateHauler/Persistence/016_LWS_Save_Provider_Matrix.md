# Prompt 016 - LWS Save Provider Matrix

| Provider | Stable ID | Payload | Pixel Crushers Record Key | Restore Scope | Prompt 016 Status | Notes |
|---|---|---|---|---|---|---|
| Profile directory | `lws.profile-directory` | `LwsSaveProfileDirectory` | `lws.profile-directory` in reserved slot 16000 | Profiles and manual-slot metadata | Implemented | Stored through Pixel Crushers `SavedGameDataStorer`, not a raw LWS file. |
| LWS semantic snapshot bridge | `lws.semantic-state` | `LwsSaveSnapshot` | `lws.semantic-state` | Captures all registered LWS participants through Pixel Crushers `Saver` | Implemented | `LwsPixelCrushersSemanticSaver` is the bridge. |
| Global player position | `lws.world.global-position` | `LwsGlobalPositionSavePayload` | inside semantic snapshot | Captures double-precision global player position and local diagnostic position | Implemented | Uses `ILwsWorldOriginService.PlayerGlobalPosition`. |
| World resume context | `lws.world.resume-context` | `LwsWorldResumeContextPayload` | inside semantic snapshot | Captures stable world ID, authored scene, saved global position, heading, and origin diagnostics for Prompt 017 pre-read | Implemented in Prompt 017 | Used only to prepare the world before Pixel Crushers applies gameplay state. |
| Player truck | `lws.vehicle.player-truck` | `LwsPlayerTruckSavePayload` | inside semantic snapshot | Captures player truck definition, pose, velocities, engine, NWH gear, trailer state, and transmission state | Implemented | Prompt 017 load coordinator prepares world/origin first, then restores the truck stationary through this payload. |
| 18-speed transmission | `vehicle.transmission.player` | Prompt 006 transmission state | inside semantic snapshot | Restores through `Lws18SpeedTransmissionController` | Existing/self-registering | Prompt 016 does not bypass transmission authority. |
| Game clock | `lws.game-clock` | `LwsGameClockSavePayload` | inside semantic snapshot | Restores LWS semantic date/time, time scale, and pause state | Implemented | Weather Maker follows LWS clock separately. |
| Weather semantic state | `lws.weather.semantic` | `LwsWeatherSavePayload` | inside semantic snapshot | Restores LWS weather preset/state/time of day | Implemented | Does not serialize Weather Maker internals. |
| Road condition semantic state | `lws.road-condition.semantic` | `LwsRoadConditionSavePayload` | inside semantic snapshot | Restores Weatherade-facing LWS road condition mode/snapshot | Implemented | Does not serialize Weatherade renderer or material internals. |
| Navigation destination intent | `lws.navigation.destination-intent` | `LwsNavigationSavePayload` | inside semantic snapshot | Restores destination intent and recalculates route when graph exists | Implemented | Does not serialize Compass rendered route, minimap pixels, or RenderTextures. |
| Dialogue System | Pixel Crushers vendor saver keys | Vendor dialogue data | vendor saver records | Dialogue variables/conversation state | Native | Use `DialogueSystemSaver`/`ConversationStateSaver`; no LWS duplicate. |
| UTS ambient traffic | None | None | None | Excluded | Implemented exclusion | Ambient traffic regenerates; individual cars are not persisted in Prompt 016. |
| Cab accessories future seam | future stable IDs | `LwsFutureCabAccessorySavePayload` | future semantic snapshot section | Schema planning only | Deferred | No accessory gameplay implemented. |
| Companion future seam | future stable IDs | `LwsFutureCompanionSavePayload` | future semantic snapshot section | Schema planning only | Deferred | No companion AI implemented. |
| Life event future seam | future stable IDs | `LwsFutureLifeEventSavePayload` | future semantic snapshot section | Schema planning only | Deferred | No family/life simulation implemented. |

## Provider Rules

Future Interstate systems may add semantic participants through `ILwsSaveParticipant`, but Pixel Crushers remains the save authority. Participants provide compact versioned payloads; they must not own platform files, slot databases, or arbitrary vendor MonoBehaviour graph serialization.
