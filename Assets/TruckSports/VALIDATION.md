# Final 31-sport Windows acceptance — October 16, 2026

Final build `2026-10-17T02:53:33.8658602Z`: **0 errors / 47 incremental build warnings**, Windows x64 PE0x8664, Mono Development, 33 explicit Sports-only scenes, TruckSports startup. Managed assembly SHA256 `8d1f8ed24a700ecb824a5ead1a51b423020c69d31fd239f8aa5d4a1d43eeb126`. The earlier full compilation produced 400 warnings (mostly host/vendor deprecations); it is retained separately and is not presented as a warning-free compile. Two Derby GetInstanceID deprecations remain; vendor sources were not edited to hide warnings. All 12 protected host-setting hashes match, including the existing host build list. The deliberate failure-restoration fixture also passed.

**33 Windows acceptance areas passed**, each successful process required exit0, a fresh PASS result and no player-log exception/shader/fatal/crash entries. 32 gameplay/visual/regression suites passed on build4 (managed assembly 6a9285186a8f408752b814b520936b5868d9aae85532c8eafcd6d3953950ebad). Final narrow corrections adjust optional probes, bind Derby displays to the vendor's real mirror faces, and reuse Bowling's existing per-camera visibility in Buzkashi. Every other player source/asset/meta is byte-identical; 11,575 compiled methods including ALL physical gameplay code remain equivalent after resolving metadata addresses. Only Buz's reviewed camera Layout binding differs in normal runtime code. Native and serialized-file proofs verify eight mirror reference arrays and four existing camera visibility adapters, with7032 Derby/2943 Buz pre-existing components unchanged. Final menu/driving smoke, Derby and Buz focused suites, and three-course workload passed on the delivered assembly. The other29 case results retain their build4 provenance. This is33 accepted areas with explicit differential validation, not a claim that all33 processes ran on the final bytes. Evidence is local under `Logs/TruckSports/FinalWindows`, with version-specific assembly identity, equivalence proof and batch manifest under `Logs/TruckSports/SevenSports`. Superseded/failed attempts remain labelled; they are not counted as passes. The first production-workload fixture incorrectly retained Derby's two-participant default and failed its four-owner assertions; its numbers are excluded from four-view performance claims. The corrected final workload actually uses four human viewports plus the remaining AI seats.

The actual Windows scene tour loads all 32 non-front scenes, checks missing scripts and one listener, starts maximum AI rosters for 25 event sessions, and repeats pause/rematch/menu cleanup. Normal overlay screenshots use actual window sizes 1280×720, 1920×1080, 1440×900, 1280×960 and 2560×1080. Focused suites additionally test four owning human viewports using software devices. Representative final images were inspected; not every captured image was individually reviewed. Controlled Camera.Render/UI composites are labelled separately from these normal-window screenshots.

## New sports and physical match coverage

All six new modes completed actual saved-scene AI series plus focused rule/input/lifecycle checks in build4; their physical gameplay IL is equivalent in the delivered build, and subsequent scene repairs are verified presentation-only. Physics series accelerate simulation time and measure progress/outcomes, not frame performance. Scoring fixtures and posed contacts are explicitly separate from naturally played matches.

