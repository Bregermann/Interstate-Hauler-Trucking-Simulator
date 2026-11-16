# Substantial authored architecture — source stage V1

This opt-in source intake prepares a **new candidate stage**. It has not replaced
the canonical world, changed World29, run Unity, or established visual/physical
acceptance. The four usable source compositions have been copied into ROOT with
their exact dependency closure and original metadata. Their native-validation
flags intentionally remain false.

| Composition | Original scene | Instances / unique prefabs | Authored content retained |
|---|---|---:|---|
| Ventuar Authored Fortified Quarter | `Ventuar3D/MedievalCastlePack/Scenes/Demo.unity` | 110 / 31 | Original wall angles, seven towers, gates, bridges, stairs, houses, church, forge and windmill assembly |
| Tarbo Authored Lumber Warehouse Yard | `Tarbo Studio/TARBO-FantasyVillage/Demo/LumberMill.unity` | 156 / 79 | Actual mill and warehouse buildings, 41 props, 54 timber instances and 59 ground instances |
| Fantastic Authored Walled Town | `Tidal Flask Studios/Fantastic Village Pack/scenes/demoscene_village_day.unity` | 382 / 40 | All 29 original building instances, 100 wall instances and 253 fence instances |
| Tarbo Authored Fishing Warehouse Yard | `Tarbo Studio/TARBO-FantasyVillage/Demo/Village.unity` | 30 / 14 | Complete original 29-instance Fishing parent plus the original nearby Cargo B building |

These are substantial architecture ensembles, not additional 89-prop gardens
around the rejected gray structures. New layouts live in
`Assets/TruckDragonKingdom/PrivateLicensedEnvironment/DerivedArchitectureLayoutsV1/`.
The four-layout closure contains 518 source assets (38,138,292 asset bytes), 26
immutable raw material snapshots and four complete immutable source-scene
snapshots. This is a closure count, not a claim that all 518 assets were new.

The importer preserves every selected instance's original local position,
rotation and scale, its complete original parent-transform chain and activation
flags, all serialized modification records and the verbatim PrefabInstance YAML.
It retains original prefab/scene/material SHA256s, GUIDs and metadata hashes.
It does not guess missing override targets or apply documentary NaNs. Native
source identity, override reconciliation, shader conversion, mesh bounds,
ground/water support and road/wing clearance still need actual Editor proof.

## Isolated invocation

```powershell
C:/Python313/python.exe -B -X utf8 Tools/TruckSports/ImportDragonKingdomEnvironment.py `
  --architecture-compositions-only `
  --architecture-composition fortified-quarter `
  --architecture-composition lumber-warehouse `
  --architecture-composition walled-town `
  --architecture-composition fishing-warehouse
