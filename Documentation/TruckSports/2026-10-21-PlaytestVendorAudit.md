# October21 broadcast / human-playtest repair — vendor decisions

The permanent acceptance ledger and the two dated source directives own requirements. No archived backlog is reopened. The accepted60-scene build and its companion backup remain intact while a separate camera executable is validated.

| Responsibility | Audited existing source / API | Decision |
|---|---|---|
| Shared camera composition | SportsEventCamera/SportsCameraFit; Cinemachine2.10.7 public CinemachineGroupComposer, CinemachineTargetGroup and Transposer; package component documentation/source | Reuse vendor group composition, dolly, lens boundaries and damping on the existing rigs. LWS supplies legitimate sport targets and actual transformed renderer-corner readability limits, which the vendor cannot infer from game rules. |
| Occlusion | CinemachineCollider public settings and extension pipeline | Reuse vendor obstacle avoidance and smoothing. Retain only a final terrain-height safety query for a camera initialized under a mountain. |
| Objective / competitor arrows | Installed Compass Navigator Pro4 prefab, public per-group POI registration/state, private implementation read only to understand supported extension points; existing Keepaway compass adapter | Reuse CNP projection, edge placement, overlap handling, UI prefab and POI lifetime. A narrow adapter filters its own broadcast cues, uses group4 only during shared view and never registers hidden opponents. No new compass engine. |
| Presentation | Existing Heat/TMP/uGUI, broadcast panels, DOTween, existing original sport PNGs | Extend existing presentation. No replacement UI framework or vendor modifications. |
| Vehicle and match logic | Existing NWH/EventTruck and older Hockey match APIs | Camera work is read-only over actual bodies, roles, objectives and elimination state. Existing setup, input, scoring and lifecycle remain authoritative. |

Original production SportsBroadcastAuthoring (HUD theme/tooling) is preserved. New camera-only editor tooling is SportsBroadcastCameraAuthoring. A first compile caught a naming collision; it was corrected by restoring the original committed tool, rather than changing its consumers. Early failed compile and overzoom Windows evidence is retained under Logs/TruckSports/PlaytestRepair. A corrected preliminary Windows comparison passed40checks; this is not yet whole-collection acceptance.

VENDOR SOURCE MODIFIED: NO. DUPLICATE VENDOR FUNCTIONALITY CREATED: NO. Further playtest repair areas require their own focused asset audit before changes.

## Fishing catch repair audit

Inspected FishingAuthoring, FishingNetAuthoring, FishingPresentationAuthoring,
FishingGoddessAuthoring, FishingMatch/Stream/Director, existing test/probe paths,
the saved four-stream scene and the current owned fish/net prefabs. The existing
Layer Lab fish retain their swimming Animator controllers and appearances.
River Modeler supplies the established water normal/texture foundation, Unity
Terrain owns banks, and DOTween owns the existing net dip/material-flow animation.
No vendor source or fish source prefab is changed.

The existing sport deliberately moves pooled fish along deterministic paths and
evaluates an analytic net-plane crossing; these fish/net models have no active
physics contact pair to repair by adding a second trigger authority. The hoop's
authored horizontal radius is1.1m, matching FishingStream.radius. The defect is
in the LWS timing adapter: movement used Spawn (which includes an upstream lead)
while scoring used arrival. The focused repair aligns those time origins and
retains the existing within-step moving-net interpolation, score rules and pool.
Two new actual-stream EditMode regressions fail against the unchanged old code
(fishing-before.xml); post-fix Windows/net acceptance is still pending.

### Fishing course/speed continuation

The explicit saved-scene upgrade reuses the current Terrain layers, River Modeler
surface material/FishingWaterFlow, Layer Lab swimming rigs, sport-owned fish
variants, existing river grass/boulders and installed JC MegaCity Tree03/07 LOD
prefabs (already tracked dependencies). The new water mesh and TerrainData author
this sport's requested winding channel; they do not replace vendor water/terrain
rendering. DOTween still owns presentation. Configurable timed speed progression
and seeded tempo variation are LWS game rules, not a new motion/animation framework.

### Kickball home-run continuation

Inspected existing KickballRules, Match, agent/ball callbacks, views/replay,
FieldGoalCrowd and Poker's DOTween cab expressions. Retain NWH/PhysX for actual
play, existing crowd meshes/audio and original PNG/Heat/TMP UI theme. LWS must
resolve the legal baseball score; no installed vendor owns these rules. Reuse
the existing replay and next-plate lifecycle. DOTween animates only the banner
and cab flourish after authoritative scoring has been sealed.