```text
-tsPoker: MATCH level=0 hands=17 actions=233 winner=2 chips=4000
-tsPoker: MATCH level=1 hands=18 actions=262 winner=4 chips=4000
-tsPoker: MATCH level=2 hands=27 actions=293 winner=5 chips=4000
-tsTug: MATCH difficulty=0 heats=4 draws=0 wins=3,1 heaves=72 peakTension=239.2272 peakSpeed=31.63701 maxAdvantage=11.34332
-tsTug: MATCH difficulty=1 heats=5 draws=0 wins=3,2 heaves=419 peakTension=700.7675 peakSpeed=33.04056 maxAdvantage=3.994428
-tsTug: MATCH difficulty=2 heats=5 draws=0 wins=3,2 heaves=520 peakTension=775.1224 peakSpeed=33.25211 maxAdvantage=4.542258
-tsSkee: MATCH case=0 winner=1 totals=10,20,10,0,0,0,0,0 attempts=9,9,9,9,0,0,0,0 physicalLaunches=36 genuineEntries=4 misses=32
-tsSkee: MATCH case=1 winner=2 totals=10,40,70,30,0,0,0,0 attempts=9,9,9,9,0,0,0,0 physicalLaunches=36 genuineEntries=8 misses=28
-tsSkee: MATCH case=2 winner=0 totals=70,30,50,40,0,0,0,0 attempts=9,9,9,9,0,0,0,0 physicalLaunches=36 genuineEntries=7 misses=29
-tsSkee: MATCH case=3 winner=0 totals=80,20,30,0,60,20,0,30 attempts=9,9,9,9,9,9,9,9 physicalLaunches=72 genuineEntries=9 misses=63
-tsSkee: MATCH case=4 winner=0 totals=20,0,0,0,0,0,0,0 attempts=9,0,0,0,0,0,0,0 physicalLaunches=9 genuineEntries=1 misses=8
-tsLogProbe: MATCH difficulty=0 winner=0 heats=6 wins=5,1,0,0 draws=0 falls=6 contacts=26 roll=73.66241 peakTruckSpeed=14.88191 braces=2
-tsLogProbe: MATCH difficulty=1 winner=1 heats=12 wins=1,5,0,3 draws=3 falls=39 contacts=122 roll=73.84644 peakTruckSpeed=14.88898 braces=2
-tsLogProbe: MATCH difficulty=2 winner=2 heats=17 wins=3,4,5,3 draws=2 falls=53 contacts=142 roll=75.83377 peakTruckSpeed=15.01416 braces=2
-tsDodgeProbe: MATCH difficulty=0 winner=1 rounds=4 wins=1,3 draws=0 bounces=131 strikes=11 returns=2 eliminations=4 neutralizations=5 friendly=0 peakY=5.43413 contactPeak=3
-tsDodgeProbe: MATCH difficulty=1 winner=0 rounds=21 wins=3,2 draws=16 bounces=6942 strikes=296 returns=26 eliminations=75 neutralizations=185 friendly=63 peakY=6.632982 contactPeak=3
-tsDodgeProbe: MATCH difficulty=2 winner=0 rounds=8 wins=3,1 draws=4 bounces=1037 strikes=41 returns=8 eliminations=14 neutralizations=19 friendly=8 peakY=5.553286 contactPeak=3
-tsDerby: MATCH difficulty=0 winner=1 heats=3 wins=0,3,0,0,0,0,0,0 hits=40 explosions=3 draws=0 peakIncomingNormal=19.34106 peakSpeed=12.08145 peakRoll=99.92561 maxDamageEventsOnTruck=260
-tsDerby: MATCH difficulty=1 winner=7 heats=3 wins=0,0,0,0,0,0,0,3 hits=273 explosions=21 draws=0 peakIncomingNormal=17.67043 peakSpeed=16.92241 peakRoll=147.1845 maxDamageEventsOnTruck=242
-tsDerby: MATCH difficulty=2 winner=2 heats=10 wins=1,0,3,1,1,1,2,1 hits=1351 explosions=70 draws=0 peakIncomingNormal=22.00971 peakSpeed=18.80929 peakRoll=166.994 maxDamageEventsOnTruck=321
-tsMattress: MATCH races=3 winner=3 total=6,8,13,19,8,19,18,16 ERRORS=0. Actual PhysX/NWH-disabled gravity platforms and AI weight controls; no hardware/human playtest.
-tsWife: MATCH races=3 winner=1 points=6,10,9,4,0,0,0,0 ERRORS=0. Actual NWH/PhysX, controlled drop/retrieval reproduction and AI; no human/hardware validation.
-tsBuzkashi: MATCH score=4,2 deliveries=6 attachments=77 breaks=70 cargoRecoveries=0 truckRecoveries=0 clock=300.0137
```

Poker's isolated and native rule acceptance covers 505 checks plus exhaustive enumeration of all 2,598,960 five-card combinations, 84 random legal-action tournaments, side pots, short all-ins/reopening, uncalled returns, odd chips, bust-outs and conservation. Actual final Windows tournaments and four-view privacy/source checks pass. AI receives only own-seat/public snapshots. No private cards enter world faces or replays; hold release clears TMP text, geometry and character metadata. Human shared-screen privacy still depends on viewers respecting each other's panes.

