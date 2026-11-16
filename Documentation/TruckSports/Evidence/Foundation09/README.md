# Release Foundation — observed Windows evidence

These are actual game-generated screenshots, inspected at720p. Software input is not physical-controller or human-playtest acceptance.

- `final-lobby-720p.png`: two profiles ready, two open seats, clean host controls. The title canvas no longer overlaps the lobby.
- `final-keyboard-720p.png`: owner-controlled name entry with readable focus and action prompts.
- `relaunch-statistics-720p.png`: saved profile statistics after closing and relaunching the executable. The first legacy profile's original creation date is unknown; the UI does not invent one.
- `category-filter-720p.png`: existing sport cards filtered to Racing, with a controller-selected Heat control and preserved previews/logos. Host-only change and B-to-lobby assertions passed.
- `visible-dance-results-720p.png`: actual completed visible Dance-Off result from Windows05.
- `whack-startup-state-720p.png`: real advancing Whack simulation. The separate short focus-pause/controller-resume acceptance passed afterward.
- `visible03-lobby-720p.png`: retained before-fix evidence of title-canvas overlap.
- `visible03-setup-TruckDanceOff-720p.png` and `visible03-gameplay-TruckDanceOff-720p.png`: actual rendered setup and beginning of the song, not evidence of a completed visible match.

| Run | Actual assertion result | Native process result |
|---|---|---|
| Windows01 | Failed before joining; hidden-window software input was focus-filtered. | Access violation on exit. |
| Windows02 | Two actual completed matches, lobby return, independent drive input, statistics, JSON/text export and telemetry end event passed. AI won both matches; human profiles each retained two losses and zero winner points. | Access violation on exit. Hidden captures are black and rejected as visual evidence. |
| Windows03 | Keyboard/setup/gameplay rendered; exposed lobby overlap. Dance-Off subsequently reached the480-second deadline without completing. | Access violation on exit. Failure retained. |
| Windows04 | Short lobby/keyboard retake passed after the canvas fix. No complete-match claim for this run. | Access violation on exit. |
| Windows05 | Visible Dance-Off completes normally. Whack starts, then the run reaches its external360-second limit. Stream samples continue around150–160FPS without a controller removal. | Owned test process terminated at the recorded limit; this is a timeout, not a native exit result. |
| Windows02 relaunch | Both saved profiles and their statistics compare identically before joining a new night; visible statistics inspected. | Access violation on exit. |
| Ordinary menu quit | Normal title and quit action, input isolation inactive, no synthetic controllers or active Game Night. | Same access violation; the failure is not limited to software-input cleanup. Native module/cause remains unconfirmed. |
| Native lifetime hypothesis | A Sports-owned adapter called the vendor's existing StopDirectInput API on quit. | Failure remained; experiment reverted completely. No vendor source changes retained. |
| StartupState01 | Five-second real Whack simulation passes: running, unpaused, Play screen, time scale1, both controllers ready, clock0→3.68. No scores/results injected. | Exit still fails. This does not certify a full match. |
| FocusResume01 | Real simulation clock0→3.70; explicit simulated focus loss pauses, host controller Start resumes. Actual focus_lost/match_paused/match_resumed stream events verified. | Native exit failure remains. |
| CatalogueControls01 | Actual host category selection, nonhost rejection, restored selection canvas and host B-to-lobby pass.720p image inspected. | Native exit failure remains. |

The existing `SportsFocusPolicy` deliberately pauses human games on focus loss. The previous full-match driver did not resume that menu. This is consistent with the visible timeouts; their exact historical cause was not logged. Current instrumentation records focus loss, pause/resume and sampled time scale/focus so subsequent reports can distinguish intentional pauses from simulation failures. A short explicit focus-pause/controller-resume check is kept separate from complete-match claims.

The candidate is not certified as a clean Windows release. The previous working installation is preserved. Raw logs and full validation reports stay in `Logs/TruckSports/Foundation09`; no physical listening, device or1080p visual acceptance is inferred.
