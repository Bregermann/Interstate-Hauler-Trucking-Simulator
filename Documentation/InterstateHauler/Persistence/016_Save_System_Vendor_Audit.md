# Prompt 016 - Save System Vendor Audit

## Decision

One save framework authority: Pixel Crushers Save System.

Pixel Crushers is physically installed, already shared by Pixel Crushers middleware, supports `Saver` components, slot save/load/delete, `SavedGameData`, `SavedGameDataStorer`, serializers, and Dialogue System persistence. LWS keeps only semantic payloads, profile/slot mapping, validation, and a thin `ILwsSaveService` facade.

## Imported Save Systems Found

| Asset | Version | Path | Documentation | Demos / Examples | Primary Save Manager | Serializer | Storage Backend | Manual Slot Support | Slot Enumeration | Delete Slot Support | Slot Metadata | Async Save Support | Custom Game Data | Scene State | Runtime Prefab State | Dialogue Integration | Console Storage Abstraction | Save Versioning | Migration Support | Current Usage | Selection |
|---|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Pixel Crushers Common Save System | 1.10.73 from installed skill metadata | `Assets/Plugins/Pixel Crushers/Common/Scripts/Save System/` | Common Save System manual and installed skill docs | Save System wrappers, test menu, saver templates | `PixelCrushers.SaveSystem` | `JsonDataSerializer`, `BinaryDataSerializer` | `SavedGameDataStorer`, `DiskSavedGameDataStorer`, `PlayerPrefsSavedGameDataStorer` | Yes, integer slots | `maxSaveSlot`, `HasSavedGameInSlot(int)` | Yes, `DeleteSavedGameInSlot(int)` | Basic scene/version metadata; LWS adds profile/manual slot metadata | Yes through `SaveToSlot` and `StoreSavedGameDataAsync` | Yes through `Saver.RecordData()` and `SavedGameData.SetData()` | Yes | Yes through spawned object savers | Shared with Dialogue System | Yes, replace `SavedGameDataStorer` later | Yes, `SaveSystem.version` and `SavedGameData.version` | Project schema layered in LWS payload | Selected by Prompt 016 | SELECTED |
| Pixel Crushers Dialogue System save integration | 2.2.73.2 from `_README.txt` | `Assets/Plugins/Pixel Crushers/Dialogue System/Scripts/Save System/` | Dialogue System docs | Dialogue demo menus and saver wrappers | Shared `PixelCrushers.SaveSystem` | Shared Pixel Crushers serializer | Shared Pixel Crushers storer | Inherits Save System slots | Inherits Save System slot checks | Inherits Save System deletion | Vendor saver data | Inherits Save System async path | Dialogue variables and conversation data | Yes | Not player truck runtime state | `DialogueSystemSaver`, `ConversationStateSaver`, `GameSaver` | Shared storer | Shared versioning | Vendor-managed | Reused under selected authority | KEEP |
| Pixel Crushers Scene Streamer saver | 1.26.1 from `_README.txt` | `Assets/Plugins/Pixel Crushers/Scene Streamer/Scripts/Extensions/SceneStreamerSaver.cs` and package mirror | Scene Streamer docs | Scene Streamer examples | Shared `PixelCrushers.SaveSystem` | Shared Pixel Crushers serializer | Shared Pixel Crushers storer | Inherits slots | Inherits slot checks | Inherits deletion | Loaded scene list | Shared path | Scene streamer state | Additive scene list | No | Shared storer | Shared versioning | Vendor-managed | Future Prompt 017 resume aid | KEEP |
| Pixel Crushers Love/Hate savers | 1.10.74 from `_README.txt` | `Assets/Plugins/Pixel Crushers/LoveHate/...Saver.cs` | Love/Hate docs | Love/Hate examples | Shared `PixelCrushers.SaveSystem` | Shared Pixel Crushers serializer | Shared Pixel Crushers storer | Inherits slots | Inherits slot checks | Inherits deletion | Faction/member payloads | Shared path | Faction state | No player truck state | No | Shared storer | Shared versioning | Vendor-managed | Not active Interstate career state yet | KEEP FOR FUTURE |
| Heat UI PlayerPrefs helpers | Heat vendor UI scripts | `Assets/Heat - Complete Modern UI/Scripts/` | Heat UI docs | UI examples | None for game save authority | PlayerPrefs values | PlayerPrefs | No proper profile/manual slot authority | No | Per-key only | UI widget settings only | No | No career state | No | No | No | Not a console career-save seam | Per-key only | Vendor UI-level settings | Not selected | NOT SELECTED |
| Compass Navigator Pro save/load demo code | Compass vendor/demo code | `Assets/Plugins/Kronnect/CompassNavigatorPro/Scripts/CompassProSaveLoad.cs` | Compass docs | Compass demos | Demo/component-specific | Direct/file-oriented vendor example | Vendor-specific | No Interstate profile model | No | Not selected | Demo-specific | No | Compass presentation data | No | No | No | Not Interstate storage abstraction | Not selected | Demo-specific | Not selected | NOT SELECTED |

