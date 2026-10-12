# Player Correction Acceptance

Scope: the 2026-10-09 attachment ending in `6d6b44f3-de43-4c0a-8576-4b9f335d5140`.
Preservation baseline: `05871832` live save/load driving repair. No voice generation or vendor changes.

## Current Evidence

Latest integrated candidate: **Windows Build 5**, `Logs/TaxiCorrection-WindowsBuild5.log`, exit 0. Executable `Builds/TruckTaxiDemo/TruckTaxi.exe`. No production changes since that build.
Latest integrated EditMode: `TaxiCorrection-IntegratedResume.xml`, **1,404 passed / 1 failed / 2 skipped** (1,407 total). Only failure is the preserved pre-existing `RegisteredFallbackPrefabsHaveThemedVisuals` analytical-robot art assertion. Current-change tests, including scenery and the corrected restart seed, pass. This is not a fully green suite.
Final executable isolated fixture `3f923b7843b94ea3910d096a96cc1758`: seed, fresh-process Continue and fresh-process manual-slot load all exit 0 (`TaxiCorrection-WindowsSeed5.log`, `TaxiCorrection-WindowsContinue5.log`, `TaxiCorrection-WindowsManual5.log`). Seed exercises real UI submits (+$30,000), confirms eight authored compatible slots, and validates multiple component-damage/weather/snow/career states. Human input/physical cabin-fit remains unverified.
Final untouched drive retry `TaxiCorrection-WindowsDriveContinue5.log` failed its strict foreground-focus gate before the W drive check. Body awake, wheels loaded, no modal/recovery/travel/pause/input locks; this is not a drive PASS. Do not repeat without restoring supported focused window access. Runtime DLL SHA256: `679795D62CF3896B438452A3D0C423F004FBE58FF25E75FCEF71676F9C1F896D`.

- Windows Build 2: `Logs/TaxiCorrection-WindowsBuild2.log`, exit 0. Executable: `Builds/TruckTaxiDemo/TruckTaxi.exe`.
- Isolated Windows restart run `0b9efb8267a343d7a7b975992c225437`: seed, fresh-process Continue, and fresh-process manual-slot load all exit 0. Logs: `TaxiCorrection-WindowsSeed2.log`, `TaxiCorrection-WindowsContinue2.log`, `TaxiCorrection-WindowsManual2.log`. These are automated executable tests, not human-style driving proof.
- Those runs check authoritative component condition and its roughly 28% summary, canonical weather fields, forecast/frozen policy, snow cells, needs/hidden bowel consequences, cash, inventory, fuel, clock, appearance and non-default global pose. The earlier failed seed exposed incomplete shared weather application; it is retained in `TaxiCorrection-WindowsSeed.log` as failure evidence.
- Shared save contracts plus restart harness tests: `TaxiCorrection-SaveContractsFinal.xml`, 91/91 passed. Focused canonical weather restore tests: `TaxiCorrection-WeatherRestartFinal.xml`, 3/3 passed.
- Automated Windows screenshots at 1920x1080 were visually inspected: pause categories, video controls, component condition and Sandbox debug controls are readable. This does not prove mouse/controller interaction or display-setting persistence.
- The occupancy screenshot revealed only one authored seat despite the 8-person maximum label. Runtime integration repair is in progress; this is NOT accepted as eight-person success.

