# Truck Sports: Party! — eighteen-sport expansion delivery

Recorded 2026-10-21T06:20:50.619280+00:00. Windows x64 Development/Mono/D3D11, Unity 6000.4.10f1. All 18 requested expansion sports have saved playable scenes, original imported PNG logos, real gameplay previews and menu integration. The combined executable contains 60 scenes and starts at the Truck Sports menu. This is an implementation and automated-acceptance delivery, not a claim of human-certified release readiness or completion of the archived unrelated backlog.

Build: Builds/TruckSports/TruckSports.exe, with all companion files. Managed SHA256: bab2a1fed553025efc4563091c294bdd08f2e868cc76b3725d622b117e902c36. Runtime manifest: 546 files / 1,960,389,072bytes. The previously accepted 57-scene build is preserved at .sports-validation/Recovery/Expansion-Fifteen-Sport-57-Scene-Verified. The final accepted build is backed up separately after verification.

## Final verification

- Native EditMode: 331/331passed; includes all 18 rule suites, saved-scene bindings, jump regression and warped-wall seam regression. XML is provided. Runtime tests below are explicit Editor PlayMode and Windows gameplay probes, not a fabricated NUnit PlayMode XML result.
- Actual Windows frog: 48checks,0errors, exit0,308.85seconds; unchanged identical managed binary.
- Actual Windows avalanche: 41checks,0errors, exit0,258.71seconds; unchanged identical managed binary.
- Actual Windows ninja: 36checks,0errors, exit0,114.36seconds; unchanged identical managed binary.
- Actual Windows hide-adapters: 42checks,0errors, exit0,11.16seconds; unchanged identical managed binary.
- Actual Windows snow: 57checks,0errors, exit0,88.28seconds; unchanged identical managed binary.
- Actual Windows menu: 384checks,0errors, exit0,66.82seconds; unchanged identical managed binary.
- The earlier 15 sport-owned sources/scenes/assemblies: 187files checked unchanged. Their previously recorded complete-match tests remain historical evidence, not a claim that every old test ran again on the new binary.
- Shared route/default-preserving tuning, HUD/camera/input adapters and snow accumulation were rechecked where genuinely affected. All 18 snow scenes and the complete registered menu are exercised by the final receipts.
- Host preservation: {"protected": {"checked": 887, "changed": []}, "settings": {"checked": 25, "changed": []}, "logos": {"checked": 41, "changed": []}}. No original logo or protected Taxi/vendor file was changed by this delivery.
- Actual Windows composed frames at 720p/1080p, replay/cab/result evidence, imported logo/preview and native receipts are in Evidence/2026-10-20-Expansion/Eighteen/.

## Per-sport implementation and evidence

The first 15 are preserved working slices. Exact earlier tests and limitations are retained in the corresponding delivery reports; they are not retrospectively relabelled human-verified.