### Hockey investigation / broadcast HUD

The actual unchanged Hockey goal authority passed33Windows physical-shot checks
at2/8/16m/s in both nets, plus outside denial and faceoff/rematch. This does not
reproduce or close the reported human defect. Visual camera review did expose
the older SportsArenaOverlay projecting HUD docks onto the now offscreen whole
rink edges. Its existing plates are retained; when its explicitly assigned camera
has the active broadcast adapter, docks use that viewport's safe region. Other
unbound precision arenas keep their existing projection. No goal physics or
vendor source was changed on the strength of an unconfirmed hypothesis.


### Fare Fight road access, fair starts, contact and objective indicators

Audited the existing public LWS road-query/planner and Taxi district's authored
paired one-way edges, UTS traffic/population availability APIs, CNP group limits
and indicator creation, Taxi collision observer and pedestrian TryStrike/MarkHitEventSent,
and the existing Fare Fight contact/possession state machines. Preserve the
private host navigation changes without copying or modifying them. Sport-owned
road access now connects nearest sampled roads and considers an explicitly
authored reverse edge; the shared planner still owns network paths. The initial
grid samples comparable remaining road distance and rotates slot assignment.
Existing UTS/population availability excludes countdown spawns; hazards use actual
truck poses. CNP owns arrow projection/edge placement; the existing project
filter adapter isolates one legitimate target per local camera. The existing
Taxi ragdoll API handles pedestrian impacts, with SportsFeedback audio pooling.
No vendor, Taxi source, private planner or passenger scoring modifications.

### Sky Tow gate repair

Audited Unity Splines lane sampling, existing real ConfigurableJoint tow physics,
normal/gold gate sweep and owned Arctic models/materials. No suitable installed
standalone hoop model was found; UTS Big_Road_Ring is road geometry, not an aerial
gate. Reuse the existing sport-owned torus authoring foundation to make segmented
safety-panel frames, inset trim and baked chevrons (one renderer per gate).
Existing gold/coral/navy/warm-white materials and real clearance geometry remain
aligned. Reuse Weatherade SnowHitFX particle configuration and already-tracked
snow-piece mesh/materials in four unpacked scene-owned pooled effects, disable
its audio and destructive stop action; no vendor source/prefab changes or new
vendor files. Unity Splines remains route authority. LWS computes the trailing
nominal path and ring difficulty; no force toward rings, teleportation or free
flight has been added. Real cable and original120-second course remain intact.


### Final integration and observed root causes

Fare uses a scene-owned layer adapter for the hidden Taxi avatar, whose layer29
conflicted with Sports cockpit four; original Taxi source and visible character
appearance stay unchanged. Existing CNP icon/group APIs and filtering remain
navigation presentation authority. Sky gate HDR artifacts came from cancelled
mesh normals in owned doubled triangles, isolated with HDR native rendering and
confirmed fixed in Windows; no vendor shader or global postprocessing edit.
Minefield reuses SportsExpansionViewHud with opt-in portrait/distance mode; LWS
owns grounded distance/finalization semantics, NWH owns wheel support/physics.

VENDOR ASSETS AUDITED: Cinemachine, CNP4, NWH, UTS/Taxi public APIs, Weatherade
SnowHitFX, Unity Splines, existing Heat/TMP/DOTween UI foundation.
VENDOR ASSETS USED: the above through existing adapters/configuration and owned
scene meshes/materials. Relevant NWH hitch/trailer APIs audited for next repair.
RELEVANT ASSETS NOT USED: UTS Big_Road_Ring is road geometry, unsuitable for aerial
gates; full terrain/road regeneration is unnecessary for these scoped fixes.
CUSTOM SYSTEMS CREATED: LWS broadcast selection/readability and per-mode semantics,
small road-access/fair-grid adapters, owned gate mesh correction and targeted tests.
VENDOR SOURCE MODIFIED: none.
DUPLICATE VENDOR FUNCTIONALITY CREATED: none; no replacement physics, route search,
weather renderer, compass projection or UI tween framework.


### Trailer Plug matched-vehicle repair

