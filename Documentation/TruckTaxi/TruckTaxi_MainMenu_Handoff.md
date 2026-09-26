# Truck Taxi main menu handoff

## Integration

- Add `TruckTaxiHud.InitializeViewRoot(RectTransform root)` to assign its private view root only. Do not invoke gameplay `Initialize`.
- Invoke `EditorTruckTaxiMainMenuSetup.Configure()` once from the Editor. It creates or refreshes only `Assets/LWS/TruckTaxi/Scenes/TruckTaxi_MainMenu.unity`, using the Demo City HUD's serialized Heat prefabs/font in a preview scene.
- Register `TruckTaxi_MainMenu` before `TruckTaxi_DemoCity` in enabled Build Settings. Scene registration belongs to the parent integration pass.
- A session/history owner may call `TruckTaxiMainMenu.PublishSessionStats(TruckTaxiMainMenuStats)` before returning to menu and `ClearSessionStats()` when a new transient session should replace the old one. This snapshot is memory-only. Until published, Stats explicitly shows `NO SESSION` and zeroes.
- The menu's Load Game intentionally says `NO SAVE AVAILABLE`. Free Play remains a driving sandbox and never calls `ILwsSaveService` or creates career data.

## Manual probe for the single integration checkpoint

1. Open the generated main menu. Confirm one AudioListener; tractor and at least three Wobble passenger renderers; idle animation; no vehicle movement, physics, bootstrap, or gameplay input.
2. Navigate every menu with keyboard arrows/WASD, Enter, Escape/Backspace; repeat with gamepad D-pad/left stick, south/east. Confirm disabled Story and Arcade cannot submit.
3. Open Audio and GPS/Display, change a control, back out, reopen, and verify current values. Enter Free Play and verify those preferences carry to the city HUD.
4. Confirm Free Play loads `TruckTaxi_DemoCity`; Load reports no save; Stats shows no session or only values explicitly published by the session owner; Quit exits the player.
5. Run `TruckTaxiMainMenuTests` and the parent-owned compile/build pass once. Inspect console for missing Heat, audio routing, prefab, and scene references.

## Constraints

The setup method is intentionally not executed by this task. It never modifies Demo City, Build Settings, vendor source, save data, or audio mixer assets. It strips the tractor display instance to visual components only. Passenger figures use existing Wobble visual authoring and profile materials directly, with a lightweight idle selector; no passenger gameplay prefab runs in the menu. The generated menu has one camera and one AudioListener.