| Sport | Implemented gameplay | Preserved acceptance reference |
|---|---|---|
| Red Light, Green Light | Twelve NWH trucks, laugh-warning red/green authority, genuine movement attribution, physical destruction and finish/results | EighteenSportsDelivery.md |
| Hot Potato | Physical possession/pass cooldown, finite fuse, actual truck destruction, scoring and rematches | EighteenSportsDelivery.md |
| Balloon Battle | Physical per-balloon hit attribution and one-time pops, attacks, elimination, cameras and match flow | EighteenSportsDelivery.md |
| Whack-a-Mole | Finite timed targets, physical valid-window hits, fair AI, scoring and rounds | EighteenSportsDelivery.md |
| King of the Hill | Moving/contested territory, authoritative possession time, actual truck competition and compact bottom-row HUD | EighteenSportsFiveSportDelivery.md; Evidence/HillHud/ |
| Seesaw | True weight-driven platforms, balance/elimination, bounded physical recovery, results/review | EighteenSportsSixSportDelivery.md |
| Floor Is Lava | Physical collapsing platforms, survival/ramming, lava/destruction, deterministic resolution and rematches | EighteenSportsSevenSportDelivery.md |
| Tetherball | Physical constrained ball and correct directional winding, competing trucks, score and replay | EighteenSportsNineSportDelivery.md |
| Jousting | Physical lance/contact attribution, damage, rounds and fair AI approaches | EighteenSportsNineSportDelivery.md |
| Magnet Mayhem | Bounded polarity forces, real occlusion and hauling, settled bay scoring,40physical scrap objects | EighteenSportsTenSportDelivery.md |
| Volleyball | Actual giant-ball contacts, teams/three-touch rules, jumps, net crossing and match flow | EighteenSportsTwelveSportDelivery.md |
| Freeze Tag | Physical tag/rescue, true frozen state, fair teams and match resolution | EighteenSportsTwelveSportDelivery.md |
| Hungry Hungry Haulers | Physical scoop/hopper and deposited-ball scoring, shared input/cab/AI, finite collection and rematch | EighteenSportsFourteenSportDelivery.md |
| Sheep Herding | Actual animated flock threat/escape and full-body pen scoring; no proximity points | EighteenSportsFourteenSportDelivery.md |
| Hide & Seek | Eight trucks, stealth engine/lights/restart, fair LOS/audio clues, physical tags, found spectators and role rotation | EighteenSportsFifteenSportDelivery.md |

### Frog (EX04)

Five independent one-life crossings and a five-challenge championship; actual UTS wheeled traffic, physical freight trains, container machinery, forklifts and moving river barges; ordered full-body gates and finish, fastest successful win point, tied replays, participant inputs/cabs, countdown, result highlights and rematch.

Scene: Assets/TruckFrogger/Scenes/TruckFrogger.unity. Scripts/authoring/tests: Assets/TruckFrogger/. Original artwork: Assets/TruckSports/Art/Logos/TruckFrogger.png; real preview beside it in Art/Previews/.

Root causes repaired: Kinematic speculative contacts after relocation created remote phantom train impacts. Moving obstacles now use ordinary Discrete contact with sufficiently thick collision geometry; genuine solid train contact still eliminates. Wheel.Initialize was rejected because it did not fix the contacts and invalidated replay wheel references. Normal-speed validation also exposed a second traffic lane 28m beyond the first: AI gap prediction and rear-body crossing clearance now cover both directions, with four regression cases. The parked-truck collision fixture now leaves NWH suspension powered and supplies neutral keyboard input, rather than disabling the motor and letting the body sink beneath the train chassis.

Reused assets: Full NWH tractor/cab, UTS public CarWalkPath/MovePath/CarAIController/CarMove via LwsUtsTrafficApi, EasyRoads/LWS road baking, ShipContainerPack, owned Sky Tow expedition hull, purchased city/industrial models, existing Fishing water material, Vista pines, Weather Maker/Weatherade, Heat/TMP/DOTween.

### Avalanche (EX07)

Twelve simultaneous NWH trucks race down a 2.4km Arctic mountain; broad road, risky cliff gap and frozen-river alternative, tunnel, bridge and rescue finish. Actual moving snow contacts push and reduce grip before a 4.5-second escape window and sustained burial; terrain-following settled-snow volumes prevent survival after the thin front has passed. Physical debris, pooled visible burial mounds, recorded snow ridge, rumble/shake, ordered finish rankings, human-only or optional AI viewports, replay and rematch.

Scene: Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity. Scripts/authoring/tests: Assets/TruckAvalancheEscape/. Original artwork: Assets/TruckSports/Art/Logos/TruckAvalancheEscape.png; real preview beside it in Art/Previews/.

