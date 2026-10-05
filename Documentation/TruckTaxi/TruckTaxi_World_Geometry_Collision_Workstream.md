# World geometry and parked-depot collision workstream

Scope: latest consolidated prompt sections 29-30. Code/tooling only. No Unity
launch, compilation/build, scene/prefab save, world regeneration, or commit was
performed. Parent owns serialized authoring and runtime acceptance.

## Evidence, not a flicker-fix claim

- `Assets/LWS/TruckTaxi/Editor/TruckTaxiRivalDepotAuthoring.cs`, `EnsureDepot`,
  disables every collider on each visible parked UTS truck. The persistent
  `TruckTaxi_DemoCity.unity` contains four parked instances and overrides the
  vendor chassis component (file ID `65109294934475136`) to `m_Enabled = 0`.
  This is direct authoring/serialized evidence for missing parked collision.
- Installed `Assets/UTS_FullPack/Models/Cars/Car_Prefabs/Day Cars/Truck_1.prefab`
  has a solid `Box_Collider`, a trigger `Checking Box`, and wheel colliders.
  The solid box's authored transform gives approximately 2.39 x 0.57 x 5.96 m
  chassis coverage. Re-enabling it alone does not cover the upper visible cab.
- `TruckTaxiRegionalAuthoring.Starts/Ends` deliberately overlap adjacent chunks
  by 40 m and use the same intervals for geometry. Serialized Town01Outskirts
  ground is centered at X=680 with width=640; Highway ground is X=1230,
  width=540. Both have Y=-0.55, height=1, depth=1000. Their ground volumes
  overlap X=960..1000 and their top faces are at Y=-0.05.
- Serialized Highway ground also overlaps the RiverBridge west-bank cube
  (X=1620, width=320) at X=1460..1500 with the same top height/depth.
  Other builder-defined boundary candidates include X=2500..2540 and
  X=3060..3100. Road meshes may overlap too; never trim an entire road based on
  a ground-slab finding.
- These are concrete candidates, NOT confirmation of the user's exact
  middle-world flicker. No affected-camera capture or Unity reproduction was
  obtained. Sloped mesh intersections, snow/depth helpers, terrain overlap,
  persistent geometry, and runtime duplication remain investigative items.
- Existing `LwsSceneStreamerAdapter.RequestLoadScene` checks both loaded scenes
  and pending names. Do not claim its loader duplicates regions without a live
  capture. The new loaded audit compares SceneManager, LWS metadata/manifest,
  adapter status, and existing vendor instances' public `IsLoaded` query.

## APIs and menus

Namespace: `LWS.TruckTaxi.Editor`.

`TruckTaxiWorldColliderValidator.AuditAuthoredWorld()` returns a serializable
`TruckTaxiWorldAuditReport`. It reads saved DemoCity plus ALL scenes currently
under `Assets/LWS/TruckTaxi/Regional/Scenes` in isolated preview scenes, closes
them in `finally`, and does not save or close designer scenes. Unsaved edits are
not included. It compares surface snapshots across scene boundaries.

`TruckTaxiWorldColliderValidator.AuditLoadedWorld(playerBodyColliders)` observes
currently loaded scenes plus persistent DontDestroyOnLoad renderer scenes. In
Play Mode pass the actual NWH player body colliders for pair-ignore checks.
Without them, player contact remains explicitly unverified.

```csharp
var report = TruckTaxiWorldColliderValidator.AuditAuthoredWorld();
TruckTaxiWorldColliderValidator.WriteReport(report, "Logs/TruckTaxiAuthoredWorldAudit");
```

Menu entries under `Truck Taxi/Validation`:

- `Audit Authored Geometry And Parked Trucks`: JSON and Markdown in
  `Logs/TruckTaxiAuthoredWorldAudit.*`.
- `Audit Loaded Geometry And Scene Residency`: JSON and Markdown in
  `Logs/TruckTaxiLoadedWorldAudit.*`. The menu has no live-player argument;
  use the API for full player pair checks.
- `Repair Selected Parked UTS Truck`: explicit single selected instance,
  canonical NWH prefab body layers, default collision layer 0. Does not save.
