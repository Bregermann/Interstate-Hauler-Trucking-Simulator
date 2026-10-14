# Truck Sports final Windows validation — 2026-10-14

## Build and preservation

Executable: F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckSports\TruckSports.exe. Unity 6000.4.10f1, Windows x64 Mono Development. Final native batch invocation: LWS.TruckSports.Editor.SportsBuilder.Build against saved scenes (build-16.log, actual Windows exit 0). Build report: zero errors, 25 inherited vendor/package warnings, 294,803,459 reported bytes, 46.722955 seconds. Startup/order: TruckSports, TruckBilliards, TruckHockey, TruckBowling, TruckAirHockey. All runtime and development companions remain together; build-manifest.csv inventories the payload. CreateValidateBuild was inspected and avoided because it reauthors scenes. Updated authoring retains current tuning and all four selections.

Scoped title and explicit scene array preserve host build configuration. Native success and deliberate-failure restoration fixtures passed. All 25 ProjectSettings SHA256 baselines and all 229 Billiards/Bowling baseline files match. Original evidence: settings-baseline.csv and preserved-sports.csv under Logs/TruckSports/AirHockey. Their gameplay, AI, handling, scenes, UI and tuning are unchanged. Unrelated Taxi/world/vendor changes are excluded from sports delivery. Generated logs, captures, caches and builds are not committed.

## Final executable cases

Each case ran sequentially in the actual final executable and recorded Windows exit 0. No failed assertion or captured managed error was reported. Evidence/captures: Logs/TruckSports/AirHockey/<case>. Native build also passed retained rules/references, including 26 Air and 32 Bowling rule fixtures.

| Case | Passed assertions | Windows exit |
| --- | ---: | ---: |
| Arena-16-720 | 77 | 0 |
| Arena-16-1080 | 77 | 0 |
| Hockey-16-720 | 118 | 0 |
| Billiards-16 | 54 | 0 |
| Bowling-16 | 80 | 0 |
| Six-AI-16 | 17 | 0 |
| Air-AI-16 | 22 | 0 |

Arena: enlarged rink/coasting/drift/hits; original-size Air table/no stick/named HUD; whole-body center/goal/rounded-rail confinement; freely crossing puck, physical truck contact and repeated banks; whole-crossing/own goals and conceding serve; sealed/escaped recovery while accessible stationary pucks remain untouched; keyboard/separate/whole/shared-stick/mixed ownership and neutral/fresh reconnect/replacement; accessible corner and center physically cleared by AI without faceoff; repeated entries/exits of all four sports. Both resolutions were inspected through native captures for front end, Ice/Air HUD and rounded tabletop. Partial-throttle physical fixtures verify drive/reverse limits independently of impact headroom.

Hockey: six keyboard sources, three shared pads, six whole pads, mixed input, duplicate rejection, brake/reverse, individual recovery/contact cleanup, stationary accessible puck/temporary defensive crowds, contact feedback, goals/overtime, side swaps, rematches and collection cleanup.

Billiards: physical opening contact, held driving through ordinary turns, AI scratch placement then real cue contact, near-rail eligible-eight contact, physical own-ball pot then retained-turn second strike, stronger full-rack opening containment, off-turn roaming and group-selection owner/reconnect/mouse UI. Modal/rule outcomes and initial layouts are injected only where identified; these are targeted reproductions, not autonomous full-rack evidence. Bowling retains physical delivery/pinfall, inputs/rules, containment and lifecycle checks. No human playtest is claimed.

## Physical measurements and sustained performance

Final focused physical measurements:

- PASS partial throttle respects Ice drive limit speed=11.11
- PASS partial throttle respects Air drive limit speed=8.91
- PASS partial throttle respects reverse limit speed=5.99
- PASS long coasting retains speed=7.27
- PASS long lateral slide=1.95
- PASS faster truck physically launches slower truck distance=4.74 speed=6.90
- PASS equal-speed head-on mutual rebound -4.99/4.99
- PASS similar-speed contact adds no large kick
- PASS repeated contact bounded/upright peak=3.81

These are controlled fixture states, separate from complete matches. PhysX alone owns collision impulses. Lower Ice grip/drag and higher restitution produce displacement; normal relative closing speed scales feedback. Partial throttle now respects the drive limit while genuine collision velocity retains bounded headroom.

Six-AI-16: three complete two-minute regulation matches, plus applicable overtime/goal pauses, and two rematches. Every bot participates physically. No successful goals, live actor positions or puck velocities are injected.