VENDOR ASSETS AUDITED: Installed GR3D SemiTruck/SemiTrailer prefabs, NWH
TrailerHitchModule/TrailerModule wrappers and public AttachTrailer/DetachTrailer,
WheelController unit-scale requirements, LwsNwhTrailerCouplingAdapter, existing
BungeeSwingForces, PlowCrowdFactory/Plow ragdoll context, SportsMotionReplay,
Weatherade scene snow adapter and NavMeshSurface.
VENDOR ASSETS USED: The matched NWH source models, original compound physics
bodies/cab joint, vendor-created hitch, native freely rolling trailer wheels,
original materials and existing owned URP conversions. Same uniform geometry
factor2.801609applied to both, with unit-scale controller transforms. Existing
PhysX bungee pump/replay, shared Plowing crowd and Weatherade bindings retained.
RELEVANT ASSETS NOT USED: LwsNwhTrailerCouplingAdapter routes manual road-hitch
input; this countdown needs the same underlying public NWH attachment API
directly. Original stretched render-only trailer remains on disk for history,
removed from the active scene. No new terrain/road framework was warranted.
CUSTOM SYSTEMS CREATED: Only LWS sport lifecycle coordination, deferred attach
after joint retirement, countdown interpolation reset, geometric coverage-center
adaptation and explicit versioned scene upgrade/regression tests. Existing custom
hitch construction was removed from the active runtime.
VENDOR SOURCE MODIFIED: None.
DUPLICATE VENDOR FUNCTIONALITY CREATED: None; NWH owns coupling and free-wheel
physics. All1019saved-scene dependencies were already tracked, with no new vendor
payload required for this repair.


### Field Goal holder and post-kick audio

VENDOR ASSETS AUDITED: Existing NWH kicking/holder model hierarchy and native
SuspensionThump.wav; SportsFeedback pooled sources and shared SportsSettings;
Weather Maker Wind1; existing Hockey/Billiards cues and Dock crowd samples;
DanceTruckRig choreography and DOTween, field/crowd/referee/presentation source.
VENDOR ASSETS USED: Original NWH render model and existing tracked SuspensionThump
as dedicated kick-slot7; existing DOTween for render-only holder celebration;
Cinemachine shot foundation and existing feedback/crowd/wind/settings adapters.
RELEVANT ASSETS NOT USED: Dance-Off's physical stage rig requires an authoritative
kinematic truck, inappropriate for this render-only holder. Reuse DOTween rather
than adding that physics authority. The billiard click remains for post clangs;
it is no longer the football kick. No new crowd framework/sample fabrication.
CUSTOM SYSTEMS CREATED: Game-specific holder alignment/choreography, confirmed-
goal cutaway, one result-audio owner and post-contact debounce; explicit versioned
scene upgrade and development-only game-listener PCM/source-timeline diagnostic.
The isolated tap is read-only and accesses neither microphone nor system audio.
VENDOR SOURCE MODIFIED: None.
DUPLICATE VENDOR FUNCTIONALITY CREATED: None; existing pooled playback, settings,
NWH physics and DOTween remain authorities. No new vendor payload; thump already
tracked. Scene/physics/scoring/branding preserved; human listening not claimed.


### Fare Fight native damage recovery

VENDOR ASSETS AUDITED: NWH DamageHandler.Repair, powertrain damage and automatic
DNR behavior; existing Ninja checkpoint repair adapter; Fare Fight route,
announced wreck recovery, rules and actual full-match diagnostic state.
VENDOR ASSETS USED: Public DamageHandler.Repair restores native meshes, engine,
transmission and wheel damage at Fare lifecycle boundaries. NWH continues
to own powertrain health and propulsion; original passenger state machine
continues to own ordered reset/theft/delivery events.
RELEVANT ASSETS NOT USED: No new recovery, input or vehicle physics framework.
NWH reverse timing is not retuned without evidence; existing route planner
(including the private pending host version) remains untouched.
CUSTOM SYSTEMS CREATED: Fare-specific destroyed-powertrain observation feeding
existing announced wreck recovery; targeted native health/possession fixtures
and read-only prolonged-stall telemetry in the existing Windows match probe.
VENDOR SOURCE MODIFIED: None.
DUPLICATE VENDOR FUNCTIONALITY CREATED: None.


### Keepaway expanded destructible arena (implementation in progress)

VENDOR ASSETS AUDITED: Core-Collapse Collapser/Support, bundled EzySlice
SlicerExtensions/SlicedHull and shatter examples; existing PlowBreakable pooled
fragments/reset; current modeled Keepaway props, NWH event wheels, rounded
boards, terrain layers and shared Cinemachine/CNP broadcast profile.
VENDOR ASSETS USED: Core-Collapse damage/support through existing PlowBreakable;
EzySlice authoring-time cuts of the actual barrel/crate/tire/timber/barrier
meshes; original installed model/material sources, NWH wheel physics and
existing fixed-pool fragments; current shared camera profile retained.
RELEVANT ASSETS NOT USED: Generic prefractured masonry debris would visually
replace wooden/rubber/metal prop silhouettes. Use the installed EzySlice tool
to cut their actual geometry instead. No runtime fracture engine or redundant
weather/road/camera system is justified. Existing terrain is already finished
and textured; resize its owned mesh, without replacing it with new terrain.
CUSTOM SYSTEMS CREATED: Keepaway-only destruction-to-navigation/reset adapter,
explicit idempotent saved-scene expansion migration and targeted tests.
VENDOR SOURCE MODIFIED: None.
DUPLICATE VENDOR FUNCTIONALITY CREATED: None; no new damage or mesh-cut authority.


