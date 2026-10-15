# Windows shared-GPU recheck - 2026-10-15

## Scope and safety
Validation started against the delivered `Builds/TruckTaxiDualWorld/TruckTaxi.exe`. A newly reproduced return-boundary defect required a narrow fix and rebuild. All runs use the existing restart harness and disposable Pixel Crushers vendor directories, never real career saves.

The user closed the original two TruckSports players. Another task subsequently built and launched CornholeValidation. The user explicitly approved continuing tests alongside that player: "just do your tests while that runs, im not too worried about it tbh". These observations are therefore SHARED-GPU, not an isolated baseline or a causal before/after comparison. No competing player was stopped by this task.

## Interrupted attempts (not passing runs)
| Disposable run | Outcome |
|---|---|
| a6b7f5682c0042eb8d97d5be6f6cb102 | Exit2, foreground focus missing before initial native driving check; no performance sample |
| 47d4749e6c8042e1ad099e590b0f792c | Exit2, foreground focus lost during initial driving; no performance sample |
| 42e4468f8bc9459bb7244c7f91a51b78 | Ten transfers and native driving after each passed; three Demo snow measurements completed; focus lost before off-road driving, Exit2. NOT an overall pass |

Raw logs are named `Windows-focus-interrupted-<run>.log`. `Windows-shared-GPU-first-monitor.json` records the concurrent headless Unity build and subsequent TruckSports player. A missing foreground window is an environmental test interruption, not evidence that vehicle locks remained active; the diagnostic shows dynamic body, unblocked gameplay and one input provider. It also does not prove the interrupted driving check passed.

## Reproduced defect and fix
Disposable run `cd90450eb21d4886890a5caf3f321839`, player PID38400, started 04:17:24 UTC, exited2. Ten transfers, all associated native drive checks, Demo accumulation/melt and curved wheel tracks passed. An eleventh arrival in Edenborough subsequently rolled downhill backwards through its outbound trigger. The trigger only checked outbound VELOCITY, not tractor HEADING. The weather failure was a downstream symptom of referencing the unloaded host. `Windows-rollback-defect-wrong-world.png` is labelled Eden light snow by the old probe but visibly shows "Arrived in Demo City"; it is NOT Eden performance evidence. Both Eden samples from this run are rejected, including the first frame showing3MPH rather than stationary. Do not reuse them as valid benchmark numbers.

Source commit `f5749b7d`: the existing boundary now requires outbound horizontal tractor heading AND the existing outbound motion threshold. No physics/input/navigation/save authority replaced. The development-only stationary probe engages the existing native parking-brake command, waits for rest and rejects world-owner changes, active transfers or movement during sampling. Strict driving/focus checks remain unchanged. Seven new direction cases;104 targeted world-travel EditMode cases PASS, raw `Arrival-guard-EditMode.json`.

Replacement Windows build succeeded04:29:05UTC, sourcef5749b7d;104 targeted tests passed. Actual hashes: Arrival-guard-Windows-build.json. Build counted one Pipeline command timeout log while succeeding; no compiler errors. No vendor source changes. Prior build was copied to Temp/TruckTaxiDualWorld/PreArrivalGuardBuild, but a subsequent Editor startup cleared Unity Temp; do not promise that temporary backup still exists.

## Final gameplay recheck and shutdown distinction
Run ab987faebd6842a0a2e78bd5ffc52e54/PID42552 completed all seed gameplay assertions:11 transfers (ten repeated career/GPS/native-input/one-owner checks), six valid parked snow measurements, accumulation/rain melt, actual curved wheel tracks on both maps, manual/autosave checkpoints. It then exited with native0xc0000005: NOT an overall clean-exit pass. Fresh EdenPID43432 restored its saved career/climate and drove4.652m, returned to Demo and saved, then also had a native shutdown error. Completion marker/progress survived; raw logs placed in project Temp were removed by another Editor startup. They have NOT been fabricated/reconstructed as raw logs.

After resetting the automation client and avoiding text/UIA capture, fresh DemoPID6228 passed controls/motion; its exact exit was not captured. Retained-launcher repeat PID18744 passed and exited EXACTLY0 at05:06:09UTC. Raw log/exit JSON retained outside Unity Temp and copied into Evidence. Earlier fast-launch focus failures are still failures. Immediate launch/activation solved startup timing without bypassing focus/input assertions or altering controls.

The local crash dump resolved through the installed Unity PDB to ExternalGPUProfiler::GetGameViewWindowHandle+0x9; the exception-thread stack[RSP] points into UIAutomationCore.DLL+0x224d00. This supports an automation/native accessibility teardown interaction, not definitive proof every map's shutdown is fixed. No speculative engine/vendor patch. Address lookup used the documented [Microsoft DbgHelp API](https://learn.microsoft.com/en-us/windows/win32/api/dbghelp/nf-dbghelp-symfromaddr); no dump was uploaded. Original dumps remain in AppData/Local/CrashDumps, not committed.

Current Demo curved path16.041m/4contacts/544stamps, GPU1818marked1667indented; Eden15.759m/4contacts/568stamps, GPU1373marked1315indented. Both detail screenshots visually inspected. Six valid timing cases and repeated-transfer counters are in Windows-recheck-summary.json. They are stationary connector/shared-GPU observations; no isolated before/after, locked60FPS, dense-city driving or exhaustive human acceptance claim. Current probe did not record Quality/VSync/frame-cap values.

Dense-city/vegetation driving, equivalent old-build before/after, and exhaustive human acceptance are not established by the stationary regional-exit benchmark.