Tug uses actual NWH traction and one physical pulling chain, not a second live force. Skee uses physical EasyRoads ramps and swept whole-truck open-ring entry, one award per delivery, solo practice, four simultaneous lanes and eight-player hot seat. Log uses the unchanged owned Floating Entities solver, real buoyant roll and water elimination; its exposed triangle-area metadata is corrected by an LWS adapter before the vendor forces. Dodge checks live/friendly/neutral states, finite returns, actual trampoline flight, same-step strike arbitration and wipes. Multi-truck AI samples are draw-heavy: one Windows Normal4v4 sample completed3–2 after21rounds,16drawn. Human pacing remains a playtest priority; completion is not evidence of ideal balance. Derby checks localized actual contact damage, public NWH consequences/deformation, rollovers, real rear-view pixels, resettable collapse props and rematch repair. A separate stationary-human-versus-AI reproduction explodes at 4.999996 active seconds; paused time is excluded and survivors remain protected for 8.2 seconds after the locked heat.

The actual Windows run found and reproduced a Derby reset failure that native full matches missed. Disabling NWH cancels RevLimiterCoroutine before it clears the public revLimiterActive latch; the vendor disable/repair/start lifecycle preserves that flag. Subsequent heats then have valid input but cut engine torque, stall and repeatedly explode at their starting positions. Controlled before/after public-state fixtures retain True/True before the fix and False/False after reset/wake with the fix. LWS clears only that transient latch on a fresh Derby reset; the normal active rev limiter, vehicle physics and five-second rule remain intact. A focused injected-state regression and per-heat all-AI departure assertion now cover it, followed by the final actual Windows full series. Failed and diagnostic player evidence is retained under BeforeDerbyResetFix. Wheel-spin retention was investigated but was not the demonstrated cause; no speculative drivetrain or physics retune was applied.

## Existing fixes and presentation

Final normal-window inspection also caught a narrower Derby presentation defect: rear-camera render textures contained valid world pixels, but authoring bound them to objects named MirrorGlassL/R, which are actually the vendor's housings. The true display meshes are mirror lft.001/rt.001 using the vendor MirrorGlass materials. Native material/mesh inspection and isolated actual-cab renders proved the mismatch. Saved references and authoring defaults now select those true faces and order left/right cameras consistently. All7032 other scene components and every other serialized document remain byte-identical. The focused Windows suite checks visible glass and all eight independent rear targets; actual final live cab screenshots were inspected after the reference repair. No UV/geometry/offset/custom shader, camera placement, truck physics, damage, scoring or vendor source changed. The preliminary visibility-only attempt did not fix the image and was reverted; its evidence is retained separately, not claimed as visual acceptance.

The actual Buzkashi four-view image exposed its owning exterior mesh blocking the newly added cab camera. The fix reuses the existing BowlingCabCamera per-camera visibility component, with lane=null so Bowling never moves the Buz camera. Layout caches each owning exterior once; its original Taxi interior stays visible while other trucks remain visible. Four existing visibility adapters are saved and installed idempotently by authoring; all2943 pre-existing scene components and other serialized documents remain unchanged. Controlled native four-camera checks prove correct ownership and complete flag restoration. Final Windows workload additionally checks the actual render-event visibility state and its live four-cab screenshot was inspected. Driving, towing, camera position, scoring and AI remain unchanged.

Minefield measures continuous supported rest rather than first touch, retains every blast/final flag, permits backwards settling and shows the full tumble. Musical Parking validates full authoritative body capture, final two-player bay, all-unsecured elimination, zero-capture replay, retargeting and served/escalating penalties. Mattress's full eight-AI series, three physical world/reset repeats, fallen trees, bounded sheep blasts, actual Taxi ragdolls and ninth replay scenery source pass. Wife's loop races, ordered route flags, drop/pickup/penalty/role swap and pair cards pass. Swing's three layouts, equivalent per-round conditions and genuine physical scoring pass. Buzkashi passes actual attach/release/ram steal, grace/reclaim, scoring/recovery, shared and owned-cab views. Wallball passes eight separated starts and immediate opening movement; its final actual4v4 match records `MATCH shots=38 physicalReturns=23 legalReturns=13 maxVolley=5 score=11:4 neutralReplays=0`. It remains explicitly experimental pending human acceptance.

