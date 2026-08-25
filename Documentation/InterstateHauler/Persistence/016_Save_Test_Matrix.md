# Prompt 016 - Save Test Matrix

| Test | Type | Expected Result | Status |
|---|---|---|---|
| Pixel Crushers Save System files exist | EditMode/source | `SaveSystem`, `Saver`, `DiskSavedGameDataStorer`, and `DialogueSystemSaver` are installed | Implemented |
| Single save authority | EditMode/source | LWS facade delegates slot save/load/delete to Pixel Crushers APIs | Implemented |
| Old LWS storage path removed | EditMode/source | No production `ILwsSaveStorage`, `LwsPcSaveStorage`, `LwsInMemorySaveStorage`, proof slot, or proof participant | Implemented |
| Profile schema exists | EditMode | `LwsSaveProfileMetadata`, `LwsManualSaveSlotMetadata`, and `LwsSaveProfileDirectory` exist | Implemented |
| Deterministic slot mapping | EditMode | Stable profile plus slot type/number maps to deterministic vendor slots | Implemented |
| Three manual slots | EditMode | Each profile exposes at least Slot 1, Slot 2, and Slot 3 | Implemented |
| Profile isolation | EditMode | Different profiles map to distinct vendor slot ranges | Implemented |
| Profile rename safety | EditMode | Rename preserves `StableProfileId` | Implemented |
| No direct runtime file IO | EditMode/source | Prompt 016 runtime save code does not call platform file APIs | Implemented |
| Global position payload precision | EditMode | global X/Y/Z fields are `double` | Implemented |
| Semantic providers register | EditMode | global position, player truck, clock, weather, road condition, and navigation providers are present | Implemented |
| Future payload seams | EditMode | cab accessory, companion, and life-event payload seams are versioned | Implemented |
| Bootstrap initializes save service | PlayMode | `ILwsSaveService` reaches ready state | Implemented |
| Pixel Crushers runtime bridge | PlayMode | `LwsPixelCrushersSemanticSaver` attaches to the Save System object | Implemented |
| Manual save executes | PlayMode | `Save(profile, Slot 1)` succeeds through Pixel Crushers slot API | Implemented |
| Manual load executes | PlayMode | `Load(profile, Slot 1)` restores registered semantic payloads | Implemented |
| Manual delete executes | PlayMode | `Delete(profile, Slot 1)` clears vendor slot and LWS metadata | Implemented |
| Pause save menu accessible | PlayMode | `ILwsPersistenceMenuService.Show()` opens a clickable pause Save / Load menu | Implemented |
| Fallback driving suppression | PlayMode | Pause save menu suppresses keyboard/gamepad driving input while open | Implemented |
| Project validator | Editor menu | `Interstate Hauler / Validate Project` reports Pixel Crushers authority and old proof path removal | Implemented |

## Manual Validation In Normal Unity Editor

Primary scene: `Assets/LWS/InterstateHauler/Roads/Validation/InterstateCorridorValidation.unity`

1. Press Play.
2. Press Escape to open the persistence pause menu.
3. Create Profile A.
4. Save Slot 1.
5. Change time/weather/position where convenient.
6. Save Slot 2.
7. Save Slot 3.
8. Load Slot 1 and confirm registered semantic values restore in the current scene.
9. Delete Slot 2 and confirm Slots 1 and 3 remain occupied.
10. Create Profile B and confirm Profile A slots are not shown as Profile B saves.
11. Stop/restart Play Mode and confirm Slot 1 still exists.

Full mid-route restart/resume ordering remains Prompt 017.
