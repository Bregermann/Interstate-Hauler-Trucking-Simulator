# Prompt 016 - Pixel Crushers API Matrix

| Feature | Pixel Crushers Class | Member | Native Support Level | LWS Usage |
|---|---|---|---|---|
| Save authority | `PixelCrushers.SaveSystem` | singleton static API | Native | Final runtime save framework authority. |
| Runtime saver model | `PixelCrushers.Saver` | `RecordData()`, `ApplyData(string)`, `OnRestartGame()` | Native | `LwsPixelCrushersSemanticSaver` bridges LWS semantic snapshots into Pixel Crushers. |
| Slot save | `PixelCrushers.SaveSystem` | `SaveToSlotImmediate(int)`, `SaveToSlot(int)` | Native | `LwsSaveService.Save(profile, slot, overwrite)` delegates here. |
| Slot load | `PixelCrushers.SaveSystem` | `LoadFromSlot(int)` | Native | `LwsSaveService.Load(profile, slot)` delegates here. |
| Slot exists | `PixelCrushers.SaveSystem` | `HasSavedGameInSlot(int)` | Native | Used by `HasSave(profile, slot)`. |
| Slot delete | `PixelCrushers.SaveSystem` | `DeleteSavedGameInSlot(int)` | Native | Used by manual slot/profile deletion. |
| Save data container | `PixelCrushers.SavedGameData` | `SetData(string,int,string)`, `GetData(string)` | Native | Stores profile directory and LWS semantic snapshot records. |
| Storage abstraction | `PixelCrushers.SavedGameDataStorer` | `StoreSavedGameData`, `RetrieveSavedGameData`, `HasDataInSlot`, `DeleteSavedGameData` | Native | One production storage path. Console ports replace this backend. |
| PC storage | `PixelCrushers.DiskSavedGameDataStorer` | disk-backed storer | Native | Current Windows/Steam backend. |
| Alternate storage | `PixelCrushers.PlayerPrefsSavedGameDataStorer` | PlayerPrefs storer | Native | Installed but not selected for Interstate career saves. |
| Serialization | `PixelCrushers.JsonDataSerializer` | serializer component | Native | Current readable development serializer. |
| Alternate serialization | `PixelCrushers.BinaryDataSerializer` | serializer component | Native | Installed but not selected for Prompt 016. |
| Versioning | `PixelCrushers.SaveSystem` / `SavedGameData` | `version` | Native | Set from `LwsSaveSchema.CurrentVersion`. |
| Scene loading | `PixelCrushers.SaveSystem` | `saveCurrentScene`, scene transition flow | Native | Prompt 016 disables forced scene load; Prompt 017 owns resume sequencing. |
| Async save | `SaveSystem.SaveToSlot`, `SavedGameDataStorer.StoreSavedGameDataAsync` | coroutine path | Partial native | Available for Prompt 017/production busy UI. Prompt 016 uses immediate save. |
| Dialogue state | `PixelCrushers.DialogueSystem.DialogueSystemSaver` | `RecordData`, `ApplyData`, `ApplyDataImmediate` | Native | Reused for Dialogue System data under the same Save System. |
| Conversation state | `PixelCrushers.DialogueSystem.ConversationStateSaver` | vendor saver | Native | Available when active conversations need persistence. |
| Dialogue helper | `PixelCrushers.DialogueSystem.GameSaver` | `SaveGame`, `LoadGame`, `RestartGame` | Native helper | Reference/helper only; Interstate gameplay uses `ILwsSaveService`. |
| Scene Streamer state | `SceneStreamerSaver` | `RecordData`, `ApplyDataImmediate` | Native extension | Future Prompt 017 can include loaded-scene state under the same authority. |
| Love/Hate state | Love/Hate saver classes | faction/member savers | Native extension | Future reputation systems can reuse without another save framework. |

## Import / Assembly Notes

Pixel Crushers runtime code is installed under `Assets/Plugins/Pixel Crushers` without asmdefs. LWS runtime code is compiled by `LWS.InterstateHauler.Runtime.asmdef`. The LWS runtime adapter uses reflection; the Pixel Crushers `Saver` bridge lives at `Assets/LWS/InterstateHaulerPixelCrushers/Save/LwsPixelCrushersSemanticSaver.cs` so it can reference the imported vendor API directly.
