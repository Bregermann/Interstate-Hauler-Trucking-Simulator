# Eleven-sport expansion validation — 2026-10-15

The combined normal-company Windows x64 Development build succeeded with0errors30vendor warnings and passed actual injected pointer/keyboard startup, selection, driving, pause/resume and return. All new events completed full AI matches; repeated collection transitions and720/1080 layouts passed. [Detailed current evidence and limits](../../Documentation/TruckSports/ExpansionValidation.md). Real controllers, rumble/audio listening and human acceptance remain manual. Cross-task dependency publication is pending approval, so clean remote-clone reproducibility is not claimed.

Earlier validation history follows.

# Seven-sport Cheese Rolling validation — 2026-10-15

Cheese Rolling implementation and native player validation are complete. [Detailed evidence](../TruckCheeseRolling/VALIDATION.md):300 native editor assertions,46 focused physical/software-input checks,27 layout/grass checks,94 full-game/lifecycle checks, all native exits0. The real-time16-truck match finishes4–2–5–4 after15 physical catches,218 explosions, zero neutral replays and531.9966s. A separate solo five-catch challenge completes. Pause/reconnect/replacement/rematch and two return cycles through all six older sports pass. Hardware and human acceptance are not claimed.

The aggregate71,980 live-frame sample averages10.0065ms onGTX1660Ti (p9518.7643 / p9925.0083 / max368.8503ms), including2,975 four-view hurricane/multiple-blast frames. The maximum hitch is unisolated; the sample includes captures/logging and multiple view counts. No user builds were closed. All1,536 settings/package hashes match. Final normal build/publication details follow in the checkpoint and Cheese build information.

Earlier Cornhole and five-sport evidence below is historical.

# Six-sport Cornhole delivery — 2026-10-15

Cornhole implementation/build and final validation are complete. See [Cornhole validation](../TruckCornhole/VALIDATION.md), [controls/scoring](../TruckCornhole/README.md) and [build information](../TruckCornhole/BUILD-INFO.md).262 native fixtures;216 complete physical/lifecycle checks;146 visible view/software-input checks;9 authority fixtures; two full four-course matches with160 launches,12 holes and zero timeouts. Normal-speed1080p match344.42s/54,183 frames: mean6.34ms,p9912.48ms, worst737.67ms (unisolated hitch, recheck manually). No physical-controller or human acceptance is claimed.

Final normal-company Windows x64 Development build24 succeeded with0 errors/13 inherited warnings; exact176 DLL and7 scene hashes match validation.1536 settings/package hashes remain unchanged. Existing sport gameplay/scenes are preserved. Sports feature d053b2a0 is pushed to codex/truck-cornhole. Both prior completed Sports work and this update are committed/pushed; final documentation publication is recorded in the checkpoint and generated build GIT-DELIVERY.txt.

Earlier five-sport evidence follows as history.

# Focused polish validation — 2026-10-14

This section is the current evidence for attachment a0079752-f75d-4987-85ef-10c63af421fc. Reports below it describe earlier updates. All native evidence and before/after PNGs are local under `Logs/TruckSports/Polish`; generated builds/logs are excluded from Git.

## Build and preservation

Final `final-build-14`: native Windows exit 0, BuildReport Succeeded, 0 errors, 4 nonfatal build warnings, 306,422,013 bytes, 33.75 seconds. Unity 6000.4.10f1, Mono, Windows x64 Development, PE machine 0x8664. Builder: `LWS.TruckSports.Editor.SportsBuilder.Build`, with no validation company/output overrides. Saved startup TruckSports, then Billiards, Hockey, Bowling, Air Hockey and Wallball. `CreateValidateBuild` was not used. Creation/legacy upgrade hooks retain the new presentation.

Exact host settings restoration passed both successful build and deliberate failure. All 1,536 baseline ProjectSettings/Packages file hashes and 20 protected gameplay-source hashes match. Native before/after audits show identical collider and Rigidbody facts in all five sports (including physical sizes, poses, masses, drag, constraints, CCD, material friction/restitution). Bowling changes delivery policy/NWH tuning, not geometry or score authority. Wallball rules, match, truck/jump, AI, ball and flight sources are unchanged. Unrelated Git status matches the initial baseline.

All 176 managed DLLs and level0–level5 in the final build match validation build 13 byte-for-byte. Only the validation build's company/preference metadata is scoped to `LWS Truck Sports Validation`; final retains the normal namespace. Automated probes mute native input and inject devices only within their own process. The user's copied testing executable was left running and untouched. The actual final executable was also launched without test flags, with a live window titled `Truck Sports`; automated gameplay evidence comes from the matching isolated-preference player, not a claim of human playtesting in that normal window.

