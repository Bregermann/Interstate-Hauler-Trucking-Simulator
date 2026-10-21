# October21 active repair pass — continuation checkpoint

This is active implementation, not a completion report. Both supplied requests are preserved in Requests/2026-10-21-GlobalBroadcastCamera.md and Requests/2026-10-21-HumanPlaytestRepair.md. Permanent ledger IDs: GBC-*, GBC-SPORT-<scene>, HPR-*.113directive entries and59per-scene camera entries were added; no historical requests silently imported.

## Execution order

Finish shared broadcast camera implementation/acceptance first; then master P0: Fishing catches, Hockey goals, Fare Fight AI, Kickball home-run state, Sky Tow stability/reachable rings, Trailer Plug hitch/scale, Minefield cumulative distance. Continue the master P1–P4 features after that. The full source instructions, including protected working mechanics and deferred Tug of War, remain authoritative.

## Implemented camera work awaiting full acceptance

- Existing Cinemachine rigs retained. Vendor GroupComposer/TargetGroup/Transposer and Collider handle composition, damping and obstruction.
- SportsBroadcastFraming selects actual active roles, limits zoom by transformed local renderer bounds, ignores far irrelevant competitors, supports perspective/ortho and uses a shared editable Keepaway broadcast framing profile with per-mode overrides.
- Explicit goal/role selectors in core possession, ball, territory, obstacle, racing and collection modes. Camera authoring binds33existing applicable scenes, including the older Hockey camera through a read-only source bridge. No scene environment regeneration.
- Existing CNP prefab and POI system provide at most three legitimate broadcast edge cues. Stealth/hidden participants are never registered. Cab/private/precision views remain separate.
- Existing retired-view spectator adapter extended: one camera pose writer, stable valid live target, original owner/input retained, cab reactivation preserved. Existing Keepaway/Derby custom wreck views also stop following flying dead bodies.
- Original production SportsBroadcastAuthoring (HUD theme) is restored unchanged. New tool is SportsBroadcastCameraAuthoring.

## Actual evidence so far

8/8 native EditMode projection tests passed (including actual rotated mesh projection). First Windows Quick reproduction failed one overzoom check; retained under Logs/TruckSports/PlaytestRepair/Quick. Corrected Quick2 passed40checks; accurate oriented-renderer/first-base-side Quick3 passed42checks. Actual720p/1080p images inspected for Kickball and Magnet. Quick3 primary height: Kickball9.0%, Magnet11.1%; objectives visible. These are focused preliminary results, NOT all-mode acceptance or human certification.

The source has since been extended for the complete33-scene and retired-AI acceptance. That latest spectator change is not yet Windows-verified. Do not relabel earlier receipts as final coverage.

## Validation tools / safety

ROOT: F:/Unity Projects/Interstate-Hauler-Trucking-Simulator.
Isolated Editor project: .sports-validation/TruckSportsPublicValidation. Unity6000.4.10f1. Run Tools/TruckSports/RunBroadcastValidation.py with bind, tests, build sequentially. It syncs only this task's sources, excluding the927 pre-existing pending-file baseline. Do not edit isolated project files while its Editor/player is running. No concurrent Editor/player use. Current pipeline receipt/log: Logs/TruckSports/PlaytestRepair/camera-validation-processes.json and camera-*.log.

Actual Windows probe: Builds/TruckSportsCamera/TruckSports.exe -tsBroadcastReadability -tsBroadcastOutput <absolute evidence directory>. Omit quick for the full pass; quick covers only Kickball/Magnet. Includes structured projected-size measurements, actual images, distant/invalid targets, AI-only rematch, supported human/AI viewport ownership, Hockey bridge and retired Snake AI cab fixtures. Explicit placements/elimination APIs are camera fixtures, not claims of complete gameplay or hardware testing. Runtime errors and watchdog failures must remain recorded.

After successful current binding/acceptance, export ONLY33bound authored scenes, the new source .meta files, owned broadcast compass prefab/profile and their metas from isolated project. Preserve all unrelated Root scenes, vendor files, original41logos, source GUIDs and private pending LwsRoutePlanner/Taxi work. No wholesale asset export, rebuild or staging.

## Delivery state

