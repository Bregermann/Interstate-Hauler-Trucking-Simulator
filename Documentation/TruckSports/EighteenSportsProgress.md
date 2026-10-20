# Eighteen-sport expansion — working implementation evidence

The original master request and UI addendum are preserved in `Requests/`. The authoritative permanent requirements remain in `TRUCK_SPORTS_ACCEPTANCE_LEDGER.md`; this note is a concise evidence index, not a replacement backlog.

## Current vertical slices

**Red Light, Green Light:** saved twelve-full-NWH-tractor stadium, whole-body swept finish detection, actual displacement-based red-light violations, one-second laugh warning without yellow, measured external-impact allowances, bounded actual-mesh destruction, placement results, rematches, shared/chase/full-cab views, global weather/crew/horn and optional AI cameras. The UTS giant master uses the original rig and animations. Existing posed stadium spectators react through the shared Field Goal crowd adapter. Source: `Assets/TruckRedLightGreenLight/`.

The stopping investigation reproduced NWH Auto D/N/R interpreting a held service brake at rest as reverse. The mode now holds stopped trucks using the handbrake without continuing the reverse request. Ordinary reverse remains available to humans; actual movement still causes violations. Earlier failed probes are retained. The keyboard rematch fixture also exposed the batch Editor's Game View input-focus filtering; the fixture temporarily selects the documented Input System editor behavior and restores it afterward. It never writes gameplay commands to manufacture a pass.

**Hot Potato:** saved eight-full-NWH-tractor arena, one atomic bomb owner, genuine collider-contact passing with no ram-speed requirement, compound/resting-contact suppression, separation and transfer cooldown, bounded random fuse cycles, carrier engine-power boost, elimination blasts and recorded actual debris, capped per-cycle pass rewards, sole-survivor results, safe recovery, and resettable reused Core-Collapse props. Carrier AI physically chases; other AI physically evade. Source: `Assets/TruckHotPotato/`.

Hot Potato reuses the existing Union Field venue and shared two-pose spectator assets. One thousand spectator seats were sampled against actual stand geometry and rendered with the existing shared posed meshes and instancing-enabled materials; temporary authoring colliders are removed before saving. Crowd reactions, weather, cabs, identity, controls, horn and camera options use existing adapters.

## Artwork and UI

The approved third Red Light logo establishes the requested scraped-paint, worn-ink PS2-era direction for the new sports. Hot Potato has distinct original artwork in that style. The existing41logos are unchanged. Prompts and generated-source provenance are retained in `Tools/TruckSports/ArtworkSources/`.

The new modes use actual gameplay previews, saved menu actions, original PNG setup/help artwork, compact rounded scorecards, viewport ownership and recorded match-end review. New shared adapters extend existing Heat/TMP/DOTween presentation and `SportsMotionReplay`; they do not replace those foundations.

**Balloon Battle:** eight full NWH tractors, exactly three giant colored balloons per truck, bounded damped kinematic tethers with two recorded cord segments, real capsule/spike contacts, quarter-second yellow telegraph,0.6-second attack and2.5-second cooldown. Each attack can pop at most one balloon on each victim. +1 per pop, +3 elimination, +5 sole-survivor bonus; simultaneous mutual final pops resolve a draw. Confetti uses pooled cut-paper particles; eliminated humans get spectator views. Shared/chase/full-cab cameras, global names/colors, weather, horn, results, rematches and recorded review are integrated. The stadium reuses the existing venue/crowd with teal/plum/marigold lanes, EasyRoads banks and authored festival pennants. Source: `Assets/TruckBalloonBattle/`.

Its first countdown test exposed diagonal spawn footprints intersecting ramp ends: maximum horizontal displacement was4.390m. Moving the full starting ring from32m to42m puts every tractor on the flat outer lane; the corrected native and Windows fixtures report approximately0.056–0.058m maximum planar settling while all eight trucks are grounded and upright. Vertical suspension settling is reported separately. Visual inspection also caught nonserialized authored MaterialPropertyBlocks producing white balloons; participant-derived colors now apply on every round/reset. Both causes were corrected rather than loosening the drift threshold or painting over captures.