Native saved-scene/reference validation passes. Pure logic checks: Hockey rules 20 and ownership 16; Billiards rules 50 and feature/series 72; Bowling scoring/rack/tenth-frame 32; Air rules 26; Wallball rules 30. These fixtures are distinct from physical full-game evidence.

## Bowling root cause and physical validation

Old EndDelivery called Clear on gutter, overturn, retreat and short timers: linear/angular velocities became zero, the body kinematic, and contacts disabled. Sideways tire scrub and opposite-throttle braking compounded the stop. No orientation-relative velocity clamp was present. Settling time was counted in both FixedUpdate and Update, effectively shortening its cap.

Baseline seeded physical poses at z=22 and +Z velocity 10, with software W held: normal reached z49.23 after three seconds; sideways scrubbed to z26.40 and -0.14 down-lane speed; backward froze kinematic at z38.67; gutter immediately froze at z22. Final motion-10 keeps all four dynamic through ordinary sliding and reaches the end pit: normal maxZ68.23, sideways68.23, backward68.48, gutter68.27. Maximum shared carry acceleration 0.55. Normal scored four physically contacted pins; the committed gutter scored zero and never returned to the lane. A late gutter after a genuine hit retains the existing legitimate pin-credit rule. Result locks occur once after pit/recovery and pin quiet checks; no arbitrary overturn/retreat stop remains.

Bowling-13-720 passes 80 retained physical/software-input/lifecycle assertions, native exit 0: actual NWH approach and physical pin contact, progressive oil, sticky gutter, genuine stopped recovery, survivor seating/sweep/result once, pause/disconnect/reconnect ownership and fresh confirmation, shared-source handoff/hot-seat frame progression, ties/rematch/scorecard and scene returns. The stopped fixture now synchronizes both poses and zeros wheel spin as well as chassis motion; residual spinning tires previously made its claimed 'stopped' body legitimately start rolling again. Pinsetter/score implementation was preserved.

## Matched full AI games

Baseline and final ai-13 use seeds900–902, four simultaneous lanes, Easy/Normal/Hard/Hard, all ordinary controls and pin physics. Every positive delivery is checked for chassis/tire contact provenance. No injected pinfall/scores or live-delivery teleports. These run at timeScale 3 for scoring/progression, not a performance benchmark. Profiles overlap and this is a small sample, not a guarantee of difficulty ordering or universal balance.

| Difficulty | Before scores | Final scores | Before mean | Final mean | Before / final spare conversion | Before / final gutters |
|---|---|---|---:|---:|---|---:|
| Easy | 83,69,80 | 77,86,68 | 77.3 | 77.0 | 2/30 → 3/30 | 2 → 6 |
| Normal | 71,64,71 | 83,91,77 | 68.7 | 83.7 | 0/30 → 3/30 | 4 → 0 |
| Hard | 77,80,77,80,78,78 | 73,89,88,87,83,100 | 78.3 | 86.7 | 0/60 → 8/60 | 0 → 0 |

Overall: 14 final spares versus2; six gutter-flagged deliveries in both samples; no strikes in either. Easy's six final gutter flags include five deliveries with earlier legitimate contact; only one is a no-contact gutter, compared with two before. Its target/reaction errors remain deliberately larger. Normal initially had overly wide spare approaches; the final reachable clamp, lower heading noise and 0.09s reaction removed that regression. Hard gains about10.6% in mean score; Normal gains about21.8%, with mistakes and widely imperfect games retained.

The separate timeScale 1 four-lane full match `bowling-four-ai-13` completes in 287.6s, scores 88/83/98/76, 80 deliveries/sweeps, 779 chassis/tire contact observations and319 actual pinfall. Lanes progress independently and finished lanes do not stop others. Final standings/full scorecard/rematch reset with all four identities pass. All positive deliveries retain physical provenance.

## Billiards and camera/UI

Visual-10 at1080p passes 53 checks, exit 0: straight roll angle follows distance/radius, stopped rotation stops, real cushion reversal, no physical angular double rotation, all 16 rack visuals reset, actual injected mouse maps/confirms cue placement, cue teleport does not roll across the table, real pocket crossing awards once and disables contact before the visible downward drop, drop/reset cleanup, group choice and pause.

