# October21 active repair pass — continuation checkpoint

## Current state — October21/22 continuation

Accepted Windows executable: Builds/TruckSports/TruckSports.exe; Dock pool/life payloadc6fcbca8381b44bbd3e89b3d2333b505c3feffd872f3100273addad49f845e33; native6/0 and Windows45/0. Prior Dance-Off/Derby accepted payloads retained; other current HPR/GBC work remains open.

Parking numbered bays5197c076,Hockey/Parking36f50402 and Snake/Wife5c0a7c03 deliveries remain pushed to both authorized branches with their own exact build identities and retained failed candidates.925unrelated pending files/private planner remain protected.

Open: Fare intermittent immobility remains unreproduced in latest recorded-seed matches;engine zero-RPM explanation unproven. Hockey human goal miss remains unreproduced. Other active HPR/GBC requirements remain open. Human/controller/listening and isolated profiling acceptance not claimed. No whole-backlog completion claim.

The dated notes below are execution history; the permanent acceptance ledger remains the requirements authority.


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


## Preserved milestone and next repair

Commit72f68e8aad23850335a9f9b7d5458c1c4315964e pushed successfully to
main and codex/truck-sports-seven-events. Windows c8 candidate promoted to
Builds/TruckSports/TruckSports.exe after verifying all544companion files. Previous
accepted build preserved at.sports-validation/Recovery/AcceptedBuildBeforeCamera-
20261021-152858. Exact receipt:canonical-build-promotion.json.
FareDefaultFive15/0 completed in545.41real seconds: five real deliveries, rematch
and menu return. One AI remained stuck for a prolonged period; HPR-8-4reopened
asPARTIAL rather than hiding that defect behind match completion.

Trailer baseline dimensions measured natively: matched source tractor4.8516m
long / trailer11.6578m, original sport tractor3m / trailer16mwith mismatched
axis scales. Both native models now share uniform factor2.801609; physics roots
and wheel controllers stay unit scale. Targeted scene migrationv2retains the
quarry, shared crowd, scoring and pumping, uses NWH public AttachTrailer/
DetachTrailer and its actual kingpin, keeps velocity/angular momentum, and
resizes the floor/liquid/navigation to openingradii(2.55,11.10). All1019scene
dependencies are already tracked; no new vendor payload needed. Native first
integration4/4failed due wheel-controller local scaling and stale serialized
snow wheel references after replacement; corrected in owned authoring and
rerunning. No assertion suppression/vendor edits. New native test also verifies
three repeated countdowns/attachments/releases. A real centered/off-center drop
fixture now accounts for the original native trailer's front-biased pivot, rather
than mistakenly placing its kingpin over the hole. This is a geometric fixture,
not evidence of a naturally earned human/AI delivery.


Trailer final native PlayMode5/5and EditMode16/16passed. The remaining repeat
fixture failure was stale interpolation from the previous delivery: cleared only
while the entire countdown assembly is kinematic, restored atGO; no active flight
velocity reset. Actual vendor anchors align, three repeat starts/releases retain
linear/angular momentum, centered real collider drop seals once and off-center
drop cannot seal. Camera test measures the larger assembly's rendered projection.

Candidate managedSHA49384f873d2384090f1f6e73ae1eee0b4c9a46e352dc398af10355d980ed4306
built60scenes, zeroerrors/69warnings; native builder206.61seconds. Exact97-source
snapshot retained. Actual720/1080Windows images inspected: matched truck/trailer
and enlarged true hole visible, no obvious missing materials. TrailerMatchedAI
is still running; impacts and partial plugs observed but completion not yet
claimed. Canonical build remains verifiedc8until this candidate passes acceptance.


## Trailer Plug Windows acceptance

Exact49384f873d23candidate completed TrailerMatchedAI17/0in146.70seconds:
12releases,35physical impacts,12settled,13605hitch samples, max0.02414798m
separation and62.07059mmax cable. TrailerMatchedMixed20/0in135.44seconds:
12releases,3impacts,12settled,11456hitch samples, max0.02414394mseparation;
software keyboard controls, recorded replay angle/speed/rewind/skip and fresh
turn input exercised. Both rematch and scene/menu cleanup passed; no extra
listeners, exceptions, or altered managed assembly.720p/1080p actual images
visually inspected. HPR-7-1through7-4are objectively VERIFIED COMPLETE, with
human feel/listening/physical-controller fields explicitly unverified.
Canonical promotion is preserving the precedingc8build and copying/verifying all
companion files. Follow-up scope is18owned source/scene/test/doc/tool paths,
zero new vendor files; all97source hashes and exported assets match the candidate.
Next implementation: Field Goal broken post-kick audio, holder alignment and
confirmed-goal celebration (HPR-1-1through1-4); source/vendor audit begun only.
Fare's prolonged single-AI recovery failure and Hockey's unreproduced legal-goal
report remain open. Do not mark the rest of the HPR/GBC directive complete.


## Next Field Goal checkpoint

Trailer repair committed/pushed as4e6db4a0to both existing branches; exact49384
canonical build promoted with all544companion files verified, precedingc8build
preserved atRecovery/AcceptedBuildBeforeCamera-20261021-155923.

Field Goal source/scene inspection identified incorrect holder clone centering:
old root(3.15,-9.5665,-.15), sideways270degreeheading and imported mesh local
offset countered only inY. Native before tests0/2failed (sideways facing; missing
confirmed-goal presentation). Explicit owned scene upgrade now measures actual
model bounds, recenters the render model, places itbesidetheball(1.75,.04,-.45),
facesuprightsandretainsitsauthoredscale. New DOTween render-only hops/wiggle
start only after rules.Lock confirms a good goal, reset/kill on new attempts and
setup, no Rigidbody/colliders/scoring authority. Brief confirmed-goal holder shot
preserves active kicking/flight cameras. Native after tests2/2passed.
This Field Goal work is not yet in a Windows executable or committed.

Audio remains uncorrected/unreproduced. Source disproved the latched-engine
hypothesis: FootContact already powers off the kicker. Scene audit maps the
existing looping wind/engine sources and feedback assets; current miss crowd
is synthesized tonal boo, and good goals call both crowd response and shared
feedback cheer. Do not declare either the exact audible root without playback
evidence. New explicit dev-only FieldGoalAudioPlaybackProbe records three actual
AI bumper kicks, source start timeline and bounded game-listener PCM (no microphone
or system audio), plus actual goal-holder frames if naturally scored. Next build
should run this baseline before changing audio. Canonical49384remains accepted.


Field Goal focused preservation native5/5passed: new holder2, existing field/end-zone
all distances, crowd and referee event guards. First Windows audio diagnostic
FieldAudioBefore on892515ea9ff3failed6checks/12errors before any kick because
I placed the recording filter on the shared Authority object, which also contains
multiple AudioSources. This is a diagnostic defect, not the reported game sound.
Corrected capture owns an isolated child listener at exactly the original pose;
it disables/restores the original and never reads microphone/system audio. New
native listener-isolation/cleanup regression1/1passed. No gameplay audio changed.
A full-solo diagnostic option now covers five real distances, rematch and menu
cleanup while recording a bounded30-second PCM prefix and the source timeline.
Next Windows run:FieldAudioBaseline2with-tsFieldAudio-tsFieldAudioFull.
Current build session25256ownsEditor57652; do not open another Editor/player or
writeCLEANassetsuntilitexits. Candidate output may be changing; canonical49384
remains accepted. Native/source/scene changes are uncommitted Field Goal scope.


FieldAudioBaseline2on4db5b35a completed actual Hard AI solo five-distance game
9/0, five naturally earned goals, rematch/menu. Non-silent game DSP recorded
1440000stereo frames at48kHz, peak0.1317098and0clipped samples. Timeline confirms
exact double Exceptional cheer after each goal: crowd1.06pitch and pooled
feedback1.00pitch. Engine alreadystopsafterkick; one kick cue, no repeated
kick or output clipping reproduced. This identifies duplicate result ownership
and a plausible warbling source, not subjective human listening acceptance.
Actual720/1080holdercelebration images inspected; black steadying arm looked
awkward after the ball left, so it now hides after launch and restores on setup.