- PASS completed physical six-AI match 0 winner=0 goals=2 emergencyFaceoffs=0 queryOverflows=0
- PASS completed physical six-AI match 1 winner=1 goals=5 emergencyFaceoffs=0 queryOverflows=0
- PASS completed physical six-AI match 2 winner=0 goals=3 emergencyFaceoffs=0 queryOverflows=0
- SUSTAINED elapsedSeconds=421.2 three complete two-minute regulation matches including overtime / pauses between goals; totalGoals=10 emergencyFaceoffs=0
- ENVIRONMENT GPU=NVIDIA GeForce GTX 1660 Ti API=Direct3D11 CPU=Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz resolution=1280x720 vSync=1 targetFrameRate=-1
- FRAME SAMPLE live frames=54688 meanMs=6.71 p95Ms=12.46 p99Ms=12.52 worstMs=74.88 managedBytesDelta=5304320 gen0Collections=1

Air-AI-16: full first-to-seven match/rematch, alternating opener, retained identity/configuration and stable results. Whole-body half/rounded-rail containment is checked every frame. Normal AI controls and physical contact only; no successful scores, live actor placements or puck velocities are injected after normal startup/reset. Final scores: Azure AI 7:1 Amber AI, then Azure AI 7:4 Amber AI.

- PASS full physical first-to-7 match 0
- PASS full physical first-to-7 match 1
- PASS repeated real truck / rail contacts 262/526
- SUSTAINED two full matches elapsed=576.1 timeScale=1; no score/puck velocity/actor placement injection
- GPU=NVIDIA GeForce GTX 1660 Ti API=Direct3D11 resolution=1280x720 timeScale=0
- PERF frames=79893 meanMs=6.45 p95Ms=6.32 p99Ms=12.50 worstMs=31.21 managedDelta=3133440 GC0=4
- RuntimeErrors=0 ExitRequest=0

Editor and TruckTaxi were closed for both final sustained runs. OBS and ordinary desktop applications remained active. GTX 1660 Ti / Direct3D11 / i9-9900K, 1280×720. Samples include development/probe overhead and desktop scheduling; they are not a release benchmark or leak proof. Short scene-loading fixtures are separate. Earlier Air-AI-15 passed gameplay but shared-load timing (mean 34.61 ms/p95 200.03 ms) included TruckTaxi/OBS; Six-AI-15 also included TruckTaxi initially. Neither is the final performance baseline.

## Repaired and interrupted evidence

Detached truck visuals came from geometry authored after moving its parent. Native authoring now creates geometry at origin before spawn/yaw and removes the separate bumper/stick. Bounds/no-stick validation and native captures verify this.

Shared-pad reconnect previously consumed held confirmation and selected Front. Air modal actions now require neutral/fresh confirmation and select the primary action when ready. Expanded final tests pass. Billiards ordinary turns retain continuous held-input behavior.

Builds 04/07–10 reproduced accessible corner stalls; isolated build-08 passed while the combined case failed. Rail AI now approaches reachable interior waypoints, makes shallow physical side contacts, backs off and clears plans on serve reset. Build 11 completed one Air match but failed rematch half containment; current/future rotation and next integrated translation are now bounded. At the user's request, radius-2.5 corners now use a saved rounded tabletop and 48 overlapping curved boards. Whole-box constraints/recovery match the outline; AI forecasts real static colliders using 64 hits plus saturation fallback. Final combined corner and full-match tests pass.

Six-AI-11-Interrupted contains two completed matches and part of a third when the PC crashed, not a successful full suite. Air-AI-13 was closed at the rounded-corner request. Air-AI-14 was interrupted by menu navigation/unload during rematch and retained as failed harness evidence. Explicit development probes now mute native input while preserving injected devices, preventing desktop typing from navigating tests. Normal launches have no isolation. Build-15's 1080p launcher alias was misspelled; final build-16 actually ran both resolutions. Failed/interrupted samples are not counted as successful.

## Remaining hands-on checks and delivery

Input checks use injected Unity Keyboard/Gamepad/Mouse devices; no physical gamepad was connected before injection. Routing/ownership/reconnect passes do not establish USB capacity, keyboard rollover, ergonomics or physical rumble. A human should assess all AI difficulty settings, balance, steering/impact feel, perceived audio/settings mix and real-device reconnect/rumble. Complete AI evidence covers the tested Hard/Normal Air pairing and six-bot Ice setup, not exhaustive difficulty balance.

Final probes restore setup registry values. The earlier PC crash interrupted an in-memory backup of the personal Hockey preset; exact pre-crash setup is not recoverable from that run. Final tests preserve post-restart presets; reselect Hockey participants if needed. Shared settings and Billiards/Bowling assets are preserved.

Native UI Automation was avoided because of the previously identified Unity accessibility/shutdown crash. Screenshots come from in-player ScreenCapture, and actual native exits/logs are checked without that attachment. Normal launch uses no flags. Git delivery hashes/status: Builds/TruckSports/GIT-DELIVERY.txt and final handoff. Published 3v3/Bowling history remains ancestry.

Vendor report: README.md lists exact audited/used APIs and exclusions. VENDOR SOURCE MODIFIED: NO. DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