**Whack-a-Mole:**36pooled holes, three original animated mole prefabs, saved Emerge/Look/Dive/Whack clips, actual moving front-bumper contact and one hit per exposure. Standard/golden/decoy targets award10/50/−25points; decoys physically launch NWH's full tractor assembly. Timed2/3/4-minute competition gradually increases active targets and shortens exposure. The complete fairground uses existing fences, scenery, stalls/pennants and shared posed spectators, a live world scoreboard and its original grungy PNG. Shared/chase/full cab, crew/weather/horn, statistical/shared-placing results, actual recorded animated-mole review and rematches are integrated. Source: `Assets/TruckWhackAMole/`. The Windows AI result trace includes five genuine golden hits and one incidental decoy hit; AI receives no contact immunity.

**King of the Hill:** five collision-matched stone plateaus, eight EasyRoads bridges with visible edge rails, an actual connected67-node/70-edge LWS road graph and protected spectator overlook. Eight full NWH tractors compete for60cumulative uncontested control seconds;30/90options, physically present grounded/upright eligibility, opposing overturned bodies still contest, inaccessible proxy colliders/airborne trucks do not score. Every40seconds the hill relocates with a three-second announced preview. Actual AI uses the existing route/driver adapter, outward shoving, physical cliff checks and separated recovery. Results lock, recorded review, rematch and software-controlled cab/chase/shared views are integrated. Native and Windows probes each passed33checks. The tested Windows default match completed with Player5reaching60control seconds; this is automated gameplay, not a human pacing approval.

Its AI investigation found per-physics-step local retargeting cleared measured stall/reversing state. The new direct-goal adapter preserves that state. A separate route-equivalence test preserves Fare Fight's existing district API. Cliff support checks and continuous visible bridge rails reduced repeated recoveries; real combat retains exposed plateau edges.

**Seesaw:** four full NWH trucks on a genuine two-axis ConfigurableJoint deck above the reused quarry. Existing NWH suspension/friction applies the real vehicle load to the board. No artificial tilt authority exists. One/three/five heats, survival/recovery/knockoff/sole-survivor scoring, actual falls and quarry-impact fragments, shared placings, complete rematches and recorded board/debris replay. The venue reuses quarry terrain, Vista rocks, warehouse/container assets, lattice cranes and448 shared posed spectators; sport-owned hazard edging, rivets, pivot and open gantry complete the deck. Final decorative bracing supports the spectator balconies. Source: `Assets/TruckSeesaw/`.

Nine playable expansion sports are integrated in51saved build scenes. Nine requested additions remain unfinished. Magnet Mayhem is the current local implementation slice; no unfinished menu entry is installed. See EighteenSportsNineSportDelivery.md for the latest exact build/evidence; earlier measurements below are retained history.

## Verification actually executed so far

- Latest native Unity6000.4.10f1 focused EditMode:69/69passed. Red Light13, Hot Potato11, Balloon13, Whack12, Hill18(including district/graph route-equivalence and five actual driving-surface metadata cases), registered artwork/viewport presentation2. Combined XML: `five-editmode-final.xml`. The first new metadata test used the wrong assembly-qualified Taxi type; that fixture was corrected and all five cases passed. No unrelated test suite is certified.
- Native Red Light actual NWH/PhysX: complete twelve-truck AI matches, grounded/separated countdown, pause, legal finishes, deliberate software-keyboard violation/destruction, rematches and recorded replay controls. Latest complete probe30checks/0errors.
- Native Hot Potato actual NWH/PhysX: complete eight-truck AI matches, actual passes, seven eliminations and one survivor, pause, results/replay, rematch and a controlled0.65m/s real-tractor contact. Resting contact does not repeatedly swap possession. Latest complete probe30checks/0errors.
- Each mode's probe also exercised one through four injected-controller participants plus AI, individual accelerator ownership, human-only cab rectangles, optional AI views, and AI-only broadcast/view allocation. Four-cab720p/1080p controlled Editor fixtures were captured.
- Native shared-camera, cab and replay fixtures were visually inspected. These are explicitly controlled Editor renders, not Windows framebuffer or human gameplay evidence.

