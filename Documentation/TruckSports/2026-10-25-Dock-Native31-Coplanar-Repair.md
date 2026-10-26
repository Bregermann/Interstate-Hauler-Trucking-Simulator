# Dock Native31 vendor-ON face ownership and explicit known-hotdog repair

Status: source frozen; native source regression and reviewed root-run asset repair pending. No Unity, scene save, private asset generation/regeneration, CLEAN writes or Git actions by this source owner. The failed Native31 assets/meta remain byte-exact.

Native evidence: `carnival-thirtyfirst-dock-geometry-native.xml` passed9/10; only hotdog's original material area failed (expected1.3724737069751427, generated1.3843388816264905). Read-only decoding of actual saved Mesh subassets identified renderer18 `door rt`, source material2, original GR3D FBX mesh4300014. The two exterior submesh0 halves contain the SAME two complete triangles, combined duplicate area0.011865140887591481, matching the native excess0.011865174651347843 within3.4e-8. They are not cap faces: both separate cap submeshes have matching area0.064513174.

Direct binary FBX source proof: Geometry713367782 `door rt`, polygon209, material2, vertices215/219/250/246. Applying original importer globalScale0.8 and centimetre/X-axis conversion matches all four duplicated output vertices within2.9e-8. The quad lies approximately1.43e-5 original vehicle units below Y0.65. Installed `Assets/CoreCollapse/EzySlice/Framework/Plane.cs` uses `Intersector.Epsilon=0.0001`; `Slicer.cs` assigns whole ON faces to its upper hull. Two independent opposite-normal cuts therefore keep this same near-plane source quad twice.

Approved narrow source correction: `DockSplitCoplanarOwnership.FilterOtherHalf` projects the explicit canonical plane into EXACTLY the same mesh-local frame as the existing frozen ClipOwnedWorkingMesh wrapper, uses the installed Plane.SideOf for all three source vertices, and filters only complete triangles classified vendor-ON. Signed centroid gives one half ownership; exact-plane ties belong only to the ORIGINAL upper half. It changes owned working triangle indices only: no source positions, normals, UVs, colors or materials change. EzySlice and the existing robust wrapper still generate every actual cut/cap. Non-ON source faces and cap logic remain unchanged. Production author passes original-upper explicitly; the existing five-argument single-clip API remains available.

First root native filter: `DockCoplanarOwnershipTests` (two cases). The epsilon-band fixture proves actual installed vendor-ON classification, exact-on upper tie ownership, source area once and exact retained UV0/UV1/color/source data. The actual current hotdog right-door material2 fixture cuts the original source, retains the existing0.5% source-area bound, detects the exact two below-plane vendor-ON source faces and compares unchanged cap areas against the retained failed artifact. It does not mutate the retained recipe. Existing `DockSplitRecipeTests` retains its original0.5% bound and should continue failing the old saved hotdog until the separate reviewed repair is actually run.

Root-reviewable explicit repair entry (NEVER automatic/import/start/install):

`LWS.TruckDockDiving.Editor.DockHotdogKnownRecipeRepair.RepairOnlyReviewedNative31Hotdog()`

It accepts ONLY:

- Generated asset SHA256 `E3941DA51F947306FBB9D4F58288F71F32991ACEB2D57033A845793DAD7CE7A9`.
- Meta SHA256 `54C5ADD2378E445BABCC5E4E165BC9C6805AF045DA152D56219B3F3010B64394`.
- Original GUID `aa59d84bd7192fd43b29148b5bb6e41e`.
- Exact current source dependency/plane/fit and stopped clean scenes.

The repair preflight refuses ANY external asset dependency on this recipe path, a stronger restriction than only refusing external old-mesh references. This is deliberately limited to Native31's pre-install artifact: Dock owner installation had not happened. Custom/changed/already-repaired artifacts refuse. It builds only the corrected hotdog replacement in memory and validates every original material area with the same0.5% bound BEFORE touching files.

Before mutation it copies and verifies FULL asset+meta bytes to `<actual Unity project>/Logs/TruckSports/PlaytestRepair/DockHotdogRecipeRepair/<UTC>/`. These backups contain PRIVATE derived licensed geometry and must remain private. It adds new owned embedded meshes to the SAME main asset, uses CopySerialized in place, verifies nonzero part references, persistent GUID/local IDs and every native Mesh body on disk, then removes old unreferenced owned subassets. It preserves the original main GUID and exact meta bytes, checks all other SIX recipe and meta hashes unchanged, reimports/revalidates, and writes a hash receipt. Any post-mutation failure restores/imports the exact verified old asset+meta backup. File operations use the actual Unity project path, independently of process working directory. No scene save, original prefab/vendor/shared recipe edits or unrelated asset saves occur.

Root sequence: review this entry and run the two source regressions; explicitly run the hash-gated repair; rerun all `DockSplitRecipeTests`; only after geometry passes run the existing Dock owner installer/native physical tests. No acceptance is inferred from this source handoff.

Static diagnostics:24source files,534cached references,0errors; no emission or Unity invocation. Original frozen22-file handoff remains historical; this separate delta replaces ONLY SportsTruckAppearanceDockAuthoring.cs and adds the narrow helper/tests/repair sources. Dock runtime/camera/physics/recorder sources are unchanged by this repair.

VENDOR ASSETS AUDITED: installed EzySlice Plane/Intersector/Slicer and current GR3D FBX polygon; original source importer transform, actual generated private Dock Mesh subassets and frozen robust clip wrapper.

VENDOR ASSETS USED: installed exact SideOf classifier, cutter/cap generation, original FBX/UV/material data and existing robust mesh validation.

RELEVANT ASSETS NOT USED: no alternate cutter or tolerance widening. Existing X/Z recipes remain untouched.

CUSTOM SYSTEMS CREATED: missing pair ownership for full vendor-ON source faces; two focused regressions; explicit single-known-artifact transactional repair.

VENDOR SOURCE MODIFIED: none.

DUPLICATE VENDOR FUNCTIONALITY CREATED: none; vendor remains cutter/classifier.