Shared snow tests observe actual NWH/PhysX supporting contacts, GPU trace readback, broad mattress/object traces, reset/accumulation and dry/pause exclusion. Water tests use actual Log/Bog/Dock entries and recorded interactions, independent underwater/above-water cameras and no global fog mutation. No new fluid solver or tire-grip policy was added. Legacy cab suites verify five existing modes, actual original Taxi interior/wheel animation, independent pad/mouse look, fixed shared sticks and source transitions. Existing deliberate modal/reconnect neutral gates remain; normal billiards turns do not acquire them.

Bowling motion/full-game and pin-rebound cases, billiards spherical displacement-driven rolling/rebounds/stopped/reset/cue/pocket presentation, Keepaway regression and the six Terrier ordered finish/solid-wall cases passed in build4 with unchanged normal gameplay IL/assets in the delivered executable. Terrier retains valid whole-body finish lock, swept crossing and gentle-scrape safety; later collision callbacks cannot revoke it. Earlier full scoring/AI evidence remains below and in the sport folders.

Actual Windows visual inspection caught two defects before delivery. Wife's team names bypassed individual score cards and incorrectly read DNF during countdown; the read-only card adapter now maps pairs and displays READY. Tug's height-fitted shallow rock meshes became 282–441m wide and hid the lava; native authoring caps only decorative footprints and adjusts presentation framing. Every physical collider/body was verified unchanged. The corrected screenshots and focused physical regressions passed before this final rebuild.

## Sustained final-executable rendering

Three separate 60-real-second, timescale1 samples at 1280×720 exercise four actual owner views; Derby includes actual mirror cameras. Normal result/replay transitions can temporarily switch to shared views, so the counters report actual camera renders rather than assuming four on every frame. The same AI planners supply the four software-owned control slots. These are whole-frame observations under uncontrolled concurrent host load, not isolated GPU benchmarks, real-controller tests or guaranteed frame rates.

```text
PERF scene=TruckDemolitionDerby seconds=60.01 frames=6874 mainRenders=27496 mirrorRenders=3168 meanMS=8.732 p95MS=18.755 p99MS=25.013 GC0=4 managedDelta=25718784 GPU=NVIDIA GeForce GTX 1660 Ti resolution=1280x720 timescale=1
PERF scene=TruckMattressRacing seconds=60.02 frames=4967 mainRenders=16668 mirrorRenders=0 meanMS=12.081 p95MS=18.763 p99MS=18.783 GC0=2 managedDelta=6717440 GPU=NVIDIA GeForce GTX 1660 Ti resolution=1280x720 timescale=1
PERF scene=TruckBuzkashi seconds=60.01 frames=9453 mainRenders=37812 mirrorRenders=0 meanMS=6.348 p95MS=6.274 p99MS=12.491 GC0=0 managedDelta=15384576 GPU=NVIDIA GeForce GTX 1660 Ti resolution=1280x720 timescale=1
```

## Remaining human checks

1. Play Derby in four-way cab view: rear mirrors, five-second warning, impact readability and audio; compare shared broadcast.
2. Try Tug timing/assist, Skee speed and hot-seat flow, Log recovery, Dodge pacing/returns, and Poker privacy/bet confirmation with real controllers/shared sticks.
3. Play the upgraded Mattress/Wife/Swing/Buz courses; review snow/water readability and subjective balance. Wallball remains experimental.
4. Check speakers, vibration, wheel hardware, focus/reconnect ergonomics and long-name HUD readability on the actual displays. These have software-device coverage, not physical-hardware approval.
5. A fresh remote clone plus licensed-package imports/build has not been repeated. Core-Collapse, Layer Lab, Floating Entities and selectively owned hair originals remain local licensed prerequisites, not public redistributions.

VENDOR ASSETS AUDITED: NWH, Heat, DOTween, Cinemachine, EasyRoads, UTS/Taxi characters/cabs, Weather Maker, Weatherade, River Modeler, Floating Entities, Core-Collapse, installed scenery/UI/fonts and the accessible owned catalogue; Rope Toolkit availability was checked.

VENDOR ASSETS USED: the appropriate foundations above, through existing or new LWS adapters. Original truck interiors/steering and Transport Helicopter presentation are preserved.

RELEVANT ASSETS NOT USED: Rope Toolkit was absent from installed/cache/accessible owned paths; one PhysX joint chain supplies the missing tug semantics. Legacy built-in water examples are unsuitable for URP. GPS, dialogue, traffic routing and online services are outside these offline arenas.