- Existing `Review Solid World Colliders` now also covers DemoCity and uses
  preview scenes rather than opening additive authored scenes.

Reports include scene paths/handles, hierarchy/sibling identity, mesh/material
asset IDs, bounds, overlap area, vehicle coverage warnings, ignored layers/pairs,
and streaming observations. Surface matches are candidates, never automatic
deletion decisions. Detection focuses on large XZ surfaces (>=25 m2), exact
mesh/material/transform copies, thin equal-top AABBs and terrain snapshots.
It does not perform exhaustive triangle intersection or shader-depth analysis;
zero findings are not visual acceptance. Renderer LODs/helper passes can require
manual exclusion. Vehicle coverage uses conservative bounds, not a contact test.

## Targeted repair contract

All operations require Edit Mode and project-owned source-scene objects (unsaved
scenes are allowed for tests). Prefab assets, preview scenes, and vendor scenes
are refused. APIs use Undo, mark only changed scenes/objects, never save, never
apply prefab overrides to a source asset, never rebuild anything, and never
change the global collision matrix. A second identical application is a no-op.

```csharp
var player = TruckTaxiWorldColliderValidator.CanonicalPlayerBodyColliders();
int changes = TruckTaxiWorldGeometryAuthoring.RepairParkedUtsTruck(parked, player);
```

Only known `Parked UTS taxi truck ...` Truck_1 prefab instances within a
`TruckTaxiRivalDepot` qualify. The repair enables the existing vendor chassis
box and adds/updates one `LWS Parked Cab Collision` box. Cab bounds are fitted
from upper body mesh vertices in truck-local coordinates, with size guards;
there is no fallback giant vehicle box. Kinematic/no-gravity/detect-collisions
state is enforced. Driving/trailer MonoBehaviours and wheel/trigger colliders
are NOT enabled. Unknown/customized structures are refused for review.

The requested layer must collide with all supplied player body layers and UTS
Car layer 9. Per-collider layer overrides or ignored player pairs cause refusal,
not a global settings rewrite. The existing `TruckTaxiTrafficBehaviour.Sense`
uses a non-trigger box cast and accepts kinematic obstacles; no new traffic AI
or obstacle manager is added. NWH and existing impact observers retain authority.
Gizmos now include kinematic parked solids and draw oriented BoxCollider shapes.

Unity permits mesh reads in Edit Mode without changing importer Read/Write:
[Unity Mesh.isReadable documentation](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/mesh/isreadable).

```csharp
int retired = TruckTaxiWorldGeometryAuthoring.RetireExactDuplicateRenderer(
    authoritativeRenderer, obsoleteRenderer, coResidencyConfirmed: true);
int trimmed = TruckTaxiWorldGeometryAuthoring.TrimRegionalGroundOverlap(
    authoritativeGround, redundantEdgeGround, coResidencyConfirmed: true);
```

These require explicitly reviewed object pairs, not auto-selected audit hits.
Confirm the surviving source stays resident wherever the removed coverage is
needed, including both approaches to a streaming boundary. Merely loading both
scenes in the Editor is insufficient evidence.

Exact retirement requires matching shared mesh/material/transform and relevant
render settings. It removes only the redundant MeshRenderer at source; all
colliders, MeshFilters, GameObjects, and surface metadata remain. LOD, moving,
and prefab renderer retirement is refused.

Ground trimming supports only audited axis-aligned built-in Cube slabs named
`Regional ground`, `West river bank`, or `East river bank`, with a unit solid
BoxCollider. It removes a partial X-edge overlap while preserving the combined
visible/physical footprint and top height. It refuses roads, terrain, arbitrary
meshes, interior overlaps, containment, custom components, layer differences,
and rotation/shear. Road meshes are NEVER trimmed by this operation. Inspect
collider boundaries and streaming residency before explicitly saving.

## Parent execution and acceptance

1. Import code in the parent's existing Unity session. Resolve compile errors
   before authoring. Run EditMode fixture
   `LWS.TruckTaxi.Tests.EditMode.TruckTaxiWorldGeometryAuditTests` (10 tests).
2. Run the authored audit before editing. Capture the exact reported flicker
   location, camera pose, loaded audit, and visible offending pair. Compare
   wireframe/Frame Debugger draws and helper visibility. Establish the cause.