- Compilation: `Logs/TaxiCorrection-Compile3.log`, Unity exit 0. Later needs/dressing/tunnel authoring also compiled successfully.
- First integrated EditMode: `Logs/TaxiCorrection-EditMode1.xml`: 1,302 passed, 15 failed, 2 skipped (1,319 total). Not a passing run.
- Second integrated EditMode: `Logs/TaxiCorrection-EditMode2.xml`: 1,332 passed, 5 failed, 2 skipped. Remaining beam lifecycle/float and legacy-save cases were corrected afterward; pre-existing robot art remained untouched.
- Focused runtime suite: `Logs/TaxiCorrection-WorldLifePlayMode.xml`: 4 passed, 1 failed. Actual filler disable/destroy cleanup, structural/breakaway behavior, world-life bootstrap and generated-child cleanup passed. The second scene-loading on-foot test encountered a Weather Maker manager lifecycle error; investigation/rerun required.
- Focused EditMode iteration 3: 68 passed, 9 failed. Newly added input tests moved to PlayMode because EditMode update frames did not deliver button presses; profiler aggregation and safe scene-fixture setup corrected for rerun.
- Legacy optional-save inline-null encoding, native beam initialization, and three obsolete/newly-invalid fixtures corrected for rerun. Filler automatic-disable coverage moved to an actual PlayMode lifecycle test.
- Existing analytical-robot art coverage failure intersects pre-existing user prefab/profile edits; those edits are preserved, not reverted to make a test pass.
- Needs image pools and baseline thirst migration executed: `TaxiCorrection-NeedsAuthoring.log`, exit 0.
- Empty Forest/Suburb/Highway pools populated with imported static NWH trees: `TaxiCorrection-DressingAuthoring.log`, exit 0. No scenery generated in scenes.
- Thunder Bowl two-way tunnel authoring executed: `TaxiCorrection-TunnelAuthoring2.log`, exit 0. First attempt failed before save on a Unity fake-null MeshFilter access; fixed explicitly.
- No new Windows human-style acceptance proof yet. Implementation, automated tests, and observed player behavior are separate claims.
- Supported Windows computer-use initialization failed twice with `windows sandbox failed: helper_unknown_error: apply deny-read ACLs`, including after a kernel reset. This blocks automated human-style window interaction, not Unity batch compilation/builds. No Windows PASS is inferred from harness execution.
- Integrated final run: `Logs/TaxiCorrection-IntegratedFinal.xml`: 1,378 passed, 3 failed, 2 skipped (1,383 total). Two failures were fixture RenderTexture teardown ordering, corrected afterward. The remaining analytical-robot authored-art failure predates this pass.
- Video ownership rerun: `Logs/TaxiCorrection-VideoFinal.xml`: 6/6 passed. No shared URP asset edits; render-texture GPS cameras excluded; inactive cameras inherit preferences; temporary effects do not override post-processing OFF.
- Final focused PlayMode: `Logs/TaxiCorrection-PlayModeMovement.xml`: 14/14 passed. Actual CharacterController jump/ceiling/landing, on-foot utilities, fuel highlight lifecycle, synthetic traffic knockdown/recovery, controller Start routing, world-life bootstrap and breakaway/structural guards. This is automated Unity runtime evidence, not Windows human driving.

## Verified Root Causes

- The shared weather participant captured condition/cloud/fog/lightning/storm but restored through a legacy setter that omitted those fields and did not invoke presentation. It now resolves the existing preset/vendor mapping, requests the complete saved semantics through the existing weather authority, and preserves saved time. Windows restart assertions now pass.

- Video-owned URP clones could outlive the gameplay scene and retain Weather Maker renderer features after their manager was destroyed. Owned quality-slot bindings are now restored before clone disposal; the repeated-scene runtime test passes.
- Long-frame jump integration previously applied gravity before displacement. At 0.333 seconds the 5.5 m/s launch became negative before moving. Bounded CharacterController substeps now preserve the jump arc without infinite air jumps or ceiling tunneling.
- Repeated snow cache discovery appended identical ground renderers. Actual EditMode production fixture over 20 passes retains 3 entries rather than the legacy-control 60; candidate visits 60 versus 630. Windows frame-time improvement remains unmeasured.
- Services could steal restroom/Road Pep interaction and open over another modal. Eligibility now requires driving mode; global utility shortcuts respect existing modal gates.
- Controller Start was consumed by world-life activities. It now reaches existing pause handling without cancelling the activity; panel Start closes the owned panel once.

## Resume Checkpoint (2026-10-11)

Latest batch: available imported scenery pools and executable fixture correction.
Changed: `TruckTaxiWorldGeometryAuthoring.Dressing.cs`, five additional profile assets, six project-owned fence/lumber prefabs, `TruckTaxiDressingTests.cs`; `TruckTaxiRestartHarness.cs` and its tests reset only disposable fixture last-session totals when replacing lifetime totals.
Validation: Unity authoring `TaxiCorrection-PropAuthoring2.log` exit 0. First attempt caught a test-only missing direct vendor assembly reference; corrected by using Unity serialized inspection, without dependency changes. Integrated rerun is next, followed by final Windows rebuild.
Build 4 actual executable confirmed eight compatible authored slots and three real UI button submits adding exactly 3,000,000 cents. Screenshot `ValidationScreenshots/02f3a2f30bc348fb9bb128535c176db7/Automated-Debug-OCCUPANCY-1920x1080.png` visually inspected: eight positions visible/readable. No eight-character cabin-fit claim. Seed subsequently failed on inconsistent test fixture last-session earnings; production validation was not relaxed.
Build 4 same-save driving retry: body awake, grounded wheels carrying approximately 16-28 kN, nonzero drive torque. Run stopped when window focus became false; **not passed**. `TaxiCorrection-WindowsDriveContinue4.log` retains the exact failure. Requested user assistance keeping the test window focused; continue independent validation meanwhile.