Audio ownership regression0/1failed before. Corrected native bundle7/7passed:
existing crowd owns result sound once (legacy fallback if absent), dedicated
existing tracked NWH SuspensionThump atslot7replaces the billiard kick click;
post clangs have120msreal-timecooldown and late locked-result contacts are silent.
Original vendor clip untouched. No invented crowd audio or changed football
physics. Explicit versioned audio authoring preserves customized clip arrays.
Probe now supports Easy/full modes and bounded90secondlistenercaptureincluding
finalwhistle. No listening claim. Current corrected candidate building in owned
Editor53952/session73149. Next run FieldAudioAfterHard/Easy; canonical49384
remains accepted until corrected Windows acceptance, scoped commit/push pending.


## Corrected Field Goal Windows acceptance

Exact348e00165f235bb7fd2791552b2ff79f83321cfccbca49fbcb4ac9b8607fbe1f
compiled102scoped sources and60scenes. FieldAudioAfterHard10/0in71.55seconds:
5actual kicks,5goals. FieldAudioAfterEasy10/0in67.68seconds:5actualkicks,2goals/
3misses. Both completed normal five-distance competitions, rematch and menu.
Actual game-listener PCM peaks0.1636124/0.1635208,0clipped samples. After each
goal only existing crowd source starts; before there was also a second pitched
feedback cheer. Kick engine stops correctly, one thump per actual launch.
Actual720/1080holder cutaway images inspected with steadying arm hidden.
Objective HPR-1-2/1-3verified;1-1remains IMPLEMENTED/UNVERIFIED for subjective
human listening. Severe-weather/controller preservation not newly revalidated.
Native corrected bundle7/7passed. No human/listening/device acceptance invented.

Source and scene changes ready for scoped preservation. Canonical promotion
348eis copying/verifying all companion files and retaining prior49384. Next
implementation is the real prolonged Fare Fight recovery failure (HPR-8-4),
not a re-audit of the entire backlog. Still inspect actual NWH engine/damage/
gear/wheel state before editing; transient reverse timing or damage are hypotheses,
not confirmed causes. Hockey goal report remains unreproduced/open.


## Field Goal preservation and Fare Fight damage reproduction

Field Goal accepted build348e00165f235bb7fd2791552b2ff79f83321cfccbca49fbcb4ac9b8607fbe1f
was promoted with all545companion files hash-verified; prior49384retained in Recovery.
Scoped commit1afcb77e pushed successfully to both main and
codex/truck-sports-seven-events. All925unrelated pending paths preserved.

Fare damage tests before repair failed0/2: both rematch and the existing announced
DestroyTruck/recovery left actual NWH engine Damage=1. A120real-second native
four-Normal-AI first-to-ten diagnostic (roughly480simseconds) did NOT reproduce
the original prolonged immobilization. It recorded actual engine/transmission
damage, gears, torque and wheel contacts; this is diagnostic evidence, not a
complete-match pass or proof of the original AI stall's cause.

Fare-only lifecycle repair now calls native DamageHandler.Repair at new-match
and announced recovery boundaries. Actual fully destroyed engine/transmission
uses the existing two-second wreck/passenger-reset path equally for humans
and AI. No active-match silent healing, free deliveries, route-planner changes,
vendor edits or reverse-timing retune. First corrected original tests2/2passed.
Additional carrier-reset tests initially passed AI but failed human fixture
initialization (headless Input System keyboard was disabled by focus policy).
These failures are retained; fixture now follows the established Goddess
IgnoreFocus/device-isolation pattern. Full corrected bundle and exact Windows
match acceptance still pending. HPR-8-4 remains PARTIAL.


Fare corrected damage bundle4/4passed after using the existing Goddess
IgnoreFocus/physical-input-isolation fixture policy. Rules preservation22/22
passed. Candidate2f062f156c0f2657e4ec8c03ccd3eae48610e16ad7cbac11d22e2b7d76b92c31
is currently running actual Windows default first-to-five, with per-slot engine,
transmission, gear and60-second no-displacement telemetry. Canonical348e stays
accepted until this candidate is verified; no new completion claim yet.

Keepaway HPR-19-1/2/3 implementation is now underway in ROOT only, outside
the currently running candidate. Existing32x18rounded-footprint constants and
fixed33x19navigation grid proved the physical expansion was missing. Existing
static crates/ramps/sweepers and loose tires have no destruction adapter.
Planned explicit versioned migration retains both authored scenes and models,
scales horizontal geometry bysqrt(3), retains count for1/3density, expands
navigation/spawns and uses existing Core-Collapse/PlowBreakable lifecycle with
vendor EzySlice offline cuts of actual prop meshes. No auto-build migration
or scene regeneration. These changes are uncompiled/unverified until the
current Windows player finishes; never sync CLEAN during its exclusive turn.


## Fare full-match failure and isolated backing evidence

FareDamageDefaultFive on exact2f062f156c0f2657e4ec8c03ccd3eae48610e16ad7cbac11d22e2b7d76b92c31
completed a genuine first-to-five match, rematch/menu in700.38real seconds,
15/16checks passed. The new recovery gate FAILED: slot0 stayed within2m for
60.20691simseconds (other maxima17.03662/9.088867/6.966797). Match seed1965199365,
two actual announced recoveries, all competitors earned deliveries. Failure
retained; candidate not promoted, canonical348e unchanged. Actual healthy
engine/transmission telemetry distinguishes the remaining stall from destroyed
powertrain health.

New native moving-carrier/wall regression0/1failed: recovery steering flipped
from-1to+1while the same reverse maneuver was active. Fix locks its chosen
steering until the bounded maneuver ends. Additional real full-NWH static-wall
fixture (no scoring/teleport/heal/artificial forces) measured only2.426857m
backward clearance before the first forward retry under the old1.4s timer;
0/1behavior failure retained. First invocation had a test-only Component.enabled
compile typo, corrected to Behaviour; not a gameplay failure.

Fare now opts into3mminimum backing distance, with the original1.4sminimum
time and a hard4smaximum. Actual backward travel, rather than a presumed
instant automatic DNR shift, governs early completion. Other users default
to0minimum-distance and retain original behavior; no Taxi/vendor changes.
Corrected five-test native health/possession/physical-clearance bundle running.
A fresh corrected Windows acceptance remains required; HPR-8-4still PARTIAL.


## Corrected native acceptance and saved Keepaway migration

Fare native health/possession/real-NWH-clearance5/5passed; actual backing before
forward retry3.051567m. Fare EditMode preservation23/23passed, including the
previously failing moving-goal steering lock. Corrected Windows full-match
acceptance remains pending; no promotion of failed2f candidate.

Keepaway explicit migration first hit an editor-only Unity fake-null/??
Rigidbody lookup failure, corrected with the explicit Unity object-null check;
no scene was saved by that failed invocation. Corrected migration succeeded
in both existing scenes: actual projected surface area2268.726to6805.251,
ratio2.999591,22potential props retained, density ratio0.3333788. Ground mesh
and collider match; boards, spawns, navigation, prop distribution and perimeter
dressing expanded horizontally; shared camera profile/rules/UI preserved.
85owned generated meshes total16943374bytes. EzySlice tool sources copied
unchanged into private validation project for authoring only, with checksum
manifest; none added to public Git scope. Optional reflected authoring dependency
keeps pre-cut scene/build compilation independent of that licensed tool source.

Targeted Keepaway PlayMode3/3passed: both saved physical areas/connected starts/
all22actual-model fracture references; genuine truck collision breaks obstacle
and rematch restores it; existing shared Core-Collapse/Plowing adapter still
breaks/resets in both original Plowing scenes. One shared project-owned guard
avoids writing velocity to kinematic intact bodies; no vendor edits.

