# Truck Sports validation — 2026-10-12

## Delivered artifact

F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckSports\TruckSports.exe

Unity 6000.4.10f1, Mono Windows x64 development player (PE machine 0x8664), product/window title Truck Sports. Final build: Logs/TruckSports/build-11.log; Succeeded with zero errors, 25 existing vendor warnings, 292,995,266 reported bytes and 39.0439 seconds. All accompanying player files are included; keep the entire build folder together. Hash inventory: build-manifest.csv. README.md has controls and build commands.

Explicit scene order: Assets/TruckSports/Scenes/TruckSports.unity, Assets/TruckBilliards/Scenes/TruckBilliards.unity, Assets/TruckHockey/Scenes/TruckHockey.unity. The builder supplies this array directly, without editing host/Truck Taxi scene lists or profiles. Existing Billiards-only builder is retained; its separate artifact was not rebuilt in this pass.

Exact baseline settings restored after successful build and a deliberately thrown build exception (TS_HOST_SETTINGS_RESTORED, TS_FAILURE_RESTORATION_PASS). SHA256 PlayerSettings: AE76D5EF37DFA2F61B8D61ED633BB475B955B5B99E513182F28BC131723C158F. EditorBuildSettings: 9554C220A85A14A6769EBB339F6EA3E56A83F6C78B02DFAF23CBE08938E7BFA6. Baseline is the session's existing worktree, preserving prior changes.

## Saved scene and logic validation

Unity compiled and validated the actual three saved scenes, serialized references, native physics materials, missing-script counts, seven Hockey audio references, one truck body collider and one active EventSystem/AudioListener per scene. All owned assets have metadata. No manual assembly is required. Hockey rules: 20 assertions; retained Billiards rules: 44; Billiards features: 72. These logic/reference fixtures are separate from physical integration evidence.

Before/at/after regulation-boundary fixtures passed. Goal policy uses the completed simulation segment and full-crossing fraction: crossing at the deadline counts; later crossing is discarded before the regulation outcome. Collision callback order does not decide the result.

## Final executable integration evidence

161 checks passed at 1280x720 and 161 at 1920x1080, both TS_SMOKE_FINISHED code=0, no captured runtime errors. Evidence: Logs/TruckSports/player-11-720.log, player-11-1080.log and Smoke-11-720/Smoke-11-1080/results.txt, fourteen captures each, performance.txt.

| Area | Verified behavior and test boundary |
|---|---|
| Front end | Title, selection cards, own Hockey setup, Billiards Match/Practice entry. Keyboard submit exercises actual UI input. Newly enabled Start is navigated by injected D-pad and submitted with South through the UI module. |
| Physical scoring | Real moving puck through right mouth; real truck bumper drives an own goal through left mouth. Correct attacker, exactly one award, immediate freeze, no clock use, separated reset/cleared velocities/countdown. Outside-mouth/side-board contact does not score. No score events injected. |
| Contact | Real board rebound reverses velocity, rounded-corner containment, flat/capped puck; truck contact blocks overlap and keeps both upright. PhysX contacts provide the impact with no extra full-strength impulse. |
| Recovery | Own-half safe truck recovery leaves puck/score/time intact and rejects repeated requests during cooldown. Accessible stationary puck remains live for 10.3 seconds. Actual two-truck physical pin is released via truck recovery just before a shortened fixture timeout, without a false faceoff. A separate real pin produces announced emergency recovery; out-of-bounds recovery is also announced. Both countdowns preserve score/time. Pin timeout is shortened only in the probe; shipping default remains ten seconds. Embedded-geometry detection is implemented, but a separate deliberate penetration fixture was not run. |
| Clock/outcomes | Non-live phases preserve clock. Shortened regulation enters tied overtime; actual first physical overtime goal finishes. Physical regulation goal plus expiry gives leader win. These are reproducible shortened fixtures, not unassisted 2/3/5-minute matches. |
| Rematch/restart | Setup, names/colors/duration retained; end swap updates spawns, goal ownership, team crease/HUD and AI direction; physical swapped-net goal credits correct player. Restart cancellation preserves state; confirmation clears score/time. Missing-pad rematch waits for explicit replacement/Start, then swaps once. |
| Device routing | Simultaneous shared keyboard; two assigned pads; mixed keyboard/pad; solo pad ignores keyboard drive. Solo-pad start passes with every keyboard device disabled. Duplicate pad rejected and AI disabled in human modes. |
| Disconnect | Injected removal/re-add/replacement during live, countdown and pause preserves puck/time/prior phase; Resume requires neutral then fresh input. Clock mesh immediately visible on reconnect. |
| AI | Easy/Normal/Hard physically drive into the puck through the common controller. Hard chooses defense and physically blocks a threat. No privileged speed, direct puck force, teleport or injected goal. Sustained corner behavior/difficulty balance needs human playtesting. |
| Feedback | Master/SFX reject playback when muted. Assigned software haptic routing, expiry, Off/pause stop, and surviving overtime winner request after Result pass. Visual brief DOTween goal/win celebration inspected. Actual motor output/audio listening not tested. |
| Lifecycle | Three Hockey > selection > Billiards Match/Practice > selection > Hockey cycles retain one active EventSystem/AudioListener/scene authority and no stale live match. Billiards series, Practice, difficulty and feedback remain wired. |
| Visuals | Saved-scene/runtime title, selection, setup, pause, goal, overtime, result, rink, controllers/rejoin, emergency recovery and Billiards captures at 1280x720 and 1920x1080. Full rink/goals visible, HUD/messages fit. Other aspect ratios/display changes remain manual. |

