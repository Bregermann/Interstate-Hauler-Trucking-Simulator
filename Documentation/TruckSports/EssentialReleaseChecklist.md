> October19 delivery update: Field Goal follow-up native42/0 and actual Windows42assertions/0errors passed twice, but both processes faulted at native Unity accessibility shutdown. Crowd35assertions/1error exposed a screenshot-path issue, now corrected in source. Combined42-scene builds succeeded; final acceptance remains open. The failed manual cleanup experiment was removed. Vendor-fixed6000.5.5f1 installer is verified, but Windows elevation was canceled; retry approval is pending. Separate public licensed-payload approval is also pending. See FieldGoalStadiumBoard.md and PartyCheckpoint.md. Do not mark the collection complete.

# Truck Sports: Party!  -  active release requirements ledger

Updated October 19, 2026, 18:39 UTC. Reconciled against the preserved chronological `Logs/TruckSports/EssentialRelease/WIP.md`, `PartyMasterLedger.md`, `GoddessPlaytestLedger.md`, `FareFightLedger.md`, queued mode specifications, native receipts and source. An encoding failure damaged the earlier working copy of this index; those independent evidence files remain intact. This index restores the requirement mapping without declaring unfinished work complete.

Statuses describe actual acceptance, not merely script existence. Native means Unity Editor runtime. Injected devices are software devices, not physical controllers. The updated source scene list contains startup plus 41 sports scenes; the last accepted executable still contains the October 18, 35-scene build. Local source preservation commits ff70d09, 9fedc7f, 3203d6b and 62195217 are complete; public push remains pending. The 42-scene combined build began at 21:02 UTC October 19 and is still compiling shaders.

## New sports

| Requirement | Status | Implementation / evidence | Still required |
|---|---|---|---|
| Kickball: physical six-versus-six diamond, innings, positions and running | PARTIAL | `Assets/TruckKickball`; 30 EditMode and 4 PlayMode tests pass. Native complete three-inning match: 66 pitches,64 physical kicks,360 catches,299 throws,198 ordered bases;11 checks/0 errors. | Broader inputs/difficulties, performance, Windows acceptance. |
| Dance-Off: DSP rhythm, original songs/charts, expressive trucks | PARTIAL | `Assets/TruckDanceOff`; seven original53-80s songs,28 charts,28 timing tests; all seven Normal songs52/0; Hard/Insane10/0 each; calibration7/0; mixed sources/replacement/show pass. | Final shared-renderer regression, Windows/hardware/music listening acceptance. |
| Limbo: real lowering suspension, collision bars, eliminations and replay | PARTIAL | `Assets/TruckLimbo`;22 rules tests; mixed and four-injected-human native runs35/0 each; actual natural fifth-round result and render-only recorded review. | Closer replay/coastal dressing saved14:45; native repeat still pending; broad AI/hardware/Windows. |
| Sky Tow: Arctic aerial tow, six sections, real cable/gates | PARTIAL | `Assets/TruckSkyTow`;25 rules tests; Easy/Normal/Hard physical runs20/0 each; recorded flight acceptance24/0;35m ConfigurableJoint cable,144 whole-body gates. | Further environmental/weather/performance, human/hardware/Windows. |
| Trailer Plug: real bungee trailer release and shared pedestrian ecosystem | PARTIAL | `Assets/TruckTrailerPlug`;16 rules tests; four-AI13/0 and mixed18/0; actual impacts, trailer recording; isolated physical seal regression passes. | Repeat latest emergence/carving changes, naturally achieved sealing, wider replay/performance/Windows. |
| Fare Fight: original Taxi district, atomic possession/steals, full NWH | PARTIAL | `Assets/TruckFareFight`;18 rules tests; physical four-AI first-to-three12/0,174 contacts/eight deliveries; four mixed injected sources23/0. | Default first-to-five/difficulties; edge precedence/water/destruction; jug/gesture/dialogue; final HUD/cab/snow/replay/performance/Windows. |
| Truck Snake: real continuous tractor, path-history cargo, jump wave, large world | PARTIAL | `Assets/TruckSnake`;22 EditMode tests. October19 growth update adds one actual starter per participant,64 gates/2051 road candidates/0.5s refill and actual-attached-length score multiplier; see `TruckSnakeGrowth.md`. Native updated starter/long-convoy fixture10/0:58 actual trailers,delayed jump and clean one-trailer reset. Final20ftjump/long-train11/0:55bodies,lowestwheel/hull29.1ft. Naturaldefaultspawncadence10/0:61contacts->62bodies over1.47km. Latest native four-AI three-minute match26/0:20 physical pickups,max12 visible trailers; one survivor2.29km, other runs ended in genuine collisions/blocked routes. Native long-convoy repeat10/0:19 real gold contacts->57 bodies,over1km,delayed eleventh-trailer jump,current NWH collider isolation/shared material cache. | Blocked AI routes and multilevel access; long convoy turns/bridges; complete world collision/art audit; broader modes/weather/inputs/performance/Windows. |