Accepted prior18-sport build remains Builds/TruckSports/TruckSports.exe (managed SHA256bab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36), backed up at .sports-validation/Recovery/Expansion-Eighteen-Sport-60-Scene-Verified. Prior completed/pushed commit451dbe14. This new repair pass is not yet committed or delivered. Separate camera build does not replace accepted executable until verified.

Do not stop after the camera phase. All new master repairs remain authorized and tracked. Human/device/listening checks must be distinguished from automation. Tug of War remains untouched and requires future brainstorming.

## Current continuation — October21, after Full1

- Full1 Windows camera run:652checks/3errors. Joust had a legacy pose writer;
  Plowing Cab was incorrectly tested as shared mode. Hill's objective center
  passed but its full ring was visually cropped, which remains a real failure.
- Corrected Joust's competing rotation and excluded its scene-owned lance/shield
  from tractor renderer-size measurements. Full2's actual Joust screenshot now
  shows the tractor at11.1% rather than a tiny tractor inside a lance-sized bound.
  Native camera projection tests:9/9pass, including long-attachment exclusion.
- Cab Chaos intentionally forces cockpit mode through PlowCabMatch.NormalizeSetup.
  The probe now verifies that preserved ownership/full-screen cab and AI-only cab
  rematch, instead of requiring an inactive arena director to own its camera.
- Full2 is running the33-scene Windows camera pass. Exact managed SHA:
  25095767039b35799b3b7085a4bc12badd4cfa3c07a1211e852e37c5c0bdda77.
  Its Hill full-ring/HUD check has failed. Do not relabel this as a passed run.
- ROOT has a subsequent, NOT YET COMPILED camera correction: Follow and Aim now
  use the same Cinemachine group center instead of two damped focus authorities;
  Hill adds70% group fill and55% screen-Y via one-time profile version5 migration.
  This requires bind/native/build and fresh Windows acceptance after Full2 ends.
- ROOT/CLEAN bound33scenes, owned compass/profile and source metas have been
  preserved with ExportBroadcastAssets.py. Last accepted canonical build is intact.

### Fishing root cause and next changes

The original movement used Spawn as flight origin although Spawn subtracts the
upstream entry lead; scoring used arrival. The fish visibly reached the net early.
Before-fix FishingCrossingRegressionTests:2/2failed. After aligning the origin to
arrival-travel:4/4Fishing tests passed (including existing10000seed fixture).
Evidence: fishing-before.xml and fishing-after.xml. The initial Windows timing
probe failed setup because its fixture omitted a virtual keyboard; it was stopped
without claiming gameplay. ROOT now includes the corrected explicit keyboard.

Full2's exact executable includes the timing fix and corrected FishingRepairProbe,
but not the later ROOT speed/river changes. Run FishingTimingFix2 with
RunPlaytestWindowsValidation.py --flag=-tsFishingRepair --output-flag=-tsFishingOutput
after Full2. It requires ten real-stream catches, score at visible net passage,
no upstream teleport, actual720/1080captures and a reset.

ROOT changes awaiting the NEXT compilation/authoring:

- FishingDirector configurable2x initial baseline, +.5per30seconds up to6x,
  measured path travel, independently seeded monotonic speed surges/slowdowns.
- FishingStream ChannelCenter(z), half-width target/net travel, shared continuous
  timing. Existing net radius1.1m/scoring/poison/debris/QTE rules retained.
- FishingRepairAuthoring.Upgrade is an explicit versioned saved-scene migration:
  reuse existing terrain layers/water/material/owned bank assets and installed
  JC MegaCity trees; winding240m river, clear pier apron,130% sport-owned fish.
  It never runs on ordinary Play/build and does not touch original fish assets.
- New baseline progression/monotonic-tempo tests appended to Fishing regressions;
  existing schedule fixture now checks relative channel center and revised travel
  bounds. THESE LATER TESTS HAVE NOT RUN YET.

### Other P0 evidence being prepared

HockeyGoalRepairProbe added, no Hockey gameplay changes yet. After Full2 run
--flag=-tsHockeyGoalRepair --output-flag=-tsHockeyOutput to test physical center
shots at2/8/16m/s in both goals, actual scorer/celebration/faceoff, duplicate denial,
outside-net shot and rematch. The current exact executable includes this probe.

Kickball's current HomeRun only sets ForcedTo=4 and waits for physical laps, contrary
to the new immediate-resolution requirement. New KickballHomeRunRegressionTests
are in ROOT, NOT compiled/run yet; run them against unchanged current rules first.
No Kickball gameplay fix is claimed yet. Existing old physical-home-run test must
be updated only when the new authoritative atomic credit/celebration is implemented.