Inputs are injected Input System state events; most menu tests invoke the same wired Heat button event. Title submit and replacement-device D-pad/South tests exercise input navigation. Probes are opt-in development flags, temporarily clone background-input settings and restore preferences/devices. Normal launch runs no probe. Zero physical gamepads observed before injection. Physical integration fixtures reposition/release actual puck/trucks, then require real contacts and full goal crossings.

Final executable retained Billiards suites: 92 feature checks and 58 local/physical-shot regression checks, both TB_LOCAL_FINISHED code=0 and no captured runtime errors. Evidence: Logs/TruckSports/billiards-features-11.log, billiards-regression-11.log; Billiards-Features-11 and Billiards-Regression-11/results.txt/captures. Series rule fixtures supply authoritative facts; Practice pots/scratches, AI opening bumper strikes and full local/single-player physical-shot regressions require real motion. The wrapper loads the actual scene and switches collection navigation off only to let the existing standalone suite follow its original Main flow. Collection routing is independently covered by repeated scene switching. These are scripted/injected tests, not unassisted full games.

## Measured performance

Four-second final development-player sample after Hard AI defense, opt-in harness present, i9-9900K / GTX 1660 Ti. Phase changes may occur during the sample; this is not an isolated continuous-live CPU/GPU benchmark.

| Resolution | Frames | Mean interval | P95 | Mean recorded GC bytes/frame |
|---|---:|---:|---:|---:|
| 1280x720 | 640 | 6.251 ms | 6.287 ms | 78 |
| 1920x1080 | 640 | 6.251 ms | 6.279 ms | 78 |

Observed frame intervals are not a hardware budget guarantee. Draw-call recorder returned zero and is unusable, so no draw-call claim is made. HUD is bounded to 10 Hz; overlap checks use a fixed nonallocating buffer/cached collider; six fixed voices and pooled confetti bound feedback. Remaining avoidable cost: Join/Disconnect refresh currently rebuilds label strings, enumerations and component arrays every frame; menu allocation magnitude was not profiled separately. Sustained memory/performance soak was not run.

## Repairs and exact limitations

- Owned UI action asset fixes unresolved second-scene default references.
- Nested smoke coroutines explicitly advance; previously skipped checks were rerun.
- Native .asset physics-material serialization fixes silently defaulted restitution; saved material assertions and physical rebounds pass.
- Result opens before winner feedback starts; finished confetti pieces hide.
- HUD meshes refresh when Match reopens; emergency countdown explicitly displays PUCK RECOVERY.
- Solo assigned-pad readiness no longer requires an enabled keyboard; pending rematch survives device replacement.
- Heat public navigation fields and uGUI links share the same policy, preventing deferred vendor initialization from overwriting new Start links. No vendor source edits.
- Truck body teleports clear that truck's contact journal, avoiding stale-pin recovery; accessible stationary puck is never sufficient to trigger recovery.
- A build-05 restoration guard stopped that attempt; on-disk baseline remained intact. Diagnostic details were added and explicit -buildTarget Win64 used; subsequent failure/success restoration checks passed. No unsupported root-cause claim is made.
- Initial build-10 smoke was blocked by sandbox PlayerPrefs access; normal Windows-access rerun reached a pin-test floating-point checkpoint failure. The checkpoint now tolerates float accumulation below 0.48; game recovery thresholds/assertions were not disabled. Final reruns pass.

Normal final executable launched as PID 7976 with only a log-file argument, no test flags; Windows Computer Use returned the exact process path and title Truck Sports. It is left open for human playtesting. Earlier native observation on build-04 failed with "Computer Use app approval timed out"; fresh window selection and one retry failed with "FrameArrived timed out: timed out waiting on channel". No native gameplay inputs were sent and native menu/driving PASS is not claimed. That earlier process was stopped for rebuilds. Final boundary/evidence: Logs/TruckSports/native-smoke-11.txt and native-player-11.log.

Generated payload: 368 files, 293,224,436 bytes including development symbols. Build-manifest.csv and Logs/TruckSports/build-manifest-11.csv contain exact file hashes. Executable SHA256: 8B85CC34BECB9A5E63DC14325C0266CF01CDAF798F5F35638FC01C0BE0CE7878; game assembly: 886DBFBF0459EA030F5067973D84E7D90BA36206AA193EAA22E7F9D5FC319DE0; globalgamemanagers: 7373BCE992F88DB31C9F1A5E67EA19072F7AF3CB64CCB98E9BF69937CA819963.

Remaining human checks: native keyboard/mouse and shared keyboard full matches; physical solo/two/mixed controllers, unplug/reassign and actual rumble; audio listening/mutes; display settings/other aspect ratios; unassisted 2/3/5-minute matches and long-play AI balance. No unresolved gameplay defect is known from successful current software runs. Vendor audit/use/custom boundaries are in README.md.