Root causes repaired: Initial AI converged on the same staging centre and blocked each other until buried. Staging lanes, bounded route offsets and mode-specific route tuning let real trucks escape. A final terrain audit found all twelve starts inside the coarse cloned terrain above the staging deck. The sport-owned heightmap is now kept below the existing road geometry, with blended shoulders and preserved ravine cuts. A subsequent solid-contact audit proved that baked vertical spline overshoot also put the road surfaces1.9m above the flat platform, penetrating the chassis; owned mesh heights now match the intended flat start and downhill grade. Baked road smoothing also disagreed with the bridge and AI route coordinates. The owned road meshes now come from EasyRoads public StraightXZ controls, matching the authoritative routes; actual NWH braking anticipates bends. Packed-snow road color is overridden locally for contrast. A real curved takeoff mesh makes the risky cliff gap traversable without scripted launch forces, and full-race acceptance checks a real finisher on each of the three routes. Actual pre-GO grounded-wheel and twelve-truck forward-start checks plus terrain-clearance regressions cover this cause. Recovery uses the authored road height rather than placing a truck below its road mesh. The snow front initially sampled an unrelated high mountain peak at x=0; it now follows the actual route elevation. Snow history uses real trailing trigger contacts and replay records visible ridge/mounds rather than depending only on particles. The actual Windows broad-road stall was reproduced by a failing offset-waypoint regression: advancement checked the centreline rather than the assigned 15 m lane offset. The fixed route evaluates the actual offset waypoint, and recovery repairs genuine vehicle damage through NWH. The setup artwork now uses the shared scene-object naming convention.

Reused assets: Owned clone of existing Sky Tow Arctic TerrainData, Vista/NWH terrain layers and pine models, EasyRoads meshes and published LWS road APIs, Toon Gas Station rock clusters, purchased city cabins/lodge, NWH public damage/vehicle APIs, Weatherade road materials/traces, Weather Maker thunder/noise texture, Cinemachine noise profile, existing Sports review/HUD/spectators.

### Ninja (EX05)

Six physical obstacles: weight-driven tilting decks, rotating traction logs, three jointed wrecking balls, a seesaw bridge, spinning discs and a curved warped wall ending in an actual hinged bumper-struck bell. Normal/charged jumps and NWH spring compression, limited air correction, timed alternating athletes, six ordered checkpoints, +5-second physical fall recovery, stuck recovery, rankings/records, a 1,350-spectator stadium, result replay and rematch.

Scene: Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity. Scripts/authoring/tests: Assets/TruckNinjaWarrior/. Original artwork: Assets/TruckSports/Art/Logos/TruckNinjaWarrior.png; real preview beside it in Art/Previews/.

Root causes repaired: Downward contact velocity was omitted from jump correction, cancelling takeoff on descending platforms; bounded takeoff delta now accounts for signed incoming vertical speed, with three regression tests. Earlier launch points overshot or lacked run-up; grounded obstacle timing now uses measured motion. The elevated finish deck started before the curved ramp ended, creating a raised collision lip; their edges/heights now match with a dedicated regression. The AI used to brake for its required landing/bell; those real rigidbodies use the existing permitted-contact target API, preserving collision authority. A focused real-physics diagnostic proved that the curved ramp itself became a false solid-wall obstacle in the AI sphere cast: desired speed dropped from 12 m/s to 0 before takeoff. The existing LWS driving adapter now accepts an explicit authored planned-ground collider for this obstacle only, while ordinary walls still stop the AI and all physical ramp collision remains enabled. Saved-scene binding and navigation/collision regressions cover both sides of this distinction. The focused wall trace is labelled as a partial fixture, not complete-course acceptance; full-course and Windows probes separately require every checkpoint and actual bell contact. Repeated acceptance exposed velocity-jitter stall detection: recovery now uses meaningful body displacement, with four regressions and a constrained physical recovery fixture. First takeoff targets the platform centre. Measured1m start/step and1.1m seesaw/pad penetrations were removed; joints connect to their own kinematic supports with only connected-part collision disabled. Safe recovery uses earned checkpoint positions, and the seesaw departure uses the existing charged jump. All weighted platforms are checked for nonconnected static overlap and level release before truck contact.