CUSTOM SYSTEMS CREATED: six sport rule/progression/AI systems, contact-qualified Derby damage semantics, shared input/camera/presentation/snow-water coordination, bounded actual-state replay and focused acceptance tooling. Vendor physics/rendering retain their authority.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Historical acceptance

# Queued production pass: shared presentation foundation (native only)

All27 current saved Sports scenes now share owned Heat Roboto Condensed font variants, beveled navy/cream/orange broadcast backing, DOTween page fades and responsive safe areas. Source-only/scene UI changes preserve every Collider/Rigidbody fingerprint. Minefield and Giant Swing maximum eight-name controlled UI captures at1280x720,1920x1080,1440x900,1280x960 and2560x1080 reproduce and fix the clipped first row; world labels have per-camera pixel caps/distance culling. Final controlled run0errors, one listener, clean collection return;720p Minefield image actually inspected. No claim of final Windows overlay, controller/human acceptance, every UI screen or completion of the queued31-sport update. Current executable is still the verified25-sport milestone below. Evidence: BroadcastBefore/BroadcastAfter and SevenSports/broadcast-apply-result.txt.

#25-sport Windows acceptance — October16,2026

Build 2026-10-16T12:20:39.8422894Z,0errors /29warnings,25sports /27scenes. Managed assembly SHA256 `1f769e585301e8891d4f8c5d86d64505fedcecfc3666991e384a43a73006251f`.12 host files unchanged. One F: output folder, all companions present. No second validation-build folder.

Windows cases are archived in Logs/TruckSports/FinalWindows. Bowling motion/full games, Pong full/focused, FieldGoalFocused, TargetFocused, SumoFocused, BungeeFocused, FishingFocused and BogFocused passed on the corrected-containment build with assembly b8b360b7e9f02f8e559b67db8643ca08c1988bd6b1c64fd16b645e6cc7bad528. Subsequent source changes are diagnostic/test-only: added KeepawayPauseProbe, isolated its opt-in flag and moved the regression pause outside an in-flight fixed coroutine. Final assembly above passed KeepPause, corrected full KeepawayRegression, TerrierFinish and normal Delivery. Game rules/physics did not change between these assemblies.

Bowling Windows: all four normal/sideways/backward/committed-gutter carries reach the physical pit; three ordinary four-lane games totals96/116/89/111,83/112/130/100,112/115/119/105; three strikes and35spares,40gutters,632actual pin returns across the samples. Hot-seat122/108 with21/20deliveries. Actual tenth bonuses, rack resets, pin-specific impact/cooldown/caps/down-latch/pause/rematch/menu passed. Score variation is physical seeded simulation, not a human balance claim. Final equivalent30-case native before/after remains141→159pins,0→23meaningful returns,0→61secondary contacts,0strikes/30imperfect.

Beer Pong Windows full matches and focused suite passed0errors, including ten physical cup interiors,500m/s entry, side/bottom/overflight rejection, real table bounce/rim-in/rim-out, charge/release, finite aftertouch, source replacement and replay lifecycle. Native83rules cover rebuttal/overtime/practice beyond the full match outcomes. Field Goal/Target/Sumo Windows focused physical and source/lifecycle checks passed; full multi-difficulty native matches are documented in their sport folders.

Rendered60-real-second Windows samples at1280x720, GTX1660Ti: Bungee8178frames/32712camera renders, mean7.36ms,p9512.51ms,0gen0; Fishing9460/37840, mean6.38ms,p956.28ms,0gen0; Sumo9239arena frames, mean6.53ms,p956.29ms,0gen0; Bog6612frames/26448views, mean9.08ms,p9512.52ms,38gen0. These are observed whole-frame times with concurrent host load uncontrolled, not isolated GPU timings or performance guarantees. Native batch samples with automaticrenders0 are not substituted for them.

Keepaway initially reported two pause assertion failures. Instrumented Windows comparison proved that WaitForFixedUpdate can resume inside the already-started physics step: pausing there allowed one remaining step (measured0.145m in one diagnostic). All six normal Update-phase pauses preserved exact position, velocity and score. The regression now yields to normal frame/input timing before requesting pause and retains exact equality. No tolerance relaxation or gameplay freeze workaround. Failed evidence is retained at Logs/TruckSports/SevenSports/WindowsKeepPauseFailure; native and Windows traces retained. The corrected full regression passed both natural autonomous possession matches, mixed rosters, physical pickup/steal/recovery, ownership, cab steering and clean menu return.