Current batch: validate the already-written load-finalization wake and configured-truck seat corrections. No implementation restart. One main worker; previous helpers are idle.
Changed files awaiting validation: `LwsSaveLoadCoordinator.cs`, `LwsSaveLoadPhysicsPlayModeTests.cs`, Taxi cab rig/layout/bootstrap, restart harness and occupancy/harness tests.
Last Windows Build 3 compiled, but driving failed three times: real clutch engagement/torque with sleeping tractor and zero wheel load. `FinalizeRestoredPhysics` now wakes only bodies placed by that load; untested at interruption.
Seat runtime check rejected the preserved companion-only slot because the fixture incorrectly required every slot to accept all passenger roles. The corrected explicit contract is driver + six shared travel seats + one companion-only seat; allocation now preserves the exclusive seat regardless of boarding order.
Batch validation: `TaxiCorrection-LoadWakePhysics.xml` 2/2 actual PhysicsScene/Rigidbody PlayMode tests passed; `TaxiCorrection-ResumeFocused.xml` 121/121 occupancy, restart and input-restoration EditMode tests passed. `TaxiCorrection-WindowsBuild4.log` build succeeded. Build 4 includes both corrections; isolated Windows driving validation is now the next step, using the unchanged previously failing fixture `c414edaffd77498f97368d67075ed71d`. Preserve failed logs and real saves.
The supported Windows computer-use kernel was retried after resume and failed before input with `orchestrator_helper_report_read_failed` / missing `setup_error.json`. Human-style UI execution remains blocked; no alternate desktop-control mechanism or security change was attempted.

## Requirement Checklist

Statuses: **verified complete** requires the requested player-facing evidence, **implemented but unverified** means code/content exists but acceptance remains, **incomplete** means work/evidence is still missing, **blocked** identifies unavailable access. No Windows human-style completion is inferred from injected input or fixture calls.

| Requirement (original scope retained) | Status | Evidence / remaining work |
| --- | --- | --- |
| Pause utility categories; on-foot cursor/mouselook; mouse/keyboard/controller navigation; contextual eject/cancel | implemented but unverified | Readable automated screenshot; input lifecycle PlayMode passes; normal Windows interaction pending |
| Deliberate E service prompts, stable range/cooldown, no auto-open, ordinary motel sleep independent of companion | implemented but unverified | Existing services retained; Windows motel/other service checks pending |
| Hidden Poopy Pants food/drink load, escalation/audio/vignette, grace/failure, toilet, persistence, debug presets | implemented but unverified | Focused tests and restart data checks; audiovisual Windows observation pending |
| Development GIVE $10,000 and exactly $30,000 after three presses | implemented but unverified | Final executable real button callbacks pass all three amounts; human navigation/click sequence blocked |
| Save/load/exit during ambient events; clear short atomic-block reasons | implemented but unverified | Ambient blanket lock removed; actual earthquake UI scenario pending |
| Weather, intensity, time, precipitation, forecast, snow and intended extreme-event progress persistence | implemented but unverified | Final executable seed/fresh-process Continue semantics pass; human weather visuals/extreme-event progression pending |
| Authoritative component damage/summary persistence, no damage steering penalty, condition/repair screen | implemented but unverified | Build 2 approximately 28% retained; screen observed; final drive/repair acceptance pending |
| Post-load automatic normal control and full career/progression, cash, inventory, needs, fuel, appearance/upgrades save integrity | blocked | Wake/loaded wheel support now confirmed; driving attempt stops on window focus loss. Final-build semantic restart checks pass. Focused drive-away and purchased-upgrade acceptance still required |
| Companion intent stack/destination/US distance, route and marker cleanup; eight real compatible slots; empty-truck rival theft | implemented but unverified | Final executable confirms eight compatible slots; mixed-slot/boarding-order tests pass. Windows theft/cabin fit and companion visuals pending |
| Existing on-foot jump and traffic ragdoll/recovery | implemented but unverified | 14 focused runtime cases passed across movement/UI/fuel; real Windows traffic impact pending |
| Massive actual-world destination beams; minimap pulse/edge arrows; hazard areas stay world-anchored | implemented but unverified | Focused marker/lease tests; long-range day/night Windows visibility pending |
| Fuel nozzle, real filler highlight, gallons/cost/progress, no parking-brake requirement, drive-away cleanup | implemented but unverified | Runtime highlight lifecycle passed; complete Windows interaction pending |
| Pooled stacked screen-space food/drink dreams, slower thirst; bounded dramatic filled-container blast | implemented but unverified | Authored pools and tests; visual/physical Windows crowd checks pending |
| Tow-only reasonable capped cost; player-facing real upgrades and effective NWH values | implemented but unverified | Quote and upgrade UI integrated; purchase/effect/restart scenario pending |
| Sharper low-speed NWH steering with stable highway behavior, before/after turning-circle measurement | incomplete | Tuning integrated; actual circle measurement and highway handling unrun |
| Bounded UTS reverse/reacquire/recovery and road-rage behavior | implemented but unverified | Existing UTS adapter extended/tested; live traffic recovery pending |
| Race input/countdown/GO/checkpoints/position/wrong-way/results, 20 rivals; two-way Thunder Bowl tunnel | implemented but unverified | Authored/tested foundation; final Windows race and both-direction drive pending |
| No infield/same-complex microfares; approximately 25% intercity mix without frequency change; optional diversions do not block final fare | implemented but unverified | Deterministic dispatch coverage; live offers/fare completion pending |
| Main-menu and pause video Apply/Confirm/Revert/timeout/persistence and sane fresh-install size | implemented but unverified | Six ownership tests and readable Windows screenshot; real setting/relaunch sequence pending |
| Adult clothed chest customization with preview/on-foot/seated appearance and persistence | implemented but unverified | Data/restart checks; rendered avatar variants pending |
| Real imported deterministic road-safe dressing; preserve structural versus breakaway collision | implemented but unverified | All eight pools now populated with available imported trees/fences/lumber; soft clutter reuses breakaway adapter. No scene generation. Missing bins/benches/work-zone signs; road-safe rendered placement still pending |
| Bounded hitch capture and measured recurring-hitch correction | incomplete | Duplicate snow-cache work proven 630 to 60 candidate visits; representative Windows frame-time attribution unrun |
| Consolidated development diagnostics, no voice generation, vendor/save/input authority preservation | implemented but unverified | Source/tests reviewed; all debug controls require player access validation |