## Goddess feature mapping

| # | Feature | Status and current evidence |
|---|---|---|
| 1 | Universal dedicated horn | PARTIAL: native NWH horn component/recordings audited; shared non-physics input/audio adapter now covers legacy and common sessions, with reserved keys, independent whole/shared-controller ownership, pause/focus stop and Help bindings. Existing Fare/Snake horns retained. Native212checks/0errors:41scene integration,39adapter-scene match starts and real horn playback/release, plus four injected keyboard/whole/shared-stick voices, pause/fresh-resume/focus callback/mute/key conflicts/cleanup. Existing Fare/Snake native horn clips are preserved; real listening/hardware/Windows and final UI capture remain open. |
| 2 | Actual screenshots, original PNG icons, clean menu cards | PARTIAL:40 original icons and actual native opening captures, stable Sprite API slicing; paged rounded Heat/uGUI cards/DOTween. Earlier menu118/0. Add Fare/Snake setup navigation to final acceptance and repeat final rendered layouts. |
| 3 | Shared selectable weather | PARTIAL: shared `SportsWeatherPresets`, picker, vendor Weather Maker/Weatherade adapters. Selected presets integrated in several repaired modes. Every applicable mode/round/rematch/snow-track acceptance remains open. |
| 4 | Universal race countdown | PARTIAL: existing Cheese/Terrier countdowns and several new modes; full race registry audit and consistent release/input acceptance pending. |
| 5 | Cornhole clean hole scores without board contact | PARTIAL: native9/9 authority guards pass; broad smoke also passed actual center passage, once-only credit, physical earlier1->3 push and knock-off checks before unrelated cab-layout fixture failure. Windows repeat pending. |
| 6 | Cornhole physical wind/weather | PARTIAL: saved continuous relative-air drag bounded to air before landing; four-AI five-round80-delivery native game25/0. Further wind-specific/human/Windows checks pending. |
| 7 | Relative wind arrow and MPH | PARTIAL: saved per-view Cornhole arrow uses authoritative flow. Final visual/configuration/Windows repeat pending. |
| 8 | Cheese location indicator contrast | PARTIAL: saved owned TMP outline/underlay material; tracking and compact sizing unchanged. Snow/bright-weather rendered repeat pending. |
| 9 | Bowling obsolete interior | PARTIAL: existing modeled Taxi cockpit and GR3D wheel alignment preserved; additive authoring defaults repaired. Native complete12-AI-game and hot-seat physical regression pass. Final environment visual/Windows acceptance pending. |
| 10 | Pin-only back-wall return | PARTIAL: saved safe solid truck wall and existing pin-only physical return; actual pin-versus-truck PlayMode regression passes. Native12 complete AI games plus hot-seat/physical fixtures pass; Windows pending. |
| 11 | Whole-truck gutters | PARTIAL: saved1.35m channels and real truck-entry regression pass. Windows/full-frame acceptance pending. |
| 12 | Bowling post-shot spectator | PARTIAL: saved pin framing/next-attempt restore; native contact/camera checks pass. Full free-camera/manual/Windows acceptance pending. |
| 13 | Bowling replay | PARTIAL: per-lane real11-root20Hz recording,three angles,slow/fast,source-owned skip,configurable policy. Native23/0; independent other lane/score invariance verified. |
| 14 | Original alley animations | PARTIAL: original expressive vector pins,DOTween strike/spare/gutter/split/near-miss cards; rendered fixtures23/0. Latest pivot refinement requires visual repeat. |
| 15 | Bowling complete acceptance | UNVERIFIED: tyres/venue/sweeper/consecutive frames/all modes/tenth-frame/Windows still require comprehensive repeat. |
| 16 | Dock Diving Canadian bubbles | PARTIAL: sport-owned occasional rounded comic bubbles, DOTween, actual diver projection and bounded cadence; saved14:23UTC. Actual two-AI native match running. |
| 17 | Minefield names attached to flags | MISSING targeted upgrade/acceptance. |
| 18 | Minefield reduced Random snow weights | UNVERIFIED current weighting; audit registry override and manual selection before changing. |
| 19 | Minefield strict gameplay preservation | UNVERIFIED final-rest/explosion/bounds/replay regression. Do not retune mine power. |
| 20 | Terrier giant cherry | PARTIAL: saved original smooth red cherry/stem,collision-free outside landing. Native physical dessert/finish tests pass; final visual/Windows pending. |
| 21 | Terrier volcanic track | PARTIAL: owned Vista basalt/normal surfaces and scene-owned URP/weather rendering restored. Final visual/human/Windows repeat pending. |
| 22 | Terrier air spin | PARTIAL: bounded angular stabilization/aftertouch, fades for genuine impacts. Baseline straight jump did not reproduce wild spin. Native full12-AI32-heat match21/0. |
| 23 | Terrier misplaced scaffolding | PARTIAL:20 repetitive staircase pylons removed; defaults corrected. Final visuals/Windows pending. |
| 24 | Terrier final-only funnel | PARTIAL:68m wide until final hurdle, continuous matched floor and six rails; actual physical geometry rays and full match pass. |
| 25 | Terrier jump momentum | PARTIAL: no new linear force/clamp; controlled actual takeoffs retain>98% forward speed. Full match passes; reported human loss not reproduced by straight fixture. |
| 26 | Terrier acceptance | PARTIAL: full12-Hard-AI32 heats21/0; centered/angled/light edge/post-finish/high-speed/solid slam15/0; winner cream/loser lava/fragments13/0. Final world-label visual and Windows repeat pending. |
| 27 | Mattress finisher spectator | PARTIAL: same-camera live-leader spectator verified by PlayMode fixture. Full race/Windows pending. |
| 28 | Mattress weather continuity | UNVERIFIED reported root cause: initialized early-finisher Rain/clock stayed active. Shared weather selection saved; complete race/rematch repeat needed. |
| 29 | Beer Pong AI pacing | PARTIAL: saved Normal/Fast-forward/Simulate actual fixed-step PhysX; native61/0 and complete2v2 match27 launches; same-commit paired physics parity regression passes. Windows pending. |
| 30 | Musical Parking first person | PARTIAL: selectable Arena/Chase/Taxi Cab,free look/steering/Show AI; one-to-four injected-human/AI-only/physical steering tests pass. Full matches/Windows/hardware pending. |
| 31 | Wife Carrying floating rocks | PARTIAL: actual cluster mesh/terrain support and13 static MeshColliders saved; physical upward-face contact tests pass. Original center rays had missed hollow cluster centers. |
| 32 | Wife checkered finish | PARTIAL: terrain-fitted markings,timber posts,banner,world TMP at actual310 progress. Native tests/captures; Windows pending. |
| 33 | Wife competitive AI | PARTIAL: real wheelbase pure pursuit,curvature/weather braking/recovery. Four-Hard-pair three-race baseline63.92-70.96s->44.20-49.22s,12/12 finishes each. Updated Easy/Normal12/12 each; no drops. Latest rock-dressing Hard repeat pending. |
| 34 | Curling10ft red/white wall paint | PARTIAL: exact3.048m collision-free sections saved; native physical regression passes. Full matches/replay/Windows pending. |
| 35 | Curling target20% closer | PARTIAL: start-30,old tee156,distance186->148.8m,new tee118.8; dependent markings/AI/camera/defaults moved;234m sheet preserved. Native bank/target regression passes. |
| 36 | Fishing offscreen spawn | PARTIAL: eight-second upstream approach and actual model-frustum checks across16:9/16:10/4:3/ultrawide; downstream speed/species/ramp preserved. Native20/0. |
| 37 | Fishing unwanted catches | PARTIAL: actual owned tyre/canister/rock pooled alongside original fish; no fish score weight or duplicate catch. Native and rules tests pass. |
| 38 | Fishing local clearing QTE | PARTIAL: four fresh alternating directions,six-second fair recovery,Pixel Crushers UI/DOTween; no global pause/other-stream penalty. Native20/0 includes complete4-AI280.22s survival; two rules tests include10,000 schedules. Final HUD capture/Windows/hardware pending. |
| 39 | Bog additional Hard course | PARTIAL: saved650m/six submerged galleries/13 checkpoints/14s air budget; Classic350m preserved. Native9/0:12 AI finishes,zero recoveries. |
| 40 | Bog meaningful oxygen | PARTIAL: actual slow throttle depletes/penalizes/restores checkpoint, while skillful AI finishes with minimum1.78s air. Native physical regression passes. Windows/human acceptance pending. |
| 41 | Sumo front costume | UNVERIFIED current authored attachments; preserve scoring. |
| 42 | Target Launch limited wings | UNVERIFIED existing air-control integration, final-rest/lava/replay acceptance. |
| 43 | Field Goal populated stands | PARTIAL: saved 2304 actual Taxi/UTS spectators, 32 live animators and 2272 shared instanced posed fans. Native 33/0 includes actual goal feedback, fixed renderer count across three rematches and inspected 720p/1080p renders. Sustained Windows workload/GPU/hardware acceptance pending. |
| 44 | Field Goal dynamic reactions | PARTIAL: actual legal physical kick triggers one cheer response; controlled jump/scuffle/jug fixtures, bounded pooled props and pause/reset invariance pass native 33/0. Windows and subjective audio acceptance pending. |
| 45 | Rare Field Goal pitch invader | PARTIAL: configurable 4.5% post-locked-result referee-truck vignette activates the existing Taxi jointed ragdoll. Forced physical presentation, score invariance, pause and cleanup pass native 33/0 plus collection 28/28. Natural rarity/human/Windows acceptance pending. |
| 46 | Poker mobile dealer | PARTIAL: saved continuous route,shuffle/reach16 face-down handoffs,real wheel animation,Cinematic/Quick/Skip. Native22/0 includes four-AI Easy/Normal/Hard tournaments and800-chip conservation. Closer view13/0 and focused privacy/input regression pass before new restaurant room; post-room repeat pending. |
| 47 | Preserve working features | PARTIAL: latest native Goddess25/25 passes18:11:37UTC, including physical Bowling/Curling/NWH/Parking/Terrier/Bog/Pong/Wife/Fishing/Poker/Snake regressions. Full collection regression remains required. |
| 48 | Actual runtime acceptance | PARTIAL: receipts above are real native runs, not hardware or Windows. All sports full matches/rematches/visual/performance acceptance remains open. |
| 49 | Dependency-aware priority | ACTIVE: missing gameplay/new sports and actual defects precede decoration. Existing authoring defaults must preserve all changes. |
| 50 | Accurate final report | ACTIVE: preserve per-sport evidence,limitations,build identity,commit hashes,push outcome and vendor accounting. Never label this backlog complete while these rows remain open. |

