# Prompt 014 - Streaming Test Matrix

| Test | Type | Status | Notes |
|---|---|---|---|
| Manifest validates stable chunk topology | EditMode | Added | `LwsWorldStreamingEditModeTests.StreamingManifestValidatesStableChunkTopology` |
| Duplicate chunk IDs rejected | EditMode | Added | Prevents ambiguous save/load and road references. |
| Missing neighbor IDs rejected | EditMode | Added | Prevents broken neighbor graph metadata. |
| Policy keeps current/ahead/trailer chunks | EditMode | Added | Verifies tractor heading and trailer safety margins. |
| Global streaming road graph validates | EditMode | Added | Uses `LwsStreamingHighwayGraphBootstrap.CreateDefaultGraph`. |
| Scene Streamer dependency boundary | EditMode | Added | Core streaming service/types avoid concrete Pixel Crushers dependency. |
| Default registry has streaming service | PlayMode | Added | Ensures bootstrap/service architecture includes Prompt 014 service. |
| Service requests/tracks loaded chunks | PlayMode | Added | Uses fake adapter; does not need physical additive scene loads. |
| Freeze prevents policy loads | PlayMode | Added | Development freeze mode validation. |
| Load all / unload distant safe with adapter | PlayMode | Added | Hardware-free service smoke test. |
| Open `StreamingHighwayValidation.unity` | Manual Editor | Required | Normal Unity Editor validation required. |
| Drive across chunk boundaries | Manual Editor | Required | Confirm loaded/unloaded chunks while tractor/trailer persist. |
| Trailer straddles seam safely | Manual Editor | Required | Confirm trailer safety margin prevents premature unload. |
| GPS remains active | Manual Editor | Required | GPS should use global road graph, not chunk objects. |
| UTS traffic remains global | Manual Editor | Required | Traffic service must not duplicate inside chunks. |
| Weather Maker remains global | Manual Editor | Required | Weather runtime should survive chunk load/unload. |
| Road-condition/Weatherade state remains active | Manual Editor | Required | Road-condition services must remain global; visual binding is presentation-only. |
| Scene reload | Manual Editor | Required | Confirm no duplicate managers, services, or player trucks. |

## Validation Policy

Normal Unity Editor validation is authoritative. Do not treat the previously known incompatible batchmode vendor compile path as a hard blocker unless the same error reproduces in the normal Editor.