Keepaway authoring dependency accounting: EzySlice is an installed pending
vendor directory outside the public source scope. Unchanged source/meta hashes
are recorded privately in keepaway-private-authoring-tool.json; authoring uses
its public Slice/SlicedHull methods through optional reflection. Existing
pre-cut scenes compile and run without the authoring tool. No raw EzySlice
source, manual or unrelated vendor file is staged/published. Core-Collapse
Support/Collapser and original prop models were already tracked. The only
shared runtime edit is a project-owned PlowBreakable kinematic velocity guard,
with reset tests passed in both original Plowing scenes.


### Paused broadcast objective widget correction

VENDOR ASSETS AUDITED: CNP public POI group state, indicator factory, animation
scale settings and per-widget CanvasGroup; its read-only indicator source
confirmed scale entrance uses Time.deltaTime. DOTween unscaled update/link/ID
lifecycle API and existing project presentation use were reviewed.
VENDOR ASSETS USED: CNP owns actual indicator creation, projection, opacity,
visibility and registration; DOTween animates only the missing paused entrance
in the existing owned SportsBroadcastCompass adapter. No CNP edit.
RELEVANT ASSETS NOT USED: No custom arrow/projection framework, global time
manipulation or replacement compass. Existing vendor fields and widgets suffice.
CUSTOM SYSTEMS CREATED: Small paused-presentation coordination in existing
adapter; regression requiring the actual rendered widget (not just active POI).
VENDOR SOURCE MODIFIED: None.
DUPLICATE VENDOR FUNCTIONALITY CREATED: None; CNP plus DOTween remain authorities.


All-human cue follow-up: inspected saved prefab overrides and CNP's public
POIisRegistered/POIRegister API. The scene guide had7targets while the prefab
allowlist retained3. LWS now binds those arrays and records prefab overrides
through Unity editor APIs, retaining CNP registration/filtering ownership.
No registration/projection framework added, no vendor source changed. Final
native2/2proves actual paused objective and three genuinely offscreen human
widgets; an initial diagonal fixture was corrected because it was onscreen.


### Terrier wind-only repair (ROOT preparation, unverified)

VENDOR ASSETS AUDITED: Existing NWH/WheelController TerrierRacer Step and
AirControl, shared SportsChaosWeather authoritative Flow, Weather Maker
preset application and Weatherade presentation. Existing code applied full
Flow with0.15ground/0.55air coupling and had no requested0.70mode multiplier.
VENDOR ASSETS USED: Existing weather Flow and native AddForce acceleration;
original weather, NWH driving/jump/stabilization/course systems retained.
RELEVANT ASSETS NOT USED: No new weather/flight/vehicle framework is needed.
CUSTOM SYSTEMS CREATED: Only an editable TerrierCourse response multiplier
and a controlled native-body wind-response test (not yet executed).
VENDOR SOURCE MODIFIED: None.
DUPLICATE VENDOR FUNCTIONALITY CREATED: None.


### Safe road recovery and wind response verification

VENDOR ASSETS AUDITED: Native NWH compound truck colliders, existing original
Taxi district LwsRoadGraph samples/restrictions, Physics overlap queries,
DamageHandler.Repair, CNP registration/indicator widgets and DOTween.
VENDOR ASSETS USED: Existing authored road samples/headings and NWH lifecycle;
PhysX checks the complete cached assembly envelope. No parallel road planner.
RELEVANT ASSETS NOT USED: No new vehicle, road, weather or navigation package
is needed for a mode-owned safe recovery placement decision.
CUSTOM SYSTEMS CREATED: FareFightMatch placement/clearance helpers and native
regressions; stalled-run diagnostics. Wind response native1/1 now passed
(old/default/disabled velocities0.11/0.07699997/0), not full race acceptance.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### Hidden wreck bounds and compact viewport presentation

