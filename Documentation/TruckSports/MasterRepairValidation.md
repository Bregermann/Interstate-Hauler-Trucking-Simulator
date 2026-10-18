# Truck Sports master repair — October 17, 2026

All ten requested repair packages are implemented, saved and validated. The combined Windows x64 Development build contains all 31 implemented sports in 33 explicit Sports-only scenes. TruckSports starts first; the normal catalogue has 30 entries and Wallball remains manually reachable in Experimental Scrap Heap.

Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the complete accompanying folder together. Build `2026-10-17T10:53:04.7155517Z`, zero errors / 47 incremental warnings, 1,191,046,536 bytes, 00:01:39.5986327. Managed assembly SHA256 `1ded15c3f1629166224a67d36d754d2d821b22a8cdf587fbfdecdf42efa9bf4d`. All 25 protected ProjectSettings files match their pre-build hashes, including the host build configuration; the deliberate failure-restoration fixture also passes.

## Implemented repairs

| Mode | Actual cause and repair |
|---|---|
| Keepaway | Sheep lacked a locomotion controller and used fixed-height placement. Bind genuine SheepV5 idle/run skeletal animation, actual displacement rate and collider grounding, with correctly scaled skinned-foot calibration. Arena stays shared/default; First Person uses only owned human views. Fit the complete arena, compact its cards, restore NWH cab-wheel steering. Rules and 30/60/90 targets remain. |
| Cheese Rolling | The starter grid began on a slope. Rebuild the actual terrain with a flat starting grid at 335m elevation, smooth transition, enormous 2:1 descent and runout. Upright randomized grid, grade-aligned recovery and relative height safety preserve ordinary NWH driving and blast flights. Mud and runout zones now match the terrain. |
| Giant Swing | Replay offsets followed recorded below-ground positions without final terrain/obstacle clearance. Ground-aware replay focus, sphere cast and final camera clearance extend the existing Cinemachine pose. Launch, actual recorded motion and first-contact scoring are unchanged. |
| Curling | Even 10m/s overshot the old 58.5m back line; unrestricted 20m/s exceeded it substantially at .65m/s² deceleration. Triple physical sheet 78→234m and release-to-back 58.5→175.5m; move tee/hog/house/rails/cameras. Apply one bounded linear .74× entry-speed conversion, accurate power HUD and inverse AI planning/braking. Subsequent sliding and takeouts remain physical. |
| Terrier Racing | Dessert logic explicitly skipped finished racers and caught losers. The old meadow was 52m below the track with scenery pushed away. Only the earliest ordered full-body crossing becomes Finished; preserve real solid-wall normal-speed crash detection and immutable results. Winner coasts with no drive/brake, leaves the 6m exit and physically lands in the aligned, winner-only cream bowl; the opening then blocks losers. Add a 2048m shaped volcanic landscape, crater, lava, installed rocks, mountains, smoke, bounded embers/refractive heat using the standard URP particle shader, and six real scaffold-model pylons; actual lava contact handles wreck feedback/cleanup. Fix mixed-racer text in the read-only broadcast cards. |
| Minefield | Hardcoded x306/z2087/y-15 boundaries failed launched trucks; a 90s timer froze unsupported flight. Extend actual terrain/colliders 2100→8400m and camera/scenery coverage. Valid flight is never failed or scored by those limits. True escape from all terrain recovers safely behind the last genuine blast with no bonus distance; only supported low-energy simulation failure has a separate 180s recovery. |
| Mattress Racing | Independent pillows were missing. Extract the actual 237-vertex pillow component and UVs from the installed Bed model. Two colored convex Rigidbody pillows per truck, separate parents/no joints, nonoverlapping starts, release on GO, recycle on rounds/rematches and record actual poses in replay. |
| Musical Parking | Occupied-target invalidation already worked; congested AI oscillated its reverse direction and had no committed escape. The original seed stalled 22 seconds with an open bay. Add progress monitoring, bounded ordinary-control escape, reverse hysteresis, stable detours and temporary blocked-target avoidance. No teleport or forced claim; ownership and penalties remain authoritative. |
| Bungee Demolition | Permanently kinematic rubble obstructed swings; center relative velocity missed real rotational contact-point speed. Keep settled rubble dynamically sleeping; use pre-physics linear plus angular normal closing speed through Core-Collapse damage only. Add exactly ten reachable 3/4-storey buildings (35 modules alongside the original 50), physical AI surface targeting and full reset/score lifecycle. No second impact impulse. |
| Poker | Tens used T and the back monogram TS was ambiguous. The reported missing-rank symptom was not reproduced in the original controlled layout; no invented root cause is claimed. Show true 10/rank/suit/corner values with clear backs; use supported TMP parse/mesh clearing to remove private data safely. Deck, evaluator, betting and privacy rules remain. |