## Other prior requirements still active

- Cheese: preserve actual grounded suspension settling, high flat platform, gentler continuous terrain/alternate paths, earned boost, all16 starters, repeated rematches, scenery/weather/cameras. `CheeseStartProbe` already contains a12-start real NWH reproduction; rerun before changing working initialization.
- Giant Swing: inspect existing terrain-aware replay clearance and run extreme/low-flight fixtures before editing; full swing/cable/release/scoring/reset/environment validation remains open.
- Minefield: confirm enlarged world, final stopped forward scoring after all explosions/bounces, flags, wind, bounds, weather and replay against original ledger.
- Mattress: existing physical colored pillows must be retained; inspect current terrain/knockable trees/rocks and complete race/free-camera/weather/rematch tests.
- Hockey/Air Hockey: preserve six participants and coherent PhysX response; verify enlarged/slippery ice, wall interactions/goals/own goals/overtime/AI/recovery and every input ownership combination. No second full-strength collision impulse.
- Keepaway: preserve enlarged rounded arena, selectable Arena/Taxi cab/chase views, bottom scores/CNP, physical sheep/props and duplicate-name-safe possession; complete AI parking/objective regressions.
- Billiards: preserve established shot/collision/rules/AI accuracy/continuous held driving; verify true rolling sphere visuals/pocket animation/group choice/cue placement/AI-only/series/rematch.
- Pedestrian Plowing/Cab Chaos: preserve neutral character appearances, existing shared crowd/Taxi ragdoll rendering and nine-block environment; native smoke/Cab/physical regression currently0 errors. Older requests to facilitate demographic-targeted reward scoring are not implemented. General performance/navigation/replay/lifecycle repairs remain in scope.
- Audit Buzkashi, Derby, Bungee, Tug of War, Skee Ball, Log Rolling, Dodgeball, Base Jumping, Wallball and every other explicit build scene against `PartyMasterLedger.md` and original queued specifications. An opening/menu entry is not full acceptance.
- Universal shared identities/duplicate names,controller assignment/reconnect/wheels,Show AI Cameras defaultOFF,cab/free-look,weather/snow/tracks/haptics/audio,screen-safe rounded/tweened UI and one-listener cleanup remain cross-mode requirements.