VENDOR ASSETS AUDITED: Existing Core/SportsFragmentRig real-mesh wreck pool
hides meshes with zero scale; Unity Renderer bounds; Cinemachine TargetGroup/
GroupComposer/Collider; current Heat-backed SportsBroadcastCanvas and TMP
fonts, Broadcast truck portrait, SportsBroadcastPlate, and DOTween score punch.
VENDOR ASSETS USED: Same Cinemachine composition and CNP POI/widget lifecycle;
existing Heat/TMP styling/resources and DOTween.DOPunchScale.SetUpdate(true).
RELEVANT ASSETS NOT USED: No replacement camera, projection, UI framework,
font, portrait or destruction system. Existing assets cover these capabilities.
CUSTOM SYSTEMS CREATED: Tiny rendered-mesh eligibility filter; opt-in read-only
SportsCompactViewportHud layout over existing viewport roots. Terrier supplies
its own authoritative viewSlots/wins/status; camera/input ownership unchanged.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### BASE flare smoke and human Mattress recovery

VENDOR ASSETS AUDITED: NWH Vehicle Physics2 Effects/Prefabs/ExhaustSmoke.prefab and its native ParticleSystem/Renderer/material, URP ParticlesLit shader/texture dependencies, existing24-object BASE sky pool, SportsRallyViews/SportsCameraOptions, Heat/TMP/portrait/plate and DOTween presentation, Unity InputSystem1.19.0 installed editor focus behavior (actual package source inspected).
VENDOR ASSETS USED: Owned Flare Smoke prefab VARIANT of NWH ExhaustSmoke; four existing flares only. Native world-space particles/inherited velocity/wind force module; StopEmittingAndClear on pool retirement/reset. Existing PhysX mattress recovery/penalty; existing participant source routing and input gate. Shared read-only compact viewport adapter with DOTween contextual fade.
RELEVANT ASSETS NOT USED: No new particle, physics, input, camera or UI framework required; existing assets cover each responsibility.
CUSTOM SYSTEMS CREATED: Additive BASE reference migration, mode-owned human recovery choice and score-data adapters; explicit development-only Windows rendering fixtures. Native smoke1/1, compact/manual recovery6/0; actual Windows acceptance pending.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### Wife/Target compact presentation and owned checkpoint views

VENDOR ASSETS AUDITED: Existing SportsRallyViews grouped driver/passenger ownership, TargetMatch.Owners/TargetRules.totals, WifePair ordered Checkpoint/Progress/FinishTime, Compass Navigator Pro4 public cameraMain/viewportRect/follow/POIRegister, installed GizmoElements/outlined icon-arrow-white PNG, Heat/TMP viewport roots and DOTween. Vendor private indicator source inspected read-only to confirm its stock100px arrow and coordinate rotation.
VENDOR ASSETS USED: Same shared compact adapter and truck portrait/plate/fonts; four saved CNP prefab instances for Wife checkpoints, individual allowlists/cameras/viewports; existing native CNP projection/edge placement. Smaller28px participant-colored outlined arrows use the installed CNP sprite. Native13/0passed all five sports compact views, actual four owned checkpoint widgets, earlier recovery/smoke/wind regressions. Wife additive authoring repeated with identical scene SHA256. Windows follow-up pending.
RELEVANT ASSETS NOT USED: No alternative GPS, UI, camera, route planner or physics system needed. Wife guidance only reads its existing next ordered gate; no road search or scoring authority added.
CUSTOM SYSTEMS CREATED: Read-only Wife/Target presentation adapters, tiny Wife checkpoint-to-CNP binder, additive scene-reference tool and focused verification fixtures.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### Flare smoke visual root cause and owned flipbook repair

VENDOR ASSETS AUDITED: NWH ExhaustSmoke prefab UVModule disabled/stale5x5 settings, ExhaustSmoke.mat URP ParticlesLit/subtractive color keyword, actual SmokeTextureSheet.png6columns×5rows. Source atlas and actual Windows close-up inspected.
VENDOR ASSETS USED: Original NWH prefab through owned variant, original smoke texture and URP shader; native textureSheetAnimation Grid/WholeSheet/Lifetime6×5 and existing renderer/particle pool. Owned Flare smoke.mat copy changes tint/color mode without touching vendor material.
RELEVANT ASSETS NOT USED: No replacement texture, shader, VFX framework or new commercial package needed.
CUSTOM SYSTEMS CREATED: Explicit migration of only the known stock broken owned adaptation. Enabled/custom authored flipbooks/materials are retained. Native base-smoke-flipbook-native1/0passed6×5 animation, white multiplicative base, actual emission and pool/rematch cleanup. Windows visual repair acceptance pending rebuild.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### Wife startup sightline / Snake pickups, HUD, steering and town

