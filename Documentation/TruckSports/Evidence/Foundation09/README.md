# Release Foundation — observed Windows evidence

These are actual game-generated screenshots, inspected at720p. Software input is not physical-controller or human-playtest acceptance.

- `final-lobby-720p.png`: two profiles ready, two open seats, clean host controls. The title canvas no longer overlaps the lobby.
- `final-keyboard-720p.png`: owner-controlled name entry with readable focus and action prompts.
- `relaunch-statistics-720p.png`: saved profile statistics after closing and relaunching the executable. The first legacy profile's original creation date is unknown; the UI does not invent one.
- `visible03-lobby-720p.png`: retained before-fix evidence of title-canvas overlap.
- `visible03-setup-TruckDanceOff-720p.png` and `visible03-gameplay-TruckDanceOff-720p.png`: actual rendered setup and beginning of the song, not evidence of a completed visible match.

| Run | Actual assertion result | Native process result |
|---|---|---|
| Windows01 | Failed before joining; hidden-window software input was focus-filtered. | Access violation on exit. |
| Windows02 | Two actual completed matches, lobby return, independent drive input, statistics, JSON/text export and telemetry end event passed. AI won both matches; human profiles each retained two losses and zero winner points. | Access violation on exit. Hidden captures are black and rejected as visual evidence. |
| Windows03 | Keyboard/setup/gameplay rendered; exposed lobby overlap. Dance-Off subsequently reached the480-second deadline without completing. | Access violation on exit. Failure retained. |
| Windows04 | Short lobby/keyboard retake passed after the canvas fix. No complete-match claim for this run. | Access violation on exit. |
| Windows02 relaunch | Both saved profiles and their statistics compare identically before joining a new night; visible statistics inspected. | Access violation on exit. |
| Ordinary menu quit | Normal title and quit action, input isolation inactive, no synthetic controllers or active Game Night. | Same access violation; the failure is not limited to software-input cleanup. Native module/cause remains unconfirmed. |

The candidate is not certified as a clean Windows release. The previous working installation is preserved. Raw logs and full validation reports stay in `Logs/TruckSports/Foundation09`; no physical listening, device or1080p visual acceptance is inferred.