Crystal Save: NOT INSTALLED - NOT CONSIDERED.

Easy Save / ES3: NOT INSTALLED - NOT CONSIDERED.

## Existing Save Class Classification

| Existing Save Class / Concept | Purpose | Current Authority | Decision | Why |
|---|---|---|---|---|
| `PixelCrushers.SaveSystem` | Save orchestration, saver collection, slot save/load/delete | Pixel Crushers | KEEP | Final production save framework authority. |
| `PixelCrushers.SavedGameData` | Vendor save payload container | Pixel Crushers | KEEP | Owns serialized save records and metadata. |
| `PixelCrushers.SavedGameDataStorer` | Vendor storage abstraction | Pixel Crushers | KEEP | One production storage path; console ports replace this backend. |
| `PixelCrushers.DiskSavedGameDataStorer` | Current PC/Steam storage backend | Pixel Crushers | KEEP | Used as current Windows/Steam backend. |
| `PixelCrushers.PlayerPrefsSavedGameDataStorer` | Alternate vendor storer | Pixel Crushers | KEEP UNUSED | Installed vendor option, not selected for PC/Steam career saves. |
| `PixelCrushers.JsonDataSerializer` | Current serializer | Pixel Crushers | KEEP | Used by Save System for LWS profile directory and semantic snapshots. |
| `PixelCrushers.BinaryDataSerializer` | Optional serializer | Pixel Crushers | KEEP UNUSED | Installed vendor option, not selected for readable dev validation. |
| `PixelCrushers.Saver` | Vendor extension point | Pixel Crushers | KEEP | LWS semantic saver bridge derives from this. |
| `PixelCrushers.SaveSystemMethods` | UI/event helper | Pixel Crushers | KEEP UNUSED | Available for future UI wiring; not current authority. |
| `PixelCrushers.SaveSystemEvents` | Save lifecycle event helper | Pixel Crushers | KEEP UNUSED | Available for later feedback/UI integration. |
| `PixelCrushers.DialogueSystem.DialogueSystemSaver` | Dialogue database/variables persistence | Pixel Crushers | KEEP | Dialogue state must remain on shared Pixel Crushers Save System. |
| `PixelCrushers.DialogueSystem.ConversationStateSaver` | Conversation state persistence | Pixel Crushers | KEEP | Reused when dialogue content needs current conversation state. |
| `PixelCrushers.DialogueSystem.GameSaver` | Dialogue System save/load helper component | Pixel Crushers | KEEP/ADAPTER | Useful reference/component, but gameplay calls LWS facade. |
| `SceneStreamerSaver` | Scene Streamer loaded-scene payload | Pixel Crushers | KEEP | Future Prompt 017 may use it for streaming resume. |
| Love/Hate faction/member savers | Reputation/faction persistence | Pixel Crushers | KEEP FOR FUTURE | Installed vendor savers should remain under the same Save System. |
| `ILwsSaveService` | Project-facing save facade | LWS facade over Pixel Crushers | MIGRATE | Kept only as thin semantic/profile/slot facade. |
| `LwsSaveService` | Former LWS root save service | LWS facade over Pixel Crushers | MIGRATE | No longer owns custom storage or a proof slot; forwards slot operations to Pixel Crushers. |
| `LwsPixelCrushersSaveAdapter` | LWS-to-Pixel reflection adapter | LWS adapter | ADAPTER | Isolates asmdef boundary and binds Save System/storer/serializer. |
| `LwsPixelCrushersSemanticSaver` | Pixel Crushers saver bridge for LWS snapshot | LWS adapter | ADAPTER | Lets Pixel Crushers invoke LWS semantic capture/restore through the vendor `Saver` model. |
| `LwsSaveProfileDirectory` | Profile and slot metadata payload | LWS semantic payload | KEEP | Stored through Pixel Crushers reserved profile-directory slot. |
| `LwsSaveProfileMetadata` | Stable profile identity | LWS semantic payload | KEEP | Renames do not change stable profile IDs. |
| `LwsManualSaveSlotMetadata` | Player-facing manual slot metadata | LWS semantic payload | KEEP | Maps visible Slot 1-3 to vendor slot numbers. |
| `LwsGlobalPositionSaveParticipant` | Floating-origin global position payload | LWS semantic payload | KEEP | Captures double-precision global player location. |
| `LwsPlayerTruckSaveParticipant` | Player truck semantic runtime payload | LWS semantic payload | KEEP | Captures truck ID, pose, velocities, engine, trailer, transmission state. |
| `Lws18SpeedTransmissionController` save participant | Transmission mode/range/splitter state | LWS semantic payload | KEEP | Prompt 006 authority remains intact and self-registers. |
| `LwsGameClockSaveParticipant` | Semantic clock state | LWS semantic payload | KEEP | Restores through `ILwsGameClockService`. |
| `LwsWeatherSaveParticipant` | Semantic weather state | LWS semantic payload | KEEP | Restores through `ILwsWeatherService`; no Weather Maker internals. |
| `LwsRoadConditionSaveParticipant` | Semantic road condition state | LWS semantic payload | KEEP | Restores through `ILwsRoadConditionService`; no Weatherade internals. |
| `LwsNavigationSaveParticipant` | Destination intent and route context | LWS semantic payload | KEEP | Recalculates route from semantic destination/graph, not rendered route pixels. |
| `ILwsSaveStorage` | Old LWS storage seam | LWS custom framework | REMOVE | Would create a second storage path beside Pixel Crushers. |
| `LwsPcSaveStorage` | Old LWS PC storer wrapper | LWS custom framework | REMOVE | PC storage is Pixel Crushers `DiskSavedGameDataStorer`. |
| `ILwsSaveStorage` old in-memory implementation | Old test-only storage concept | LWS custom framework | REMOVE | Not present in production and must not return. |
| Old proof slot implementation | Single dev-only slot | LWS custom framework | REMOVE | Replaced by profiles plus manual slots 1-3. |
| `LwsValidationSaveParticipant` | Old roundtrip proof variable | LWS custom framework | REMOVE | Replaced by real semantic participants and tests. |
| `LwsPlaceholderSaveParticipant` | Old placeholder participant | LWS custom framework | REMOVE | Replaced by real semantic participants. |

## Final Authority

- Save framework authority: Pixel Crushers.
- Production storage path: Pixel Crushers `SavedGameDataStorer` abstraction.
- Current PC backend: `DiskSavedGameDataStorer`.
- LWS custom save framework: none.
- LWS in-memory production storage: none.
- Old proof slot active in production: no.
- Existing good Pixel Crushers code reused: yes.
