# Prompt 017 - Console Persistence Handoff

The production save framework remains Pixel Crushers Save System. Current Windows/Steam builds use `PixelCrushers.DiskSavedGameDataStorer`; console builds should replace the Pixel Crushers `SavedGameDataStorer` backend, not the LWS semantic providers or slot/profile semantics.

## Current PC Stack

- Save authority: `PixelCrushers.SaveSystem`
- Save container: `PixelCrushers.SavedGameData`
- Storage abstraction: `PixelCrushers.SavedGameDataStorer`
- Current PC backend: `PixelCrushers.DiskSavedGameDataStorer`
- Serializer: `PixelCrushers.JsonDataSerializer`
- LWS facade: `ILwsSaveService`

## Console Replacement Point

Replace or configure the active Pixel Crushers storer for the target platform. Keep these LWS concepts unchanged unless certification requires a slot-layout change:

- profile stable IDs
- player-facing manual slot numbers
- autosave slot semantics
- hidden backup semantics
- world resume context
- semantic save participants
- load coordinator ordering

## No Direct Filesystem Dependency

Gameplay persistence does not call platform filesystem APIs. Backups copy vendor `SavedGameData` through `SavedGameDataStorer.RetrieveSavedGameData` and `SavedGameDataStorer.StoreSavedGameData`. Manual saves, autosaves, backups, recovery, and profile deletion all remain behind the vendor storer abstraction.

## Scene Streamer

The installed Pixel Crushers `SceneStreamerSaver` remains available for future Scene Streamer integration, but Prompt 017 does not activate it for the current authored validation scene. Future streaming work can satisfy `PrepareWorldForGlobalPosition` through the existing world streaming service and then continue using the same Pixel Crushers save flow.