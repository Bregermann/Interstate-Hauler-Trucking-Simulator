# Prompt 017 Handoff - Mid-Route Save / Resume

Use `ILwsSaveService` as the save entry point. Do not call Pixel Crushers APIs directly from gameplay systems.

## Implemented Prompt 016 APIs

- Save service: `ILwsSaveService`
- Save implementation: `LwsSaveService`
- Pixel Crushers adapter: `LwsPixelCrushersSaveAdapter`
- PC storage seam: `ILwsSaveStorage` / `LwsPcSaveStorage`
- Proof slot: `LwsSaveService.DevelopmentTestSlot`
- Save proof: `ILwsSaveService.SaveTestState()`
- Load proof: `ILwsSaveService.LoadTestState()`
- Diagnostics: `ILwsSaveService.BuildDiagnosticsReport()`

## Pixel Crushers APIs Used

- `PixelCrushers.SaveSystem.RecordSavedGameData()`
- `PixelCrushers.SaveSystem.ApplySavedGameData(SavedGameData)`
- `PixelCrushers.SaveSystem.storer`
- `PixelCrushers.SavedGameData.SetData(string key, int sceneIndex, string data)`
- `PixelCrushers.SavedGameData.GetData(string key)`
- `PixelCrushers.SavedGameDataStorer.StoreSavedGameData(int slotNumber, SavedGameData data)`
- `PixelCrushers.SavedGameDataStorer.RetrieveSavedGameData(int slotNumber)`

## Payloads Prompt 017 Should Consume

- Global position: `LwsGlobalPositionSavePayload`
- Clock: `LwsGameClockSavePayload`
- Weather: `LwsWeatherSavePayload`
- Existing transmission participant: `Lws18SpeedTransmissionController` via `ILwsSaveParticipant`

## Required Resume Work

Prompt 017 should apply the restored global position to:

- `ILwsWorldOriginService`
- active streaming neighborhood / `ILwsWorldStreamingService`
- player tractor transform and NWH state
- trailer identity/attachment and trailer transform
- GPS destination intent and route recalculation
- road-condition service context

Dialogue System variables should use Pixel Crushers `DialogueSystemSaver`; do not duplicate Dialogue System persistence in LWS.
