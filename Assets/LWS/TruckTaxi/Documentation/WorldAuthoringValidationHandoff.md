# Priorities 26-29: bounded editor checkpoint

Update: the embedded Spline Spawner package was subsequently located. Earlier Assets-only discovery blockers below are superseded by SplineDressingHandoff.md and its real-vendor profile/binding checkpoint. Approved prefab pools and parent acceptance remain outstanding; no package installation is needed.

Source-only work. No Unity, build, tests, regeneration, scene opening, scene/prefab/YAML edits, imports, commits or saves were performed by this workstream. These changes require parent compilation and EditMode execution. Existing authored content is preserved. This is NOT a claim that all world-content requirements are complete.

## Existing entry points extended

- `Truck Taxi/Validation/Audit Authored Geometry And Parked Trucks`: same existing audit, now also aggregates semantic IDs and scene snapshots across its isolated previews. No second builder or separate validator menu. The existing command opens/closes previews only when the parent invokes it; it writes reports under Logs, not scene changes.
- `Truck Taxi/Validation/Audit Loaded Geometry And Scene Residency`: same additions for loaded scope, including unsaved content. Does not force-load other scenes.
- `Truck Taxi/Map/Compose Selected Service Location`: editor window attached to the existing service validator. Supply actual scene facility art, an existing RideLocation with a truckStopPoint and stable location ID, and explicit capabilities. The operation composes service/store/refuel/restroom components and a service-bound map marker with real references. Icon category derives from the existing capability presentation authority, not a guessed venue name. Sleep/food/repair semantics remain owned by existing service consumers. No art, anchors, roads or scenes are invented.

Composition adds capabilities, preserves existing service ID, creates `service.<locationId>` only when absent, rejects conflicting existing identities/bay ownership and duplicate loaded service/map IDs before mutation, supports Undo and does not save. A repeat with identical inputs reuses components and avoids dirtying unchanged serialized values. Facilities with multiple bays must use distinct service owner objects and distinct authored RideLocations. An explicit source audit is still needed for unloaded-scene duplicate IDs.

## Validation coverage

The existing hidden-solid, disabled-renderer/live-collider, oversized-collider, duplicate-mesh, coplanar-bounds, capability, toilet and parking checks now join the world report. Additional checks cover:

- Cross-scene duplicate/missing IDs for locations, services, map entries, world interactions and classified props. Domains are separate because a service and its presentation intentionally share an ID.
- Missing truck/passenger anchors; nearest-road coverage within 80m and road/stop height discrepancies over 3m.
- Bidirectional reachability from a deterministic node using the existing LwsRoutePlanner. This detects disconnected graph components and one-way traps; it is not a physical truck drive or proof of lane restrictions. Explicit width/height/truck restrictions are reported separately because the current planner does not enforce them.
- Road sample/node endpoint discrepancies over 0.25m; uncovered regional bounds sampled at up to 10m intervals along every road segment (capped at 10,000 subdivisions per sample segment). Missing/duplicate region IDs and unavailable coverage are explicit findings. The existing manifest validation and vendor residency queries remain in place. Sub-sample gaps, terrain triangles, scene load timing and traversability still require acceptance.
- Classified prop support via scoped Collider.Raycast, excluding its own hierarchy: missing support, bottom more than 0.3m above support, or more than 0.5m below. These are review candidates only; center support cannot establish a complete footprint, intent or foundation depth. Unclassified scenery is not guessed into a prop category.
- Invalid breakaway enum/load-bearing classification, absent collision/nonconvex moving mesh candidates, missing/disabled prop art.
- Explicit interaction-anchor approach-volume overlaps and missing-anchor coverage. Conservative 0.6x1.8x0.6m bounds are candidates, not door traversal/NavMesh proof; authored moving doors may legitimately intersect them.

No arbitrary geometry is deleted, moved or repaired by the audit. Existing narrowly guarded geometry repairs remain separate explicit commands.

## Exact outstanding content and integration blockers

1. Priority 28 is NOT implemented: no installed Spline Spawner implementation was located by asset filename searches; Packages/manifest.json identifies Unity Splines 2.9.0, which is not proof of the purchased spawner. Parent/designer must identify the vendor package/profile API before density integration. No replacement generator was written.
2. Eight dressing contexts still need approved actual prefab pools and vendor profile references: forest, suburb, urban, industrial, highway, motel district, truck stop, construction. No density assets were created. Deterministic seed, lane/bay/entrance/route exclusions and vendor generation must be wired and validated together; these are not certified by this audit.
3. Existing facility art and authored truck/passenger/interaction anchors are required inputs. This work does not create final diner/motel/truck-stop shells, operable doors, toilet/porta-potty pieces, destructible meshes, collision hulls or entrance paths. The parent-run audit must supply exact offending scene/object identities; without running it, no specific scene defect is claimed fixed.
4. One-click authoring for companion stops, rival depots, scenic/hazard content, individual toilet pieces and structural/destructible classification remains in existing specialist tools/Inspector workflows, not fully unified here. RaceAuthoring and PolishRideAuthoring were deliberately untouched. Generic service composition cannot substitute for those explicit gameplay references.
5. Existing 3x3x8m bay and approach bounds require canonical-tractor swept-clearance tests, all-bay ingress/egress tests, and runtime streaming seam traversal. No such runtime evidence exists from this workstream.
6. Priority 29 honored: no voice generation, reference searches, cloning, guessed imports, radio changes or audio assets. Subtitles and empty AudioClip fields remain valid; future user-supplied WAV references belong to the later voice pass.

## Files / tests

- Editor/TruckTaxiWorldColliderValidator.cs: integrates aggregation into both existing audits.
- Editor/TruckTaxiWorldColliderValidator.Semantics.cs: partial implementation of the SAME validator, snapshots, route/seam and placement checks.
- Editor/TruckTaxiServiceMapValidator.cs: referenced composition API/window.
- Tests/EditMode/TruckTaxiWorldAuthoringCompletionTests.cs: composition reference/idempotence, fail-before-mutation missing anchor/duplicate map identity/empty art, non-finite road rejection, disconnected route/interior stream gap, endpoint mismatch, floating/buried support, blocked interaction approach and report-only invalid classification. Physics cases sync only their temporary fixture objects; these tests have not been executed by this workstream.
- This handoff. Parent Unity import owns new .meta files.

## Vendor report

- VENDOR ASSETS AUDITED: existing EasyRoads integration `Assets/LWS/InterstateHauler/Roads/EasyRoads/LwsEasyRoadsExportBoundary.cs` (public GetRoads/GetRoadObjects export boundary), existing Scene Streamer query/manifest integration in the world validator, existing NWH canonical prefab collider boundary. Spline Spawner asset/package discovery found no implementation path; availability is unresolved.
- VENDOR ASSETS USED: existing vendor-authored geometry and adapters only; LwsRoadGraph/LwsRoutePlanner semantics, unchanged Scene Streamer residency audit, canonical NWH reference. No new vendor runtime dependency.
- RELEVANT ASSETS NOT USED: Spline Spawner density authoring blocked pending actual installed API and pools; EasyRoads/Vista generation intentionally not invoked because regeneration is prohibited.
- CUSTOM SYSTEMS CREATED: project-specific editor composition and audit extensions only.
- VENDOR SOURCE MODIFIED: NO.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