## Executed Windows acceptance

All 33 suites below passed with explicit payload provenance. 4 ran on the delivered managed assembly; 29 earlier cases cover unchanged modes. Earlier results bind assemblies `3f69e06c097f7e0bf46688c0c6179993c7f916a400af08846855186d32677220` and `5da92920c6295b549d8dd7b78d93c9edf80e5c599c80293d99d8ec611521eb43`; each report records its exact version. The first covers nine unchanged acceptance areas; the second covers twenty. The final payload reran startup/delivery, the complete Terrier match, the complete Minefield match and the entire scene tour. Across rebuilds, exactly nine reviewed source/asset paths differ: three optional test harnesses, the read-only score-card binding, two Terrier atmosphere authors, its scene and heat material/meta. Decoded IL confirms 11,746 other methods, including all authoritative physics, scoring, AI, input and camera methods, remain equivalent. The sole normal code change in the last rebuild binds Curling's active card to DELIVERING/READY; it cannot write turns or scores. This is version-labelled acceptance, not a claim that all 33 processes ran the final payload. Each launcher records the process, assembly hash, fresh result, exit code and runtime exception scan. All passed with zero captured gameplay errors/exceptions. Native validation additionally checks all author/rule fixtures, all 33 saved scenes for scripts/listeners/event systems and required Unity metadata. The broad scene tour exercises all 32 non-front scenes, five actual window sizes (1280×720, 1920×1080, 1440×900, 1280×960, 2560×1080), long names, pause, rematch and repeated menu returns. Brief scene-tour samples are not full physical matches.

| Case | Probe / scope | Result |
|---|---|---|
| 1 | `-tsDelivery`  | PASS · final |
| 2 | `-tsMasterRepair` keepaway-layout-win | PASS · earlier; verified unchanged |
| 3 | `-tsTerrierFinish`  | PASS · earlier; verified unchanged |
| 4 | `-tsMasterRepair` terrier-win | PASS · earlier; verified unchanged |
| 5 | `-tsMasterRepair` curling-win | PASS · earlier; verified unchanged |
| 6 | `-tsCurling`  | PASS · earlier; verified unchanged |
| 7 | `-tsMasterRepair` minefield-win | PASS · earlier; verified unchanged |
| 8 | `-tsMineRest`  | PASS · earlier; verified unchanged |
| 9 | `-tsMinefieldFocused`  | PASS · earlier; verified unchanged |
| 10 | `-tsMasterRepair` mattress-win | PASS · earlier; verified unchanged |
| 11 | `-tsMattressFocused`  | PASS · earlier; verified unchanged |
| 12 | `-tsMasterRepair` cheese-win | PASS · earlier; verified unchanged |
| 13 | `-tsCheese`  | PASS · earlier; verified unchanged |
| 14 | `-tsMasterRepair` parking-full-win | PASS · earlier; verified unchanged |
| 15 | `-tsParkingFinal`  | PASS · earlier; verified unchanged |
| 16 | `-tsParkingFocused`  | PASS · earlier; verified unchanged |
| 17 | `-tsMasterRepair` swing-replay-win | PASS · earlier; verified unchanged |
| 18 | `-tsSwingFocused`  | PASS · earlier; verified unchanged |
| 19 | `-tsMasterRepair` bungee-damage-win | PASS · earlier; verified unchanged |
| 20 | `-tsMasterRepair` bungee-full-win | PASS · earlier; verified unchanged |
| 21 | `-tsBungeeSlice`  | PASS · earlier; verified unchanged |
| 22 | `-tsMasterRepair` poker-win | PASS · earlier; verified unchanged |
| 23 | `-tsPokerFocused`  | PASS · earlier; verified unchanged |
| 24 | `-tsMasterRepair` keepaway-grounded-win | PASS · earlier; verified unchanged |
| 25 | `-tsKeepawayRegression`  | PASS · earlier; verified unchanged |
| 26 | `-tsTerrier`  | PASS · final |
| 27 | `-tsProductionTour`  | PASS · final |
| 28 | `-tsProductionWorkload`  | PASS · earlier; verified unchanged |
| 29 | `-tsMattress`  | PASS · earlier; verified unchanged |
| 30 | `-tsMattressWorld`  | PASS · earlier; verified unchanged |
| 31 | `-tsMinefield`  | PASS · final |
| 32 | `-tsFocusMatrix`  | PASS · earlier; verified unchanged |
| 33 | `-tsLegacyLook`  | PASS · earlier; verified unchanged |

