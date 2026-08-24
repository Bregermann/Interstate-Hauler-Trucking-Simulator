# Prompt 016 - Pixel Crushers API Matrix

| Feature | Pixel Crushers Class | Member | Support | LWS Usage |
|---|---|---|---|---|
| Save authority | `PixelCrushers.SaveSystem` | singleton static API | Native | Adapter invokes vendor orchestration. |
| Saver model | `PixelCrushers.Saver` | `RecordData()`, `ApplyData(string)` | Native | Documented extension point; LWS uses `SavedGameData` records in Prompt 016 due asmdef boundary. |
| Save collection | `PixelCrushers.SaveSystem` | `RecordSavedGameData()` | Native | Called before LWS semantic records are inserted. |
| Save apply | `PixelCrushers.SaveSystem` | `ApplySavedGameData(SavedGameData)` | Native | Called before LWS participants restore semantic payloads. |
| Slot save | `PixelCrushers.SaveSystem` | `SaveToSlot(int)`, `SaveToSlotImmediate(int)` | Native | Documented; Prompt 016 uses storer directly after adding LWS records. |
| Slot load | `PixelCrushers.SaveSystem` | `LoadFromSlot(int)` | Native | Documented; Prompt 016 uses retrieve/apply to avoid scene-loading side effects. |
| Save data | `PixelCrushers.SavedGameData` | `SetData(string,int,string)` | Native | Stores each LWS participant payload under its stable participant ID. |
| Load data | `PixelCrushers.SavedGameData` | `GetData(string)` | Native | Retrieves LWS participant payloads by stable ID. |
| Storer base | `PixelCrushers.SavedGameDataStorer` | `StoreSavedGameData`, `RetrieveSavedGameData`, `HasDataInSlot`, `DeleteSavedGameData` | Native | Adapter uses the active vendor storer. |
| PC disk storage | `PixelCrushers.DiskSavedGameDataStorer` | disk-backed storer | Native | `LwsPcSaveStorage` routes PC storage through this vendor storer. |
| PlayerPrefs storage | `PixelCrushers.PlayerPrefsSavedGameDataStorer` | PlayerPrefs-backed storer | Native | Available but not the preferred PC/Steam seam. |
| Serialization | `PixelCrushers.SaveSystem` | `Serialize(object)`, `Deserialize<T>(string)` | Native | Documented. LWS participant payloads use Unity `JsonUtility` inside Pixel Crushers records. |
| Scene persistence | `PixelCrushers.SaveSystem` | `saveCurrentScene`, scene load/apply flow | Native | Prompt 016 disables scene load for proof load; Prompt 017 must decide resume scene strategy. |
| Dialogue integration | `PixelCrushers.DialogueSystem.DialogueSystemSaver` | `RecordData`, `ApplyData`, `ApplyDataImmediate` | Native | Use for Dialogue System variables; do not duplicate in LWS. |
| Dialogue game saver | `PixelCrushers.DialogueSystem.GameSaver` | `SaveGame`, `LoadGame`, `RestartGame` | Native | Useful reference, but gameplay should go through `ILwsSaveService`. |
| Async save | `PixelCrushers.SaveSystem` / `SavedGameDataStorer` | `SaveToSlot`, `StoreSavedGameDataAsync` | Partial native | Available; Prompt 016 proof uses synchronous vendor storer API. |
| Encryption | `PixelCrushers.DiskSavedGameDataStorer` | `encrypt`, `encryptionPassword` | Native | Available; Prompt 018 should choose final policy. |
| Backup saves | N/A | N/A | Not identified as direct core feature | Deferred to Prompt 018. |

## Import / Assembly Notes

Pixel Crushers runtime code is installed under `Assets/Plugins/Pixel Crushers` without asmdefs. LWS runtime code is compiled by `LWS.InterstateHauler.Runtime.asmdef`. Prompt 016 therefore uses a reflective adapter for the exact public members above and does not modify vendor source or asmdefs.