Exported both saved scenes,85mesh assets+metas, and five new source metas to
ROOT. Related review manifest199paths/31194680bytes excludes every pending
vendor source and all925unrelated host paths (hashes verified unchanged).
Corrected60-scene candidate building with114scoped sources. Next Windows runs:
expanded Keepaway full matches/rematches and rendered720p/1080p; focused two-
scene broadcast/split ownership; corrected Fare default first-to-five with
healthy-stall gate. Canonical348e remains accepted until those pass.


## Expanded Windows matches and newly reproduced paused-indicator defect

Candidateff39c3e39914b6ef37ffea3c813d127900d94a90b028d4f523b9eb39606f53df
compiled60scenes/114scoped sources. KeepawayExpandedMatches25/0in56.85s: two
complete four-AI first-to-thirty matches, natural pickups/steals/explosions and
actual obstacle breaks, restored rematches and menu cleanup. Actual720p/1080p
Arena/cab-AI-broadcast images inspected; existing modeled props and close
readable trucks render, sparse terrain/nav expansion integrated. No human claim.

KeepawayExpandedCamera54checks/1failure onff39retained. Primary readable11%,
but the expanded one-human spawn put the ball beyond the configured objective
range. The old fixture unconditionally required the ball onscreen; that ignores
the requested readability/offscreen-indicator policy. Inspection also found a
REAL independent indicator defect: after toggling framing at Time.timeScale=0,
CNP created its widget at zero scale and never animated it, despite the POI
being registered and its opacity nonzero. Native paused-widget regression0/1
failed at scale0. This reopens GBC-05 indicator acceptance; marker registration
alone was insufficient prior evidence.

Owned SportsBroadcastCompass now uses DOTween SetUpdate(true) only to finish
new-widget scale entrances while simulation is frozen; CNP still owns placement,
visibility, group membership and lifetime. Tweens link to widget disable and
are killed by this adapter's ID on resume. Vendor source unchanged. Native
final DOTween widget test1/1passed; broadcast projection preservation9/9passed.
Readability probe now checks actual widget scale, opacity, arrow, screen bounds
and objective association for a legitimately distant Keepaway ball; nearby
objectives still must stay onscreen. Three distant-human cues also require
actual visible widgets, not only active markers.

New115-source/60-scene candidate1f9ac32f4251ecc8fa2f505f6644a307f3c5fbf0ade2f8ed53321e60b449daa4
built in210.58s. Current sequential Windows acceptance: ExpandedCamera33,
existing Keepaway mixed-input/default60s/rain-snow/nameplate/regression probe,
then corrected Fare default first-to-five healthy-stall gate. Tools runner
explicitly supports the existing Keepaway ERRORS=N. footer, counting its actual
PASS/FAIL lines and retaining fresh-file/process/hash checks; strict default
CHECKS parsing unchanged. These runs are pending; canonical348e stays accepted.


## Source preservation and all-human cue binding correction

Scoped implementation commit78b0ff5a0f3ced0e84710b8a4be0f65f6fe253a6 (204paths)
was pushed and both main and codex/truck-sports-seven-events remote hashes verified.
No925unrelated path or raw EzySlice source was staged. Canonical348e unchanged.

ExpandedCamera33 on1f9completed693checks in404.75s,692passed/1failed. Stronger
actual-widget check exposed missing third distant-human cue; other camera
checks, including the expanded Keepaway far-ball arrow, passed. Sequential
script stopped on this failure, so Legacy Keepaway/Fare were NOT run.

Native four-human diagnostic0/1failed: third marker active but group4state
had no widget and viewport0. Saved Magnet scene proves guide targets7 while
compass prefab override permittedTargets.Array.size=3. Owned Guidance now
binds its compass allowlist to the actual authoritative targets array and
uses CNP's public POIRegister only when an active legitimate target is absent.
Authoring records prefab property modifications, including its existing-
capacity early-return path; no destructive scene regeneration or vendor edit.

First corrected native diagnostic still failed for a separate fixture mistake:
one diagonal world-distant human projected INSIDE the perspective frustum
(viewport0.52,0.87), where hiding an offscreen arrow is correct. The revised
fixture places all three remote humans behind the actual camera, explicitly
asserts out-of-frustum positions, tests reference equality of the complete
allowlist and requires actual visible/screen-safe vendor widgets. This does
not weaken the offscreen requirement or add unnecessary onscreen labels.
Final native bundle2/2passed; all three arrows have actual visible widgets,
opacity0.9, correct positions and screen bounds. Paused entrance still passes.

115-source corrected candidate rebuilding. Next run independent Keepaway
default60s/mixed-input regression and Fare full default match, then final
33-scene actual-widget acceptance; retain failures without skipping unrelated
validation. Do not promote the1f9candidate whose camera gate failed.


Future HPR-20-1 work prepared in ROOT only, outside the current115-source
candidate: TerrierCourse editable windForceMultiplier default0.70, used only
in TerrierRacer's existing wind AddForce. Existing0.15ground/0.55air response,
NWH propulsion, jump6.2, air-control, course geometry and shared weather
visuals remain unchanged. Controlled real-body wind-response test authored,
not yet executed. Course/Racer/test are NOT included in the pending camera/
Keepaway/Fare candidate. Verify this feature in its next dedicated build.


Current ad7abc22c9af30a0604b54a2de47efd1438e5d51838ee89d068fa9b49563f077
Windows sequence: KeepawayExpandedDefault60 completed109checks/1failure
in185.34s. BOTH full default60s natural-hazard games passed: scores
39.24071/60/45.42085/40.16073 and38.08068/53.36103/60/36.38064. Mixed input,
pause,3.54707m actual bounded recovery, cab/nameplate checks and menu passed.
One isolated actor3-to-thief0 test ended with thief owning the ball but zero
steals, indicating a loose-ball pickup rather than the intended controlled
theft. The fixture left explosive sheep active. Preserve this failed gate;
do not assume an AI/scoring defect from it. ROOT test-only follow-up makes
sheep dormant in the isolated collision cases, logs carrier/pickup/steal/blast
state, and selects actual registry Clear/Rain/Snow profiles instead of relying
on obsolete weatherChoice integers. Natural full-match hazard tests stay active.
That follow-up is NOT yet compiled/in this ad7candidate; rerun in next build.
Fare full default match is running independently; final33camera pass follows.


October21 continued acceptance: ad7 candidate final33 camera run completed
697 checks/7 failures, all in the deliberately displaced four-human Magnet
fixture: its own frustum assertions failed. Actual saved-scene readability,
all other camera checks passed. Screenshot shows an empty gray field during
paused countdown; investigate NWH interpolated Rigidbody-only fixture teleports
before changing production framing. ROOT fixture now writes physics AND render
transform together and records both poses, camera, lead and viewport.

FareRecoveryDefaultFive ad7 completed first-to-five, rematch and menu in402.62s,
16 checks/1 failure: one healthy truck remained in a2m pocket114.0912 simulation
seconds. Ten announced recoveries occurred. Not accepted/promoted. Source also
proved unsafe nearest-node recovery: actual native fare-safe-road-before0/1
failed because recovered chassis penetrated another full NWH competitor. New
mode-owned placement samples existing truck-allowed road lanes and heading,
checks complete authored compound bounds against solid occupants, excludes
pickup/delivery triggers, and retries if blocked. fare-safe-road-after1/1 passed.
A separate native chassis-high-centering fixture is pending; do not yet claim
that this placement fix resolves all long physical stalls.

Terrier HPR20.1 actual native wind-tunnel test passed1/1: same NWH body and
actual Racer.Step produced old response0.11m/s,0.70 response0.07699997m/s,
disabled0. Existing authoritative weather Flow remained unchanged. This is
force response verification, not full race/weather visual acceptance.


