# Waterfront timber source authority and next integration gate

The read-only V350 prerequisite audit finds all four architecture layouts and
their 518 distinct source dependencies present. Content hashes, metadata hashes,
GUIDs and all 34 raw material/scene/original-scene-metadata snapshots match.
There is no missing required source in that architecture closure. The strict
native geometry witness remains V312's complete source receipt; no new full-world
acceptance is inferred from this file audit.

One concrete integration blocker existed outside that closure: the new waterfront
watcher unconditionally required
`PolygonKnights/Prefabs/Buildings/SM_Prop_Beam_01.prefab`, which does not exist in
the actual source cohort. The established `WorldAuthoring.Place` intentionally
resolves absent Knights prefab requests to their existing model under `Models`.
The actual `Models/SM_Prop_Beam_01.fbx` and metadata exist and match the original
source. Existing bearings and quay support already use that same authority.

The waterfront watcher now resolves the actual native source using precisely the
same prefab-first, Knights-model-fallback path rule, then protects the real source
and all its Assets dependency files/metadata. An absent source model or actual
dependency still fails. It creates no substitute prefab and waives no source,
collision, guardian, water, terrain or ferry gate.

The focused native regression calls the real existing `FitOwnedBeam` placement
with material conversion disabled in a disposable preview scene. It verifies that
the watcher resolution and actual instantiated native source agree, source meshes/
materials/collision exist, and all actual dependency bytes remain exact. This
proves the resolver path only; it does not prove waterfront bed or pile contacts.

## First concrete integration operation

After the actual native terrain quality gate passes, run one **fresh short-tag**
full integration candidate using the current existing native fixture:

`DragonKingdomNativeRoadReliefTests.ActualEasyRoadsReliefCandidatePreservesNativeSamplesAndPassesOriginalPhysicalWorldGates`

Required arguments:

```text
-tsDragonWorldCandidate NA01
-tsDragonVistaFoundation RoundedVista01
-tsDragonTerrainCoordination VendorGradedRelief01
-tsDragonFinishArt
-tsDragonFinishedArchitecture
-tsDragonArchitectureGeometry <absolute V312/rendered-evidence/authored-architecture-geometry.json>
-tsDragonPhysicalCrossings
-tsDragonOuterBand
-tsDragonOutput <fresh run evidence folder>
```

Use a different short tag if `NA01` exists. Retain the exact vendor grading/falloff
and cavern profile arguments that actually pass their independent native gates;
do not silently promote the currently failing terrain trial or unaccepted cave
profile. The existing strict geometry witness SHA is
`82703c8e90d15eb19bce03b570a767de8e30814d82a58fd781f9850c9bbedd51`.

This existing path performs the material preflight, adds the original finished
streetfront/windmill, inventories and seats all 25 retained buildings, disables
the exact 26 prototype components and composes all 678 source instances **before**
vendor grading and native coordination. The 525 source parcels and 25 retained
parcels must be supplied as 550 individual footings. Do not paste architecture
onto previously passed terrain and claim those contacts remain accepted.

The native fixture already checks the schema 4 branch and the 525 live collider
contact proofs, 25 retained buildings and physical waterfront. The older broad
`MeasuredRoundedVistaAndRealDigger...` fixture still explicitly expects schema 3
and historical flat-cave counts; it is not the correct first fixture for this
new schema 4 integration. Preserve those legacy tests against their intended
completed legacy candidate.

Current known blockers are the real terrain quality failure (V349: 21,681 steep65
cells, 6,606 steep80 cells; zero conflicting ground cores and exact seams), the
new full integration's unexecuted 550-footing and waterfront contacts, and later
saved identity/material/whole-frame validation and 18 accepted regional views.
No source dependency audit removes those physical or visual requirements.

VENDOR ASSETS AUDITED: exact four source layout closures and raw snapshots;
Synty PolygonKnights original beam FBX/materials and the existing prefab-or-model
authoring resolver, bearings/quay and native source metadata.

VENDOR ASSETS USED: existing original Synty beam model and its dependency closure,
existing `FitOwnedBeam`, accurate source mesh collision and native prefab APIs.

RELEVANT ASSETS NOT USED: no missing optional beam prefab is fabricated. The
genuinely unavailable optional full TARBO Town mesh remains excluded, not waived.

CUSTOM SYSTEMS CREATED: a narrow source-resolution watcher adapter and a focused
actual-source regression for the existing waterfront support path.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
