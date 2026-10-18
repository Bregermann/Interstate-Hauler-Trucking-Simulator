# Party continuation — Cheese starts and Plowing presentation

This milestone delivers grounded Cheese starts/terrain, Plowing impact presentation, optional AI cameras, swept Cornhole hole scoring and concrete Bowling cab/model repairs. Global identities, remaining Party repairs/new sports, Fare Fight and most of the Goddess pass remain outstanding in their ledgers.

## Cheese Rolling

The old reset held trucks kinematic at terrain+1.2m. Their .30m tires, .13m suspension travel and +.02m anchors left approximately .7934m of unsupported clearance. Twelve controlled starts reproduced an approximately −4m/s drop at GO, followed by suspension rebound. No truck overlap, explosions or earned jump forces were present. Neutral flat-start spinning was not reproduced; the evidence does not establish that every reported spin was an explosion.

Reset now synchronizes the authoritative body/transform pose and interpolation, clears previous spring/wheel motion, and calls the installed NWH public Initialize API. A short preparation phase permits native suspension/gravity settling in Y while holding horizontal position and rotation. Four-wheel support, a stable 2mm ride-height window and bounded velocity are required before each truck is held through the countdown. All active trucks must prepare. A genuine failure cancels safely after a bounded wait. GO restores ordinary dynamics and constraints; the existing deliberately earned jump remains intact.

The main downhill grade is 12.5% gentler. The enormous elevated flat grid, continuous transition, side banks and mud runout remain. Broad smooth noise, rounded humps/divots and a curved rut provide varied paths with bounded gradients. The saved native Terrain and TerrainCollider share the same height data. Scene dressing is fitted to that height. The actual owned truck wheel meshes now use NWH rotating containers; duplicate static cylinder tires were removed. Tire radius, truck mass, normal handling, impacts and scoring remain unchanged.

Two failures were found during validation and corrected rather than accepted: a .04m/s readiness cutoff rejected a stationary supported chassis whose solver velocity alternated near ±.058m/s; readiness now checks actual pose stability as well as bounded velocity/support. The older Windows timing guard then caught StartElapsed reporting 3 during preparation. It now reports 0 until countdown begins, with the original guard assertions retained. Failed/cancelled candidate receipts remain separate from acceptance.

The expanded native regression passed all 12 repeated starts across 1–4 participants plus 12 background trucks, overlap/support/pose checks, early/on-time/late accelerator timing, held driving and real earned jump. A full autonomous match completed through 14 genuine catches at 5–4–3–2, with 235 intentional race explosions, no neutral replays and zero captured errors. Earlier Windows candidate 569e872d also completed 14 catches at 4–5–3–2, but its timing-API guard failure means it is not final acceptance.

## Plowing impact presentation

Both saved modes share a **Blood Amount** percentage slider under **Pause → Settings**. Zero disables the short stylized splashes; the maximum is bounded to 18 small particles per impact, eight pooled systems and short lifetimes. This is presentation on an already accepted physical hit. It never changes target ownership, points, movement, collision qualification or input routing. The preference loads on scene entry and survives rematches/scene transitions. Save/Apply or scene exit persists it.

Hit ragdolls remain visible until the round resets. Once settled they leave active physics. The original flat-color characters combine into a single mesh in their actual resting pose; original rigs are restored on rematch. Custom textured/non-readable meshes retain their original renderers and need their own performance check. Settled presentation bodies do not obstruct driving. Effects and runtime posed meshes are cleaned on reset/exit. Character appearance remains independent of points and the existing +10/+100/−100 rules remain unchanged.

For authoring defaults, choose **Truck Sports → Pedestrian Plowing → Select Crowd Settings**, then expand **Impact Presentation**. **Blood Amount** is the default 0–1 slider; an existing player's in-game preference takes precedence. **Keep Ragdolls** defaults on. See PedestrianPlowingCustomization.md for complete prefab/spawn instructions. Normal build operations preserve the saved scenes and roster. The targeted **Update Impact Options (preserve layout)** authoring command updates these controls in both scenes. Unknown editor-job requests now fail explicitly instead of rebuilding the city.

Native functional tests passed in both arena/chase and Cab Chaos: zero/max/clamped amount, saved setting, genuine physical award, expiry without disappearance, real posed geometry, inactive settled physics, no duplicate scoring, complete rig restoration on rematch and scene cleanup. Hidden-Editor captures are explicitly NOT_TESTED. Actual Windows 720p/1080p settings and close-pose/splash captures are used for visual acceptance. An initial diagnostic camera followed the parked truck; it was corrected before accepting retained-pose visual evidence.

## Current Windows acceptance

Windows x64 Development: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the entire folder. 35 explicit Sports-only scenes; TruckSports startup. Managed SHA256 `ef8a78590f6119af62340482d3b2c16fcc49be010f1e87e0cfa435fb19ea7b28`. Build succeeded with zero errors; all 21 version-bound Windows suites passed. Human/hardware checks remain manual.