VENDOR ASSETS AUDITED: Existing Cinemachine Transposer chase settings; Compass Navigator Pro4 public POI/camera/viewport lifecycle and owned compass prefab; NWH Vehicle Physics2 steering/powertrain public configuration and existing LWS TruckTaxiVehicleHandlingOverride; existing LwsRoutePlanner road access/query code inspected read-only, with private pending host planner excluded from public validation; Heat/TMP/DOTween compact viewport presentation; existing UTS traffic/pedestrian adapters and sport-owned World population profile; tracked JC_LP_MegaCity Tree03/Tree07, Commercial03 and House04_V1 prefabs/material dependencies.
VENDOR ASSETS USED: Wife saved Cinemachine stock-only offset override. Snake continues actual NWH steering/engine/brake authority; only owned instance handling fields change, with bounded human turn-speed target trimming. Existing shared route planner determines pickup accessibility. Four saved CNP prefab instances render shared/split objectives. Shared Heat/TMP/portrait/plate/DOTween HUD gains an optional telemetry row. Existing UTS profile increases logical counts while retaining live simulation budgets. Thirty-two owned asset trees and twelve actual asset buildings added beyond reserved roads using the established model fitting helper.
RELEVANT ASSETS NOT USED: No Vista/EasyRoads scene regeneration, replacement navigation/traffic, new weather, custom projection or separate UI framework required. Existing authored five-region terrain/roads and convoy physics remain intact.
CUSTOM SYSTEMS CREATED: Small old-pickup retirement predicate over the existing finite pool; read-only Snake telemetry/objective adapter, exposing the existing AI route query for legitimate objective selection; explicit idempotent sport-owned authoring and targeted native/Windows fixtures. No independent pathfinding algorithm, camera, weather or physics implementation.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


Traffic follow-up: the existing interstate lane builder rejects LocalRoad classes by design. Snake now restores the reused Fare/Taxi district authored loops through its sport-owned authoring adapter, and uses the existing ResetTraffic/ResetPopulation APIs for rematches. Native actual traffic59logical/58live/50moving observed; subsequent reset validation pending. Public CNP distance booleans disable metric labels in this mode. No vendor or shared Taxi source changes.


October22 final acceptance: exact545-file Windows payload85d6770a passed320assertions. Actual UTS traffic59logical/56live and four rematch starter fixtures passed. NWH owned shiftDuration.18 plus stock-only handling tuning measured4.40s to3.40s to20MPH in cold native comparisons. SnakeRouteAI retains existing planner/avoidance authority; only missed-objective replanning and stale-detour reset corrected. CNP/Heat/TMP/DOTween presentation reused. No vendor source or original Truck Taxi source modified; no duplicate vendor framework created. Human/controller/listening and isolated profiling remain unverified.


### Hockey presentation continuation

VENDOR ASSETS AUDITED: Heat ButtonManager public text/navigation/input references at Assets/Heat - Complete Modern UI/Scripts/UI Elements/ButtonManager.cs; existing SportsUIAuthoring/SportsBroadcastCanvas/Plate/Page/Scores, TMP fonts, saved Hockey six-slot menu hierarchy and DOTween public punch/tween lifetime APIs.
VENDOR ASSETS USED: Existing Heat setup/pause/rematch actions, existing owned Heat-derived heading/body fonts, actual broadcast truck portrait, SportsBroadcastPlate rounded geometry and DOTween feedback. No new generic UI framework; HockeyPresentation only binds read-only HockeyRules/player data to the existing menu.
RELEVANT ASSETS NOT USED: No camera/physics/traffic/weather replacement needed for this UI repair. Existing sport-specific Hockey rules and cameras intentionally retained.
CUSTOM SYSTEMS CREATED: Small Hockey presentation adapter, explicit additive saved-scene authoring, targeted native/Windows verification fixtures.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### Musical Parking elimination display

VENDOR ASSETS AUDITED: Existing SportsBroadcastScores card hierarchy, SportsEventMenus viewport roots, Heat-derived TMP fonts, actual truck portrait, red lock and capture glyphs, shared camera/input preferences and ParkingMatch active/secured/capture/penalty authority.
VENDOR ASSETS USED: Same shared rounded cards, portraits, fonts and existing lock/progress components. New opt-in semantic values are supplied by ParkingMatch; no independent HUD framework or duplicate camera/physics system.
RELEVANT ASSETS NOT USED: No new art generator, navigation, physics or weather package required for icon-based elimination presentation. Existing parking rules/AI intentionally preserved.
CUSTOM SYSTEMS CREATED: Small elimination-layout extension to the established shared score-card adapter and targeted verification fixtures.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### Parking numbered bay presentation (work in progress)