## Later continuation — latest state supersedes provisional notes above

Canonical accepted build/previous commit still unchanged; this repair pass remains
uncommitted. Latest separate Windows candidate has managed SHA
8380fd0ae4c870ad1512a1fcc32901f6e7b82b167552dfa3ccecb0f6a68db729.
Its build source manifest contains62files. It DOES NOT include pending Sky Tow
camera edits, the new camera LegacyPoseAllowed guards, larger human-cue capacity,
or the newer Fishing Hard-only extended-watchdog probe.

### Passed objective evidence

- Fishing native12/12; actual Windows FishingRiverAndTiming343/0, ten real-stream
  net catches and actual720/1080river captures. Scoring/motion alignment confirmed.
- Complete Fishing Easy and Normal four-AI survival matches resolve naturally;
  real model movement measured (Normal peak21.99m/s at165simulated seconds).
  Actual four-camera1280/1920images with naturally spawned fish inspected.
- FishingCompleteMatches365checks/1failure: Hard was still validly alive at255s
  when the fixture's65real-second budget expired. All four had caught132–136lb,
  missed9–13lb, with no pool overflow. This is NOT evidence of a game softlock.
  ROOT probe now permits200real seconds for Hard and supports-tsFishingHardOnly
  with-tsFishingFull; new combined watchdog480sec. Rerun Hard only after build.
- Kickball before-fix native2/2fail; after immediate home-run credit33/33pass.
  Saved-scene runtime KickballHomeRuns16/0: two real physical fair fence clearances
  from explicitly seeded legal-flight states after actual pitch release, immediate
  score, late-tag denial, banner/camera, one next at-bat, reset.720/1080images inspected.
  Four actual Kickball PlayMode tests also pass (NWH/PhysX play, pause and states).
  Waiting camera freedom and AI fast-forward remain unimplemented/unverified.
- Hockey unchanged physical goal rules33/0, then viewport HUD guard39/0 actual
  Windows center shots both goals at2/8/16m/s, outside denial, faceoff/reset and
  screen-safe score/clock/goal labels. Human goal failure not reproduced/closed.
  UI is visible again, but full Hockey presentation overhaul remains open.
- Fare Fight baseline11/0 with one actual physical delivery; logs expose other
  trucks creeping/sliding against buildings for minutes. Native new recovery case
  fails before fix (positive throttle despite no goal progress). After scoped
  actual-position progress recovery20/20native Fare Fight tests pass. Windows
  FareFightRecoveryAfter11/0 gets a real delivery but two trucks still get stuck
  despite many bounded reversals. DO NOT mark AI pursuit complete. Different
  random fares mean before/after times are not a controlled performance comparison.
  Private pending LwsRoutePlanner source has NOT been changed or exported.
- SkyTowBefore24/0 full real four-AI flight, clear weather: hits18,18,18,14/36;
  airborne116.33s, existing replay/rematch. This is baseline before new Sky changes.

### Camera acceptance still open

Full2 and Full3 each687checks/1failure: full Hill ring/HUD clearance. Other32bound
scene paths and retired-AI fixtures pass their checks in those exact candidates.
Native renderer/projection suite9/9pass. Joust screenshot visibly shows actual
tractor at11.1% after excluding its long lance/shield from measurement.

HillBoundedFraming57checks/2failures in the latest candidate: increasing ortho cap
from5to7.5truck lengths makes the ring fully appear, but its lower edge still
overlaps the score row; an additional legacy Update rotation write was found.
ROOT now guards direct legacy ortho/rotation assignments in13explicitly bound
match scripts using SportsEventCamera.LegacyPoseAllowed (Joust already guarded).
ROOT authoring migration7 shifts Hill Cinemachine screenY to.43; cap7.5 and
minimum truck height.07 retained. This needs bind/build and a new full pass.

Also pending in ROOT: CNP cue capacity grows from objective+2humans to enough
for all local humans (up to6in Hockey), skips redundant primary-human cue, and
a four-human far-separation fixture in the Magnet probe verifies all other
humans have real CNP markers. Authoring reuses existing compass/POIs and adds
only missing owned POIs. No hidden opponents or AI-only irrelevant cues added.

