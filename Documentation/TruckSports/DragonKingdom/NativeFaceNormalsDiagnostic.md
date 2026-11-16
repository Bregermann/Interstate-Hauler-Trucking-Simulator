# Candidate-only native face-normal diagnostic

## Actual cause evidence

V368's same-shader neutral-lit entry image still shows heavy ribs/grid shading. Two disposable materials neutralized8authored diffuse maps across419renderer slots; original shader/geometry/lighting stayed in use. All receipt source/voxel/physics/height/binding restoration checks are true. Neutral PNG SHA256: `a9bd05f0bfd0899fba2799a4e13c444c5ec34e89b796b8913b99ea41d7be2c85`; restored PNG is bit-identical to baseline SHA `011eccd570437210c80167af744f41448df0870fbc064b203fb4cefcc8e592c2`. Diffuse/normal-map/mask variation is not necessary for the visible bands.

Read-only actual saved Cave07 native LOD0 data covers140source meshes; the visible entry ROI intersects38meshes/67,880triangles, with zero degenerate faces. At world(450,166.32200349,770), actual face normal is(0.000002246,-0.951818699,0.306661319), but interpolated stored normal is(0.000004406,-0.135175053,0.990821732):64.373degrees apart. Along X449..455 at Z770 the physical roof stays approximately166.322m. This establishes substantial stored-normal disagreement in a region with little corresponding height ripple; it does not prove all remaining joins or geometry are smooth.

Full actual source hashes, translation-only scene ancestry, positions/normals and ray/triangle cross-sections are recorded in `Logs/TruckSports/DragonKingdom/CavernRibbingAuditV368/actual-entry-mesh-sections.json` and the two CSV profiles. The numerical profiles are descriptive, with no acceptance threshold or mesh edit. Native sphere/custom-brush changes remain a separate coordinator investigation.

## Reused installed capability

Public `MarchingCubesPolygonizer(bool lowPolyStyle)` already implements geometric face normals when true. Its native job only replaces the three output corner normals with the normalized cross product; it uses the existing native positions, winding, vertex colors/UVs/splat payloads and polygonization. The public `APolygonizerProvider` is an installed future production extension point. The Editor diagnostic uses a plain candidate-scoped C# factory that validates its exact owner/tag/material identity and delegates directly to `new MarchingCubesPolygonizer(true)`. It is not a MonoBehaviour, cannot be attached to a master and is never serialized.

## Concrete hook

Coordinator-owned `CreateIsolatedRoundedCavernProbe` may explicitly call the following AFTER `InstallSharedCavernFinish`, while fresh/copy native voxel arrays remain loaded, and BEFORE saving:

```csharp
proof.nativeFaceNormals = DragonKingdomRoadSurfaceProbe
    .CaptureNativeFaceNormalDiagnostic(world, tag);
```

Suggested opt-in flag: `-tsDragonCavernNativeFaceNormalsDiagnostic`. This helper is not automatically called and does not change Main, existing excavation behavior or any production setting. The coordinator owns the receipt-field/flag integration.

The helper directly calls public native BuildMesh/CompleteBuildMesh on existing loaded chunks at their actual1/2/4LOD levels. It binds only temporary visual meshes, captures separate face-normal/restored entry images, then restores visual bindings and destroys temporary meshes/camera in try/finally. The factory is an ordinary managed object with no attached component. It never attaches a provider to the production master, calls saved-scene Init, changes collider meshes, persists Digger data, saves assets or rewrites original saved meshes.

## Strict qualification

Exact native vertex-layout bytes are compared as vertex and cyclic-oriented/submesh triangle multisets, excluding ONLY the intended12normal bytes per vertex. Positions, vertex counts, submesh/winding, color, UV and splat data remain byte-exact under allowed ordering permutations. The original paint normal-identity comparator remains unchanged and independently records its result. Face output must repeat the same normal bits on each triangle's three corners, be finite/unit and align with its actual geometric face. A1e-5 arithmetic bound is used solely for recomputing unit-vector length/dot across native and managed floating-point evaluation; it is not a coordinate, topology, UV, color, SDF or source tolerance.

All loaded voxel bytes, original collider bindings/state, physical/anchor hash, terrain heights/holes, material/renderer/LOD/light state, source/dependency bytes and voxel membership must survive. All temporary meshes/camera must be destroyed, and original native master/provider references must remain exact. Existing fresh-probe11946floor/roof/clearance/critical/source acceptance remains authoritative and cannot be waived by this diagnostic.

Thirteen small automated controls cover face-normal-only changes, triangle order, one-coordinate-bit changes, reversed winding, UV/color changes, nonunit/wrong face normals, exact candidate context and unconfigured-provider rejection. V370 actual controls passed12/13; Unity rejected attaching the initial Editor-assembly provider, and the fresh candidate failed before repolygonization while reading a long dependency path. The corrected diagnostic uses the plain factory and the existing RoadSurfaceProbe.IoPath authority for every dependency Exists/ReadAllBytes call; no dependency is exempted. Its unconfigured-use control still requires the same explicit validation failure and now also proves the factory is not a Component. Corrected Csc Editor/tests passed exit0(4.881s/0.988s). V371 actual roundtrip now repolygonized419meshes from161loaded chunks, with419intended normal changes. All native face-normal/geometry/non-normal attributes/full-voxel/physics/height/source/membership/restoration/cleanup checks are true; min face dot0.999999702 and max squared-unit error5.3644e-7. Direct review REJECTS this as a production finish: heavy ribs remain and become harsher angular stair facets, with the abrupt join unchanged. Native face PNG SHA45fc97c3fe625b604eea37c81cb79cefbf2384421609cd5c9c085fc29cbc1598; restored PNG SHAc9434271f979c123ee95919f56da313e716497daca922ce62879b0a48f02db7d. The final batch NUnit XML was not yet emitted at this review, so no whole-batch result is claimed. No production provider or baked face style is implemented; remaining physical surface/join work is separate.

VENDOR ASSETS AUDITED: Digger MarchingCubesPolygonizer, MarchingCubesJob, APolygonizerProvider, VoxelChunk, ChunkLODGroup, native VertexData/PolyOut and actual persisted cave sources.

VENDOR ASSETS USED: existing native face-normal mode, polygonizer jobs, original mesh/material/physics systems and public Unity inspection APIs.

RELEVANT ASSETS NOT USED: saved-source Init/rebuild, global master changes, custom normal reconstruction, replacement shaders and custom brushes would confound this diagnosis.

CUSTOM SYSTEMS CREATED: candidate-only factory adapter, exact intended-normal-change validator, bounded roundtrip capture and regression controls.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none; Digger still generates all meshes/normals.