# Saved architecture validation adapter

This extends the existing `DragonKingdomRegionalWorldIntegration` receipt and
saved-world validation responsibilities. It does not create another adoption
authority or enable schema 4 in `DragonKingdomWorldAdoptionPlan`/Apply.

The four new source/meta paths are the `SavedFinishedArchitecture` Editor partial
and `DragonKingdomSavedFinishedArchitectureTests` in the existing World tests.
The agreed main receipt fields are `finishedArchitecture` and
`retainedNativeBuildings`; their integration remains owned by the lead/world agent.

## Live terrain support

`ValidateFinishedArchitectureTerrainContacts(world, composition)` requires a
stopped, nonpersistent candidate scene with the actual coordinated fields.
It checks nine positions on every one of the 525 source-module footprints;
stations on field seams must pass on each participating field. The expected
terrain surface is the existing coordinator's `surfaceY - 0.08`, with maximum
0.25 m deviation. Each station also needs the matching enabled TerrainCollider,
the same TerrainData and an actual collider ray within 0.03 m of interpolation.

The returned `FinishedArchitectureTerrainSupport` includes parcel/sample counts,
maximum error, native-ray distinction and each field's actual asset GUID, SHA,
metadata SHA and measured points. It changes neither the world nor the receipt.
The caller must save the four owned candidate TerrainData assets before this
measurement so the witness hashes describe the final coordinated fields. Set
`terrainSupportVerified` only after this actual native proof passes. Never infer
it from assigning coordinator targets.

## Saved binding verification

`ValidateSavedFinishedArchitecture(world, composition, retained, nativeRelief)`
requires an actual reloaded persistent candidate. It checks:

- All four original source frames and 678 uniquely matched prefab instances,
  original parent chains, poses, active states, source GUIDs/hashes, primary mesh
  envelopes and original/added collider bindings.
- Exactly one pinned owned ladder correction: the original prefab's stale null
  reference remains unchanged; the saved owned instance uses the same FBX's native
  local ID 4300002 with 216 vertices, 324 indices and matching collision.
- Four distinct, persistent public-upgraded materials, exact source/target asset
  hashes and actual supported shader/color/texture/local-ID/UV properties.
- Each of the 525 source footings belongs to its actual measured source module;
  all 550 source-plus-retained parcels exactly match the native relief receipt.
- The actual retained inventory is 25 buildings: 13 village, 8 harbor, 4 refuge,
  with matching sources, geometry, poses, structural bases and final contacts.
- Waterfront physical evidence has passed for the intact 30-instance source
  group, both hulls, three structures, original pad/gangway, timber supports and
  53 ferry queries. Saved timber geometry and hull bed clearances are rechecked.

Saved heightfield/collider-binding checks reuse the live contact measurement
core, but explicitly record `nativeColliderRaysPerformed=false`. Inspecting a
persistent prefab is not a new live physics test. The validator does not mutate
the proof to assert completion and does not grant visual or driving acceptance.

`ValidateFinishedArchitectureSavePrerequisites` provides the same strict receipt
contract for the future existing-reader branch. Disposable materials, the
10-house canonical preview, missing source counts, incomplete waterfront and
uncoordinated footings reject rather than upgrading a source-stage pass.

## Tests and remaining integration

Targeted tests cover missing receipt rejection and the **actual V327 source
receipt** rejecting saved-world authorization because its four material copies
were disposable. They also mutate/restore actual count fields and verify the
original receipt bytes and object are unchanged. Use:

`-tsDragonFinishedArchitectureReceipt <actual V327 source-stage JSON>`

Compilation and actual Unity test execution are separate. Exact current compiler
results and file hashes are in
`Logs/TruckSports/DragonKingdom/SavedArchitectureValidationReady01/status.json`.
Actual full saved-world positive validation is unavailable until the candidate
terrain/waterfront support passes and its persistent materials/world are saved.
The schema 4 reader/application retirement mapping, whole-frame persistence,
full world gates and 18 fresh visually accepted regional images remain required.

VENDOR ASSETS AUDITED: existing exact source architecture, public Unity prefab,
mesh/material asset identity, TerrainData and TerrainCollider APIs; existing
world adoption, architecture and harbor receipt/validation adapters.

VENDOR ASSETS USED: those existing APIs, commercial source models/materials and
the public-upgrader provenance already recorded by the existing architecture tool.

RELEVANT ASSETS NOT USED: Pixel Crushers save orchestration is unrelated to this
Editor-only saved-prefab acceptance check. No runtime save system is replaced.

CUSTOM SYSTEMS CREATED: one narrow read-only saved-architecture validation partial
and shared final individual terrain-contact measurement.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