Detailed native logs/XML are in `Logs/TruckSports/EighteenSports/`; native probe receipts/captures are under the isolated project's matching log folder. Windows receipts/captures are in the host log folder. No physical controller, steering-wheel, listening or human-playtest acceptance is claimed.

**Current six-sport verification:** Native focused EditMode87/87passed: Red Light13, Hot11, Balloon13, Whack12, Hill22, Seesaw14 and presentation2. Current combined XML is `six-editmode-final.xml`; earlier69-test evidence above remains historical. Native full Seesaw38/0, Hill33/0 and six-scene snow18/0passed.

Actual Windows probes: Hot32/0, Red36/0, Balloon34/0, Whack33/0, Hill33/0, Seesaw38/0, six-arena snow18/0 and menu310/0. Every initial process passed on managed SHA256 `4445fba4cfd2caff74635c91cb4f19cf6419f9eee30dc04e1bee5a85fa8337ee`. Final decorative Seesaw-only balcony bracing repeated Seesaw/snow/menu; unchanged managed code and other five scene hashes are recorded in `six-bracing-scene-preservation.json`. Both process phases are retained. Screenshots are actual composed Windows frames; software-injected controllers are explicitly distinguished from physical hardware.

Seesaw's controlled real-physics weight fixture measured approximately0.49° centered,26.36° right-edge and26.65° forward-edge tilt, lowering the occupied side in both axes. Its real eight-corner tilted-deck camera check, actual accelerator-driven fall/quarry collision, three-heat AI match, pause, replay, rematch,1–4 software humans and AI-only camera policy passed. Maximum simultaneous competitors is four; AI fills only remaining slots, never extra trucks beyond that capacity. Moving-deck historical tire-track anchoring remains visually unverified.

Final Windows build:48scenes,0errors/34warnings,1,808,291,064reported bytes; canonical `Builds/TruckSports/TruckSports.exe`. Runtime manifest and build/process evidence are tracked in `Evidence/2026-10-20-Expansion/`. Prior47-scene build is locally preserved.

## Open work

These six vertical slices are not a declaration that the entire expansion is finished. Their remaining acceptance criteria are recorded against individual ledger entries. A severe-weather replay fixture produced insufficient visibility in one earlier run; later clear-weather recorded frames rendered correctly. Severe-weather replay visibility and subjective audio remain unverified rather than silently certified.

The other12sports retain their original requirements. Next: Floor Is Lava, Tetherball, Jousting, Magnet Mayhem; Volleyball, Freeze Tag, Hungry Hungry Haulers, Sheep Herding; Hide and Seek, Frogger, Avalanche Escape, Ninja Warrior. Existing source scenes, private Taxi changes and imported vendor work are preserved. The preservation check found zero changes across887protected files and25host settings. Original41logo hashes are unchanged.

Specific remaining acceptance limitations: no physical controllers/wheels, subjective listening, human matches or isolated CPU/GPU benchmark; severe-weather replay visibility remains unverified. Replay records truck/debris/balloon transforms, not the entire historic weather/light/particle/prop-damage timeline. Red Light's unrequested183-second absolute timeout was removed and has a new regression test: slower legal races continue until the rules resolve. Digital stadium standings, PA towers, pooled NWH-derived smoke/sparks and Cinemachine shake are integrated; actual violation-triggered particles/stat updates/impulse passed native and Windows probes. Hot Potato now tightens physical intercept/steering and escape commitment over the final six fuse seconds; complete Windows matches still pass the shared contact/scoring rules. These limitations are not a declaration that the entire18-sport expansion or every presentation detail has passed.

## Vendor accounting

**VENDOR ASSETS AUDITED:** NWH full tractor/input/transmission, UTS character/animation/traffic-light assets, RoadProps, EasyRoads, existing Core-Collapse integrations, Weather Maker/Weatherade, Heat, DOTween, Cinemachine and the existing Sports replay/crowd adapters. See `EighteenSportsVendorAudit.md`.

