# Inferno eye audit and explicit visible eyelids

2026-10-28. Inspected the actual V51 `action-Sleep-continuous-after-loop.png`: the guardian is now genuinely curled/folded on the ground, but its fully open painted eye still reads as alert rest. Functional rig/curve playback alone did not satisfy the sleeping expression.

## Actual owned source

The Inferno prefab has one SkinnedMeshRenderer, one original `Dragon Inferno.mat` and no source blend shapes. No eye/eyelid bone was found in its actual hierarchy. Its eyes are painted directly into the 2048×2048 `Textures/Dragon Inferno.psd` face atlas. The white-eye pixels occupy approximately UV (0.7461,0.6812) through (0.8330,0.7480); the actual dark outline was measured separately and expanded slightly for surface coverage. Both mirrored face sides use this atlas region. There is no separate existing eye mesh/material to close.

Source paths under `Assets/TruckDragonKingdom/PrivateLicensedDragons/Monsters Ultimate Pack 02 Cute Series/Dragon Inferno Cute Series/`:

- `Prefabs/Dragon Inferno.prefab`
- `FBX/Dragon Inferno.FBX`
- `Materials/Dragon Inferno.mat`
- `Textures/Dragon Inferno.psd`

Reviewed atlas SHA256: `e184e816f6a80056fe57935775457c4cfc68defd912a944c6470806092a8f4b6`.

The actual owned cached `Meshtint Studio/3D ModelsCharactersCreatures/Monsters Ultimate Pack 02 Cute Series.unitypackage` was inspected read-only through its pathname inventory. It contains 31 Inferno prefab/material/texture/model/motion paths, one original prefab/material/atlas set, and zero Blink/Eyelid/Sleep-named paths across the complete package. Existing Fire/Spark sources also supplied no suitable Inferno eyelid variant. No account download access or external asset ownership is inferred.

## Explicit asset configuration

`LWS.TruckDragonKingdom.DragonEyelidAuthoring.InstallInfernoEyelids()` upgrades the existing project-owned configured Animal prefab. The saved Actor contains a nested reference to that Animal and inherits the addition. No raw prefab, material, PSD, FBX, existing Sleep/Wake curve or original bone/channel is changed. No automatic Play/import/build generation occurs. `RegenerateInfernoEyelids()` is a separate explicit selected regeneration path that preserves existing derived mesh GUIDs. A public `InstallOnConfiguredInstance(contents,species)` hook exists for a later reviewed explicit factory integration; it has not been inserted into the mandatory mouth migration.

The missing visible art comprises paired upper/lower lids fitted to the actual eye UV surfaces, plus a narrow closed-eye crease per side. Native authoring reads the original UV stream through Unity's read-only mesh snapshot API and bakes the actual skin only into temporary working meshes. Barycentric surface sampling supplies real head-local positions/normals, mirrored side selection and the original face curvature. A temporary actual-head rotation verifies that the sampled surface is sufficiently rigid to that real head before it can be fitted; failure stops generation rather than placing substitute geometry.

The new meshes have genuine authored closure morph frames at 10/25/50/75/100 percent. These are real new project-owned geometry channels, never claimed to exist in the original rig. The original `RigHead` is the sole attachment bone. No fake `RigEye` bone, Animator blink parameter or fabricated original channel is introduced. Opening/closing follows the curved source surface through multiple morph samples. Small outward separation avoids z-fighting. Normal depth testing remains active; no overlay-camera trick or original eye erasure is used.

The new skin surfaces reuse the actual configured material and adjacent orange face-atlas palette; the crease reuses an actual dark outline texel. No replacement raster is generated or imported. No source texture, tint or material is mutated. Four small source-derived mesh assets remain private at `Assets/TruckDragonKingdom/PrivateLicensedDragons/DerivedEyelids/`.

The lead reports that V61 explicit native installation succeeded: 672 vertices/1040 triangles across the four meshes, maximum actual head-local rigidity error about 5.69e−7, original source bytes exact, and generated assets retained. The native author writes `EyelidAuthoringReceipt.json` with source/derived file hashes and measured counts; retention of that public receipt in ROOT is separately requested. Generation alone does **not** prove visible closed eyes.

`DragonEyelidPresentation` observes the real motor's mapped Sleep/Wake playback and normalized action time. Sleep closes the actual mesh lids; Wake reopens them while the body rises. Pause freezes morph weights; reset/deactivation restores the original open-eye appearance. Actual weighted registered sleep clips cover an outgoing sleep blend. The public `RegisterSleepingClip` supports an explicitly reviewed scene-specific sleeping-vigilance override without inventing a mode callback or changing source clips. The common prefab initially registers the actual catalog Sleep only. After the lead's V62 actual continuous-Sleep image review confirmed closed lids/folded rest, Heist authoring now registers only its derived actual curled-vigilance clip; ordinary LookAround remains unregistered. Both new installation and explicit refresh use the same path and record prefab-instance overrides for saving. Actual saved Heist vigilance playback remains unrun here.

## Native fixture and remaining verification

`DragonEyelidNativeTests.ActualSurfaceLidsCloseOverPaintedEyesDuringSleepFreezeOnPauseAndReopenWithWake` compiles but was **not run by this workstream**. It instantiates the saved actual configured Animal, uses ordinary Prepare/TryAction Sleep/Interrupt/Wake and Pause APIs, and does not call Animator.Play, sample animation, force states, edit poses/meshes or inject callbacks. It requires actual lid/crease morph weights to close, pause to preserve them and actual Wake to reopen. A calibrated head-attached subject-only camera retains awake/sleep/wake PNGs below `-tsDragonOutput/Eyelids/<run>`. Actual eye-white pixels must be visible awake, fall below 30% during Sleep and reappear during Wake; this tests real rendered occlusion rather than only morph existence.

Source/API compilation passes for the runtime, explicit authoring and native fixture. Actual native fixture results, rendered seam/skin-color fit, complete pupil/white occlusion from both sides, natural opening timing, pause/reset and Heist vigilance expression still require native execution and image review. No human/listening/device or normal-gameplay/performance acceptance is claimed.

V63's original dedicated fixture failure is retained: `awakewhitePixels=0`, before Sleep evaluation. Inspection of its actual `awake-open-eye.png` showed a clear open eye whose painted white lay in brow shadow, around gray 130, below the absolute bright-white classifier. The fixture's arbitrary world-facing sun did not illuminate the viewed surface. A source-only correction now places a white shadow-free key along the same real eye-view normal, with a small off-axis fill; both follow the calibrated awake/sleep/wake camera. The existing >300 awake pixels, <30% closed/awake ratio and reopened-pixel requirement, including the pixel classifier itself, are unchanged. The previous scene sun is restored during cleanup. This revised fixture compiles but has not been rerun by this workstream; no occlusion acceptance is inferred from a lighting repair.

- **VENDOR ASSETS AUDITED:** Actual Inferno mesh/rig/material/UV/PSD and cached Meshtint variants/motions; existing configured animator/motor playback APIs.
- **VENDOR ASSETS USED:** Actual head bone, original surface geometry/atlas/material and mapped Sleep/Wake playback; Unity skinned mesh/morph APIs.
- **RELEVANT ASSETS NOT USED:** No owned compatible lid/blink variant was found; source eye bones/morphs do not exist.
- **CUSTOM SYSTEMS CREATED:** Missing visible surface eyelids/crease geometry, explicit selected configuration and playback-derived visual coordination/test.
- **VENDOR SOURCE MODIFIED:** NO.
- **DUPLICATE VENDOR FUNCTIONALITY CREATED:** NO; no creature animation runtime, rig, motor, sensing or physics framework was replaced.