## Windows Checklist

Every row below remains **blocked** for the required human-style execution because the supported Windows control kernel fails before input. Automated player evidence above is retained separately. No criterion is waived; keyboard/controller/gameplay observations must be rerun when supported access is available.

| Scenario | Implementation | Windows human-style result |
| --- | --- | --- |
| 1. Enter on foot without giant clickable dashboard; pause shows cursor; navigate; resume restores mouselook | Integrated; automated rendered pause inspected | Blocked |
| 2. Park at motel; prompt only while idle; interact opens once; close does not immediately reopen | Integrated | Blocked |
| 3. Bowel debug 25/50/75/90/99%; increasing audiovisual effects; force failure; toilet reduces/resets | Integrated; semantic restart checked | Blocked |
| 4. GIVE $10,000 real wallet, repeated press | Three actual UI callbacks verified in executable | Blocked |
| 5. Trigger earthquake; pause; save; Main Menu; reload; no dead buttons | Ambient gate corrected; short atomic recovery retained | Blocked |
| 6. Set rain; save; quit executable; relaunch/load; rain persists; repeat snow where practical | Final executable semantic restart checked | Blocked |
| 7. Damage approximately 25%; inspect components; save/quit/reload; summary/components retained; steering normal | Final executable damage retained; steering/repair interaction pending | Blocked |
| 8. Pick up/assign companion; destination, world beam, minimap marker, route, destination name/distance all visible | Integrated | Blocked |
| 9. Incrementally board 1/8 through 8/8; ninth incompatible gets NO ROOM; empty truck hits rival carrier and stolen passenger boards | Actual eight-slot composition confirmed; cabin fit/theft unproven | Blocked |
| 10. Exit truck; jump; walk into moving traffic; actual impact/ragdoll/recovery; normal control returns | Existing controller and UTS ragdoll adapter reused | Blocked |
| 11. Accept fare; long-distance pickup beam; board and switch to destination; selected waypoint beam | Existing Compass art / pooled world renderers | Blocked |
| 12. Pick up nozzle; correct filler glows; connect/fuel; readable cost/progress; driving away ends interaction safely | Integrated | Blocked |
| 13. Hunger 100 shows multiple food images; thirst 100 multiple drink images; both stack; no giant static world billboard | Integrated, image pools authored | Blocked |
| 14. Throw filled container into crowd; large radius; dramatic near-center launch; vehicles/props respond; no physics leak | Bounded shared blast integrated | Blocked |
| 15. Affordable local tow | Tow-only quote $15 + $4/mile + $10 overturn, $60 cap | Blocked |
| 16. Drive normally; debug race; 3-2-1-GO; left/right steering; race; finish/reset; steering still works | Ownership correction integrated | Blocked |
| 17. Countdown/HUD/checkpoints/position/progress/wrong-way/finish/reward/results; 20 rivals remain | Integrated; optional audio clips unassigned | Blocked |
| 18. Drive outside to inner bowl; turn around; drive back out; no collider trap | Authored; no clearance driving proof | Blocked |
| 19. Generate offers; no Parking-to-Infield or same-complex microfare; no ordinary dropoff on active race surface | Eligibility/fairness integrated | Blocked |
| 20. Main Menu Video; change resolution/window mode; Apply/Confirm; restart retains it; pause Revert/timeout also retained as correction scope | Integrated | Blocked |
| 21. Debug cash; buy real upgrade; wallet decreases; NWH setting changes; save/reload retains effect | Existing upgrades exposed with real multipliers | Blocked |
| 22. Tight low-speed U-turn visibly improved and measured; highway remains stable | NWH tuning changed; turning circle unmeasured | Blocked |
| 23. Representative session; capture spikes; identify and correct recurring measured causes; no unmeasured optimization claim | Capture added; repeated snow target append corrected, bounded-work regression test | Blocked |

