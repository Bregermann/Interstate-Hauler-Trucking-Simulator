# Pedestrian Plowing update — October 18, 2026

Two saved menu entries share one sport: **Pedestrian Plowing** (arena/chase/cab) and **Plowing · Cab Chaos** (full Taxi driver interior, independent live GPS, animated wheel/free look and jug challenge). Up to eight competitors, four local humans, solo and AI-only play use the existing offline lifecycle. The finite crowd occupies nine connected city blocks with burning buildings, flipped burning cars and festival dressing. Character appearances remain independent of points.

Windows x64 Development executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep its entire accompanying folder. Built through `LWS.TruckSports.Editor.SportsBuilder.Build` at2026-10-18T03:52:00.8606738Z: zero errors,47 warnings,1,246,555,118bytes,1m46.245s. Managed assembly SHA256 `e48749992b233aa8f5c061cccb596b3ac9aa768991a87fe3ebfd8e2f581896bf`; build GUID `90f93087e950471c9864a98c5e0f51e5`. All25 protected settings restored; deliberate failure restoration passed. All35 explicit Sports-only scenes and TruckSports startup are retained. Generated builds are excluded from Git.

Eight final Windows suites completed with zero captured runtime errors:384-crowd density, LOD/accessories, actual contact scoring/replay, cab/input/jug/reconnect, smoke/rematch,720p/1080p view layouts and two60-second four-view performance samples. Twelve earlier baseline suites include six complete eight-AI matches, five legacy cab modes and the35-scene/five-aspect tour. These are version-bound automated/injected-input/real-PhysX results, not human or physical-controller testing. Chase/cab mean frame times were29.58/29.28ms; performance is specific to this PC with user applications left running.

[Implementation, evidence and manual limits](../../Documentation/TruckSports/PedestrianPlowingValidation.md) · [Crowd/model/spawn customization](../../Documentation/TruckSports/PedestrianPlowingCustomization.md) · [Checkpoint and ordered outstanding queue](../../Documentation/TruckSports/PedestrianPlowingCheckpoint.md) · [Vendor audit](../../Documentation/TruckSports/PedestrianPlowingVendorAudit.md).

Git publication is being completed separately; historical receipts below apply only to their named milestone. Party/Cheese, Show AI Cameras, Fare Fight and Goddess repairs remain outstanding and are tracked in their ledgers. Human/controller, haptics, audio balance and subjective gameplay acceptance remain manual.

## Historical milestone documentation

# Truck Sports master repair — October 17, 2026

All ten requested repair packages are implemented, saved and validated. The combined Windows x64 Development build contains all 31 implemented sports in 33 explicit Sports-only scenes. TruckSports starts first; the normal catalogue has 30 entries and Wallball remains manually reachable in Experimental Scrap Heap.

Executable: `F:/Unity Projects/Interstate-Hauler-Trucking-Simulator/Builds/TruckSports/TruckSports.exe`. Keep the complete accompanying folder together. Build `2026-10-17T10:53:04.7155517Z`, zero errors / 47 incremental warnings, 1,191,046,536 bytes, 00:01:39.5986327. Managed assembly SHA256 `1ded15c3f1629166224a67d36d754d2d821b22a8cdf587fbfdecdf42efa9bf4d`. All 25 protected ProjectSettings files match their pre-build hashes, including the host build configuration; the deliberate failure-restoration fixture also passes.

The ten-mode repair updates Keepaway sheep/cameras, Cheese's giant hill, Swing replays, Curling's longer controllable sheet, Terrier's winner landing/volcano, Minefield's landing world, Mattress pillows, Parking recovery, Bungee damage/ten buildings and Poker faces. Existing sports, offline participant setup, named/color slots, whole/shared input ownership, pause/reconnect and rematches are retained.

Controls remain in each sport's setup and pause/help pages. Normal billiards turn changes keep held movement uninterrupted. Use SportsBuilder.Build; preserve the explicit scene list and exact host-setting restoration. Original licensed vendor prerequisites are required for a source checkout and are not republished.