**VENDOR ASSETS USED:** NWH Vehicle Physics2 through `SportsNwhTractorMotor`/`FareFightVehicleAuthoring`; UTS `Man_11`, talk/cheer clips and Traffic Light_1; RoadProps barriers; EasyRoads baked ramps; existing `PlowBreakable` Core-Collapse props; Weather Maker/Weatherade through shared Sports adapters; Heat/TMP, DOTween and Cinemachine through existing presentation components. Original two-pose Taxi spectator meshes and sport-owned venue geometry are reused.

**RELEVANT ASSETS NOT USED:** no installed suitable giant cartoon bomb model was found; the bomb shell/fuse is original sport-owned geometry. A second destruction, replay, traffic, vehicle or weather framework was unnecessary.

**CUSTOM SYSTEMS CREATED:** sport-specific rule authorities, AI input coordination, scene authoring, contact adapters, evidence probes, original artwork and small read-only presentation adapters over existing shared systems.

**VENDOR SOURCE MODIFIED:** NO.

**DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO.

Explicit normal snow checks found unnamed competition floors missing existing Weatherade track metadata. Sport-owned tags now bind all five actual surfaces. Balloon's visible25mm overlay now has a matching MeshCollider. Native and Windows15-check probes passed actual accumulation and wheel-generated track stamps in all five saved scenes. The first multi-scene probe incorrectly stayed in its initial base session; explicit scene identity checks fixed that fixture, and the final probe genuinely loaded all five scenes.

### Hill corrections in the six-sport build

The published five-sport visual bridge rails lacked colliders because the shared Box authoring helper removes a Collider when no PhysicsMaterial is supplied. The explicit scene migration now adds16 matching solid BoxColliders; native bounds/collider tests and actual Windows Hill gameplay passed.

A further AI investigation found the navigation planner's default35m junction connector could connect nearby but physically separate bridge approaches across open air. The Hill-only route adapter now supplies0.5m connector tolerance and nearest physical approach. The original Fare Fight parameterless API keeps its existing tuning. All20 cross-plateau route combinations and physical waypoint proximity passed without editing the private LWS planner or vendor source. The final physical default60-second-control AI match finished; human pacing is not certified.


## Nine-sport milestone — latest measured status

Floor Is Lava:25finite real slabs, first-contact warning/collapse, bounded jump and ram combat, real lava destruction, survivor/knockoff ranking, replay/rematch; native18/18 and Windows37/0. Reset restores physics and interpolated pose. Actual gameplay cameras enable existing HDR grading; native forced Blizzard37/0 restores snow surface detail.

Tetherball: actual two-truck striking, bounded ball joint and visible winding cable; signed real travel winds/unwinds,720degrees wins round, best3/5, real AI, cab/shared views, reset and recorded cable replay. Native14/14; Windows19/0.

Jousting: physical adjustable lances/shields, normal-impact1/2points and observed knockdown upgrade3,3/5passes, true AI aiming/driving, Core Collapse splinters, full input/aim transitions, reset/review. Native15/15; Windows21/0. Prestarting NWH engine during countdown fixed sluggish return from aim; measured human fixture moves approximately5m over1.5s after fix.

Final native combined135/135. Windows final targeted432checks/0errors (Lava37,Tether19,Joust21,snow27,menu328), exact current managed hash in nine-sport delivery. Preceding complete eight-sport phase609/0 retained. No physical-device/listening/human gameplay claims. Magnet Mayhem source is under native compilation/scene/AI validation and is not included in the nine-sport milestone.


## Tenth playable addition — Magnet Mayhem

The latest delivered milestone is ten additions /52saved scenes; see EighteenSportsTenSportDelivery.md for exact hashes, distinct Windows phases and preserved failed attempts. Magnet passes21native rules and38actual Windows gameplay checks; combined156native tests and final presentation3pass. Ten-arena snow30/0 and final menu335/0 are recorded separately. Eight requested additions remain unfinished. Volleyball native authoring/match acceptance is next; Freeze Tag and Hungry Haulers remain local drafts. No empty menu entries were installed for those drafts.


