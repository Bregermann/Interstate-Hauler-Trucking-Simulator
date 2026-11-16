# Saved cavern Unity smooth-normal diagnostic

This is an optional, reversible saved-view diagnostic, not a production cave repair. V378 ran against the already saved `CavernRoundedProbe14`; no excavation or voxel initialization was required. Its native controls and restoration checks passed, but the rendered result remains visually rejected.

## Question and implementation

Earlier directional-shadow, normal-map amplitude, neutral diffuse and LOD pairs retained the visible ribs. Native hard-face normals also retained objectionable angular shading. Read-only mesh-section evidence records a nearly planar roof region with a large stored-normal versus geometric-normal difference. These findings justify testing smooth normals calculated from actual geometric faces; they do not prove that every visible seam has one cause.

`DragonKingdomRoadSurfaceProbe.WeldedNormals.cs` groups actual saved Digger MeshFilters by native LOD. Translation-only source frames map original vertices to world positions. A disposable proxy shares positions only when all three IEEE float components are exactly equal; there is no proximity threshold. The proxy preserves the complete oriented triangle sequence, including face multiplicity. Unity's `Mesh.RecalculateNormals()` is the sole normal-generation algorithm. Its output is mapped back onto disposable copies of the original-layout meshes.

The original shader, materials, UVs, colors, barycentrics, splat data, source positions, oriented faces, submeshes, mesh bounds and collision meshes remain unchanged. The exact native-layout comparer independently checks every non-normal attribute. The existing normal-aware painting equivalence guard remains intact; it is not relaxed to authorize this explicitly intended diagnostic normal change.

The helper captures the same entry camera with temporary smooth-normal copies, restores every original MeshFilter reference in `finally`, destroys the temporary copies/proxies, and captures the restored frame. The ordinary three frames remain separate from the additional two diagnostic frames. Renderer/material references, LODs, lights, transforms, camera and physical state are checked exactly. The enclosing saved-reader capture continues to guard source/dependency/voxel files, heights and scene membership. Unused or cancelling proxy vertices producing invalid normals are reported explicitly and rejected without substitution.

Unity documents that normal recalculation uses shared vertices, that imported seams may split those vertices, and that recalculation can convert vertex positions to Float32. Consequently the API operates only on the stripped disposable Float32 proxy, never on the original native vertex layout. [Unity 6000.4 Mesh.RecalculateNormals documentation](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Mesh.RecalculateNormals.html).

## Invocation and verification

Run the existing saved-render test with `-tsDragonCavernProbe CavernRoundedProbe14 -tsDragonCavernWeldedNormalDiagnostic`, together with `DragonKingdomWeldedNormalTests`. The five controls cover exact same-LOD sharing, translated chunks, independent LODs, one-ULP-separated positions and cancellation rejection. They use transient native-layout meshes and preserve their originals.

Final source compilation completed with exit 0 for Editor (4.834 s) and WorldAuthoringTests (0.973 s). Exact six-asset hashes and native filter are in `Logs/TruckSports/DragonKingdom/SavedWeldedNormalsReady01/ready-manifest.json`. Compilation establishes source readiness only. Native restoration, actual control outcomes, rendered appearance, save/reload, runtime and Windows acceptance must be recorded separately. No persistent output or production provider is authorized by this diagnostic.

V378 completed with Editor exit 0 (90.9 s process wall time) and all six native tests passing (41.620 s combined). The actual saved-view receipt records 421 changed mesh copies: 140 LOD0 meshes, 141 LOD1 and 140 LOD2. Unity calculated all three proxy groups; each preserved proxy positions/oriented indices and original-layout non-normal attributes exactly. There were zero unused proxy vertices. The baseline and original-restored PNG bytes are identical (SHA256 `f852ee5f66da1a9323abd5763c2e45342dae38c6aaa6f31b010870ad4c3fd58a`). All original references and physical/source state were restored and temporary objects destroyed. The original three frames and additional two comparison frames are retained under `Logs/TruckSports/DragonKingdom/V378/rendered-evidence/ActualRoadSurface`.

The coordinator and architecture agent directly inspected the actual smooth entry image. Heavy ribs/grid and abrupt joins remain. Therefore Unity exact-position smooth normals are **not an accepted production repair**. No persistent mesh-normal pipeline will be added from this result. This negative shading result redirects investigation to the actual sampled density field and marched geometry; full cave, runtime and Windows acceptance remain open.

## Vendor accounting

VENDOR ASSETS AUDITED: Installed Digger mesh generation, native gradient/hard-face modes and saved mesh layout; installed package/source inventory for Mesh Baker, MeshKit, DigitalOpus, ProBuilder, Polybrush and general normal/welding tools; Unity Mesh API documentation. No suitable installed dedicated general smooth-normal authoring tool was found in that inventory.

VENDOR ASSETS USED: Existing Digger meshes, source shader and material layout; Unity `Mesh.RecalculateNormals`, Mesh copy/binding APIs and existing project-owned exact geometry/restoration evidence.

RELEVANT ASSETS NOT USED: Digger gradient and hard-face output remain the recorded comparison baselines; neither provides this exact shared-position smooth-normal control. EasyRoads owns road authoring and is not a generic cave shading tool. No shader replacement, runtime voxel framework or vendor patch is required.

CUSTOM SYSTEMS CREATED: A bounded saved-view adapter, exact index correspondence, restoration evidence and five focused controls. Normal generation itself remains Unity-owned.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; the adapter supplies exact per-LOD sharing and evidence around Unity's existing normal calculation.