Targeted probes distinguish controlled initial poses/rule fixtures from normal autonomous matches. Curling travel uses actual PhysX at .01/.02/.0333333 fixed steps: weakest approximately .47m, 25% approximately 10.6m, 50% approximately 42m, full approximately 169m. The full AI series checks real draws/takeouts/guard restoration. Terrier tests centered/angled passage, genuine edge contact, solid-wall slam, 550m/s swept detection, delayed post-finish collision; separate 12/20/36m/s physical catches, losing gate impacts and lava/fragments, plus the full twelve-truck first-to-five series. Minefield tests actual extended landing beyond z2400, genuine outside-world recovery, ordinary final rest/history and unsupported flight past 90 seconds. Mattress checks all sixteen independent pillows/colors, physical displacement/falls and rematch/replay reset. Parking runs three seeded eight-AI full matches and dedicated last-bay/ownership/rules probes. Bungee verifies repeated real contact damage, genuine partial health then return-hit destruction, ten reachable buildings, complete four-bay series and immutable finale scoring. Poker renders all 52 faces and exercises peek/privacy/betting/source transitions. Keepaway observes grounded animated sheep for 60 real seconds in each view and exercises complete possession/steal/victory flows.

The delivered payload completed Terrier’s twelve-AI first-to-five series in 14 heats. Its complete eight-AI Minefield match finished in 17 heats, with 197 launches, 133 final rests, 23 correctly classified true escapes and zero supported timer locks. The three seeded Parking matches each completed seven rounds and 28 physical captures (84 total).

Earlier candidate failures remain in ignored Candidate1Diagnostics/Candidate2Diagnostics/Candidate3Diagnostics. Terrier originally demanded a winner in the first natural heat rather than permitting a legal all-wreck rerun; MineRest expected the removed 90-second airborne freeze. Cheese's fixture treated temporary device Disabled focus suspension as physical removal; it now uses actual InputSystem.RemoveDevice. Terrier's full-series fixture lacked a virtual keyboard while native hardware was deliberately isolated; explicit injected-keyboard setup and cleanup corrected its layout checks. Minefield's old full-match assertion rejected all RecoveryLocked outcomes, although every flagged case matched a logged true outside-world escape. The final fixture distinguishes actual supported final rest after fair escape from unsupported or generic timer locks, checks one final lock and no distance beyond the genuine last blast. All corrected suites passed fresh Windows runs. No fixture change grants an automatic winner, privileged input, score or trajectory. A restricted-shell startup failed Unity PlayerPrefs writes; the same final payload passed under normal Windows permissions without a code workaround. The failed restricted launch remains in ignored diagnostics. The full focus/removal matrix and legacy cab wheel/look regression also pass.

## Code and saved assets

