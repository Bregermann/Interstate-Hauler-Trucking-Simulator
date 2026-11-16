# Native face provider: production feasibility only

Status: source/API audit and qualification plan. No production provider has been created or installed. The existing Editor diagnostic is a plain candidate-scoped factory with no attachable component; it never serializes. A production style decision requires actual native pair acceptance first; all whole-world, gameplay and build gates remain separate.

V371 outcome: the actual native face-normal pair passed its exact roundtrip restoration evidence but was directly rejected as a production finish because it accentuates the ribs into angular stair facets. This audit remains a conditional feasibility record; neither runtime-provider implementation nor baked face-mode adoption is justified by that result.

## Supported vendor route

`DiggerSystemEditor.IsBlockMaterial` returns `provider.GetMaterials() != null`. Therefore a future runtime-safe owned `APolygonizerProvider` can return null from GetMaterials and let the installed normal URP material/palette setup continue independently for each Digger terrain. Returning the diagnostic provider's one-owner material array from a shared production master would be wrong for this purpose.

`DiggerSystem.Reload` obtains `master.GetComponent<APolygonizerProvider>()`, calls Init and uses that reference through `VoxelChunk.GetPolygonizer`. The latter caches polygonizers per LOD. The provider must therefore be serialized on the owned master before its first initialization/reload; adding it after cached default polygonizers exist does not retroactively replace them. Use a fresh qualified candidate/reload through existing public lifecycle, not reflection or private-cache mutation.

The runtime adapter would be very small: Init has no editor dependencies, NewPolygonizer returns the existing `new MarchingCubesPolygonizer(true)`, and GetMaterials returns null. It should live outside Editor under the normal owned runtime source hierarchy, with its own class/file/GUID, and be explicitly installed only on the accepted world master. `Digger.Core` is runtime-capable, autoReferenced and already owns Mathematics/Burst/Collections references. No new geometry, normal reconstruction, shader, material or physics implementation is needed.

The current `DragonKingdomNativeFacePolygonizerProvider` name is retained for asset continuity, but after V370 it is a plain Editor-side factory, not APolygonizerProvider/MonoBehaviour. V370 proved that an Editor-assembly behaviour cannot be attached. The factory validates one candidate owner/tag/material identity and calls the public native polygonizer directly; it does not participate in master lifecycle/material detection and cannot serialize. A future runtime provider remains a separate class/decision, never a relocation of this diagnostic.

## Save/reload and runtime qualification plan

1. Establish native face-pair image acceptance and exact intended-normal-change qualification, including positions/winding/submesh, all other vertex bytes, source fields, physical geometry and original materials. A nice image alone cannot replace those gates.
2. Create a fresh owned candidate using the same accepted excavation recipe/grid. Install exactly one runtime provider on its native DiggerMaster BEFORE DiggerSystemEditor.Init(false). Assert GetMaterials is null and the ordinary URP path remains selected. Keep original per-tile Materials arrays, terrain material templates, layer/texture identities and normalized alpha weights.
3. Generate and persist all nonempty1/2/4LOD native meshes with the provider. Merely attaching a provider to old saved meshes will not repair their old normals until they are rebuilt, and would allow mixed shading later. Require the same exact geometry/non-normal attribute proof and all original source/11946floor/roof/critical guards before accepting these owned assets.
4. Save the owned scene/prefab and reload it in a stopped isolated Editor. Verify the runtime class is the only provider on the master, with zero missing scripts and no diagnostic Editor component, temporary Mesh references or temporary provider objects. Verify saved face-normal mesh bytes/bindings, all source identities/materials and physics/terrain state.
5. In a disposable retained copy, exercise actual public Digger initialization and native rebuild after reload. Require native provider lookup from its own master, correct per-LOD output, unchanged SDF/non-normal geometry/materials and no stale default-polygonizer mix. Do not call destructive reload/rebuild on the original saved source.
6. Verify the Windows player through the established build pipeline. If existing runtime voxel editing is present, DiggerMasterRuntime.Awake invokes PreInit and Init (Minimal in Editor, Minimal_and_LoadVoxels in player), so the serialized runtime provider must already exist. Exercise a controlled owned-copy native rebuild/new chunk and verify its face normals and materials. Check rematch/scene unload/reload for duplicate masters/providers, stale references and leaks. Never claim this from compilation alone.
7. Distinguish static baked cave presentation from runtime voxel editing. Installed PreprocessSceneBuild includes voxel data only when it finds DiggerMasterRuntime; adding a face-normal provider does not itself request or enable voxel editing. Do not add a runtime editing feature merely to alter static shading. Where the mode is intentionally static, verify the baked saved meshes/holes and runtime script references render correctly without claiming voxel persistence.
8. Keep existing native build-preprocessor ownership: it manages StreamingAssets/DiggerData. Record actual inclusion decisions and source/persistence paths through the established pipeline, rather than hand-copying or adding a second persistence authority. No build or persistence cleanup has been executed for this audit.

## Limits and expected cost

The native true branch changes only output normals; it is a faceted visual choice that still needs actual review. It cannot make real displaced ribs, abrupt intersecting volumes, bad silhouettes or insufficient roof/floor clearance disappear. Existing geometric defects remain separate requirements. The public provider retains the existing native per-chunk/per-LOD cache and buffer lifecycle, with no duplicate visual/collision framework; actual performance remains to be measured.

GetMaterials null is supported by the installed Editor detection branch, not an inferred no-op. Existing single-master world isolation remains important because Editor material detection uses FindFirstObjectByType globally, while runtime provider lookup uses each system's master. Validate the actual accepted world context; do not alter unrelated vendor/block terrain scenes.

VENDOR ASSETS AUDITED: APolygonizerProvider, MarchingCubesPolygonizer/Job, DiggerSystem Reload, VoxelChunk polygonizer cache, DiggerSystemEditor material selection, DiggerMasterRuntime Awake/Init, native build preprocessors and runtime asmdefs.

VENDOR ASSETS USED: prospective runtime route uses existing public provider and native face mode; no production change executed.

RELEVANT ASSETS NOT USED: Editor-only diagnostic components, custom normals, per-tile material replacement, private-cache reflection and manual voxel persistence are unsuitable for production integration.

CUSTOM SYSTEMS CREATED: audit/qualification plan only; no production runtime code.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none.