VENDOR ASSETS AUDITED: installed JC_LP_MegaCity public subset, RoadPropsPack/URP/Prefabs/RoadSign_URP.prefab availability,existing Parking bay paint/markings,Heat/TMP fonts/shared portrait/rounded plate and DOTween state feedback.
VENDOR ASSETS USED: existing SportsUIAuthoring and Heat-derived theme,shared truck portrait,SportsBroadcastPlate,DOTween,original painted boundary geometry and a sport-owned clone of stock Bay marking enamel. One shared WorldSpace canvas for all8markers; no new physics/UI framework.
RELEVANT ASSETS NOT USED: vertical RoadProps signage is unnecessary for readable painted bay-state identifiers and would add objects around the capture area. No new vendor raw assets introduced. Existing UI foundation covers presentation; LWS only binds ParkingBay ownership/open state.
CUSTOM SYSTEMS CREATED: small read-only ParkingBayAppearance adapter,explicit additive editor authoring and focused verification fixture.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.


### Bungee city buildings and contact momentum (in progress)

VENDOR ASSETS AUDITED: Assets/CoreCollapse/Core-Collapse.pdf baseline;Scripts/Structural/CollapserPrefrag.cs,Collapser.cs,Support.cs,Pushable.cs;EzySlice/SlicerExtensions.cs and SlicedHull.cs;JC_LP_MegaCity building prefab bounds/materials;existing Bungee module/support/force/drop/debris lifecycle.
VENDOR ASSETS USED: Core-Collapse prefractured damage/support APIs and EzySlice offline cutting;JC_LP_MegaCity House_06_V1,Hotel_01,House_04_V1 models and authored materials;existing NWH trucks,SpringJoint/cable,Heat UI,Cinemachine cameras and installed audio.
RELEVANT ASSETS NOT USED: Core-Collapse procedural runtime slicing is unnecessary when pre-cut owned model assets avoid runtime slicing cost. Pushable models a radial push and initializes on Start;this correction hands off actual incoming contact momentum during prefab replacement in Awake. Existing vendor damage/fracture remains authoritative. Rope Toolkit remains unavailable;established explicit fallback retained. No new rope/physics framework.
CUSTOM SYSTEMS CREATED: explicit sport-owned model authoring/preview and verification fixtures;small bounded contact-context handoff between existing Bungee adapters. Repeated middle storeys preserve facade proportions.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO;vendor slicing,damage,support and prefab fracture remain authoritative. LWS owns contact attribution,momentum handoff,scoring and lifecycle.


### Minefield outer mountain ranges

VENDOR ASSETS AUDITED: Vista TerrainGraph, TerrainGenerationConfigs, HeightOutputNode, DataPool/GraphRenderTexture lifecycle, installed mountain graph, sample pine/grass/rock layers, existing Minefield terrain/scoring integration and NWH authority.
VENDOR ASSETS USED: Vista ExecuteImmediate through owned terrain variants; sample pine/terrain layers through sport-owned variants; native TerrainData, TerrainCollider and tree instancing/LOD. Existing NWH, Weather Maker, Weatherade and camera/UI systems remain authoritative.
RELEVANT ASSETS NOT USED: Separate cliff meshes would approximate the continuous colliding range already provided by Vista terrain. No new roads, weather, vehicle, input or streaming framework is needed.
CUSTOM SYSTEMS CREATED: Explicit course-preserving terrain integration, runout-edge stitching and focused native/Windows verification. No runtime terrain/noise generator.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

### Billiards graphical gameplay HUD

VENDOR ASSETS AUDITED: Installed Heat ProgressBar and TMP fonts; shared broadcast theme, truck portrait and rounded plates; DOTween; existing Billiards HUD/session/input/rules.
VENDOR ASSETS USED: Heat ProgressBar, existing Heat-derived heading font, uGUI/TMP, built-in round UI sprites, shared portrait/rounded plate, DOTween turn-marker animation.
RELEVANT ASSETS NOT USED: No replacement menu framework, camera system or logo generation is needed for this HUD pass.
CUSTOM SYSTEMS CREATED: Read-only HUD presentation adapter, explicit additive authoring tool and focused verification. Existing HUD refresh remains the sole update authority.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

### Bowling HUD-only modernization