[Repair causes, Windows results, images and manual checks](../../Documentation/TruckSports/MasterRepairValidation.md) · [Build](BUILD-INFO.md) · [Checkpoint](CHECKPOINT.md). Verified Git delivery is recorded below.

## Earlier collection milestones (historical)

# Truck Sports — eleven offline sports



## Four-event expansion — 2026-10-15



The combined collection now includes eleven playable sports in twelve explicit sports-only scenes, starting at TruckSports. New events: [Dock Diving](../TruckDockDiving/README.md), [Keepaway](../TruckKeepaway/README.md), [Curling](../TruckCurling/README.md) and [Base Jumping](../TruckBaseJumping/README.md). All use the existing offline participant setup, names/colors, whole/shared controller ownership, keyboard clusters, independent AI, shared settings, pause/reconnect, results and rematches.



Wallball supports uneven teams of1–4 with a fresh ball-only contact envelope and coordinated interceptors. Bowling has a physical overhead sweeper with proper lower/back/return/raise/replace phases. Ice Hockey adds capped pre-impact knockback/stopped counters and articulated, puck-only, wall-aware sticks. Existing handling/scoring, Cornhole cab views/replays and Cheese Rolling features are preserved.



The truck mesh authoring offset and repeated Weather Maker scene-transition leaks found during validation are fixed. Dock supports human-owned slow/fast multiangle replay without an automatic timeout once manually controlled; solo records use the same whole-inch score as display/ties. Base uses a compact colored per-view HUD and bounded bloom so canopies and trucks stay visible.



Build through `LWS.TruckSports.Editor.SportsBuilder.Build`; never use `CreateValidateBuild` for delivery because it reauthors older scenes. See [validation](VALIDATION.md), [build information](BUILD-INFO.md), [checkpoint](CHECKPOINT.md) and [approved dependency delivery](../../Documentation/TruckSports/ExpansionDependencyRepair.md). The normal-company Windows x64 Development build is complete and passes startup/input, physical new-event matches and repeated collection navigation. The explicitly approved dependencies are committed and pushed as `12f0653faae7290f0e6895097e9ff68ce06447b3`. Current source and the single final delivery are in the main F: project; see [delivery policy](../../Documentation/TruckSports/BuildDeliveryPolicy.md). A fresh remote clone/build was not repeated after publication. Earlier milestone descriptions below are historical.



## Hardcore Truck Cheese Rolling — 2026-10-15



The seventh sport adds one to four simultaneous marked racers plus twelve non-scoring background trucks, a physical rolling cheese wheel, a steep grass hill and muddy runout. First genuine participant contact earns one catch; first to five wins, with a timed solo challenge. Clear/blizzard/hurricane rounds reuse Weather Maker. Actual explosive sheep launch trucks without stopping their momentum.



Each round reshuffles the full-width start grid and randomizes trees, rocks, cats, wolves and tall-grass patches. A dramatic 3 / 2 / 1 start rewards a fresh accelerator press during 2, held through GO, with one physical jump boost. Trees have breakable physical roots and can be knocked down by trucks and sheep explosions. Taxi's exact mushroom world shader is reused with a sport-owned animated sky, pipeline and grading; pause offers full / low / off. Existing sports retain their tuning.



See [Cheese controls and rules](../TruckCheeseRolling/README.md), [validation](../TruckCheeseRolling/VALIDATION.md), [sources](../TruckCheeseRolling/SOURCES.md) and [build information](../TruckCheeseRolling/BUILD-INFO.md). The combined saved-scene builder now contains eight explicit sports-only scenes with TruckSports startup. Do not use CreateValidateBuild for delivery.



## Truck Cornhole — 2026-10-15



Cornhole joins Billiards, Ice Hockey, Bowling, Air Hockey and Wallball. One to four simultaneous courses launch the truck itself from a physical trampoline onto an inclined open-hole board. Five rounds × four deliveries use provisional board points, complete downward hole credit and cumulative free-for-all totals. Existing sports and their tuning are preserved.



