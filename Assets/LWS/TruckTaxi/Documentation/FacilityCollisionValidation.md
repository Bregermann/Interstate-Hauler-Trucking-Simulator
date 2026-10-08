# Phase 0.5 / 0.6 facility collision handoff

## Evidence and scoped repairs

Unity 6000.4.10f1; source, targeted tests and development-player diagnostics. The exact human-observed reverse-impact
wall and flashing road/ground pair remain UNIDENTIFIED without runtime evidence.
No arbitrary wall, road, terrain, regional collider, or vendor asset was removed.

Two source-confirmed generated-art lifecycle defects were corrected in
`TruckTaxiPhysicalFacilities`:

- Generated children belong to service points/the player, not the generator's
  hierarchy. Previously destroying the generator destroyed their materials but
  left those children and their colliders alive. Teardown now deactivates and
  retires only the exact roots/filler tracked by this generator.
- Turning `generateValidationFacility` off after construction previously left
  existing validation art behind. Refresh now retires its tracked instance when
  the flag is off, its service is removed, or its stable ID changes. Authored
  siblings, including identically named objects, are preserved.

These fixes prevent specific stale generated geometry. They do not establish
the cause of the reported wall or large flashing road surface. Normal facility
dimensions, material depth settings, and road colliders are unchanged.

## Parent hook

After the Truck Taxi player has been created, add this in the parent-owned
bootstrap path (Bootstrap was NOT edited by this workstream):

```csharp
TruckTaxiSignificantCollisionDiagnostic.Attach(Player.gameObject);
```

Parent confirmed this hook was wired after `sensor.Initialize` during handoff.

The call is conditional on `UNITY_EDITOR` or `DEVELOPMENT_BUILD`. It attaches at
most one observer, subscribes to NWH `VehicleController.onCollision`, and removes
the listener on disable. It has no Update loop or global scene search, and never
changes colliders, physics, collision scoring, or component damage.

`TT SIGNIFICANT COLLISION` records full hierarchy/sibling identity, scene handle,
object/instance, collider type, layer number/name, parent, world bounds, local box
dimensions, direct renderer state, visibility/bounds review, impulse, relative
speed and up to four contacts. Duplicate status is explicitly UNVERIFIED;
same-object solid count is evidence, not a duplicate verdict. Defaults accept
relative speed >= 0.75 m/s OR impulse >= 1500 Ns, at most once per second globally
and once per collider per five seconds. The throttle retains at most 64 IDs.

For a staged teleport fixture, explicitly call
`Player.GetComponent<TruckTaxiSignificantCollisionDiagnostic>().CaptureLocalOverlaps("rain post-teleport")`.
This dev-only API is limited to once per unscaled second and 128 broadphase hits
within the tractor body AABB plus 0.5m. It reports static/kinematic/dynamic body
identity, velocity, ignored pairs/layer overrides and supported-shape penetration
depths. It never clears traffic or moves/disables geometry. Saturation is explicit;
an unsaturated bounds query is still not proof of contacts or visible obstruction.
Call before/after teleport and once while immobile; no automatic polling is added.

## Clockout staging evidence

Fresh parent evidence: town driving succeeded, the rain stage became nearly
immobile after teleporting to `taxi.stop.00` (about 2.5m/25s), earthquake remained
immobile, and a highway teleport freed the truck. The earlier continuous-map
control passed this spot. This distinguishes runs, not collider identity.

Checked-in source/scene facts (no scene edited or opened in Unity):

- `TruckTaxi_DemoCity.unity` Clockout Offices (object 2115350692) has only
  Transform/RideLocation. Its `taxi.stop.00` bay is (-307, 0.16, -260), identity
  rotation; `TeleportNear` requests tractor root (-307, 1.76, -260).
- Clockout is locationType Apartment, with no service/store/gas/bathroom component.
  Normal service binding does not generate a validation facility there. Runtime
  additions must be checked separately; generated facility blockage is not proven.
- `Taxi_Town01/Clockout Offices scenery/Taxi bay`: BoxCollider 1681723481,
  layer 0, enabled non-trigger; renderer enabled. Center (-307, 0.09, -260), size
  (9, 0.08, 22); bounds X[-311.5,-302.5], Y[0.05,0.13], Z[-271,-249]. This is
  the expected low supporting slab, not an oversized vertical wall.
- `Taxi_Town01/Clockout Offices scenery/Taxi stop marker`: BoxCollider 831372141,
  layer 0, enabled non-trigger; renderer enabled. Center (-301,1.66,-255), size
  (0.25,3,0.25); bounds X[-301.125,-300.875], Y[0.16,3.16], Z[-255.125,-254.875].
  This visible post is 6m right and 5m forward of the bay; retain it pending contact
  evidence. Duplicate status for both objects is runtime-unverified.
- The co-located `TT_SCENIC_North_View` at (-307,0.2,-260) has only semantic
  components and Transform in the checked-in scene: no solid collider.
