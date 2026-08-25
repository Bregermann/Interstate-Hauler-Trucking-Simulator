# Prompt 016 - Pixel Crushers Save Architecture

## Summary

Pixel Crushers Save System is the authoritative save-state framework for Interstate: Hauler. LWS owns only the project-facing facade, profile/slot semantics, stable semantic payloads, validation, and gameplay-facing UI state.

The final runtime flow is:

```text
Gameplay semantics
-> ILwsSaveService thin facade / LWS Saver payloads
-> Pixel Crushers SaveSystem
-> Pixel Crushers SavedGameData
-> Pixel Crushers SavedGameDataStorer
```

There is no production LWS storage backend, no in-memory production database, and no proof slot.

## Installed Asset

- Exact asset: Pixel Crushers Common Save System / Dialogue System
- Common version: 1.10.73 from installed Pixel Crushers Common skill metadata
- Dialogue System version: 2.2.73.2 from `Assets/Plugins/Pixel Crushers/Dialogue System/_README.txt`
- Root path: `Assets/Plugins/Pixel Crushers`
- Main namespace: `PixelCrushers`
- Dialogue namespace: `PixelCrushers.DialogueSystem`
- Manual: `Assets/Plugins/Pixel Crushers/Common/Documentation/Save_System_Manual.pdf`

## Vendor Authority

Pixel Crushers owns save orchestration, save/load/delete slot operations, `SavedGameData`, serializer selection, storer selection, saver registration, and persistence lifecycle.

Prompt 016 uses these discovered Pixel Crushers APIs:

- `PixelCrushers.SaveSystem.SaveToSlotImmediate(int)`
- `PixelCrushers.SaveSystem.LoadFromSlot(int)`
- `PixelCrushers.SaveSystem.HasSavedGameInSlot(int)`
- `PixelCrushers.SaveSystem.DeleteSavedGameInSlot(int)`
- `PixelCrushers.SaveSystem.Serialize(object)`
- `PixelCrushers.SaveSystem.Deserialize<T>(string, T)`
- `PixelCrushers.Saver.RecordData()`
- `PixelCrushers.Saver.ApplyData(string)`
- `PixelCrushers.SavedGameData.SetData(string key, int sceneIndex, string data)`
- `PixelCrushers.SavedGameData.GetData(string key)`
- `PixelCrushers.SavedGameDataStorer.StoreSavedGameData(int, SavedGameData)`
- `PixelCrushers.SavedGameDataStorer.RetrieveSavedGameData(int)`
- `PixelCrushers.SavedGameDataStorer.HasDataInSlot(int)`
- `PixelCrushers.SavedGameDataStorer.DeleteSavedGameData(int)`

Because Pixel Crushers is installed without asmdefs while LWS uses asmdefs, `LwsPixelCrushersSaveAdapter` resolves Pixel Crushers members reflectively. No vendor asmdefs or source files are modified.

## LWS Runtime Pieces

- Facade: `ILwsSaveService`
- Implementation: `LwsSaveService`
- Vendor adapter: `LwsPixelCrushersSaveAdapter`
- Vendor saver bridge: `LwsPixelCrushersSemanticSaver`
- Profile directory payload: `LwsSaveProfileDirectory`
- Manual slot payload: `LwsManualSaveSlotMetadata`
- Runtime player menu: `ILwsPersistenceMenuService` / `LwsPersistencePauseMenu`

`LwsSaveService` maps player-facing profile/manual slot choices to deterministic Pixel Crushers slot numbers. It does not own save files, serialize the whole game independently, or use an LWS storage backend.

## Profiles And Slots

- Default profile: `profile.development.driver`
- Minimum manual slots per profile: 3
- Profile directory vendor slot: `16000`
- First profile save slot base: `16100`
- Slots per profile namespace: 20
- Autosave and backup ranges are reserved for Prompt 017.

The player sees Slot 1, Slot 2, and Slot 3. Vendor slot numbers remain internal.

## Semantic Providers

Prompt 016 registers these project-owned providers:

- `LwsGlobalPositionSaveParticipant`
- `LwsPlayerTruckSaveParticipant`
- `LwsGameClockSaveParticipant`
- `LwsWeatherSaveParticipant`
- `LwsRoadConditionSaveParticipant`
- `LwsNavigationSaveParticipant`

The Prompt 006 transmission controller remains the transmission authority and self-registers as `vehicle.transmission.player` when present.

## Dialogue System

Dialogue System state should use Pixel Crushers `DialogueSystemSaver`, `ConversationStateSaver`, and related vendor components. LWS does not duplicate dialogue persistence. Dialogue savers and LWS semantic saver coexist under the same Pixel Crushers `SaveSystem` instance.

## Player-Facing Menu

`LwsPersistencePauseMenu` provides the current gameplay pause save/load UI:

- Resume
- Save / Load
- Profiles
- Save Slot 1-3
- Load occupied slots
- Overwrite with confirmation
- Delete with confirmation
- Create/select/rename/delete profiles

This is not a final Heat UI screen, but it is a player-facing runtime menu with clickable Unity UI controls. The dev control center only links to it and prints diagnostics.

## Deferred Work

Prompt 017 owns complete mid-route load ordering, scene restart/resume sequencing, autosave, rolling backups, corruption handling, and deeper streaming/trailer recovery.