Each Cornhole player starts in the driver seat. A higher ballistic arc gives roughly six seconds of readable flight, with limited aftertouch and fully physical landings. Per-view recorded replays offer cab, chase, board-side and overhead angles, 0.25×–4× speed, rewind and return, while other courses continue. Shared setup/source ownership, audio, steering settings, haptics and modal input policies follow the collection conventions.



See [Cornhole controls and scoring](../TruckCornhole/README.md), [validation](../TruckCornhole/VALIDATION.md), and [build information](../TruckCornhole/BUILD-INFO.md). The current explicit combined build contains seven scenes, starts at TruckSports, and uses `LWS.TruckSports.Editor.SportsBuilder.Build`. Legacy scene-authoring hooks retain the sixth menu entry. Exact host settings restore on success and failure. Earlier five-sport polish details below are retained as history.



## Focused polish — 2026-10-14



Bowling now separates steering/braking authority from a live delivery's Rigidbody motion. Sideways, backward and gutter slides continue toward the pit. NWH remains the tire/suspension authority; the shared bounded forward carry reads the world lane, grounded wheels and a clear obstacle query, never pin targets. AI uses earlier alignment, pocket-side rack lines, reachable spare clusters and smaller late corrections with distinct difficulty errors/reactions. The retained score/rack and cab/split-screen systems remain authoritative.



Billiards uses smooth Unity spheres with curved UV numbers/stripes, URP Lit gloss and soft contact shadows. A collider-free visual child rolls by actual interpolated displacement/radius. Existing frozen physical rotation, radii, materials, shot power, rules and AI remain intact. Pocket scoring disables physical contact immediately; DOTween lowers only the visual, with no turn delay. Rack and cue placement reset the presentation.



Billiards, Ice Hockey, Air Hockey and Wallball fit their complete fixed arena envelopes to the current aspect ratio. Compact screen-aligned panels follow rails/boards/court edges. Six hockey trucks use B1–B3/O1–O3 identifiers and two named roster columns. Full controls remain in pause/help; clocks, turn, groups, remaining balls, server/eligibility, bounce/foul messages and series progress stay visible. Wallball changes are presentation only. Its colored names stay in the HUD rather than covering truck roofs. Bowling cameras are unchanged.



Use `LWS.TruckSports.Editor.SportsBuilder.Build` for the combined Windows development build, from saved scenes. `CreateValidateBuild` regenerates scenes; do not use it for ordinary validation. Creation and legacy upgrade defaults call the focused presentation authoring so regeneration retains this update. The explicit six-scene build does not change the host scene list. Product/company overrides restore exact host bytes on success and failure.



Development probes can use `-tsValidationPrefs` and `-tsBuildOutput` inside this project's Builds directory. This keeps automated setup/preferences separate from a user's running copied build. Final builds omit both flags and retain the normal preference namespace. See VALIDATION.md for physical, software-device, full-game, camera and performance evidence and remaining human checks.





Wallball adds a physical 1v1 front-wall court with grounded jumping, eligible-only contacts, first-to-11/win-by-two scoring, named human/AI slots and full local input setup. Space / Right Shift / controller South jumps; shared pads use LT and RT independently. See ../TruckWallball/README.md and VALIDATION.md for controls, vendor ownership and current evidence. Billiards, Bowling, Ice Hockey and Air Hockey gameplay/tuning remain unchanged. Final Windows builder is LWS.TruckSports.Editor.SportsBuilder.Build with six explicit saved scenes and exact host-setting restoration.



The four-sport and earlier reports below are historical; the Wallball validation report identifies the current build and delivery.



# Truck Sports — four offline sports



2026-10-14 update: larger, more slippery 3v3 Ice Hockey with strong coherent physical hits, plus first-to-7 Truck Air Hockey. Billiards and first-person Bowling source/scenes/tuning are preserved. Start at TruckSports; five-scene Windows development builder: LWS.TruckSports.Editor.SportsBuilder.Build. Output Builds/TruckSports/TruckSports.exe. Keep the complete generated directory together.