Reused assets: Full NWH vehicles and public wheel-spring/damage APIs; working Limbo compression and shared bounded assembly-velocity jump foundations; PhysX ConfigurableJoint/HingeJoint and existing cable rendering; existing Field Goal/Hot Potato optimized posed crowd and audio; Fishing URP water/result chime; Cinemachine, Heat/TMP/DOTween, Weather Maker/Weatherade and Sports replay.

## Actual Windows gameplay frames

![Frogger](Evidence/2026-10-20-Expansion/Eighteen/Frog-challenge-1-1080.png)

![Avalanche Escape](Evidence/2026-10-20-Expansion/Eighteen/Avalanche-mountain-1080.png)

![Ninja Warrior](Evidence/2026-10-20-Expansion/Eighteen/Ninja-course-1080.png)

## Built menu evidence

![Windows sport-selection showcase](Evidence/2026-10-20-Expansion/Eighteen/Menu-showcase-1920x1080.png)

All eight collection pages, title screen,720p/1080p/1440p and ultrawide captures are retained beside this image. These are actual composed Windows frames from the final menu acceptance.

## Measured development-build performance

Unity ProfilerRecorder and FrameTimingManager telemetry during normal-speed live play, after warmup; capture pauses excluded. This is the current workstation/development build, not a controlled hardware benchmark. This measures normal-speed AI-only broadcast play in clear weather and the neutral burial fixture; it is not a four-viewport severe-weather benchmark. A zero counter sample count means unavailable, never zero cost. Complete JSON, including sample counts and memory, is preserved.

| Mode | Frames | Frame mean/p95 ms | CPU mean/p95 ms | Physics mean/p95 ms | GPU samples | Peak allocated MiB |
|---|---:|---:|---:|---:|---:|---:|
| Frog | 12000 | 6.59/6.35 | 6.58/6.99 | 0.36/0.59 | 11987 | 291.9 |
| Avalanche | 12000 | 7.60/12.51 | 7.59/10.24 | 0.58/0.84 | 11273 | 370.2 |
| Ninja | 12000 | 6.61/12.44 | 6.60/12.42 | 0.31/0.60 | 11997 | 288.1 |

## Windows-discovered AI and artwork corrections

The first complete Windows pass was retained with its failures: Avalanche41checks/1error, Ninja32checks/2errors, menu384checks/1error. Avalanche's broad-road AI reached its15m-offset waypoint but checked advancement against the road centre; the new focused test reproduced the stuck index on the old code. Advancement now measures the actual assigned waypoint, and checkpoint recovery repairs genuine vehicle damage through NWH's public API. Ninja's original fixed takeoff points launched too early when real forward speed was lower. Takeoffs now approach the actual tilting-deck edges, charge the jump before contact, and use the existing permitted-landing-body navigation API. The first safe landing remains separated by a real 13.5 m jump; renderer/collider agreement has a saved-scene regression test. Avalanche's imported setup logo existed but used a different object name; it now follows the shared authored artwork convention. Final receipts above are the post-correction Windows results, not the earlier failed attempts.

## Round reset presentation correction

Visual review caught an old SPLASH +5 announcement persisting after a fresh Ninja round because the match clock reset while its transient-message expiry did not. Ninja and Avalanche now clear announcement text, its expiry and old feedback voices in BeginGame. The final Windows probes explicitly check fresh-round messages; preceding native gameplay receipts are retained separately and are not represented as post-fix UI verification.

Frogger's final-approach crossing counter is also clamped to the actual gate total, preventing5/4after the fourth gate. The final HUD change was compiled and the native suite rerun, then actual Windows screenshots were installed as menu previews. Because this source change produced a new managed binary, all six Windows suites were rerun on that final binary rather than reusing the earlier gameplay receipts. The exact before/after hashes and preview-integration preservation record are supplied.

