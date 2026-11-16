# Saved cavern ribbing diagnosis

## Established native evidence

V366 saved CavernRoundedProbe07 rendering and optional LOD capture passed 33.273002 s with exact restored source/voxel/physics state. All three entry PNGs (baseline, native LOD0 preferred, restored) are 1,136,678 bytes and SHA256 `011eccd570437210c80167af744f41448df0870fbc064b203fb4cefcc8e592c2`. The diagnostic changed 161 native LOD groups; 140 had nonempty first-level meshes. LOD threshold changes alone did not change this captured view. This is not a claim about every possible viewpoint.

The new rock painting removed the previously observed green roof patches. Direct V366 entry inspection still shows heavy regular ribs/grid shading and abrupt vault/corridor joins. Cave07 therefore remains visually rejected. The earlier directional-shadow-disabled and normal-amplitude-zero pairs also left the ribs visible. Existing terrain/world acceptance remains independent and incomplete.

## Installed source audit

`ChunkLODGroup.cs` creates native 1/2/4 voxel levels, with saved transition heights 0.1/0.05/0 and collider LOD0. Source03 had 161 groups, 141 with size50 m and20 with size1 m. The V366 runtime inventory, rather than this earlier source count, is authoritative for nonempty Cave07 meshes.

`MeshLitPasses.hlsl::AdjustedNormalTriplanar` computes a face normal from ddx/ddy world position and blends it with interpolated mesh normals using `saturate(min(bary.x,bary.y,bary.z)*10)`. Those adjusted normals drive triplanar diffuse/normal/mask sampling. `_BaryContrast` is declared but unused in this URP implementation, so changing it is not a supported effective repair.

`MarchingCubesJob::ComputeNormalAt` takes forward SDF differences along each voxel lattice axis, interpolates those gradients to edge vertices, then normalizes them during triangle output. Vertex positions independently multiply by voxel scale. With current voxel scale 1.5625/0.5/1.5625, a measured comparison between stored normals and geometric face normals may be useful if neutral-lit ribs remain. This is a source-derived hypothesis, not a confirmed vendor defect or an instruction to modify vendor code.

## Smallest next discriminator

Use the same camera, source meshes, original Digger shader and lights. Temporarily assign disposable copies of only the actual Digger renderer materials. Give every diffuse layer the same constant gray texture/remap scale, zero normal amplitude, disable mask contribution/metallic/smoothness and height blending on those copies. Leave original shader, mesh normals, geometry, splat coordinates and lights intact.

This removes spatial texture/normal-map/mask/triplanar color variation while retaining geometric normal lighting. If regular bands remain on the neutral surface, geometric shape/vertex-normal shading is implicated; if they vanish, texture/triplanar or layer blending is implicated. This does not itself distinguish actual displaced ripples from biased vertex normals, and does not repair either.

Keep the original three views and put the neutral/restored pair in a separate two-frame field. Restore exact renderer/material references in try/finally and destroy only temporary copies. Require exact source materials, mesh bindings, lights, camera, source/dependency/voxel bytes and membership, heights and physical state after restoration. Record actual copy settings and original identities, and inspect the real PNGs before drawing a conclusion. No re-excavation or production material/LOD tuning is authorized by this diagnostic.

VENDOR ASSETS AUDITED: installed Digger ChunkLODGroup, MarchingCubesJob, MeshLitPasses.hlsl, mesh URP shaders and actual saved receipts.

VENDOR ASSETS USED: native Digger meshes/material shader and public Unity material/LOD APIs for controlled inspection.

RELEVANT ASSETS NOT USED: custom geometry, replacement renderer/shader, broad smoothing or vendor patches; these would confound the test.

CUSTOM SYSTEMS CREATED: bounded read-only diagnostic capture adapters and evidence.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
## Prepared candidate and actual V368 outcome

The optional `-tsDragonCavernNeutralSurfaceDiagnostic` code/test pair was prepared in disposable `Logs/TruckSports/DragonKingdom/CavernNeutralSurfaceDraft01` copies (Csc Editor exit0/6.468 s, tests exit0/1.006 s), then the coordinator promoted the exact two candidates into V368. Actual V368 neutral/restored images and a completed restoration receipt now exist: two materials, eight authored diffuse maps and419renderer slots were covered; source/physics/height/voxel/binding checks are true. Direct neutral-image review still shows the heavy ribs, so diffuse/triplanar texture and bump/mask variation are not necessary for the bands. Restored image SHA is the exact original011ecc...; neutral image SHA is a9bd05f0bfd0899fba2799a4e13c444c5ec34e89b796b8913b99ea41d7be2c85. Native face-normal follow-up and exact geometry/normal evidence are described in `NativeFaceNormalsDiagnostic.md`; no production repair is claimed.

One actual saved Cave07 LOD0 mesh (10,044 vertices /3,348 faces, source SHA `7f64f5fc99beeb9fa071aa1809b5868cab2751d77ce1b851d8abf5f2a330bdf1`) was parsed read-only: zero degenerate triangles and zero backward stored normals. Stored-normal vs face-normal angles had median0.537 degrees and p9010.616 degrees. A hypothetical inverse-voxel-scale adjustment improves the median but worsens p90; this is not sufficient evidence for a normal correction. Exact path/bytes/results and the limitation that smooth vertex normals legitimately differ from face normals are recorded in `CavernRibbingAuditV366/representative-mesh-normals.json`.