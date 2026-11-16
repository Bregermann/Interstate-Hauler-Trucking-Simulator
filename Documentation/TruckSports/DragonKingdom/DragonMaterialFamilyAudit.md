# Actual column material audit and selected repair

All18actual V92candidate PNGs (nine overviews+nine eye views) and three direct V74comparisons were inspected with view_image. The candidate's repaired textured ground/shoulder mountains improve the old black surfaces and extremely stretched exterior pillars. It remains visually rejected: actual forest columns are white/flat; windmill sails float without a visible tower; refuge eye view is dominated by a blank facade; caverns' foreground pillars obscure its chamber; mountains eye view exposes a gap under the refuge rock row; mountain/volcano overviews frame the entire small world amid gray void. Sparse foliage, broad tiled ground, steep repeated plateau walls and patchwork-looking brown roads remain unfinished. Functional physical gates do not imply art acceptance.

## Proven white-column identity/cause

Actual hierarchy: `Shared Dragon Kingdom/Whisperwood/P_PROP_column_ancient_02`,12authored props, three LODs each. Source: `PrivateLicensedEnvironment/Fantastic Ancient Forest/Prefabs/Props/P_PROP_column_ancient_02.prefab`; source mesh `3d/SM_PROP_column_ancient_02.fbx`, GUID9e01c97d6ced6ef4e9fb3ea1053fa1e0. The binary FBX contains no LayerElementColor/Colors nodes; the actual shader does not consume a vertex-color stream. This is not lost vertex colors or foliage misidentified as pillars.

Source `Materials/M_PROP_column_ancient_02.mat` GUID6045b0d731b32e345a193bf7cff0b759 uses the installed `TidalFlask/Triplanar TopProject URP Advanced U6-1` shader GUID592ca707d361721498c831d1f0c3817b. It samples world-projected `_DetailTexture`, `_TopTexture`, `_AssetSpecificNormal`, `_DetailNormal`, PBR and top-normal fields. Its legacy `_BaseMap`/`_MainTex` are intentionally empty. Original detail texture is genuine light-gray stone, with a separate teal-green moss top texture; both source PNGs were actually inspected. It should not be arbitrarily darkened/recolored.

Candidate material slots use project variant GUID5edca6e6d3aaa6e4990c26b74b895e8c, `Materials/Environment/6045b0d731b32e345a193bf7cff0b759 URP.mat`: generic URP/Lit, white BaseColor, empty BaseMap/BumpMap. The old mapper omitted the real triplanar channels; the subsequent standard-albedo predicate incorrectly skipped it as intentional flat color. The installed vendor URP shader already provides the capability. No custom shader is justified.

## Selected source-only correction

Only approved `DragonKingdomWorldVisualMigration.ConfigureOwnedMaterial`, `HasAuthoredAlbedo` and their necessary family helpers change. Other methods are retained; the independent parent's FunctionalShoulders terrain-path allowance is unrelated and preserved. The predicate recognizes actual detail/top textures in the named triplanar family. Configuration requires the real supported UniversalPipeline source shader and persistent actual texture inputs, clones/copies all vendor properties/keywords/texture references/tiling/normal/PBR fields and flags into the separate project-owned target. It refuses unsupported shaders, a source-equals-target path and existing non-Material files. No source shader/material/texture/mesh edit, geometry/collider movement or generic white fallback.

Explicit stopped authoring sequence (parent-owned native execution):

1. `LWS.TruckDragonKingdom.Editor.DragonMaterialFamilyAuthoring.PrepareControlledProofMaterials()` snapshots the actual observed white project variant without overwriting history, creates source-faithful proof materials under `VisualCandidates/MaterialFamilyProof`, checks repeated material configuration byte-idempotence and hashes source dependencies before/after.
2. Native filter `DragonTriplanarMaterialNativeTests` (one actual controlled render test). It requires the prepared assets, renders the same real column/camera/light as original vendor, observed flat variant and configured vendor, checks every active shader property and actual detail/top/normal/PBR reference, and verifies no source vertex-color stream. Original/configured renders must closely match and actual old flat output must differ. Existing source tree/foliage retains its genuine alpha texture/cutoff/wind fields; a temporary unsaved cutout-disabled counterfactual must change actual coverage, and later actual vendor wind must change rendered pixels. Original file hashes remain exact. Six PNGs/observations are retained under `-tsDragonOutput/MaterialFamilyProof` for inspection.
3. Only after actual controlled rendering/review, invoke `DragonMaterialFamilyAuthoring.UpgradeAncientColumnMaterial()` to update the one existing project column material with its GUID retained. It repeats the operation to require byte-idempotence, proves original dependencies unchanged and does not regenerate/save a world or scene.

`DragonMaterialFamily.SourceCheck.json` records zero-diagnostic standalone source/native-fixture compilation. Authoring/render tests are **unrun by this agent**. A source-faithful material repair does not establish complete forest/world art acceptance.

## Related Heist mapper review

`HoardHeistAuthoring.OwnedUrpMaterial` already preserves original UniversalPipeline sources on new authoring, including this triplanar original. Its legacy fallback does omit `_BaseTexture` and blindly reuses stale shared variants, so the same class of loss is possible there. Proposed reuse is the shared owned public configuration method for genuine textured legacy inputs, retaining the original supported-vendor-URP and intentional-color paths. No Heist authoring change has been made in this scoped repair; review/coordination has been reported to parent. Road shader/depth-bias inspection and physical mesh changes are outside this column repair; existing road meshes/colliders are untouched.

VENDOR ASSETS AUDITED: actual V74/V92 renders, actual Ancient Forest column prefab/FBX/material/textures and TidalFlask URP shader, original foliage cutout/wind shader, world/Heist material adapters.

VENDOR ASSETS USED: original supported TidalFlask triplanar projection/detail/PBR/normal behavior and genuine texture inputs; existing leaf cutout/wind shader and actual source meshes.

RELEVANT ASSETS NOT USED: generic URP/Lit for a triplanar source; speculative vertex-color fixes; new custom shader or dark recolor; unrelated road physics changes.

CUSTOM SYSTEMS CREATED: narrow material-family preservation, explicit selected migration/idempotence receipts and controlled source-vs-variant native proof.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; the installed shader remains the surface authority.
