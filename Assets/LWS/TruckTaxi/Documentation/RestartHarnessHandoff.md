# Standalone Restart Harness Checkpoint

## Current Version 2 Evidence

Final UI-corrected build: `TaxiStabilization-WindowsBuild3.log`, zero errors,
593865669 bytes, runtime DLL SHA256
`A3F0FB6A6EAA6FC85CB897AA55D491E056AA2A07886A63C9F3E2CCF1DD7FBF65`.
Run `ad426293787e413d9e5106ceac63d0ce`: seed PID 24208, Continue PID 32876,
manual PID 31152, Continue PID 20104; all exit 0 with version-2 PASS. Logs:
`TaxiStabilization-FinalRestart-{seed,continue,manual,continue2}.log`.
Cleanup succeeded for both version-2 runs through the facade. The final run's
soak attempt failed safe during road-approach preflight, before timed driving;
it is not one-hour evidence. Final screenshot inspection covers 720p, 1080p,
1440p and ultrawide. Requested 3840x2160 was actually 3840x1440, not native 4K.

Windows build `TaxiStabilization-WindowsBuild2.log` succeeded with zero errors,
593865088 bytes. Runtime DLL SHA256:
`C8C80A69060FF577574C328090B24433A139B8DBBD447CA06B11882EDBB7B32E`.
Isolated run `08dddce8451046ed857a95918ebe755e` completed with exit 0 and an explicit
version-2 PASS in each separate process: seed PID 30364, Continue PID 28048,
manual slot PID 26400, second Continue PID 12008. Logs are
`Logs/TaxiStabilization-RestartV2-{seed,continue,manual,continue2}.log`.

This exercises the damage, non-spawn grounded pose, stationary velocity, Career
identity/appearance and claimed first-guide assertions described below. Actual
origin offset was zero; nonzero-origin stress coverage is not claimed. This is
scripted persistence acceptance, not a human driving or complete sandbox pass.
Screenshot review found pause UI behind Career; a subsequent focused rendered
test passed after the visibility-event repair. Final UI packaging and isolated
cleanup are tracked in the parent ledger.

The original checkpoint notes below are historical. Statements that version 2
has not compiled/run are superseded by the exact evidence above, not by inference.

## Preserved Prior Evidence

Parent-reported actual execution evidence (not independently rerun by this workstream):

- Unity: `6000.4.10f1`; Windows build: `0` errors; size: `593096209` bytes.
- Runtime.dll SHA256: `C22FB8A7E3082744FCC583D172A6764D011ECAE4A6D4872CB9E1F0014597C856`.
- Run: `08db3a120d9a4ef4937815935e09257d`.
- seed: PID `29700`, separate process, exit `0`, PASS; `TaxiStabilization-Restart-seed.log`.
- continue: PID `26320`, separate process, exit `0`, PASS; `TaxiStabilization-Restart-continue.log`.
- manual: PID `31300`, separate process, exit `0`, PASS; `TaxiStabilization-Restart-manual.log`.
- continue2: PID `21588`, separate process, exit `0`, PASS; `TaxiStabilization-Restart-continue2.log`.
- cleanup: exit `0`, PASS; `TaxiStabilization-Restart-cleanup.log` (PID not supplied).
- Normal user root untouched. Parent completed isolated-profile cleanup after the four passes.

This remains valid evidence for version 1's assertions. No prior logs/build were edited or removed by this extension. The new damage/non-spawn-pose assertions require a NEW GUID and new build/run evidence. Enhanced version-2 harness and Career semantics are NOT yet tested; the prior passes must not be represented as coverage for either.

## Entry And Isolation

`TruckTaxiRestartHarness.Requested`, `Configured`, and `Failure` exist in every build, including non-development players. A normal launch creates no harness, touches no vendor object, and resolves no storage path. An opt-in release-player launch fails with exit 2. Standalone development builds run; Editor opt-in is refused without modifying storage.

Both flags are required:

```text
-truck-taxi-restart-test seed|continue|manual|cleanup -truck-taxi-test-run <nonzero-32hex-GUID>
```

