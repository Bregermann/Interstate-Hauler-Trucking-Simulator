# F3 Save / Load Menu

## Summary

The development save/load entry point is now F3. It opens the existing Pixel Crushers-backed LWS persistence menu directly on the Save / Load view.

This does not create a second save system. The UI talks only to `ILwsPersistenceMenuService`, which uses `LwsPersistencePauseMenu`, which calls the existing `ILwsSaveService` facade from Prompt 016/017.

## Controls

- `F3`: open/close the direct Save / Load menu.
- `Escape`: open/close the pause root menu.
- Pause menu `SAVE / LOAD`: opens the same Save / Load screen used by F3.
- Save / Load opened by F3 shows `CLOSE` and returns directly to gameplay.
- Save / Load opened from pause shows `BACK` and returns to the pause root.

## Pause Root

The Escape pause root contains:

- `RESUME`
- `SAVE / LOAD`
- `SETTINGS (COMING LATER)`
- `CONTROLS (COMING LATER)`
- `QUIT TO MAIN MENU (COMING LATER)`
- `QUIT GAME`

Settings, Controls, and Quit To Main Menu are intentionally disabled until their production flows exist.

## Save Authority

Production save authority remains Pixel Crushers:

`LwsPersistencePauseMenu -> ILwsSaveService -> LwsPixelCrushersSaveAdapter / LwsPixelCrushersSemanticSaver -> Pixel Crushers SaveSystem -> SavedGameDataStorer`

The menu does not call Pixel Crushers static save APIs directly and does not use a custom LWS serializer, custom slot database, or direct file path.

## Layout

The menu uses the same broad, readable development-panel visual language as the F2 weather panel:

- screen-space overlay canvas
- `CanvasScaler.ScaleWithScreenSize`
- 1920 x 1080 reference resolution
- large panel from 8 percent to 92 percent screen width and 4 percent to 96 percent height
- 60 pixel preferred button height
- large profile/autosave/manual slot rows
- dark panel, teal buttons, muted disabled rows

This replaces the earlier too-small save presentation that was hard to read in 1920/2560 desktop testing.

## Overlay Exclusivity

The development overlays are treated as one family:

- opening F3 closes the dev control center, full map, and F2 weather test panel
- opening F2/weather, control center, or big map closes the persistence menu
- the DEV button hides while any major overlay is open
- driving input is suppressed while F3 or Escape persistence UI is open
- cursor is visible and unlocked while the menu is open, then restored on close

## Persistence Features Exposed

The F3 menu exposes the existing Prompt 017 persistence features:

- active profile
- profile selection/creation/rename/delete
- autosave row
- manual slots 1-3
- overwrite confirmation
- delete confirmation
- recovery offer row when a backup is available
- busy state labels for saving/loading/world loading
- last save failure text when present

## Validation Notes

Automated coverage was added for the source-level F3 contract and PlayMode service entry points. Visual validation still requires normal Unity Editor play mode because the final requirement is readability and interaction in the actual game viewport.