Integrated native follow-up fare-safe-recovery-combined-native10/10 passed:
occupied-node exclusion, authored-road heading, all-roads-blocked retry with
no fallback teleport, existing chassis-support-to-wreck recovery, actual NWH
backing clearance, human/AI engine damage recovery, rematch repair, paused
objective and three-human CNP widgets, Terrier force response. The chassis
fixture recovered through existing physical damage/rollover handling; it did
NOT reproduce the long healthy-truck stall and justifies no new high-centering
rule. Fare probe now records rotation, actual grounded wheel count and nearby
solid collider names/heights after10s without displacement. Continue actual
full match diagnosis. fare-safe-recovery-edit23/23 passed. An earlier wrong
namespace filter ran0tests and was rejected by the wrapper; corrected actual
class filters ran23. No zero-test acceptance claimed.

All925 unrelated pending file hashes remain identical to the host baseline.
The isolated Editor is sequentially saving corrected CNP allowlist references
before the next Windows candidate. Candidate acceptance remains open.


958751b6e0f5d80bd2d6c41fd73796bb79f8a043c3c3ffd4f491dc19f535085a
Windows targeted CNP run67checks/7failures (47.89s) and Fare first-to-five
16checks/1failure (435.53s) retained. Fare seed616736539: slot1 healthy,
4grounded wheels, no neighboring solid collider, longest pocket508.6762simsec;
other slots7.887695/6.746582/6.616364,8announced recoveries. First-to-five,
rematch and menu succeeded. Safe placement alone is not full AI acceptance.
Warm and cold actual NWH turning fixtures at the observed pose passed;
therefore no guessed new steering/physics tuning was justified. Probe now
adds actual input, powertrain/wheel torque/brake, RPM, sleep and constraints.

Actual camera cause isolated in native reproduction including legacy camera
comparison, distant-AI teleport, target retirement, then repeated split/shared
switches. Camera-owned primary bounds became~359m across, despite a normal
6m truck. Logs identify enabled ZERO-SCALE pooled wreck meshes at old scene
positions around(-301,-8,-301), with zero-size render bounds. These invisible
points inflated both group bounds and sampled corner projections. Production
SportsBroadcastFraming now excludes meshes with effectively zero world scale
or zero-size bounds from BOTH measurements. Existing Cinemachine composition,
profile settings, CNP presentation and gameplay remain unchanged. Before native
camera test failed; camera-hidden-mesh-fix-and-hud4/4passed afterwards. Actual
compact Terrier HUD also passed1/1 covering1/2/3/4software-human views and
1human+3AI views, ownership, one score group, no overlapping viewport bounds,
score update and repeated rematches at native640x480. Cold slot1 turn1/1
passed separately. Windows resolution/render validation remains pending.

Future ROOT TerrierGoddess views probe now adds actual player-window screenshots
and geometric/ownership checks at1280x720 and1920x1080 for all those layouts;
not yet executed. It distinguishes Windows from controlled Editor captures.
All changes after958751b6remain source-only pending next build.


Current Windows candidate7810634d310fabaf7a0dad3e494c50f3bd891da6e0760a592a62a789bfb169f2
built60scenes from125exact scoped sources. CnpHiddenWreckTarget67/0,49.64s:
actual arena restored, primary truck readable, all3offscreen human widgets
visible after repeated switches. Screenshot manually inspected. Further visual
finding: bottom-edge arrows overlap the compact score row; future CNP authoring
migration raises only the old stock margin0.075to0.17, preserving deliberate
designer overrides. Actual arrow-vs-scoreboard clearance regression prepared,
not yet run/migrated/included in this candidate.

FareRecordedSeedPowertrain13/0,51.81s: real pickup/delivery/rematch/menu with
recorded seed616736539; observed pockets2.4/2.4/2.5/6.1simsec in this SHORT
run. Not long-match acceptance. A preceding native120real-second same-seed
instrumented diagnostic did NOT reproduce prolonged stall; do not call the
remaining full-match AI requirement complete from that diagnostic.

KeepawayExpandedDefaultFinal111/0,154.92s: controlled fixture sheep isolation
resolved its contaminated theft test, actual default60s games in registry
Rain/Snow, all existing regression checks and rematches/menu passed. The three
specific area/density/destructibility requirements now have automated verified
status, with human verification still pending.

TerrierWindCompactViews retained as FAILED/incomplete: some actual window HUD
bounds/ownership checks passed for1/2/3humans at720p/1080p, but relative
ScreenCapture paths were interpreted under player_Data and failed; then normal
focus/resolution pause stalled its scaled wait. Owned watchdog ended at300.18s.
Source-only harness follow-up uses absolute evidence paths, realtime waits and
acknowledges focus pauses through the existing Resume flow only during isolated
software-device layouts. Real gameplay focus/reconnect behavior unchanged.

ExpandedCameraHiddenWreck33 retained as FAILED/incomplete: software-human starts
failed after scene switches, then the harness incorrectly attempted to measure
an unstarted Hockey match, causing Primary=-1 bounds access. Verified owned
PID51652 was stopped at477.95s; unrelated user Build20 PID58240 was preserved.
Root-only harness follow-up sets InputSystem IgnoreFocus, enables its OWN
synthetic devices at setup, records rejection reasons and guards failed starts.
No production participant validation was weakened. Current sequence proceeds
to the independent actual twelve-truck Terrier match.

Future ROOT work outside7810634d: BASE Jumping compact score adapter and
explicit Show AI Cameras support over its already-existing4-view selection;
NWH ExhaustSmoke owned prefab-variant authoring/lifecycle for the existing4
pooled flares (authoring NOT run, scene/variant NOT created yet); generic
BASE/Terrier viewport test; Mattress manual-reset physical reproduction tests
(NOT run, automatic reset production behavior NOT changed yet). Preserve these
pending changes; do not report them as complete.


### Compact HUD, flare smoke and manual recovery — current tests

Native compact-hud-manual-reset-flare-cue: 7 passed / 2 failed. Passed BASE four-flare native NWH smoke emission/rematch cleanup; BASE and Terrier compact HUD layouts for 1–4 software humans and optional AI views; duplicate-name-safe four-card Terrier broadcast leaders; paused objective widget; native 0.70 wind response. Mattress AI recovery passed. Human no-auto-reset/body-dynamic assertions passed, but fresh software-key confirmation still failed and is instrumented rather than declared fixed. CNP arrow clearance failed: original existing-capacity authoring returned before invoking the prefab migration. Explicit owned-prefab migration has now run (no scene regeneration); fresh cue/manual-input tests pending.

BASE uses an owned variant of NWH ExhaustSmoke in the existing four-flare/24-object pool. Authoring added four references, not a duplicate pool. World-space native particle force uses the existing wind field. Source-only Windows compact HUD capture probe now covers BASE/Mattress at 720p/1080p; not yet built or run. Mattress compact HUD reads actual running place/time and exposes an assigned-action contextual reset popup. Physical reset fixture now also checks actual popup geometry and assigned control. Full backlog remains open.


Native manual-reset-and-compact-final6/0passed: human keeps inverted dynamic physics while action is held; neutral+fresh assigned keyboard press invokes exactly one original5s recovery, actual popup stays within viewport and displays assigned action; AI recovery unchanged. Test-only keyboard failure isolated to InputSettings.editorInputBehaviorInPlayMode: batch Editor routed keyboard events to editor despite IgnoreFocus. Fixture now temporarily selects AllDeviceInputAlwaysGoesToGameView and restores it. No production input workaround. Actual queued held-key assertion now passes. BASE/Terrier/Mattress each passed1–4human and optionalAI ownership/bounds/rematch tests; Terrier shared four-card leaders keep duplicate display names separate. Running-place test uses physical progress, not series points. CNP actual arrow clearance2/0passed after explicit prefab migration. Windows acceptance still pending.