- The perf probe reuses the stuck position for earthquake and does not teleport
  again until highway staging. Persistence across those two stages alone does
  not establish an earthquake defect. `TeleportNear` does not reserve a clear
  traffic-free destination; an occupied bay remains a runtime hypothesis.

Capture actual contacts/penetrations before changing these objects. No fix or
deletion to Clockout scenery, staging, traffic or weather was made here.

## Existing authoring validator extended

Use `Truck Taxi/Map/Validate Services And World Rings` for loaded scene roots,
or `Truck Taxi/Map/Validate Selected Service Authoring` for selected hierarchies.
Select the service plus surrounding geometry when using the focused action.
The callable API is `TruckTaxiServiceMapValidator.ValidateAuthoring(roots)`.
No automatic repair, object mutation, scene opening, or save occurs.

Checks include inactive duplicate service/map/interaction IDs, missing toilet IDs,
missing service data/capabilities/bays, invalid radii, hidden or oversized solids,
disabled-renderer/live-collider pairs, coincident solid bounds, duplicate meshes,
coplanar top-bound overlap, and a conservative 3x3x8m parking clearance sample.
Existing `TruckTaxiWorldColliderReview` and `TruckTaxiWorldGeometryAuthoring`
provide the visibility/bounds and exact/coplanar rules; there is no second world
validator or automatic deletion tool.

Bounds candidates are not triangle/contact proof. Legitimate separate visual and
collision hierarchies, LODs, sloped surfaces and helper passes require review.
The focused surface pass uses MeshRenderers >= 1m2 XZ area and thin (<= 2m) bounds
for near-coplanar checks. It is not a full triangle, floating/buried-prop,
terrain, destructible-classification, doorway, route-connectivity or streaming
seam validator. The existing world audit remains the broader scene/terrain/
residency evidence path; loaded-scene-root checks do not include DontDestroyOnLoad.

## Verification and parent acceptance

- Added 12 EditMode tests in `TruckTaxiFacilityCollisionValidationTests` for IDs,
  visibility/oversize, duplicate bounds, coplanar/height/edge cases, bay clearance,
  diagnostic contents, impact thresholds, bounded throttling, read-only repeated
  audit. All passed within the 83/83 focused suite in
  `Logs/TaxiFoundation-Phase0-EditMode2.xml`.
- Exact-instance generator teardown belongs in PlayMode: ordinary MonoBehaviour
  destruction is not a valid EditMode lifecycle fixture. It now passes there,
  preserving an identically named authored sibling while retiring tracked roots
  and filler (`Logs/TaxiFoundation-Phase0-PlayMode4.xml`, 5/5 integrated tests).
- Windows build succeeded with zero errors. No scene/YAML or vendor source edits.
- The corrected visible performance probe drove 315m in rain and 346m during
  earthquake with 100% focus (`Logs/TaxiFoundation-Phase0-Focused.json`). The
  previous immobility was not reproduced after fixing the probe's engine toggle
  and unsolicited dispatch-modal setup. This does NOT identify the human wall.
- NWH contacts in the development-player log include exact road hierarchy/bounds;
  no evidence justified deleting those authoritative supporting road colliders.
- In the visible Windows development player, reverse at the reported service;
  preserve the impact log and select that exact collider. Compare renderer state,
  bounds and scene residency. Repeat contacts to verify throttling.
- Capture the original flashing location/camera and overlapping renderer pair;
  use existing loaded/authored world audits, then verify individual draw calls
  and co-residency before any targeted repair. Preserve collision continuity.
- Verify generator teardown/recreation and the validation-art flag, plus service
  access, solid authored walls and terrain after load/unload. Human acceptance
  and the feature-freeze decision remain parent-owned.

## VENDOR ASSETS AUDITED

- NWH Vehicle Physics 2: `Assets/NWH/Vehicle Physics 2/__ Readme.txt` and
  `Scripts/VehicleController/VehicleController.Collision.cs`; installed version
  not established. Public `UnityEvent<Collision> onCollision` inspected directly.
- EasyRoads3D Pro: `Assets/EasyRoads3D/_Readme.txt`; existing LWS DemoBuilder/
  RegionalAuthoring road mesh/collider integration and world-geometry handoff.

## VENDOR ASSETS USED

NWH `VehicleController.onCollision.AddListener/RemoveListener` for diagnostic
input. Existing NWH physics and EasyRoads-authored geometry remain authoritative.

## RELEVANT ASSETS NOT USED

No EasyRoads rebuild, terrain regeneration or vendor physics replacement is
needed for owned-object cleanup and Truck Taxi stable-ID/bounds diagnostics.
These are project-specific lifecycle and authoring checks, not vendor road or
vehicle simulation functionality.

## CUSTOM SYSTEMS CREATED

One development-only NWH collision observer with bounded log throttling;
extensions to the existing service validator, focused tests and this handoff.

## VENDOR SOURCE MODIFIED

NO.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

NO.