Built through LWS.TruckSports.Editor.SportsBuilder.Build. AMD64 PE machine 0x8664 and required UnityPlayer/Data/Mono/CrashHandler companions verified.

```
Succeeded
Errors: 0
Warnings: 31
Bytes: 1246707246
Duration: 00:00:37.7964409
```

All 23 pre-build ProjectSettings hashes and 25 original unrelated dirty-file hashes match. Deliberate failure restoration and full saved-scene validation pass. Warnings are documented existing vendor/compiler/large-mesh items; no vendor sources changed.

All **21 strict Windows suites PASS**, with zero captured runtime errors, matching assembly/catalogue hashes, fresh completion footers and successful process exits. Camera selection: 2,467 checks across 20 saved scenes, actual 720p/1080p views, 0–4 human sources, OFF/ON, long names, nonadjacent humans, shared/hot-seat exclusions, HUD ownership and menu cleanup. Two independent-process preference fixtures pass. Cornhole: 67 clean-hole checks, including 15 repeated real PhysX downward drops, near-hole/upward/hanging rejection and nine original guards. Cheese: 12 repeated supported starts, a full 16-truck autonomous match, 46 guards and 27 view checks. Poker/Skee focused regressions pass. Plowing: contact scoring/replays, impact settings/rest poses in both scenes, cab/jug/GPS/software-device reconnect, 384 crowd, split layouts, two 60-second four-view samples and six complete eight-AI games across Easy/Normal/Hard. A complete four-course five-round Cornhole AI game also passes.

```
tsPlowing-performance-1280x720: PERFORMANCE 60 real seconds / four AI views / eight competitors / 240 crowd / frames=2407 meanMs=24.93783 p95Ms=37.515 p99Ms=43.78699
tsPlowing-performance-1280x720: PERFORMANCE_LIFECYCLE liveFrames=2407 focusedFrames=2407 total=2407 actualViews=4 rematches=0
tsPlowing-cabperformance-1280x720: PERFORMANCE 60 real seconds / four AI views / eight competitors / 240 crowd / frames=2257 meanMs=26.59241 p95Ms=37.536 p99Ms=50.00391
tsPlowing-cabperformance-1280x720: PERFORMANCE_LIFECYCLE liveFrames=2257 focusedFrames=2257 total=2257 actualViews=4 rematches=0
```

Performance is an automated eight-AI/four-rendered-view workload on this PC with other applications left running. It is not a hardware-controller or human-driving benchmark. An earlier human-source sample correctly paused on focus loss and was rejected for insufficient live frames; the accepted workload uses AI-only matches without changing production human pause/disconnect policy. Superseded receipts preserve that failure and earlier cab/model defects. No partial PASS or paused sample is accepted.

Actual Windows images inspected include Bowling human/canonical cab, four-view long names and aligned external wheel meshes; Cornhole 720p setup; camera OFF/ON layouts; Plowing 720p/1080p settings and retained poses. Hidden-Editor renders remain separately labelled controlled captures. Detailed causes and source files are in ShowAICameras.md, CornholeHoleRepair.md, BowlingCabRepair.md, CheeseStartRepair.md and PedestrianPlowingCustomization.md.

Human handling/comfort, physical controllers, wheel hardware, haptics, subjective sound balance and custom textured-character performance remain manual. The full queued release is not complete. Git publication is recorded separately after scoped commit/push; previous work remains published at bcba7784.

The Normal arena AI run completed, but two competitors had zero impacts. Individual AI wall recovery remains a concrete follow-up: obstacle throttle is exactly .3 while the old stall counter requires greater than .3. See PartyNextRepairAudit.md for the observed results and next physical reproduction. Full timed-match completion is not a claim that every competitor made progress or every target was cleared.

## Vendor accounting

VENDOR ASSETS AUDITED: NWH wheel/suspension/visual APIs; existing Terrain authoring and Vista/Path Painter layers; Taxi/UTS ragdoll pooling; Heat/TMP/DOTween; native Mesh/LODGroup/ParticleSystem; existing Sports camera/input/settings and Core-Collapse props.

VENDOR ASSETS USED: NWH native physics/visual containers, native Terrain/collider and existing layers/truck art; original Taxi jointed rigs/appearances; Heat slider and existing UI; native mesh combining and pooled particles; existing cameras/settings/impact feedback.

RELEVANT ASSETS NOT USED: no replacement vehicle controller, terrain generator, save framework, UI framework, fracture framework or graphic gore system is introduced. Existing sport settings remain the preference authority; native primitives supply the small missing presentation effect.

CUSTOM SYSTEMS CREATED: sport-specific preparation semantics, bounded impact preference/presentation adapter, actual settled-pose capture, targeted authoring and explicit regression probes.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
