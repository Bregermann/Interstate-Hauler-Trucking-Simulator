# Prompt 016 - LWS Save Provider Matrix

| Provider | Stable ID | Payload | Pixel Crushers Record Key | Restore Scope | Prompt 016 Status | Notes |
|---|---|---|---|---|---|---|
| Global player position | `lws.world.global-position` | `LwsGlobalPositionSavePayload` | same as stable ID | Captures global double position; stores restored payload for Prompt 017 | Implemented | Does not yet move the truck/world on load. |
| Game clock | `lws.game-clock` | `LwsGameClockSavePayload` | same as stable ID | Restores LWS semantic clock date/time, scale, paused state | Implemented | Weather Maker follows LWS time separately. |
| Weather semantic state | `lws.weather.semantic` | `LwsWeatherSavePayload` | same as stable ID | Restores LWS weather state and time of day | Implemented | Does not serialize Weather Maker internals. |
| Validation variable | `lws.validation.proof` | `LwsValidationSavePayload` | same as stable ID | Roundtrip proof for Prompt 016 | Implemented | Used by development save/load proof and tests. |
| Dialogue System | `pixel-crushers.dialogue` | Pixel Crushers native `DialogueSystemSaver` data | vendor saver key | Native Dialogue System restore | Native/deferred | Use `DialogueSystemSaver`; LWS placeholder keeps the seam visible. |
| Player truck | `vehicle.truck` | future truck/trailer runtime state | same as stable ID | Deferred mid-route resume | Deferred | Prompt 017 owns full truck/trailer resume. |
| Compass / navigation vendor state | `compass.navigator` | future semantic/vendor state | same as stable ID | Deferred | Deferred | GPS route intent should remain LWS semantic. |
| Jobs | `jobs.state` | future job/cargo/economy payload | same as stable ID | Deferred | Deferred | Prompt 018+ scope. |

## Provider Rules

Future systems add semantic providers through `ILwsSaveParticipant` and register with `ILwsSaveService`. They should not write platform files directly and should not serialize arbitrary vendor MonoBehaviour graphs.