3. Select each of the four parked depot Truck_1 instances in DemoCity and run
   the selected repair. Inspect chassis/cab gizmos at both orientations. Re-run
   each operation: expect zero changes. Review and explicitly save DemoCity.
4. Only after evidence, invoke the appropriate duplicate/ground pair operation.
   Review the source diff, Undo/Redo, collider continuity and streaming approach
   in both directions; explicitly save only the intended source scenes.
5. Re-run authored and loaded audits. Drive the canonical NWH tractor into each
   parked cab/chassis from front, side and rear. No pass-through or large empty
   collision volumes; qualifying impact/damage follows existing rules. Observe
   ambient UTS avoidance near the lot. Repeat after unload/reload/save/reopen.
6. Drive the exact original flicker location and adjacent road/bridge boundaries.
   No flicker claim is valid until this succeeds with preserved road physics.

No existing builder was changed. Do not rerun the full regional/depot generators
to apply these repairs. If the parent wants prevention integrated into future
generation, first approve/own `TruckTaxiRivalDepotAuthoring.cs` for a call after
its parked conversion and `TruckTaxiRegionalAuthoring.cs` for separating chunk
load bounds from nonoverlapping surface authoring. This workstream does not
claim those builder follow-ups are complete.

## Verification status

- Five changed/new C# files parsed with Roslyn: zero syntax errors. This is NOT
  Unity compilation, semantic type checking, or test execution.
- Scoped `git diff --check`: no whitespace errors.
- Added 10 EditMode tests: exact-only retirement, collider preservation,
  residency guard, different-mesh refusal, overlap trimming/idempotency,
  Undo/Redo, unsupported-shape refusal, near-plane/edge rejection, missing/
  trigger/wheel/chassis-only collision, player-ignore reporting, read-only
  evidence, and installed UTS repair/idempotency/layer preflight/vendor safety.
- Unity EditMode tests, serialized repair, visual fit, UTS behavior, NWH contact,
  save/reopen and original flicker reproduction: NOT RUN, parent-owned.
- No Unity launch, build, commit, vendor source change, scene/prefab YAML edit,
  or unrelated dirty-file restoration was performed.

## VENDOR ASSETS AUDITED

- UTS Full Pack: installed Truck_1 prefab above; `Scripts/Car/CarAIController.cs`;
  existing LWS `TruckTaxiTrafficBehaviour.Sense` and depot authoring.
- NWH Vehicle Physics 2: canonical project-owned
  `Assets/LWS/TruckTaxi/Prefabs/TruckTaxi_InterstateTractor.prefab` collision
  context and existing `TruckTaxiCollisionObserver`; physics authority preserved.
- EasyRoads3D Pro: `Assets/EasyRoads3D/_Readme.txt`, regional baked mesh authoring
  in `TruckTaxiRegionalAuthoring` and public-API use in `TruckTaxiWorldBuilderRoads`.
- Scene Streamer: `Assets/Plugins/Pixel Crushers/Scene Streamer/Scripts/SceneStreamer.cs`,
  public instance `IsLoaded`, existing LWS adapter (documents vendor 1.26.3).
- Weatherade SRS readme and Weather Maker river/helper authoring were inspected
  as visual-overlap context, not established causes. Other exact vendor versions
  were not established. Project Unity version: 6000.4.10f1.

## VENDOR ASSETS USED

UTS Truck_1 body mesh and original chassis BoxCollider, existing LWS/UTS sensing;
canonical NWH player colliders/physics; Scene Streamer public instance registry
query through the read-only LWS editor diagnostic. Existing EasyRoads geometry
and collision remain untouched.

## RELEVANT ASSETS NOT USED

No EasyRoads rebuild, terrain tooling, Weatherade/Weather Maker visual rewrite,
or streaming mechanism replacement. No audited vendor provides this project's
cross-source geometry evidence report and targeted depot-instance repair.

## CUSTOM SYSTEMS CREATED

One project-owned editor repair helper and focused audit/report extensions, plus
tests. No new runtime physics, traffic, streaming, or world-generation authority.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
