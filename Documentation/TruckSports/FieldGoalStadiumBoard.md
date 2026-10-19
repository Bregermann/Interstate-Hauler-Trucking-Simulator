# Field Goal stadium repair - October 19, 2026

## Delivered source changes

The upper-right upright now carries the actual wind sock, on a one-metre mast at 19 metres above the turf. It follows every kick-distance change and uses the existing authoritative wind. The midfield Party wordmark faces the right-hand (+X) stands. The formerly white stadium apron and retained end-zone concourse use the existing textured grass foundation and real static collision.

The field is 120 yards (109.728 metres), including two 10-yard (9.144 metre) end zones. Short hashes advance exactly one yard (0.9144 metres); both sidelines show 10/20/30/40/50/40/30/20/10. The Party wordmark sits at the true 50-yard line. New original transparent Field Goal artwork spans 50 metres across the goal-side end zone. Paint has no collision.

A physical scoreboard behind and above the uprights contains participant names/colors, goals, kicks, current shooter, attempt/tiebreak, distance, result, weather, actual wind MPH/bearing, input prompts, career statistics and Heat replay/practice controls. Oversized duplicate gameplay strips are hidden while this board owns presentation; setup, help, pause, reconnect and results stay readable screen interfaces. Long names fit their cards. The approach view keeps the destination and sign visible, including the longest existing kick.

The audience contains 2,304 existing Taxi-character spectators: 32 live animation rigs and 2,272 baked posed fans using shared instanced materials. DOTween coordinates jumps, paired mock scuffles and eight pooled RoadProps canisters dressed with the existing amber jug material. Cosmetic jugs cannot collide with a kick.

A rare post-result vignette uses the production truck visual in referee stripes and an oversized cap. It chases an existing Taxi pitch invader, calls the existing `TruckTaxiPedestrian.TryStrike` jointed ragdoll, launches the character above the stadium, then leaves. It starts only after the authoritative kick locks, cannot start twice, pauses/resumes, and cannot change scoring. Replay waits for it to finish. Defaults: 4.5% chance per resolved kick, ordinary crowd antics eight seconds apart. The fixed event pool is reused. Configure `FieldGoalStadiumAntics.refereeChance` on the saved match object; zero disables it.

## Actual causes

* The old turf spanned z=-50 to 130 while the goal/end zone moved between z=30 and 62. Fixed paint extended beyond the goal, making the end zone appear midfield. One anchored field assembly now follows the goal between attempts; the turf back line ends at the goal plane. Existing ball-at-origin distances, kicking response, AI and swept goal rules are retained.
* The stadium apron was created through a visual-only helper that deliberately removed its primitive collider. The main field collider ended at the goal plane. In addition, `FieldGoalProjectile.Contact` recognized only the main field as ground. The repair adds a shallow-below-turf solid runoff slab and retained-concourse mesh collision, and recognizes those owned surfaces in the existing ground-contact chronology. A missed ball settles above ground and resolves once; no second scoring authority was introduced.
* The referee's source truck mesh has a nonzero model offset. Preserving that offset fixes the initial floating presentation. Null-safe Animator cleanup fixes an observed destroyed-object access during menu exit.
* The owned Editor test dispatcher retained a completed TestRunnerApi handle after domain reload, preventing the next queued job from starting despite passing tests. Finished-run cleanup now releases only the owned completed API/callback and preserves any new run. The subsequent 30-test suite, scene preflight and combined-build dispatch executed successfully.

## Saved integration and assets

Saved scene: `Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity`.

* `FieldGoalBoardAuthoring`: additive field/scoreboard/wind/runoff upgrade.
* `FieldGoalFieldArtAuthoring`: exact paint, numbers, midfield orientation and end-zone art.
* `FieldGoalCrowdAuthoring` / `FieldGoalAnticsAuthoring`: existing-character crowd and fixed event pool.
* `FieldGoalStadiumBoard`: read-only per-slot display and owned ground membership.
* `FieldGoalProjectile`: existing contact chronology extended to owned runoff.
* `FieldGoalCrowd` / `FieldGoalStadiumAntics`: bounded presentation tied to locked results.
* `FieldGoalBoardProbe` / `FieldGoalCrowdProbe`: explicit development acceptance, not ordinary match behavior.
* `SportsAudienceAuthoring`: optional preservation of the existing Taxi ragdoll; default behavior for other consumers is unchanged.
* `GoddessPhysicsTests`: cross-mode physical/lifecycle regression, including referee cleanup.

Original artwork: `Assets/TruckFieldGoal/Art/Truck Field Goal end-zone wordmark.png` (2172 x 724, transparent). Built-in imagegen produced original hand-lettered late-1990s/early-2000s console-party lettering: ivory/petrol/gold, semi-truck T, football O, goalpost L. Material UVs omit transparent padding without modifying the image. Other owned additions are the grass-runoff material, referee stripe texture/material and charcoal sign material. `Art/Menu Preview.png` is an actual updated native gameplay capture; its asset identity is retained.