Air Hockey uses the original 24 × 12 footprint, two confined trucks without stick attachments, radius-2.5 rounded corners, freely crossing low-drag puck, crisp rails, conceding-side serves and alternating rematch opening. All local keyboard/controller/shared-stick combinations, named human/AI slots and independent difficulty are supported. See ../TruckAirHockey/README.md for exact controls/rules/tuning/vendor APIs and VALIDATION.md for current evidence/manual limits. Hockey's enlarged footprint is 34.8 × 17.4; its README documents independent ice tuning. Prior Bowling and 3v3 reports below are historical build metadata.



# Truck Sports



Truck Sports is the existing local Windows collection in Interstate: Hauler. Start with Assets/TruckSports/Scenes/TruckSports.unity. The development executable is F:\Unity Projects\Interstate-Hauler-Trucking-Simulator\Builds\TruckSports\TruckSports.exe; keep its entire build payload together.



TRUCK SPORTS > Play > Billiards Match, Billiards Practice, Hockey, Bowling, Air Hockey or Wallball. Existing title, settings, navigation, scene transitions and DOTween results remain. Shared Master/SFX/steering/display/vibration preferences retain the TruckBilliards.Settings.* keys. Each sport saves its participant setup separately. UPDATE_3V3.md records the previous update. Current Bowling and combined-build evidence is in ../TruckBowling/VALIDATION.md; VALIDATION.md preserves the earlier three-scene evidence.



## Bowling: the truck is the ball



Bowling adds a saved fully 3D alley and first-person tiny-truck cab view. NWH WheelController 3D owns tire/suspension physics. The approach is grippy; increasing oil reduces lateral correction and post-commit power/braking so entry momentum dominates. A configurable lane-edge assist reads only position/velocity and allows genuine gutters. There is no ball, throw or shoot button.



Simultaneous play uses 1–4 permanent participant/lane assignments and independent split-screen deliveries. Hot seat uses 1–8 on one lane, a whole frame per participant including tenth-frame bonuses. Any human/AI mix; named per-slot AI difficulty, shared/whole/stick/keyboard sources, explicit ownership, persisted setup/rematches and owner-only dialogs. AI drives physically through the same controls and advances all-AI matches without confirmations. Shared human hot seat announces and confirms the handoff.



Ten frames, standard strike/spare/tenth bonuses, pending cumulative totals, maximum 300 and ties. Crossing the foul line is legal for the truck. Actual stable pinfall is locked before visible lifting/sweeping/setting. Survivors retain their positions for second deliveries; fresh racks follow bowling rules. Gutters cannot return to score, slow/overturned/stuck deliveries resolve, and all timers/machinery pause without consuming time. Final standings wait for all lanes and show final totals.



Keyboard slots: W/A/S/D; arrows; T/F/G/H; I/J/K/L (simultaneous); remaining hot-seat presets Z/X/C/V, numpad, number row and navigation keys. Shared hot seat uses W/A/S/D. Whole pads use left stick/RT/LT; shared sticks use X steering and Y drive. Opposite throttle brakes before reversing. Escape/assigned Start pauses, Tab opens the full scorecard. Reduced Camera Motion is available. Shared Master/SFX/vibration and one AudioListener are retained.



See ../TruckBowling/README.md for complete rules, controls, tuning and vendor paths; ../TruckBowling/VALIDATION.md for physical/injected-device evidence, sustained matches and manual limits. Offline only; no online services or powerups.

## Hockey: three per team



Six editable participants support every human/AI mixture, including six AI or six humans. Each has a name, source, independent AI difficulty, team color and B1–B3 / O1–O3 marker. Click a source to choose AI, Keyboard, WholeController, LeftStick or RightStick. Presets include Keyboard Chaos, six individual controllers, three shared controllers, AI-only and Custom. Default match is three minutes; two and five are available.



| Slot | Forward | Left | Brake / reverse | Right |

|---|---|---|---|---|

| B1 | W | A | S | D |

| B2 | T | F | G | H |

