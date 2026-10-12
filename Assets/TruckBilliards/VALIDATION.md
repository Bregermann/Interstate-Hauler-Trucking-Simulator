> Historical standalone validation. The current combined Truck Sports build, repaired defects, actual process exits and manual limitations are recorded in ../TruckSports/VALIDATION.md.

# Truck Billiards validation — series / difficulty / Practice / feedback
Date: 2026-10-12. Implementation and Windows x64 development build COMPLETE; hardware/native-input/listening gates remain before production release.

## Artifact and scope
Executable: F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckBilliards\TruckBilliards.exe
Unity 6000.4.10f1; Mono Windows x64 development player. TruckBilliards is the only/startup scene. Required generated companions remain together: TruckBilliards_Data, UnityPlayer.dll, MonoBleedingEdge, native libraries, crash handler and development debug files.
Mode build: LWS.TruckBilliards.Editor.BilliardsBuilder.Build. Saved-scene/menu upgrade: BilliardsMenuBuilder.UpgradeBuild. Final build log: Logs/TruckBilliards/features-build-release.log; succeeded, Editor exit 0, zero errors, 25 existing host/vendor warnings. No TruckBilliards compiler warnings reported.
The build uses its own scene array; host/Truck Taxi scene lists/profiles were not replaced. Builder compares EditorBuildSettings bytes before/after. SHA256 remains 9554C220A85A14A6769EBB339F6EA3E56A83F6C78B02DFAF23CBE08938E7BFA6. Source changes are confined to Assets/TruckBilliards; logs and build artifacts are outside Assets.
Truck handling, swept capped bumper strike, ball physics, existing competitive pool rules and local control modes were preserved. Arcade and truck-character systems were not introduced.

## Strongest evidence
- Unity compilation and actual saved-scene validation: 16 balls, six pockets, two collider-free truck rigs, camera/AI/HUD/session/input/settings, series menus, eight clips and celebration references. No missing scripts; all asset metadata present.
- BilliardsRuleFixtures: 44 assertions for existing rules, including early/illegal 8 and 8 plus scratch.
- BilliardsFeatureFixtures: 72 assertions for Single Rack, 2–0/2–1 Best of 3 and 3–0/3–2 Best of 5; exactly-once awards; breaker alternation; early stopping/rematch; Practice 8/no-groups/no-winner, one strike, scratch/manual placement, competitive manual placement refusal.
- Final 720p feature player: 92 PASS checks, TB_LOCAL_FINISHED code=0, no captured runtime errors.
- Final 1080p feature player: 92 PASS checks, TB_LOCAL_FINISHED code=0, no captured runtime errors; inspected Setup, Result, Practice/Pause and Settings captures are readable.
- Existing local multiplayer regression: 58 PASS checks on this final executable, TB_LOCAL_FINISHED code=0, no captured runtime errors.

## Acceptance / evidence matrix
| Feature | Status and actual evidence |
|---|---|
| Series formats / scoreboard | PASS. Setup selections, names/wins/rack/target score display; all requested scorelines and duplicate resolution callbacks tested. BilliardsRules remains rack authority; BilliardsSeries only records its winner. |
| Between racks | PASS. Next Rack alternates opener, clears groups/shot/pockets/velocities/preview, preserves identities/devices/format/difficulty/score. Deciding rack stops; Rematch clears score and alternates the previous original opener. |
| Restart actions | PASS. Restart Rack during motion preserves previous score/current breaker. Restart Series clears score/rack count and restores original opener. Confirmation cancellation also remains in the existing suite. |
| Multiplayer series | PASS software fixtures. Shared keyboard, distinct pads and hot seat; result-screen disconnect/reconnect restores Result, not live ended-rack gameplay. Hot-seat next rack requires fresh incoming confirmation and ignores held Enter/South. |
| AI difficulty / persistence | PASS. Actual setup selection persisted and reloaded. Three identical layouts × three levels × 12 fixed seeds measured planning differences; every level then physically drove and bumper-struck the opening cue within the shared power cap. See comparison below. |
| Practice | PASS. One active truck, no AI/group/turn/winner. Actual cue/object motion physically pots the 8 and leaves it down without a competitive outcome. Actual cue scratch opens same-player placement after settling. Driving continues during resolution while another strike is forbidden. |
| Practice tools | PASS. Keyboard and assigned-controller placement restore control. Manual reposition works when settled, is blocked during motion, and is refused in competitive play. Reset Rack mid-shot clears velocities, shot/pocket/preview state. Truck reset remains separate. |
| Practice ownership | PASS injected pads. First joined controller stays locked; another pad's North cannot overwrite it. Inactive second slot holds no pad/control assignment. |
| Audio | PASS routing/transport checks. Eight distinct original PCM assets wired, non-identical hashes, no clipped samples (measured peaks .45–.80). Real ball pair produces exactly one clack; real cushion collision routes a distinct effect. Strength scaling, maximum impact headroom, six-voice cap and Master/SFX mute checked. Listening to the mix is NOT RUN. |
| Haptics | PASS software request routing/expiry/lifecycle checks only. Correct assigned pad, persistent Off blocks requests, pause/disconnect/disable/Main and unscaled expiry clear requests. Shared-device handoff stops requests. Missing support caused no runtime exceptions. Actual motor output is NOT RUN. |
| Celebrations | PASS runtime and visual inspection. Winner-colored rack burst vs stronger series burst/sting, score/name/result buttons readable. Repeat callbacks do not replay/award; immediate continuation clears the effect. Practice does not trigger competitive bursts. |
| Repeated transitions / prior modes | PASS software/runtime fixtures. No stale AI, input, placement, ball registry or win effects observed after reset/rack/series/menu transitions. Existing physical full-match regression uses real motion/capture. |
| UI | PASS inspected standalone captures: Main, single-player and two-pad Setup, rack/series Result, Practice panel/Pause and Vibration Settings. No overlapping controls found in the inspected 720p and 1080p captures. |