VENDOR ASSETS AUDITED: Heat ProgressBar/font assets, DOTween, existing broadcast theme/portrait/rounded plates, saved Bowling HUD/lane ownership and existing scoring/control fixtures.
VENDOR ASSETS USED: Heat ProgressBar, existing Heat-derived TMP font/truck portrait, uGUI, shared rounded plate and DOTween bounded score feedback. Existing NWH, alley art, cameras, replay and pinsetter are unchanged.
RELEVANT ASSETS NOT USED: Replacement environments, physics and cameras are outside the approved UI-only scope. No new logo artwork is needed.
CUSTOM SYSTEMS CREATED: Read-only lane HUD adapter, explicit saved-UI authoring and targeted verification. Existing BowlingMenus remains the refresh/lifecycle authority.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

### Air Hockey and Scrap Heap

VENDOR ASSETS AUDITED: Existing Heat controls/fonts, DOTween, broadcast theme/portrait/rounded plates, source/focus/reconnect handlers and catalogue.
VENDOR ASSETS USED: Existing Heat/uGUI/TMP UI, DOTween, shared portrait/rounded plates and original PNG logos/gameplay screenshots.
RELEVANT ASSETS NOT USED: No replacement physics, camera, input or artwork-generation framework required.
CUSTOM SYSTEMS CREATED: Read-only presentation adapter/display clock, explicit saved-UI/category authoring and focused verification.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

### Poker table presentation

VENDOR ASSETS AUDITED: Heat buttons/fonts, DOTween public API, existing original card/vector-suit renderers, private panes, source handlers, mobile dealer, restaurant and broadcast presentation.
VENDOR ASSETS USED: Existing Heat/uGUI/TMP, original portrait/font/rounded plates, DOTween public gaze interpolation and original action/award/dealer animation.
RELEVANT ASSETS NOT USED: No replacement poker engine, card renderer, input framework, physical controller or new artwork is required.
CUSTOM SYSTEMS CREATED: Public-only read-only HUD/award captions, safe ambient visual pivots, explicit idempotent authoring and targeted tests/probe.
VENDOR SOURCE MODIFIED: NO.
DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

### Cheese Rolling owned presentation

VENDOR ASSETS AUDITED: Milo the Cat/MalberS owned installation and actual rig/clips; GAPH Particle Ingredient Pack examples/materials/textures; Core-Collapse example layers; existing Heat/TMP/DOTween, NWH, Cheese physics/Weather Maker/accumulation integration.

VENDOR ASSETS USED: Owned Milo avatar/four clips; GAPH Explosion3 artwork with project-owned URP conversion; existing Heat fonts, original truck portrait, uGUI/TMP, DOTween and Unity Animator/ParticleSystem APIs; original NWH race and existing weather.

RELEVANT ASSETS NOT USED: Core-Collapse's inspected example emphasizes shockwave/dust; the owned GAPH example supplies the requested distinct fire/smoke/spark layers. No replacement vehicle, damage, weather, navigation or UI framework is needed. Cat walking/running does not replace stable scenery colliders.

CUSTOM SYSTEMS CREATED: Small visual adapters, explicit idempotent authoring, owner-only licensed intake scripts, saved HUD configuration and targeted tests/probes. No custom animation rig, particle engine or vehicle physics.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.

### Dock finished venue and stable bubbles

VENDOR ASSETS AUDITED: Existing UTS clothed female/male prefabs, exact generic Bip01 rigs and original controller clips; Construction Props timber, MegaCity fences/building, installed Weather Maker normals and existing shared water/reflection/URP integration, Heat/TMP/DOTween, NWH, existing FieldGoalCrowd and SportsAudienceAuthoring.

VENDOR ASSETS USED: Eight owned UTS clothed models with their compatible idle/dance/applause clips and textures; Construction Props and MegaCity art; existing original logo, Heat/TMP/uGUI, DOTween water/fades, shared water spray/ripple/audio/replay, Unity ReflectionProbe/BakeMesh and original NWH driving.

RELEVANT ASSETS NOT USED: UTS traffic brains/ragdoll/nav components are unnecessary for protected render-only spectators. FieldGoal run-on antics are not introduced into Dock scoring. No replacement water/vehicle/weather/input engine or new logo is needed; the existing URP-compatible normal/flow and shared interaction systems are reused.

CUSTOM SYSTEMS CREATED: One bounded runtime pose-bake/binding adapter over existing crowd reactions, explicit saved venue authoring, focused bubble anchor filter and targeted diagnostics/tests. Pool/enclosure semantics remain LWS-owned.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