Windows candidate e3433d137e2a752933dd21291be356ac3ee0aeffca45fd81e0740dfcc7f28102 built60scenes from134 exact scoped sources. CompactHudBaseMattressFinal94/0,50.12s: actual720p/1080p captures for1–4humans and optionalAI views in both modes; actual smoke emission/clear pool, physical human inversion/held-action protection/fresh-pad confirmation, one5s recovery, reset popup geometry, rematch/menu. Manually inspected BASE/Mattress four-player720p and Mattress reset1080p screenshots. TerrierWindCompactViewsFinal59/0,69.92s: real window1–4human/optionalAI cab HUDs at720p/1080p, native takeoff, weather fixtures and menu. Prior failed harness evidence preserved. Full33camera pass currently running on same candidate; Fare long-match acceptance still open.

Future ROOT-only next slice (OUTSIDE e343): Target Launch and Wife compact data adapters over existing owners/rules/pair progress; additive Wife per-viewport CNP checkpoint authoring/source prepared but NOT executed. Additional native ownership tests prepared, not run. No new game rules or physics changes.


Promoted e343 candidate after697/94/59/16passing Windows checks and verified545files. Original accepted348e build preserved in dated recovery folder. Same-seed Fare full match completed actual first-to-five with pockets9.229/7.647/14.232/9.249seconds,7announced recoveries, no recurring zero-power case. HPR8-4 remains open: non-reproduction is not root-cause resolution. Next compile adds engine component/connection/ignition/stall/throttle diagnostics without changing propulsion.


Next source slice compiled/authored successfully. Native compact-five-sports-cnp-checkpoints-native13/0passed: all BASE/Terrier/Mattress/Wife/Target compact layouts, duplicate-name Terrier leaders, actual four independently owned CNP checkpoint widgets, small colored glyph checks, original human/AI recovery and smoke/wind regressions. Wife migration second run leaves saved scene SHA256 identical. Only additive owned references exported;925unrelated pending files rechecked unchanged. Next candidate build in progress, canonical e343 remains available.


Windows205c0bf4 CompactFourSportsCheckpointFinal182/0passed actual layouts/ownership and four Wife native checkpoint widgets; however MANUAL smoke close-up visual acceptance FAILED. BASE-native-smoke-camera-fixture.png shows an entire30-cell atlas on each billboard. Installed NWH ExhaustSmoke prefab disables UV module and retains stale5x5tiles while its actual SmokeTextureSheet has6x5cells (source image inspected). No vendor edit. Owned variant repair now enables native6x5 lifetime flipbook and uses owned material copy with normal multiplicative white base instead of subtractive soot tint. New exact configuration assertions prepared; migration/test/rebuild pending. Do not claim visual acceptance or promote205from automated182checks alone. Current CNP205targeted pass is running independently.


Asset-only smoke rebuild retained gameplay DLL205c0bf4, exposing a validation provenance gap: managed SHA alone cannot identify scene/material payloads. Tools now fingerprint every executable/dependency/asset file, bind fresh receipts to buildContentSha256 and require unchanged full content for promotion. Source archives/manifests now use full-content key; existing conflicting archive keys are rejected rather than silently reused. Current corrected payload13804d9fbaddaf9d30193f31acd9a5aa7917832bb2d2d7c18161311d3a569ab8,545files,139source inputs. Targeted Python regression passed unchanged-code/changed-asset and timestamp-only cases. Full fresh Windows UI/smoke and CNP receipts required; older205code-only receipts cannot approve the revised assets. Native flipbook1/0passed.


Full payload7cc60afd accepted and promoted:182/67/6/18fresh Windows checks, all with unchanged545-file fingerprints. FullTarget Easy/Normal/Hard totals45/135/160/100 in Hard final (all5attempts), actual recorded replay/rematch/pause/menu passed. Wife12finishes, no drops, three-race scores12/9/6/3, rematch and menu passed. Actual smoke screenshot after normal alpha repair shows no square glossy billboard silhouettes. Native renderer migration used public UnityEditor.BaseShaderGUI.SetupMaterialBlendMode; initial incorrect helper namespace compile failure retained in history and corrected. No vendor source modified.


### Continued Wife camera / Snake repair — October22

Wife startup banner root cause reproduced:18blocked passenger sightlines across two starts. Saved camera offset(0,6,-13), look-ahead4, height6 put cameras behind the render-only banner. Explicit stock-only migration to z=-9 keeps Cinemachine, original course and physics. Native after2/0 and combined9/0 passed repeated starts and checkpoint markers; current Windows visual evidence pending.

Snake existing full-payload7cc Windows baseline26/0: actual four-AI crash-resolved match43.3simsec,8contacts, maximum8trailers; does not establish long survival or competent town routing. Natural straight-road baseline10/0:63attached trailers over more than1km, no pickup-position overrides. Exact receipts SnakeExistingPickupBaseline and SnakeLongPickupBaseline. Generic legacy probe footer calls itself native; external receipt identifies actual Windows executable and full content hash. No human/GPU acceptance.

Actual finite-pool edge case reproduced separately: all live uncollected gates far behind a surviving truck prevent fresh district supply. Before native failed; after1/0 and combined9/0 pass. Small fix retires only old gates beyond900m and outside every displayed camera. Existing pool,0.5s refill, physical pickup scoring and convoy physics retained.

Snake compact HUD adds typed per-owner telemetry through existing Heat/TMP/portrait/plate/DOTween component. Four saved CNP instances mark nearest genuinely reachable gate via existing planner, including AI-only/shared spectator. Road queries throttled; native max12.24ms selection group. Route tests23/0 reject off-road/disconnected/collected gates and preserve authored graph. Native combined9/0 passes saved1–4human HUD/CNP ownership and surrounding compact-camera regressions. Windows build/captures remain pending.

Handling: existing TruckTaxiVehicleHandlingOverride public NWH configuration audited. Proposed mode-only tuning is being physically compared before saving; no shared Taxi source changes. First measurement fixture had an InputSystem helper compile error, corrected to actual GamepadState.leftStick; no pass claimed from that failed run.


Snake follow-up: native saved world/rules24/0 validates32trees/12properties, road corridor clearance, terrain bases, four serialized NWH overrides and unchanged48traffic/100pedestrian full-physics budgets. UTS logical targets210pedestrians/59traffic through the existing owned profile. Runtime population/rendering acceptance still pending.

Physical tuning experiments retained:48degree city lock worsened real turns;36degree city lock alone improved one comparison but not the70degree low-speed lock plus turn trim. Final tested mode-only values: low50/city36degrees, response500, smoothing.015, launch4.2/2.8 and human full-turn speed target scale.65 (minimum7m/s preserved). NWH remains sole propulsion/steering authority. Initial passing warm-rematch comparison later failed launch threshold in the combined suite (11pass/1fail), so it was not accepted as final. The controlled comparison now reloads the saved scene and uses an identical seed for each fresh powertrain: cold-start1/0 observed stock3.74s versus tuned3.56s to20MPH, and14.94m versus13.62m for60degree turn, both upright. Full combined rerun currently running; do not claim repeatable acceleration from the earlier single warm-rematch pass.


### Windows rejection and actual traffic root cause — October22

Candidate71575a5cedf8631adec48e73b2f312af5669ff10d9b5065f868545b3d5b4abf5 (manageda6f74da775a6639d49ec358e49f96e942eb6865c7a371d6399b688078b1d5d36) was NOT promoted. WifeSnakeCompactFirstWindows232checks/2errors: Snake two-line trailer/speed text truncated at both720p/1080p. The other UI/ownership checks passed; actual Wife startup views no longer cross the banner. Retained current captured failures. Optional telemetry cards now116rather than102units high, detail48rather than34; native text truncation assertion added. Snake CNP distance labels disabled through public vendor booleans so metric labels do not conflict with feet/miles HUD.