Regeneration installs the same configuration through the existing authoring path. The additive menu command is **Truck Sports > Field Goal > Upgrade stadium scoreboard**. Ordinary builds validate the saved scene and do not rebuild hand-edited scenes.

## Acceptance evidence

| Run | Observed result | Limits |
|---|---|---|
| Native final field/board | 42 checks, 0 errors; October 19, 20:47:21 UTC. Full four-Hard-AI physical bumper-kick match, 20 attempts/all five distances, actual legal goals, exact field art, upper-upright sock, right-facing midfield art, long names, Heat angle/speed/rewind/continue, two rematches and one-listener menu cleanup. | Editor runtime; no human/controller claim. |
| Native runoff fixtures | Two actual authoritative football bodies under gravity with 30 m/s downward CCD starting velocity: side runoff and beyond the end line. Both contacted green ground, stayed above it and locked one zero-point miss. | Injected starting state followed by PhysX, not human kicks. Included in the 42 checks. |
| Native dense crowd | 33 checks, 0 errors. Real bumper contact/legal goal triggered one reaction; forced jump/scuffle/jug/referee fixtures; actual shared ragdoll; pause/resume, score invariance, three rematches, fixed renderer counts and menu cleanup. Overview contained 1,791 visible distant fans. | Forced presentation fixtures are distinguished from the real kick. Paused manual-camera CPU samples are not sustained Windows/GPU benchmarks. |
| Collection PlayMode | 28/28 passed, October 19, 20:54:07 UTC. Includes Mattress rain/spectator, Parking 1-4 ownership, both real Trailer Plug drops and referee lifecycle. | Software-device and physical simulation checks, not physical-controller testing. |
| Kickball EditMode | 30/30 passed, October 19, 20:58:02 UTC. | Rule tests, not proof of all gameplay. |
| Combined saved-scene preflight | Passed October 19, 20:57:35 UTC; startup plus 41 sports scenes. | Windows build/player acceptance is separately recorded below when finished. |

Controlled native renders at 1280x720, 1920x1080, 1024x768 and 2560x1080 were captured and inspected. The physical board stays fully visible at the longest kick and occupies at least 180 vertical pixels at 720p. These are explicitly rendered gameplay targets, not standalone-window captures.

Durable native receipts: [FieldGoalEvidence/20261019-native.txt](FieldGoalEvidence/20261019-native.txt). Detailed local receipts/images remain under `Logs/TruckSports/EssentialRelease/Accepted/FieldGoal-Board-42-20261019`, `FieldGoal-Dense-Crowd-33-20261019` and `Goddess-28-20261019`.

Rejected/interrupted evidence is retained: initial long-name overflow, destroyed-Animator cleanup, missing animation reference, missing model offset, one exact-name encoding mismatch, and the first 23/28 PlayMode repeat. The latter involved four background software-input starts and one omitted optional reflection argument; the fixture now sets/restores its own background input policy, enables its software device and supplies `Pause(0,false)`. Assertions and production input rules were not weakened. Two confirmed whole-PC unexpected restarts interrupted runs; interrupted runs are not passes. A dirty-scene launch guard did not execute tests; a recovery copy was saved before opening the saved front end.

## Windows delivery status

The first 42-scene Windows x64 Development build succeeded October 19 at 21:59:17 UTC: zero errors, 410 warnings, 56m31s, startup plus 41 sports scenes. It compiled 281 shader binaries without shader errors. Compiler warnings include obsolete APIs, unused fields, existing Taxi reference comparisons and the existing legacy/UPM Scene Streamer type conflict; shader warnings include deprecated Forward+/GBuffer interfaces, vendor potentially-uninitialized values and duplicate keywords. No vendor source was changed to silence warnings.

The first actual Windows Field Goal run completed all 42 assertions with zero reported gameplay errors, including both real runoff contacts, four resolutions, the complete four-AI match, rematches and menu cleanup. It then exited with 0xC0000005. The crowd run completed 35 assertions with one error: its relative runtime screenshot path resolved inside the player data folder. Its 60-second actual-rendering sample completed (8233 frames, mean7.29ms, p95 12.50ms, p99 12.53ms at1280x720 on GTX1660Ti, timeScale1, vSync1, uncontrolled host load), but it also faulted at shutdown. These runs are retained as rejected acceptance, not passed releases.