Visible title/menu and representative Bowling/Pong/FieldGoal camera captures were inspected. Native/manual camera-only five-aspect captures omit overlay UI and are not full-window HUD acceptance. The queued shared UI pass must validate actual full windows at720p/1080p/16:10/4:3/ultrawide. Real controllers, shared-device ergonomics, vibration, speakers/audio, human handling/balance and clean-clone licensed imports remain manual checks.

VENDOR ASSETS AUDITED: existing NWH/PhysX, DOTween, Heat, Cinemachine, EasyRoads, Weather Maker, Weatherade, Core-Collapse, Layer Lab fish, installed scenery/Taxi cab and owned local asset catalogs.
VENDOR ASSETS USED: those foundations where documented per sport; pin-wall presentation reuses Pong coils and Cornhole launch audio.
RELEVANT ASSETS NOT USED: Rope Toolkit absent from audited installed/cache/owned paths; Bungee documents its one-spring fallback. Legacy built-in water samples were not blindly installed into URP.
CUSTOM SYSTEMS CREATED: sport rules/coordination/adapters and focused acceptance probes; no competing vehicle/weather/UI framework.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


## Earlier evidence (historical)

# Eleven-sport expansion validation — 2026-10-15

The combined normal-company Windows x64 Development build succeeded with0errors30vendor warnings and passed actual injected pointer/keyboard startup, selection, driving, pause/resume and return. All new events completed full AI matches; repeated collection transitions and720/1080 layouts passed. [Detailed current evidence and limits](../../Documentation/TruckSports/ExpansionValidation.md). Real controllers, rumble/audio listening and human acceptance remain manual. Approved dependency publication is complete in `12f0653faae7290f0e6895097e9ff68ce06447b3`; original asset/metadata blobs and existing executable/DLL hashes were rechecked. Fresh remote-clone import/build was not repeated. The verified final player and current source now reside in the main F: project.

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


## Shared focus versus reconnect policy — native acceptance

InputSystem1.19 emits InputDeviceChange.Disabled both for explicit suspension and its TemporaryWhilePlayerIsInBackground focus path. Devices stay added while enabled becomes false. Existing readiness checks then misreported an attached keyboard as disconnected; controller callbacks treated Disabled directly as unplugged. FocusBefore reproduces that exact installed-manager path: added=True,enabled=False,paused=True,screen=Join. FocusAfter retains the same attached source and opens ordinary Pause.

Scene-owned SportsFocusPolicy now distinguishes attached suspension from Removed/Disconnected. Human focus loss pauses without automatic resume. Actual missing keyboards/controllers still require reconnect/replacement. Existing neutral/fresh modal gates remain in each sport; ordinary Billiards turns receive no new gate. An application-background override lets AI-only play continue and is restored on scene exit. No host setting or vendor input source is edited.

Eight saved-scene families passed160native checks/0errors: shared event sessions, Bowling, Cornhole, Ice Hockey, Air Hockey, Wallball, Cheese Rolling, Billiards. Each exercises keyboard suspension/sync, exact motion while paused, no automatic focus-return resume, held-input exclusion and neutral rearming, AI continuation with suspended devices, actual software keyboard removal/replacement, controller removal/explicit reassignment and continued match. Billiards motion sample is its authoritative cue-ball body; the truck is intentionally a collider-free visual rig. Earlier four matrix failures were countdown-phase fixture errors: hockey does not arm driving until the faceoff completes. The test now waits for real live hockey before asserting neutral readiness; failures retained in FocusMatrixCountdownFixture. No physics/rules relaxation.

Evidence under Logs/TruckSports/TerrierUpdate/{FocusBefore,FocusAfter,FocusMatrixEvidence}. These use real installed InputSystem lifecycle calls with injected devices, not OS focus switching, physical-controller checks or human playtesting. Actual Windows focus/hardware checks remain pending the final combined production build. The existing25-sport executable predates this stage. Saved native adapters retain every collider/Rigidbody fingerprint; source authoring/runtime scene installation preserves the policy on regeneration.