## Verification and delivery boundary

Native Snake long-convoy repeat:19 actual gold contacts->57 visible bodies,over1km,delayed eleventh trailer jump,110k+ sweeps,current wheel/hull hybrid contact and material cache checks pass.1,467 Editor frames/37.73real seconds is not a GPU benchmark, Windows performance result, or curved/bridge traversal acceptance. Four-AI three-minute run12/0 includes10 pickups,max6,eight jumps and clean lifecycle, but blocked route crashes remain a real navigation limitation.

Only use `LWS.TruckSports.Editor.SportsBuilder.Build` for final Windows x64 development output at `Builds/TruckSports/TruckSports.exe`. Do not call `CreateValidateBuild`; it reauthors saved scenes. Preserve host settings on success/failure. Keep one canonical final build in the project; no C-drive build farm. Final acceptance must launch the actual executable and exercise meaningful matches,menus,rematches,errors and performance.

Git: local root `main` includes private Taxi ancestry and unrelated protected WIP. Public baseline `b76cad205539f4e8abe55d9d2a69be7ed3382b53`; do not push private root ancestry wholesale. Audit scoped related assets/scenes/metas/documentation and clean-checkout dependencies, commit safely on the established public delivery branch, normal push, verify remote hashes. No force push or unrelated staging.

