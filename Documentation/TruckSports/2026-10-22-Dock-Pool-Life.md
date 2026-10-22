# Dock Diving — finished approach, calm pool and lively spectators

Implements the October22 follow-up recorded in Requests/2026-10-22-Dock-Pool-Crowd-Followup.md: HPR-18-2/4 reopened from developer feedback and new HPR-18-6. The prior automated acceptance did not override that feedback.

## Changes and causes

- Finished the right side of the approach with a solid service platform, owned Construction Props timber, MegaCity safety fencing and a closed near-end ceramic pool wall. All new collision geometry stays outside the original launch/water corridor. Windows review caught the solid platform top hiding the timber; its visible mesh now omits that duplicate top while retaining its solid collider.
- Removed the Dock-only FishingWaterFlow adapter. River Modeler normals now oscillate gently around a fixed origin on a bounded6369-vertex grid, with subtle physical-looking swells. Local URP source inspection established that normal sampling uses the BaseMap UV transform; correcting that transform also removes the stretched river-like streaking. Fishing's flow remains unchanged.
- The existing SportsWaterInteraction still owns real body crossing detection, history, foam rings and water audio. A small optional recorded-entry observer additionally supplies actual point velocity and mass. Dock uses incoming normal energy to drive a capped radial crown, varied gravity-driven droplets, thin mist and four spreading wave envelopes. Its normalized12-unit Rigidbody requires a600-unit reference energy; the initial90,000-unit reference incorrectly held ordinary impacts at minimum strength. No vehicle mass or forces were changed.
- A textured, briefly coherent325-vertex water film breaks apart into the spray; no full fluid simulation is claimed. Own effects follow the authoritative replay clock/speed and clear on rewind/reset. The original splash simulation and shared spray pool remain, but their nine old renderers are hidden in Dock: repeated identical-position/velocity particles produced the distracting opaque white orb. The existing camera now holds a valid splash's actual impact point briefly, rather than chasing the truck underwater and cropping the crest. Approach, flight and manual replay cameras remain unchanged.
- All464 spectators and eight original clothed models remain. Thirty-three distinct compatible UTS clips across three generic Bip01 rig families supply idle/look/talk/listen/dance/clap/cheer/walk motions. Twenty-four nearby rigs add staggered performances, bounded aisle steps, a small laughing body gesture and two-handed HQ phone poses. Distant spectators share sixteen animated destination meshes plus eight reusable source buffers. The update path does not create meshes, materials or people. Nearby rigs keep a fresh base pose offscreen; compatible source indexing is preserved across all materials.
- The close-up exposed legacy TextMesh distance lettering drawing over spectators. Its54 renderers are retained disabled and replaced by depth-tested world TMP labels using the existing Heat Roboto Condensed Bold SDF font. The rendered native test observed1121 dark glyph pixels when visible and zero behind an opaque occluder. This also makes the pool measurements readable on the pale surround.

## Actual validation

Final native EditMode2/0 and PlayMode4/0: saved references, repeated-authoring byte preservation, actual far-mesh deformation/submesh indexing, valid phone/hand placement, bounded walking, pause, real NWH water entry/positive incoming energy, growing crest, rematch and actual font occlusion. The observed crest exceeded six metres. A prior single-frame180ms height assertion failed; telemetry repeats did not confirm a restart defect. The final stronger test measures the actual rise over600ms and requires a crest above two metres. The earlier failure remains in the evidence.

Exact-payload Windows45/0:29 mixed-source checks and16 original eight-AI checks. These run complete actual three-attempt matches, four software human sources plus four AI, separate eight-AI play, correct first-contact distance/one score lock, bubbles, pause/reconnect, positional audio/mute state, replay, results, rematch and menu return. Agent inspected actual720p/1080p/ultrawide/4:3 venue images, natural photo behavior and actual water-impact frames. Controlled venue/photo views are explicitly labelled; no scoring outcome is injected.

Early28/0 and29/0 candidates were held back after visual review. Their hidden timber, smooth glass-like sheet, stacked orb and through-body lettering informed the corrections; receipts/images and exact source archives remain locally. Passing assertions alone did not cause promotion.