## Additional Boundaries

- Objective beams reuse Compass art; point pulse/edge direction excludes hazard area footprints.
- Motel and optional-stop ownership retain existing authorities. Accepted optional diversions no longer prevent valid final-fare arrival.
- Jump uses Space / gamepad West. Pickup cancellation uses F6; gamepad cancellation is unbound to avoid existing driving conflicts. Existing eject command remains.
- Chest shape uses the current adult clothed geometry and career field. Player ragdoll currently uses the supplied UTS visual, not the customized seated avatar.
- All eight profiles now contain available imported tree/fence/lumber art. Project-owned fence/lumber prefabs reuse the existing bounded breakaway adapter; no new placement system or scene generation. Static bins/benches/work-zone signs were not found in the focused inventory; the dynamic Weatherade crate and unresolved-material streetlight were excluded. Pool authoring is not proof of dressed levels.
- No broad collider rewrite. No route, save, input, physics or UI replacement framework.
- The existing restart harness now offers `interactive` mode: only strict GUID vendor-root isolation, no injected inputs, fixture state, menu calls or automatic PASS. Real saves remain outside that root.

## Vendor Report

- VENDOR ASSETS AUDITED: NWH, UTS, Compass, Pixel Crushers, Weather Maker, Weatherade, Heat, DOTween, Spline Spawner, EasyRoads, installed scenery.
- VENDOR ASSETS USED: existing public APIs and adapters; Heat/uGUI controls; existing Compass beacon art; NWH static trees; project-owned EasyRoads tunnel authoring.
- RELEVANT ASSETS NOT USED: dynamic crate in static dressing; destructive one-shot Compass beacon lifecycle; unassigned optional race sound clips.
- CUSTOM SYSTEMS CREATED: semantic snapshot extensions, focused presentation/adapters, machine-local video settings and bounded development frame capture. No second gameplay/save/navigation authority.
- VENDOR SOURCE MODIFIED: NO by this pass. Unrelated pre-existing vendor dirt is excluded.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Local Checkpoints And Next Step

- `74c809aa`: canonical weather restore.
- `3390a0d1`: coordinated restored-body wake plus actual physics tests.
- `f3a9ad34`: integrated player-facing runtime corrections and focused tests.
- `5fd612d8`: authored needs art, available imported dressing pools, and two-way speedway access.
- No push. Unrelated robot art, vendor changes and EasyRoads queue metadata remain outside these commits.
- Cleanup through vendor-backed facade passed for obsolete isolated runs `c3eda705292e42c69973eac62840d583` and `0b9efb8267a343d7a7b975992c225437`. Cleanup correctly refused `858b82ce0bc04707930a823fa154132c` because its early failure left a differently named profile; nothing deleted from that root. Similarly retain failed `02f3a2f30bc348fb9bb128535c176db7` rather than bypass profile guards. Real saves untouched.
- Retain current semantic fixture `3f923b7843b94ea3910d096a96cc1758` and exact driving repro `c414edaffd77498f97368d67075ed71d` for validation continuity.
- **Exact next step:** when foreground Windows interaction is available, run final executable with `-truck-taxi-restart-test continue -truck-taxi-test-run c414edaffd77498f97368d67075ed71d -truck-taxi-test-drive`, keep focus, then `manual` and repeat/save in another region. Do not modify input gates, engine state, pose or saves to make it pass. Complete the 23 rows against this candidate, including turning-circle and representative hitch measurement. All independent implementation/checkpoint work above is retained; no rebuild/re-audit is needed before this unchanged-build test.
