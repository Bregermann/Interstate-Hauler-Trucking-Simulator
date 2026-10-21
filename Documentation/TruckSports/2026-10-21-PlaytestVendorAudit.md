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