### Sky Tow current source work

Audited Match.Present, RallyViews.Follow, racer/joint/cable, Unity Splines course,
ring sweep, existing Aero/rule tests, authoring and actual baseline logs.
Match.Present evaluated each camera twice perframe (generic Follow then its own
spline pose). ROOT removes redundant Follow; SportsEventCamera has development-only
FrameEvaluations counter for direct validation. NOT COMPILED/TESTED YET.
No Sky physics, course or ring changes have been made yet. Ring/boat-turn repairs
remain authorized. Actual baseline shows long stable flight but only14–18hits;
normal and bonus rings currently share4.8m radius. Asset filename audit finds only
UTS Big_Road_Ring (road geometry, unsuitable as an aerial gate); no suitable owned
standalone ring/portal/hoop model was found. Only Weatherade SnowHitFX was found
as a relevant owned snow-hit effect. Inspect its prefab before choosing it.

Potential next work, NOT IMPLEMENTED: true delayed tow-path nominal/ring alignment,
normal gates wider than clearly marked precision bonuses, bounded tangential
cable damping if justified by actual flight, richer owned Arctic gate art/VFX.
Preserve the existing environment, real cable/physics and~120second route.

### Immediate next operations

No Unity/player process remains after session76336. All its receipts are retained.
Next sync/native9tests, bind migration7/CNP capacity, build separate candidate;
run full broadcast acceptance (legacy guards affect13modes), Hard-only Fishing
completion, and targeted existing regression as required. Add/validate Sky camera
counter probe before claiming that fix. Avoid another arbitrary broad historical
audit. Continue active master P0 after cameras; never declare entire backlog done.

Export via Tools/TruckSports/ExportPlaytestRepairAssets.py after successful authoring:
it exports33bound scenes/profile/compass/metas plus exactly8Fishing scene/prefab/
new Terrain/water paths. Last export is already in ROOT. Never copy private
navigation/Taxi/vendor/settings changes or run scene regeneration.


## Continuation: actual candidate b47e7720 Windows results

The sequential owned-player chain completed: Full4 broadcast693/0 across33bound
scenes, actual720p/1080p captures, 1/2/3/4 software-human viewport ownership,
all-human CNP cue coverage, AI-only rematches, Hill complete zone/HUD clearance,
and retired AI viewpoints. Hill and four-human cue captures were inspected.
This is targeted rendered/fixture evidence, not complete human matches in33sports.
FishingHardCompletion350/0: real four-AI Hard survival finished at620.12match
seconds (4x stated acceleration), caught396/389/376/372, misses17/24/20/24,
372906 model-translation samples, no pool overflow; rematch reset passed.
Earlier Easy/Normal full rounds also completed; their shorter Hard watchdog
failure remains recorded. SkyTowSingleCameraOwner6/0: full physical four-tow
flight,17044camera samples, zero duplicate evaluations/outside centers,
peak47.89degrees/real-second,15/17/20/21real ring hits, rematch spectator passed.
Actual Sky screenshots inspected. Human steering/listening/controller acceptance
and dense-scene GPU profiling remain unperformed. Accepted canonical build unchanged.

Fare Fight new road-access adapter native off-road joining test passed.
Actual-district fair-grid test exposed coincident opposing one-way graph edges:
the nearest-road tie discarded the legal reverse direction, making measured
routes loop around blocks. Sport-owned adapter now considers the explicitly
authored counterpart while preserving genuine one-way restrictions. Current
native run farefight-directed-access is testing this; do not claim it passed yet.
Fair-grid integration into runtime is not yet done. Earlier actual AI delivery
checks do not establish reliable pursuit for every participant.


## Fare Fight / Sky Tow integration checkpoint

FareFight directed access and every authored pickup's four fair starts now pass
22/22native tests. Integrated code also22/22. Actual Windows candidate5869909c
completed FareFightFairRoutesFull2 first-to-three match15/0,343.38real seconds,
physical pickups/thefts/deliveries, actual nonoverlapping colliders, four measured
initial road distances250.01m, rematch and menu return. Initial sandboxed attempt
was stopped only after Unity PlayerPrefs writes failed; failure log preserved in
FareFightFairRoutesFull/prefs-failure-progress.txt. Rerun had normal registry access.
The first-to-three fixture's redundant default-five check is now conditional;
this run does NOT establish a full default first-to-five match.