Windows counters also exposed REAL missing traffic: target59 but logical/live0. Saved cityLanes was empty. Root cause confirmed in original SnakeAuthoring.Roads: LwsTrafficLaneBuilder intentionally accepts Interstate and selected Ramp classes, rejecting all ordinary LocalRoad streets. Explicit additive repair restores the actual original Fare/Taxi district's authored closed loops, preserving their positions, prefabs, signals and UTS driving logic; no new traffic system, no shared Taxi/vendor source edits. Creation fallback corrected so the empty result cannot silently replace those loops again. Native snake-traffic-empty-lanes-before fails0logical. After restoration, the next suite observed59logical/58live/50moving/full48.

That combined suite was10pass/3fail: acceleration comparison had equal20MPH times despite higher engine multiplier, and two repeated-start fixtures found starter bays blocked by retained NPC populations. Existing public ResetTraffic/ResetPopulation now run on BeginGame; starter-bay errors additionally include the real blocker object/position instead of hiding it. Current native follow-up is snake-population-reset-and-hockey-edges. Shift duration.35vs.18 is being measured using public NWH configuration; it has NOT yet been saved in the scene or accepted. Engine multiplier alone is not accepted as proof of improved launch performance.

Hockey: original currentWindows center-goal39/0 was insufficient to dismiss the human report. New native actual-PhysX test covers both net signs,2/8/16m/s and center/both98percent clear frame edges, correct team, once-only scoring, celebration and next faceoff. No speculative Hockey scoring change made.

Preservation review: Snake's previous15007serialized documents all remain;739new documents,8existing documents changed (authority references, four NWH prefab overrides, child/root lists). Evidence snake-scene-preservation-review.json. Original925pending unrelated files stillSHA256-identical; accepted canonical7cc build remains intact; both Git branches still11651d08 until this follow-up passes and is delivered.


### Current native acceptance — October22,03:05UTC

Final snake-hockey-final-regressions14/0passed, including complete saved Snake telemetry/no truncation,1–4human/AI CNP ownership, actual UTS traffic motion, repeated population reset/rematch, original compact BASE/Terrier/Mattress/Target/Wife data ownership, Mattress fresh-action recovery, Wife startup sightlines, and18physical Hockey goal passages/faceoffs. Shorter NWH shift.18 now saved only on four Snake instances; final cold stock3.88s versus tuned3.10s to20MPH, upright turns14.90m versus14.73m. Prior failed runs retained. Engine multiplier alone was insufficient.

Traffic rematch problem is corrected using existing public ResetTraffic and ResetPopulation on BeginGame. Actual final traffic59logical/55live/53moving/full48. Starter-bay diagnostics now name any actual blocking collider, without skipping obstruction checks. No phantom points or invulnerability. Source convoy following/jump/contact/scoring authority otherwise unchanged.

Hockey18shots scored once at center/both clear frame edges, both nets,2/8/16m/s. Earlier test timeout was fixture timing (StartMatch resets timeScale1; goal+faceoff4.5s exceeds old4s timeout); corrected fixture reasserts3x and6s deadline. Production Hockey goal logic unchanged; human miss remains unconfirmed/open.

Windows scenery fixture715was visually invalid: aiming at a solid building center caused Cinemachine occlusion to pull the fixture indoors. Kept capture; corrected explicit camera now aims in front of facade, uses PoseRaw with fixture framing temporarily disabled, and asserts camera outside property before capture. This is a labelled scenery camera fixture, not ordinary driving camera proof.

Next: refreshed saved-asset EditMode24check, explicit asset/source-meta export, second candidate build, Windows compact/Hockey/Snake full match/cadence/fifty-trailer-jump checks. Canonical7cc and pushed11651d08 remain the accepted delivery until successful promotion. No entire-backlog completion claim.


### Current Windows240 and AI follow-up

Full candidate240cb032ea6c6b5f482396c0e0a2108c0b69564bb299b11ffa6ef85a419ea68d, managede6e47c99fa3345af9f61694e33f96a253c57076925789ef690b2257091f66a1c. Actual WifeSnakeTrafficCompactWindows234/0, inspected real720p/1080p captures: two-line Snake telemetry fits, no metric CNP labels, actual traffic59logical/56live/full46 and pedestrians210logical/80live, saved32trees/12properties, scenic camera outside facade, Wife banner sightline clear. Candidate NOT promoted: full four-AI match SnakeTrafficFullMatchWindows26checks/1failure; only6pickups/max7trailers, everyone crashed within35.54simsec. Keep original>=8contacts/max6growth acceptance.

Current240 natural road cadence Windows10/0:63attached trailers, more than1km,2744frames/38.18realsec during fixture (~72observedFPS, not isolated GPU/performance certification). Current240 fifty-trailer/jump Windows11/0:58attached, actual lowest truck clearance9.646m and delayed eleventh trailer9.561m,2820frames/37.23sec (~76observedFPS); actual NWH/PhysX/software keyboard, rematch/cleanup. Legacy probe footer incorrectly says not Windows; wrapper receipts/fullhash prove real standalone process. Footer corrected for next build.

AI lane-spacing experiment was NOT accepted: snake-ai-separated-approaches native1failed with4physical pickups. Removed exactly that experiment after matching the240archived source (line-ending normalization required by the preservation guard); no other source work discarded. Next hypothesis: forced7m/s floor and detour9m/s prevented real low-speed evasive steering, with late bend braking. Current ROOT/CLEAN EXPERIMENT: stock-only minimum4.5m/s, real reverse-input speed trim down to positive floor, AI detours5.5m/s and actual-route bend braking up to55m ahead using existing NWH controls. No extra forces, no scoring/collision changes, no stops or automatic pickups. Explicit handling authoring running; next native SnakeCompetitiveFlowTests must pass unchanged>=8contact/max6growth condition before rebuilding. No outcome claimed yet.


### AI objective lifecycle confirmed — latest active continuation

Candidate240remains NOT promoted. Actual Windows234/0compact/traffic/scenery; natural63-trailer10/0 and58-trailer twenty-foot delayed jump11/0passed. FullAI26/1failed6pickups. Lane-spacing experiment then failed4pickups and was fully reverted. AI slower detours/early-braking experiment failed6pickups and was also removed after comparing against the archived240source. Human control improvement retained separately: positive minimum4.5m/s and full reverse-input trim to that positive floor; never reverses/stops. This still needs fresh Windows validation.

Confirmed useful AI fix now in SnakeRouteAI: missed nearby gates behind the travel direction cause retargeting; candidate ranking strongly prefers ahead gates and checks the existing road planner (bounded8attempts); changing objective clears stale avoidance detour state. Original AI speed/avoidance policy restored. No scoring, collision, traffic immunity or convoy-following shortcut. Native snake-ai-forward-objective-lifecycle1/0:55actual pickup contacts,31maximum attached trailers, complete physical match/results and one-trailer rematch. Original>=8contact/max6growth acceptance unchanged. Per-slot position/RouteDiagnostic recorded by SnakeCompetitiveFlowTests.

CURRENT JOB: snake-forward-objective-control-regressions PlayMode, owned Editor PID56132, shell session65041. Expected four tests: SnakeHandlingTests, SnakePresentationTests, SnakePickupSupplyTests, SnakeTrafficTests. Wait for its exit before CLEAN writes or another Editor/player. Current source sync155ownedC#inputs (two more than the built240payload153). New Convoy probe footer correctly identifies Editor versus Windows; older receipts retain their original contradictory boilerplate.

NEXT: if four native tests pass, run saved-scene SnakePathTests24, export only authored assets/metas, build third Windows candidate. Then fresh full AI match, compact720/1080views, natural cadence, fifty-trailer delayed jump, existing Hockey goal probe. Promote only passing full-content-hash-bound payload to Builds/TruckSports/TruckSports.exe; preserve prior canonical7cc via promoter backup. Review/stage exact owned manifest (currently35plus follow-up files), confirm925unrelated SHA values, commit and non-force atomic push HEAD to main and codex/truck-sports-seven-events. Both remotes last verified11651d08; follow-up not committed/pushed yet.