Vendor foundations audited/used: NWH vehicle and wheel physics, Heat/uGUI, DOTween, Cinemachine, Weather Maker/Weatherade, Compass Navigator Pro, Vista/EasyRoads, UTS/Taxi models/cabs/traffic and owned environment models. LWS owns sport semantics/adapters/coordination/recorded-event presentation. No vendor or original Taxi source modified by this continuation; final report must include all six required vendor-accounting headings.

## October19 continuation: Derby and Buzkashi

Derby collision-only damage multiplier2, outward starts, initial AI reverse and contextual warnings are saved. Actual8-Hard first-to-three match:6heats,873 qualifying contacts,42explosions,0draws,0errors, clean rematch. Expanded geometry reproduction failed all four transverse bank approaches; matching seven-metre shoulders fixed those same cases:22checks/0errors. The full match preceded the final shoulder repair; repeat full match/Windows remains open. See `DerbyGoddessRepair.md`.

Buzkashi captions use compact outlined labels and separate carrier icons. Full native five-minute six-AI game, rematch and menu exit:11checks/0errors. A first failed run exposed and fixed a real cross-mode null-owner dereference in reused BowlingCabCamera;24/24Goddess PlayMode tests now include an actual Buz camera render. Live and deliberately posed paused-cluster captures were inspected.