Windows recorded UnityPlayer.dll faults at RVAs0x1df033 and0x1d41c9. Offline symbolication against the matching installed Unity PDB resolves these to `PlatformAccessibilityNodeRoot::get_BoundingRectangle` and `PlatformAccessibilityTestUtilities::GetWindow`. An October18 WER record has the same fault bucket. Unity documents the matching Windows UI Automation shutdown issue UUM-146676 in its [6000.6 release notes](https://unity.com/releases/editor/whats-new/6000.6.0f1).

A targeted process-local `UiaDisconnectAllProviders` experiment returned S_OK but did not prevent the native crash: the repeat again completed 42 assertions with zero errors, then exited0xC0000005. The failed experiment was archived and removed from both source copies. It is not reported as a fix. The screenshot probe's absolute filename correction remains.

Unity6000.5.5f1 is the earliest verified newer Editor release inspected here that lists UUM-146676 as fixed. Its official x64 installer (changesetd16e074b49fd,4,144,689,232bytes) was downloaded inside the project; published MD5 and Unity Authenticode signature both verify. Windows canceled the elevation prompt, so that Editor was not installed. A separate approval question is pending before retrying the project-local installation. The existing host project remains6000.4.10f1; its25settings files and the global install-root/default preferences remain unchanged. The source still uses native accessibility; no module or feature was disabled to conceal the fault.

The final source-matching QA rebuild completed22:40:33UTC with zero errors/57warnings,2m14s and42explicit scenes. The failed cleanup experiment is absent. Managed SHA256 `785f8676ba8c9fe0f35ef5a35f03e30460dccfc3a250c72418a093b487041ffe`;504-file runtime manifest `41fcf93a5a3b8dcc1559e1bc723370d10ac3fdae8ef45b78455a9a0483d8ce17`. The exact-build crowd repeat passed35assertions/0script errors, including the corrected actual-player screenshot, three rematches, one-listener cleanup and60.01s of automatic rendering. It still exited0xC0000005 at native shutdown, so full Windows acceptance remains failed.

That final sample recorded8652frames/renders, mean6.94ms, p95 12.50ms, p99 12.53ms, five generation0collections and a net managed-memory decrease, GTX1660Ti at1280x720, timeScale1, vSync1. Host load was uncontrolled; this is not an isolated GPU benchmark. The actual PNG was inspected at `Logs/TruckSports/EssentialRelease/FieldGoal/Crowd/dense-crowd-actual-player-window.png`. Its close replay angle crops the upper physical scoreboard/transport area; approach views fit, but replay transport visibility remains a specific presentation follow-up. It is not covered by the approach-framing assertions.

The canonical folder is labeled QA pending, not a release candidate. Source is preserved on localmain (includingda4eb683); private Taxi changes remain unstaged and unchanged. Root25host settings and validation25settings match their baselines after restoring the validation-only GraphicsSettings line-ending normalization. No Editor or owned player remains running. The failed manual workaround is not shipped in this source-matching binary.

The one canonical output remains `Builds/TruckSports/TruckSports.exe`, with all companions. The first (historical) managed SHA256 was `f46f4b6b48f4c4c00b63b43e729ee3522b8ad29aa60f5f799d28563640eb5089`; that identity belongs to the rejected shutdown revision. Root25host settings were unchanged; the two scoped title/scene overrides restored exactly. A validation-copy GraphicsSettings LF/CRLF-only normalization was restored to its exact prebuild hash after Editor exit.

Public push is blocked by automatic approval review pending explicit consent for the reviewed source/assets payload and both public destinations. No new public push has occurred. Human playtesting, physical gamepads/wheels, subjective audio listening and an isolated GPU benchmark remain unverified. The full backlog, including the per-sport logo rollout, remains open in `EssentialReleaseChecklist.md`.

## Vendor accounting

* VENDOR ASSETS AUDITED: Union Field stadium, Vista turf, NWH truck/football setup, Taxi/Plowing characters and ragdolls, UTS animations, RoadPropsPack, Heat/uGUI/TMP, DOTween, Weather Maker and installed sign/scoreboard assets.
* VENDOR ASSETS USED: existing stadium/turf; NWH vehicle authority/render hierarchy; Taxi rigs and `TruckTaxiPedestrian`/`TruckTaxiUtsRagdollAdapter`; UTS Girl no Heel idle1_f/cheer_f/idle2_f/run_f/shout_f/work_assembly_f/dance_f; RoadProps Canister_URP; Heat/uGUI/TMP, DOTween and existing crowd sounds/weather wind.
* RELEVANT ASSETS NOT USED: the vendor-fixed Unity6000.5.5f1 Editor is verified but not installed because Windows elevation was canceled; approval is pending. No matching owned scoreboard prefab was found. EasyRoads, traffic and GPS do not provide this static stadium sign. A full dynamic two-thousand-agent crowd is unnecessary for spectators.
* CUSTOM SYSTEMS CREATED: read-only scoreboard/owned-ground adapter, targeted venue authoring, original field artwork, bounded stadium choreography and scoped acceptance probes.
* VENDOR SOURCE MODIFIED: none.
* DUPLICATE VENDOR FUNCTIONALITY CREATED: none. Existing vehicle, weather, rendering, UI, tweening and ragdoll providers remain authoritative.
