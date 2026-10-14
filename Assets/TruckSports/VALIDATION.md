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
