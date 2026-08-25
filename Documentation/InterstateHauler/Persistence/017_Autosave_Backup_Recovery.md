# Prompt 017 - Autosave Backup Recovery

Autosaves and backups use the same Pixel Crushers Save System, `SavedGameData`, and `SavedGameDataStorer` path as manual saves. There is no LWS file copy, no custom autosave format, and no separate backup serializer.

## Slot Schema

Profile directory vendor slot: `16000`.

Profile namespaces start at `16100` and reserve 20 vendor slots per profile index.

For profile index 0:

| Player/System Slot | Vendor Slot |
|---|---:|
| Manual Slot 1 | 16101 |
| Manual Slot 2 | 16102 |
| Manual Slot 3 | 16103 |
| Autosave | 16111 |
| Manual Slot 1 Backup | 16116 |
| Manual Slot 2 Backup | 16117 |
| Manual Slot 3 Backup | 16118 |
| Autosave Backup | 16119 |

For profile index N, add `N * 20` to the profile namespace slots above.

## Autosave

`LwsAutosaveConfiguration` defaults to:

- autosave enabled
- autosave interval: 300 seconds
- minimum time between autosaves: 60 seconds

`LwsSaveService.TickAutosave` drives periodic safe autosaves through a small runtime driver. `RequestAutosave(reason)` marks `PendingAutosave` when the current state is unsafe and performs one autosave at the next safe opportunity. Manual slots are not overwritten by autosave.

## Backup Flow

Before overwriting an occupied manual slot or autosave slot, `LwsSaveService.PrepareBackupBeforeOverwrite` calls `LwsPixelCrushersSaveAdapter.CopySavedGameDataSlot`.

That adapter uses Pixel Crushers storer APIs:

- `SavedGameDataStorer.HasDataInSlot`
- `SavedGameDataStorer.RetrieveSavedGameData`
- `SavedGameDataStorer.StoreSavedGameData`

It does not call `File.Copy` or inspect Pixel Crushers disk filenames.

## Recovery Flow

If a primary load fails validation or application, `LwsSaveService.PrepareRecoveryOffer` checks the matching hidden backup slot. If the backup pre-read validates, the pause menu shows:

`SAVE COULD NOT BE LOADED. A BACKUP FROM [timestamp] IS AVAILABLE.`

The player can choose `LOAD BACKUP` or cancel. Loading a backup uses the same pre-read, world preparation, Pixel Crushers application, and finalization coordinator as a normal slot.

## Delete Behavior

Deleting a manual save deletes its hidden backup. Deleting autosave deletes autosave backup. Deleting a profile iterates the reserved vendor slots for that profile and removes manual slots, autosave, backups, and profile metadata while leaving other profiles untouched.

## UI

`LwsPersistencePauseMenu` keeps the Prompt 016 menu and adds:

- autosave row
- save/load busy status
- loading-world status
- recovery offer row
- disabled save/load/delete/profile switching during conflicting transactions