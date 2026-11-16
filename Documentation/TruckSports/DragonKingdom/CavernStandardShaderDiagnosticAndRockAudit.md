# Saved cavern shader discriminator and owned rock-module audit

This is a reversible saved-view diagnostic, not a production cave material or finished-environment claim. No shader, vendor source, voxel field, geometry, canonical asset or installed build is changed. The coordinator owns native execution after the shared Unity slot is available.

## Actual baseline and floor evidence

V385's repaired Cave17 passed full physical/source/art qualification (220.155 s), including all 11,946 floor and roof rays. Every measured floor hit was 105.00099945 m, retaining the original 105±0.3 m bound. Saved three-view rendering passed separately (42.509 s). The coordinator and architecture agent directly reviewed the actual entry image: regular grid/ribbing and abrupt joins remain. The floor correction is physically successful; the cave is not visually accepted.

The read-only saved-mesh audit covers 13,751 exactly flat LOD0 floor triangles / 41,253 corners in x440..605, z730..845. All share the same float world height 105.0009994506836 and upward geometric face normal. Stored-normal versus face-angle median, p90 and p99 are all zero degrees. Isolated boundary samples reach 70.77 degrees; that does not establish widespread normal error over the flat floor. Every native 128-byte vertex payload value is finite. The rock splat is constant layer3=1; color contains triangle barycentric coordinates while UV varies. Exact scene/mesh source hashes and payload ranges are retained in `CavernStandardLitReady01/actual-cave17-flat-floor-payload.json`.

The earlier neutral-material diagnostic retained the Digger shader. It removed texture/normal variation but could not exclude a shader-specific payload/lighting artifact. The new pair substitutes the actual installed `Universal Render Pipeline/Lit` shader with constant gray diffuse, opaque rendering and no maps, specular, reflections or emission. It changes only temporary renderer material references. This distinguishes the native Digger shader path from the same saved mesh/light/camera configuration; it does not authorize a bland replacement as final artwork.

## Reversible capture contract

Use `-tsDragonCavernStandardLitDiagnostic` with the existing saved `CavernRoundedProbe17` render fixture. Original three frames remain unchanged; the entry adds separate standard-URP and restored-Digger frames. Original material JSON/references, all renderer state/poses, raw mesh vertex/index payload (including normals, UVs, color, barycentrics and splat), layouts/submeshes/bounds, collider references/state, LODs, lights and camera are compared exactly. All temporary materials are destroyed in `finally`. The enclosing capture continues to verify source/dependency/voxel bytes and membership, physical state and heights.

Renderer property blocks must be proven absent, globally and per material slot. A nonempty block rejects the diagnostic before material replacement rather than silently retaining a potentially confounding property. Absence is checked again after restoration. Focused controls exercise the actual standard material, property-block absence/rejection, and raw-payload sensitivity to a normal-only mutation. Native execution and pair inspection remain pending; source compilation is recorded separately in `CavernStandardLitReady01/compile.json`.

## Actual owned art candidates

The installed ROOT and owned Plugins inventory scanned does not expose a complete natural cave module kit. It does provide substantial source-authored rock modules, with exact paths/hashes and prefab structure in `CavernStandardLitReady01/owned-cave-rock-modules.json`:

| Source | Actual available modules | Current state and appropriate use |
|---|---|---|
| Polyart HandpaintedEnvironmentVol1 | `Meshes/Prefab/Cliffs/RockFormation_01` through `_04` | All four imported prefabs are byte-exact to owned source. Each has three rendered LOD meshes, one LODGroup and three serialized mesh colliders. Their authored textures/materials and genuine irregular silhouettes are the strongest coherent foundation for selected chamber perimeter, entrance and ledge compositions. Existing eight floor rocks do not constitute that wall/ceiling composition. |
| Staggart Fantasy Adventure Environment | `Prefabs/Rocks/Cliff_A` through `_F`; `RockCluster_A` through `_D` | Source files exist in the owned collection, but these prefab modules are not imported into the current private Kingdom intake. Cliffs have three render meshes, one LODGroup and one collider each; clusters vary. They are alternatives requiring actual dependency closure, native dimensions/collision checks and material conversion/render review before use. No import or visual approval is inferred. |
| Synty PolygonKnights | `SM_Bld_Rockwall_Archway_01` and `SM_Bld_Rockwall_Straight_01` | Source prefabs exist; related source models are already used by Kingdom architecture, but the exact surveyed prefabs are not imported at those private paths. Useful for designed entrance masonry/ruin framing, not a substitute natural chamber shell. |
| Existing Tarbo crystal and owned lantern sources | Existing twelve crystals and twelve source-owned single-light lantern assemblies | Already qualified by shared finishing. Preserve their rigs/materials/source identities and use the same placement/light authority when composing additional art. More decorative objects alone will not repair the shader/grid cause. |

The next substantive art step is a deliberately authored set of coherent perimeter/entrance/ledge compositions using the imported Polyart modules, retaining actual source shapes, textures and LODs. Their scale, orientation, footing/wall support, source colliders and exposed overhangs must be measured in native geometry before placement. Every selected composition must preserve actual guardian-skin clearance, road portals, starts, gold anchors and the complete physical floor/roof checks. Embedded edges should conceal transitions naturally, rather than laying arbitrary props over the player corridor or calling sparse rocks a completed cave. No such composition or art acceptance is claimed by this inventory.

## Vendor accounting

VENDOR ASSETS AUDITED: Installed Digger native layout/material/shader path; installed URP Lit shader; actual owned Polyart, Staggart and Synty rock/architecture modules; existing Tarbo/lantern finishing authority.

VENDOR ASSETS USED: Digger's saved geometry/collision/persistence; actual standard Unity URP shader for the reversible comparison; Unity material, mesh read-only data and rendering APIs. Existing shared capture/restoration and source guards remain authoritative.

RELEVANT ASSETS NOT USED: Additional excavation/pressure trials are stopped. No replacement cave renderer, custom shader or persistent neutral production material is introduced. Alternative art modules are audited candidates, not silently imported substitutes.

CUSTOM SYSTEMS CREATED: A bounded standard-shader comparison adapter, four native controls and read-only source/payload inventory.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None.