The latest Editor gameplay-probe receipts are labelled separately. Those physical gameplay runs preceded the final one-line Frogger HUD edit; the324-test EditMode suite and all final Windows gameplay checks were rerun after that edit. No earlier Editor result is represented as a post-edit human or hardware playtest.

## Intermittent beached-truck recovery correction

The next complete Windows repetition caught a genuine Ninja failure despite a prior passing match: the truck remained near the second tilting step for over100seconds. Suspension contact jitter repeatedly reset its instantaneous-speed stall timer. Recovery now measures meaningful body displacement, counting real driving and vertical jumps while ignoring small vibration. Four EditMode regressions cover jitter, movement, jumping and checkpoint reset. A new explicitly constrained real-NWH beached-truck fixture verifies bounded recovery, exactly one five-second penalty, an upright return to the earned checkpoint and no invented progress; temporary fixture constraints are restored. The full-course and Windows acceptance assertions remain strict. The failed4870binary result is retained separately from the final pass.

The measured first-jump arc also landed near the moving deck's far edge; its input takeoff now targets the middle. A separate saved-scene collision audit reproduced a1m penetration between the starting deck and first moving step, plus world-anchored moving decks intersecting their static supports. The start pad retains its spawn and flat surface but ends before the moving step. Four joint supports are now kinematic connected bodies: the joints disable collision only with their own mechanically interlocking supports, while trucks still collide normally with both. No global collision layer, jump force, scoring shortcut or vendor source was changed. Two initially failing saved-scene regressions now pass, and actual gameplay checks that unloaded steps remain level at their anchors before the truck arrives. Consecutive complete physical attempts and final Windows acceptance are recorded separately.

The whole-platform contact audit additionally measured1.1m seesaw penetration into each adjacent static pad. The pads now leave genuine clearances at both ends, retain the earned scoring/checkpoint planes, and provide safe full-truck recovery at the checkpoint itself. AI uses its existing charged jump for the seesaw departure. The additional regression checks every weighted deck against all unconnected static geometry; the live unloaded-platform check includes the seesaw as well as all three steps. No overlapping scenery is silently ignored by a global layer change.

## Honest remaining acceptance

Physical gamepads/wheels, listening, subjective balance and human visual/playtest approval were not performed. Software-created input devices exercise ownership and camera/control integration, not hardware certification. Extended multi-round alternating championships, Easy/Normal balance across every course, severe-weather replay readability and long soak/memory behavior are not exhaustively certified. Raw performance evidence above must be reviewed on target hardware. These are individually retained against the permanent IDs; no failed test is hidden by deleting its assertion.

Ledger status counts for these 180 expansion requirements: {"IMPLEMENTED / UNVERIFIED": 144, "VERIFIED COMPLETE": 36}. IMPLEMENTED / UNVERIFIED entries intentionally remain distinct from objective VERIFIED COMPLETE and human approval. The archived historical backlog is not silently reopened or declared finished.

## Vendor accounting

VENDOR ASSETS AUDITED: NWH public vehicle/wheel/damage APIs, UTS public traffic/controller/path APIs, EasyRoads/LWS authoring/navigation, Sky Tow/Vista Arctic terrain and installed city/industrial/rock/pine inventories, Weather Maker/Weatherade, Cinemachine, Heat/TMP/DOTween, existing crowd/water/input/crew/replay systems.

VENDOR ASSETS USED: full NWH tractors/cabs; UTS real traffic; existing EasyRoads/world baking and LWS route adapter; owned clones of purchased terrain, buildings, containers, rocks and foliage; existing Fishing URP water; Weather Maker effects/audio and Weatherade snow/trace material APIs; Cinemachine, existing Heat/TMP/DOTween and Sports review.

RELEVANT ASSETS NOT USED: installed inventory had no complete matching Frogger freight train/container machinery, competitive avalanche game authority or six-obstacle truck-Ninja sport. Those game-specific structures/semantics were authored. Scene Streamer is unnecessary for bounded single-scene courses and was not duplicated. No replacement vehicle, traffic, weather or UI framework was created.