## Twelve-sport milestone — current measured status

# Truck Sports expansion — twelve playable additions; assignment continues

Windows x64 Development/Mono/D3D11, Unity6000.4.10f1.54saved scenes, startup TruckSports. Build report0errors/57warnings. Managed SHA256 08c070300cd5fec8ee88adee6f58f044a429b49b6955aab1d71344cf21e8013c.

Added in this milestone: Truck Volleyball and Truck Freeze Tag. The preceding ten additions remain included. Six of the18requested additions remain unfinished: Hungry Hungry Haulers, Sheep Herding, Hide & Seek, Frogger, Avalanche Escape and Ninja Warrior. Local drafts are excluded from this executable and scoped publication.

Volleyball: full NWH tractors, physical oversized striped beach ball/net, bounded suspension jumps, contact-normal returns,3touch/fourth-touch/double-touch rules, first15winby2cap21, actual coordinated AI, physical service/rallies, selectable team sizes, sand/palms/posed crowd, world scoreboard, original grungy PNG and actual gameplay preview. Native15rules and actual Windows33checks passed, including a complete AI match, real collision scoring, replay, rematch and1–4software-input views.

Freeze Tag: asymmetric cannon/runner teams, actual first-solid spray casts,3hit freezing, no automatic thaw, actual teammate-impact rescue with a brief protection window,4alternating-role rounds, all-frozen/timer team wins, physical pursuit/evasion/rescue, readable ice shells/cooldowns, solid stacked-container cover, pooled snow effects, original grungy PNG and captured preview. Native17rules and actual Windows36checks passed, including full4v4matches and real input-driven freeze/rescue.

Root causes corrected before this build: the reused snow effect's Destroy stop action invalidated pooled replay references after rescue; every owned instance/child now uses None and safe per-child stopping. No vendor prefab/source changed. Container visual orientation/fit originally mismatched cover bounds; owned stacked visuals and exact collider bounds now agree. Volleyball's initial floor fixture accidentally overlapped a real opposing truck; only the fixture position changed to isolate genuine court contact. Neither scoring thresholds nor physics rules were weakened.

Combined native EditMode188/188passed,0failed/skipped. Current Windows phases all passed on the exact managed hash above: Volleyball33/0, Freeze36/0, twelve-arena accumulating snow/actual NWH tire tracks36/0, menu347/0. Menu checks launch saved setups, inspect imported logos/previews, return/navigation and representative display layouts. Earlier eight/nine/ten-sport receipts remain distinct history, with unchanged earlier gameplay sources/scenes verified by twelve-preservation-result.json. Those old full-match suites were not all rerun on this DLL.

Actual gameplay, replay and four-cab720p/1080p frames were captured and inspected. Software-injected controllers are not physical-device or human testing. No subjective listening, human balance approval or isolated CPU/GPU profiling is claimed. Freeze AI often produces runner wins/shared2–2results; difficulty/pacing needs human review. Severe-weather replay visibility and moving-platform historical track anchoring remain unverified.

Canonical executable: Builds/TruckSports/TruckSports.exe, with its complete companion files. Runtime manifest 386files / 1,814,470,857bytes, JSON SHA256 17eef1af8141606ee42460b80bbe134d699b04030e4f476776130d5fea2465d6. Complete accepted ten-sport build preserved separately; this twelve-sport build is also copied to .sports-validation/Recovery/Expansion-Twelve-Sport-54-Scene-Verified. Git delivery receipt is written after the scoped commit/push verifies both authorized public branches.

Preservation: 887protected files, 25host settings and 41original logos checked, zero changes. Private Taxi/navigation history and the private recovery branch are excluded. No force push.

