# Prompt 017 Handoff - Mid-Route Save / Resume

Use `ILwsSaveService` as the project-facing save entry point. Pixel Crushers Save System remains the actual save authority.

## Implemented Prompt 016 APIs

- Save facade: `ILwsSaveService`
- Save implementation: `LwsSaveService`
- Pixel Crushers adapter: `LwsPixelCrushersSaveAdapter`
- Pixel Crushers saver bridge: `LwsPixelCrushersSemanticSaver`
- Profile types: `LwsSaveProfileDirectory`, `LwsSaveProfileMetadata`, `LwsManualSaveSlotMetadata`
- Manual slots: `Save(profileId, slot, overwrite)`, `Load(profileId, slot)`, `Delete(profileId, slot)`, `HasSave(profileId, slot)`
- Runtime menu service: `ILwsPersistenceMenuService`
- Diagnostics: `ILwsSaveService.BuildDiagnosticsReport()`

## Pixel Crushers APIs Used

- `SaveSystem.SaveToSlotImmediate(int)`
- `SaveSystem.LoadFromSlot(int)`
- `SaveSystem.HasSavedGameInSlot(int)`
- `SaveSystem.DeleteSavedGameInSlot(int)`
- `SaveSystem.Serialize(object)`
- `SaveSystem.Deserialize<T>(string, T)`
- `SavedGameData.SetData(string, int, string)`
- `SavedGameData.GetData(string)`
- `SavedGameDataStorer.StoreSavedGameData(int, SavedGameData)`
- `SavedGameDataStorer.RetrieveSavedGameData(int)`
- `SavedGameDataStorer.HasDataInSlot(int)`
- `SavedGameDataStorer.DeleteSavedGameData(int)`

## Payloads Prompt 017 Should Consume

- Profile directory: `LwsSaveProfileDirectory`
- Semantic snapshot: `LwsSaveSnapshot`
- Global position: `LwsGlobalPositionSavePayload`
- Player truck: `LwsPlayerTruckSavePayload`
- Transmission: existing `Lws18SpeedTransmissionController` save participant
- Clock: `LwsGameClockSavePayload`
- Weather: `LwsWeatherSavePayload`
- Road condition: `LwsRoadConditionSavePayload`
- Navigation destination intent: `LwsNavigationSavePayload`

## Required Resume Work

Prompt 017 should finalize load ordering for:

- target scene selection and validation
- floating origin restoration before vehicle placement
- streamed road/chunk neighborhood restoration
- player tractor pose and NWH runtime state
- trailer identity/attachment and trailer pose
- GPS destination intent and route recalculation
- road-condition service context
- Save/load busy UI and post-load camera focus
- autosave, rolling backups, and corruption recovery

Dialogue System variables should use Pixel Crushers `DialogueSystemSaver`; LWS should not duplicate Dialogue System persistence.