CUSTOM SYSTEMS CREATED: finite Frog/Avalanche/Ninja rules and AI coordination; seeded physical obstacle scheduling and whole-body gates; actual-contact snow exposure/burial; NWH suspension/jump/checkpoint adaptation; owned course/environment composition and original PNGs; focused acceptance probes and wrappers over Unity profiler APIs.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

## Exact build report

Succeeded
Errors: 0
Warnings: 38
Bytes: 1954362736
Duration: 00:02:43.8422855
Startup: Assets/TruckSports/Scenes/TruckSports.unity
Scenes: Assets/TruckSports/Scenes/TruckSports.unity, Assets/TruckBilliards/Scenes/TruckBilliards.unity, Assets/TruckHockey/Scenes/TruckHockey.unity, Assets/TruckBowling/Scenes/TruckBowling.unity, Assets/TruckAirHockey/Scenes/TruckAirHockey.unity, Assets/TruckWallball/Scenes/TruckWallball.unity, Assets/TruckCornhole/Scenes/TruckCornhole.unity, Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity, Assets/TruckDockDiving/Scenes/TruckDockDiving.unity, Assets/TruckKeepaway/Scenes/TruckKeepaway.unity, Assets/TruckKeepaway/Scenes/TruckKeepawayFirstPerson.unity, Assets/TruckCurling/Scenes/TruckCurling.unity, Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity, Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity, Assets/TruckMinefield/Scenes/TruckMinefield.unity, Assets/TruckMattressRacing/Scenes/TruckMattressRacing.unity, Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity, Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity, Assets/TruckGiantSwing/Scenes/TruckGiantSwing.unity, Assets/TruckBuzkashi/Scenes/TruckBuzkashi.unity, Assets/TruckBogSnorkelling/Scenes/TruckBogSnorkelling.unity, Assets/TruckBungeeDemolition/Scenes/TruckBungeeDemolition.unity, Assets/TruckFishing/Scenes/TruckFishing.unity, Assets/TruckSumo/Scenes/TruckSumo.unity, Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity, Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity, Assets/TruckBeerPong/Scenes/TruckBeerPong.unity, Assets/TruckPoker/Scenes/TruckPoker.unity, Assets/TruckTugOfWar/Scenes/TruckTugOfWar.unity, Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity, Assets/TruckLogRolling/Scenes/TruckLogRolling.unity, Assets/TruckTrampolineDodgeball/Scenes/TruckTrampolineDodgeball.unity, Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity, Assets/TruckPedestrianPlowing/Scenes/TruckPedestrianPlowing.unity, Assets/TruckPedestrianPlowing/Scenes/TruckPedestrianPlowingFirstPerson.unity, Assets/TruckKickball/Scenes/TruckKickball.unity, Assets/TruckDanceOff/Scenes/TruckDanceOff.unity, Assets/TruckLimbo/Scenes/TruckLimbo.unity, Assets/TruckSkyTow/Scenes/TruckSkyTow.unity, Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity, Assets/TruckFareFight/Scenes/TruckFareFight.unity, Assets/TruckSnake/Scenes/TruckSnake.unity, Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity, Assets/TruckHotPotato/Scenes/TruckHotPotato.unity, Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity, Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity, Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity, Assets/TruckSeesaw/Scenes/TruckSeesaw.unity, Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity, Assets/TruckTetherball/Scenes/TruckTetherball.unity, Assets/TruckJousting/Scenes/TruckJousting.unity, Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity, Assets/TruckVolleyball/Scenes/TruckVolleyball.unity, Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity, Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity, Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity, Assets/TruckHideAndSeek/Scenes/TruckHideAndSeek.unity, Assets/TruckFrogger/Scenes/TruckFrogger.unity, Assets/TruckAvalancheEscape/Scenes/TruckAvalancheEscape.unity, Assets/TruckNinjaWarrior/Scenes/TruckNinjaWarrior.unity