VENDOR ASSETS AUDITED: NWH vehicle/jump/contact/input APIs; PhysX; Vista beach/palm assets; ShipContainerPack; Weatherade Projectile/SnowHitFX and SnowHit audio; existing snowfield/crowd/venue assets; Heat/TMP/DOTween; Weather Maker; existing driving views, identities, horn and recorded review.
VENDOR ASSETS USED: full NWH tractors/cabs and AddAssemblyVelocity; PhysX collisions/sphere casts; existing genuine sand/palms/containers; SnowHitFX/SnowHit recordings; shared posed spectators and FieldGoalCrowd; existing Heat/TMP/DOTween/weather/review foundations.
RELEVANT ASSETS NOT USED: sample projectile instantiation/destruction does not provide persistent freeze/team/rescue rules or reusable pool lifetime. No installed volleyball/net gameplay solution found. Their mode semantics and missing sport geometry are owned code/content.
CUSTOM SYSTEMS CREATED: VolleyRules/VolleyMatch physical rally coordination, FreezeRules/FreezeMatch team/freeze/rescue authority, owned authoring/contact/board adapters and explicit native/Windows evidence probes; original PNG artwork and provenance.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.



## Fourteen-sport accepted automated milestone — work continues

# Truck Sports expansion — fourteen playable additions; assignment continues

Windows x64 Development/Mono/D3D11, Unity6000.4.10f1.56saved scenes, startup TruckSports. Build report0errors/58warnings. Managed SHA256 269fc2ed6b2e860436c70d1a9a3bce32800b566c8c8532ce7e572f1d7b0f8700.

Added in this milestone: Hungry Hungry Haulers and Truck Sheep Herding. The preceding twelve additions remain included. Four of the18requested additions remain unfinished: Hide & Seek, Frogger, Avalanche Escape and Ninja Warrior. Hide & Seek has local runtime/authoring drafts and original artwork; Frogger has local rules/tests/artwork. Those drafts and their optional shared presentation changes are excluded from this executable and scoped publication. The assignment continues after this milestone.

Hungry Hungry Haulers: four full NWH tractors,256finite physical normal/gold/fuse balls, actual animated telescoping front scoop, six visible hopper slots, one-time owned-bin banking, no collection-only points, real rival ram cargo spills, bounded bomb disruptions, physical AI collection/approach/deposit, three-minute default, two/five-minute options, shared/chase/cab views, horn, crew, weather, recorded review and rematch. Original grungy PNG and actual gameplay preview are installed in the saved menu/setup/venue. Native20rules/binding tests, full native45physical checks and actual Windows45/0 passed.

Truck Sheep Herding:48genuine rigged SheepV5 animals with existing idle/run animation, Unity NavMesh avoidance and terrain grounding, real truck/horn pressure and flock behavior, four color-coded open timber pens, once-only whole-animal capture, three-minute default, count/last-capture-time ranking, rare marked explosive sheep and bounded physical loose-fence reactions. Actual four-slot AI guides sheep through pens; normal collisions never give herd points. Rolling meadow/path terrain, detailed timber barn, installed rural scenery and reused Vista horizon, original grungy PNG, current preview, world scoreboard, shared/chase/cab, review/reset are saved. Native19rules and native/Windows52physical checks passed.

Concrete repairs: HaulerMouth formerly lived as an extra MonoBehaviour inside HaulerScoop.cs; Unity embedded an unbacked local MonoScript that worked in the Editor but corrupted player level54. A dedicated backing script/meta, exact component rebinding and saved-scene regression repaired the actual build. Actual intake lip contacts/overlap match the visible scoop and detect balls rebounding before trigger-center entry. AI banking uses a deliberate bay approach and applies its hold after driving. Farm paint now follows terrain instead of intersecting it, and score values fit the bottom cards. A mismatched Herd artwork object name caused a menu-probe failure despite the correct PNG; owned hierarchy names now match the standard integration contract.

A further real rematch defect retained NWH engine, transmission and tyre damage after visible resets. Controlled deliberately damaged starts failed before the fix. Both modes now call NWH's public DamageHandler.Repair (or public powertrain/wheel repair fallback) when beginning a new match. Actual collision forces and normal in-round damage behavior are preserved. Repaired native and Windows fixtures verify clean powertrains and real subsequent gameplay. An earlier contested-match assertion incorrectly required every losing AI to score; it was replaced with genuine contested capture/conservation plus separate physical pen-entry cases for each AI slot. Those stronger cases exposed the rematch defect; earlier failures remain in evidence.