Each command takes ONE mode, not the pipe-separated list. Use the same GUID for successive processes. Other Truck Taxi smoke flags conflict and are rejected. Use a fresh GUID for each seed attempt; seed refuses an existing directory. Missing, duplicate, malformed and path-like values fail closed.

BeforeSceneLoad resolves installed Pixel Crushers reflection metadata once, gets SaveSystem.instance, and configures/reuses its DiskSavedGameDataStorer via public storeSaveFilesIn=Custom and customPath. The shared LWS adapter must reuse that component (current implementation does). No vendor assembly reference, custom storage backend, game-state file, or PlayerPrefs write was added.

The ONLY allowed root is Application.temporaryCachePath/TruckTaxiRestartTests/<normalized GUID>. Ancestor and existing-entry reparse points, nested directories, and unexpected storers fail closed. Only seed creates the verified directory. Other modes require an existing root and vendor saveinfo.dat. The selected storer/path is rechecked before harness operations. Do not place logs/subdirectories inside the vendor root; use Unity's -logFile elsewhere.

Parent's guard must reject Requested && !Configured before any facade registration/initialization. Configured becomes false on harness failure. No operation falls back to the normal save root. The harness does not override failed-load policy, grounded-pose safety, or save validation.

## Parent Execution Sequence

Run and wait for actual process exit in this order, using the same GUID and separate logs:

1. seed
2. continue
3. manual
4. continue (second fresh-process cycle)
5. cleanup (only after collecting evidence)

Require exit 0 AND `TRUCK TAXI RESTART PASS mode=<mode> run=<GUID>` in each process's log. Exceptions/assertion errors/player errors, invalid configuration, operation failures, or bounded waits exit 2. Watchdog is 600 seconds; menu readiness waits 90 seconds and gameplay readiness 180 seconds. PASS is explicitly per-process; only the parent can establish that real process death/relaunch occurred.

Seed invokes actual MainMenu.NewVibin, waits for an active resumable run, renames its profile ASTRA_SANDBOX_TEST through LwsSaveService.RenameProfile, and installs validated public semantic state. It writes slot 1, overwrites slot 1, writes slot 2, and saves an immediate autosave through Persistence.SaveBeforeLeaving. It returns through Bootstrap.ReturnToMainMenu, invokes actual OpenLoad/LoadSlot(0), validates, returns again, invokes ContinueVibin, validates, then leaves a final signature-3 autosave for the next process.

Continue discovers metadata in a new process and invokes actual ContinueVibin; expected signature 3. Manual invokes actual OpenLoad/LoadSlot(1), meaning user slot 2; expected signature 2. Slot 1 overwrite is signature 1; its obsolete initial write is signature 0.

Each signature checks profile ownership, vendor slot existence, cash, two completed rides/nine total stars, score, collision/traffic/pedestrian/jug/throw/special counters, inventory, hunger/thirst/fatigue/bladder/bowel, fuel liters and calendar. Signature N has earnings 123400+N cents, snack count 7+N, hunger .2+.1*N, fuel 40+N liters, and 2026-08-12 14:0N. A run-GUID ambient choice marker uses the existing semantic snapshot and is stored exclusively by the vendor.

Version 2 additionally captures existing ComponentDamage, selects the ordinal-first Body-category part, changes only that captured entry to condition `.61 - .07*N`, validates through ValidSnapshot, and applies public Restore. Every seed/load checks the same part, vehicle identity and condition (tolerance .0001).

Pose selection is independent of the saved truck pose: enumerate active authored RideLocations with a truckStopPoint in the authored spawn's region, reject missing/duplicate stable IDs, exclude stops within 50 horizontal meters of playerSpawn, sort ordinal by locationId, and select N modulo count. Do not change authored scene content between processes. Every selected stop must already be loaded and pass the existing complete-footprint TryGroundedPose/HasClearance checks. Unsafe selections FAIL; the harness never skips to another location or forces a stream load.