The authoritative oracle loads a fresh local physics scene for each shot and invokes the existing ball/match FixedUpdate with .02s physics. Three reference shots are repeated with presentation disabled, then enabled: all nine runs match positions to four decimals, pocket flags, settle-step counts, turns and notices. Reusing a physics world/contact cache and Update-timed full-rack breaks was not a repeatable oracle; those early differing captures are retained, not presented as changed gameplay. The original physical methods/settings and AI sources remain unchanged.

Retained standalone suites also pass: Billiards54 (actual opening bumper contact, scratch/near-rail AI strikes, retained-turn contact and held driving through ordinary changes); Hockey118 (six slots, ownership/reconnect, physical goals/recovery/restart); Arena77 (Ice/Air, truck-as-mallet, rounded corners and modal/lifecycle cases); Wallball70 (actual serve/wall rebound, jump/volley, ghost/eligibility contacts, pause/reconnect and five-sport returns). Group-10-1080 adds seven actual mouse/software-controller group-choice checks. Each native exit 0, no captured managed errors/exceptions.

Final layout-13 passes471 assertions over all four affected sports at1280×720,1920×1080,1600×1000,1280×960 and2560×1080: full arena volumes, HUD plate bounds, untruncated long names/state, pause captures and clean menu returns. All 20 arena images and representative modals/high-ball poses were visually inspected. Ice/Air brief notices sit above the score, clear of board-adjacent puck paths. Wallball's preserved camera angle fits the complete legal height/depth envelope; its high-flight position fixture is explicitly injected, not autonomous-flight evidence. Colored HUD names replace labels over its truck roofs. No physical stretching, boundary cropping or Wallball handling/rule redesign. Bowling cab/split cameras are unchanged.

## Sustained performance and manual boundaries

GTX 1660 Ti, Direct3D11,1920×1080, vSync1. Rendering fixture:30s with sphere/shadow rendering off:4800 frames, mean6.25ms,p95 6.29,p99 6.33,GC0=0;30s visible:4797 frames,mean6.25,p95 6.29,p99 6.32,GC0=1. Off removes new rendering only; it is not an old-flat-label build benchmark. The equal observed means are refresh-limited and do not prove zero GPU cost.

Four-lane real-time match:45,879 sampled frames,mean 6.27 ms,p95 6.28,p99 6.31,worst81.29,managed delta3,678,208 bytes,one Gen0 collection. No owned Editor/build job overlapped this sustained run. Concurrent user applications/testing builds were left alone; this is observed development-player performance, not an isolated hardware benchmark.

Manual checks remaining: subjective slide/AI balance, real keyboard rollover, physical controllers/shared sticks/reconnect, vibration/audio, and human Wallball play. No human playtest or real-controller validation is claimed. Performance on other GPUs/displays remains unmeasured. Early failed fixtures/UI layouts/scoring experiments were fixed and retained in ignored evidence; final listed suites pass.

VENDOR ASSETS AUDITED: NWH WheelController/VehiclePhysics APIs and audio, GR3D, Heat/uGUI/TMP, DOTween, existing Sports cameras/input, installed Cinemachine GroupComposer.
VENDOR ASSETS USED: NWH tire/suspension/torque/grip and truck audio, GR3D geometry, Heat/TMP menus/font atlas, Unity smooth sphere/URP Lit, DOTween visual drop and retained pinsetter.
RELEVANT ASSETS NOT USED: Cinemachine tracked-group framing is unnecessary for the existing fixed arena-envelope camera authority; road VehicleController/UTS/world/weather/online systems do not supply these offline semantics. No billiard-specific installed ball artwork was found.
CUSTOM SYSTEMS CREATED: collider-free ball presentation/UV authoring, arena-edge UI docking, focused motion/AI/render/viewport probes; existing LWS bowling delivery/AI and camera adapters extended.
VENDOR SOURCE MODIFIED: NO by this update; unrelated existing vendor work preserved.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO new vendor subsystem; NWH remains the tire/suspension authority and existing camera/UI adapters remain the presentation authority.

---

# Five-sport Wallball validation — 2026-10-14

Current saved-scene build 07 and final physical/software-input probes passed. See ../TruckWallball/VALIDATION.md for native exits, 720p/1080p checks, complete autonomous match, preservation hashes, performance under concurrent desktop load and manual hardware/handling limits. All four older sport folders and host settings are preserved. The prior reports below are historical.

> Current five-scene/four-sport Windows validation: ../TruckAirHockey/VALIDATION.md. Final build 16, both-resolution physical fixtures, sustained full AI match/rematch evidence, preservation hashes and manual limits. Older reports below are explicitly historical.