## AI comparison: observed plans, not win rates
Tuning is serialized in BilliardsAI.easy/normal/hard. Easy aim/power bounds ±5.5°/±28%, Normal ±1.7°/±12%, Hard ±.55°/±4.5%. Easy requires alignment .8 and pot distance ≤8 with simple first-target selection; Normal .25 retains baseline scoring; Hard .32 adds reachable ghost/approach checks, rail/approach scoring and cut-aware power. All use the same truck/ball physics and 15 shot-speed cap. Shared timeout 12 seconds, three recoveries; reachable legal fallback preferred. Error is sampled once per planned attempt.
Comparison layouts use cue (0, .5, -6), ball 9 (-2, .5, 7), and:
| Layout | Ball 1 | Ball 2 |
|---|---|---|
| 0 | (0, .5, -1) | (3, .5, 4) |
| 1 | (-3, .5, -2) | (3, .5, 4) |
| 2 | (0, .5, 2) | (0, .5, -1) |
Remaining objects are inactive only in the test layout. Identical seeds 123–134:
| Difficulty | Mean absolute sampled aim error | Mean absolute power error | Pot candidates for layouts 0 / 1 / 2 |
|---|---:|---:|---|
| Easy | 2.8450° | .1481 | 0 / 1 / 0 (legal fallback in 0 and 2) |
| Normal | .8794° | .0635 | 4 / 6 / 3 |
| Hard | .2845° | .0238 | 4 / 6 / 3 |
Normal and Hard chose the same target/pocket in these three layouts; Hard used different useful-power evaluation and smaller sampled error. This establishes observed planning differences, not superior pot percentages or human win rates. Physical truck approaches can add deviation beyond the sampled plan error.
CSV records final-seed target/pocket/power and the 12-seed error means. Physical opening strikes on all three levels passed separately; full long-play difficulty balance is still a human playtest.

## Exact evidence and limitations
- Logs/TruckBilliards/features-build-release.log, build-summary.txt.
- Logs/TruckBilliards/features-release-720.log and Features-Release-720/results.txt/screenshots/ai-comparison.csv.
- Logs/TruckBilliards/features-release-1080.log and Features-Release-1080 artifacts.
- Logs/TruckBilliards/features-release-regression.log and Features-Release-Regression artifacts.
- Logs/TruckBilliards/authored-audio-metrics.csv: distinct SHA256s, raw WAV peaks.
- Latest optional outputs: Builds/TruckBilliards/FeatureSmokeTest and LocalSmokeTest. Development flags: -tbFeatureSmoke and -tbLocalSmoke. Normal launch does not run probes.

Series outcome/restart fixtures deliberately supply authoritative rule facts. They prove scoring/state flow, not physically played full series. Practice pocket/cushion/scratch tests and AI strikes require real motion/contact/capture; no recorded pocket facts are injected for those tests. Existing H-v-H/single-player full-match fixtures reposition balls between shots, then use real physics. They are not unassisted human matches.
Input uses virtual Keyboard/Mouse/Gamepad state events; haptic checks observe routed software requests, not hardware motors. The test harness clones in-memory Input System background settings to avoid desktop focus interference, then restores settings/preferences/devices. Zero physical gamepads were observed before injection.
Normal final executable launched successfully (process 25212), exposed a targetable Interstate Hauler window, and emitted no runtime exceptions/errors in Logs/TruckBilliards/features-native-player.log. Computer Use capture failed with "FrameArrived timed out: timed out waiting on channel". Fresh window selection/activation and one retry failed with "window capture timed out: timed out waiting on channel". No native inputs were sent and no native-input PASS is claimed. The player remains open at startup for a human smoke test.