Continue remaining active HPR work after this milestone. Fare intermittent healthy zero-RPM/power stall remains unresolved; Hockey human goal miss has not reproduced despite eighteen native physical center/edge passages and earlier Windows center/outside39checks. Do not declare the full backlog finished. Do not reopen retired historical backlog or alter deferred Tug of War.

## October22 — exact final follow-up delivery

Final payload85d6770a passed320Windows assertions across five sequential owned processes. Earlier rejected715/240payloads and failed steering/AI experiments remain in execution history. The accepted AI change clears stale detours and replans missed behind-the-truck objectives; original AI speed/avoidance policy restored. Human minimum4.5m/s and reverse-input cruise trimming remain positive-forward only. Public NWH shift.18 supplies measured launch improvement. Restored authored UTS loops and public traffic/pedestrian reset APIs eliminate empty city traffic and retained rematch bay obstructions. CNP and Heat/TMP/DOTween retain presentation authority. No vendor or original Taxi source modified. Continue active repairs after this milestone; Hockey HUD is next.

## October22 — Hockey UI continuation (HPR-13-2)

Inspected actual final Hockey Windows rendering: thin raw top bars, oversized mostly-empty roster docks, and BLUE/ORANGE text colored using unrelated first-member crew colors. Fixed through an additive saved Hockey presentation adapter, existing Heat controls, shared fonts/portrait/rounded plate and DOTween. Six crew-color identity cards occupy one bottom row; separate fixed team accents avoid changing global player colors. Existing score, clock and message references are retained; their empty legacy docks are disabled. Setup gains six compact portrait-backed cards with existing editable names/sources/difficulty actions. Pause shows actual score/clock; results show actual score, goal count and both rosters. No gameplay/physics/camera changes.

First sandbox Editor could not reach licensing IPC; only owned validation PID64920 was terminated after timeout. Retried existing Editor with normal desktop IPC; authoring exited0. Targeted native UI, physical goals and neighboring Snake regression tests are running. Windows acceptance still pending. Hockey13-1 human miss remains unreproduced and open.


### Hockey first Windows candidate and Parking continuation

Windows payload13677153 automated Hockey453/0 completed two actual physical six-AI matches, goal accounting, results, rematch and menu return at720p/1080p/ultrawide. Visual review REJECTED promotion: SportsArenaOverlay.LateUpdate resurrected five empty old docks even after their text moved. Saved Hockey overlay is now disabled without changing the shared broadcast camera or other sports. Controls footer restored. Explicit no-active-old-dock tests added. Native combined Hockey UI3tests/physical18shots and Parking icons passed; overall4passed/1failed because neighboring Snake expected an objective after a fixed1.2s sleep. No production CNP change made; final fixture now asserts actual countdown release before freezing and checking objectives. Failure retained; timing hypothesis not yet established as root cause.

Parking HPR24-3/4/5: shared SportsBroadcastScores gains opt-in compact elimination icons rather than a duplicate UI framework. Eight actual truck portraits preserve crew names/colors, gray eliminated competitors and keep red penalty locks/capture progress. One displayed camera uses bottom-row anchors; multiple views share one center group. Large header/footer/repeated viewport blocks hidden; countdown and parking deadline retained in compact phase text. No parking physics/rules/AI/camera changes. Final native and Windows checks pending. Bay presentation24-2 is not claimed complete.

## October22 — Hockey/Parking accepted delivery

Final 6c5b4e08 payload passed all four matching Windows receipts. Hockey old overlay was the actual empty-bar writer; disabled only on the saved Hockey HUD. Small contrast plates corrected bright-ice footer/rink-end readability. Parking uses the existing shared score-card adapter,8actual portraits,gray OUT states,penalty lock/capture progress and bounded compact phase text. No sport rules,AI,physics,cameras or crew colors changed.

Rejected history:136candidate453/0 software assertions but visually resurrected docks; b94Hockey603/0,Parking1033/2 first-frame checks before LateUpdate;656Hockey623/0,Parking1081/85 due unisolated software controllers/focus pause. Frozen Windows fixture now isolates background input and uses the existing Resume API only for its valid synthetic roster. Production focus/pause/reconnect remains unchanged. Snake fixture waits for actual GO instead of a fixed real-time sleep; prior failure retained. Physical-controller or human acceptance is not inferred from these changes.


## October22 — Fare recorded-seed continuation and remaining Parking bays

FareCurrentRecordedSeedNormal passed16/0 on accepted6c5b4e08,551.25real seconds,actual first-to-five Normal AI match with seed616736539. Longest two-metre pockets16.016/11.414/10.581/9.109simsec;4announced recoveries,real pickups/deliveries/traffic,pause,rematch and menu cleanup. No zero-RPM diagnostic samples appeared. Re-reading the old958failed log confirms508.676simsec immobility/zero engine and transmission damage/four grounded wheels, but it contains NO RPM/generated-power fields. Earlier checkpoint descriptions of a confirmed zero-power/zero-RPM cause were too strong. Engine failure remains only a hypothesis. Current source also includes the later full-truck recovery-clearance correction; no new engine patch made from this non-reproduction.

HPR24-2 source implementation underway: eight readable numbered world decals,open/closed/secured glyphs,owner portraits/colors,one shared Heat/TMP canvas,DOTween state touch and owned reflective paint. Original ParkingBay state,footprint,colliders,rules and AI retained. Audited installed JC subset and RoadPropsPack signage; vertical roadside props are not required for these noncolliding floor-state identifiers. Reuse the existing UI/paint foundation. Explicit saved-scene authoring/native fixtures running; no new Windows claim yet.925unrelated files remain protected.


## October22 — Parking numbered bays accepted

Full payload541e5c98 / managed241ba26a,545files/60scenes/166ownedC#inputs promoted only after Windows1093/0. Actual8-AI match7rounds/28captures,winner4,seed95112;20software-human/AI-camera configurations at720p/1080p,world state fixtures,cab view,rematch/menu cleanup. Latest targeted native4/0. Found actual TMP minimum65/max68 in70pxbox truncated the digits to ellipsis; before-fix test failed,known-stock font fitting60/max60/min54 in78pxbox corrected it. Actual Windows images show numbers/states/owner accents. Explicit close-up/state/cab positions are labelled fixtures,not human driving evidence.

All4343original Parking serialized documents retained;403new presentation documents,zero removed.81original documents changed only stock paint width/material,old caption renderer,added component/parent lists.Original physics,cameras,rules and AI unchanged.925unrelated files remain byte-identical. Fare last recorded-seed current16/0 retained; old engine zero-RPM claim corrected as unproven. Current Bungee work starts with owned building/Core-Collapse audit; no Bungee gameplay tuning accepted yet.


## October22 — Bungee owned-building and impact continuation

Audited Core-Collapse CollapserPrefrag,Support,Pushable and EzySlice APIs;existing Bungee support graphs/controls and actual JC city building prefabs/materials. Source city meshes and vendor scripts remain unchanged. Explicit migration creates owned city/hotel prefabs and offline real-model fracture assets; original module counts,budgets,HP,colliders and support links compare equal. First art preview looked stretched;unshipped v1 retained privately while v2 repeats natural-height middle storeys,preserving entry/roof.

Actual collision test found inert replacement rubble: static structural collision consumed the hit,then vendor spawned fragments with zero initial contact momentum. Before fix1.074m/s max speed,0.101m travel,0.268m/s down over1s. New LWS contact-scoped handoff uses35percent incoming velocity,capped12m/s,12m proximity,shared150N-s/half-source-momentum cap and bounded lever arm. Core-Collapse still owns damage/support/fracture. No extra impulse from support-only/finale calls;reset clears context. Current native2/0 plus controls/reset2/0: actual16.425m travel,13.935m/s downward over3s. Previous failed trajectory test sampled too early for clearance from the intact lower storey;original failed receipts retained. One diagnostic-parenthesis compile error corrected and retained. V2 native/Windows acceptance pending.