> Historical three-scene handoff (2026-10-12). The executable has since been rebuilt with Bowling as the third sport. Current four-scene build, physical match/regression evidence and limitations: ../TruckBowling/VALIDATION.md.

# Truck Sports validation — 2026-10-12

## Build and settings

Executable: F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckSports\TruckSports.exe

Unity 6000.4.10f1, Windows x64 Mono Development build, Truck Sports product title. Final build entry is LWS.TruckSports.Editor.SportsBuilder.Build; log: Logs/TruckSports/Update-3v3/combined-build-16.log. Build succeeded: zero errors, 25 pre-existing vendor/package warnings, 293,290,390 reported bytes, 42.4568650 seconds. All accompanying Data/Mono/D3D12/DLL/development files remain with the executable. BUILD-INFO.md and build-manifest.csv in the build directory describe the final payload.

Startup is Assets/TruckSports/Scenes/TruckSports.unity. The build-specific array then includes Assets/TruckBilliards/Scenes/TruckBilliards.unity and Assets/TruckHockey/Scenes/TruckHockey.unity. CreateValidateBuild was inspected and avoided for final delivery because it reauthors scenes. The saved implementation was extended; authoring defaults/migrations now produce six hockey slots and current Billiards menus. The legacy Billiards-only builder remains; its separate standalone artifact was not rebuilt by this update.

Success and a deliberately thrown build exception both restore the exact pre-existing host settings. SHA256 ProjectSettings.asset: AE76D5EF37DFA2F61B8D61ED633BB475B955B5B99E513182F28BC131723C158F. EditorBuildSettings.asset: 9554C220A85A14A6769EBB339F6EA3E56A83F6C78B02DFAF23CBE08938E7BFA6. The baseline includes the user's prior dirty host changes. No host/Truck Taxi scene list, package manifest, input/physics setting or vendor source was intentionally changed.

## Saved-scene and rule checks

The actual saved scenes compile and validate their references, missing-script counts, listener/EventSystem counts, six hockey players/trucks/agents/labels/input-name slots, puck/goal barriers, collider model and authored audio/materials. All owned assets have Unity .meta files. Hockey team rule fixtures: 20. Hockey participant/source ownership fixtures: 16. Billiards rules: 50. Billiards feature fixtures: 72. These are reference/logic fixtures, separate from live physical execution. Scores are two teams, not a two-participant assumption.

## Final executable checks

Evidence is local under Logs/TruckSports/Update-3v3. Tests run sequentially to avoid preference races and performance interference. They temporarily inject Input System devices, clone input settings and restore saved setup/settings. Each result is checked for managed errors and the actual Windows process exit code.

- Feature-10: 92 retained Billiards feature checks; Local-10: 58 retained local-play checks. Both actual Windows exits 0, no captured runtime errors.
- Billiards-16: 54 checks, actual Windows exit 0, no captured runtime errors. Actual physical human opening/contact and held-drive transitions; scratch placement then AI bumper contact; near-rail cue/eligible-eight contact; a physical own-ball pot then retained-turn second strike; boosted full-rack opening containment; off-turn roaming; group choice, wrong-pad rejection, reconnect ownership, actual mouse UI selection and held-mouse rejection. Rule outcomes are injected only for modal/series fixtures; physical AI cases identify their initial rail/group/scratch layout.
- Hockey-16-720 and Hockey-16-1080: 118 checks each, actual Windows exits 0, no captured runtime errors. Six keyboard sources, three shared pads, six whole pads, mixed sources, duplicate rejection, brake/reverse, individual recovery/contact cleanup, accessible stationary puck and temporary defensive crowding, physical impact feedback, out-of-bounds faceoff, actual own/overtime goals, restart/rematch and three repeated sport transitions. Shared reconnect requires neutral then a fresh South press through the menu; both stick owners and fresh driving are checked. Short fixture frame samples: 720p 3,669 live frames, mean 6.29 ms/p95 6.29/p99 6.34/worst 24.97, managed delta 5,697,536 bytes and 13 gen-0 collections; 1080p 3,620 frames, mean 6.37/p95 6.29/p99 12.48/worst 62.48, managed delta 5,636,096 and 13 gen-0 collections. These fixture samples include repeated scene loading and are separate from the sustained run below.
- Autonomous-10: 12 real bumper strikes in 199.9 seconds, completed AI-only rack without initial/shot-to-shot relocation, actual Windows exit 0, no captured runtime errors. The final outcome is an early/illegal-eight loss (winner 1), not legal full-group clearance. Legitimate scratch placement/rack resets remain normal gameplay. No human confirmation was needed.
- Six-AI-11: 17 checks, three full two-minute regulation matches and two rematches, 18 physical goals, 453.0 seconds elapsed; all six bots made physical puck contact/decisions across the run. Zero emergency faceoffs, query overflows or captured exceptions; actual Windows exit 0. The editor was closed for the entire sustained run, native automation detached. GTX 1660 Ti, Direct3D11, i9-9900K, 1280x720, vSync 1, targetFrameRate -1: 57,450 live frames, mean 6.28 ms, p95 6.29, p99 6.33, worst 12.55; managed-memory delta 5,861,376 bytes and one gen-0 collection. Probe overhead and other desktop applications remain; this is sustained desktop development evidence, not a release benchmark or leak proof.