| B3 | I | J | K | L |

| O1 | Up | Left | Down | Right |

| O2 | Z | X | C | V |

| O3 | Numpad 8 | Numpad 4 | Numpad 5 | Numpad 6 |



Keyboard Bindings edits each cluster. Active duplicate gameplay keys and Escape are rejected. Input Check highlights the received controls for all six participants. Test simultaneous combinations on your physical keyboard: software routing does not remove hardware rollover/ghosting limits.



Whole controller: left stick steers, RT drives forward, LT brakes/reverses. Shared controller: each stick owns a separate truck; stick X steers and Y supplies vehicle-relative forward/brake/reverse. A whole pad is exclusive; its left and right sticks may otherwise belong to different participants. Duplicate sticks are rejected. There is no artificial four-slot limit; actual USB/backend capabilities still require hardware testing. North joins vacant pad sources. Preset source changes clear incompatible assignments; explicit Reassign replaces an owner.



Opposite throttle first brakes longitudinal motion, then drives the other direction after stopping. The same controller applies this to keyboard, pads and AI. Neutral coasts. Hockey has no required dedicated brake, shooting, pass or boost button. Escape / assigned Start pauses; an AI-only spectator can use any pad's Start. Pause provides six individual recovery buttons; a whole-pad owner can also use North. Recovery selects an unoccupied own-half position, checks all five other trucks, clears that truck's stale contact record and preserves the puck, score, time and other trucks. Cooldown is two seconds.



Disconnect pauses without losing assignments. A shared-pad disconnect identifies both owners. Reconnect the same pad or explicitly reassign, then Resume; both stick owners must release and apply fresh input. Setup, names, difficulties and sources persist across rematches and menu returns in TruckHockey.Setup.v2.



## Hockey rules, physics and AI



The retained team rules handle completed-step full-puck goal crossing, own goals, exactly-once scores and deadline fractions. The live-only regulation clock stops during pause, goals and faceoffs. Ties enter sudden death; the first overtime goal wins. Rematch keeps all six slots and swaps team spawns, attack direction and crease ownership. Restart preserves setup/current ends and resets scores/time.



All six upright truck bodies and the flat puck use one PhysX contact model. No additional custom collision impulse is applied. CCD, speed bounds and depenetration limits remain. Relative impact speed controls restrained audio, short pooled sparks and affected-pad rumble; settings and pause/unload stop motors. Rounded boards and truck-only goal barriers remain.



AI roles change between challenger, support and defensive coverage, with a challenger preference/hysteresis and spaced targets. Each AI uses its own Easy/Normal/Hard reaction, prediction and error profile, and the same physical driving component. Blocked AI reverses and retries, without teleporting or applying remote puck forces.



Recovery distinguishes an accessible stationary puck or ordinary blocks from invalid geometry or sustained trapping. A crowd pin requires prolonged stillness, multiple pushing contacts, and no sampled escape direction. Out-of-rink/embedded pucks announce a score/time-preserving faceoff. Contact journals contain six participants; 64-entry overlap queries fall back to a complete query on saturation.



## Billiards update



Both slots have editable names (including AI), independent source/difficulty and colors. Human/human, human/AI and AI/AI coexist with the retained Practice, hot seat, best-of-3/best-of-5, audio, rumble and celebrations. Setup persists in TruckBilliards.Setup.v2. AI-only placement and group choice are automatic; between-rack series results advance after 2.5 seconds, while the final series result stays visible.



Human held driving and momentum continue across ordinary turns and shot settling. Bumpers remain eligible only for the active shooter, once per shot. Ghost trucks do not obstruct balls or players. Pause, placement, group choice, hot-seat handoff and reconnect deliberately require fresh input.



AI no longer forfeits for navigation failure. It clears stale plans after placement/retained turns, reverses and repositions, then relaxes the desired contact direction after bounded failed maneuvers. It must drive its bumper into the cue, even for a poor or foul shot. Hard aim/power/quality parameters remain unchanged. Off-turn AI cruises the perimeter with restrained reactions; modal screens stop it and an active turn starts fresh planning.