Relocation uses Bootstrap.TeleportNear after validating its intermediate lifted pose, then existing TruckTaxiRecoverySpace.Move to apply the prevalidated grounded position while paused, and zeroes linear/angular velocity. This avoids waiting for an uncontrolled physics fall. Expected grounding is independently reconstructed from the same authored ID/signature on each validation, with no sidecar or custom save format. Current real ILwsWorldOriginService converts expected/actual poses; assertions cover full local/global position (0.15m), full rotation (1 degree), global-to-local roundtrip, transform/rigidbody agreement, stationary velocities, distance from default spawn, registry player identity, one live LwsPlayerTruck, one Taxi bootstrap/world, and canonical paused registry clock including seconds. No validation code relocates a loaded truck to hide a failed restore.

New logs include evidenceVersion=2, POSE SEEDED, POSE VERIFIED, actual/expected coordinates, origin offset, authored ID/region and damage values. The harness does not force a nonzero floating-origin shift; evidence proves behavior with the real origin used by that run, not an unexecuted rebasing stress test.

The facade's empty scoped placeholder profile is allowed but must have no saves. Any other unexpected profile fails. Cleanup deletes ONLY the uniquely named ASTRA_SANDBOX_TEST profile through DeleteProfile and verifies its reserved vendor slots are absent. It never deletes directories, the normal root, or the facade's empty placeholder. The isolated directory remains available for inspection.

## Files And Tests

- Scripts/TruckTaxiRestartHarness.cs: standalone opt-in orchestration, public guard surface, cached reflection, vendor/menu checks and semantic fixture.
- Scripts/TruckTaxiRestartTestArguments.cs: pure argument/root parsing and filesystem boundary validation.
- Tests/EditMode/TruckTaxiRestartHarnessTests.cs: no-I/O parser cases, existing session/damage validator checks, deterministic pose selection and missing/duplicate target rejection. Pose tests create/destroy temporary GameObjects only; no vendor storage or authored scene loading.

Parent Unity import should generate new .meta files. No existing file was modified in this assignment.

## Limitations

Career startup integration: seed waits up to 30 seconds after run readiness for the actual first-day panel, then calls public `host.hud.Career.Close()` before any fixture mutation. It checks active-profile identity and acknowledgement, re-pauses, and waits for persistence readiness. Fixtures start from captured session state and preserve `ownedProfile`; a four-signature regression test covers this. Every validation (including fresh continue/manual) checks valid same-profile Career identity, retained claimed guide and closed panel, then calls `OpenFirstDay()` and asserts it remains closed. Missing startup integration fails rather than silently claiming the guide. These additions are NOT compiled/executed and do not extend the v1 evidence.

The prior version-1 passes above are preserved; no version-2 compile, tests, player launch, build, or real-restart pass is claimed yet. Seed uses existing validated semantic restore APIs, not a physically driven fare; this proves persistence only. It does not prove player driving controls, complete Career content, all inventory items, or last-session menu presentation. Current session statistics are directly asserted; additional stats-agent counters require parent extension after their API stabilizes. Main-menu callback/field renames, changed authored location lists, unsafe selected stops, missing scenes, different fuel capacity, or unrelated player errors intentionally fail rather than silently bypassing real integration. Temporary cache storage can be cleared by the OS; a missing test root fails instead of creating a replacement. Run processes serially; no multi-process writer locking is added to the production vendor.

## Vendor Report

- VENDOR ASSETS AUDITED: Pixel Crushers Common SaveSystem.cs and Storers/DiskSavedGameDataStorer.cs under Assets/Plugins/Pixel Crushers/Common/Scripts/Save System; LwsPixelCrushersSaveAdapter binding; existing opt-in TruckTaxiPlayerSmokeTest.
- VENDOR ASSETS USED: SaveSystem.instance/storer, DiskSavedGameDataStorer.storeSaveFilesIn/customPath, existing scoped LwsSaveService profile/save/load/delete facade and actual Truck Taxi menu callbacks.
- RELEVANT ASSETS NOT USED: No alternate save backend. Other gameplay vendors retain their existing ownership; NWH fuel is accessed through the existing Truck Taxi fuel adapter.
- CUSTOM SYSTEMS CREATED: Development-only test orchestration and pure argument validation, not a game save system.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
