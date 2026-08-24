# Prompt 016 - Save Test Matrix

| Test | Type | Expected Result | Status |
|---|---|---|---|
| Pixel Crushers Save System files exist | EditMode/source | `SaveSystem`, `Saver`, and `DiskSavedGameDataStorer` are installed | Implemented |
| LWS save root exists | EditMode/source | `ILwsSaveService`, `LwsSaveService`, and `LwsPixelCrushersSaveAdapter` exist | Implemented |
| Storage seam exists | EditMode/source | `ILwsSaveStorage` and `LwsPcSaveStorage` exist | Implemented |
| No direct LWS platform file writes | EditMode/source | Prompt 016 save code does not use `File.WriteAllText`, `FileStream`, `StreamWriter`, etc. | Implemented |
| Global position payload precision | EditMode | global X/Y/Z fields are `double` | Implemented |
| Semantic providers register | EditMode | global position, clock, weather, and validation providers are present | Implemented |
| Bootstrap initializes save service | PlayMode | `ILwsSaveService` reaches ready state | Implemented |
| Proof save executes | PlayMode | `SaveTestState()` succeeds through Pixel Crushers storer | Implemented |
| Proof load executes | PlayMode | `LoadTestState()` succeeds through Pixel Crushers apply path | Implemented |
| Validation variable roundtrip | PlayMode | saved marker equals loaded marker | Implemented |
| Global position payload roundtrip | PlayMode | saved global Z is restored as the captured double value | Implemented |
| Game clock payload roundtrip | PlayMode | LWS clock state is restored | Implemented |
| Weather payload roundtrip | PlayMode | LWS weather preset is restored | Implemented |

## Manual Validation

In normal Unity Editor Play Mode:

1. Open `Assets/LWS/InterstateHauler/World/Origin/Validation/IH_50MileFloatingOriginValidation.unity`.
2. Press Play.
3. Open the development control center.
4. Open `Save / Persistence`.
5. Move or teleport the truck to a distant mile.
6. Change time/weather.
7. Press `SAVE TEST STATE`.
8. Move/change time/weather again.
9. Press `LOAD TEST STATE`.
10. Confirm diagnostics report Pixel Crushers available, adapter ready, validation roundtrip pass, and the saved global position payload remains double-precision.

Full mid-route resume application remains Prompt 017.