## Repairs and test corrections during this pass
- Pending handoff remains independent of result/disconnect screens; between-rack/series restart ownership is explicit.
- Immediate HUD refresh eliminates the inspected one-frame stale text when changing mode/returning Main. Main restores the idle rack and clears velocities/AI; rack/series wording is distinct.
- Practice inactive player slot no longer retains a controller, and join routing does not overwrite its locked device without explicit reassignment.
- Setup spacing/autosizing leaves room for both controller-binding rows and Back/Start.
- Impact headroom and quieter cue strikes reserve room for simultaneous break effects; all eight audio clips are original procedural assets.
- New capture timing initially missed early menu frames. Tests now yield after captures. Adding a pause screenshot exposed a fixture that attempted W before a neutral post-Resume frame; the game correctly consumed it, and fixture timing was repaired.
No failing assertion was disabled, no vendor source was patched, and no unrelated host tests/settings were changed to get a pass.

## Remaining release checks / known limitations
Subsequent player-reported AI forfeits and transition freezes supersede this historical result. See ../TruckSports/UPDATE_3V3.md for their reproduction and current validation. Required production gates:
1. Native menus/name editing, sustained keyboard/mouse driving and cue placement; unassisted full single-player and multiplayer series at both supported sizes.
2. Two physical pads and one-pad Practice/hot seat: device mapping, independent ownership, unplug/replug/replacement, fresh handoffs after scratches/between racks, and actual strike/score/win rumble with On/Off and pause/quit stopping motors.
3. Listen on real speakers/headphones: break voice balance, gentle versus strong contacts, distinct clack/cushion/pocket/turn/foul/win cues and both mute settings.
4. Long-play AI difficulty/balance, constrained approach recovery, ordinary six-pocket/edge shots, restart mid-motion and repeated rack/series transitions.
5. Display switching/relaunch and settings persistence on actual displays.
The requested artifact is a development build. OS window/product title still inherits Interstate Hauler to preserve host PlayerSettings; distribution branding and production packaging remain later release work. These do not block this Windows build.

## Vendor report
VENDOR ASSETS AUDITED: NWH Vehicle Physics 2 / GR3D; Heat control APIs/navigation and Audio; UTS traffic foundation; vehicle/environment audio directories; DOTween readme/public APIs; Input System and declared DirectInput/wheel backend.
VENDOR ASSETS USED: Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab render geometry retained; Assets/Heat - Complete Modern UI/Scripts/UI Elements ButtonManager/SliderManager/InputFieldManager, and Audio/Click.wav; Assets/Plugins/Demigiant/DOTween/DOTween.dll (DOTween.To, DOScale, SetUpdate, SetEase, Kill); Unity Input System 1.19, URP/uGUI/TextMeshPro.
RELEVANT ASSETS NOT USED: Full NWH/UTS road dynamics and wheel/DirectInput backend are unnecessary for the authorized miniature mode. Audited vehicle/environment/menu clips did not contain a pool sound set; original pool effects were authored. No vendor supplies pool series/Practice semantics or this shot evaluation.
CUSTOM SYSTEMS CREATED: Series bookkeeping around existing rack authority; Practice flag in that authority; difficulty tuning/evaluation; six-voice feedback and assigned-pad haptic adapter; pool-specific DOTween celebration orchestration; focused fixtures and mode UI bindings.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


## Truck Sports collection regression — 2026-10-12

The final Truck Sports Windows x64 development executable includes this unchanged saved Billiards scene. Logs/TruckSports/billiards-features-11.log and Billiards-Features-11/results.txt: 92 passing existing feature checks, exit 0, no captured runtime errors. Logs/TruckSports/billiards-regression-11.log and Billiards-Regression-11/results.txt: 58 passing existing local/physical-shot regression checks, exit 0, no captured runtime errors. Feature captures for series result, Practice and Settings inspected at 720p. Previous 1080p Billiards feature results remain retained; collection captures at 1080p are in Logs/TruckSports/Smoke-11-1080.

The opt-in wrapper loads Billiards from the collection, then temporarily sets collection navigation off for the existing standalone suite. That suite supplies the same injected devices/fixtures as before. Collection navigation itself is covered separately by repeated real scene loads in the Truck Sports smoke. The earlier statement about OS title refers to the old Billiards-only artifact; Truck Sports uses scoped product branding and preserves host settings.

See ../TruckSports/VALIDATION.md for physical versus logic fixture boundaries, measured hockey performance, final collection runs and remaining native/hardware checks.