| Mode | Key project-owned changes |
|---|---|
| Keepaway | `Assets/TruckKeepaway/Scripts/KeepawayMatch.cs`, `KeepawaySheep.cs`; both Keepaway scenes; shared `SportsSheepMotion` and `SheepMotionAuthoring`; broadcast/cab adapters. |
| Cheese | `CheeseRules.cs`, `CheeseTruck.cs`, `CheeseMatch.cs`, `CheeseRepairAuthoring.cs`; saved Cheese scene and actual hill TerrainData. |
| Swing | `Assets/TruckGiantSwing/Scripts/SwingMatch.cs` replay-only camera safety. |
| Curling | `CurlingRules.cs`, `CurlingStone.cs`, `CurlingMatch.cs`, `CurlingRepairAuthoring.cs`; saved scene, existing authors and physical probes. |
| Terrier | `TerrierMatch.cs`, `TerrierRacer.cs`, `TerrierCourse.cs`, `TerrierDessert.cs`, `TerrierCreamSurface.cs`, `TerrierVolcano.cs`, `TerrierLavaSurface.cs`; native dessert/volcano authors, saved scene, TerrainData and pylon mesh. |
| Minefield | `MinefieldRacer.cs`, `MinefieldMatch.cs`, `MinefieldLandscapeAuthoring.cs`; saved scene and three actual TerrainData assets. |
| Mattress | `MattressWorld.cs`, `MattressMatch.cs`, `MattressPillowAuthoring.cs`; derived pillow mesh, contact material, saved scene and render-only world ghost. |
| Parking | `Assets/TruckMusicalParking/Scripts/ParkingMatch.cs` navigation/recovery and diagnostic state. |
| Bungee | `BungeeBay.cs`, `BungeeModule.cs`, `BungeeDebris.cs`, `BungeeMatch.cs`, `BungeeExpansionAuthoring.cs`; ten fracture prefabs, expansion prefab and saved scene. |
| Poker | `PokerCardView.cs`, `PokerHand.cs`, `PokerRepairAuthoring.cs`; saved layout/card faces. |

Shared native defaults are in `Assets/TruckSports/Editor/SportsProductionAuthoring.cs`; optional targeted development checks are in `SportsMasterRepairProbe.cs`. Project EditMode author/rule fixtures ran through SportsBuilder.Validate, with native Editor PlayMode probes and actual Windows PhysX checks; these are not claimed as a new NUnit test assembly.

Other Sports scenes load and the tested ownership/configuration, controls, cameras, scoring and rematches pass. Unrelated Taxi/host source was preserved and all protected settings restored; Taxi and the main Hauler gameplay were not playtested during this Sports repair pass.

## Sustained rendered performance

Four workloads each run for 60 real seconds at timescale 1 with four owned views, ordinary AI movement commands and real rendering/physics. These are whole-frame development-player samples under uncontrolled host load, not an isolated GPU benchmark; user builds were left running. Recorded raw measurements (unchanged physical/rendering workloads on the preceding assembly, with exact provenance above):

```text
PERF scene=TruckDemolitionDerby seconds=60.01 frames=6626 mainRenders=26504 mirrorRenders=2440 meanMS=9.060 p95MS=15.139 p99MS=24.468 GC0=2 managedDelta=25931776 GPU=NVIDIA GeForce GTX 1660 Ti resolution=1280x720 timescale=1
PERF scene=TruckMattressRacing seconds=60.01 frames=4701 mainRenders=15604 mirrorRenders=0 meanMS=12.766 p95MS=17.256 p99MS=19.467 GC0=2 managedDelta=-303104 GPU=NVIDIA GeForce GTX 1660 Ti resolution=1280x720 timescale=1
PERF scene=TruckBungeeDemolition seconds=60.01 frames=8200 mainRenders=31444 mirrorRenders=0 meanMS=7.318 p95MS=10.769 p99MS=11.609 GC0=0 managedDelta=3043328 GPU=NVIDIA GeForce GTX 1660 Ti resolution=1280x720 timescale=1
PERF scene=TruckBuzkashi seconds=60.01 frames=9543 mainRenders=38172 mirrorRenders=0 meanMS=6.288 p95MS=6.626 p99MS=7.681 GC0=0 managedDelta=15441920 GPU=NVIDIA GeForce GTX 1660 Ti resolution=1280x720 timescale=1
```