```

The default is a dry run. Add `--apply` only for this explicit ROOT source intake.
The tool never starts Unity or writes CLEAN. Any different existing source,
layout or owned folder metadata is a conflict, not an overwrite. Normal builds
and the existing default importer path do not regenerate these layouts. Existing
Market/Finish layouts, their 143-field classifications and 51-material provenance
remain untouched; all 156 prior layout/provenance/snapshot files passed the actual
before/after SHA check.

The separate intake report is
`Documentation/TruckSports/DragonKingdom/ArchitectureAuthoredCompositionsIntake-fortified-quarter-lumber-warehouse-walled-town-fishing-warehouse.json`.

## One original source remains blocked

The optional full TARBO Town/Market selection contains 673 instances / 196
prefabs, including 63 buildings, 122 market instances, fencing, blacksmith,
farm, signs, well and authored grounds. Its exact source closure is **blocked**:

* Prefab: `Tarbo Studio/TARBO-FantasyVillage/Prefabs/Buildings_Sets/TB_Bd_SWHouse_TwoStory_C.prefab`.
* Child: `TB_Bd_SWHouse_Open`; MeshFilter local ID `33631215317921816`.
* Missing mesh GUID: `93f9786ca43c6244c9d219101ed1672d`, fileID `4300000`.

The GUID is absent from the full accessible owned Assets metadata tree. It is
not an optional material map. No mesh was substituted, omitted or waived.
Selecting all five compositions continues to reject this dependency. The other
four selections remain independent. The original failed all-five dry-run report
is retained as `ArchitectureAuthoredCompositionsIntake.json`.

## Explicit gray-castle replacement boundary for the next candidate

The canonical source currently has GUID `7cc05905626c7234289a4d2e3eaa9869`, SHA256
`820c9dc867faac9243579acce615b7209e5fd57fdf5b1f5e45974819c14e1744`.
The obsolete owned geometry scope proposed for replacement is precisely:

* Path: `Crownwatch Castle[0]/Fortified castle sanctuary[0]`.
* GameObject local ID: `7910969967957938101`.
* Transform local ID: `1276675040747331789`.
* Parent Transform local ID: `6570627952296715997` (`Crownwatch Castle`).
* It contains 17 directly nested old prefab instances: the oversized tower,
  wall and gate assembly. This is a proposed art replacement boundary, not
  permission to destroy referenced anchors or change every object in the region.

Replace the obsolete appearance with the actual 110-instance Ventuar composition
and its real palette/texture/materials in a **new** candidate. Preserve unrelated
objects, perches, objective anchors, references, controllers and NWH data. Before
any retirement is applied, native inspection must enumerate the exact affected
renderer/collider component IDs and record their old/new source provenance and
intended deltas; the source audit above does not invent inherited component IDs.

Existing authored roads are concrete constraints: village–castle and
castle–harbor are 22m wide; castle–pass and castle–caverns are 20m;
forest–castle is 18m; castle–fortress-inner is 22m. The current inner approach
runs from `(0,60,360)` to `(0,60,620)`. New gate openings must be measured from
actual imported mesh/collider geometry against these corridors and the existing
truck/dragon/start/objective buffers. No gate-opening width, complete mesh bounds
or feasible uniform composition scale has yet been measured; none is claimed.
Do not squeeze a 22m road through a guessed opening or leave the gray shell active
under the new art. Full physical, mounted-course and NavMesh acceptance follows
the explicitly scoped replacement. Cave retirements are not included here.

## Actual verification

* 13 source-tool unit tests passed: original TRS/parent/activation preservation,
  complete raw modification retention, exact scene recipe, independent subsets,
  no writes during extraction, and rejection of missing references, ambiguous
  roots, nonfinite active transforms and incomplete parent chains.
* Four-composition dry run: no unresolved GUIDs/includes or conflicts.
* ROOT-only apply and repeat apply succeeded with exact post-copy bytes.
* All 156 existing layout/material-provenance files remained byte-exact.
* Unity/native composition, rendering, materials, placement and gameplay tests:
  not run in this source stage.

## Native inspection source ready; execution pending

`DragonKingdomRegionalWorldIntegration.CaptureArchitectureMaterialPreflight()`
prepares the existing exact import authorities and inspects the complete
518-source closure. Of 26 raw materials, 22 use the established 19/51/Quay
authorities. Four original Standard materials are new to this stage: TARBO
`Village_Base` / `Village_ColorA` and Ventuar `CastleMat` / `Light`. The adapter
uses Unity's public `StandardUpgrader` on **temporary copies**, checks nine
texture/UV mappings, color/emission, metallic/smoothness and shader support, and
destroys the copies. It creates no persistent final materials or visual approval.
Unknown source or metadata changes remain rejections.

The new isolated `DragonKingdomArchitectureGeometryProbe.cs` exposes
`CaptureAuthoredArchitectureGeometry(Action prepareNativeImports = null)` and
`CaptureAuthoredArchitectureGeometryFromCommandLine()`. The command-line wrapper
calls the shared material preflight before freezing geometry input bytes. Its
temporary preview scene reconstructs all 678 authored parent/instance chains.
It enumerates every one of the 9,637 raw modification targets before any
disposable override work, reuses existing native source-identity pairing and the
finite TRS/activation/material adapter, and reports all unresolved targets.
Whole-root TRS already assigned from the pinned source is verified through native
readback rather than redundantly resolving/writing thousands of scalar fields.
An unresolved original target is never invented or silently bypassed; affected
geometry stays explicitly provisional.

The probe measures true primary-LOD vertex envelopes, source mesh/collider
identities and exact parent mapping. Gate cross-sections use disposable colliders
on the actual primary meshes, both ray directions and separate original authored
collider queries. Widths include the original authored scale. Seven recorded
height planes and horizontal-only 22m scaling estimates do **not** establish
truck-height clearance, usable road access or placement feasibility. It also
records actual renderer/collider component IDs, source chains, JSON hashes,
primary bounds and scoped incoming references for the canonical and World29 gray
sanctuary boundaries; no deletion or retirement is approved by observation.

Durable output: `<tsDragonOutput>/authored-architecture-geometry.json`, including
partial/rejected results and source/context preservation checks. Native filter:

`DragonKingdomArchitectureGeometryProbeTests.ActualFourAuthoredEnsemblesEnumerateAll678InstancesAnd9637OriginalFieldsWithoutWorldOrSourceWrites`

Editor and test assemblies compile successfully. This native case has **not**
been executed in this source stage. Its acceptance is complete inspection and
preservation, not resolution of every source issue or finished world art.

VENDOR ASSETS AUDITED: actual Ventuar Castle Demo, TARBO Village/LumberMill,
and Fantastic Village day scenes and selected dependency closures.

VENDOR ASSETS USED: exact original prefab/model/material/texture/metadata bytes,
authored scene poses, hierarchy and serialized modifications.

RELEVANT ASSETS NOT USED: TARBO's broken full-town source; demo cameras,
lighting/weather managers and unselected vegetation; no runtime demo scene load.

CUSTOM SYSTEMS CREATED: one bounded composition extractor and opt-in branch in
the existing dependency-closed source importer. No new runtime physics, weather,
rendering, navigation or generic prefab-override framework.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none; intake/provenance coordination only.