Version boundaries: feature/local and autonomous checks used build 10. Sustained six-AI used build 11 after identity-label contrast/size tuning. Subsequent builds change only modal mouse/controller handling, reconnect focus and related probes; AI, collision, rules and sustained gameplay physics remain the tested implementations. Final targeted reruns use build 16. Screenshots are Unity ScreenCapture output at 1280x720/1920x1080; no native input/vision playtest is claimed. Ordinary crowding is tested; a deliberately enclosed genuine multi-truck trap/embedding recovery was not separately forced.

The stronger Hockey-14/15 shared reconnect tests exposed a carried-over stick/South interaction opening Sport Selection confirmation, even though shared ownership remained intact. Every reconnect-menu action now waits for a later neutral observation and checks a fresh raw device edge; neutral restores Resume focus. The final probe separately checks actual fresh UI resume and the held-driving gameplay gate. Hockey-14/15 are failed diagnostic runs and are not counted as clean. Historical exception-bearing and native-crashing runs are likewise superseded. UPDATE_3V3.md records the original Billiards defect reproductions and repairs.
## Native shutdown limitation

During native automation, several otherwise completed runs returned Windows 0xC0000005 on exit. Windows event/dump evidence and the installed Unity PDB identify ExternalGPUProfiler::GetGameViewWindowHandle +9, reading address 0x138 from PlatformAccessibilityManager static destruction during RuntimeCleanup. Releasing the computer-use client restored actual exit 0 in a focused reconnect run and the retained feature/local suites. No production force-exit or accessibility-disable workaround was added.

This matches Unity's documented Windows shutdown issue after UI Automation/screen-reader interaction, UUM-146676, fixed in newer engine releases: https://unity.com/releases/editor/whats-new/6000.6.0f1. The engine limitation remains in this project's installed 6000.4.10f1; a controlled editor upgrade/rebuild is needed to resolve that scenario. Native capture timed out, and no native gameplay input was sent. Further tests keep that client detached. A normal clean exit does not certify quitting with a screen reader or automation client attached.

## Human playtest boundaries

All controller/source assertions use injected devices; there were zero physical gamepads before injection. They do not certify physical six-pad enumeration, Bluetooth/USB/XInput/HID compatibility, rumble or keyboard rollover. Human six-player matches, audio listening/mute quality, real unplug/reassign, shared-stick ergonomics, alternative aspect ratios/display settings and long-term AI balance remain manual. Sustained development frame samples include probe overhead and goal pauses; they are not a release benchmark or leak proof.

## Git and vendor scope

Previous completed sports work: 3a8801772d305e05cd64da8ba046e18d8ff59f69, already pushed to origin/codex/truck-sports-3v3. The current update is delivered as a separate commit after validation; the final handoff gives its exact hash and push outcome. Unrelated dirty work is preserved. No generated build, cache, log or credential is staged. Native Unity scene serialization has its usual empty-value whitespace; source/document diff checks are separate.

VENDOR ASSETS AUDITED: NWH controller/ArcadeModule/GR3D SemiTruck, UTS CarAIController, Heat controls/audio, DOTween, installed Input System and existing sports adapters.
VENDOR ASSETS USED: NWH/GR3D render geometry, Heat UI/click audio, DOTween, Input System/uGUI/TMP/URP.
RELEVANT ASSETS NOT USED: Road-wheel powertrain/traffic-path systems do not provide constrained sports physics or team rules; world/wheel authoring systems do not provide these mechanics.
CUSTOM SYSTEMS CREATED: Extended six-player team coordination, source ownership, key setup, recovery/contact feedback and validation; extended existing Billiards navigation, opening break and group choice.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