## Visual evidence and remaining human checks

Files named `controlled` use actual Camera.Render plus composited existing UI; files named `window` use normal ScreenCapture. Selected actual Windows renders and version-bound reports are in [MasterRepairEvidence](MasterRepairEvidence). All ten repaired modes were inspected in their actual rendered output. The screenshots are not substitutes for the physical/rules assertions. Earlier numerical reproductions and native evidence remain locally under Logs/TruckSports/MasterRepair and TerrierUpdate; generated logs/builds are not committed.

Remaining manual acceptance: real controllers/shared sticks/replacement and haptics; subjective sound mix and squish; four-player readability/feel on real displays; human steering, replay cinematography and difficulty balance. Wallball remains experimental and still needs its first human playtest. A fresh remote checkout with the licensed vendor prerequisites was not rebuilt in this pass. Existing shader warnings include URP deprecated keywords/includes and Weather Maker uninitialized-variable warnings; vendor source was preserved. Passing automation does not assert every possible gameplay situation is defect-free.

## Selected inspected images

Images show the actual renderer; their exact payload/capture provenance is in [VisualInspection.json](MasterRepairEvidence/VisualInspection.json). Controlled renders are labelled separately from normal Windows captures.

| View | Inspected capture |
|---|---|
| Keepaway | [Shared arena with three human sources; no AI-only viewports.](MasterRepairEvidence/Keepaway-TruckKeepaway-humans3-1920x1080-window.png) |
| KeepawayFirstPerson | [Three human cab views; injected source configuration.](MasterRepairEvidence/KeepawayFirstPerson-TruckKeepawayFirstPerson-humans3-1920x1080-window.png) |
| Keepaway43 | [4:3 full arena and compact long-name cards.](MasterRepairEvidence/Keepaway43-TruckKeepaway-1280x960-window.png) |
| Cheese | [Actual Windows four-view downhill play.](MasterRepairEvidence/Cheese-04-four-downhill.png) |
| CheeseHill | [Controlled view of actual physical terrain; flat starter and long descent.](MasterRepairEvidence/CheeseHill-cheese-hill-1280x720-controlled.png) |
| Swing | [Actual recorded natural flight sought to midflight; final replay camera stays above ground.](MasterRepairEvidence/Swing-replay-1-angle-0-controlled.png) |
| Curling | [Final payload long sheet, approach cab and corrected card status.](MasterRepairEvidence/Curling-TruckCurling-1920x1080-window.png) |
| Curling43 | [Final payload 4:3 cab/target view and readable DELIVERING/READY cards.](MasterRepairEvidence/Curling43-TruckCurling-1280x960-window.png) |
| Terrier | [Final payload course, actual pylon models and typed race cards.](MasterRepairEvidence/Terrier-TruckTerrierRacing-1920x1080-window.png) |
| TerrierSplit | [Final payload four human cab views using injected keyboard-source configuration.](MasterRepairEvidence/TerrierSplit-cab-layout-4.png) |
| TerrierWinner | [Actual dynamic winner after ordered full-body crossing and cream catch.](MasterRepairEvidence/TerrierWinner-winner-cream-20-controlled.png) |
| Minefield | [Actual enlarged snowy field; broadcast view, not a claim all eight trucks always fit.](MasterRepairEvidence/Minefield-TruckMinefield-1920x1080-window.png) |
| MinefieldUltrawide | [Ultrawide HUD in actual player capture.](MasterRepairEvidence/MinefieldUltrawide-TruckMinefield-2560x1080-window.png) |
| Mattress | [Actual sixteen independent colored pillow bodies after physical movement.](MasterRepairEvidence/Mattress-pillows-race-1920x1080-controlled.png) |
| MattressSplit | [Four rendered moving cab views during sixty-real-second workload.](MasterRepairEvidence/MattressSplit-TruckMattressRacing-four-view-window.png) |
| Bungee | [Ten added reachable buildings after real contact/damage tests.](MasterRepairEvidence/Bungee-ten-real-building-impacts-controlled.png) |
| Poker | [All 52 actual rendered faces; all tens and suits inspected.](MasterRepairEvidence/Poker-all52-1920x1080-controlled.png) |
| Parking | [Controlled initial occupied-bay fixture; normal AI/NWH drives 27.5m to a vacant bay without later pose writes.](MasterRepairEvidence/Parking-ai-reaches-alternative-bay-controlled-native.png) |
| TerrierAtmosphere | [Native wide environment view of actual shaped terrain/crater/mountains/bowl.](MasterRepairEvidence/TerrierAtmosphere-volcano-environment-controlled.png) |
| TerrierHeat | [Native controlled close view with bounded genuine standard-URP distortion and embers.](MasterRepairEvidence/TerrierHeat-heat-embers-on-controlled.png) |