The actual opening bumper contact multiplies cue speed by configurable openingBreakBoost (1.65), capped at maxBreakShotSpeed (24), above the ordinary 15 limit. Both humans and AI use that rule. It is consumed once and resets for a new rack. Rack scattering comes from ball physics. Swept pocket detection complements CCD; break-8 respot is retained. A brief break sound/visual/rumble cue marks the strike.



On an open table, one legally potted group assigns automatically. If the same legal shot pots both solids and stripes, the shooter chooses after settling; both pots stay down and the turn is retained. Scratches and illegal shots do not choose or assign. This simplified policy also applies on the opening break. F/G chooses for WASD, Right Ctrl/Right Shift for arrows; the owning controller selects and submits with South, or the human clicks. Opponent pads cannot confirm. Pause and disconnect preserve the choice.



## Build and future authoring



Final builds use LWS.TruckSports.Editor.SportsBuilder.Build from the saved scenes. It validates and supplies a build-specific explicit array: TruckSports, TruckBilliards, TruckHockey, TruckBowling, TruckAirHockey, TruckWallball. Truck Sports branding is scoped, with exact host PlayerSettings and EditorBuildSettings restoration on success and failure. Original host/Truck Taxi configurations remain untouched.



SportsUpdateAuthoring.UpgradeAll is the explicit owned-scene migration. SportsBuilder.CreateValidateBuild reauthors the front end and hockey scene; inspect it before use on hand-tuned scenes. Updated authoring defaults now produce six trucks and the current setup. BilliardsMenuBuilder's explicit recreation also applies the current Billiards upgrade. The existing Billiards-only builder remains.



Opt-in development probes: -tsBilliardsUpdate, -tsBilliardsAI, -tsHockeyUpdate (also -tsSmoke), -tsSixAI; retained feature/local probes use -tsBilliardsFeature -tbFeatureSmoke and -tsBilliardsRegression -tbLocalSmoke. Normal launches run none. Probes distinguish initial layouts/rule fixtures from actual physical execution and restore injected devices/preferences.



Future sports must provide editable named human/AI participants, per-slot AI difficulty, clear identity/input ownership, persisted rematches/menu returns and autonomous normal AI-only gameplay. Bowling is the only additional sport in this update; no playlist or speculative framework is implemented.



## Vendor report

VENDOR ASSETS AUDITED: NWH Vehicle Physics 2 readme, VehicleController and ArcadeModule; NWH/GR3D SemiTruck prefab; UTS_FullPack CarAIController; Heat ButtonManager/SliderManager/InputFieldManager and audio; existing Billiards audio/input/settings; DOTween readme/APIs; installed Input System 1.19 and UI action asset.

VENDOR ASSETS USED: Assets/NWH/Vehicle Physics 2/Vehicles/Euro Truck by GR3D/SemiTruck.prefab render geometry; Assets/Heat - Complete Modern UI controls and Audio/Click.wav; DOTween Assets/Plugins/Demigiant/DOTween/DOTween.dll (DOTween.To, DOScale, SetUpdate, Kill); Input System default UI bindings copied into Assets/TruckSports/Input/SportsUI.inputactions; uGUI/TMP/URP and retained Billiards control/visual/setting conventions.

RELEVANT ASSETS NOT USED: NWH powertrain/wheel ArcadeModule needs grounded road-wheel dynamics, not the constrained planar sports rink/puck model; UTS relies on road MovePath/CarMove and does not supply hockey interception/goal-side positioning. Wheel/DirectInput and world authoring systems do not supply these sports mechanics. Audited environment audio has no hockey horn/whistle set; three original procedural cues were authored.

CUSTOM SYSTEMS CREATED: Hockey team rules/simulation timing, planar contact/control adapter, six-participant input ownership, coordinated roles, recovery and device coordination; shared front end/settings/input/feedback adapters, scene authoring and focused validation. Existing Billiards rules are not reused by Hockey.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.