## October22 — Bungee city accepted Windows milestone

Exact f70add93 Windows payload passed77assertions across full-match12/0,physical support/drop27/0 and focused38/0. Full-match totals1160/1015/1700/2010;all actual contact scores,locked finales and rematch/menu cleanup. Measured original drop50.604m,28.584m/s,6.660m extension;two lost supports collapse all35unsupported upper tower modules. Original shared-stick/trigger routing,source rejection,pause,software reconnect,fresh input,tiebreak,cross-bay rubble isolation,1/2/4views and rematch acceptance retained. Four-camera60.008s workload6420frames/25680automatic renders,mean9.351ms,p9512.524ms,p9918.757ms,zeroGC0,manageddelta3387392bytes. This is actual Windows workload evidence,not an isolated GPU benchmark or physical-human playtest.

Actual Windows colors/textures render correctly. Static Editor one-shot previews sometimes rendered transient gray materials;no vendor/material edits were justified by that preview alone. Final art repeats natural-height middle storeys;unshipped stretched v1 stays only in the isolated validation workspace. Exported only owned v2 variants,11mesh assets,45real-model fracture prefabs,source metas and4scene reference changes. Original3210scene documents preserved. Renderers665to305perbay. No original Taxi/vendor or925unrelated pending bytes modified. P1Bungee acceptance complete for automated tested scope;continue other active work.


## October 22 — recorded-seed Hard Fare continuation

FareRecordedSeedHardWindows passed 16 checks on accepted build f70add93. The actual four-AI first-to-five Hard match used seed 616736539 and took 488.95 real seconds, including pause, rematch and menu return. Longest 2-metre pockets were 12.292/10.414/6.767/9.515 simulated seconds, with six announced recoveries. The original long immobility remains unreproduced. The old failure log contains no RPM/power telemetry, so an engine failure is unproven and no speculative engine patch is claimed. HPR-8-4 remains open.

## October 22 — Minefield mountain and Billiards HUD continuation

Minefield's existing physical world is 614 metres wide and 8.4 kilometres long, but the original side ranges peaked at only 19.53/21.18 metres. Explicit authoring reuses the installed Vista mountain graph; owned terrain variants now reach 161.08/190.61 metres with continuous outer crests and 2,500 grounded native pine instances per side. The central course and mine forces remain unchanged. An existing 14.019-metre runout edge step is reduced to 0.000458 metres by blending only the outer 17-metre margins. Repeat authoring preserves saved assets byte for byte. Native EditMode 4/0 and PlayMode 1/0 pass. A first complete Windows match exposed a probe classification error: one true escape was counted as a supported timer lock. Its failed receipt is retained; the corrected fresh run passed all 23 checks across thirteen heats, with one genuine escape and zero supported timer locks. Wide landscape fixtures still show vendor texture repetition; broad art-direction approval is not claimed.

Billiards now has two graphical portrait/score/objective cards, dimmed pocketed balls, a dedicated eight-ball objective, compact rack/turn/outcome information and an actual Heat shot-power bar with MPH and assigned-device prompts. Existing HUD objects and references remain hidden but intact; table, physics, cameras, controls and rules remain authoritative. Native EditMode 1/0, PlayMode 1/0 and 50 existing rule assertions pass. Exact f4a39ad0 Windows acceptance passes 18 checks across four resolutions and a complete physical AI rack, results, rematch and collection return. Images and portable receipts are retained in Evidence/2026-10-22-Billiards-HUD. Human/hardware/listening verification is unperformed. The exact f4a39ad0 combined Windows build is promoted to Builds/TruckSports/TruckSports.exe after 107 checks with zero failures. Its prior accepted build and 184 exact source inputs remain recoverable. Other active HPR/GBC repairs remain unfinished.

## October 22 — Bowling HUD accepted

HPR-11 changes only the existing lane HUD. Rounded portrait/color cards show resolved totals, frame/delivery/rack and Heat frame progress; compact guidance uses MPH and device-appropriate menu hints. All 6,058 original scene documents remain, with 145 UI documents added and 46 original UI documents changed. The four world-space lane signs, alley, truck, physics, pins, pinsetter, cameras and scoring remain unchanged. Repeat authoring preserves the scene bytes. Native EditMode 1/0, PlayMode 1/0 and 32 existing rule assertions pass. Exact Windows 56b740d8 passes 67 checks, including twenty software-input/AI-camera layouts and a complete four-AI ten-frame physical match (99/114/136/95), results, rematch, pause and menu cleanup. Human/hardware/listening acceptance remains unperformed. The result image catches the original fade onset; no settled-menu visual claim is made from that frame. Build promoted with the previous f4a39ad0 payload retained.

## October 22 — Air Hockey and Scrap Heap accepted

HPR-14/15 delivered with native EditMode 2/0, PlayMode 2/0, 26 existing rules assertions and 63 exact-payload Windows checks. Original first-to-seven physics, input ownership and all original scene documents preserved. Actual images reviewed after rejecting the earlier 55-check candidate for duplicate branding, inactive controls and competing legacy layout. Refined setup/live/goal/pause/results and three Scrap cards render correctly; Log Rolling and Trampoline Dodgeball remain playable through Scrap Heap. Exact 040a0a1d payload promoted with prior Bowling build recoverable; 201 source inputs retained. No human/hardware/listening acceptance claimed. Continue Poker and other open current-directive requirements.

## October 22 — Poker table presentation accepted

HPR-16-1 through HPR-16-4 now retain the approved original engine/cards/dealer while adding public chip/button/actor panels, owned viewport lower thirds, public-only hand-rank feedback and bounded render-only AI presence. Original action/winner animations remain. Six native tests and 505 original core assertions pass. Exact Windows e4ab7c47 passes 186 checks with zero failures: twenty layouts/four resolutions, visible chip glyphs, actual projected-logo clearance, ordinary conserved eight-AI tournament, private input ownership/disconnect/handoff, rematch and collection return. Intermediate visual and test-fixture failures are retained. All 9,482 original scene documents and 925 unrelated pending files remain intact. Human/hardware/listening acceptance is unperformed. Continue remaining current-directive work.

## October22 — Cheese presentation accepted

See 2026-10-22-Cheese-Presentation.md and its recorded evidence. Original course/physics/scoring/boost preserved, licensed intake remains private, all925 unrelated pending files retained. Next active area: HPR-18 Dock Diving; other open HPR/GBC entries remain open. No entire-backlog completion claim.

## October22 — Dock venue accepted

See2026-10-22-Dock-Venue-Repair.md and its actual native/Windows evidence. Preserve all925 unrelated files, original gameplay and all10672 original scene documents. Next active area: HPR-19 Keepaway and remaining current HPR/GBC entries. No entire-backlog completion claim.

## October22 — Derby health and branding accepted

See2026-10-22-Derby-Damage-HUD.md and actual evidence. Preserve original rules/physics/replays and925unrelated files. First57/0 candidate rejected visually before correction; current exact-payload layout and physical matches passed. Next: current Dance-Off requirements and other open HPR/GBC entries. No whole-backlog completion claim.

## October22 — Dance-Off concert and timing accepted

See2026-10-22-Dance-Concert.md and actual evidence. Original choreography/charts/input/HUD retained. Newly recorded Dock human feedback reopens HPR-18-2/4 and adds HPR-18-6; do not treat earlier automated acceptance as current developer approval.

## October22 — Dock human follow-up accepted by automated checks

See2026-10-22-Dock-Pool-Life.md. Rejected intermediate visuals retained; final evidence covers right platform, calm impact-responsive pool, animated crowd/photo behavior, real matches and rematches. No developer/hardware/listening/profile acceptance claimed. Continue the current open HPR/GBC requirements.