One later mixed-source run ended paused after19attempts and failed its completion watchdog. Its paused-state image and29/1 receipt are preserved. Normal production focus/device policy intentionally pauses human matches; that is consistent with the interruption, although the particular incoming event was not instrumented. The unchanged candidate subsequently completed the repeated mixed run. No production pause behavior was disabled and no forced score/result was used.

All17569 of17570 original scene documents remain; the single removed document is the superseded Dock river-scroll adapter. Every original Rigidbody/collider/physical transform and particle simulation document is unchanged. Eight decorative finger quaternion values changed only below1e-7 during Unity serialization. The only DockMatch source change is the read-only splash-camera branch; shared water changes are restricted to optional recorded mass/velocity observation. All925 unrelated pending files retain their baseline hashes.

No developer playtest, physical controllers/wheels, listening, isolated CPU/GPU profiling or broad severe-weather acceptance was performed. Body laughter is a visual gesture, not a new facial/lip-sync or recorded-laughter system. This turn-based sport retains its existing one-active-diver presentation; no simultaneous split-screen mode is claimed.

## Customization and delivery

Open Assets/TruckDockDiving/Scenes/TruckDockDiving.unity. On the match object, edit DockPoolPresentation ambientHeight, impactScale and referenceImpactEnergy; edit the three pooled spray systems and the Gravity-driven displaced water crown material for appearance. The right-hand approach platform is a normal editable hierarchy.

DockAudiencePresentation.variants contains each model's compatible ambientClips. Nearby rigs use DockSpectatorLife and Presentation/Lively audience0–7.controller; edit their states/cadence while retaining matching generic bone bindings. The owned phone model is imported locally by Tools/TruckSports/ImportDockPhoneAsset.py; its original installation and raw licensed source remain unchanged/private. ImportDockWaterVfx.py reproduces the optional asset audit; those flat GAPH rainfall examples are not required by this runtime effect.

Truck Sports → Dock Diving → Upgrade calm pool and lively spectators is an explicit versioned installation tool. Repeating it preserves the saved version4 scene; normal builds do not regenerate choices.

Combined Windows build: Builds/TruckSports/TruckSports.exe,60scenes/546 verified files; full payload `c6fcbca8381b44bbd3e89b3d2333b505c3feffd872f3100273addad49f845e33`, managed `07597b0ba207d2afd74a896c68fa2bc9b4c146f864cd3a8af96b57cdd29e6c45`,256 exact archived scoped source inputs. Previous accepted Dance-Off build remains recoverable. Other active HPR/GBC requirements remain open.

VENDOR ASSETS AUDITED: UTS Full Pack2.0 character controllers/three compatible generic rig families; owned humanoid-package inventory; HQ Residential House phone; NWH water/vehicle foundation; River Modeler2024 normals/splash art/VFX examples; four GAPH water-splash examples; existing FieldGoalCrowd, Heat/TMP/DOTween and Cinemachine adapters.

VENDOR ASSETS USED: Assets/UTS_FullPack/Models/People/Animations and original eight prefabs; ConstructionPropsBundle/URP/Prefabs/SM_Planks01 Variant.prefab; JC_LP_MegaCity/Prefabs/FloorProps/SM_FloorProps_Fence_01.prefab; owned HQ phone mesh; RiverModeler/Textures/WaveNormal.png and Splash_AlbedoAlpha.png; existing NWH physics, FieldGoalCrowd reactions, DOTween, Heat RobotoCondensed-Bold SDF, stock TMP/URP particle shaders and original Cinemachine rig.

RELEVANT ASSETS NOT USED: GAPH examples provide flat rainfall rings, not a cannonball displacement crown. River cascade/flow effects are unsuitable for a calm closed pool. Generic Bip01 characters are not blindly retargeted with incompatible humanoid clips. No replacement traffic, navigation, audio, vehicle or weather framework is needed.

CUSTOM SYSTEMS CREATED: Small visual water/contact and spectator adapters, compatible saved Animator configuration, explicit versioned authoring, private owned-asset intake, a bounded missing water-film response and targeted diagnostics/tests. Original gameplay remains authoritative.

VENDOR SOURCE MODIFIED: NO.

DUPLICATE VENDOR FUNCTIONALITY CREATED: NO.