Musical Parking: eight actual trucks, up to four humans, random music/horn, seven-to-one eliminations. Full-footprint continuous stable capture, protected secured trucks, visible anti-camping progress and deterministic bounded timeout. Shared persisted Music volume; same source/reconnect policies. See Assets/TruckMusicalParking/README.md and VALIDATION.md. Native three complete Easy/Normal/Hard matches and focused checks pass; final Windows build pending.


Giant Swing: one to eight sequential competitors, three real pendulum throws each, timing-based pump and actual release velocity, finite aftertouch, first-contact10/25/50/100zones, tied-leader extra attempts, soloPB and recorded multiangle speed/rewind/skip replay. Native complete8AI24throw match and focused physical/input/lifecycle checks0errors; final Windows build pending. See Assets/TruckGiantSwing/README.md and VALIDATION.md.


Field Goal BOTH variants now native-complete:24sports/26saved scenes,80rule checks, six complete4AI matches/176actual kicks and focused physical/input/replay suite0errors. Final seeded longest tiebreak10rounds. See Assets/TruckFieldGoal docs. Existing executable remains19sports; Windows expansion acceptance pending. Next Beer Pong, then actual Bowling before/after wall comparisons, combined build/tests and publication, then full queued dbf production pass.


Beer Pong: sequential1v1/2v2 and unlimited solo practice, actual NWH approach/charged spring launch,20hollow cups/penetration scoring, real table/rim bounces, rebuttal/repeated overtime, recorded replay and retained ownership/settings.83rule checks, two full AI games and focused physical/input suite0errors. Native25sports/27scenes; Windows expansion build still pending. See Assets/TruckBeerPong docs.


Bowling trampoline pit wall: capped pin-only return, actual spring bed/audio, down-state latch and protected settling/pinsetter flow.30equivalent before/after deliveries increased pinfall141→159 with23meaningful returns/61secondary contacts; no strikes in that grid. Three complete mixed-difficulty four-lane games produced4strikes/30spares plus real tenth bonuses; hot-seat full game and focused contact/reset tests0errors. See Bowling validation. Combined25sport Windows update pending.


## Focus and input availability

Human focus loss or attached input suspension now pauses normally. Returning focus never auto-resumes; existing fresh/neutral confirmation applies. Actual keyboard/controller removal still opens reconnect and permits explicit replacement. All-AI play continues in the background. This policy is shared across all existing sport families and does not gate ordinary Billiards turns. Native eight-family/160-check acceptance passed; final production Windows OS-focus/hardware acceptance is pending.

## Verified Git delivery

Implementation commit `6febbc6b8028ed1e1311bbadd436fd2733b4df1b` on `codex/truck-sports-seven-events` was pushed successfully to both that branch and GitHub `main`, verified with ls-remote at 2026-10-17T03:04:44.946800+00:00. **Both previous relevant Truck Sports work and this production update are committed and pushed to main.** All32 Sports folder trees, folder metadata and the checkpoint match the reviewed root implementation. Unrelated local Taxi/host work is preserved and excluded; the private Taxi ancestor is absent from the public history. Generated builds/logs/caches and original licensed package prerequisites are excluded. This documentation record is committed as a subsequent clean public descendant; the delivered folder's GIT-DELIVERY.txt records the final verified tip.

## Verified Git delivery

Implementation `bfd14cadbc72ceab0b865c912a6e8661f67c198e` on `codex/truck-sports-seven-events` was pushed successfully to that branch and GitHub `main`, verified at 2026-10-18T00:59:19.606915+00:00. **Both previous relevant Truck Sports work and this repair update are committed and pushed to main.** The public Sports tree matches the reviewed local Sports implementation. Unrelated local Taxi/host/vendor work and private Taxi ancestry are preserved and excluded; generated builds, logs, caches and original licensed package prerequisites are excluded.

This receipt is committed as a subsequent clean public descendant. The delivered folder's GIT-DELIVERY.txt records the final verified public tip after the receipt push. All build, test and remaining-manual limits above still apply.