## Source, regeneration and delivery

Use `LWS.TruckSports.Editor.SportsBuilder.Build` from the saved scenes. Do not routinely call `CreateValidateBuild`: it reauthors the front end and hockey. Native repair defaults live behind `SportsProductionAuthoring.MasterRepairs` and each sport's author. Scene serialization used native Unity APIs. All 2,898 final Sports source/asset/metadata files were frozen through final Windows testing; README edits are excluded. The complete reviewed delta from the earlier acceptance candidate contains exactly nine paths. The final rebuild differs from the immediately preceding candidate only in the optional Terrier/Minefield harnesses and read-only Curling card label. Native comparison preserves 9,631 pre-existing Terrier components, exempting only the volcanic child list and camera scene-color-copy settings. No authoritative gameplay method changed across these final rebuilds.

Git delivery is verified below. The reviewed scope includes all remaining relevant Sports source/assets/scenes/metas/docs, while preserving unrelated Taxi/host/vendor work. No generated build, cache, credentials or player/Editor log is staged.

## Vendor record

- **VENDOR ASSETS AUDITED:** NWH VP2/WheelController, Cinemachine, Core-Collapse, Weather Maker, Weatherade/Taxi adapters, Haberdashery SheepV5 rig/clips, HQ ResidentialHouse Bed/fabric, ScaffoldsPack model, Vista terrain layers, Path Painter mud, Heat/TMP, DOTween, installed scenery and UTS foundation.
- **VENDOR ASSETS USED:** Existing NWH vehicle physics/input, Cinemachine, Core-Collapse damage/support/fractures, Weather Maker/Weatherade, real SheepV5 and Bed components, ScaffoldsPack bridge supports, Vista layers/Unity Terrain, installed rocks, existing Tug lava shader and NWH smoke, Heat/TMP/DOTween presentation.
- **RELEVANT ASSETS NOT USED:** UTS road navigation does not implement competitive vacant-bay ownership/recovery; existing NWH Sports controls remain. Rope Toolkit is absent; retain the established SpringJoint/LineRenderer fallback.
- **CUSTOM SYSTEMS CREATED:** Sport-specific motion/lifecycle adapters, native repair migrations, winner-only cream/lava coordination, camera safety and opt-in development regression probes.
- **VENDOR SOURCE MODIFIED:** None.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** None; existing vendor foundations retain their authority.

## Verified Git delivery

Implementation `bfd14cadbc72ceab0b865c912a6e8661f67c198e` on `codex/truck-sports-seven-events` was pushed successfully to that branch and GitHub `main`, verified at 2026-10-18T00:59:19.606915+00:00. **Both previous relevant Truck Sports work and this repair update are committed and pushed to main.** The public Sports tree matches the reviewed local Sports implementation. Unrelated local Taxi/host/vendor work and private Taxi ancestry are preserved and excluded; generated builds, logs, caches and original licensed package prerequisites are excluded.

This receipt is committed as a subsequent clean public descendant. The delivered folder's GIT-DELIVERY.txt records the final verified public tip after the receipt push. All build, test and remaining-manual limits above still apply.