ROOT/CLEAN now contain the later CNP objective-arrow and Taxi pedestrian contact
bridge; these are not in5869909c. One initial authoring call triggered CNP Refresh
against an inactive uninitialized minimap; corrected using editor SerializedObject
configuration, without vendor edits. Authoring then succeeded. Device probe now
checks four isolated actual goal markers and exercises an actual bumper/ragdoll
fixture; captures use the real rendered Windows framebuffer. Not yet run.

Sky saved scene upgraded once: normal8m vs gold4.8m, matching inner clearance,
delayed authored tow-path position/orientation through bends, segmented models
and four pooled Weatherade snow bursts. Camera/cable presentation samples the
interpolated visual transform. Native combined suite56/57: only the new test's
empty batch-scene cleanup failed. Fixed that test cleanup; Sky26/26passed,
with Fare22/22 and broadcast9/9already passing in the combined run. No new Sky
physical flight/ring reachability claim yet. New Windows build currently running
from80scoped source snapshots; accepted canonical executable unchanged.

Targeted export preserved33bound camera scenes plus Fishing's8paths and Sky's
saved scene/six owned mesh assets/metas. No vendor, private navigation, Taxi,
unrelated asset or project-settings export. All current repair work remains
uncommitted pending this coherent validation milestone; preserve baseline927paths.


## Later continuation: candidate97b8abdd, two visual root causes, Minefield

Windows candidate97b8abdd (84source manifest retained) completed:
- SkyTowBoundedGateFX10/0physical flight, normal gates25-26/30; HOWEVER real
  screenshots still showed severe white glare. Numerical passage is not visual acceptance.
- FareFightContactAndViews2:30/0, real four-view framebuffer720/1080captures,
  separate actual CNP goal markers, keyboard/full/shared-controller software inputs,
  disconnect/reconnect/rematch, real existing Taxi pedestrian bumper impact8.1786m/s,
  forward impulse(0,70,327.14), no delivery score. Initial collision fixture was
  flawed: ConfigureTractor intentionally suppresses walking solid contacts and
  installs Taxi-bootstrap-only trigger handling. It is not used by this sport;
  remove that setup from the fixture rather than adding fake collision gameplay.
  Initial screenshot paths were relative (Unity resolved under Data); fixed absolute.
- MinefieldProgressAndRest52/0actual terrain/PhysX: live grounded pre-blast
  starting-line progression, out-of-world last-ground finalization, original
  bounce/slide/backward-slide, unsupported apex, later blast, supported recovery,
  flags, rematch and menu. Actual720/1080pre-blast captures inspected. Current
  candidate's old large text backing is still ugly; later source/scene now adapts
  existing SportsExpansionViewHud to a compact portrait+distance panel (not built yet).

Minefield cumulative root: standings displayed dash until a final landing and
Driving HUD showed distance remaining. Keep final Distance=-1until rest, add
CurrentDistance for live starting-line progress. Record real valid support during
NWH-grounded driving/static supported contact; outside falling locks the last
valid ground, never arbitrary falling Zor zero. Preserve genuine final backward
slides, mine force, vehicle controls and driving finish. Old outside teleport
behind last blast is superseded by the new directive. Driver failure retains
valid grounded progress. Native initial fixture failed as expected; subsequent
inactive Rigidbody COM made the new fixture read0. Corrected active native body
fixture now1/1passed. No additional mine power/terrain tuning done.

Sky glare actual root isolated in native PlayMode: LDRCamera.Render obscured it;
HDR ARGBHalf reproduced the real Windows artifact. Turning off all particles
DID NOT remove glare. Turning off gate models did. New gate-normal test failed:
chevron double-sided triangles shared vertices, canceling normals and causing
invalid HDR lighting/bloom. Targeted owned meshes now use distinct front/back/
side vertices. Existing mesh GUIDs preserved, course migration3. Sky26/26native
passed, HDR visual fixture1/1passed and image inspected: glare/black triangle
artifacts gone. Snow bursts remain bounded non-emissive chips; snow was not the
root cause. No vendor shader or global post-processing changes. Native LDR images
archived in SkyTowNativeVisualDiagnosisLDR; bad Windows frames remain in both
SkyTowGateRepair and SkyTowBoundedGateFX. HDR fixed captures currently in
SkyTowNativeVisualDiagnosis. New runtime -tsSkyTowVisualDiagnosis can capture
real Windows frozen isolation if needed; not yet in a Windows executable.