Poker modeled room source is additive around the unchanged table/cards/dealer; outdoor Weather Maker renderer installation was inappropriate for this enclosed scene and has been removed. Native save/view/privacy acceptance is still pending.

No new Windows build, commit or push has been delivered by these updates. All unverified and missing rows remain open.

Preservation check16:29UTC: all25 host-setting files are byte-identical to this continuation baseline;886/887 protected pre-existing paths are identical. The only changed protected path is the already-dirty generated TMP fallback font cache, excluded from delivery. No unrelated source has been reverted or staged.

## Accepted crowd/capture evidence

Field Goal crowd focused native run at16:30UTC:22checks/0errors, actual physical goal, one reaction per locked attempt, rare runner explicitly forced, pause/route continuation, three rematches and cleanup. Readable720p gameplay image inspected with real GOOD33-yard result and wind HUD;1080p also captured. The424-fan frustum fixture uses a separately controlled overview and is not the pixel count in the gameplay screenshot. The capture utility now uses isolated black/white UI mattes and linear color composition, waiting for DOTween transitions. Earlier black/fading captures are retained as rejected evidence.

Controlled paused Editor observation:60 Main Thread samples per case,12.77ms crowd-on versus10.07ms hidden in one earlier run. Draw Call counter was unavailable. This is not a sustained benchmark, an isolated GPU measurement or a Windows performance result.

October19 validation correction: Cornhole replay consumed previous-frame time after mid-frame rewind. A deliberate180ms-hitch PlayMode test reproduced Clock0.389791 immediately after rewind. Monotonic action-time anchors now exclude that carry-over; the same focused test and all25 physical regressions pass. Layout harness is being updated to validate the actual five-part Taxi cockpit and its deliberately detached free-look hierarchy; earlier stale placeholder-count failures are retained and are not successful layout evidence. No new Windows build, commit or push.


## October19 current stadium and recovery update

Field Goal source now has a goal-line anchored120-yard field with the ten-yard end zone terminating at the uprights, an8.5m windsock, a modeled behind-goal scoreboard and embedded live stats/wind/actions. Native30checks/0errors at19:56:48UTC: real complete4AI match, five distances, Heat replay controls, long-name layouts, two rematches/pause/menu. See `FieldGoalStadiumBoard.md`. A dedicated PlayMode regression is included; complete collection suite and Windows acceptance are pending. The actual gameplay menu screenshot has been refreshed.

Cornhole replay-clock and Weather Maker lightning camera/worker teardown fixes passed26physical PlayMode tests before this stadium update. Corrected Cornhole layouts passed485checks across20layouts; the broader583-check run completed its physical match but was rejected for a lightning error before that shared fix. Its clean repeat is still required. No natural AI hole was observed in that run; physical centered-hole fixtures passed.

Source preservation: root local `ff70d09db7931ee381a147efd4e6175ab7689815` saves the reviewed expansion; `9fedc7ff1b506e553d133e36c22ed8680d998f06` saves the stadium update. Those commits retain private root ancestry and are NOT the public delivery commits. A clean public-base validation source inside the project compiles fresh. Current source has not yet been built/pushed. The3:45PM Windows reboot interrupted one run; source text/scene integrity was verified and that run remains rejected.

## Added 2026-10-19 — Original logos throughout the collection
Status: QUEUED after Field Goal and existing gameplay/repair priorities.
User requests a distinct original logo for EVERY sport, matching the new Truck Field Goal hand-lettered retro party-game theme. Prominently integrate each in sport-selection cards/configuration and logically appropriate world signage, boards, arena paint and venue dressing. Preserve actual gameplay screenshots, navigation and readability. Generate/audit/inspect original transparent assets, use existing Heat presentation, validate every registered sport and record per-logo integration/evidence. This is not complete merely because a menu icon exists.

Field Goal October19: native dense-crowd/referee repeat33checks/0errors after fixes to scene-exit Animator null handling and preserved NWH render-subtree offset. Actual legal physical kick, bounded eight-jug pool, forced antics/referee fixtures, pause, three rematches and menu cleanup. Controlled720p/1080p renders inspected. Saved scene2304fans/32liveanimators. Field/end-zone/native full-match acceptance running; Windows update and public push still pending.