Combined native EditMode227/227passed after the repair,0failed/skipped. On the exact final managed hash above, Windows Haulers45/0, Herding52/0, fourteen-arena accumulating snow/actual NWH tyre tracks42/0 and menu359/0passed, all exit0. Native full PlayMode-style runtime probes also passed45/0 and52/0. These are explicit actual NWH/PhysX/NavMesh software-input scenarios, not physical-device or human certification. Earlier twelve-sport full-match receipts remain separate historical evidence, with earlier gameplay sources/scenes checked unchanged; they were not all rerun on this DLL.

Actual Windows gameplay/replay and four-cab720p/1080p frames were inspected. The compact score cards use one bottom row above the information rail. No subjective listening, human balance approval, physical controller/wheel test or isolated CPU/GPU profiling is claimed. AI approach/recovery balance, severe-weather replay readability and long-run crowd/physics performance still require human/profile follow-up. This is verified focused automated gameplay/integration, not a claim of release-ready human acceptance for the whole collection.

Canonical executable: Builds/TruckSports/TruckSports.exe with all companion files. Runtime manifest 391files / 1,825,417,367bytes, JSON SHA256 489341d4401ee5bb15dfe46a1e65c5c2a5300f66fd03107ddfe80748560b5ecc. A complete backup is stored at .sports-validation/Recovery/Expansion-Fourteen-Sport-56-Scene-Verified. The accepted twelve-sport build remains preserved separately. GIT-DELIVERY.txt records the actual scoped commit/push after both authorized public refs are verified.

Preservation:887protected assets/files,25host settings and41original logos checked with zero changes. Private Taxi/navigation/history, unrelated work and the private recovery branch are excluded; no force push.

VENDOR ASSETS AUDITED: NWH full tractor/powertrain/damage/horn/contact APIs; existing cargo/trailer visuals; Haberdashery SheepV5 and RunCycle; Unity AI Navigation; existing SheepMotion/grounding and explosive-sheep adapters; Vista terrain/layers; installed MegaCity rural assets; Heat/TMP/DOTween; Weather Maker/Weatherade; shared input/identity/view/replay/audio adapters.
VENDOR ASSETS USED: NWH VehiclePhysics2/WheelController tractors/cabs, DamageHandler.Repair and powertrain.Repair; actual PhysX contacts/forces; SheepV5 rigs and existing idle/walk animation; Unity NavMeshAgent/Surface; Vista layers and existing generated TerrainData; installed farmhouses/warehouse/pines; DOTween scoop/unloading presentation; shared Heat/TMP/weather/tyre tracks/horn/crew/review.
RELEVANT ASSETS NOT USED: no installed Hungry Haulers or farm-herding competition authority found. Existing queue-on-contact explosive sheep are inappropriate for ordinary herding targets; only explicitly marked hazards reuse that behavior. Heavyweight dynamic simulation is not retained for secured cargo. No replacement vehicle, navigation, weather, UI or animation framework was made.
CUSTOM SYSTEMS CREATED: finite HaulerRules/HerdRules scoring and lifecycle semantics; Hauler scoop/hopper/lip/ram/bin adapters; Herd sheep threat/flock goals and whole-body pen ownership; mode-owned scene/world/board/AI coordination; original scoop/farm geometry and PNG artwork; explicit native/Windows acceptance fixtures.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

Exact build report:

Succeeded
Errors: 0
Warnings: 58
Bytes: 1837815619
Duration: 00:02:52.3396309
Startup: Assets/TruckSports/Scenes/TruckSports.unity
Scenes: Assets/TruckSports/Scenes/TruckSports.unity, Assets/TruckBilliards/Scenes/TruckBilliards.unity, Assets/TruckHockey/Scenes/TruckHockey.unity, Assets/TruckBowling/Scenes/TruckBowling.unity, Assets/TruckAirHockey/Scenes/TruckAirHockey.unity, Assets/TruckWallball/Scenes/TruckWallball.unity, Assets/TruckCornhole/Scenes/TruckCornhole.unity, Assets/TruckCheeseRolling/Scenes/TruckCheeseRolling.unity, Assets/TruckDockDiving/Scenes/TruckDockDiving.unity, Assets/TruckKeepaway/Scenes/TruckKeepaway.unity, Assets/TruckKeepaway/Scenes/TruckKeepawayFirstPerson.unity, Assets/TruckCurling/Scenes/TruckCurling.unity, Assets/TruckBaseJumping/Scenes/TruckBaseJumping.unity, Assets/TruckTerrierRacing/Scenes/TruckTerrierRacing.unity, Assets/TruckMinefield/Scenes/TruckMinefield.unity, Assets/TruckMattressRacing/Scenes/TruckMattressRacing.unity, Assets/TruckWifeCarrying/Scenes/TruckWifeCarrying.unity, Assets/TruckMusicalParking/Scenes/TruckMusicalParking.unity, Assets/TruckGiantSwing/Scenes/TruckGiantSwing.unity, Assets/TruckBuzkashi/Scenes/TruckBuzkashi.unity, Assets/TruckBogSnorkelling/Scenes/TruckBogSnorkelling.unity, Assets/TruckBungeeDemolition/Scenes/TruckBungeeDemolition.unity, Assets/TruckFishing/Scenes/TruckFishing.unity, Assets/TruckSumo/Scenes/TruckSumo.unity, Assets/TruckTargetLaunch/Scenes/TruckTargetLaunch.unity, Assets/TruckFieldGoal/Scenes/TruckFieldGoal.unity, Assets/TruckBeerPong/Scenes/TruckBeerPong.unity, Assets/TruckPoker/Scenes/TruckPoker.unity, Assets/TruckTugOfWar/Scenes/TruckTugOfWar.unity, Assets/TruckSkeeBall/Scenes/TruckSkeeBall.unity, Assets/TruckLogRolling/Scenes/TruckLogRolling.unity, Assets/TruckTrampolineDodgeball/Scenes/TruckTrampolineDodgeball.unity, Assets/TruckDemolitionDerby/Scenes/TruckDemolitionDerby.unity, Assets/TruckPedestrianPlowing/Scenes/TruckPedestrianPlowing.unity, Assets/TruckPedestrianPlowing/Scenes/TruckPedestrianPlowingFirstPerson.unity, Assets/TruckKickball/Scenes/TruckKickball.unity, Assets/TruckDanceOff/Scenes/TruckDanceOff.unity, Assets/TruckLimbo/Scenes/TruckLimbo.unity, Assets/TruckSkyTow/Scenes/TruckSkyTow.unity, Assets/TruckTrailerPlug/Scenes/TruckTrailerPlug.unity, Assets/TruckFareFight/Scenes/TruckFareFight.unity, Assets/TruckSnake/Scenes/TruckSnake.unity, Assets/TruckRedLightGreenLight/Scenes/TruckRedLightGreenLight.unity, Assets/TruckHotPotato/Scenes/TruckHotPotato.unity, Assets/TruckBalloonBattle/Scenes/TruckBalloonBattle.unity, Assets/TruckWhackAMole/Scenes/TruckWhackAMole.unity, Assets/TruckKingOfTheHill/Scenes/TruckKingOfTheHill.unity, Assets/TruckSeesaw/Scenes/TruckSeesaw.unity, Assets/TruckFloorIsLava/Scenes/TruckFloorIsLava.unity, Assets/TruckTetherball/Scenes/TruckTetherball.unity, Assets/TruckJousting/Scenes/TruckJousting.unity, Assets/TruckMagnetMayhem/Scenes/TruckMagnetMayhem.unity, Assets/TruckVolleyball/Scenes/TruckVolleyball.unity, Assets/TruckFreezeTag/Scenes/TruckFreezeTag.unity, Assets/TruckHungryHaulers/Scenes/TruckHungryHaulers.unity, Assets/TruckSheepHerding/Scenes/TruckSheepHerding.unity