Fare dashboard obstruction actual root: Taxi's private hidden seated-avatar layer29
aliases the fourth Sports cockpit layer29. Exact native regression fails on
avatar Body in local driving view3 (mask536870912). FareFightDriver now puts only
that hidden avatar on scene-local layer25and excludes it from these sport cameras,
keeping all four cockpit layers untouched. This matches the intended hidden-driver
presentation and does not modify Taxi source/character appearance. Fare authoring
migration2hides the duplicate metric CNP distance; existing FT/MIHUD remains.
Native fare-sky-mine-playmode5/5passed: layer regression, Fare full NWH propulsion/
brake/reverse,1-4human/AIownership in Fare andSky, Mine flags/weather integration.
No physical devices or human acceptance claimed. NEWLAYER FIX NOT YET IN WINDOWS.

ROOT/CLEAN now90scoped sources (including shared HUD adaptation, native tests and
explicit Minefield HUD authoring). Current sequential session24369is authoring
Minefield UI then running final-camera-mine-gate-editmode. Check its actual process
before another Editor/player. No new Trailer Plug code has been written.

TrailerPlugBefore Windows15/0,12actual releases/3ped impacts/12settled drops,
maxCable56.05, rematch/menu. Capture inspected: tiny3m tractor against distorted
6m-wide16m trailer, small hole; genuine user scale bug confirmed visually.
Audited NWH TrailerHitchModule.AttachTrailer/DetachTrailer and wrappers' Awake
(which initialize even disabled VehicleControllers), LWS coupling adapter, original
SemiTruck/SemiTrailer prefabs, existing Rig/Match/rules/replay/floor authoring.
Current sport uses custom ConfigurableJoint and stretched render-only trailer,
not NWH coupling. Proposed NOTIMPLEMENTED: matched full NWH tractor/trailer pair,
same uniform scale (preserve their native ratio), actual vendor hitch API, delayed
attach after old joint destroy on rematch, retained momentum, resized physical
hole/coverage samples, increased clearance height only as needed. Must preserve
pump controls/scoring/crowd/replay. Current floor inner ellipse(2.4,6), tractor3m,
trailer(6,4.6,16), bungee anchorY65/rest48/k180/damper12, masses12/9.
Do not make arbitrary one-object scaling or fake detach; measure native model
bounds/hitch anchors. No implementation/acceptance claim yet.

Still uncommitted: this entire repair pass. Canonical accepted build untouched.
Next finish current native, export explicitly owned assets, build next candidate,
verify Sky HDR in Windows, Fare fourth cab visual, Mine compact HUD; then preserve
coherent scoped milestone with commit/push before continuing Trailer/remainingHPR.
Do not stop at this checkpoint or claim the100-item directive is complete.


## Final c8 Windows milestone — October21

The final90-source candidate managed SHA is
`c8b0b9c1aed92af5778f7bb0be10fbb3950be7b75d5a6dfa8e9be8fb62c64890`.
All four sequential Windows runs exited0 with unchanged managed assembly:
FinalSkyGateNormals10/0, FinalFareCabViews30/0, FinalMineProgressHUD52/0,
FinalCamera33 693/0. Their process.json/result.txt receipts are under
Logs/TruckSports/PlaytestRepair. Actual720/1080frames were visually inspected:
Sky gates have no former HDR glare/black triangles, fourth Fare cockpit is clear,
and Minefield has the compact portrait+distance panel. Native final-camera-mine-
gate EditMode11/11 and fare-sky-mine PlayMode5/5passed. Earlier failure records
above remain historical, superseded only for those exact repaired conditions.

No human, physical-controller or listening acceptance is claimed. The33-scene
camera suite is not35complete gameplay matches. Hockey's reported legal-goal
bug remains unreproduced; Minefield mountains and remaining HPR scope remain open.
FareDefaultFive is running separately to extend the prior complete first-to-three
AI result; do not assume its outcome. Trailer Plug remains the next P0repair.

Reviewed staging scope:250allowlisted paths,190changed files before this final
document update; exact90source hashes match the tested candidate.925unrelated
pre-existing pending paths remain outside scope, plus two already-captured current
request documents intentionally included. Private navigation source excluded.
