# Prompt 016 - Pixel Crushers Save Architecture

## Summary

Pixel Crushers Save System is the authoritative save-state framework for Interstate: Hauler. LWS owns the project-facing facade, stable semantic payloads, and platform storage seam. Gameplay systems should use `ILwsSaveService` and should not call Pixel Crushers or platform file APIs directly.

## Installed Asset

- Exact asset: Pixel Crushers Common Save System / Dialogue System
- Version: 2.2.73.2, discovered from Pixel Crushers asset metadata
- Root path: `Assets/Plugins/Pixel Crushers`
- Main namespace: `PixelCrushers`
- Dialogue namespace: `PixelCrushers.DialogueSystem`
- Manual: `Assets/Plugins/Pixel Crushers/Common/Documentation/Save_System_Manual.pdf`

## Vendor Authority

Pixel Crushers owns save orchestration, `SavedGameData`, saver collection/application, and the storer implementation. LWS does not create a competing JSON slot framework.

Prompt 016 uses these discovered Pixel Crushers APIs through `LwsPixelCrushersSaveAdapter`:

- `PixelCrushers.SaveSystem.RecordSavedGameData()`
- `PixelCrushers.SaveSystem.ApplySavedGameData(SavedGameData)`
- `PixelCrushers.SaveSystem.storer`
- `PixelCrushers.SavedGameData.SetData(string key, int sceneIndex, string data)`
- `PixelCrushers.SavedGameData.GetData(string key)`
- `PixelCrushers.SavedGameDataStorer.StoreSavedGameData(int slotNumber, SavedGameData data)`
- `PixelCrushers.SavedGameDataStorer.RetrieveSavedGameData(int slotNumber)`
- `PixelCrushers.SavedGameDataStorer.HasDataInSlot(int slotNumber)`
- `PixelCrushers.SavedGameDataStorer.DeleteSavedGameData(int slotNumber)`

Because Pixel Crushers is installed without asmdefs while LWS uses asmdefs, the adapter resolves these exact public types/members reflectively. No vendor asmdefs were added or modified.

## LWS Save Root

- Interface: `ILwsSaveService`
- Implementation: `LwsSaveService`
- Adapter: `LwsPixelCrushersSaveAdapter`
- Storage seam: `ILwsSaveStorage`
- PC storage implementation: `LwsPcSaveStorage`

`LwsSaveService` keeps the existing LWS participant registration model, but Prompt 016 save/load operations persist those participants into Pixel Crushers `SavedGameData` records through the adapter.

## Semantic Providers

Prompt 016 registers these project-owned providers:

- `LwsGlobalPositionSaveParticipant`
- `LwsGameClockSaveParticipant`
- `LwsWeatherSaveParticipant`
- `LwsValidationSaveParticipant`

Deferred placeholders remain for truck, jobs, compass, and Dialogue System integration points until Prompts 017-018 and later gameplay prompts provide complete resume data.

## Global Position

Player location is captured from `ILwsWorldOriginService.PlayerGlobalPosition`, which is a `LwsWorldPositionD` double-precision global coordinate. The save payload stores:

- `double globalX`
- `double globalY`
- `double globalZ`
- local position diagnostic fields
- rotation quaternion seam
- origin version
- origin shift count

Prompt 016 does not yet execute the full polished world-resume sequence. Prompt 017 should consume the restored payload to restore world origin, streaming neighborhood, truck pose, trailer pose, GPS intent, and road condition context.

## Game Clock

`LwsGameClockSaveParticipant` captures `ILwsGameClockService.CurrentSnapshot` and restores through:

- `ILwsGameClockService.SetDateTime(...)`
- `ILwsGameClockService.SetTimeScale(...)`
- `ILwsGameClockService.SetPaused(...)`

Weather Maker does not own authoritative time; it should continue following the restored LWS game clock.

## Weather

`LwsWeatherSaveParticipant` captures `ILwsWeatherService.CurrentSnapshot` as semantic LWS weather state. It restores through:

- `ILwsWeatherService.SetState(...)`
- `ILwsWeatherService.SetTimeOfDayHours(...)`

Weather Maker GameObjects and internals are not serialized as authority.

## Development Proof

The development control center now has a `Save / Persistence` tab with:

- `SAVE TEST STATE`
- `LOAD TEST STATE`
- `PRINT SAVE DIAGNOSTICS`

The proof slot is `16`, exposed by `LwsSaveService.DevelopmentTestSlot`.

## Failure Behavior

If Pixel Crushers is unavailable, `LwsPixelCrushersSaveAdapter` reports `PIXEL CRUSHERS SAVE SYSTEM UNAVAILABLE` and does not fall back to a homegrown save system.

## Deferred Work

Prompt 017 owns complete mid-route save/resume for truck, trailer, streaming, GPS destination, road condition state, and player pose application.

Prompt 018 owns profiles, manual save UX, autosave, backup saves, and platform profile management.
